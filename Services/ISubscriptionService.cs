using CafeMenu.Api.Models;

namespace CafeMenu.Api.Services;

public interface ISubscriptionService
{
    Task<SubscriptionPlan[]> GetPlansAsync();
    Task<CafeSubscription?> GetCurrentSubscriptionAsync(int cafeId);
    Task<Payment> CreatePendingPaymentAsync(int cafeId, int planId);
    Task<bool> ActivateSubscriptionAsync(int cafeId, int subscriptionId, string authority, long refId);
    Task CheckExpirationsAndSendWarningsAsync();
    Task UpdateMenuAvailabilityAsync();
    Task<CafeSubscription> AssignTrialSubscriptionAsync(int cafeId);

}