using Microsoft.Extensions.DependencyInjection;

namespace CafeMenu.Api.Services;

public class SubscriptionMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionMonitoringService> _logger;

    public SubscriptionMonitoringService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionMonitoringService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

                await subscriptionService.CheckExpirationsAndSendWarningsAsync();
                await subscriptionService.UpdateMenuAvailabilityAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription monitoring cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
