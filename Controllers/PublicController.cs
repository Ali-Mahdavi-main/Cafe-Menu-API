using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeMenu.Api.Data;
using CafeMenu.Api.Helpers;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/public/{cafeId}/{accessKey}")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicMenu(int cafeId, string accessKey)
    {
        var cafe = await _context.Cafes
            .Include(c => c.Categories)
                .ThenInclude(cat => cat.MenuItems)
            .Include(c => c.CafeEvents)
            .FirstOrDefaultAsync(c => c.Id == cafeId);

        if (cafe == null || cafe.PublicAccessKey != accessKey)
            return NotFound("کافه پیدا نشد");

        // --- Build default theme as a mutable Dictionary ---
        var theme = new Dictionary<string, object>
        {
            { "primaryColor", "#1e293b" },
            { "secondaryColor", "#64748b" },
            { "backgroundColor", "#f8fafc" },
            { "textColor", "#0f172a" },
            { "cardBackground", "#ffffff" },
            { "borderColor", "#e2e8f0" },
            { "borderRadius", 16 },
            { "shadow", "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1)" },
            { "fontFamily", "Vazirmatn" },
            { "headingFontSize", 28 },
            { "bodyFontSize", 16 },
            { "priceColor", "#0d9488" },
            { "cardWidth", 300 },
            { "cardHeight", 300 },
            { "imageAspectRatio", "1/1" },   // e.g. "1/1", "4/3", "16/9"
            { "textAspectRatio", "auto" },   // "auto" or a ratio like "1/2"
            { "headerStyle", 1 },
            { "footerStyle", 1 },
            { "specialCardEnabled", false },  // to show a featured item at top
            { "specialCardItemId", null } 
                    };

        // --- Merge custom theme from database (if any) ---
        if (!string.IsNullOrEmpty(cafe.ThemeConfigJson))
        {
            try
            {
                var custom = JsonSerializer.Deserialize<Dictionary<string, object>>(cafe.ThemeConfigJson);
                if (custom != null)
                {
                    foreach (var kv in custom)
                    {
                        theme[kv.Key] = kv.Value;   // overwrite default with custom value
                    }
                }
            }
            catch { /* ignore malformed JSON, keep defaults */ }
        }

        // --- Build menu structure ---
        var menu = cafe.Categories
    .Where(cat => cat.MenuItems.Any(m => m.IsAvailable))
    .Select(cat => new
    {
        categoryName = cat.Name,
        items = cat.MenuItems
            .Where(m => m.IsAvailable)
            .Select(m => new
            {
                id = m.Id,
                title = m.Title,
                description = m.Description,
                price = m.Price,
                imageUrl = ImageUrlHelper.ToAbsolute(m.ImageUrl, Request),  // <--
                isAvailable = m.IsAvailable,
                IsSpecial = m.IsSpecial
            })
    })
    .ToList();

return Ok(new
    {
        cafeName = cafe.Name,
        logoUrl = ImageUrlHelper.ToAbsolute(cafe.LogoUrl, Request),
        address = cafe.Address,
        phone = cafe.Phone,
        instagram = cafe.InstagramUrl,
        workingHours = cafe.WorkingHours,
        eventsEnabled = cafe.EventsEnabled,
        theme,
        menu
    });
    }
}