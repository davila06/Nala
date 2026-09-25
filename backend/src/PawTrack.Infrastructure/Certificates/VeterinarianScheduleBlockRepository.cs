using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Certificates;

public sealed class VeterinarianScheduleBlockRepository(PawTrackDbContext dbContext)
    : IVeterinarianScheduleBlockRepository
{
    public Task<VeterinarianScheduleBlock?> GetByIdAsync(Guid blockId, CancellationToken cancellationToken = default) =>
        dbContext.VeterinarianScheduleBlocks.FirstOrDefaultAsync(block => block.Id == blockId, cancellationToken);

    public async Task<IReadOnlyList<VeterinarianScheduleBlock>> GetForClinicAsync(
        Guid clinicId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await dbContext.VeterinarianScheduleBlocks.AsNoTracking()
            .Where(block => block.ClinicId == clinicId && block.StartsAt < to && from < block.EndsAt)
            .OrderBy(block => block.StartsAt)
            .Take(500)
            .ToListAsync(cancellationToken);

    public Task<bool> HasOverlapAsync(
        Guid veterinarianId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CancellationToken cancellationToken = default) =>
        HasOverlapAsync(veterinarianId, startsAt, endsAt, Guid.Empty, cancellationToken);

    public Task<bool> HasOverlapAsync(
        Guid veterinarianId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid excludingBlockId,
        CancellationToken cancellationToken = default) =>
        dbContext.VeterinarianScheduleBlocks.AsNoTracking().AnyAsync(
            block => block.VeterinarianId == veterinarianId
                && (excludingBlockId == Guid.Empty || block.Id != excludingBlockId)
                && block.StartsAt < endsAt
                && startsAt < block.EndsAt,
            cancellationToken);

    public async Task AddAsync(
        VeterinarianScheduleBlock block,
        CancellationToken cancellationToken = default) =>
        await dbContext.VeterinarianScheduleBlocks.AddAsync(block, cancellationToken);

    public void Update(VeterinarianScheduleBlock block) => dbContext.VeterinarianScheduleBlocks.Update(block);

    public void Delete(VeterinarianScheduleBlock block) => dbContext.VeterinarianScheduleBlocks.Remove(block);
}
