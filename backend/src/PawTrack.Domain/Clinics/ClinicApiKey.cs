namespace PawTrack.Domain.Clinics;

public static class ClinicApiScope
{
    public const string Scan = "scan";
    public const string MedicalRead = "medical:read";
    public const string MedicalWrite = "medical:write";
    public const string MedicalExport = "medical:export";
    public const string Certificates = "certificates";
    public const string Analytics = "analytics";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Scan, MedicalRead, MedicalWrite, MedicalExport, Certificates, Analytics,
    };
}

public sealed class ClinicApiKey
{
    private ClinicApiKey() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    /// <summary>SHA-256 hex of the raw key — never store the raw key.</summary>
    public string KeyHash { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public bool IsRevoked { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    /// <summary>Keys expire after 1 year by default — long-lived unrotated keys are a security risk.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }
    /// <summary>When set, this key was replaced by a rotation and should be treated as historical.</summary>
    public Guid? RotatedToKeyId { get; private set; }
    public string Scopes { get; private set; } = string.Empty;

    public static ClinicApiKey Create(
        Guid clinicId,
        string keyHash,
        string label,
        TimeSpan? lifetime = null,
        IEnumerable<string>? scopes = null) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            KeyHash = keyHash,
            Label = label.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromDays(365)),
            Scopes = SerializeScopes(scopes ?? ClinicApiScope.All),
        };

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsUsable => !IsRevoked && !IsExpired;

    public bool HasScope(string scope) =>
        IsUsable && DeserializeScopes(Scopes).Contains(scope);

    public IReadOnlyList<string> GetScopes() => DeserializeScopes(Scopes).ToList();

    public void Revoke() => IsRevoked = true;
    public void RecordUsage() => LastUsedAt = DateTimeOffset.UtcNow;
    public void MarkRotatedTo(Guid newKeyId)
    {
        RotatedToKeyId = newKeyId;
        Revoke();
    }

    private static string SerializeScopes(IEnumerable<string> scopes) =>
        System.Text.Json.JsonSerializer.Serialize(
            scopes.Where(scope => ClinicApiScope.All.Contains(scope)).Distinct(StringComparer.Ordinal));

    private static IReadOnlySet<string> DeserializeScopes(string scopes)
    {
        if (string.IsNullOrWhiteSpace(scopes)) return ClinicApiScope.All;
        try
        {
            var values = System.Text.Json.JsonSerializer.Deserialize<string[]>(scopes);
            return values is null
                ? ClinicApiScope.All
                : values.ToHashSet(StringComparer.Ordinal);
        }
        catch (System.Text.Json.JsonException)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }
}

