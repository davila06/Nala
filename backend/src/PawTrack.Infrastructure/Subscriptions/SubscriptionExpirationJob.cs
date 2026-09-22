using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Municipalities;
using PawTrack.Domain.Subscriptions;
using PawTrack.Application.Subscriptions.Services;

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
            var clinicApiKeyRepository = scope.ServiceProvider.GetRequiredService<IClinicApiKeyRepository>();
            var widgetDomainRepository = scope.ServiceProvider.GetRequiredService<PawTrack.Application.Clinics.Interfaces.IClinicWidgetDomainRepository>();
            var municipalRepo = scope.ServiceProvider.GetRequiredService<IMunicipalProfileRepository>();
            var providerRepository = scope.ServiceProvider.GetRequiredService<PawTrack.Application.Common.Interfaces.IServiceProviderRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var expired = await subscriptionRepository.GetExpiredActiveAsync(ct);

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

                    if (sub.Tier == SubscriptionTier.ClinicPartner)
                    {
                        var keys = await clinicApiKeyRepository.GetForClinicAsync(sub.ClinicId.Value, ct);
                        foreach (var key in keys.Where(k => !k.IsRevoked))
                        {
                            key.Revoke();
                            clinicApiKeyRepository.Update(key);
                        }
                        var widgetDomains = await widgetDomainRepository.GetForClinicAsync(sub.ClinicId.Value, ct);
                        foreach (var domain in widgetDomains.Where(domain => domain.IsActive))
                        {
                            domain.Deactivate();
                            widgetDomainRepository.Update(domain);
                        }
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

                if (sub.UserId.HasValue && SubscriptionPricing.IsMunicipalTier(sub.Tier))
                {
                    var profile = await municipalRepo.GetByUserIdAsync(sub.UserId.Value, ct);
                    if (profile is not null)
                    {
                        profile.Upgrade(MunicipalTier.Basica, null);
                        municipalRepo.Update(profile);
                    }
                }
            }

            var scheduledDue = await subscriptionRepository.GetScheduledDueAsync(ct);
            foreach (var sub in scheduledDue)
            {
                sub.Activate(SubscriptionPricing.IsMunicipalTier(sub.Tier) ? 12 : 1);
                subscriptionRepository.Update(sub);
                await ApplyDowngradeResourceModeAsync(sub, storeRepository, ct);
                await ApplyProviderAndMunicipalReadModeAsync(sub, providerRepository, municipalRepo, ct);
            }

            if (expired.Count == 0 && scheduledDue.Count == 0) return;

            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation(
                "[SubscriptionExpiration] Expired {ExpiredCount} subscriptions and activated {ScheduledCount} scheduled changes.",
                expired.Count,
                scheduledDue.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[SubscriptionExpiration] Failed to expire due subscriptions.");
        }
    }

    private static async Task ApplyDowngradeResourceModeAsync(
        Subscription subscription,
        IStoreRepository storeRepository,
        CancellationToken ct)
    {
        if (subscription.UserId is null || subscription.Tier is not (SubscriptionTier.StorePlus or SubscriptionTier.StorePartner))
            return;

        var store = await storeRepository.GetByUserIdAsync(subscription.UserId.Value, ct);
        if (store is null) return;

        var products = await storeRepository.GetProductsByStoreAsync(store.Id, ct);
        var productLimit = subscription.Tier == SubscriptionTier.StorePartner ? 1000 : 100;
        foreach (var product in products.Where(product => product.PlanRestricted))
        {
            product.RestoreFromPlan();
            storeRepository.UpdateProduct(product);
        }
        var activeProductIndex = 0;
        foreach (var product in products.Where(product => product.IsAvailable).OrderByDescending(product => product.CreatedAt))
        {
            if (!DowngradeResourcePolicy.ShouldPauseProviderService(
                    PawTrack.Domain.ServiceProviders.ProviderServiceStatus.Published,
                    activeProductIndex, productLimit))
            {
                activeProductIndex++;
                continue;
            }
            product.RestrictByPlan();
            storeRepository.UpdateProduct(product);
            activeProductIndex++;
        }

        var locations = await storeRepository.GetLocationsByStoreAsync(store.Id, ct);
        var locationLimit = subscription.Tier == SubscriptionTier.StorePartner ? 5 : 1;
        foreach (var location in locations.Where(location => location.PlanRestricted))
        {
            location.RestoreFromPlan();
            storeRepository.UpdateLocation(location);
        }
        var activeLocationIndex = 0;
        foreach (var location in locations.Where(location => location.IsActive).OrderByDescending(location => location.CreatedAt))
        {
            if (!DowngradeResourcePolicy.ShouldDeactivateStoreLocation(
                    location.IsPrimary, location.IsActive, activeLocationIndex, locationLimit))
            {
                activeLocationIndex++;
                continue;
            }
            location.RestrictByPlan();
            storeRepository.UpdateLocation(location);
            activeLocationIndex++;
        }
    }

    private static async Task ApplyProviderAndMunicipalReadModeAsync(
        Subscription subscription,
        PawTrack.Application.Common.Interfaces.IServiceProviderRepository providerRepository,
        IMunicipalProfileRepository municipalRepository,
        CancellationToken ct)
    {
        if (subscription.UserId is null) return;

        if (subscription.Tier is SubscriptionTier.StorePlus or SubscriptionTier.StorePartner)
        {
            var provider = await providerRepository.GetByUserIdAsync(subscription.UserId.Value, ct);
            if (provider is not null)
            {
                var services = await providerRepository.GetServicesByProviderAsync(provider.Id, ct);
                foreach (var service in services.Where(service => service.PlanRestricted))
                {
                    service.RestoreFromPlan();
                    providerRepository.UpdateService(service);
                }
                var serviceIndex = 0;
                foreach (var service in services.Where(service => service.Status == PawTrack.Domain.ServiceProviders.ProviderServiceStatus.Published))
                {
                    if (!DowngradeResourcePolicy.ShouldPauseProviderService(service.Status, serviceIndex, 3))
                    {
                        serviceIndex++;
                        continue;
                    }
                    service.RestrictByPlan();
                    providerRepository.UpdateService(service);
                    serviceIndex++;
                }
            }
        }

        if (SubscriptionPricing.IsMunicipalTier(subscription.Tier))
        {
            var profile = await municipalRepository.GetByUserIdAsync(subscription.UserId.Value, ct);
            if (profile is not null && subscription.Tier == SubscriptionTier.MuniBasica)
            {
                var allowedCantons = profile.AllCantons.Take(1).ToList();
                profile.SetAdditionalCantons([]);
                municipalRepository.Update(profile);
            }
        }
    }
}
