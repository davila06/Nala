using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Certificates;

public sealed class ClinicVerificationRepository(PawTrackDbContext dbContext) : IClinicVerificationRepository
{
    public Task<ClinicVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.ClinicVerifications.FindAsync([id], cancellationToken).AsTask();

    public Task<ClinicVerification?> GetActiveForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return dbContext.ClinicVerifications
            .AsNoTracking()
            .Where(verification => verification.ClinicId == clinicId
                && verification.Status == ClinicVerificationStatus.Verified
                && verification.SupersededAt == null
                && verification.DocumentUrl != null
                && (!verification.ExpiresAt.HasValue || verification.ExpiresAt.Value >= today))
            .OrderByDescending(verification => verification.VerifiedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ClinicVerification?> GetLatestForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        dbContext.ClinicVerifications
            .AsNoTracking()
            .Where(verification => verification.ClinicId == clinicId)
            .OrderByDescending(verification => verification.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicVerification>> GetPendingPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.ClinicVerifications
            .AsNoTracking()
            .Where(verification => verification.Status == ClinicVerificationStatus.Pending)
            .OrderBy(verification => verification.SubmittedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicVerification>> GetExpiringWithinAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var until = today.AddDays(days);
        return await dbContext.ClinicVerifications
            .AsNoTracking()
            .Where(verification => verification.Status == ClinicVerificationStatus.Verified
                && verification.SupersededAt == null
                && verification.ExpiresAt.HasValue
                && verification.ExpiresAt.Value <= until)
            .OrderBy(verification => verification.ExpiresAt)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveVerificationAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        GetActiveForClinicAsync(clinicId, cancellationToken)
            .ContinueWith(task => task.Result is not null, cancellationToken);

    public async Task AddAsync(ClinicVerification verification, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicVerifications.AddAsync(verification, cancellationToken);

    public void Update(ClinicVerification verification) =>
        dbContext.ClinicVerifications.Update(verification);
}
