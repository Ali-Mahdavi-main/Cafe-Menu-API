using Microsoft.Extensions.Logging;

namespace CafeMenu.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendSubscriptionExpiredWarningAsync(int cafeId, string? contact, DateTime subscriptionEndDate, DateTime gracePeriodEnd)
    {
        _logger.LogWarning("Cafe {CafeId} subscription expired on {SubscriptionEndDate}. Grace period ends on {GracePeriodEnd}. Contact: {Contact}", cafeId, subscriptionEndDate, gracePeriodEnd, contact ?? "n/a");
        return Task.CompletedTask;
    }

    public Task SendGracePeriodReminderAsync(int cafeId, string? contact, int daysLeft)
    {
        _logger.LogInformation("Cafe {CafeId} has {DaysLeft} day(s) left in grace period. Contact: {Contact}", cafeId, daysLeft, contact ?? "n/a");
        return Task.CompletedTask;
    }

    public Task SendMenuHiddenNotificationAsync(int cafeId, string? contact)
    {
        _logger.LogWarning("Cafe {CafeId} menu has been disabled after subscription grace period exhaustion. Contact: {Contact}", cafeId, contact ?? "n/a");
        return Task.CompletedTask;
    }
}
