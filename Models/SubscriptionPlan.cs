namespace CafeMenu.Api.Models;

public class SubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public int Discount { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
}
