using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.UpdateVeterinarianScheduleBlock;

public sealed record UpdateVeterinarianScheduleBlockCommand(
    Guid ClinicId,
    Guid RequestingUserId,
    Guid BlockId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Reason)
    : IRequest<Result<Guid>>;

public sealed class UpdateVeterinarianScheduleBlockCommandValidator
    : AbstractValidator<UpdateVeterinarianScheduleBlockCommand>
{
    public UpdateVeterinarianScheduleBlockCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
        RuleFor(x => x.Reason).MaximumLength(200);
    }
}

public sealed class UpdateVeterinarianScheduleBlockCommandHandler(
    IClinicRepository clinicRepository,
    IVeterinarianScheduleBlockRepository blockRepository,
    IVeterinarianAppointmentRepository appointmentRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVeterinarianScheduleBlockCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateVeterinarianScheduleBlockCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<Guid>("Acceso denegado.");

        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null || block.ClinicId != request.ClinicId)
            return Result.Failure<Guid>("Bloqueo no encontrado.");

        if (await blockRepository.HasOverlapAsync(block.VeterinarianId, request.StartsAt, request.EndsAt, block.Id, cancellationToken))
            return Result.Failure<Guid>("El veterinario ya tiene un bloqueo en ese horario.");

        if (await appointmentRepository.HasOverlapAsync(block.VeterinarianId, request.StartsAt, request.EndsAt, cancellationToken))
            return Result.Failure<Guid>("El veterinario ya tiene una cita en ese horario.");

        try
        {
            block.UpdateWindow(request.StartsAt, request.EndsAt, request.Reason);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        blockRepository.Update(block);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.RequestingUserId,
                AuditAction.ClinicScheduleBlockUpdated,
                "VeterinarianScheduleBlock",
                block.Id.ToString(),
                request.Reason),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(block.Id);
    }
}
