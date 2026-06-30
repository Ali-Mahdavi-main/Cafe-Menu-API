using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CafeMenu.Api.Data;
using CafeMenu.Api.Models;
using CafeMenu.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using CafeMenu.Api.Dtos.Category;

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
        var category = new Category
        {
            Name = dto.Name,
            CafeId = dto.CafeId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
       
        return NoContent();
    }
}