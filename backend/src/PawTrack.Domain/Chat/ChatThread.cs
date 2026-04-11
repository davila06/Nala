namespace PawTrack.Domain.Chat;

/// <summary>
/// A masked communication channel between a pet owner and a finder.
/// Neither party's contact details (phone, email) are exposed through this channel.
/// </summary>
public sealed class ChatThread
{
    private ChatThread() { } // EF Core

    public Guid Id              { get; private set; }
    public Guid LostPetEventId  { get; private set; }

    /// <summary>The user who initiated the conversation (the finder/rescuer).</summary>
    public Guid InitiatorUserId { get; private set; }

    /// <summary>The pet owner.</summary>
    public Guid OwnerUserId     { get; private set; }

    public ChatThreadStatus Status     { get; private set; }
    public DateTimeOffset   CreatedAt  { get; private set; }

    /// <summary>Updated every time a new message is appended; used for inbox ordering.</summary>
    public DateTimeOffset LastMessageAt { get; private set; }

    /// <summary>Human-readable reason stored when <see cref="Status"/> is set to <see cref="ChatThreadStatus.Flagged"/>.</summary>
    public string? FlagReason { get; private set; }

    // ── Navigation ─────────────────────────────────────────────────────────────

    private readonly List<ChatMessage> _messages = [];
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ChatThread Open(Guid lostPetEventId, Guid initiatorUserId, Guid ownerUserId)
    {
        var now = DateTimeOffset.UtcNow;
        return new ChatThread
        {
            Id              = Guid.CreateVersion7(),
            LostPetEventId  = lostPetEventId,
            InitiatorUserId = initiatorUserId,
            OwnerUserId     = ownerUserId,
            Status          = ChatThreadStatus.Active,
            CreatedAt       = now,
            LastMessageAt   = now,
        };
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Close() => Status = ChatThreadStatus.Closed;

    public void Flag(string reason)
    {
        Status     = ChatThreadStatus.Flagged;
        FlagReason = reason;
    }

    /// <summary>Called by the handler after a new message is persisted.</summary>
    public void TouchLastMessageAt() => LastMessageAt = DateTimeOffset.UtcNow;
}
