namespace PawTrack.Domain.Clinics;

public sealed class ClinicWidgetDomain
{
    private ClinicWidgetDomain() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public string Domain { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ClinicWidgetDomain Create(Guid clinicId, string domain)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic is required.", nameof(clinicId));
        if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentException("Domain is required.", nameof(domain));

        var input = domain.Trim();
        var normalized = input;
        if (Uri.TryCreate(input, UriKind.Absolute, out var parsedUri))
        {
            if (parsedUri.Scheme is not ("http" or "https") ||
                !string.IsNullOrEmpty(parsedUri.UserInfo) ||
                (parsedUri.AbsolutePath is not ("" or "/")) ||
                !string.IsNullOrEmpty(parsedUri.Query) ||
                !string.IsNullOrEmpty(parsedUri.Fragment))
                throw new ArgumentException("Only an http(s) hostname is allowed.", nameof(domain));
            normalized = parsedUri.Host;
        }

        normalized = normalized.TrimEnd('/').ToLowerInvariant();
        if (normalized.Contains('/') || normalized.Contains(':') || normalized.Contains(' '))
            throw new ArgumentException("Only a hostname is allowed.", nameof(domain));
        if (!Uri.CheckHostName(normalized).Equals(UriHostNameType.Dns))
            throw new ArgumentException("A valid DNS hostname is required.", nameof(domain));

        return new ClinicWidgetDomain
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            Domain = normalized,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Deactivate() => IsActive = false;
}
