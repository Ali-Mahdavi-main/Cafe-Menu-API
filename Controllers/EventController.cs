using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos.Events;
using CafeMenu.Api.Helpers;
using CafeMenu.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/admin/events")]
[Authorize(Policy = "AdminOnly")]
public class EventController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.CafeEvents
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
        if (dto.CafeId <= 0 || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.ImageUrl))
            return BadRequest("CafeId, title, description, and image URL are required.");

        var cafe = await _context.Cafes.FindAsync(dto.CafeId);
        if (cafe is null)
            return BadRequest("The selected cafe does not exist.");

        var cafeEvent = new CafeEvent
        {
            CafeId = dto.CafeId,
            Title = dto.Title,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            Fee = dto.Fee,
            EventDate = dto.EventDate,
            IsActive = true
        };

        _context.CafeEvents.Add(cafeEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = cafeEvent.Id }, cafeEvent);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var cafeEvent = await _context.CafeEvents.FindAsync(id);
        if (cafeEvent is null)
            return NotFound();

        return Ok(new
        {
            cafeEvent.Id,
            cafeEvent.Title,
            cafeEvent.Description,
            cafeEvent.ImageUrl,
            cafeEvent.Fee,
            cafeEvent.EventDate,
            eventDateShamsi = PersianDateHelper.ToPersianDateString(cafeEvent.EventDate),
            cafeEvent.IsActive
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] AdminUpdateEventDto dto)
    {
        var cafeEvent = await _context.CafeEvents.FindAsync(id);
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
        var cafeEvent = await _context.CafeEvents.FindAsync(id);
        if (cafeEvent is null)
            return NotFound();

        _context.CafeEvents.Remove(cafeEvent);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
