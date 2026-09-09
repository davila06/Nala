using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicProfileChangeRepository
{
    Task AddAsync(ClinicProfileChange change, CancellationToken ct = default);
    Task<ClinicProfileChange?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ClinicProfileChange>> GetPendingAsync(int take = 100, CancellationToken ct = default);
    void Update(ClinicProfileChange change);
}