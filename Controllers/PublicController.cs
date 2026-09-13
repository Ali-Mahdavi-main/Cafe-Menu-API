using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeMenu.Api.Data;
using CafeMenu.Api.Helpers;
using CafeMenu.Api.Models;

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
            .AsNoTracking()
            .Where(c => c.Id == cafeId && c.PublicAccessKey == accessKey)
            .Select(c => new
            {
                c.Name,
                c.LogoUrl,
                c.Address,
                c.Phone,
                c.InstagramUrl,
                c.WorkingHours,
                c.EventsEnabled,
                c.ThemeConfigJson,
                ParentCategories = c.Categories
                    .Where(cat => cat.ParentCategoryId != null && cat.ParentCategory.IsEnabled)
                    .Select(cat => new
                    {
                        parentId = cat.ParentCategory.Id,
                        parentName = cat.ParentCategory.Name,
                        subCategories = new[] { cat }.Select(cat2 => new
                        {
                            cat2.Id,
                            cat2.Name,
                            cat2.ParentCategoryId,
                            Items = cat2.MenuItems
                                .Where(m => m.IsAvailable)
                                .Select(m => new
                                {
                                    m.Id,
                                    m.Title,
                                    m.Description,
                                    m.Price,
                                    m.ImageUrl,
                                    m.IsAvailable,
                                    m.IsSpecial
                                })
                        })
                    })
                    .GroupBy(x => new { x.parentId, x.parentName })
                    .Select(g => new
                    {
                        parentId = g.Key.parentId,
                        parentName = g.Key.parentName,
                        subCategories = g.SelectMany(x => x.subCategories).ToList()
                    })
                    .Where(pc => pc.subCategories.Any(sc => sc.Items.Any()))
            })
            .FirstOrDefaultAsync();

        if (cafe == null)
            return NotFound("کافه پیدا نشد");

        var theme = new Dictionary<string, object?>
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
            { "imageAspectRatio", "1/1" },
            { "textAspectRatio", "auto" },
            { "headerStyle", 1 },
            { "footerStyle", 1 },
            { "specialCardEnabled", false },
            { "specialCardItemId", null }
        };

        if (!string.IsNullOrEmpty(cafe.ThemeConfigJson))
        {
            try
            {
                var custom = JsonSerializer.Deserialize<Dictionary<string, object>>(cafe.ThemeConfigJson);
                if (custom != null)
                {
                    foreach (var kv in custom)
                        theme[kv.Key] = kv.Value;
                }
            }
            catch
            {
            }
        }

        var menu = cafe.ParentCategories.Select(pc => new
        {
            parentCategoryName = pc.parentName,
            parentCategoryId = pc.parentId,
            subCategories = pc.subCategories.Select(sc => new
            {
                categoryId = sc.Id,
                categoryName = sc.Name,
                items = sc.Items.Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    description = m.Description,
                    price = m.Price,
                    imageUrl = ImageUrlHelper.ToAbsolute(m.ImageUrl, Request),
                    isAvailable = m.IsAvailable,
                    isSpecial = m.IsSpecial
                })
            })
        });

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