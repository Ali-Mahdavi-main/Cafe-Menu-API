using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos.Events;
using CafeMenu.Api.Helpers;
using CafeMenu.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventController(AppDbContext context) => _context = context;

    // Safely extract CafeId from JWT
    private int GetCafeId()
    {
        var claim = User.FindFirst("CafeId")?.Value;
        return int.TryParse(claim, out var cafeId) ? cafeId : 0;
    }

    // GET /api/events – only for the logged‑in café
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var cafeId = GetCafeId();
        if (cafeId == 0)
            return Unauthorized(new { message = "Cafe not identified" });

        // 1. Fetch from DB without any conversion
        var events = await _context.CafeEvents
            .Where(e => e.CafeId == cafeId)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();                       // <-- materialise here

        // 2. Map to response objects safely in memory
        var result = events.Select(e => new
        {
            e.Id,
            e.Title,
            e.Description,
            e.ImageUrl,
            e.Fee,
            e.EventDate,
            eventDateShamsi = PersianDateHelper.ToPersianDateString(e.EventDate),
            e.IsActive
        });

        return Ok(result);
    }

    // POST /api/events
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        var cafeId = GetCafeId();
        if (cafeId == 0) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Title) ||
            string.IsNullOrWhiteSpace(dto.Description) ||
            string.IsNullOrWhiteSpace(dto.ImageUrl) )
            return BadRequest("Title, description, and image URL are required.");
         if (dto.EventDate < new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc))
         return BadRequest("Event date is too far in the past.");   

        var cafeEvent = new CafeEvent
        {
            CafeId = cafeId,
            Title = dto.Title,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            Fee = dto.Fee,
            EventDate = dto.EventDate == default ? DateTime.UtcNow : dto.EventDate,
            IsActive = true
        };

        _context.CafeEvents.Add(cafeEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = cafeEvent.Id }, new
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var cafeId = GetCafeId();
        var cafeEvent = await _context.CafeEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.CafeId == cafeId);

        if (cafeEvent is null) return NotFound();

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
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
    {
        var cafeId = GetCafeId();
        var cafeEvent = await _context.CafeEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.CafeId == cafeId);

        if (cafeEvent is null) return NotFound();

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
        var cafeId = GetCafeId();
        var cafeEvent = await _context.CafeEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.CafeId == cafeId);

        if (cafeEvent is null) return NotFound();

        _context.CafeEvents.Remove(cafeEvent);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}