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
                await scope.ServiceProvider.GetRequiredService<OverdueFineRepository>().SynchronizeAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to update reservations and overdue fines.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
