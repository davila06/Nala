using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.UpdateVeterinarianAppointmentStatus;

public sealed record UpdateVeterinarianAppointmentStatusCommand(
    Guid ClinicId,
    Guid RequestingUserId,
    Guid AppointmentId,
    VeterinarianAppointmentStatus Status)
    : IRequest<Result<Guid>>;

public sealed class UpdateVeterinarianAppointmentStatusCommandHandler(
    IClinicRepository clinicRepository,
    IVeterinarianAppointmentRepository appointmentRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVeterinarianAppointmentStatusCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        UpdateVeterinarianAppointmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<Guid>("Acceso denegado.");

        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null || appointment.ClinicId != request.ClinicId)
            return Result.Failure<Guid>("Cita no encontrada.");

        try
        {
            ApplyStatus(appointment, request.Status);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        appointmentRepository.Update(appointment);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.RequestingUserId,
                AuditAction.ClinicAppointmentStatusChanged,
                "VeterinarianAppointment",
                appointment.Id.ToString(),
                request.Status.ToString()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(appointment.Id);
    }

    private static void ApplyStatus(
        VeterinarianAppointment appointment,
        VeterinarianAppointmentStatus status)
    {
        switch (status)
        {
            case VeterinarianAppointmentStatus.Confirmed:
                appointment.Confirm();
                break;
            case VeterinarianAppointmentStatus.CheckedIn:
                appointment.CheckIn();
                break;
            case VeterinarianAppointmentStatus.InConsultation:
                appointment.StartConsultation();
                break;
            case VeterinarianAppointmentStatus.Completed:
                appointment.Complete();
                break;
            case VeterinarianAppointmentStatus.NoShow:
                appointment.MarkNoShow();
                break;
            case VeterinarianAppointmentStatus.Cancelled:
                appointment.Cancel();
                break;
            case VeterinarianAppointmentStatus.Scheduled:
            default:
                throw new InvalidOperationException("El estado solicitado no es una transición operativa válida.");
        }
    }
}
