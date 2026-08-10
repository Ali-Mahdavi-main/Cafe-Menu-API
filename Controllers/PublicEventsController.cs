using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos.Events;
using CafeMenu.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/public/events")]
[AllowAnonymous]
public class PublicEventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicEventsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{cafeId}/{accessKey}")]
    public async Task<IActionResult> GetEvents(int cafeId, string accessKey)
    {
        var cafe = await _context.Cafes
            .Where(c => c.Id == cafeId && c.PublicAccessKey == accessKey)
            .Select(c => new { c.EventsEnabled })
            .FirstOrDefaultAsync();

        if (cafe is null)
            return NotFound("کافه پیدا نشد");

        // Same class of "hide everything" cases GetMenuByCafe already handles via IsAvailable —
        // events have no such flag, so these are checked explicitly instead of leaking the reason.
        var isDisabled = await _context.CafeDisableStatuses
            .AnyAsync(s => s.CafeId == cafeId && s.IsDisabled);

        if (isDisabled || !cafe.EventsEnabled)
            return Ok(Array.Empty<PublicEventDto>());

        var now = DateTime.UtcNow;
        var hasActiveSubscription = await _context.CafeSubscriptions
            .AnyAsync(s => s.CafeId == cafeId && s.IsActive && s.EndDate > now);

        if (!hasActiveSubscription)
            return Ok(Array.Empty<PublicEventDto>());

        var events = await _context.CafeEvents
            .Where(e => e.CafeId == cafeId && e.IsActive)
            .OrderBy(e => e.EventDate)
            .Select(e => new PublicEventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                ImageUrl = e.ImageUrl,
                Fee = e.Fee,
                EventDateShamsi = PersianDateHelper.ToPersianDateString(e.EventDate)
            })
            .ToListAsync();

        return Ok(events);
    }
}