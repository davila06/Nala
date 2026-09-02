namespace PawTrack.Domain.Collars;

/// <summary>
/// Immutable audit trail entry for a CollarTag/Collar lifecycle event.
/// References either <see cref="CollarId"/> (once activated) or <see cref="Serial"/>
/// (before activation, when no Collar exists yet), or both.
/// </summary>
public sealed class CollarAuditEntry
{
    private CollarAuditEntry() { } // EF Core

    public Guid Id { get; private set; }
    public Guid? CollarId { get; private set; }
    public string? Serial { get; private set; }
    /// <summary>User who triggered the event; null for system/device-originated events.</summary>
    public Guid? UserId { get; private set; }
    public CollarAuditEvent Event { get; private set; }
    public string Details { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static CollarAuditEntry Create(
        CollarAuditEvent @event,
        string details,
        Guid? collarId = null,
        string? serial = null,
        Guid? userId = null)
    {
        if (collarId is null && string.IsNullOrWhiteSpace(serial))
            throw new ArgumentException("Un registro de auditoría debe referenciar un CollarId o un Serial.");

        return new CollarAuditEntry
        {
            Id = Guid.CreateVersion7(),
            CollarId = collarId,
            Serial = serial?.ToUpperInvariant(),
            UserId = userId,
            Event = @event,
            Details = details.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
