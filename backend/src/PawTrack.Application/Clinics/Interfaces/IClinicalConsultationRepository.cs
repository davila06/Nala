using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicalConsultationRepository
{
    Task<ClinicalConsultation?> GetByIdAsync(Guid consultationId, CancellationToken cancellationToken = default);
    Task<ClinicalConsultation?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicalConsultation consultation, CancellationToken cancellationToken = default);
    void Update(ClinicalConsultation consultation);
}
