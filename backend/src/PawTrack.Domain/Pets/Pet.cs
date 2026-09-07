using PawTrack.Domain.Common;
using PawTrack.Domain.Pets.Events;

namespace PawTrack.Domain.Pets;

public sealed class Pet : IHasDomainEvents
{
    private Pet() { } // EF Core

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? PhotoUrl { get; private set; }
    public PetStatus Status { get; private set; }
    /// <summary>ISO 11784 RFID microchip identifier (max 15 chars). Null if not microchipped.</summary>
    public string? MicrochipId { get; private set; }
    public PetSex Sex { get; private set; }
    public string? Color { get; private set; }
    public string? DistinctiveMarks { get; private set; }
    public SterilizedStatus SterilizedStatus { get; private set; }
    public DateOnly? SterilizedAt { get; private set; }
    public string? ResidenceCanton { get; private set; }
    public MicrochipVerificationStatus MicrochipVerificationStatus { get; private set; }
    public DateTimeOffset? MicrochipVerifiedAt { get; private set; }
    public Guid? MicrochipVerifiedByClinicId { get; private set; }
    public string? MicrochipVerificationNotes { get; private set; }
    public Guid ResponsibleOwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Domain events — dispatched by the handler after persist
    private readonly List<object> _domainEvents = [];
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public static Pet Create(
        Guid ownerId,
        string name,
        PetSpecies species,
        string? breed,
        DateOnly? birthDate)
    {
        var pet = new Pet
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Name = name.Trim(),
            Species = species,
            Breed = string.IsNullOrWhiteSpace(breed) ? null : breed.Trim(),
            BirthDate = birthDate,
            PhotoUrl = null,
            Status = PetStatus.Active,
            Sex = PetSex.Unknown,
            SterilizedStatus = SterilizedStatus.Unknown,
            MicrochipVerificationStatus = MicrochipVerificationStatus.NotProvided,
            ResponsibleOwnerId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        pet._domainEvents.Add(new PetCreatedDomainEvent(pet.Id, ownerId, pet.Name));
        return pet;
    }

