using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.CancelCollarHandoverCode;

public sealed record CancelCollarHandoverCodeCommand(Guid HandoverCodeId, Guid OwnerId) : IRequest<Result<bool>>;

public sealed class CancelCollarHandoverCodeCommandHandler(
    ICollarHandoverCodeRepository handoverRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelCollarHandoverCodeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        CancelCollarHandoverCodeCommand request, CancellationToken cancellationToken)
    {
        var code = await handoverRepository.GetByIdAsync(request.HandoverCodeId, cancellationToken);
        if (code is null)
            return Result.Failure<bool>("Código no encontrado.");

        if (code.GeneratedByOwnerId != request.OwnerId)
            return Result.Failure<bool>("Access denied.");

        if (code.IsRedeemed)
            return Result.Failure<bool>("No se puede cancelar un código ya canjeado.");

        code.Cancel();
        handoverRepository.Update(code);

        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.HandoverCancelled,
                "Código de transferencia cancelado por el propietario",
                collarId: code.CollarId, userId: request.OwnerId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
