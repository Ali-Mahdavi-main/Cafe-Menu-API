namespace CafeMenu.Api.Dtos;

public class AdminCreatePlanDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DurationDays { get; set; }
    public decimal Price { get; set; }
    public int? Discount { get; set; }
    public bool IsFeatured { get; set; }
}