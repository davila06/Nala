using System.Text.RegularExpressions;

namespace PawTrack.Domain.Collars;

public sealed class CollarTag
{
    // Serial format: PT-[4 hex chars]-[7 digit sequence], e.g. PT-A3F9-0001234
    private static readonly Regex SerialFormat = new(@"^PT-[0-9A-Fa-f]{4}-\d{7}$", RegexOptions.Compiled);

    private CollarTag() { } // EF Core

    public Guid Id { get; private set; }
    public string Serial { get; private set; } = string.Empty;
    public Guid? CollarId { get; private set; }
    public CollarTagStatus Status { get; private set; }
    public string FirmwareVersion { get; private set; } = string.Empty;
    public DateTimeOffset ManufacturedAt { get; private set; }
    public DateTimeOffset? SoldAt { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? LastPingAt { get; private set; }

    public bool IsAvailable => Status == CollarTagStatus.Unactivated;

    public static CollarTag CreateFromFactory(string serial, string firmwareVersion)
    {
        if (!SerialFormat.IsMatch(serial))
            throw new ArgumentException($"Invalid CollarTag serial format: '{serial}'. Expected PT-[4hex]-[7digits].");

        return new CollarTag
        {
            Id = Guid.CreateVersion7(),
            Serial = serial.ToUpperInvariant(),
            Status = CollarTagStatus.Unactivated,
            FirmwareVersion = firmwareVersion.Trim(),
            ManufacturedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkSold()
    {
        if (Status != CollarTagStatus.Unactivated)
            throw new InvalidOperationException($"Cannot mark CollarTag as sold when Status={Status}.");
        SoldAt = DateTimeOffset.UtcNow;
    }

    public void Activate(Guid collarId)
    {
        if (Status != CollarTagStatus.Unactivated)
            throw new InvalidOperationException($"Cannot activate CollarTag: Status={Status}.");
        CollarId = collarId;
        Status = CollarTagStatus.Activated;
        ActivatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (Status != CollarTagStatus.Activated)
            throw new InvalidOperationException($"Cannot deactivate CollarTag: Status={Status}.");
        CollarId = null;
        Status = CollarTagStatus.Unactivated;
    }

    public void UpdateLastPing() => LastPingAt = DateTimeOffset.UtcNow;
}
