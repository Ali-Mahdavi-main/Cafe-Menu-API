using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos;
using CafeMenu.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Controllers;
[ApiController]
[Route("api/[controller]")] // api/menu
public class MenuController : ControllerBase
{
    private readonly AppDbContext _context;

    public MenuController(AppDbContext context)
    {
        _context = context;
    }

    //GET
    [HttpGet("{cafeId}")]
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
                CategoryName = m.Category != null ? m.Category.Name : "بدون دسته بندی"
            })
            .ToListAsync();
        return Ok(menuItems);
    }


    //POST
    [HttpPost]
    public async Task<ActionResult<IEnumerable<GetMenuItemDto>>> CreateMenuItem([FromBody]  CreateMenuItemDto dto)
    {
        var item = new MenuItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            CafeId = dto.CafeId,
            CategoryId = dto.CategoryId,
            IsAvailable = dto.IsAvailable,

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
            CategoryName = string.Empty
        };
        return CreatedAtAction(nameof(GetMenuByCafe), new{cafeId = item.CafeId}, resultDto);
    }

    //PUT   
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMenuItem(int id,[FromQuery] int currentCafeId ,[FromBody] UpdateMenuItemDto dto)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if(item == null) return NotFound("ایتم مورد نظر پیدا نشد");
                if (item.CafeId != currentCafeId)
        {
            return Forbid("شما اجازه دسترسی ندارید");
        }
        item.Title = dto.Title;
        item.CategoryId = dto.CategoryId;
        item.ImageUrl = dto.ImageUrl;
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.IsAvailable = dto.IsAvailable;
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
    public async Task<IActionResult> DeleteMenuItem(int id, [FromQuery] int currentCafeId)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null) return NotFound();
        if (item.CafeId != currentCafeId)
        {
            return Forbid("شما اجازه دسترسی ندارید");
        }
        _context.MenuItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}