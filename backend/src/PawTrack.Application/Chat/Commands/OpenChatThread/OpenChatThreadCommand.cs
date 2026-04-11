using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Chat;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Chat.Commands.OpenChatThread;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Opens a new masked chat thread between an authenticated user (the finder) and
/// the pet owner, linked to an active lost-pet event.
/// If a thread already exists for the same finder/event pair, returns the existing thread ID.
/// </summary>
/// <remarks>
/// The owner ID is intentionally NOT a parameter — it is always resolved server-side
/// from the lost-pet event to prevent BOLA: a malicious caller must not be able to
/// forge an arbitrary victim's GUID as the thread owner.
/// </remarks>
public sealed record OpenChatThreadCommand(
    Guid LostPetEventId,
    Guid InitiatorUserId)
    : IRequest<Result<Guid>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class OpenChatThreadCommandHandler(
    IChatRepository chatRepository,
    ILostPetRepository lostPetRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<OpenChatThreadCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        OpenChatThreadCommand command,
        CancellationToken     cancellationToken)
    {
        // Validate that the lost-pet event exists and is still active.
        // This load also gives us the authoritative owner ID — we must NOT trust
        // any owner id supplied by the client (BOLA / forged-owner-id prevention).
        var lostEvent = await lostPetRepository.GetByIdAsync(command.LostPetEventId, cancellationToken);
        if (lostEvent is null)
            return Result.Failure<Guid>("El reporte de pérdida no existe.");

        var ownerUserId = lostEvent.OwnerId; // always derived from DB, never from client

        // The finder cannot open a thread with themselves.
        if (command.InitiatorUserId == ownerUserId)
            return Result.Failure<Guid>("No puedes abrir un hilo contigo mismo.");

        // Idempotent: return existing thread if the same finder already opened one.
        var alreadyExists = await chatRepository.ThreadExistsAsync(
            command.LostPetEventId, command.InitiatorUserId, cancellationToken);

        if (alreadyExists)
        {
            // Load and return the existing thread id.
            var existing = (await chatRepository.GetThreadsByLostPetEventAsync(
                    command.LostPetEventId, cancellationToken))
                .FirstOrDefault(t => t.InitiatorUserId == command.InitiatorUserId);

            return existing is not null
                ? Result.Success(existing.Id)
                : Result.Failure<Guid>("Error al recuperar el hilo existente.");
        }

        var thread = ChatThread.Open(command.LostPetEventId, command.InitiatorUserId, ownerUserId);
        await chatRepository.AddThreadAsync(thread, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(thread.Id);
    }
}
