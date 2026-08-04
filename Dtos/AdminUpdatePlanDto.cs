namespace CafeMenu.Api.Dtos;

public class AdminUpdatePlanDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int DurationDays { get; set; }
    public decimal? Price { get; set; }
    public int? Discount { get; set; }
    public bool? IsFeatured { get; set; }
}