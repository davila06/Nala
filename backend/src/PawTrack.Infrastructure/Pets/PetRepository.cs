using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Pets;

public sealed class PetRepository(PawTrackDbContext dbContext) : IPetRepository
{
    public async Task<Pet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Pets
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Pet>> GetByOwnerIdAsync(
        Guid ownerId, CancellationToken cancellationToken = default) =>
        await dbContext.Pets
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Pet?> GetByMicrochipIdAsync(
        string microchipId, CancellationToken cancellationToken = default) =>
        await dbContext.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MicrochipId == microchipId, cancellationToken);

    public async Task AddAsync(Pet pet, CancellationToken cancellationToken = default) =>
        await dbContext.Pets.AddAsync(pet, cancellationToken);

    public void Update(Pet pet) =>
        dbContext.Pets.Update(pet);

    public void Delete(Pet pet) =>
        dbContext.Pets.Remove(pet);
}
