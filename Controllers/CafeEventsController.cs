using System.Security.Claims;
using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos.Events;
using CafeMenu.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class CafeEventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CafeEventsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyEvents()
    {
        var cafeId = int.Parse(User.FindFirstValue("CafeId")!);

        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe is null)
            return NotFound();

        var hasActiveSubscription = await _context.CafeSubscriptions
            .AnyAsync(s => s.CafeId == cafeId && s.IsActive && s.EndDate > DateTime.UtcNow);

        if (!cafe.EventsEnabled || !hasActiveSubscription)
            return Ok(new object[0]);

        var events = await _context.CafeEvents
            .Where(e => e.CafeId == cafeId)
            .OrderByDescending(e => e.EventDate)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.ImageUrl,
                e.Fee,
                e.EventDate,
                eventDateShamsi = PersianDateHelper.ToPersianDateString(e.EventDate),
                e.IsActive
            })
            .ToListAsync();

        return Ok(events);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] AdminCreateEventDto dto)
    {
        var cafeId = int.Parse(User.FindFirstValue("CafeId")!);

        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.ImageUrl))
            return BadRequest("Title, description, and image URL are required.");

        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe is null)
            return NotFound();

        var hasActiveSubscription = await _context.CafeSubscriptions
            .AnyAsync(s => s.CafeId == cafeId && s.IsActive && s.EndDate > DateTime.UtcNow);

        if (!cafe.EventsEnabled || !hasActiveSubscription)
            return StatusCode(StatusCodes.Status403Forbidden, "بخش رویدادها فقط برای کافه‌های دارای اشتراک فعال در دسترس است.");

        var cafeEvent = new CafeMenu.Api.Models.CafeEvent
        {
            CafeId = cafeId,
            Title = dto.Title,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            Fee = dto.Fee,
            EventDate = dto.EventDate,
            IsActive = true
        };

        _context.CafeEvents.Add(cafeEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyEvents), new { id = cafeEvent.Id }, cafeEvent);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] AdminUpdateEventDto dto)
    {
        var cafeId = int.Parse(User.FindFirstValue("CafeId")!);

        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe is null)
            return NotFound();

        var hasActiveSubscription = await _context.CafeSubscriptions
            .AnyAsync(s => s.CafeId == cafeId && s.IsActive && s.EndDate > DateTime.UtcNow);

        if (!cafe.EventsEnabled || !hasActiveSubscription)
            return StatusCode(StatusCodes.Status403Forbidden, "بخش رویدادها فقط برای کافه‌های دارای اشتراک فعال در دسترس است.");

        var cafeEvent = await _context.CafeEvents.FirstOrDefaultAsync(e => e.Id == id && e.CafeId == cafeId);
        if (cafeEvent is null)
            return NotFound();

        cafeEvent.Title = dto.Title;
        cafeEvent.Description = dto.Description;
        cafeEvent.ImageUrl = dto.ImageUrl;
        cafeEvent.Fee = dto.Fee;
        cafeEvent.EventDate = dto.EventDate;
        cafeEvent.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var cafeId = int.Parse(User.FindFirstValue("CafeId")!);

        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe is null)
            return NotFound();

        var hasActiveSubscription = await _context.CafeSubscriptions
            .AnyAsync(s => s.CafeId == cafeId && s.IsActive && s.EndDate > DateTime.UtcNow);

        if (!cafe.EventsEnabled || !hasActiveSubscription)
            return StatusCode(StatusCodes.Status403Forbidden, "بخش رویدادها فقط برای کافه‌های دارای اشتراک فعال در دسترس است.");

        var cafeEvent = await _context.CafeEvents.FirstOrDefaultAsync(e => e.Id == id && e.CafeId == cafeId);
        if (cafeEvent is null)
            return NotFound();

        _context.CafeEvents.Remove(cafeEvent);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
