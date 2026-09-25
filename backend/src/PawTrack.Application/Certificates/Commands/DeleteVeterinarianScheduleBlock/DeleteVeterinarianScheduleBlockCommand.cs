using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.DeleteVeterinarianScheduleBlock;

public sealed record DeleteVeterinarianScheduleBlockCommand(
    Guid ClinicId,
    Guid RequestingUserId,
    Guid BlockId)
    : IRequest<Result<Unit>>;

public sealed class DeleteVeterinarianScheduleBlockCommandHandler(
    IClinicRepository clinicRepository,
    IVeterinarianScheduleBlockRepository blockRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteVeterinarianScheduleBlockCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteVeterinarianScheduleBlockCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<Unit>("Acceso denegado.");

        var block = await blockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null || block.ClinicId != request.ClinicId)
            return Result.Failure<Unit>("Bloqueo no encontrado.");

        blockRepository.Delete(block);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.RequestingUserId,
                AuditAction.ClinicScheduleBlockDeleted,
                "VeterinarianScheduleBlock",
                block.Id.ToString(),
                block.Reason),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Unit.Value);
    }
}
