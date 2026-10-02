using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Application.Stores;

public sealed class StoreOrderReservationExpirationJob(
    IStoreOrderRepository repository,
    IDistributedJobLock jobLock)
{
    public async Task<int> RunOnceAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var lease = await jobLock.TryAcquireAsync(
            "StoreOrderReservationExpiration", TimeSpan.FromMinutes(5), cancellationToken);
        if (lease is null) return 0;

        var expiredOrderIds = await repository.GetExpiredStockReservationOrderIdsAsync(now, 100, cancellationToken);
        var expiredCount = 0;
        foreach (var orderId in expiredOrderIds)
        {
            if (await repository.ExpireStockReservationAsync(orderId, now, cancellationToken))
                expiredCount++;
        }

        return expiredCount;
    }
}