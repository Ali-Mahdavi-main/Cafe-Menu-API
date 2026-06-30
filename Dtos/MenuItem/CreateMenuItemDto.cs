
namespace CafeMenu.Api.Dtos
{
    public class CreateMenuItemDto
    {
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl {get; set;} = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public int CategoryId { get; set; }
    public int CafeId { get; set; } 
    }
}