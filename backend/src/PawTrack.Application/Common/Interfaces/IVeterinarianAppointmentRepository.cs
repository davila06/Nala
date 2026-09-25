using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Common.Interfaces;

public interface IVeterinarianAppointmentRepository
{
    Task<VeterinarianAppointment?> GetByIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task<bool> HasOverlapAsync(Guid veterinarianId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken ct = default);
    Task<bool> HasOverlapAsync(Guid veterinarianId, DateTimeOffset startsAt, DateTimeOffset endsAt, Guid excludingAppointmentId, CancellationToken ct = default);
    Task AddAsync(VeterinarianAppointment appointment, CancellationToken ct = default);
    Task<IReadOnlyList<VeterinarianAppointment>> GetForClinicAsync(Guid clinicId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    void Update(VeterinarianAppointment appointment);
}
