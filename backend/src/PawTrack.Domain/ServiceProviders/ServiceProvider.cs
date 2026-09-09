namespace PawTrack.Domain.ServiceProviders;

public sealed class ServiceProvider
{
    private ServiceProvider() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ServiceProviderCategory Category { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public decimal Lat { get; private set; }
    public decimal Lng { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? WhatsAppNumber { get; private set; }
    public bool IsWhatsAppContactEnabled { get; private set; }
    public string? Website { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsFeatured { get; private set; }
    public ServiceProviderStatus Status { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public ProviderMembershipTier MembershipTier { get; private set; }
    public DateTimeOffset? TrialEndsAt { get; private set; }
    public bool IsMembershipManual { get; private set; }

    /// <summary>Perfil base (Free) solo incluye directorio/contacto; catalogo, disponibilidad y reservas requieren Verified+.</summary>
    public bool HasCatalogAccess => MembershipTier != ProviderMembershipTier.Free;

    public const int TrialDurationDays = 30;

    public static ServiceProvider Create(
        Guid userId,
        string name,
        string description,
        ServiceProviderCategory category,
        string address,
        decimal lat,
        decimal lng,
        string contactEmail) => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name.Trim(),
            Description = description.Trim(),
            Category = category,
            Address = address.Trim(),
            Lat = lat,
            Lng = lng,
            ContactEmail = contactEmail.Trim().ToLowerInvariant(),
            Status = ServiceProviderStatus.Pending,
            RegisteredAt = DateTimeOffset.UtcNow,
            MembershipTier = ProviderMembershipTier.Free,
        };

    public void Activate()
    {
        Status = ServiceProviderStatus.Active;
        SuspensionReason = null;
        // First approval grants a one-time 30-day Verified trial.
        if (TrialEndsAt is null && !IsMembershipManual)
        {
            MembershipTier = ProviderMembershipTier.Verified;
            TrialEndsAt = DateTimeOffset.UtcNow.AddDays(TrialDurationDays);
        }
    }
    public void Reject() => Status = ServiceProviderStatus.Rejected;
    public void Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Se requiere un motivo de suspension.");
        Status = ServiceProviderStatus.Suspended;
        SuspensionReason = reason.Trim();
    }
    public void SetFeatured(bool value) => IsFeatured = value;
    public void SetLogoUrl(string url) => LogoUrl = url;

    public void UpdateWhatsAppContact(string? whatsAppNumber, bool enabled)
    {
        var normalized = NormalizeCostaRicaMobile(whatsAppNumber);
        WhatsAppNumber = normalized;
        IsWhatsAppContactEnabled = enabled && normalized is not null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Admin grant/renewal. Manual assignments are exempt from automatic trial-expiration downgrades.</summary>
    public void SetMembership(ProviderMembershipTier tier, bool manual)
    {
        MembershipTier = tier;
        IsMembershipManual = manual;
        if (tier == ProviderMembershipTier.Free) TrialEndsAt = null;
    }

    /// <summary>Downgrades an expired, non-manual trial back to Free. Returns true if a change was made.</summary>
    public bool ExpireTrialIfDue(DateTimeOffset now)
    {
        if (IsMembershipManual || MembershipTier == ProviderMembershipTier.Free) return false;
        if (TrialEndsAt is null || TrialEndsAt > now) return false;
        MembershipTier = ProviderMembershipTier.Free;
        IsFeatured = false;
        return true;
    }

    public void UpdateProfile(
        string name,
        string description,
        ServiceProviderCategory category,
        string address,
        decimal lat,
        decimal lng,
        string? phoneNumber,
        string? website)
    {
        Name = name.Trim();
        Description = description.Trim();
        Category = category;
        Address = address.Trim();
        Lat = lat;
        Lng = lng;
        PhoneNumber = phoneNumber is null ? PhoneNumber : (phoneNumber.Trim() is { Length: > 0 } phone ? phone : null);
        Website = website is null ? Website : (website.Trim() is { Length: > 0 } site ? site : null);
        UpdatedAt = DateTimeOffset.UtcNow;
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