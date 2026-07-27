using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos.Cafe;
using CafeMenu.Api.Models;
using CafeMenu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        public AuthController(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register( [FromBody] RegisterCafeDto dto)
        {
            if (await _context.Cafes.AnyAsync(c => c.UserName == dto.Username))
            {
                return BadRequest("نام کاربری قبلا انتخواب شده است");
            }
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var cafe = new Cafe
            {
                Name = dto.CafeName,
                Address = dto.Address,
                LogoUrl = dto.LogoUrl,
                InstagramUrl = dto.InstagramUrl,
                ThemeConfigJson = dto.ThemeConfigJson,
                UserName = dto.Username,
                PasswordHash = hashedPassword
            };
            _context.Cafes.Add(cafe);
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(cafe);
            return Ok(new
            {
                token,
                cafeName = cafe.Name
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.UserName == dto.Username);
            if (cafe == null) return Unauthorized(new {message = "نام کاربری یا رمز عبور اشتباه است"});

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, cafe.PasswordHash);
            if(!isPasswordValid) return Unauthorized(new{message = "نام کاربری یا رمز عبور اشتباه است"});

            var token = _tokenService.GenerateToken(cafe);
            
            return Ok(new { 
            token,
            cafeName = cafe.Name,
            theme = cafe.ThemeConfigJson,
            instagram = cafe.InstagramUrl
            });
        }
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var cafeIdClaim = User.FindFirst("CafeId")?.Value;
            var userName = User.Identity?.Name;
            var cafeName = User.FindFirst("CafeName")?.Value;

            if (string.IsNullOrEmpty(cafeIdClaim) || !int.TryParse(cafeIdClaim, out var cafeId))
            {
                return Unauthorized();
            }

            var latestSubscription = await _context.CafeSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.CafeId == cafeId)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            var now = DateTime.UtcNow;
            object? subscriptionInfo = null;

            if (latestSubscription != null)
            {
                var remainingDays = latestSubscription.EndDate > now
                    ? (int)Math.Ceiling((latestSubscription.EndDate - now).TotalDays)
                    : 0;

                string status;
                if (latestSubscription.IsActive && latestSubscription.EndDate > now)
                {
                    status = "فعال";
                }
                else if (latestSubscription.GracePeriodEnd.HasValue && latestSubscription.GracePeriodEnd.Value > now)
                {
                    status = "در دوره تمدید";
                }
                else
                {
                    status = "غیر فعال";
                }

                subscriptionInfo = new
                {
                    status,
                    isActive = latestSubscription.IsActive,
                    planName = latestSubscription.Plan?.Name,
                    remainingDays,
                    warningCount = latestSubscription.WarningCount,
                    endDate = latestSubscription.EndDate,
                    gracePeriodEnd = latestSubscription.GracePeriodEnd
                };
            }

            var cafe = await _context.Cafes.FindAsync(cafeId);

            object eventsEnabled = cafe?.EventsEnabled ?? true;

            return Ok(new
            {
                cafeId,
                userName,
                cafeName,
                eventsEnabled,
                subscription = subscriptionInfo
            });
        }
    }
}