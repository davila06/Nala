using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicRepository(PawTrackDbContext dbContext) : IClinicRepository
{
    public async Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Clinic?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task<Clinic?> GetByLicenseNumberAsync(
        string licenseNumber, CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LicenseNumber == licenseNumber.ToUpperInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Clinic>> GetAllPendingAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Clinics
            .AsNoTracking()
            .Where(c => c.Status == ClinicStatus.Pending)
            .OrderBy(c => c.RegisteredAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default) =>
        await dbContext.Clinics.AddAsync(clinic, cancellationToken);

    public void Update(Clinic clinic) =>
        dbContext.Clinics.Update(clinic);
}
