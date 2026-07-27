namespace CafeMenu.Api.Models;

public class Payment
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;

    public int SubscriptionId { get; set; }
    public CafeSubscription Subscription { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IRR";
    public string Status { get; set; } = "Pending";

    public string? Authority { get; set; }
    public string? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
