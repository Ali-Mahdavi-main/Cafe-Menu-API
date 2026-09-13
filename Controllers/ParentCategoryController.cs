using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CafeMenu.Api.Data;
using CafeMenu.Api.Models;
using CafeMenu.Api.Dtos.Category;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ParentCategoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public ParentCategoryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<GetParentCategoryDto>>> GetMyParentCategories()
    {
        var cafeIdClaim = User.FindFirstValue("CafeId");
        if (string.IsNullOrEmpty(cafeIdClaim))
            return Unauthorized("توکن نامعتبر است");

        int cafeId = int.Parse(cafeIdClaim);

        var parentCategories = await _context.ParentCategories
            .Where(pc => pc.CafeId == cafeId)
            .Select(pc => new GetParentCategoryDto
            {
                Id = pc.Id,
                Name = pc.Name,
                IsEnabled = pc.IsEnabled,
                CategoryCount = pc.Categories.Count
            })
            .ToListAsync();

        return Ok(parentCategories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetParentCategoryDto>> GetParentCategory(int id)
    {
        var cafeIdClaim = User.FindFirstValue("CafeId");
        if (string.IsNullOrEmpty(cafeIdClaim))
            return Unauthorized("توکن نامعتبر است");

        int cafeId = int.Parse(cafeIdClaim);

        var parentCategory = await _context.ParentCategories
            .Where(pc => pc.Id == id && pc.CafeId == cafeId)
            .Select(pc => new GetParentCategoryDto
            {
                Id = pc.Id,
                Name = pc.Name,
                IsEnabled = pc.IsEnabled,
                CategoryCount = pc.Categories.Count
            })
            .FirstOrDefaultAsync();

        if (parentCategory == null) return NotFound("دسته‌بندی والد یافت نشد");
        return Ok(parentCategory);
    }

    [HttpPost]
    public async Task<ActionResult> CreateParentCategory([FromBody] CreateParentCategoryDto dto)
    {
        int cafeId = int.Parse(User.FindFirstValue("CafeId")!);

        var parentCategory = new ParentCategory
        {
            Name = dto.Name,
            CafeId = cafeId,
            IsEnabled = true
        };

        _context.ParentCategories.Add(parentCategory);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateParentCategory(int id, [FromBody] ModifyParentCategory dto)
    {
        int cafeId = int.Parse(User.FindFirstValue("CafeId")!);

        var parentCategory = await _context.ParentCategories
            .FirstOrDefaultAsync(pc => pc.Id == id && pc.CafeId == cafeId);

        if (parentCategory == null) return NotFound("دسته‌بندی والد یافت نشد");

        parentCategory.Name = dto.Name;
        parentCategory.IsEnabled = dto.IsEnabled;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.ParentCategories.Any(e => e.Id == id)) return NotFound("دسته‌بندی والد یافت نشد");
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteParentCategory(int id)
    {
        int cafeId = int.Parse(User.FindFirstValue("CafeId")!);

        var parentCategory = await _context.ParentCategories
            .FirstOrDefaultAsync(pc => pc.Id == id && pc.CafeId == cafeId);

        if (parentCategory == null) return NotFound("دسته‌بندی والد یافت نشد");

        _context.ParentCategories.Remove(parentCategory);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> ToggleParentCategory(int id)
    {
        int cafeId = int.Parse(User.FindFirstValue("CafeId")!);

        var parentCategory = await _context.ParentCategories
            .FirstOrDefaultAsync(pc => pc.Id == id && pc.CafeId == cafeId);

        if (parentCategory == null) return NotFound("دسته‌بندی والد یافت نشد");

        parentCategory.IsEnabled = !parentCategory.IsEnabled;
        await _context.SaveChangesAsync();

        return Ok(new { isEnabled = parentCategory.IsEnabled });
    }
}
