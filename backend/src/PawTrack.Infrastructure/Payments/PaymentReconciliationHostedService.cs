using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Payments.Reconciliation;

namespace PawTrack.Infrastructure.Payments;

public sealed class PaymentReconciliationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentReconciliationHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetService<IPaymentSettlementReportProvider>();
        if (provider is null)
        {
            logger.LogWarning("Payment reconciliation is disabled: no settlement report provider is configured.");
            return;
        }

        var reconciliation = scope.ServiceProvider.GetRequiredService<PaymentReconciliationService>();
        var since = DateTimeOffset.UtcNow.AddDays(-2);
        var snapshots = await provider.GetSettlementsAsync(since, cancellationToken);
        var result = await reconciliation.ReconcileAsync(snapshots, since, Guid.CreateVersion7().ToString("N"), cancellationToken);

        if (result.Issues.Count > 0)
            logger.LogError("Payment reconciliation found {IssueCount} issues. OperationId={OperationId}", result.Issues.Count, result.OperationId);
        else
            logger.LogInformation("Payment reconciliation completed successfully. OperationId={OperationId}", result.OperationId);
    }
}
