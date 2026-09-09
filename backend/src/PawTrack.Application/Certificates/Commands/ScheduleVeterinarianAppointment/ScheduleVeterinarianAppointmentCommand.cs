using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.ScheduleVeterinarianAppointment;

public sealed record ScheduleVeterinarianAppointmentCommand(
    Guid ClinicId, Guid RequestingUserId, Guid VeterinarianId, Guid PetId,
    DateTimeOffset StartsAt, int DurationMinutes)
    : IRequest<Result<Guid>>;

public sealed class ScheduleVeterinarianAppointmentCommandValidator : AbstractValidator<ScheduleVeterinarianAppointmentCommand>
{
    public ScheduleVeterinarianAppointmentCommandValidator()
    {
        RuleFor(x => x.VeterinarianId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 480);
        RuleFor(x => x.StartsAt).GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5));
    }
}

public sealed class ScheduleVeterinarianAppointmentCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IVeterinarianAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ScheduleVeterinarianAppointmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ScheduleVeterinarianAppointmentCommand request, CancellationToken ct)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<Guid>("Acceso denegado.");
        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, ct);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId || !veterinarian.IsActive)
            return Result.Failure<Guid>("El veterinario no está autorizado.");
        var endsAt = request.StartsAt.AddMinutes(request.DurationMinutes);
        if (await appointmentRepository.HasOverlapAsync(request.VeterinarianId, request.StartsAt, endsAt, ct))
            return Result.Failure<Guid>("El veterinario ya tiene una cita en ese horario.");
        var appointment = VeterinarianAppointment.Schedule(
            request.ClinicId, request.VeterinarianId, request.PetId,
            request.StartsAt, TimeSpan.FromMinutes(request.DurationMinutes), request.RequestingUserId);
        await appointmentRepository.AddAsync(appointment, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(appointment.Id);
    }
}