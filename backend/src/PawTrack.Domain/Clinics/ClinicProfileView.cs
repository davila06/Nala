namespace PawTrack.Domain.Clinics;

/// <summary>
/// Records a single view/impression of a clinic profile, map click, or search appearance.
/// Rows older than 90 days are pruned by a retention job.
/// </summary>
public sealed class ClinicProfileView
{
    private ClinicProfileView() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public DateTimeOffset ViewedAt { get; private set; }
    /// <summary>"map", "directory", "search", "alert", "scan_result"</summary>
    public string Source { get; private set; } = string.Empty;
    /// <summary>SHA-256 of the viewer's IP — never raw IP.</summary>
    public string? IpHash { get; private set; }

    public static ClinicProfileView Record(Guid clinicId, string source, string? ipHash = null) => new()
    {
        Id = Guid.CreateVersion7(),
        ClinicId = clinicId,
        ViewedAt = DateTimeOffset.UtcNow,
        Source = source,
        IpHash = ipHash,
    };
}
