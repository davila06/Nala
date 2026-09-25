using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicInventoryRepository
{
    Task<ClinicInventoryItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicInventoryItem>> GetItemsByClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task AddItemAsync(ClinicInventoryItem item, CancellationToken cancellationToken = default);
    void UpdateItem(ClinicInventoryItem item);

    Task<ClinicInventoryLot?> GetLotByIdAsync(Guid lotId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicInventoryLot>> GetLotsByItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicInventoryLot>> GetAvailableLotsByItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task AddLotAsync(ClinicInventoryLot lot, CancellationToken cancellationToken = default);
    void UpdateLot(ClinicInventoryLot lot);

    Task AddMovementAsync(ClinicInventoryMovement movement, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicInventoryMovement>> GetMovementsByItemAsync(Guid itemId, int take = 100, CancellationToken cancellationToken = default);
}
