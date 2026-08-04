using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CafeMenu.Api.Data;
using CafeMenu.Api.Helpers;
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
    private readonly ISubscriptionService _subscriptionService;

    public AdminController(
        AppDbContext context,
        ITokenService tokenService,
        IConfiguration config,
        ISubscriptionService subscriptionService)
    {
        _context = context;
        _tokenService = tokenService;
        _config = config;
        _subscriptionService = subscriptionService;
    }

    // ==============================
    // ADMIN LOGIN
    // ==============================
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

    // ==============================
    // CAFE MANAGEMENT
    // ==============================
    [HttpPost("cafes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateCafe([FromBody] AdminCreateCafeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CafeName) || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("نام کافه، نام کاربری و رمز عبور الزامی است");

        bool userExists = await _context.Cafes.AnyAsync(c => c.UserName == dto.Username);
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

        // Trial assignment now goes through one shared method (also used by public
        // self-registration) so trial terms can't drift or accidentally pick up
        // whatever paid plan happens to sort first in the table.
        await _subscriptionService.AssignTrialSubscriptionAsync(cafe.Id);

        return Ok(new
        {
            cafeId = cafe.Id,
            cafeName = cafe.Name,
            username = cafe.UserName,
            eventsEnabled = cafe.EventsEnabled,
            publicMenuUrl = $"{Request.Scheme}://{Request.Host}/menu/{cafe.Id}/{cafe.PublicAccessKey}"
        });
    }

    [HttpGet("cafes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllCafes()
    {
        var disabledIds = await _context.CafeDisableStatuses
            .Where(s => s.IsDisabled)
            .Select(s => s.CafeId)
            .ToListAsync();

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

        var result = cafes.Select(c => new
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
            c.PublicAccessKey,
            IsDisabled = disabledIds.Contains(c.Id)
        });

        return Ok(result);
    }

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
            cafe.EventsEnabled = dto.EventsEnabled.Value;

        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            bool userExists = await _context.Cafes.AnyAsync(c => c.UserName == dto.Username && c.Id != id);
            if (userExists)
                return BadRequest("نام کاربری قبلاً استفاده شده است");
            cafe.UserName = dto.Username;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
            cafe.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ==============================
    // DISABLE / ENABLE CAFE
    // ==============================
    [HttpPost("cafes/{id}/disable")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DisableCafe(int id)
    {
        var cafe = await _context.Cafes.FindAsync(id);
        if (cafe == null) return NotFound("کافه یافت نشد");

        var status = await _context.CafeDisableStatuses
            .FirstOrDefaultAsync(s => s.CafeId == id);
        if (status == null)
        {
            status = new CafeDisableStatus
            {
                CafeId = id,
                IsDisabled = true,
                DisabledAt = DateTime.UtcNow,
                DisabledBy = User.Identity?.Name ?? "admin"
            };
            _context.CafeDisableStatuses.Add(status);
        }
        else
        {
            status.IsDisabled = true;
            status.DisabledAt = DateTime.UtcNow;
            status.DisabledBy = User.Identity?.Name ?? "admin";
        }

        // NOTE: subscriptions are intentionally left untouched here. Admin-disable
        // is an independent switch from billing state — Enable below relies on the
        // subscription's real IsActive/EndDate to decide whether to restore the menu.
        await SetMenuAvailabilityAsync(id, false);

        await _context.SaveChangesAsync();
        return Ok(new { message = "کافه غیرفعال شد", cafeId = id });
    }

    [HttpPost("cafes/{id}/enable")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EnableCafe(int id)
    {
        var cafe = await _context.Cafes.FindAsync(id);
        if (cafe == null) return NotFound("کافه یافت نشد");

        var status = await _context.CafeDisableStatuses
            .FirstOrDefaultAsync(s => s.CafeId == id);
        if (status != null)
            status.IsDisabled = false;

        var activeSub = await _context.CafeSubscriptions
            .FirstOrDefaultAsync(s => s.CafeId == id && s.IsActive && s.EndDate > DateTime.UtcNow);
        if (activeSub != null)
            await SetMenuAvailabilityAsync(id, true);

        await _context.SaveChangesAsync();
        return Ok(new { message = "کافه فعال شد", cafeId = id });
    }

    // ==============================
    // PLAN MANAGEMENT
    // ==============================
    [HttpGet("plans")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.DurationDays,
                p.Price,
                p.Discount,
                p.IsFeatured,
                priceAfterDiscount = p.Price - (p.Price * p.Discount / 100),
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost("plans")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreatePlan([FromBody] AdminCreatePlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Plan name is required.");

        var plan = new SubscriptionPlan
        {
            Name = dto.Name,
            Description = dto.Description ?? "",
            DurationDays = dto.DurationDays ?? 30,
            Price = dto.Price,
            Discount = dto.Discount ?? 0,
            IsFeatured = dto.IsFeatured,
            IsActive = true
        };

        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPlans), new { id = plan.Id }, plan);
    }

    [HttpPut("plans/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] AdminUpdatePlanDto dto)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Name)) plan.Name = dto.Name;
        if (dto.Description is not null) plan.Description = dto.Description;
        if (dto.DurationDays > 0) plan.DurationDays = dto.DurationDays;

        // Requires AdminUpdatePlanDto.Price to be `decimal?` — see note above the file.
        if (dto.Price.HasValue) plan.Price = dto.Price.Value;

        if (dto.Discount is not null) plan.Discount = dto.Discount.Value;
        if (dto.IsFeatured is not null) plan.IsFeatured = dto.IsFeatured.Value;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("plans/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeletePlan(int id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan is null) return NotFound();

        plan.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ==============================
    // STATS
    // ==============================
    [HttpGet("cafes/stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetCafeStats()
    {
        var now = DateTime.UtcNow;

        var totalCafes = await _context.Cafes.CountAsync();
        var activeSubscriptions = await _context.CafeSubscriptions
            .CountAsync(s => s.IsActive && s.EndDate > now);
        var expiredSubscriptions = await _context.CafeSubscriptions
            .CountAsync(s => s.IsActive && s.EndDate <= now);
        var gracePeriodSubscriptions = await _context.CafeSubscriptions
            .CountAsync(s => s.GracePeriodStart.HasValue && s.GracePeriodEnd > now && s.EndDate <= now);
        var freeSubscriptions = await _context.CafeSubscriptions
            .CountAsync(s => s.IsFree && s.IsActive);

        var recentSubscriptions = await _context.CafeSubscriptions
            .Include(s => s.Cafe)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.StartDate)
            .Take(10)
            .Select(s => new
            {
                s.Id,
                cafeName = s.Cafe.Name,
                planName = s.Plan.Name,
                isActive = s.IsActive && s.EndDate > now,
                startDateShamsi = PersianDateHelper.ToPersianDateString(s.StartDate),
                endDateShamsi = PersianDateHelper.ToPersianDateString(s.EndDate),
                s.IsFree
            })
            .ToListAsync();

        var cafesExpiringSoon = await _context.CafeSubscriptions
            .Include(s => s.Cafe)
            .Include(s => s.Plan)
            .Where(s => s.IsActive && s.EndDate > now && s.EndDate <= now.AddDays(5))
            .Select(s => new
            {
                cafeId = s.Cafe.Id,
                cafeName = s.Cafe.Name,
                planName = s.Plan.Name,
                endDateShamsi = PersianDateHelper.ToPersianDateString(s.EndDate),
                daysRemaining = (int)(s.EndDate - now).TotalDays
            })
            .ToListAsync();

        return Ok(new
        {
            totalCafes,
            activeSubscriptions,
            expiredSubscriptions,
            gracePeriodSubscriptions,
            freeSubscriptions,
            recentSubscriptions,
            cafesExpiringSoon
        });
    }

    // ==============================
    // FREE SUBSCRIPTION ASSIGNMENT (manual admin grant — distinct from the
    // automatic trial, since here the admin explicitly picks a plan/duration)
    // ==============================
    [HttpPost("cafes/{id}/free-subscription")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GiveFreeSubscription(int id, [FromBody] AdminFreeSubscriptionDto dto)
    {
        var cafe = await _context.Cafes.FindAsync(id);
        if (cafe is null) return NotFound("کافه پیدا نشد");

        var planId = dto.PlanId;
        if (planId <= 0)
        {
            var freePlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.IsActive && p.Price == 0);
            if (freePlan == null)
            {
                freePlan = new SubscriptionPlan
                {
                    Name = "رایگان",
                    DurationDays = dto.DurationDays > 0 ? dto.DurationDays : 30,
                    Price = 0,
                    IsActive = true
                };
                _context.SubscriptionPlans.Add(freePlan);
                await _context.SaveChangesAsync();
            }
            planId = freePlan.Id;
        }

        var plan = await _context.SubscriptionPlans.FindAsync(planId);
        if (plan is null) return BadRequest("پلن معتبر نیست");

        var existingActive = await _context.CafeSubscriptions
            .Where(s => s.CafeId == id && s.IsActive)
            .ToListAsync();
        foreach (var sub in existingActive)
            sub.IsActive = false;

        var subscription = new CafeSubscription
        {
            CafeId = id,
            PlanId = planId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.DurationDays),
            IsActive = true,
            IsFree = true,
            WarningCount = 0
        };

        _context.CafeSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        await SetMenuAvailabilityAsync(id, true);

        return Ok(new
        {
            subscriptionId = subscription.Id,
            cafeId = id,
            planName = plan.Name,
            endDateShamsi = PersianDateHelper.ToPersianDateString(subscription.EndDate),
            isFree = true
        });
    }

    // ==============================
    // PRIVATE HELPER
    // ==============================
    private async Task SetMenuAvailabilityAsync(int cafeId, bool available)
    {
        var menuItems = await _context.MenuItems
            .Where(m => m.CafeId == cafeId)
            .ToListAsync();

        foreach (var item in menuItems)
            item.IsAvailable = available;

        await _context.SaveChangesAsync();
    }
}