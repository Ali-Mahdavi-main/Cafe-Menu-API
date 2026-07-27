using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;          // <-- required for AnyAsync
using Microsoft.Extensions.Configuration;
using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos;
using CafeMenu.Api.Models;
using CafeMenu.Api.Services;


namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public AdminController(AppDbContext context, ITokenService tokenService, IConfiguration config)
    {
        _context = context;
        _tokenService = tokenService;
        _config = config;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginDto dto)
    {
        var adminUsername = _config["AdminSettings:Username"];
        var adminPassword = _config["AdminSettings:Password"];

        if (dto.Username != adminUsername || dto.Password != adminPassword)
            return Unauthorized(new { message = "اطلاعات ادمین اشتباه است" });

        var token = _tokenService.GenerateAdminToken(adminUsername, "Admin");
        return Ok(new { token });
    }

    [HttpPost("cafes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateCafe([FromBody] AdminCreateCafeDto dto)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(dto.CafeName) || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("نام کافه، نام کاربری و رمز عبور الزامی است");

        // Check for duplicate username
        bool userExists = await _context.Cafes
            .Where(c => c.UserName == dto.Username)
            .AnyAsync();

        if (userExists)
            return BadRequest("نام کاربری قبلاً استفاده شده است");

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var cafe = new Cafe
        {
            Name = dto.CafeName,
            Address = dto.Address ?? "",
            LogoUrl = dto.LogoUrl ?? "",
            InstagramUrl = dto.InstagramUrl ?? "",
            ThemeConfigJson = dto.ThemeConfigJson ?? "{}",
            Phone = dto.Phone,
            WorkingHours = dto.WorkingHours,
            EventsEnabled = dto.EventsEnabled ?? true,
            UserName = dto.Username,
            PasswordHash = hashedPassword,
            PublicAccessKey = Guid.NewGuid().ToString("N")[..12]
        };

        _context.Cafes.Add(cafe);
        await _context.SaveChangesAsync();

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.IsActive);

        if (plan is null)
        {
            plan = new SubscriptionPlan
            {
                Name = "ماهانه پایه",
                DurationDays = 30,
                Price = 0,
                IsActive = true
            };
            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();
        }

        var initialSubscription = new CafeSubscription
        {
            CafeId = cafe.Id,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.DurationDays),
            IsActive = true,
            WarningCount = 0
        };

        _context.CafeSubscriptions.Add(initialSubscription);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            cafeId = cafe.Id,
            cafeName = cafe.Name,
            username = cafe.UserName,
            eventsEnabled = cafe.EventsEnabled,
            publicMenuUrl = $"{Request.Scheme}://{Request.Host}/menu/{cafe.Id}/{cafe.PublicAccessKey}"
        });
    }
 // GET /api/admin/cafes – list all cafes (admin only)
    [HttpGet("cafes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllCafes()
    {
        var cafes = await _context.Cafes
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.UserName,
                c.Phone,
                c.Address,
                c.InstagramUrl,
                c.LogoUrl,
                c.WorkingHours,
                c.ThemeConfigJson,
                c.EventsEnabled,
                PublicAccessKey = c.PublicAccessKey
            })
            .ToListAsync();

        return Ok(cafes);
    }

    // PUT /api/admin/cafes/{id} – update a cafe (admin only)
    [HttpPut("cafes/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateCafe(int id, [FromBody] AdminUpdateCafeDto dto)
    {
        var cafe = await _context.Cafes.FindAsync(id);
        if (cafe == null) return NotFound();

        cafe.Name = dto.CafeName;
        cafe.Address = dto.Address ?? "";
        cafe.LogoUrl = dto.LogoUrl ?? "";
        cafe.InstagramUrl = dto.InstagramUrl ?? "";
        cafe.Phone = dto.Phone;
        cafe.WorkingHours = dto.WorkingHours;
        cafe.ThemeConfigJson = dto.ThemeConfigJson ?? "{}";
        if (dto.EventsEnabled.HasValue)
        {
            cafe.EventsEnabled = dto.EventsEnabled.Value;
        }

        // Optionally change username/password if provided
        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            // Check duplicate
            bool userExists = await _context.Cafes
                .Where(c => c.UserName == dto.Username && c.Id != id)
                .AnyAsync();
            if (userExists)
                return BadRequest("نام کاربری قبلاً استفاده شده است");

            cafe.UserName = dto.Username;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            cafe.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}