    public void SetPhoto(string photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Records or updates the pet's ISO 11784 RFID microchip identifier.</summary>
    public void SetMicrochip(string chipId)
    {
        SetMicrochipDeclared(chipId);
    }

    public Result<bool> SetMicrochipDeclared(string? chipId)
    {
        if (string.IsNullOrWhiteSpace(chipId))
        {
            if (MicrochipVerificationStatus == MicrochipVerificationStatus.Verified)
                return Result.Failure<bool>("No se puede eliminar un microchip verificado sin revisión.");

            MicrochipId = null;
            MicrochipVerificationStatus = MicrochipVerificationStatus.NotProvided;
            MicrochipVerifiedAt = null;
            MicrochipVerifiedByClinicId = null;
            MicrochipVerificationNotes = null;
            UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(true);
        }

        var normalized = chipId.Trim().ToUpperInvariant();
        if (!IsValidMicrochip(normalized))
            return Result.Failure<bool>("El microchip debe contener de 1 a 15 dígitos.");

        if (MicrochipVerificationStatus == MicrochipVerificationStatus.Verified && MicrochipId != normalized)
            return Result.Failure<bool>("No se puede cambiar un microchip verificado sin revisión.");

        MicrochipId = normalized;
        if (MicrochipVerificationStatus != MicrochipVerificationStatus.Verified)
            MicrochipVerificationStatus = MicrochipVerificationStatus.Declared;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(true);
    }

    public void Update(string name, PetSpecies species, string? breed, DateOnly? birthDate)
    {
        Name = name.Trim();
        Species = species;
        Breed = string.IsNullOrWhiteSpace(breed) ? null : breed.Trim();
        BirthDate = birthDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Result<bool> UpdateSanitaryIdentity(
        PetSex sex,
        string? color,
        string? distinctiveMarks,
        SterilizedStatus sterilizedStatus,
        DateOnly? sterilizedAt,
        string? residenceCanton)
    {
        if (sterilizedAt.HasValue && sterilizedAt.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure<bool>("La fecha de esterilización no puede ser futura.");

        if (!string.IsNullOrWhiteSpace(residenceCanton) && LooksLikeExactAddress(residenceCanton))
            return Result.Failure<bool>("Usa solo el cantón de residencia, no una dirección exacta.");

        Sex = sex;
        Color = NormalizeOptional(color);
        DistinctiveMarks = NormalizeOptional(distinctiveMarks);
        SterilizedStatus = sterilizedStatus;
        SterilizedAt = sterilizedStatus == SterilizedStatus.Yes ? sterilizedAt : null;
        ResidenceCanton = NormalizeOptional(residenceCanton);
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(true);
    }

    public Result<bool> VerifyMicrochip(Guid clinicId, string observedChipId, string? notes)
    {
        if (clinicId == Guid.Empty)
            return Result.Failure<bool>("La clínica verificadora es requerida.");

        var normalized = observedChipId.Trim().ToUpperInvariant();
        if (!IsValidMicrochip(normalized))
            return Result.Failure<bool>("El microchip debe contener de 1 a 15 dígitos.");

        if (MicrochipId is not null && MicrochipId != normalized)
        {
            FlagMicrochipConflict(clinicId, normalized, notes ?? "Microchip leído no coincide.");
            return Result.Failure<bool>("El microchip leído no coincide con el microchip declarado.");
        }

        MicrochipId = normalized;
        MicrochipVerificationStatus = MicrochipVerificationStatus.Verified;
        MicrochipVerifiedAt = DateTimeOffset.UtcNow;
        MicrochipVerifiedByClinicId = clinicId;
        MicrochipVerificationNotes = NormalizeOptional(notes);
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(true);
    }

    public Result<bool> FlagMicrochipConflict(Guid clinicId, string observedChipId, string reason)
    {
        if (clinicId == Guid.Empty)
            return Result.Failure<bool>("La clínica reportante es requerida.");
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<bool>("El motivo del conflicto es requerido.");

        var normalized = observedChipId.Trim().ToUpperInvariant();
        if (!IsValidMicrochip(normalized))
            return Result.Failure<bool>("El microchip debe contener de 1 a 15 dígitos.");

        MicrochipVerificationStatus = MicrochipVerificationStatus.Conflict;
        MicrochipVerifiedByClinicId = clinicId;
        MicrochipVerificationNotes = $"Observado: {normalized}. {reason.Trim()}";
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(true);
    }

    public Result<bool> ClearMicrochipVerification(Guid actorId, string reason)
    {
        if (actorId == Guid.Empty)
            return Result.Failure<bool>("El actor es requerido.");
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<bool>("El motivo es requerido.");

        MicrochipVerificationStatus = MicrochipId is null
            ? MicrochipVerificationStatus.NotProvided
            : MicrochipVerificationStatus.Revoked;
        MicrochipVerifiedAt = null;
        MicrochipVerifiedByClinicId = null;
        MicrochipVerificationNotes = reason.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(true);
    }

    public Result<bool> ResolveMicrochipConflict(Guid actorId, string confirmedChipId, string reason)
    {
        if (actorId == Guid.Empty)
            return Result.Failure<bool>("El actor es requerido.");
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<bool>("El motivo es requerido.");

        var normalized = confirmedChipId.Trim().ToUpperInvariant();
        if (!IsValidMicrochip(normalized))
            return Result.Failure<bool>("El microchip debe contener de 1 a 15 dígitos.");

        MicrochipId = normalized;
        MicrochipVerificationStatus = MicrochipVerificationStatus.Verified;
        MicrochipVerifiedAt = DateTimeOffset.UtcNow;
        MicrochipVerificationNotes = reason.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(true);
    }

    public void MarkAsLost()
    {
        Status = PetStatus.Lost;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsReunited()
    {
        Status = PetStatus.Reunited;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsActive()
    {
        Status = PetStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Transitions a reunited pet back to active so the owner can file a new lost report.
    /// Only valid from <see cref="PetStatus.Reunited"/>; any other source status returns failure.
    /// </summary>
    public Result<bool> Reactivate()
    {
        if (Status != PetStatus.Reunited)
            return Result.Failure<bool>("Only reunited pets can be reactivated.");

        Status = PetStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
        _domainEvents.Add(new Events.PetReactivatedDomainEvent(Id, OwnerId, Name));
        return Result.Success(true);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsValidMicrochip(string value) =>
        value.Length is > 0 and <= 15 && value.All(char.IsDigit);

    private static bool LooksLikeExactAddress(string value)
    {
        var normalized = value.ToLowerInvariant();
        return normalized.Contains("calle ") || normalized.Contains("avenida ") || normalized.Contains("casa ");
    }
}
