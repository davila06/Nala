using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PawTrack.Infrastructure.Certificates;

public sealed class ClinicVeterinarianRepository(PawTrackDbContext dbContext) : IClinicVeterinarianRepository
{
    public Task<ClinicVeterinarian?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.ClinicVeterinarians.FindAsync([id], cancellationToken).AsTask();

    public async Task<IReadOnlyList<ClinicVeterinarian>> GetActiveForClinicAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ClinicVeterinarians
            .AsNoTracking()
            .Where(veterinarian => veterinarian.ClinicId == clinicId
                && veterinarian.Status == ClinicVeterinarianStatus.Authorized
                && veterinarian.DocumentUrl != null
                && veterinarian.RevokedAt == null
                && (!veterinarian.ExpiresAt.HasValue || veterinarian.ExpiresAt.Value >= DateOnly.FromDateTime(DateTime.UtcNow)))
            .OrderBy(veterinarian => veterinarian.FullName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicVeterinarian>> GetByClinicAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ClinicVeterinarians
            .AsNoTracking()
            .Where(veterinarian => veterinarian.ClinicId == clinicId)
            .OrderBy(veterinarian => veterinarian.FullName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicVeterinarian>> GetPendingPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.ClinicVeterinarians
            .AsNoTracking()
            .Where(veterinarian => veterinarian.Status == ClinicVeterinarianStatus.PendingReview)
            .OrderBy(veterinarian => veterinarian.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicVeterinarian>> GetExpiringWithinAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var until = today.AddDays(days);
        return await dbContext.ClinicVeterinarians
            .AsNoTracking()
            .Where(veterinarian => veterinarian.Status == ClinicVeterinarianStatus.Authorized
                && veterinarian.ExpiresAt.HasValue
                && veterinarian.ExpiresAt.Value <= until)
            .OrderBy(veterinarian => veterinarian.ExpiresAt)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> LicenseExistsForClinicAsync(
        Guid clinicId,
        string licenseNumber,
        CancellationToken cancellationToken = default) =>
        dbContext.ClinicVeterinarians
            .AsNoTracking()
            .AnyAsync(veterinarian => veterinarian.ClinicId == clinicId
                && veterinarian.LicenseNumber == licenseNumber.Trim().ToUpperInvariant(), cancellationToken);

    public async Task AddAsync(ClinicVeterinarian veterinarian, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicVeterinarians.AddAsync(veterinarian, cancellationToken);

    public void Update(ClinicVeterinarian veterinarian) =>
        dbContext.ClinicVeterinarians.Update(veterinarian);
}
