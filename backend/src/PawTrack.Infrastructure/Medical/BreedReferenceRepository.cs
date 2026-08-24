using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Medical;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Medical;

public sealed class BreedReferenceRepository(PawTrackDbContext db) : IBreedReferenceRepository
{
    public Task<BreedReference?> GetByBreedKeyAsync(string breedKey, string species, CancellationToken ct = default) =>
        db.BreedReferences.AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.BreedKey == breedKey.Trim().ToLowerInvariant() &&
                     b.Species == species &&
                     !b.IsSpeciesFallback, ct);

    public Task<BreedReference?> GetSpeciesFallbackAsync(string species, CancellationToken ct = default) =>
        db.BreedReferences.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Species == species && b.IsSpeciesFallback, ct);

    public async Task<BreedReference?> ResolveAsync(string? breedKey, string species, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(breedKey))
        {
            var match = await GetByBreedKeyAsync(breedKey, species, ct);
            if (match is not null) return match;
        }
        return await GetSpeciesFallbackAsync(species, ct);
    }

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        db.BreedReferences.AnyAsync(ct);

    public async Task AddRangeAsync(IEnumerable<BreedReference> entries, CancellationToken ct = default) =>
        await db.BreedReferences.AddRangeAsync(entries, ct);
}
