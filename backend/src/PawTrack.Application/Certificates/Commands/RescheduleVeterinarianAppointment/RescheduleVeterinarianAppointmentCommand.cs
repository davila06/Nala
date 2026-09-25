using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.RescheduleVeterinarianAppointment;

public sealed record RescheduleVeterinarianAppointmentCommand(
    Guid ClinicId,
    Guid RequestingUserId,
    Guid AppointmentId,
    DateTimeOffset StartsAt,
    int DurationMinutes)
    : IRequest<Result<Guid>>;

public sealed class RescheduleVeterinarianAppointmentCommandValidator
    : AbstractValidator<RescheduleVeterinarianAppointmentCommand>
{
    public RescheduleVeterinarianAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 480);
        RuleFor(x => x.StartsAt).GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5));
    }
}

public sealed class RescheduleVeterinarianAppointmentCommandHandler(
    IClinicRepository clinicRepository,
    IVeterinarianAppointmentRepository appointmentRepository,
    IVeterinarianScheduleBlockRepository blockRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RescheduleVeterinarianAppointmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        RescheduleVeterinarianAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<Guid>("Acceso denegado.");

        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null || appointment.ClinicId != request.ClinicId)
            return Result.Failure<Guid>("Cita no encontrada.");

        var endsAt = request.StartsAt.AddMinutes(request.DurationMinutes);
        if (await blockRepository.HasOverlapAsync(
            appointment.VeterinarianId,
            request.StartsAt,
            endsAt,
            cancellationToken))
            return Result.Failure<Guid>("El veterinario tiene la agenda bloqueada en ese horario.");

        if (await appointmentRepository.HasOverlapAsync(
                appointment.VeterinarianId,
                request.StartsAt,
                endsAt,
                appointment.Id,
                cancellationToken))
            return Result.Failure<Guid>("El veterinario ya tiene una cita en ese horario.");

        try
        {
            appointment.Reschedule(request.StartsAt, TimeSpan.FromMinutes(request.DurationMinutes));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        appointmentRepository.Update(appointment);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.RequestingUserId,
                AuditAction.ClinicAppointmentRescheduled,
                "VeterinarianAppointment",
                appointment.Id.ToString(),
                $"{request.StartsAt:O}|{request.DurationMinutes}"),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(appointment.Id);
    }
}
