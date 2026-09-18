using Microsoft.EntityFrameworkCore;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Domain.CastrationCampaigns;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.CastrationCampaigns;

public sealed class CastrationAppointmentRepository(PawTrackDbContext dbContext)
    : ICastrationAppointmentRepository
{
    public Task<CastrationAppointment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.CastrationAppointments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsForPetAsync(
        Guid campaignId,
        Guid petId,
        CancellationToken cancellationToken = default) =>
        dbContext.CastrationAppointments.AnyAsync(
            x => x.CampaignId == campaignId && x.PetId == petId,
            cancellationToken);

    public async Task<(IReadOnlyList<CastrationAppointment> Items, int Total)> GetByCampaignPagedAsync(
        Guid campaignId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CastrationAppointments.AsNoTracking()
            .Where(x => x.CampaignId == campaignId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.ScheduledAt).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<CastrationAppointment> Items, int Total)> GetByOwnerPagedAsync(
        Guid ownerUserId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CastrationAppointments.AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.ScheduledAt).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(
        CastrationAppointment appointment,
        CancellationToken cancellationToken = default) =>
        await dbContext.CastrationAppointments.AddAsync(appointment, cancellationToken);

    public void Update(CastrationAppointment appointment) =>
        dbContext.CastrationAppointments.Update(appointment);
}
