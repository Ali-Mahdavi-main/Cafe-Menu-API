namespace CafeMenu.Api.Models;

public class CafeEvent
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Fee { get; set; }
    public DateTime EventDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
