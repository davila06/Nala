using PawTrack.Domain.Pets.Events;

namespace PawTrack.Domain.Pets;

public sealed class Pet
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
        MicrochipId = string.IsNullOrWhiteSpace(chipId) ? null : chipId.Trim().ToUpperInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, PetSpecies species, string? breed, DateOnly? birthDate)
    {
        Name = name.Trim();
        Species = species;
        Breed = string.IsNullOrWhiteSpace(breed) ? null : breed.Trim();
        BirthDate = birthDate;
        UpdatedAt = DateTimeOffset.UtcNow;
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
}
