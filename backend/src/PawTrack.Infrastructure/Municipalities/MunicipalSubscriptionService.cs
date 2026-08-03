using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Infrastructure.Municipalities;

public sealed class MunicipalSubscriptionService(IMunicipalProfileRepository repo)
    : IMunicipalSubscriptionService
{
    private async Task<MunicipalityProfile?> GetAsync(Guid userId, CancellationToken ct) =>
        await repo.GetByUserIdAsync(userId, ct);

    public async Task<MunicipalTier> GetTierAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await GetAsync(userId, ct);
        if (profile is null || !profile.IsActive || profile.IsExpired) return MunicipalTier.Basica;
        return profile.Tier;
    }

    public async Task<bool> IsFullOrAboveAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await GetAsync(userId, ct);
        return profile?.IsFullOrAbove == true;
    }

    public async Task<bool> IsRedRegionalAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await GetAsync(userId, ct);
        return profile?.IsRedRegional == true;
    }

    public async Task<IReadOnlyList<string>> GetAuthorizedCantonsAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await GetAsync(userId, ct);
        if (profile is null) return [];
        // Básica: own canton only; Full+: all cantons in profile
        if (!profile.IsFullOrAbove) return [profile.Canton];
        return profile.AllCantons;
    }
}
