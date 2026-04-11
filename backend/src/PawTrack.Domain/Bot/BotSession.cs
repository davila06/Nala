namespace PawTrack.Domain.Bot;

using System.Text.Json;

/// <summary>
/// Persists the conversational state for a single WhatsApp user across messages.
/// One session per phone number; a new session is created after the previous one
/// reaches <see cref="BotStep.Completed"/> or <see cref="BotStep.Expired"/>.
/// </summary>
public sealed class BotSession
{
    private BotSession() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>
    /// SHA-256 hash (hex) of the E.164 WhatsApp phone number ("wa_id").
    /// Never store the raw number — only the hash.
    /// </summary>
    public string PhoneNumberHash { get; private set; } = string.Empty;

    /// <summary>Current step in the conversation state machine.</summary>
    public BotStep Step { get; private set; }

    // ── Collected data ────────────────────────────────────────────────────────

    public string? PetName { get; private set; }

    /// <summary>Raw time text as provided by the user (e.g. "hoy a las 3pm").</summary>
    public string? LastSeenRaw { get; private set; }

    /// <summary>Parsed UTC timestamp; null until location step completes.</summary>
    public DateTimeOffset? LastSeenAt { get; private set; }

    /// <summary>Raw location text provided by the user.</summary>
    public string? LocationRaw { get; private set; }

    public double? LastSeenLat { get; private set; }
    public double? LastSeenLng { get; private set; }

    // ── Outcome ───────────────────────────────────────────────────────────────

    /// <summary>ID of the guest <c>User</c> created to own the report.</summary>
    public Guid? GuestUserId { get; private set; }

    /// <summary>ID of the <c>Pet</c> created for the bot report.</summary>
    public Guid? PetId { get; private set; }

    /// <summary>ID of the <c>LostPetEvent</c> created when the flow is completed.</summary>
    public Guid? LostEventId { get; private set; }

    // ── Idempotency ───────────────────────────────────────────────────────────

    /// <summary>
    /// JSON array of already-processed WhatsApp message IDs (wamid).
    /// Meta Cloud API may re-deliver webhooks; this prevents duplicate processing.
    /// </summary>
    public string ProcessedMessageIds { get; private set; } = "[]";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Sessions expire 24 hours after creation if not completed.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Creates a new session in <see cref="BotStep.AwaitingPetName"/> state.</summary>
    public static BotSession Create(string phoneNumberHash) => new()
    {
        Id = Guid.CreateVersion7(),
        PhoneNumberHash = phoneNumberHash,
        Step = BotStep.AwaitingPetName,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
    };

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void SetPetName(string name)
    {
        const int MaxPetNameLength = 50;
        var trimmed = name.Trim();
        PetName = trimmed.Length > MaxPetNameLength
            ? trimmed[..MaxPetNameLength].TrimEnd()
            : trimmed;
        Step = BotStep.AwaitingLastSeenTime;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLastSeenTime(string raw, DateTimeOffset parsedUtc)
    {
        LastSeenRaw = raw;
        LastSeenAt = parsedUtc;
        Step = BotStep.AwaitingLocation;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLocation(string raw, double? lat, double? lng)
    {
        LocationRaw = raw;
        LastSeenLat = lat;
        LastSeenLng = lng;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(Guid guestUserId, Guid petId, Guid lostEventId)
    {
        GuestUserId = guestUserId;
        PetId = petId;
        LostEventId = lostEventId;
        Step = BotStep.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Expire()
    {
        Step = BotStep.Expired;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkMessageProcessed(string wamid)
    {
        var ids = JsonSerializer.Deserialize<List<string>>(ProcessedMessageIds) ?? [];
        if (!ids.Contains(wamid, StringComparer.Ordinal))
            ids.Add(wamid);
        ProcessedMessageIds = JsonSerializer.Serialize(ids);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsMessageProcessed(string wamid) =>
        (JsonSerializer.Deserialize<List<string>>(ProcessedMessageIds) ?? [])
            .Contains(wamid, StringComparer.Ordinal);

    public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAt;
}
