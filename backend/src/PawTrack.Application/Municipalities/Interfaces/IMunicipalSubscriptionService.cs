using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Interfaces;

public interface IMunicipalSubscriptionService
{
    Task<MunicipalTier> GetTierAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsFullOrAboveAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsRedRegionalAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns the cantons this user is authorized to access. Básica = own canton only.</summary>
    Task<IReadOnlyList<string>> GetAuthorizedCantonsAsync(Guid userId, CancellationToken ct = default);
}
