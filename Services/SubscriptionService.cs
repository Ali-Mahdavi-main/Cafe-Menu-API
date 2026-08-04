using CafeMenu.Api.Data;
using CafeMenu.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Services;

public class SubscriptionService : ISubscriptionService
{
    private const string TrialPlanName = "دوره آزمایشی رایگان";
    private const int TrialDurationDays = 14;

    private readonly AppDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly INotificationService _notificationService;

    public SubscriptionService(
        AppDbContext context,
        ILogger<SubscriptionService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<SubscriptionPlan[]> GetPlansAsync()
    {
        return await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToArrayAsync();
    }

    public async Task<CafeSubscription?> GetCurrentSubscriptionAsync(int cafeId)
    {
        return await _context.CafeSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.CafeId == cafeId && s.IsActive)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<Payment> CreatePendingPaymentAsync(int cafeId, int planId)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(planId)
            ?? throw new ArgumentException("Invalid plan");

        var subscription = new CafeSubscription
        {
            CafeId = cafeId,
            PlanId = planId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.DurationDays),
            IsActive = false,
            WarningCount = 0
        };

        _context.CafeSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            CafeId = cafeId,
            SubscriptionId = subscription.Id,
            Amount = plan.Price,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return payment;
    }

    /// <summary>
    /// Creates (or reuses) a dedicated trial plan and assigns it as the cafe's
    /// active subscription. Used both for admin-created cafes and public self-registration,
    /// so trial terms can never accidentally drift from whatever plan happens to be
    /// first in the table.
    /// </summary>
    public async Task<CafeSubscription> AssignTrialSubscriptionAsync(int cafeId)
    {
        var trialPlan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.IsActive && p.Name == TrialPlanName);

        if (trialPlan is null)
        {
            trialPlan = new SubscriptionPlan
            {
                Name = TrialPlanName,
                DurationDays = TrialDurationDays,
                Price = 0,
                IsActive = true
            };
            _context.SubscriptionPlans.Add(trialPlan);
            await _context.SaveChangesAsync();
        }

        var subscription = new CafeSubscription
        {
            CafeId = cafeId,
            PlanId = trialPlan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(trialPlan.DurationDays),
            IsActive = true,
            IsFree = true,
            WarningCount = 0
        };

        _context.CafeSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return subscription;
    }

    public async Task<bool> ActivateSubscriptionAsync(int cafeId, int subscriptionId, string authority, long refId)
    {
        var subscription = await _context.CafeSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.CafeId == cafeId);

        if (subscription is null)
            return false;

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.SubscriptionId == subscriptionId && p.Authority == authority && p.Status != "Success");

        if (payment is null)
            return false;

        // Deactivate any existing active subscriptions for this cafe — this is the
        // ONLY place an old subscription should be turned off, and only once payment
        // has actually been confirmed.
        var existingSubs = await _context.CafeSubscriptions
            .Where(s => s.CafeId == cafeId && s.IsActive && s.Id != subscriptionId)
            .ToArrayAsync();
        foreach (var sub in existingSubs)
        {
            sub.IsActive = false;
        }

        payment.Status = "Success";
        payment.ReferenceId = refId.ToString();
        payment.CompletedAt = DateTime.UtcNow;

        subscription.IsActive = true;
        subscription.StartDate = DateTime.UtcNow;
        subscription.EndDate = DateTime.UtcNow.AddDays(subscription.Plan.DurationDays);
        subscription.GracePeriodStart = null;
        subscription.GracePeriodEnd = null;
        subscription.WarningCount = 0;
        subscription.LastWarningSent = null;

        await SetMenuAvailabilityAsync(cafeId, true);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription {SubscriptionId} activated for cafe {CafeId}", subscriptionId, cafeId);
        return true;
    }

    public async Task CheckExpirationsAndSendWarningsAsync()
    {
        var now = DateTime.UtcNow;

        var activeExpiringSoon = await _context.CafeSubscriptions
            .Include(s => s.Cafe)
            .Include(s => s.Plan)
            .Where(s => s.IsActive && s.EndDate > now && s.EndDate <= now.AddDays(5))
            .ToArrayAsync();

        foreach (var subscription in activeExpiringSoon)
        {
            var daysUntilExpiry = (subscription.EndDate - now).Days;
            if (daysUntilExpiry <= 0) continue;

            if (!subscription.LastWarningSent.HasValue || (now - subscription.LastWarningSent.Value).TotalHours >= 24)
            {
                await _notificationService.SendExpiryReminderAsync(
                    subscription.CafeId,
                    subscription.Cafe?.Name,
                    daysUntilExpiry,
                    subscription.EndDate);

                subscription.LastWarningSent = now;
                subscription.WarningCount++;
            }
        }

        var expiredSubscriptions = await _context.CafeSubscriptions
            .Include(s => s.Cafe)
            .Include(s => s.Plan)
            .Where(s => s.IsActive && s.EndDate < now)
            .ToArrayAsync();

        foreach (var subscription in expiredSubscriptions)
        {
            if (!subscription.GracePeriodStart.HasValue)
            {
                subscription.GracePeriodStart = now;
                subscription.GracePeriodEnd = now.AddDays(5);
                subscription.IsActive = false;

                await _notificationService.SendSubscriptionExpiredWarningAsync(
                    subscription.CafeId,
                    subscription.Cafe?.Name,
                    subscription.EndDate,
                    subscription.GracePeriodEnd.Value);

                subscription.LastWarningSent = now;
                subscription.WarningCount = 1;

                _logger.LogInformation("Cafe {CafeId} subscription expired. Grace period until {GracePeriodEnd}", subscription.CafeId, subscription.GracePeriodEnd);
            }
            else if (subscription.GracePeriodEnd.HasValue && now > subscription.GracePeriodEnd.Value)
            {
                await _notificationService.SendMenuHiddenNotificationAsync(
                    subscription.CafeId,
                    subscription.Cafe?.Name);

                await SetMenuAvailabilityAsync(subscription.CafeId, false);
                _logger.LogWarning("Cafe {CafeId} menu has been hidden due to expired subscription", subscription.CafeId);
            }
            else if (subscription.GracePeriodEnd.HasValue)
            {
                var daysLeft = (subscription.GracePeriodEnd.Value - now).Days;
                if (daysLeft <= 2 && (!subscription.LastWarningSent.HasValue || (now - subscription.LastWarningSent.Value).TotalHours >= 24))
                {
                    await _notificationService.SendGracePeriodReminderAsync(
                        subscription.CafeId,
                        subscription.Cafe?.Name,
                        daysLeft);

                    subscription.LastWarningSent = now;
                    subscription.WarningCount++;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateMenuAvailabilityAsync()
    {
        var now = DateTime.UtcNow;

        var cafesToHide = await _context.CafeSubscriptions
            .Where(s => s.GracePeriodEnd.HasValue && s.GracePeriodEnd.Value < now)
            .Select(s => s.CafeId)
            .Distinct()
            .ToArrayAsync();

        foreach (var cafeId in cafesToHide)
        {
            await SetMenuAvailabilityAsync(cafeId, false);
        }

        var cafesToShow = await _context.CafeSubscriptions
            .Where(s => s.IsActive && s.EndDate > now)
            .Select(s => s.CafeId)
            .Distinct()
            .ToArrayAsync();

        foreach (var cafeId in cafesToShow)
        {
            await SetMenuAvailabilityAsync(cafeId, true);
        }
    }

    private async Task SetMenuAvailabilityAsync(int cafeId, bool available)
    {
        var menuItems = await _context.MenuItems
            .Where(m => m.CafeId == cafeId)
            .ToArrayAsync();

        foreach (var item in menuItems)
        {
            item.IsAvailable = available;
        }

        await _context.SaveChangesAsync();
    }
}