
using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.Dtos.MenuItem
{
    public class CreateMenuItemDto
    {
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    [Range(0, 100000000)]
    public decimal Price { get; set; }
    [Url]
    public string ImageUrl {get; set;} = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public bool IsSpecial { get; set; } = false;
    
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
   
    }
}