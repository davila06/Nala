using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Safety;

namespace PawTrack.Application.Safety.Commands.GenerateHandoverCode;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Generates (or regenerates) a 4-digit handover code for the specified lost-pet event.
/// Only the pet owner may call this.  Any previously active code is superseded — both
/// old and new codes expire 24 hours after generation.
/// Returns the plain 4-digit code to display to the owner ONCE.
/// </summary>
public sealed record GenerateHandoverCodeCommand(
    Guid LostPetEventId,
    Guid RequestingUserId)
    : IRequest<Result<string>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GenerateHandoverCodeCommandHandler(
    IHandoverCodeRepository handoverCodeRepository,
    ILostPetRepository      lostPetRepository,
    IUnitOfWork             unitOfWork)
    : IRequestHandler<GenerateHandoverCodeCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        GenerateHandoverCodeCommand command,
        CancellationToken           cancellationToken)
    {
        var lostEvent = await lostPetRepository.GetByIdAsync(command.LostPetEventId, cancellationToken);
        if (lostEvent is null)
            return Result.Failure<string>("El reporte de pérdida no existe.");

        if (lostEvent.OwnerId != command.RequestingUserId)
            return Result.Failure<string>("Solo el dueño puede generar el código de entrega.");

        // Expire the previous active code if there is one (supersede it by marking used).
        var existing = await handoverCodeRepository.GetActiveByLostPetEventIdAsync(
            command.LostPetEventId, cancellationToken);

        if (existing is not null)
        {
            existing.MarkAsUsed(command.RequestingUserId); // owner revoked it
            handoverCodeRepository.Update(existing);
        }

        var code = HandoverCode.Generate(command.LostPetEventId);
        await handoverCodeRepository.AddAsync(code, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Return the plain code to show to the owner.
        return Result.Success(code.Code);
    }
}
