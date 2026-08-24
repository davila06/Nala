namespace PawTrack.Domain.Bot;

/// <summary>
/// DB-level idempotency record for WhatsApp webhook delivery.
/// Storing each wamid as a separate row with a unique constraint prevents duplicate processing
/// even when multiple Container App instances receive the same Meta re-delivery simultaneously.
/// </summary>
public sealed class WhatsAppProcessedMessage
{
    private WhatsAppProcessedMessage() { } // EF Core

    public Guid Id { get; private set; }
    /// <summary>Meta Cloud API message ID (wamid) — unique per delivered message.</summary>
    public string Wamid { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }

    public static WhatsAppProcessedMessage Create(string wamid) => new()
    {
        Id = Guid.CreateVersion7(),
        Wamid = wamid,
        ReceivedAt = DateTimeOffset.UtcNow,
    };
}
