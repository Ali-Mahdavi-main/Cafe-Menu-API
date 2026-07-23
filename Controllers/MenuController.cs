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
        var cafeIdClaim = User.FindFirstValue("CafeId");
        if (string.IsNullOrEmpty(cafeIdClaim))
            return Unauthorized("توکن نامعتبر است");

        int cafeId = int.Parse(cafeIdClaim);
        var items = await _context.MenuItems
            .Where(m => m.CafeId == cafeId)
            .Include(m => m.Category)
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
    //GET
    [HttpGet("{cafeId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GetMenuItemDto>>> GetMenuByCafe(int cafeId)
    {
        var menuItems = await _context.MenuItems
            .Where(m => m.CafeId == cafeId && m.IsAvailable)
            .Include(m => m.Category)
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


    //POST
    [HttpPost]
    public async Task<ActionResult<IEnumerable<GetMenuItemDto>>> CreateMenuItem([FromBody]  CreateMenuItemDto dto)
    {
        int cafeId = int.Parse(
            User.FindFirstValue("CafeId")!
        );
        var item = new MenuItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            IsAvailable = dto.IsAvailable,
            IsSpecial = dto.IsSpecial,
            CafeId = cafeId
        };
        item.Cafe = null;
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
        return CreatedAtAction(nameof(GetMenuByCafe), new{cafeId = item.CafeId}, resultDto);
    }

    //PUT   
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMenuItem(int id,[FromBody] UpdateMenuItemDto dto)
    {   
        var cafeId = int.Parse(
            User.FindFirstValue("CafeId")!
        );
        var item = await _context.MenuItems.FirstOrDefaultAsync(x => x.Id == id && 
        x.CafeId == cafeId);

        if(item == null) return NotFound("ایتم مورد نظر پیدا نشد");

        item.Title = dto.Title;
        item.CategoryId = dto.CategoryId;
        item.ImageUrl = dto.ImageUrl;
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.IsAvailable = dto.IsAvailable;
        item.IsSpecial = dto.IsSpecial;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.MenuItems.Any(e => e.Id == id)) return NotFound("ایتم پیدا نشد");
            throw;
        }
        return NoContent();
    }

// DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {   
        var cafeId = int.Parse(
            User.FindFirstValue("CafeId")!
        );
        var item = await _context.MenuItems.FirstOrDefaultAsync(x => 
        x.Id == id &&
        x.CafeId == cafeId);
      
        if (item == null) return NotFound();

        _context.MenuItems.Remove(item);
        await _context.SaveChangesAsync(); 

        return NoContent();
    }
}