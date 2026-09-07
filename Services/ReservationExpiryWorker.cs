using Lib_System.Repositories;

namespace Lib_System.Services;

public class ReservationExpiryWorker(IServiceScopeFactory scopes, ILogger<ReservationExpiryWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<PaymentWorkflowRepository>().ExpireAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to expire pending reservations.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
