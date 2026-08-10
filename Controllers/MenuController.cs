using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos;
using CafeMenu.Api.Models;
using Microsoft.EntityFrameworkCore;
using CafeMenu.Api.Dtos.MenuItem;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // api/menu
[Authorize]
public class MenuController : ControllerBase
{
    private readonly AppDbContext _context;

    public MenuController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/menu – for dashboard (uses logged-in cafeId, returns all items)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetMenuItemDto>>> GetMyMenu()
    {
        if (!TryGetCafeId(out var cafeId))
            return Unauthorized("توکن نامعتبر است");

        var items = await _context.MenuItems
            .Where(m => m.CafeId == cafeId)
            .Include(m => m.Category)
            .OrderBy(m => m.Category != null ? m.Category.Name : string.Empty)
            .ThenBy(m => m.Title)
            .Select(m => new GetMenuItemDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Price = m.Price,
                ImageUrl = m.ImageUrl,
                IsAvailable = m.IsAvailable,
                IsSpecial = m.IsSpecial,
                CategoryName = m.Category != null ? m.Category.Name : "بدون دسته بندی"
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/menu/{cafeId} – public menu (only available items)
    [HttpGet("{cafeId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GetMenuItemDto>>> GetMenuByCafe(int cafeId)
    {
        var menuItems = await _context.MenuItems
            .Where(m => m.CafeId == cafeId && m.IsAvailable)
            .Include(m => m.Category)
            .OrderBy(m => m.Category != null ? m.Category.Name : string.Empty)
            .ThenBy(m => m.Title)
            .Select(m => new GetMenuItemDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Price = m.Price,
                ImageUrl = m.ImageUrl,
                IsAvailable = m.IsAvailable,
                IsSpecial = m.IsSpecial,
                CategoryName = m.Category != null ? m.Category.Name : "بدون دسته بندی"
            })
            .ToListAsync();

        return Ok(menuItems);
    }

    // POST /api/menu
    [HttpPost]
    public async Task<ActionResult<GetMenuItemDto>> CreateMenuItem([FromBody] CreateMenuItemDto dto)
    {
        if (!TryGetCafeId(out var cafeId))
            return Unauthorized("توکن نامعتبر است");

        var validationError = await ValidateItemAsync(dto.Title, dto.Price, dto.CategoryId, cafeId);
        if (validationError != null)
            return BadRequest(validationError);

        var item = new MenuItem
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            IsAvailable = dto.IsAvailable,
            IsSpecial = dto.IsSpecial,
            CafeId = cafeId
        };

        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();

        var resultDto = new GetMenuItemDto
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Price = item.Price,
            ImageUrl = item.ImageUrl,
            IsAvailable = item.IsAvailable,
            IsSpecial = item.IsSpecial,
            CategoryName = string.Empty
        };

        return CreatedAtAction(nameof(GetMenuByCafe), new { cafeId = item.CafeId }, resultDto);
    }

    // PUT /api/menu/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMenuItem(int id, [FromBody] UpdateMenuItemDto dto)
    {
        if (!TryGetCafeId(out var cafeId))
            return Unauthorized("توکن نامعتبر است");

        var item = await _context.MenuItems
            .FirstOrDefaultAsync(x => x.Id == id && x.CafeId == cafeId);

        if (item == null)
            return NotFound("آیتم مورد نظر پیدا نشد");

        var validationError = await ValidateItemAsync(dto.Title, dto.Price, dto.CategoryId, cafeId);
        if (validationError != null)
            return BadRequest(validationError);

        item.Title = dto.Title.Trim();
        item.Description = dto.Description?.Trim() ?? string.Empty;
        item.CategoryId = dto.CategoryId;
        item.ImageUrl = dto.ImageUrl;
        item.Price = dto.Price;
        item.IsAvailable = dto.IsAvailable;
        item.IsSpecial = dto.IsSpecial;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.MenuItems.AnyAsync(e => e.Id == id))
                return NotFound("آیتم پیدا نشد");
            throw;
        }

        return NoContent();
    }

    // DELETE /api/menu/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {
        if (!TryGetCafeId(out var cafeId))
            return Unauthorized("توکن نامعتبر است");

        var item = await _context.MenuItems
            .FirstOrDefaultAsync(x => x.Id == id && x.CafeId == cafeId);

        if (item == null)
            return NotFound();

        _context.MenuItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ==============================
    // PRIVATE HELPERS
    // ==============================

    private bool TryGetCafeId(out int cafeId)
    {
        var claim = User.FindFirstValue("CafeId");
        return int.TryParse(claim, out cafeId);
    }

    /// <summary>
    /// Shared validation for create/update: required title, non-negative price, and — importantly —
    /// confirms the chosen category (if any) actually belongs to this cafe, so one cafe can't attach
    /// its menu items to another cafe's category by passing an arbitrary CategoryId.
    /// Assumes CategoryId is a non-nullable int where 0 means "no category" (matches your frontend's
    /// `form.categoryId ? parseInt(form.categoryId) : 0` convention) — adjust if your DTO differs.
    /// </summary>
    private async Task<string?> ValidateItemAsync(string? title, decimal price, int categoryId, int cafeId)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "عنوان الزامی است";

        if (price < 0)
            return "قیمت نمی‌تواند منفی باشد";

        if (categoryId != 0)
        {
            var categoryBelongsToCafe = await _context.Categories
                .AnyAsync(c => c.Id == categoryId && c.CafeId == cafeId);

            if (!categoryBelongsToCafe)
                return "دسته‌بندی انتخاب‌شده معتبر نیست";
        }

        return null;
    }
}