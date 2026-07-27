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
            .Include(c => c.CafeEvents)
            .FirstOrDefaultAsync(c => c.Id == cafeId && c.PublicAccessKey == accessKey);

        if (cafe is null)
            return NotFound("کافه پیدا نشد");

        var hasActiveSubscription = await _context.CafeSubscriptions
            .AnyAsync(s => s.CafeId == cafeId && s.IsActive && s.EndDate > DateTime.UtcNow);

        if (!cafe.EventsEnabled || !hasActiveSubscription)
            return Ok(Array.Empty<object>());

        var events = cafe.CafeEvents
            .Where(e => e.IsActive)
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
            .ToList();

        return Ok(events);
    }
}
