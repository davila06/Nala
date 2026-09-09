namespace PawTrack.Domain.Clinics;

public sealed class Clinic
{
    private Clinic() { } // EF Core

    public Guid Id { get; private set; }
    /// <summary>The <see cref="PawTrack.Domain.Auth.User"/> account associated with this clinic (Role = Clinic).</summary>
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    /// <summary>SENASA license number — uniquely identifies the clinic in CR.</summary>
    public string LicenseNumber { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public decimal Lat { get; private set; }
    public decimal Lng { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? WhatsAppNumber { get; private set; }
    public bool IsWhatsAppContactEnabled { get; private set; }
    public string? Website { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }
    public string? Services { get; private set; }
    public string? OpeningHours { get; private set; }
    /// <summary>True when the clinic has an active ClinicPlus or ClinicPartner subscription.</summary>
    public bool IsFeatured { get; private set; }
    /// <summary>True when the clinic offers 24/7 emergency care.</summary>
    public bool IsEmergency24h { get; private set; }
    /// <summary>Dedicated emergency phone line; may differ from PhoneNumber.</summary>
    public string? EmergencyPhone { get; private set; }
    public ClinicStatus Status { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    public static Clinic Create(
        Guid userId,
        string name,
        string licenseNumber,
        string address,
        decimal lat,
        decimal lng,
        string contactEmail)
    {
        return new Clinic
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name.Trim(),
            LicenseNumber = licenseNumber.Trim().ToUpperInvariant(),
            Address = address.Trim(),
            Lat = lat,
            Lng = lng,
            ContactEmail = contactEmail.Trim().ToLowerInvariant(),
            Status = ClinicStatus.Pending,
            RegisteredAt = DateTimeOffset.UtcNow,
        };
    }

    public void Activate() => Status = ClinicStatus.Active;
    public void Suspend() => Status = ClinicStatus.Suspended;
    public void SetFeatured(bool value) => IsFeatured = value;
    public void SetLogoUrl(string url) => LogoUrl = url;

    public void UpdateWhatsAppContact(string? whatsAppNumber, bool enabled)
    {
        var normalized = NormalizeCostaRicaMobile(whatsAppNumber);
        WhatsAppNumber = normalized;
        IsWhatsAppContactEnabled = enabled && normalized is not null;
    }

    public void SetEmergencyStatus(bool is24h, string? emergencyPhone)
    {
        IsEmergency24h = is24h;
        EmergencyPhone = emergencyPhone?.Trim();
    }

    public void UpdateContactDetails(string? phoneNumber, string? website)
    {
        if (phoneNumber is not null) PhoneNumber = phoneNumber.Trim();
        if (website is not null) Website = website.Trim();
    }

    public void UpdateProfile(
        string name,
        string address,
        string? phoneNumber,
        string? website,
        bool? isEmergency24h,
        string? emergencyPhone,
        string? description,
        string? services,
        string? openingHours)
    {
        Name = name.Trim();
        Address = address.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Website = website?.Trim();
        Description = description?.Trim();
        Services = services?.Trim();
        OpeningHours = openingHours?.Trim();

        if (isEmergency24h.HasValue || emergencyPhone is not null)
            SetEmergencyStatus(isEmergency24h ?? IsEmergency24h, emergencyPhone);
    }

    private static string? NormalizeCostaRicaMobile(string? number)
    {
        if (string.IsNullOrWhiteSpace(number)) return null;
        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.Length == 8) digits = $"506{digits}";
        if (digits.Length != 11 || !digits.StartsWith("506", StringComparison.Ordinal) || digits[3] is not ('6' or '7' or '8'))
            throw new ArgumentException("El WhatsApp debe ser un número móvil de Costa Rica.");
        return digits;
    }
}
