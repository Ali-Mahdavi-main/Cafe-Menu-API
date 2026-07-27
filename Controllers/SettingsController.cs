using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeMenu.Api.Data;
using System.Security.Claims;
using CafeMenu.Api.Dtos.Settings;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        int cafeId = int.Parse(User.FindFirstValue("CafeId")!);
        var cafe = await _context.Cafes.FindAsync(cafeId);
        if (cafe == null) return NotFound();

        return Ok(new
        {
            cafeName = cafe.Name,
            logoUrl = cafe.LogoUrl,
            address = cafe.Address,
            phone = cafe.Phone,
            instagram = cafe.InstagramUrl,
            workingHours = cafe.WorkingHours,
            eventsEnabled = cafe.EventsEnabled
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto dto)
    {
        int cafeId = int.Parse(User.FindFirstValue("CafeId")!);
        var cafe = await _context.Cafes.FindAsync(cafeId);
        if (cafe == null) return NotFound();

        cafe.Name = dto.CafeName;
        cafe.LogoUrl = dto.LogoUrl;
        cafe.Address = dto.Address;
        cafe.Phone = dto.Phone;                
        cafe.InstagramUrl = dto.Instagram;
        cafe.WorkingHours = dto.WorkingHours;  

        await _context.SaveChangesAsync();
        return NoContent();
    }
    [HttpGet("public-key")]
    public async Task<IActionResult> GetPublicKey()
    {
        int cafeId = int.Parse(User.FindFirstValue("CafeId")!);
        var cafe = await _context.Cafes.FindAsync(cafeId);
        if (cafe == null) return NotFound();

        
        if (string.IsNullOrEmpty(cafe.PublicAccessKey))
        {
            cafe.PublicAccessKey = Guid.NewGuid().ToString("N")[..12];
            await _context.SaveChangesAsync();
        }

        return Ok(new { publicAccessKey = cafe.PublicAccessKey });
    }

    [HttpPost("regenerate-key")]
    public async Task<IActionResult> RegeneratePublicKey()
    {
        int cafeId = int.Parse(User.FindFirstValue("CafeId")!);
        var cafe = await _context.Cafes.FindAsync(cafeId);
        if (cafe == null) return NotFound();

        cafe.PublicAccessKey = Guid.NewGuid().ToString("N")[..12];
        await _context.SaveChangesAsync();

        return Ok(new { publicAccessKey = cafe.PublicAccessKey });
    }
}