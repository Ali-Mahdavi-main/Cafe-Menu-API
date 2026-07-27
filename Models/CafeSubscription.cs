namespace CafeMenu.Api.Models;

public class CafeSubscription
{
    public int Id { get; set; }
    public int CafeId { get; set; }
    public Cafe Cafe { get; set; } = null!;

    public int PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? GracePeriodStart { get; set; }
    public DateTime? GracePeriodEnd { get; set; }
    public DateTime? LastWarningSent { get; set; }
    public int WarningCount { get; set; }
}
