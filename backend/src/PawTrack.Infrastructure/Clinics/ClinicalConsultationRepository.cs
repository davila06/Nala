using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicalConsultationRepository(PawTrackDbContext dbContext)
    : IClinicalConsultationRepository
{
    public Task<ClinicalConsultation?> GetByIdAsync(
        Guid consultationId,
        CancellationToken cancellationToken = default) =>
        dbContext.ClinicalConsultations.FirstOrDefaultAsync(
            consultation => consultation.Id == consultationId,
            cancellationToken);

    public Task<ClinicalConsultation?> GetByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.ClinicalConsultations.FirstOrDefaultAsync(
            consultation => consultation.AppointmentId == appointmentId,
            cancellationToken);

    public async Task AddAsync(
        ClinicalConsultation consultation,
        CancellationToken cancellationToken = default) =>
        await dbContext.ClinicalConsultations.AddAsync(consultation, cancellationToken);

    public void Update(ClinicalConsultation consultation) =>
        dbContext.ClinicalConsultations.Update(consultation);
}
