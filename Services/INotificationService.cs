namespace CafeMenu.Api.Services;

public interface INotificationService
{
    Task SendSubscriptionExpiredWarningAsync(int cafeId, string? contact, DateTime subscriptionEndDate, DateTime gracePeriodEnd);
    Task SendGracePeriodReminderAsync(int cafeId, string? contact, int daysLeft);
    Task SendMenuHiddenNotificationAsync(int cafeId, string? contact);
}
