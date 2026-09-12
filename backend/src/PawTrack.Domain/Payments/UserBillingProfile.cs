namespace PawTrack.Domain.Payments;

/// <summary>
/// Tipos de identificación reconocidos por la Dirección General de Tributación (DGT - Costa Rica).
/// </summary>
public enum TaxIdentificationType
{
    Fisica = 1,      // Cédula física (9 dígitos)
    Juridica = 2,    // Cédula jurídica (10 dígitos)
    Dimex = 3,       // Documento de Identificación Migratorio para Extranjeros (11 o 12 dígitos)
    Nite = 4,        // Número de Identificación Tributario Especial (10 dígitos)
    Extranjero = 5,  // Pasaporte u otro documento para no residentes
}

/// <summary>
/// Perfil fiscal y datos de facturación electrónica de un usuario o clínica.
/// </summary>
public sealed class UserBillingProfile
{
    private UserBillingProfile() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TaxIdentificationType IdentificationType { get; private set; }
    public string IdentificationNumber { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string BillingEmail { get; private set; } = string.Empty;
    public string? Province { get; private set; }
    public string? Canton { get; private set; }
    public string? District { get; private set; }
    public string? AddressDetails { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool RequiresInvoice { get; private set; } // true = Factura Electrónica (crédito fiscal); false = Tiquete Electrónico
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static UserBillingProfile Create(
        Guid userId,
        TaxIdentificationType identificationType,
        string identificationNumber,
        string legalName,
        string billingEmail,
        bool requiresInvoice = true,
        string? province = null,
        string? canton = null,
        string? district = null,
        string? addressDetails = null,
        string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificationNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(billingEmail);

        return new UserBillingProfile
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            IdentificationType = identificationType,
            IdentificationNumber = identificationNumber.Trim().Replace("-", "").Replace(" ", ""),
            LegalName = legalName.Trim().ToUpperInvariant(),
            BillingEmail = billingEmail.Trim().ToLowerInvariant(),
            RequiresInvoice = requiresInvoice,
            Province = province?.Trim(),
            Canton = canton?.Trim(),
            District = district?.Trim(),
            AddressDetails = addressDetails?.Trim(),
            PhoneNumber = phoneNumber?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        TaxIdentificationType identificationType,
        string identificationNumber,
        string legalName,
        string billingEmail,
        bool requiresInvoice,
        string? province,
        string? canton,
        string? district,
        string? addressDetails,
        string? phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificationNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(billingEmail);

        IdentificationType = identificationType;
        IdentificationNumber = identificationNumber.Trim().Replace("-", "").Replace(" ", "");
        LegalName = legalName.Trim().ToUpperInvariant();
        BillingEmail = billingEmail.Trim().ToLowerInvariant();
        RequiresInvoice = requiresInvoice;
        Province = province?.Trim();
        Canton = canton?.Trim();
        District = district?.Trim();
        AddressDetails = addressDetails?.Trim();
        PhoneNumber = phoneNumber?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
