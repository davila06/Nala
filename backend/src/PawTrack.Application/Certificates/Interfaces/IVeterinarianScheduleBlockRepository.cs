using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Certificates.Interfaces;

public interface IVeterinarianScheduleBlockRepository
{
    Task<VeterinarianScheduleBlock?> GetByIdAsync(Guid blockId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VeterinarianScheduleBlock>> GetForClinicAsync(Guid clinicId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<bool> HasOverlapAsync(Guid veterinarianId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken = default);
    Task<bool> HasOverlapAsync(Guid veterinarianId, DateTimeOffset startsAt, DateTimeOffset endsAt, Guid excludingBlockId, CancellationToken cancellationToken = default);
    Task AddAsync(VeterinarianScheduleBlock block, CancellationToken cancellationToken = default);
    void Update(VeterinarianScheduleBlock block);
    void Delete(VeterinarianScheduleBlock block);
}
