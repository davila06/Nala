using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Chat;
using PawTrack.Domain.Common;
using System.Text.RegularExpressions;

namespace PawTrack.Application.Chat.Commands.SendChatMessage;

// ── Guard constants ───────────────────────────────────────────────────────────

internal static class Guards
{
    public const int MaxBodyLength = 800;

    // Compiled at JIT time for fast repeated execution.
    // The 100 ms timeout prevents denial-of-service via pathological backtracking inputs.
    private static readonly Regex PhonePattern = new(
        @"\d[\d\s\-\(\)]{6,}\d",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Returns true when the body appears to contain a phone number or email address.
    /// A <see cref="RegexMatchTimeoutException"/> is treated as a non-match (fail-open)
    /// so that crafted inputs cannot block the request thread.
    /// </summary>
    public static bool ContainsContactDetail(string body)
    {
        // Fast path: @ is unambiguous for emails
        if (body.Contains('@'))
            return true;

        try
        {
            return PhonePattern.IsMatch(body);
        }
        catch (RegexMatchTimeoutException)
        {
            // Adversarial input hit the 100 ms timeout — treat as non-match (fail-open).
            // The body still passes through the PiiScrubber before persistence.
            return false;
        }
    }
}

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Appends a message to an existing chat thread.
/// The sender's contact details are never included — a content-safety guard
/// rejects bodies that appear to contain phone numbers or email addresses.
/// </summary>
public sealed record SendChatMessageCommand(
    Guid   ThreadId,
    Guid   SenderUserId,
    string Body)
    : IRequest<Result<Guid>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SendChatMessageCommandHandler(
    IChatRepository        chatRepository,
    IUserRepository        userRepository,
    INotificationDispatcher notificationDispatcher,
    ILostPetRepository     lostPetRepository,
    IPetRepository         petRepository,
    IUnitOfWork            unitOfWork,
    ILogger<SendChatMessageCommandHandler> logger)
    : IRequestHandler<SendChatMessageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        SendChatMessageCommand command,
        CancellationToken      cancellationToken)
    {
        // ── Validate ───────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(command.Body))
            return Result.Failure<Guid>("El mensaje no puede estar vacío.");

        if (command.Body.Length > Guards.MaxBodyLength)
            return Result.Failure<Guid>($"El mensaje no puede superar {Guards.MaxBodyLength} caracteres.");

        if (Guards.ContainsContactDetail(command.Body))
            return Result.Failure<Guid>(
                "Por seguridad, no se permite incluir números de teléfono ni correos en el chat. " +
                "Utiliza el código de entrega para la reunificación presencial.");

        // ── Load and authorise ─────────────────────────────────────────────────
        var thread = await chatRepository.GetThreadByIdAsync(command.ThreadId, cancellationToken);
        if (thread is null)
            return Result.Failure<Guid>("El hilo de conversación no existe.");

        if (thread.Status == Domain.Chat.ChatThreadStatus.Closed)
            return Result.Failure<Guid>("Este hilo está cerrado.");

        if (thread.Status == Domain.Chat.ChatThreadStatus.Flagged)
            return Result.Failure<Guid>("Este hilo ha sido suspendido temporalmente por seguridad.");

        var isParticipant = command.SenderUserId == thread.InitiatorUserId
                            || command.SenderUserId == thread.OwnerUserId;
        if (!isParticipant)
            return Result.Failure<Guid>("No tienes acceso a este hilo.");

        // ── Persist ────────────────────────────────────────────────────────────
        var message = ChatMessage.Create(command.ThreadId, command.SenderUserId, command.Body);
        await chatRepository.AddMessageAsync(message, cancellationToken);

        // Update last-message timestamp on the thread.
        thread.TouchLastMessageAt();
        chatRepository.UpdateThread(thread);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Notify recipient (fire-and-forget; don't block the caller) ─────────
        var recipientId = command.SenderUserId == thread.OwnerUserId
            ? thread.InitiatorUserId
            : thread.OwnerUserId;

        _ = Task.Run(async () =>
        {
            try
            {
                var recipient = await userRepository.GetByIdAsync(recipientId, cancellationToken);
                if (recipient is null) return;

                var lostEvent = await lostPetRepository.GetByIdAsync(thread.LostPetEventId, cancellationToken);
                var petName   = lostEvent is null ? "tu mascota" : string.Empty;
                if (lostEvent is not null)
                {
                    var pet = await petRepository.GetByIdAsync(lostEvent.PetId, cancellationToken);
                    petName = pet?.Name ?? "tu mascota";
                }

                await notificationDispatcher.DispatchNewChatMessageAsync(
                    recipientId,
                    recipient.Email,
                    petName,
                    command.ThreadId.ToString(),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to dispatch chat notification for thread {ThreadId}", command.ThreadId);
            }
        }, CancellationToken.None);

        return Result.Success(message.Id);
    }
}
