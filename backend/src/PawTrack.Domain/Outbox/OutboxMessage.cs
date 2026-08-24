namespace PawTrack.Domain.Outbox;

public enum OutboxMessageStatus { Pending, Processed, Failed }

/// <summary>
/// Persisted in the same DB transaction as the domain change.
/// A background processor delivers it at-least-once after commit.
/// </summary>
public sealed class OutboxMessage
{
    private OutboxMessage() { } // EF Core

    public Guid Id { get; private set; }
    /// <summary>Full type name of the notification payload (e.g. "PawTrack.Domain.LostPets.Events.LostPetReportedDomainEvent").</summary>
    public string MessageType { get; private set; } = string.Empty;
    /// <summary>JSON-serialized notification payload.</summary>
    public string Payload { get; private set; } = string.Empty;
    public OutboxMessageStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    /// <summary>Number of delivery attempts made so far.</summary>
    public int AttemptCount { get; private set; }

    public static OutboxMessage Create(string messageType, string payload) => new()
    {
        Id = Guid.CreateVersion7(),
        MessageType = messageType,
        Payload = payload,
        Status = OutboxMessageStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow,
        AttemptCount = 0,
    };

    public void MarkProcessed()
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        AttemptCount++;
        Error = error.Length > 1000 ? error[..1000] : error;
        Status = AttemptCount >= 5 ? OutboxMessageStatus.Failed : OutboxMessageStatus.Pending;
    }
}
