namespace PawTrack.Domain.Chat;

/// <summary>
/// A single message within a <see cref="ChatThread"/>.
/// Message body must not contain the sender's contact details — the API layer
/// enforces a content-safety guard before persisting.
/// </summary>
public sealed class ChatMessage
{
    private ChatMessage() { } // EF Core

    public Guid           Id                { get; private set; }
    public Guid           ThreadId          { get; private set; }
    public Guid           SenderUserId      { get; private set; }

    /// <summary>Maximum 800 characters; validated at application layer.</summary>
    public string         Body              { get; private set; } = string.Empty;

    public DateTimeOffset SentAt            { get; private set; }
    public bool           IsReadByRecipient { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ChatMessage Create(Guid threadId, Guid senderUserId, string body) =>
        new()
        {
            Id                = Guid.CreateVersion7(),
            ThreadId          = threadId,
            SenderUserId      = senderUserId,
            Body              = body.Trim(),
            SentAt            = DateTimeOffset.UtcNow,
            IsReadByRecipient = false,
        };

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void MarkAsRead() => IsReadByRecipient = true;
}
