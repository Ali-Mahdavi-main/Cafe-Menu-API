
namespace CafeMenu.Api.Dtos
{
    public class UpdateMenuItemDto
    {
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl {get; set;} = string.Empty;
    public bool IsAvailable { get; set; }
    public int CategoryId { get; set; }
    }
}