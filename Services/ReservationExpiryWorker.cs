using Lib_System.Services.Interfaces;

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
                await scope.ServiceProvider.GetRequiredService<IPaymentWorkflowService>().ExpireAsync();
                await scope.ServiceProvider.GetRequiredService<IOverdueFineService>().SynchronizeAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to update reservations and overdue fines.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
