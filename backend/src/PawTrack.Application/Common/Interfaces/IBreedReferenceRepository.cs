using PawTrack.Domain.Medical;

namespace PawTrack.Application.Common.Interfaces;

public interface IBreedReferenceRepository
{
    Task<BreedReference?> GetByBreedKeyAsync(string breedKey, string species, CancellationToken ct = default);
    Task<BreedReference?> GetSpeciesFallbackAsync(string species, CancellationToken ct = default);
    /// <summary>Returns the breed match if found, otherwise the species fallback.</summary>
    Task<BreedReference?> ResolveAsync(string? breedKey, string species, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<BreedReference> entries, CancellationToken ct = default);
}
