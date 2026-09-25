using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.CreateVeterinarianScheduleBlock;

public sealed record CreateVeterinarianScheduleBlockCommand(
    Guid ClinicId,
    Guid RequestingUserId,
    Guid VeterinarianId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Reason)
    : IRequest<Result<Guid>>;

public sealed class CreateVeterinarianScheduleBlockCommandValidator
    : AbstractValidator<CreateVeterinarianScheduleBlockCommand>
{
    public CreateVeterinarianScheduleBlockCommandValidator()
    {
        RuleFor(x => x.VeterinarianId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(200);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
    }
}

public sealed class CreateVeterinarianScheduleBlockCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IVeterinarianScheduleBlockRepository blockRepository,
    IVeterinarianAppointmentRepository appointmentRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVeterinarianScheduleBlockCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateVeterinarianScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<Guid>("Acceso denegado.");

        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, cancellationToken);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId || !veterinarian.IsActive)
            return Result.Failure<Guid>("El veterinario no está autorizado.");

        if (await blockRepository.HasOverlapAsync(request.VeterinarianId, request.StartsAt, request.EndsAt, cancellationToken))
            return Result.Failure<Guid>("El veterinario ya tiene un bloqueo en ese horario.");

        if (await appointmentRepository.HasOverlapAsync(request.VeterinarianId, request.StartsAt, request.EndsAt, cancellationToken))
            return Result.Failure<Guid>("El veterinario ya tiene una cita en ese horario.");

        var block = VeterinarianScheduleBlock.Create(
            request.ClinicId,
            request.VeterinarianId,
            request.StartsAt,
            request.EndsAt,
            request.Reason,
            request.RequestingUserId);

        await blockRepository.AddAsync(block, cancellationToken);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.RequestingUserId,
                AuditAction.ClinicScheduleBlockCreated,
                "VeterinarianScheduleBlock",
                block.Id.ToString(),
                request.Reason),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(block.Id);
    }
}
