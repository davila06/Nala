using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Application.Subscriptions.Services;

public static class DowngradeResourcePolicy
{
    public static bool ShouldPauseProviderService(
        ProviderServiceStatus status,
        int activeIndex,
        decimal allowedActiveServices) =>
        status == ProviderServiceStatus.Published && activeIndex >= allowedActiveServices;

    public static bool ShouldDeactivateStoreLocation(
        bool isPrimary,
        bool isActive,
        int activeIndex,
        decimal allowedLocations) =>
        !isPrimary && isActive && activeIndex >= allowedLocations;

    public static bool ShouldDropMunicipalCanton(
        string canton,
        IReadOnlyList<string> allowedCantons) =>
        !allowedCantons.Contains(canton, StringComparer.OrdinalIgnoreCase);
}
