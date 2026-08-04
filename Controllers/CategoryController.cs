using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CafeMenu.Api.Data;
using CafeMenu.Api.Models;
using CafeMenu.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using CafeMenu.Api.Dtos.Category;
using System.Security.Claims;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<GetCategoryDto>>> GetMyCategories()
    {
        var cafeIdClaim = User.FindFirstValue("CafeId");
        if (string.IsNullOrEmpty(cafeIdClaim))
            return Unauthorized("توکن نامعتبر است");

        int cafeId = int.Parse(cafeIdClaim);

        var categories = await _context.Categories
            .Where(c => c.CafeId == cafeId)
            .Include(c => c.Cafe)
            .Select(c => new GetCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                CafeName = c.Cafe.Name
            })
            .ToListAsync();

        return Ok(categories);
    }
    // گرفتن تمام دسته‌بندی‌های یک کافه خاص
    [HttpGet("{cafeId}")]
    public async Task<ActionResult<List<GetCategoryDto>>> GetCategoriesByCafe(int cafeId)
    {
        var categories = await _context.Categories
            .Where(c => c.CafeId == cafeId)
            .Include(c => c.Cafe)
            .Select(c => new GetCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                CafeName = c.Cafe.Name
            })
            .ToListAsync();

        return Ok(categories);
    }

  
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        int cafeId = int.Parse(
            User.FindFirstValue("CafeId")!
        );
        var category = new Category
        {
            Name = dto.Name,
            CafeId = cafeId
        };
        category.Cafe = null;
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
       
        return NoContent();
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id,[FromBody] ModiifyCategory dto)
    {   
        var cafeId = int.Parse(
            User.FindFirstValue("CafeId")!
        );
        var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id && 
        x.CafeId == cafeId);

        if(category == null) return NotFound("ایتم مورد نظر پیدا نشد");

        category.Name = dto.Name;
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Categories.Any(e => e.Id == id)) return NotFound("ایتم پیدا نشد");
            throw;
        }
        return NoContent();
    }

// DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {   
        var cafeId = int.Parse(
            User.FindFirstValue("CafeId")!
        );
        var category = await _context.Categories.FirstOrDefaultAsync(x => 
        x.Id == id &&
        x.CafeId == cafeId);
      
        if (category == null) return NotFound();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(); 

        return NoContent();
    }
}