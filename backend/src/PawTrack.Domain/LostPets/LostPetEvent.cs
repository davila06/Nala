using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets.Events;

namespace PawTrack.Domain.LostPets;

public sealed class LostPetEvent
{
    private LostPetEvent() { } // EF Core

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public LostPetStatus Status { get; private set; }
    public string? Description { get; private set; }
    public double? LastSeenLat { get; private set; }
    public double? LastSeenLng { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset ReportedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public double? ReunionLat { get; private set; }
    public double? ReunionLng { get; private set; }
    public double? RecoveryDistanceMeters { get; private set; }
    public TimeSpan? RecoveryTime { get; private set; }
    public string? CantonName { get; private set; }
    public string? RecentPhotoUrl { get; private set; }

    /// <summary>
    /// Optional free-text message from the owner shown prominently in the public QR profile
    /// when the pet is reported lost. Maximum 200 characters. Safe to expose publicly.
    /// </summary>
    public string? PublicMessage { get; private set; }

    /// <summary>Optional name of the person to contact when the pet is found (safe to expose publicly).</summary>
    public string? ContactName { get; private set; }

    /// <summary>
    /// Phone number to call when the pet is found.
    /// <b>Must NOT be exposed in public endpoints</b> — only surfaced behind authenticated access.
    /// </summary>
    public string? ContactPhone { get; private set; }

    /// <summary>
    /// Optional monetary reward declared by the owner. Safe to show publicly.
    /// Currency is always CRC (Costa Rican colón) in this MVP.
    /// </summary>
    public decimal? RewardAmount { get; private set; }

    /// <summary>Free-text note attached to the reward (max 150 chars). Safe to expose publicly.</summary>
    public string? RewardNote { get; private set; }

    private readonly List<object> _domainEvents = [];
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public static LostPetEvent Create(
        Guid petId,
        Guid ownerId,
        string? description,
        double? lastSeenLat,
        double? lastSeenLng,
        DateTimeOffset lastSeenAt,
        string? publicMessage = null,
        string? contactName = null,
        string? contactPhone = null,
        decimal? rewardAmount = null,
        string? rewardNote = null)
    {
        var report = new LostPetEvent
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            OwnerId = ownerId,
            Status = LostPetStatus.Active,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            PublicMessage = string.IsNullOrWhiteSpace(publicMessage) ? null : publicMessage.Trim(),
            LastSeenLat = lastSeenLat,
            LastSeenLng = lastSeenLng,
            LastSeenAt = lastSeenAt,
            ReportedAt = DateTimeOffset.UtcNow,
            ResolvedAt = null,
            ReunionLat = null,
            ReunionLng = null,
            RecoveryDistanceMeters = null,
            RecoveryTime = null,
            CantonName = null,
            ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim(),
            RewardAmount = rewardAmount > 0 ? rewardAmount : null,
            RewardNote = string.IsNullOrWhiteSpace(rewardNote) ? null : rewardNote.Trim(),
        };

        report._domainEvents.Add(new LostPetReportedDomainEvent(report.Id, petId, ownerId));
        return report;
    }

    public Result<bool> Resolve(LostPetStatus newStatus)
    {
        if (Status != LostPetStatus.Active)
            return Result.Failure<bool>("Only active reports can be resolved.");

        if (newStatus == LostPetStatus.Active)
            return Result.Failure<bool>("Cannot transition back to Active.");

        if (newStatus == LostPetStatus.Reunited)
            return ResolveAsReunited(DateTimeOffset.UtcNow, null, null, null);

        Status = newStatus;
        ResolvedAt = DateTimeOffset.UtcNow;

        return Result.Success(true);
    }

    public Result<bool> ResolveAsReunited(
        DateTimeOffset reunitedAt,
        double? reunionLat,
        double? reunionLng,
        string? cantonName)
    {
        if (Status != LostPetStatus.Active)
            return Result.Failure<bool>("Only active reports can be resolved.");

        Status = LostPetStatus.Reunited;
        ResolvedAt = reunitedAt;
        ReunionLat = reunionLat;
        ReunionLng = reunionLng;
        CantonName = string.IsNullOrWhiteSpace(cantonName) ? null : cantonName.Trim();

        var recoveryDuration = reunitedAt - ReportedAt;
        RecoveryTime = recoveryDuration < TimeSpan.Zero ? TimeSpan.Zero : recoveryDuration;

        RecoveryDistanceMeters = LastSeenLat.HasValue && LastSeenLng.HasValue &&
                                 reunionLat.HasValue && reunionLng.HasValue
            ? HaversineMetres(LastSeenLat.Value, LastSeenLng.Value, reunionLat.Value, reunionLng.Value)
            : null;

        _domainEvents.Add(new PetReunitedDomainEvent(PetId, OwnerId));
        return Result.Success(true);
    }

    /// <summary>
    /// Sets the URL of a recently-taken photo uploaded at time of loss report.
    /// This photo takes precedence over the pet's profile photo in public-facing displays.
    /// </summary>
    public void SetRecentPhoto(string url) => RecentPhotoUrl = url;

    /// <summary>
    /// Allows the owner to set or update the optional reward after report creation.
    /// Passing <c>null</c> clears the reward.
    /// </summary>
    public void SetReward(decimal? amount, string? note)
    {
        RewardAmount = amount > 0 ? amount : null;
        RewardNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private static double HaversineMetres(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMetres = 6_371_000.0;
        var phi1 = ToRadians(lat1);
        var phi2 = ToRadians(lat2);
        var deltaPhi = ToRadians(lat2 - lat1);
        var deltaLambda = ToRadians(lng2 - lng1);

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2)
              + Math.Cos(phi1) * Math.Cos(phi2)
              * Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);

        return 2 * earthRadiusMetres * Math.Asin(Math.Sqrt(a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
