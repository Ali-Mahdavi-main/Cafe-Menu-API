namespace CafeMenu.Api.Dtos.Events;

public class PublicEventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Fee { get; set; }
    public bool IsFree => Fee <= 0;
    public string EventDateShamsi { get; set; } = string.Empty;
}
