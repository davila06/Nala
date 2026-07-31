using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Municipalities;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Municipalities;

public sealed class CapturedAnimalRepository(PawTrackDbContext dbContext) : ICapturedAnimalRepository
{
    public Task<CapturedAnimal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.CapturedAnimals.FindAsync([id], cancellationToken).AsTask();

    public async Task<(IReadOnlyList<CapturedAnimal> Items, int Total)> SearchAsync(
        string? canton,
        CapturedAnimalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = dbContext.CapturedAnimals.AsQueryable();

        if (!string.IsNullOrWhiteSpace(canton))
            q = q.Where(a => a.Canton.Contains(canton));

        if (status.HasValue)
            q = q.Where(a => a.Status == status.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(a => a.CapturedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(CapturedAnimal animal, CancellationToken cancellationToken = default) =>
        await dbContext.CapturedAnimals.AddAsync(animal, cancellationToken);

    public void Update(CapturedAnimal animal) =>
        dbContext.CapturedAnimals.Update(animal);
}
