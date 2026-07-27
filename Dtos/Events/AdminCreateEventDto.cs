namespace CafeMenu.Api.Dtos.Events;

public class AdminCreateEventDto
{
    public int CafeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Fee { get; set; }
    public DateTime EventDate { get; set; }
}
