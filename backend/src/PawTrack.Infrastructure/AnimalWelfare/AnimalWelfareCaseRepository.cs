using Microsoft.EntityFrameworkCore;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.AnimalWelfare;

public sealed class AnimalWelfareCaseRepository(PawTrackDbContext dbContext) : IAnimalWelfareCaseRepository
{
    public Task<AnimalWelfareCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.AnimalWelfareCases.FindAsync([id], cancellationToken).AsTask();

    public Task<AnimalWelfareCase?> GetByPublicCodeAsync(string publicCode, CancellationToken cancellationToken = default) =>
        dbContext.AnimalWelfareCases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicCode == publicCode, cancellationToken);

    public async Task<IReadOnlyList<AnimalWelfareCase>> GetQueueAsync(
        WelfareCaseStatus? status,
        WelfareSeverity? severity,
        string? canton,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AnimalWelfareCases.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        if (severity.HasValue) query = query.Where(c => c.Severity == severity.Value);
        if (!string.IsNullOrWhiteSpace(canton)) query = query.Where(c => c.Canton == canton);

        return await query.OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnimalWelfareCase>> GetAssignedAsync(
        Guid organizationUserId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareCases.AsNoTracking()
            .Where(c => c.AssignedOrganizationUserId == organizationUserId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AnimalWelfareCase welfareCase, CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareCases.AddAsync(welfareCase, cancellationToken);

    public void Update(AnimalWelfareCase welfareCase) => dbContext.AnimalWelfareCases.Update(welfareCase);
}
