using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicInventoryRepository(PawTrackDbContext dbContext) : IClinicInventoryRepository
{
    public Task<ClinicInventoryItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        dbContext.ClinicInventoryItems.FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

    public async Task<IReadOnlyList<ClinicInventoryItem>> GetItemsByClinicAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicInventoryItems.AsNoTracking()
            .Where(item => item.ClinicId == clinicId)
            .OrderBy(item => item.Type)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

    public async Task AddItemAsync(ClinicInventoryItem item, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicInventoryItems.AddAsync(item, cancellationToken);

    public void UpdateItem(ClinicInventoryItem item) => dbContext.ClinicInventoryItems.Update(item);

    public Task<ClinicInventoryLot?> GetLotByIdAsync(Guid lotId, CancellationToken cancellationToken = default) =>
        dbContext.ClinicInventoryLots.FirstOrDefaultAsync(lot => lot.Id == lotId, cancellationToken);

    public async Task<IReadOnlyList<ClinicInventoryLot>> GetLotsByItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicInventoryLots.AsNoTracking()
            .Where(lot => lot.ItemId == itemId)
            .OrderBy(lot => lot.ExpiresAt ?? DateOnly.MaxValue)
            .ThenBy(lot => lot.ReceivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicInventoryLot>> GetAvailableLotsByItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicInventoryLots
            .Where(lot => lot.ItemId == itemId && lot.AvailableQuantity > 0)
            .OrderBy(lot => lot.ExpiresAt ?? DateOnly.MaxValue)
            .ThenBy(lot => lot.ReceivedAt)
            .ToListAsync(cancellationToken);

    public async Task AddLotAsync(ClinicInventoryLot lot, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicInventoryLots.AddAsync(lot, cancellationToken);

    public void UpdateLot(ClinicInventoryLot lot) => dbContext.ClinicInventoryLots.Update(lot);

    public async Task AddMovementAsync(ClinicInventoryMovement movement, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicInventoryMovements.AddAsync(movement, cancellationToken);

    public async Task<IReadOnlyList<ClinicInventoryMovement>> GetMovementsByItemAsync(Guid itemId, int take = 100, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicInventoryMovements.AsNoTracking()
            .Where(movement => movement.ItemId == itemId)
            .OrderByDescending(movement => movement.CreatedAt)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(cancellationToken);
}
