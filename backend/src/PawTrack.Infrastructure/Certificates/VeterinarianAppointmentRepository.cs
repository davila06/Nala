using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Certificates;

public sealed class VeterinarianAppointmentRepository(PawTrackDbContext db) : IVeterinarianAppointmentRepository
{
    public Task<bool> HasOverlapAsync(Guid veterinarianId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken ct = default) =>
        db.VeterinarianAppointments.AsNoTracking().AnyAsync(x =>
            x.VeterinarianId == veterinarianId
            && x.Status == VeterinarianAppointmentStatus.Scheduled
            && x.StartsAt < endsAt && startsAt < x.EndsAt, ct);

    public async Task AddAsync(VeterinarianAppointment appointment, CancellationToken ct = default) =>
        await db.VeterinarianAppointments.AddAsync(appointment, ct);

    public async Task<IReadOnlyList<VeterinarianAppointment>> GetForClinicAsync(Guid clinicId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) =>
        await db.VeterinarianAppointments.AsNoTracking()
            .Where(x => x.ClinicId == clinicId && x.StartsAt < to && from < x.EndsAt)
            .OrderBy(x => x.StartsAt).Take(500).ToListAsync(ct);
}