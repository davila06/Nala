using Microsoft.EntityFrameworkCore;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Domain.CastrationCampaigns;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.CastrationCampaigns;

public sealed class CastrationCampaignRepository(PawTrackDbContext dbContext)
    : ICastrationCampaignRepository
{
    public Task<CastrationCampaign?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.CastrationCampaigns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<CastrationCampaign> Items, int Total)> GetPublishedPagedAsync(
        string? canton,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = dbContext.CastrationCampaigns
            .AsNoTracking()
            .Where(x =>
                (x.Status == CastrationCampaignStatus.Published || x.Status == CastrationCampaignStatus.Active) &&
                x.EndsAt > now &&
                x.ReservedCount < x.Capacity);

        if (!string.IsNullOrWhiteSpace(canton))
        {
            var normalizedCanton = canton.Trim();
            query = query.Where(x => x.Canton == normalizedCanton);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.StartsAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(
        CastrationCampaign campaign,
        CancellationToken cancellationToken = default) =>
        await dbContext.CastrationCampaigns.AddAsync(campaign, cancellationToken);

    public void Update(CastrationCampaign campaign) =>
        dbContext.CastrationCampaigns.Update(campaign);
}
