using PawTrack.Domain.Medical;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicMedicalAccessGrantRepository
{
    Task<ClinicMedicalAccessGrant?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the active grant between a clinic and a pet, or null.</summary>
    Task<ClinicMedicalAccessGrant?> GetActiveGrantAsync(Guid clinicId, Guid petId, CancellationToken ct = default);

    /// <summary>All non-revoked grants for a pet (includes pending and active).</summary>
    Task<IReadOnlyList<ClinicMedicalAccessGrant>> GetByPetIdAsync(Guid petId, CancellationToken ct = default);

    /// <summary>All active grants for a clinic (pets the clinic can access).</summary>
    Task<IReadOnlyList<ClinicMedicalAccessGrant>> GetByClinicIdAsync(Guid clinicId, CancellationToken ct = default);

    /// <summary>True when an active (accepted + not revoked) grant exists.</summary>
    Task<bool> HasActiveGrantAsync(Guid clinicId, Guid petId, CancellationToken ct = default);

    /// <summary>Finds the most recent pending grant matching the code hash (for accept flows).</summary>
    Task<ClinicMedicalAccessGrant?> FindPendingByCodeHashAsync(string codeHash, CancellationToken ct = default);

    Task AddAsync(ClinicMedicalAccessGrant grant, CancellationToken ct = default);
    void Update(ClinicMedicalAccessGrant grant);
}
