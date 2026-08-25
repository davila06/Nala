using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Subscriptions;

/// <summary>
/// Flips subscriptions past their <c>ExpiresAt</c> from Active to Expired and revokes any
/// derived entitlement (Clinic/Store featured flag) that was granted while the subscription
/// was active. Without this job, expired-but-unrenewed subscriptions would keep their paid
/// tier benefits forever, since nothing else ever calls <see cref="Subscription.MarkExpired"/>.
/// </summary>
public sealed class SubscriptionExpirationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionExpirationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            await ExpireDueSubscriptionsAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireDueSubscriptionsAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var clinicRepository = scope.ServiceProvider.GetRequiredService<IClinicRepository>();
            var storeRepository = scope.ServiceProvider.GetRequiredService<IStoreRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var expired = await subscriptionRepository.GetExpiredActiveAsync(ct);
            if (expired.Count == 0) return;

            foreach (var sub in expired)
            {
                sub.MarkExpired();
                subscriptionRepository.Update(sub);

                if (sub.ClinicId.HasValue && sub.Tier >= SubscriptionTier.ClinicPlus)
                {
                    var clinic = await clinicRepository.GetByIdAsync(sub.ClinicId.Value, ct);
                    if (clinic is not null)
                    {
                        clinic.SetFeatured(false);
                        clinicRepository.Update(clinic);
                    }
                }

                if (sub.UserId.HasValue && sub.Tier is SubscriptionTier.StorePlus or SubscriptionTier.StorePartner)
                {
                    var store = await storeRepository.GetByUserIdAsync(sub.UserId.Value, ct);
                    if (store is not null)
                    {
                        store.SetFeatured(false);
                        storeRepository.Update(store);
                    }
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("[SubscriptionExpiration] Expired {Count} subscriptions.", expired.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[SubscriptionExpiration] Failed to expire due subscriptions.");
        }
    }
}
