using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.Application.CastrationCampaigns.Interfaces;

public interface ICastrationCampaignRepository
{
    Task<CastrationCampaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CastrationCampaign> Items, int Total)> GetPublishedPagedAsync(
        string? canton,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task AddAsync(CastrationCampaign campaign, CancellationToken cancellationToken = default);
    void Update(CastrationCampaign campaign);
}
