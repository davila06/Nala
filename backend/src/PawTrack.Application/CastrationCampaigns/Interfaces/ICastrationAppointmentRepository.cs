using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.Application.CastrationCampaigns.Interfaces;

public interface ICastrationAppointmentRepository
{
    Task<CastrationAppointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForPetAsync(Guid campaignId, Guid petId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CastrationAppointment> Items, int Total)> GetByCampaignPagedAsync(
        Guid campaignId, int skip, int take, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CastrationAppointment> Items, int Total)> GetByOwnerPagedAsync(
        Guid ownerUserId, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(CastrationAppointment appointment, CancellationToken cancellationToken = default);
    void Update(CastrationAppointment appointment);
}
