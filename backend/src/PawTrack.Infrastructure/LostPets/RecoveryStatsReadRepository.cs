using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.LostPets;

public sealed class RecoveryStatsReadRepository(PawTrackDbContext dbContext) : IRecoveryStatsReadRepository
{
    public async Task<RecoveryStatsRawData> GetRecoveryStatsRawAsync(
        string? species,
        string? breed,
        string? canton,
        CancellationToken cancellationToken = default)
    {
        var normalizedSpecies = Normalize(species);
        var normalizedBreed = Normalize(breed);
        var normalizedCanton = Normalize(canton);

        var baseQuery = dbContext.LostPetEvents
            .AsNoTracking()
            .Join(
                dbContext.Pets.AsNoTracking(),
                lost => lost.PetId,
                pet => pet.Id,
                (lost, pet) => new
                {
                    lost.Status,
                    lost.CantonName,
                    lost.RecoveryDistanceMeters,
                    lost.RecoveryTime,
                    Species = pet.Species,
                    pet.Breed,
                });

        if (!string.IsNullOrWhiteSpace(normalizedSpecies) &&
            Enum.TryParse<PetSpecies>(normalizedSpecies, true, out var parsedSpecies))
        {
            baseQuery = baseQuery.Where(x => x.Species == parsedSpecies);
        }

        if (!string.IsNullOrWhiteSpace(normalizedBreed))
        {
            baseQuery = baseQuery.Where(x => x.Breed != null && x.Breed == normalizedBreed);
        }

        if (!string.IsNullOrWhiteSpace(normalizedCanton))
        {
            baseQuery = baseQuery.Where(x => x.CantonName != null && x.CantonName == normalizedCanton);
        }

        var totalReports = await baseQuery.CountAsync(cancellationToken);

        var recoveredRows = await baseQuery
            .Where(x => x.Status == Domain.LostPets.LostPetStatus.Reunited)
            .Where(x => x.RecoveryDistanceMeters != null && x.RecoveryTime != null)
            .Select(x => new
            {
                Distance = x.RecoveryDistanceMeters!.Value,
                Hours = x.RecoveryTime!.Value.TotalHours,
            })
            .ToListAsync(cancellationToken);

        return new RecoveryStatsRawData(
            TotalReports: totalReports,
            RecoveredDistancesMeters: recoveredRows.Select(x => x.Distance).ToArray(),
            RecoveryDurationsHours: recoveredRows.Select(x => x.Hours).ToArray());
    }

    public async Task<RecoveryStatsOverviewRawData> GetRecoveryOverviewRawAsync(
        CancellationToken cancellationToken = default)
    {
        var joined = dbContext.LostPetEvents
            .AsNoTracking()
            .Join(
                dbContext.Pets.AsNoTracking(),
                lost => lost.PetId,
                pet => pet.Id,
                (lost, pet) => new
                {
                    lost.Status,
                    lost.CantonName,
                    lost.RecoveryTime,
                    Species = pet.Species,
                });

        var cantonRows = await joined
            .GroupBy(x => x.CantonName ?? "Sin cantón")
            .Select(group => new RecoveryCantonRawItem(
                group.Key,
                group.Count(),
                group.Count(x => x.Status == Domain.LostPets.LostPetStatus.Reunited)))
            .ToListAsync(cancellationToken);

        var speciesRawRows = await joined
            .Where(x => x.Status == Domain.LostPets.LostPetStatus.Reunited && x.RecoveryTime != null)
            .Select(x => new
            {
                Species = x.Species.ToString(),
                Hours = x.RecoveryTime!.Value.TotalHours,
            })
            .ToListAsync(cancellationToken);

        var speciesRows = speciesRawRows
            .GroupBy(x => x.Species)
            .Select(group => new RecoverySpeciesRawItem(
                group.Key,
                group.Select(x => x.Hours).ToArray(),
                group.Count()))
            .ToList();

        return new RecoveryStatsOverviewRawData(cantonRows, speciesRows);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}
