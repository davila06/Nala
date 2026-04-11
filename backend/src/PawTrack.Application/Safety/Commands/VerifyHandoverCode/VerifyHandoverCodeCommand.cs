using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Safety.Commands.VerifyHandoverCode;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// An authenticated rescuer submits the 4-digit code they received verbally from the owner
/// to confirm the physical safe handover of the pet.
/// Returns <c>true</c> on success, <c>false</c> if the code is invalid or expired.
/// </summary>
public sealed record VerifyHandoverCodeCommand(
    Guid   LostPetEventId,
    Guid   VerifierUserId,
    string Code)
    : IRequest<Result<bool>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class VerifyHandoverCodeCommandHandler(
    IHandoverCodeRepository handoverCodeRepository,
    ILostPetRepository      lostPetRepository,
    IUnitOfWork             unitOfWork)
    : IRequestHandler<VerifyHandoverCodeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        VerifyHandoverCodeCommand command,
        CancellationToken         cancellationToken)
    {
        var lostEvent = await lostPetRepository.GetByIdAsync(command.LostPetEventId, cancellationToken);
        if (lostEvent is null)
            return Result.Failure<bool>("El reporte de pérdida no existe.");

        // Owner cannot verify their own code.
        if (lostEvent.OwnerId == command.VerifierUserId)
            return Result.Failure<bool>("El dueño no puede verificar su propio código.");

        var code = await handoverCodeRepository.GetActiveByLostPetEventIdAsync(
            command.LostPetEventId, cancellationToken);

        if (code is null)
            return Result.Success(false); // No active code — quietly return false.

        if (!code.IsValid(command.Code))
            return Result.Success(false);

        code.MarkAsUsed(command.VerifierUserId);
        handoverCodeRepository.Update(code);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
