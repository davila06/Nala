# PawTrack CR — Módulo de Adopciones: Especificación Técnica

> **Versión:** 2.0 | **Fecha:** 2026-08-21
> **Estado:** Listo para implementación — análisis basado en código real
> **Audiencia:** Desarrolladores, PO
> **Stack:** .NET 9 · React 19 · Azure · EF Core 9 · MediatR 12

---

## Tabla de contenidos

1. [Contexto y justificación](#1-contexto-y-justificación)
2. [Actores y roles](#2-actores-y-roles)
3. [Casos de uso](#3-casos-de-uso)
4. [Infraestructura reutilizable](#4-infraestructura-reutilizable)
5. [Domain Model](#5-domain-model)
6. [Application Layer — Commands y Queries](#6-application-layer--commands-y-queries)
7. [Interfaces de repositorio](#7-interfaces-de-repositorio)
8. [Infrastructure — EF Core y Repositorios](#8-infrastructure--ef-core-y-repositorios)
9. [API Controller](#9-api-controller)
10. [Frontend — páginas, hooks y API client](#10-frontend--páginas-hooks-y-api-client)
11. [Notificaciones — extensiones requeridas](#11-notificaciones--extensiones-requeridas)
12. [WhatsApp Bot — intents de adopción](#12-whatsapp-bot--intents-de-adopción)
13. [Migraciones EF Core](#13-migraciones-ef-core)
14. [DI Registration](#14-di-registration)
15. [Tests requeridos](#15-tests-requeridos)
16. [Monetización y planes](#16-monetización-y-planes)
17. [Roadmap de sprints](#17-roadmap-de-sprints)

---

## 1. Contexto y justificación

### El problema

Costa Rica tiene entre **800,000–1,200,000 animales en situación de calle**. Las organizaciones de rescate gestionan adopciones por grupos de Facebook (sin persistencia), WhatsApp (sin rastreo) y ferias presenciales sin seguimiento digital.

### Por qué PawTrack CR es el lugar correcto

El módulo de adopciones NO requiere infraestructura nueva. Es una **capa de features sobre lo que ya existe**:

| Infraestructura existente | Cómo la reutiliza adopciones |
|---|---|
| `AllyType.Shelter` en `Domain/Allies/AllyType.cs` | El actor "organización de adopción" ya existe |
| `IAllyProfileRepository.GetVerifiedByUserIdAsync` | Verificar que quien publica es un Ally verificado |
| `IBlobStorageService` + `BlobHelper.SanitizeFileName` | Fotos de animales en `adoption-photos/` container |
| `INotificationDispatcher` | Nuevos métodos para alertas de adopción |
| `INotificationRepository` + `NotificationType` enum | Historial de notificaciones de adopción |
| `ChatThread` / `ChatMessage` domain | Canal enmascarado adoptante ↔ organización |
| `UserRole.Ally` + `[Authorize(Roles = "Ally")]` | Proteger endpoints de publicación |
| `IUserLocationRepository` | Alertas geofenceadas de ferias de adopción |
| `IPiiScrubber` | Scrubbing de notas en chat |
| `PagedResult<T>` | Paginación de listados |
| `Result<T>` | Manejo de errores |
| `GeoHelper.DistanceMetres` | Filtro por distancia |
| `PetSpecies` enum | Especie del animal en adopción |
| `SubscriptionTier` enum | Nuevo tier `ShelterBasic` / `ShelterPlus` |
| Rate limiting `public-api` | Proteger endpoints públicos |

---

## 2. Actores y roles

| Actor | `UserRole` en código | Capacidades |
|---|---|---|
| Visitante anónimo | — | Ver animales, ver mapa, buscar por filtros |
| Usuario `Owner` | `UserRole.Owner` | Marcar interés, aplicar para adoptar, chatear |
| Aliado verificado `Shelter` | `UserRole.Ally` | Publicar animales, gestionar aplicaciones, crear ferias |
| Admin | `UserRole.Admin` | Moderar, destacar campañas, ver estadísticas globales |

> `AllyType.Shelter` ya existe. Un Ally con `AllyType.Shelter` y `VerificationStatus.Verified` es automáticamente el actor de adopción. No se crea ningún rol nuevo.

---

## 3. Casos de uso

### UC-01 — Publicar animal en adopción
Ally verificado con `AllyType.Shelter` publica un animal con fotos, historia, requisitos y coordenadas de referencia (sin dirección exacta). Estado inicial: `Available`.

### UC-02 — Buscar y filtrar animales
Anónimo o autenticado busca por `PetSpecies`, `PetSize`, `AgeCategory`, distancia GPS, y estado. El resultado incluye pins en el mapa público.

### UC-03 — Aplicar para adoptar
`Owner` autenticado envía una aplicación de adopción. La organización recibe notificación. Se abre un `ChatThread` enmascarado para comunicación.

### UC-04 — Gestionar aplicaciones (organización)
La organización ve todos los `AdoptionApplication` de un animal, cambia estados, y marca el animal como `InProcess` o `Adopted`.

### UC-05 — Crear feria de adopción (evento temporal)
La organización crea un evento con fecha, lugar GPS y lista de animales presentes. El sistema envía alertas geofenceadas a usuarios en radio de 10km. Pin especial en el mapa.

### UC-06 — Seguimiento post-adopción (fase 2)
El adoptante puede registrar actualizaciones con fotos. La organización hace check-in a los 30/90/365 días. Datos de éxito para el módulo de incentivos.

---

## 4. Infraestructura reutilizable

### 4.1 Verificar que el usuario es un Shelter verificado

```csharp
// Patrón usado en handlers: ConfirmAllyAlertActionCommandHandler, etc.
var allyProfile = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
if (allyProfile is null || allyProfile.AllyType != AllyType.Shelter)
    return Result.Failure<AdoptablePetDto>("not_verified_shelter");
```

### 4.2 Subir foto al blob storage

```csharp
// Patrón idéntico al usado en PetsController.UploadPhoto, StoreProductsController
var sanitized = BlobHelper.SanitizeFileName(file.FileName);
var blobName = $"adoption-photos/{animalId}/{Guid.CreateVersion7()}-{sanitized}";
var url = await blobStorage.UploadAsync(blobName, file.OpenReadStream(), file.ContentType, ct);
animal.AddPhoto(url);
```

### 4.3 Abrir ChatThread enmascarado

```csharp
// ChatThread.Create usa LostPetEventId como FK del evento relacionado.
// Para adopciones se usa AdoptionApplicationId como identificador del contexto.
// El campo LostPetEventId se puede reusar con semántica "RelatedEntityId" si se
// extendiera ChatThread, O se crea un thread separado con un nuevo campo AdoptionApplicationId.
// Recomendación: NUEVO campo opcional en ChatThread (ver sección 5.3).
```

---

## 5. Domain Model

### 5.1 `AdoptablePet` — el animal en adopción

```csharp
// backend/src/PawTrack.Domain/Adoptions/AdoptablePet.cs
namespace PawTrack.Domain.Adoptions;

public enum PetSize { XSmall, Small, Medium, Large, XLarge }
public enum AdoptionStatus { Available, InProcess, Adopted, Paused, Removed }
public enum AgeCategory { Puppy, Young, Adult, Senior } // <1y, 1-3y, 3-8y, 8y+

public sealed class AdoptablePet
{
    private AdoptablePet() { } // EF Core
    private readonly List<string> _photoUrls = [];

    public Guid Id { get; private set; }
    /// <summary>FK to AllyProfile.UserId — el shelter que publica este animal.</summary>
    public Guid OrganizationUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public PetSize Size { get; private set; }
    public AgeCategory AgeCategory { get; private set; }
    public int? AgeMonthsApprox { get; private set; }
    public string Story { get; private set; } = string.Empty;
    public string? Requirements { get; private set; }
    public string? MedicalNotes { get; private set; }
    public bool IsVaccinated { get; private set; }
    public bool IsSterilized { get; private set; }
    public bool IsMicrochipped { get; private set; }
    public bool OkWithKids { get; private set; }
    public bool OkWithDogs { get; private set; }
    public bool OkWithCats { get; private set; }
    public bool NeedsYard { get; private set; }
    /// <summary>Coordenada de referencia — NO dirección exacta del shelter.</summary>
    public double RefLat { get; private set; }
    public double RefLng { get; private set; }
    public string? RefLabel { get; private set; } // "San José, Escazú"
    public AdoptionStatus Status { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? AdoptedAt { get; private set; }

    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AdoptablePet Create(
        Guid organizationUserId,
        string name,
        PetSpecies species,
        PetSize size,
        AgeCategory ageCategory,
        string story,
        double refLat,
        double refLng,
        string? refLabel,
        string? breed = null,
        int? ageMonthsApprox = null,
        string? requirements = null,
        string? medicalNotes = null,
        bool isVaccinated = false,
        bool isSterilized = false,
        bool isMicrochipped = false,
        bool okWithKids = false,
        bool okWithDogs = false,
        bool okWithCats = false,
        bool needsYard = false) => new()
        {
            Id = Guid.CreateVersion7(),
            OrganizationUserId = organizationUserId,
            Name = name.Trim(),
            Species = species,
            Breed = breed?.Trim(),
            Size = size,
            AgeCategory = ageCategory,
            AgeMonthsApprox = ageMonthsApprox,
            Story = story.Trim(),
            Requirements = requirements?.Trim(),
            MedicalNotes = medicalNotes?.Trim(),
            IsVaccinated = isVaccinated,
            IsSterilized = isSterilized,
            IsMicrochipped = isMicrochipped,
            OkWithKids = okWithKids,
            OkWithDogs = okWithDogs,
            OkWithCats = okWithCats,
            NeedsYard = needsYard,
            RefLat = refLat,
            RefLng = refLng,
            RefLabel = refLabel?.Trim(),
            Status = AdoptionStatus.Available,
            PublishedAt = DateTimeOffset.UtcNow,
        };

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void AddPhoto(string url)
    {
        if (_photoUrls.Count >= 5) throw new InvalidOperationException("Maximum 5 photos per animal.");
        _photoUrls.Add(url);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemovePhoto(string url)
    {
        _photoUrls.Remove(url);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkInProcess() { Status = AdoptionStatus.InProcess; UpdatedAt = DateTimeOffset.UtcNow; }
    public void MarkAdopted()   { Status = AdoptionStatus.Adopted; AdoptedAt = DateTimeOffset.UtcNow; }
    public void Pause()         { Status = AdoptionStatus.Paused; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Republish()     { Status = AdoptionStatus.Available; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Remove()        { Status = AdoptionStatus.Removed; UpdatedAt = DateTimeOffset.UtcNow; }

    public void UpdateDetails(
        string name, string story, string? requirements, string? medicalNotes,
        bool isVaccinated, bool isSterilized, bool isMicrochipped,
        bool okWithKids, bool okWithDogs, bool okWithCats, bool needsYard)
    {
        Name = name.Trim();
        Story = story.Trim();
        Requirements = requirements?.Trim();
        MedicalNotes = medicalNotes?.Trim();
        IsVaccinated = isVaccinated;
        IsSterilized = isSterilized;
        IsMicrochipped = isMicrochipped;
        OkWithKids = okWithKids;
        OkWithDogs = okWithDogs;
        OkWithCats = okWithCats;
        NeedsYard = needsYard;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

### 5.2 `AdoptionApplication` — solicitud de adopción

```csharp
// backend/src/PawTrack.Domain/Adoptions/AdoptionApplication.cs
namespace PawTrack.Domain.Adoptions;

public enum ApplicationStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    Withdrawn,
}

public sealed class AdoptionApplication
{
    private AdoptionApplication() { } // EF Core

    public Guid Id { get; private set; }
    public Guid AdoptablePetId { get; private set; }
    /// <summary>FK a Auth.Users del solicitante (Role = Owner).</summary>
    public Guid ApplicantUserId { get; private set; }
    public string ApplicantNote { get; private set; } = string.Empty;
    public ApplicationStatus Status { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AdoptionApplication Create(
        Guid adoptablePetId,
        Guid applicantUserId,
        string applicantNote) => new()
        {
            Id = Guid.CreateVersion7(),
            AdoptablePetId = adoptablePetId,
            ApplicantUserId = applicantUserId,
            ApplicantNote = applicantNote.Trim(),
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTimeOffset.UtcNow,
        };

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void StartReview() { Status = ApplicationStatus.UnderReview; ReviewedAt = DateTimeOffset.UtcNow; }
    public void Approve(string? note = null) { Status = ApplicationStatus.Approved; ReviewNote = note?.Trim(); ReviewedAt = DateTimeOffset.UtcNow; }
    public void Reject(string? note = null)  { Status = ApplicationStatus.Rejected; ReviewNote = note?.Trim(); ReviewedAt = DateTimeOffset.UtcNow; }
    public void Withdraw()                   { Status = ApplicationStatus.Withdrawn; }
}
```

### 5.3 `AdoptionFair` — feria/evento de adopción

```csharp
// backend/src/PawTrack.Domain/Adoptions/AdoptionFair.cs
namespace PawTrack.Domain.Adoptions;

public enum FairStatus { Upcoming, Active, Finished, Cancelled }

public sealed class AdoptionFair
{
    private AdoptionFair() { } // EF Core
    private readonly List<Guid> _animalIds = [];

    public Guid Id { get; private set; }
    public Guid OrganizationUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string VenueLabel { get; private set; } = string.Empty;
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public FairStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>IDs of AdoptablePet records that will be present at this fair.</summary>
    public IReadOnlyList<Guid> AnimalIds => _animalIds.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AdoptionFair Create(
        Guid organizationUserId,
        string title,
        string venueLabel,
        double lat,
        double lng,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? description = null)
    {
        if (endsAt <= startsAt) throw new ArgumentException("EndsAt must be after StartsAt.");
        return new AdoptionFair
        {
            Id = Guid.CreateVersion7(),
            OrganizationUserId = organizationUserId,
            Title = title.Trim(),
            Description = description?.Trim(),
            VenueLabel = venueLabel.Trim(),
            Lat = lat,
            Lng = lng,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = FairStatus.Upcoming,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void AddAnimal(Guid animalId)
    {
        if (!_animalIds.Contains(animalId)) _animalIds.Add(animalId);
    }

    public void RemoveAnimal(Guid animalId) => _animalIds.Remove(animalId);
    public void Activate()  { Status = FairStatus.Active;    UpdatedAt = DateTimeOffset.UtcNow; }
    public void Finish()    { Status = FairStatus.Finished;  UpdatedAt = DateTimeOffset.UtcNow; }
    public void Cancel()    { Status = FairStatus.Cancelled; UpdatedAt = DateTimeOffset.UtcNow; }

    public bool IsCurrentlyActive =>
        Status == FairStatus.Active &&
        DateTimeOffset.UtcNow >= StartsAt &&
        DateTimeOffset.UtcNow < EndsAt;
}
```

### 5.4 Extensión de `NotificationType`

```csharp
// backend/src/PawTrack.Domain/Notifications/NotificationType.cs
// AGREGAR al enum existente (no reemplazar):
public enum NotificationType
{
    // ... valores existentes ...
    AdoptionInterest,    // el shelter recibe: alguien aplicó para adoptar
    AdoptionApproved,    // el adoptante recibe: su aplicación fue aprobada
    AdoptionRejected,    // el adoptante recibe: su aplicación fue rechazada
    AdoptionFairAlert,   // usuarios cercanos: hay una feria de adopción cerca
}
```

### 5.5 Extensión de `SubscriptionTier`

```csharp
// backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs
// AGREGAR al enum existente:
public enum SubscriptionTier
{
    // ... valores existentes ...
    // Shelter/adoption tiers
    ShelterBasic  = 300, // gratis — directorio + publicar hasta 5 animales
    ShelterPlus   = 310, // ₡8,000/mes — ilimitado + ferias + destacado en mapa
}
```

---

## 6. Application Layer — Commands y Queries

Crear el archivo: `backend/src/PawTrack.Application/Adoptions/AdoptionCommands.cs`

```csharp
// backend/src/PawTrack.Application/Adoptions/AdoptionCommands.cs
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Adoptions;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record AdoptablePetDto(
    string Id,
    string OrganizationUserId,
    string OrganizationName,
    string Name,
    string Species,
    string? Breed,
    string Size,
    string AgeCategory,
    int? AgeMonthsApprox,
    string Story,
    string? Requirements,
    string? MedicalNotes,
    bool IsVaccinated,
    bool IsSterilized,
    bool IsMicrochipped,
    bool OkWithKids,
    bool OkWithDogs,
    bool OkWithCats,
    bool NeedsYard,
    double RefLat,
    double RefLng,
    string? RefLabel,
    string Status,
    IReadOnlyList<string> PhotoUrls,
    DateTimeOffset PublishedAt)
{
    public static AdoptablePetDto FromDomain(AdoptablePet p, string organizationName) => new(
        p.Id.ToString(), p.OrganizationUserId.ToString(), organizationName,
        p.Name, p.Species.ToString(), p.Breed, p.Size.ToString(), p.AgeCategory.ToString(),
        p.AgeMonthsApprox, p.Story, p.Requirements, p.MedicalNotes,
        p.IsVaccinated, p.IsSterilized, p.IsMicrochipped,
        p.OkWithKids, p.OkWithDogs, p.OkWithCats, p.NeedsYard,
        p.RefLat, p.RefLng, p.RefLabel, p.Status.ToString(),
        p.PhotoUrls, p.PublishedAt);
}

public sealed record AdoptionApplicationDto(
    string Id,
    string AdoptablePetId,
    string ApplicantUserId,
    string ApplicantNote,
    string Status,
    string? ReviewNote,
    DateTimeOffset AppliedAt,
    DateTimeOffset? ReviewedAt);

public sealed record AdoptionFairDto(
    string Id,
    string OrganizationUserId,
    string Title,
    string? Description,
    string VenueLabel,
    double Lat,
    double Lng,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status,
    IReadOnlyList<string> AnimalIds)
{
    public static AdoptionFairDto FromDomain(AdoptionFair f) => new(
        f.Id.ToString(), f.OrganizationUserId.ToString(),
        f.Title, f.Description, f.VenueLabel, f.Lat, f.Lng,
        f.StartsAt, f.EndsAt, f.Status.ToString(),
        f.AnimalIds.Select(id => id.ToString()).ToList().AsReadOnly());
}

// ── Publish animal ────────────────────────────────────────────────────────────

public sealed record PublishAdoptablePetCommand(
    Guid OrganizationUserId,
    string Name,
    PetSpecies Species,
    PetSize Size,
    AgeCategory AgeCategory,
    string Story,
    double RefLat,
    double RefLng,
    string? RefLabel,
    string? Breed,
    int? AgeMonthsApprox,
    string? Requirements,
    string? MedicalNotes,
    bool IsVaccinated,
    bool IsSterilized,
    bool IsMicrochipped,
    bool OkWithKids,
    bool OkWithDogs,
    bool OkWithCats,
    bool NeedsYard) : IRequest<Result<AdoptablePetDto>>;

public sealed class PublishAdoptablePetCommandValidator : AbstractValidator<PublishAdoptablePetCommand>
{
    public PublishAdoptablePetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Story).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Requirements).MaximumLength(500);
        RuleFor(x => x.MedicalNotes).MaximumLength(500);
        RuleFor(x => x.Breed).MaximumLength(100);
        RuleFor(x => x.RefLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.RefLng).InclusiveBetween(-180, 180);
        RuleFor(x => x.AgeMonthsApprox).GreaterThan(0).When(x => x.AgeMonthsApprox.HasValue);
    }
}

public sealed class PublishAdoptablePetCommandHandler(
    IAllyProfileRepository allyProfileRepository,
    IAdoptionRepository adoptionRepository,
    IUnitOfWork unitOfWork,
    ILogger<PublishAdoptablePetCommandHandler> logger)
    : IRequestHandler<PublishAdoptablePetCommand, Result<AdoptablePetDto>>
{
    internal const string NotVerifiedShelterError = "not_verified_shelter";

    public async Task<Result<AdoptablePetDto>> Handle(
        PublishAdoptablePetCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptablePetDto>(NotVerifiedShelterError);

        var animal = AdoptablePet.Create(
            request.OrganizationUserId, request.Name, request.Species,
            request.Size, request.AgeCategory, request.Story,
            request.RefLat, request.RefLng, request.RefLabel,
            request.Breed, request.AgeMonthsApprox, request.Requirements,
            request.MedicalNotes, request.IsVaccinated, request.IsSterilized,
            request.IsMicrochipped, request.OkWithKids, request.OkWithDogs,
            request.OkWithCats, request.NeedsYard);

        await adoptionRepository.AddAnimalAsync(animal, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Animal {AnimalId} published for adoption by shelter {ShelterId}",
            animal.Id, request.OrganizationUserId);

        return Result.Success(AdoptablePetDto.FromDomain(animal, ally.OrganizationName));
    }
}

// ── Apply to adopt ────────────────────────────────────────────────────────────

public sealed record ApplyToAdoptCommand(
    Guid ApplicantUserId,
    Guid AdoptablePetId,
    string ApplicantNote) : IRequest<Result<AdoptionApplicationDto>>;

public sealed class ApplyToAdoptCommandValidator : AbstractValidator<ApplyToAdoptCommand>
{
    public ApplyToAdoptCommandValidator()
    {
        RuleFor(x => x.ApplicantNote).NotEmpty().MaximumLength(500);
    }
}

public sealed class ApplyToAdoptCommandHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork,
    ILogger<ApplyToAdoptCommandHandler> logger)
    : IRequestHandler<ApplyToAdoptCommand, Result<AdoptionApplicationDto>>
{
    internal const string AnimalNotFoundError = "animal_not_found";
    internal const string AnimalNotAvailableError = "animal_not_available";
    internal const string DuplicateApplicationError = "duplicate_application";

    public async Task<Result<AdoptionApplicationDto>> Handle(
        ApplyToAdoptCommand request, CancellationToken ct)
    {
        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AdoptablePetId, ct);
        if (animal is null)
            return Result.Failure<AdoptionApplicationDto>(AnimalNotFoundError);

        if (animal.Status != AdoptionStatus.Available)
            return Result.Failure<AdoptionApplicationDto>(AnimalNotAvailableError);

        var existing = await adoptionRepository.GetApplicationByApplicantAndAnimalAsync(
            request.ApplicantUserId, request.AdoptablePetId, ct);
        if (existing is not null && existing.Status == ApplicationStatus.Pending)
            return Result.Failure<AdoptionApplicationDto>(DuplicateApplicationError);

        var application = AdoptionApplication.Create(
            request.AdoptablePetId, request.ApplicantUserId, request.ApplicantNote);

        await adoptionRepository.AddApplicationAsync(application, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Notify the shelter asynchronously
        _ = notificationDispatcher.DispatchAdoptionInterestAsync(
                animal.OrganizationUserId,
                animal.Name,
                application.Id, ct)
            .ContinueWith(t => logger.LogWarning(t.Exception,
                "Adoption interest notification failed for app {AppId}", application.Id),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        return Result.Success(new AdoptionApplicationDto(
            application.Id.ToString(), application.AdoptablePetId.ToString(),
            application.ApplicantUserId.ToString(), application.ApplicantNote,
            application.Status.ToString(), application.ReviewNote,
            application.AppliedAt, application.ReviewedAt));
    }
}

// ── Review application (shelter) ─────────────────────────────────────────────

public sealed record ReviewAdoptionApplicationCommand(
    Guid OrganizationUserId,
    Guid ApplicationId,
    bool Approve,
    string? ReviewNote) : IRequest<Result<AdoptionApplicationDto>>;

public sealed class ReviewAdoptionApplicationCommandValidator
    : AbstractValidator<ReviewAdoptionApplicationCommand>
{
    public ReviewAdoptionApplicationCommandValidator()
    {
        RuleFor(x => x.ReviewNote).MaximumLength(300);
    }
}

public sealed class ReviewAdoptionApplicationCommandHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewAdoptionApplicationCommand, Result<AdoptionApplicationDto>>
{
    public async Task<Result<AdoptionApplicationDto>> Handle(
        ReviewAdoptionApplicationCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptionApplicationDto>("not_verified_shelter");

        var application = await adoptionRepository.GetApplicationByIdAsync(request.ApplicationId, ct);
        if (application is null)
            return Result.Failure<AdoptionApplicationDto>("application_not_found");

        var animal = await adoptionRepository.GetAnimalByIdAsync(application.AdoptablePetId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<AdoptionApplicationDto>("access_denied");

        if (request.Approve)
        {
            application.Approve(request.ReviewNote);
            animal.MarkInProcess();
            adoptionRepository.UpdateAnimal(animal);

            _ = notificationDispatcher.DispatchAdoptionApprovedAsync(
                application.ApplicantUserId, animal.Name, application.Id, ct);
        }
        else
        {
            application.Reject(request.ReviewNote);

            _ = notificationDispatcher.DispatchAdoptionRejectedAsync(
                application.ApplicantUserId, animal.Name, application.Id, ct);
        }

        adoptionRepository.UpdateApplication(application);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new AdoptionApplicationDto(
            application.Id.ToString(), application.AdoptablePetId.ToString(),
            application.ApplicantUserId.ToString(), application.ApplicantNote,
            application.Status.ToString(), application.ReviewNote,
            application.AppliedAt, application.ReviewedAt));
    }
}

// ── Mark adopted ─────────────────────────────────────────────────────────────

public sealed record MarkAdoptedCommand(
    Guid OrganizationUserId,
    Guid AdoptablePetId) : IRequest<Result<AdoptablePetDto>>;

public sealed class MarkAdoptedCommandHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkAdoptedCommand, Result<AdoptablePetDto>>
{
    public async Task<Result<AdoptablePetDto>> Handle(
        MarkAdoptedCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptablePetDto>("not_verified_shelter");

        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AdoptablePetId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<AdoptablePetDto>("access_denied");

        animal.MarkAdopted();
        adoptionRepository.UpdateAnimal(animal);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(AdoptablePetDto.FromDomain(animal, ally.OrganizationName));
    }
}

// ── Create fair ───────────────────────────────────────────────────────────────

public sealed record CreateAdoptionFairCommand(
    Guid OrganizationUserId,
    string Title,
    string VenueLabel,
    double Lat,
    double Lng,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Description,
    IReadOnlyList<Guid> AnimalIds) : IRequest<Result<AdoptionFairDto>>;

public sealed class CreateAdoptionFairCommandValidator : AbstractValidator<CreateAdoptionFairCommand>
{
    public CreateAdoptionFairCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.VenueLabel).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
        RuleFor(x => x.StartsAt).GreaterThan(DateTimeOffset.UtcNow);
    }
}

public sealed class CreateAdoptionFairCommandHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork,
    ILogger<CreateAdoptionFairCommandHandler> logger)
    : IRequestHandler<CreateAdoptionFairCommand, Result<AdoptionFairDto>>
{
    public async Task<Result<AdoptionFairDto>> Handle(
        CreateAdoptionFairCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptionFairDto>("not_verified_shelter");

        var fair = AdoptionFair.Create(
            request.OrganizationUserId, request.Title, request.VenueLabel,
            request.Lat, request.Lng, request.StartsAt, request.EndsAt, request.Description);

        foreach (var animalId in request.AnimalIds)
            fair.AddAnimal(animalId);

        await adoptionRepository.AddFairAsync(fair, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Geofenced alert en radio 10km
        _ = notificationDispatcher.DispatchAdoptionFairAlertAsync(
                fair.Id, fair.Title, fair.Lat, fair.Lng,
                radiusMetres: 10_000, fair.StartsAt, ct)
            .ContinueWith(t => logger.LogWarning(t.Exception, "Fair geofence alert failed"),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        return Result.Success(AdoptionFairDto.FromDomain(fair));
    }
}
```

### 6.2 Queries

Crear el archivo: `backend/src/PawTrack.Application/Adoptions/AdoptionQueries.cs`

```csharp
// backend/src/PawTrack.Application/Adoptions/AdoptionQueries.cs
using FluentValidation;
using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Adoptions;

// ── Get public animals (paginado, filtrable) ──────────────────────────────────

public sealed record GetAdoptablePetsQuery(
    PetSpecies? Species,
    PetSize? Size,
    AgeCategory? AgeCategory,
    bool? IsVaccinated,
    bool? IsSterilized,
    bool? OkWithKids,
    bool? OkWithDogs,
    double? NearLat,
    double? NearLng,
    int? RadiusKm,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdoptablePetDto>>>;

public sealed class GetAdoptablePetsQueryHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetAdoptablePetsQuery, Result<PagedResult<AdoptablePetDto>>>
{
    public async Task<Result<PagedResult<AdoptablePetDto>>> Handle(
        GetAdoptablePetsQuery request, CancellationToken ct)
    {
        var (items, total) = await adoptionRepository.GetAvailablePagedAsync(
            request.Species, request.Size, request.AgeCategory,
            request.IsVaccinated, request.IsSterilized, request.OkWithKids,
            request.OkWithDogs, request.NearLat, request.NearLng,
            request.RadiusKm ?? 50,
            (request.Page - 1) * request.PageSize, request.PageSize, ct);

        // Cargar nombres de organización en batch
        var orgIds = items.Select(a => a.OrganizationUserId).Distinct().ToList();
        var allies = await allyProfileRepository.GetByUserIdsAsync(orgIds, ct);
        var orgNames = allies.ToDictionary(a => a.UserId, a => a.OrganizationName);

        var dtos = items
            .Select(a => AdoptablePetDto.FromDomain(a, orgNames.GetValueOrDefault(a.OrganizationUserId, "Organización")))
            .ToList();

        return Result.Success(new PagedResult<AdoptablePetDto>(dtos, total, request.Page, request.PageSize));
    }
}

// ── Get my organization's animals ─────────────────────────────────────────────

public sealed record GetMyAdoptionAnimalsQuery(
    Guid OrganizationUserId,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdoptablePetDto>>>;

public sealed class GetMyAdoptionAnimalsQueryHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetMyAdoptionAnimalsQuery, Result<PagedResult<AdoptablePetDto>>>
{
    public async Task<Result<PagedResult<AdoptablePetDto>>> Handle(
        GetMyAdoptionAnimalsQuery request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null)
            return Result.Failure<PagedResult<AdoptablePetDto>>("ally_not_found");

        var skip = (request.Page - 1) * request.PageSize;
        var items = await adoptionRepository.GetByOrganizationAsync(
            request.OrganizationUserId, skip, request.PageSize, ct);
        var total = await adoptionRepository.CountByOrganizationAsync(request.OrganizationUserId, ct);

        var dtos = items.Select(a => AdoptablePetDto.FromDomain(a, ally.OrganizationName)).ToList();
        return Result.Success(new PagedResult<AdoptablePetDto>(dtos, total, request.Page, request.PageSize));
    }
}

// ── Get applications for an animal (shelter view) ────────────────────────────

public sealed record GetApplicationsForAnimalQuery(
    Guid OrganizationUserId,
    Guid AdoptablePetId) : IRequest<Result<IReadOnlyList<AdoptionApplicationDto>>>;

public sealed class GetApplicationsForAnimalQueryHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetApplicationsForAnimalQuery, Result<IReadOnlyList<AdoptionApplicationDto>>>
{
    public async Task<Result<IReadOnlyList<AdoptionApplicationDto>>> Handle(
        GetApplicationsForAnimalQuery request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<IReadOnlyList<AdoptionApplicationDto>>("not_verified_shelter");

        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AdoptablePetId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<IReadOnlyList<AdoptionApplicationDto>>("access_denied");

        var apps = await adoptionRepository.GetApplicationsByAnimalAsync(request.AdoptablePetId, ct);
        var dtos = apps.Select(a => new AdoptionApplicationDto(
            a.Id.ToString(), a.AdoptablePetId.ToString(), a.ApplicantUserId.ToString(),
            a.ApplicantNote, a.Status.ToString(), a.ReviewNote, a.AppliedAt, a.ReviewedAt))
            .ToList();

        return Result.Success<IReadOnlyList<AdoptionApplicationDto>>(dtos);
    }
}

// ── Get my applications (applicant view) ─────────────────────────────────────

public sealed record GetMyAdoptionApplicationsQuery(
    Guid ApplicantUserId,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdoptionApplicationDto>>>;

// ── Get upcoming fairs (public, por zona) ────────────────────────────────────

public sealed record GetUpcomingFairsQuery(
    double? NearLat,
    double? NearLng,
    int? RadiusKm) : IRequest<Result<IReadOnlyList<AdoptionFairDto>>>;

public sealed class GetUpcomingFairsQueryHandler(IAdoptionRepository adoptionRepository)
    : IRequestHandler<GetUpcomingFairsQuery, Result<IReadOnlyList<AdoptionFairDto>>>
{
    public async Task<Result<IReadOnlyList<AdoptionFairDto>>> Handle(
        GetUpcomingFairsQuery request, CancellationToken ct)
    {
        var fairs = await adoptionRepository.GetUpcomingFairsAsync(
            request.NearLat, request.NearLng, request.RadiusKm ?? 50, ct);
        return Result.Success(fairs.Select(AdoptionFairDto.FromDomain).ToList()
            .AsReadOnly() as IReadOnlyList<AdoptionFairDto>);
    }
}
```

---

## 7. Interfaces de repositorio

Crear: `backend/src/PawTrack.Application/Common/Interfaces/IAdoptionRepository.cs`

```csharp
// backend/src/PawTrack.Application/Common/Interfaces/IAdoptionRepository.cs
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Common.Interfaces;

public interface IAdoptionRepository
{
    // Animals
    Task<AdoptablePet?> GetAnimalByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptablePet>> GetByOrganizationAsync(Guid orgUserId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByOrganizationAsync(Guid orgUserId, CancellationToken ct = default);
    Task<(IReadOnlyList<AdoptablePet> Items, int Total)> GetAvailablePagedAsync(
        PetSpecies? species, PetSize? size, AgeCategory? ageCategory,
        bool? isVaccinated, bool? isSterilized, bool? okWithKids, bool? okWithDogs,
        double? nearLat, double? nearLng, int radiusKm,
        int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptablePet>> GetAvailableAllAsync(CancellationToken ct = default); // para el mapa
    Task AddAnimalAsync(AdoptablePet animal, CancellationToken ct = default);
    void UpdateAnimal(AdoptablePet animal);

    // Applications
    Task<AdoptionApplication?> GetApplicationByIdAsync(Guid id, CancellationToken ct = default);
    Task<AdoptionApplication?> GetApplicationByApplicantAndAnimalAsync(Guid applicantUserId, Guid animalId, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionApplication>> GetApplicationsByAnimalAsync(Guid animalId, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionApplication>> GetApplicationsByApplicantAsync(Guid applicantUserId, int skip, int take, CancellationToken ct = default);
    Task<int> CountApplicationsByApplicantAsync(Guid applicantUserId, CancellationToken ct = default);
    Task AddApplicationAsync(AdoptionApplication application, CancellationToken ct = default);
    void UpdateApplication(AdoptionApplication application);

    // Fairs
    Task<AdoptionFair?> GetFairByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionFair>> GetUpcomingFairsAsync(double? nearLat, double? nearLng, int radiusKm, CancellationToken ct = default);
    Task AddFairAsync(AdoptionFair fair, CancellationToken ct = default);
    void UpdateFair(AdoptionFair fair);
}
```

También añadir a `IAllyProfileRepository.cs`:

```csharp
// AGREGAR al interface IAllyProfileRepository existente:
Task<IReadOnlyList<AllyProfile>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
```

---

## 8. Infrastructure — EF Core y Repositorios

### 8.1 EF Core Configuration

Crear: `backend/src/PawTrack.Infrastructure/Adoptions/AdoptionConfiguration.cs`

```csharp
// backend/src/PawTrack.Infrastructure/Adoptions/AdoptionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Adoptions;

namespace PawTrack.Infrastructure.Adoptions;

internal sealed class AdoptablePetConfiguration : IEntityTypeConfiguration<AdoptablePet>
{
    public void Configure(EntityTypeBuilder<AdoptablePet> b)
    {
        b.ToTable("AdoptableAnimals");
        b.HasKey(a => a.Id);

        b.Property(a => a.Name).HasMaxLength(80).IsRequired();
        b.Property(a => a.Story).HasMaxLength(2000).IsRequired();
        b.Property(a => a.Breed).HasMaxLength(100);
        b.Property(a => a.Requirements).HasMaxLength(500);
        b.Property(a => a.MedicalNotes).HasMaxLength(500);
        b.Property(a => a.RefLabel).HasMaxLength(100);
        b.Property(a => a.RefLat).HasColumnType("decimal(9,6)");
        b.Property(a => a.RefLng).HasColumnType("decimal(9,6)");
        b.Property(a => a.Species).HasConversion<string>();
        b.Property(a => a.Size).HasConversion<string>();
        b.Property(a => a.AgeCategory).HasConversion<string>();
        b.Property(a => a.Status).HasConversion<string>();

        // PhotoUrls stored as JSON array
        b.Property<List<string>>("_photoUrls")
            .HasField("_photoUrls")
            .HasColumnName("PhotoUrls")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        b.HasIndex(a => a.OrganizationUserId);
        b.HasIndex(a => a.Status);
        b.HasIndex(a => new { a.Species, a.Status });
    }
}

internal sealed class AdoptionApplicationConfiguration : IEntityTypeConfiguration<AdoptionApplication>
{
    public void Configure(EntityTypeBuilder<AdoptionApplication> b)
    {
        b.ToTable("AdoptionApplications");
        b.HasKey(a => a.Id);

        b.Property(a => a.ApplicantNote).HasMaxLength(500).IsRequired();
        b.Property(a => a.ReviewNote).HasMaxLength(300);
        b.Property(a => a.Status).HasConversion<string>();

        b.HasIndex(a => a.AdoptablePetId);
        b.HasIndex(a => a.ApplicantUserId);
        b.HasIndex(a => new { a.ApplicantUserId, a.AdoptablePetId }).IsUnique(); // Un solo pending por par
    }
}

internal sealed class AdoptionFairConfiguration : IEntityTypeConfiguration<AdoptionFair>
{
    public void Configure(EntityTypeBuilder<AdoptionFair> b)
    {
        b.ToTable("AdoptionFairs");
        b.HasKey(f => f.Id);

        b.Property(f => f.Title).HasMaxLength(150).IsRequired();
        b.Property(f => f.Description).HasMaxLength(1000);
        b.Property(f => f.VenueLabel).HasMaxLength(200).IsRequired();
        b.Property(f => f.Status).HasConversion<string>();

        // AnimalIds stored as JSON array
        b.Property<List<Guid>>("_animalIds")
            .HasField("_animalIds")
            .HasColumnName("AnimalIds")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        b.HasIndex(f => f.OrganizationUserId);
        b.HasIndex(f => f.Status);
        b.HasIndex(f => f.StartsAt);
    }
}
```

### 8.2 Repositorio

Crear: `backend/src/PawTrack.Infrastructure/Adoptions/AdoptionRepository.cs`

```csharp
// backend/src/PawTrack.Infrastructure/Adoptions/AdoptionRepository.cs
using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Adoptions;

public sealed class AdoptionRepository(PawTrackDbContext db) : IAdoptionRepository
{
    public Task<AdoptablePet?> GetAnimalByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AdoptableAnimals.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AdoptablePet>> GetByOrganizationAsync(
        Guid orgUserId, int skip, int take, CancellationToken ct = default) =>
        await db.AdoptableAnimals.AsNoTracking()
            .Where(a => a.OrganizationUserId == orgUserId)
            .OrderByDescending(a => a.PublishedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountByOrganizationAsync(Guid orgUserId, CancellationToken ct = default) =>
        db.AdoptableAnimals.CountAsync(a => a.OrganizationUserId == orgUserId, ct);

    public async Task<(IReadOnlyList<AdoptablePet> Items, int Total)> GetAvailablePagedAsync(
        PetSpecies? species, PetSize? size, AgeCategory? ageCategory,
        bool? isVaccinated, bool? isSterilized, bool? okWithKids, bool? okWithDogs,
        double? nearLat, double? nearLng, int radiusKm,
        int skip, int take, CancellationToken ct = default)
    {
        var q = db.AdoptableAnimals.AsNoTracking()
            .Where(a => a.Status == AdoptionStatus.Available);

        if (species.HasValue)    q = q.Where(a => a.Species == species.Value);
        if (size.HasValue)       q = q.Where(a => a.Size == size.Value);
        if (ageCategory.HasValue)q = q.Where(a => a.AgeCategory == ageCategory.Value);
        if (isVaccinated == true)q = q.Where(a => a.IsVaccinated);
        if (isSterilized == true)q = q.Where(a => a.IsSterilized);
        if (okWithKids == true)  q = q.Where(a => a.OkWithKids);
        if (okWithDogs == true)  q = q.Where(a => a.OkWithDogs);

        // Filtro geográfico en DB si hay coordenadas (Haversine aproximado en SQL)
        if (nearLat.HasValue && nearLng.HasValue)
        {
            var latDelta = radiusKm / 111.0;
            var lngDelta = radiusKm / (111.0 * Math.Cos(nearLat.Value * Math.PI / 180.0));
            q = q.Where(a =>
                a.RefLat >= nearLat.Value - latDelta && a.RefLat <= nearLat.Value + latDelta &&
                a.RefLng >= nearLng.Value - lngDelta && a.RefLng <= nearLng.Value + lngDelta);
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.PublishedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<AdoptablePet>> GetAvailableAllAsync(CancellationToken ct = default) =>
        await db.AdoptableAnimals.AsNoTracking()
            .Where(a => a.Status == AdoptionStatus.Available)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(ct);

    public async Task AddAnimalAsync(AdoptablePet animal, CancellationToken ct = default) =>
        await db.AdoptableAnimals.AddAsync(animal, ct);

    public void UpdateAnimal(AdoptablePet animal) => db.AdoptableAnimals.Update(animal);

    // Applications

    public Task<AdoptionApplication?> GetApplicationByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AdoptionApplications.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<AdoptionApplication?> GetApplicationByApplicantAndAnimalAsync(
        Guid applicantUserId, Guid animalId, CancellationToken ct = default) =>
        db.AdoptionApplications.FirstOrDefaultAsync(
            a => a.ApplicantUserId == applicantUserId && a.AdoptablePetId == animalId, ct);

    public async Task<IReadOnlyList<AdoptionApplication>> GetApplicationsByAnimalAsync(
        Guid animalId, CancellationToken ct = default) =>
        await db.AdoptionApplications.AsNoTracking()
            .Where(a => a.AdoptablePetId == animalId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AdoptionApplication>> GetApplicationsByApplicantAsync(
        Guid applicantUserId, int skip, int take, CancellationToken ct = default) =>
        await db.AdoptionApplications.AsNoTracking()
            .Where(a => a.ApplicantUserId == applicantUserId)
            .OrderByDescending(a => a.AppliedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountApplicationsByApplicantAsync(Guid applicantUserId, CancellationToken ct = default) =>
        db.AdoptionApplications.CountAsync(a => a.ApplicantUserId == applicantUserId, ct);

    public async Task AddApplicationAsync(AdoptionApplication application, CancellationToken ct = default) =>
        await db.AdoptionApplications.AddAsync(application, ct);

    public void UpdateApplication(AdoptionApplication application) =>
        db.AdoptionApplications.Update(application);

    // Fairs

    public Task<AdoptionFair?> GetFairByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AdoptionFairs.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<AdoptionFair>> GetUpcomingFairsAsync(
        double? nearLat, double? nearLng, int radiusKm, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var q = db.AdoptionFairs.AsNoTracking()
            .Where(f => f.Status != FairStatus.Cancelled && f.EndsAt > now);

        if (nearLat.HasValue && nearLng.HasValue)
        {
            var latDelta = radiusKm / 111.0;
            var lngDelta = radiusKm / (111.0 * Math.Cos(nearLat.Value * Math.PI / 180.0));
            q = q.Where(f =>
                f.Lat >= nearLat.Value - latDelta && f.Lat <= nearLat.Value + latDelta &&
                f.Lng >= nearLng.Value - lngDelta && f.Lng <= nearLng.Value + lngDelta);
        }

        return await q.OrderBy(f => f.StartsAt).ToListAsync(ct);
    }

    public async Task AddFairAsync(AdoptionFair fair, CancellationToken ct = default) =>
        await db.AdoptionFairs.AddAsync(fair, ct);

    public void UpdateFair(AdoptionFair fair) => db.AdoptionFairs.Update(fair);
}
```

### 8.3 Añadir DbSets al contexto

En `PawTrackDbContext.cs`, añadir después de la línea de `Billboards`:

```csharp
// AGREGAR a PawTrackDbContext después de los DbSets existentes:
public DbSet<AdoptablePet> AdoptableAnimals => Set<AdoptablePet>();
public DbSet<AdoptionApplication> AdoptionApplications => Set<AdoptionApplication>();
public DbSet<AdoptionFair> AdoptionFairs => Set<AdoptionFair>();
```

Añadir el `using` requerido en la parte superior:

```csharp
using PawTrack.Domain.Adoptions;
```

---

## 9. API Controller

Crear: `backend/src/PawTrack.API/Controllers/AdoptionsController.cs`

```csharp
// backend/src/PawTrack.API/Controllers/AdoptionsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/adoptions")]
public sealed class AdoptionsController(ISender sender, IBlobStorageService blobStorage) : ControllerBase
{
    // ── Public endpoints ──────────────────────────────────────────────────────

    [HttpGet("animals")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAnimals(
        [FromQuery] PetSpecies? species,
        [FromQuery] PetSize? size,
        [FromQuery] AgeCategory? ageCategory,
        [FromQuery] bool? isVaccinated,
        [FromQuery] bool? isSterilized,
        [FromQuery] bool? okWithKids,
        [FromQuery] bool? okWithDogs,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] int? radiusKm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await sender.Send(new GetAdoptablePetsQuery(
            species, size, ageCategory, isVaccinated, isSterilized,
            okWithKids, okWithDogs, lat, lng, radiusKm, page, pageSize),
            cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("animals/map")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAnimalsForMap(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAdoptablePetsQuery(
            null, null, null, null, null, null, null, null, null, null, 1, 500),
            cancellationToken);
        return Ok(result.Value?.Items);
    }

    [HttpGet("animals/{id:guid}")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAnimal(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAdoptablePetByIdQuery(id), cancellationToken);
        if (result.IsFailure) return NotFound();
        return Ok(result.Value);
    }

    [HttpGet("fairs")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetFairs(
        [FromQuery] double? lat, [FromQuery] double? lng,
        [FromQuery] int? radiusKm, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUpcomingFairsQuery(lat, lng, radiusKm), cancellationToken);
        return Ok(result.Value);
    }

    // ── Authenticated — Owner (aplicar) ───────────────────────────────────────

    [HttpPost("animals/{id:guid}/apply")]
    [Authorize(Roles = "Owner")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Apply(
        Guid id,
        [FromBody] ApplyToAdoptRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new ApplyToAdoptCommand(userId, id, request.Note), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Apply failed", Detail = string.Join("; ", result.Errors), Status = 400 });
        return Created($"/api/adoptions/applications/{result.Value!.Id}", result.Value);
    }

    [HttpGet("applications/mine")]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMyApplications(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyAdoptionApplicationsQuery(userId, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    // ── Authenticated — Ally Shelter (gestionar) ──────────────────────────────

    [HttpPost("animals")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(16_384)] // 16 KB para texto del perfil del animal
    public async Task<IActionResult> Publish(
        [FromBody] PublishAdoptablePetRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new PublishAdoptablePetCommand(
            userId, request.Name, request.Species, request.Size, request.AgeCategory,
            request.Story, request.RefLat, request.RefLng, request.RefLabel,
            request.Breed, request.AgeMonthsApprox, request.Requirements,
            request.MedicalNotes, request.IsVaccinated, request.IsSterilized,
            request.IsMicrochipped, request.OkWithKids, request.OkWithDogs,
            request.OkWithCats, request.NeedsYard), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Publish failed", Detail = string.Join("; ", result.Errors), Status = 400 });
        return Created($"/api/adoptions/animals/{result.Value!.Id}", result.Value);
    }

    [HttpPost("animals/{id:guid}/photos")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    public async Task<IActionResult> UploadPhoto(
        Guid id, IFormFile photo, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (photo is null || photo.Length == 0) return BadRequest("No file");

        var result = await sender.Send(new UploadAdoptionPhotoCommand(userId, id, photo), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Upload failed", Detail = string.Join("; ", result.Errors), Status = 400 });
        return Ok(new { photoUrl = result.Value });
    }

    [HttpGet("animals/mine")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetMyAdoptionAnimalsQuery(userId, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("animals/{id:guid}/applications")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetApplications(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetApplicationsForAnimalQuery(userId, id), cancellationToken);
        if (result.IsFailure) return Forbid();
        return Ok(result.Value);
    }

    [HttpPatch("applications/{applicationId:guid}/review")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Review(
        Guid applicationId,
        [FromBody] ReviewApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ReviewAdoptionApplicationCommand(
            userId, applicationId, request.Approve, request.ReviewNote), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Review failed", Detail = string.Join("; ", result.Errors), Status = 400 });
        return Ok(result.Value);
    }

    [HttpPatch("animals/{id:guid}/mark-adopted")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> MarkAdopted(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new MarkAdoptedCommand(userId, id), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Mark adopted failed", Detail = string.Join("; ", result.Errors), Status = 400 });
        return Ok(result.Value);
    }

    [HttpPost("fairs")]
    [Authorize(Roles = "Ally")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(8192)]
    public async Task<IActionResult> CreateFair(
        [FromBody] CreateAdoptionFairRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CreateAdoptionFairCommand(
            userId, request.Title, request.VenueLabel, request.Lat, request.Lng,
            request.StartsAt, request.EndsAt, request.Description,
            request.AnimalIds ?? []), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Create fair failed", Detail = string.Join("; ", result.Errors), Status = 400 });
        return Created($"/api/adoptions/fairs/{result.Value!.Id}", result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }
}

// ── Request body types ────────────────────────────────────────────────────────

public sealed record ApplyToAdoptRequest(string Note);
public sealed record ReviewApplicationRequest(bool Approve, string? ReviewNote);
public sealed record PublishAdoptablePetRequest(
    string Name, PetSpecies Species, PetSize Size, AgeCategory AgeCategory,
    string Story, double RefLat, double RefLng, string? RefLabel,
    string? Breed, int? AgeMonthsApprox, string? Requirements, string? MedicalNotes,
    bool IsVaccinated, bool IsSterilized, bool IsMicrochipped,
    bool OkWithKids, bool OkWithDogs, bool OkWithCats, bool NeedsYard);
public sealed record CreateAdoptionFairRequest(
    string Title, string VenueLabel, double Lat, double Lng,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    string? Description, IReadOnlyList<Guid>? AnimalIds);
```

---

## 10. Frontend — páginas, hooks y API client

### 10.1 Tipos TypeScript

Crear: `frontend/src/features/adoptions/api/adoptionsApi.ts`

```typescript
// frontend/src/features/adoptions/api/adoptionsApi.ts
import { apiClient } from "@/shared/lib/apiClient";

export type PetSpecies = "Dog" | "Cat" | "Bird" | "Rabbit" | "Other";
export type PetSize = "XSmall" | "Small" | "Medium" | "Large" | "XLarge";
export type AgeCategory = "Puppy" | "Young" | "Adult" | "Senior";
export type AdoptionStatus = "Available" | "InProcess" | "Adopted" | "Paused" | "Removed";
export type ApplicationStatus = "Pending" | "UnderReview" | "Approved" | "Rejected" | "Withdrawn";
export type FairStatus = "Upcoming" | "Active" | "Finished" | "Cancelled";

export interface AdoptablePetDto {
  id: string;
  organizationUserId: string;
  organizationName: string;
  name: string;
  species: PetSpecies;
  breed: string | null;
  size: PetSize;
  ageCategory: AgeCategory;
  ageMonthsApprox: number | null;
  story: string;
  requirements: string | null;
  medicalNotes: string | null;
  isVaccinated: boolean;
  isSterilized: boolean;
  isMicrochipped: boolean;
  okWithKids: boolean;
  okWithDogs: boolean;
  okWithCats: boolean;
  needsYard: boolean;
  refLat: number;
  refLng: number;
  refLabel: string | null;
  status: AdoptionStatus;
  photoUrls: string[];
  publishedAt: string;
}

export interface AdoptionApplicationDto {
  id: string;
  adoptablePetId: string;
  applicantUserId: string;
  applicantNote: string;
  status: ApplicationStatus;
  reviewNote: string | null;
  appliedAt: string;
  reviewedAt: string | null;
}

export interface AdoptionFairDto {
  id: string;
  organizationUserId: string;
  title: string;
  description: string | null;
  venueLabel: string;
  lat: number;
  lng: number;
  startsAt: string;
  endsAt: string;
  status: FairStatus;
  animalIds: string[];
}

export interface AdoptionFilters {
  species?: PetSpecies;
  size?: PetSize;
  ageCategory?: AgeCategory;
  isVaccinated?: boolean;
  isSterilized?: boolean;
  okWithKids?: boolean;
  okWithDogs?: boolean;
  lat?: number;
  lng?: number;
  radiusKm?: number;
  page?: number;
  pageSize?: number;
}

export interface PagedAdoptions {
  items: AdoptablePetDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export const adoptionsApi = {
  getAnimals: (filters: AdoptionFilters = {}) =>
    apiClient.get<PagedAdoptions>("/adoptions/animals", { params: filters }).then(r => r.data),

  getAnimalsForMap: () =>
    apiClient.get<AdoptablePetDto[]>("/adoptions/animals/map").then(r => r.data),

  getAnimal: (id: string) =>
    apiClient.get<AdoptablePetDto>(`/adoptions/animals/${id}`).then(r => r.data),

  publishAnimal: (data: Omit<AdoptablePetDto, "id" | "organizationUserId" | "organizationName" | "status" | "photoUrls" | "publishedAt">) =>
    apiClient.post<AdoptablePetDto>("/adoptions/animals", data).then(r => r.data),

  uploadPhoto: (animalId: string, file: File) => {
    const form = new FormData();
    form.append("photo", file);
    return apiClient.post<{ photoUrl: string }>(`/adoptions/animals/${animalId}/photos`, form, {
      headers: { "Content-Type": "multipart/form-data" },
    }).then(r => r.data);
  },

  getMyAnimals: (page = 1, pageSize = 20) =>
    apiClient.get<PagedAdoptions>("/adoptions/animals/mine", { params: { page, pageSize } }).then(r => r.data),

  applyToAdopt: (animalId: string, note: string) =>
    apiClient.post<AdoptionApplicationDto>(`/adoptions/animals/${animalId}/apply`, { note }).then(r => r.data),

  getApplicationsForAnimal: (animalId: string) =>
    apiClient.get<AdoptionApplicationDto[]>(`/adoptions/animals/${animalId}/applications`).then(r => r.data),

  reviewApplication: (applicationId: string, approve: boolean, reviewNote?: string) =>
    apiClient.patch<AdoptionApplicationDto>(`/adoptions/applications/${applicationId}/review`,
      { approve, reviewNote }).then(r => r.data),

  markAdopted: (animalId: string) =>
    apiClient.patch<AdoptablePetDto>(`/adoptions/animals/${animalId}/mark-adopted`).then(r => r.data),

  getMyApplications: (page = 1, pageSize = 20) =>
    apiClient.get<PagedAdoptions>("/adoptions/applications/mine", { params: { page, pageSize } }).then(r => r.data),

  getFairs: (lat?: number, lng?: number, radiusKm?: number) =>
    apiClient.get<AdoptionFairDto[]>("/adoptions/fairs", { params: { lat, lng, radiusKm } }).then(r => r.data),

  createFair: (data: Omit<AdoptionFairDto, "id" | "organizationUserId" | "status">) =>
    apiClient.post<AdoptionFairDto>("/adoptions/fairs", data).then(r => r.data),
};
```

### 10.2 React Query Hooks

Crear: `frontend/src/features/adoptions/hooks/useAdoptions.ts`

```typescript
// frontend/src/features/adoptions/hooks/useAdoptions.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { adoptionsApi, type AdoptionFilters } from "../api/adoptionsApi";

export function useAdoptableAnimals(filters: AdoptionFilters = {}) {
  return useQuery({
    queryKey: ["adoptions", "animals", filters],
    queryFn: () => adoptionsApi.getAnimals(filters),
    staleTime: 3 * 60_000,
  });
}

export function useAdoptableAnimalsForMap() {
  return useQuery({
    queryKey: ["adoptions", "animals", "map"],
    queryFn: adoptionsApi.getAnimalsForMap,
    staleTime: 5 * 60_000,
  });
}

export function useAdoptableAnimal(id: string, enabled = true) {
  return useQuery({
    queryKey: ["adoptions", "animals", id],
    queryFn: () => adoptionsApi.getAnimal(id),
    enabled: !!id && enabled,
    staleTime: 2 * 60_000,
  });
}

export function useMyAdoptionAnimals(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["adoptions", "mine", page, pageSize],
    queryFn: () => adoptionsApi.getMyAnimals(page, pageSize),
    staleTime: 60_000,
  });
}

export function usePublishAnimal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: adoptionsApi.publishAnimal,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["adoptions", "mine"] }),
  });
}

export function useUploadAdoptionPhoto() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ animalId, file }: { animalId: string; file: File }) =>
      adoptionsApi.uploadPhoto(animalId, file),
    onSuccess: (_data, vars) =>
      void qc.invalidateQueries({ queryKey: ["adoptions", "animals", vars.animalId] }),
  });
}

export function useApplyToAdopt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ animalId, note }: { animalId: string; note: string }) =>
      adoptionsApi.applyToAdopt(animalId, note),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["adoptions", "applications", "mine"] }),
  });
}

export function useApplicationsForAnimal(animalId: string, enabled = true) {
  return useQuery({
    queryKey: ["adoptions", "applications", animalId],
    queryFn: () => adoptionsApi.getApplicationsForAnimal(animalId),
    enabled: !!animalId && enabled,
    staleTime: 30_000,
  });
}

export function useReviewApplication() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ applicationId, approve, reviewNote }: { applicationId: string; approve: boolean; reviewNote?: string }) =>
      adoptionsApi.reviewApplication(applicationId, approve, reviewNote),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["adoptions", "applications"] }),
  });
}

export function useMarkAdopted() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: adoptionsApi.markAdopted,
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["adoptions", "mine"] });
      void qc.invalidateQueries({ queryKey: ["adoptions", "animals"] });
    },
  });
}

export function useMyAdoptionApplications(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["adoptions", "applications", "mine", page],
    queryFn: () => adoptionsApi.getMyApplications(page, pageSize),
    staleTime: 60_000,
  });
}

export function useUpcomingFairs(lat?: number, lng?: number, radiusKm?: number) {
  return useQuery({
    queryKey: ["adoptions", "fairs", lat, lng, radiusKm],
    queryFn: () => adoptionsApi.getFairs(lat, lng, radiusKm),
    staleTime: 5 * 60_000,
  });
}

export function useCreateFair() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: adoptionsApi.createFair,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["adoptions", "fairs"] }),
  });
}
```

### 10.3 Estructura de páginas y rutas

```
frontend/src/features/adoptions/
  api/
    adoptionsApi.ts
  hooks/
    useAdoptions.ts
  pages/
    AdoptionDirectoryPage.tsx     -- público: listado con filtros
    AdoptionDetailPage.tsx        -- público: perfil del animal
    AdoptionFairsPage.tsx         -- público: ferias y eventos
    ShelterDashboardPage.tsx      -- Ally Shelter: gestión de animales
    ShelterPublishPage.tsx        -- Ally Shelter: publicar nuevo animal
    ShelterApplicationsPage.tsx   -- Ally Shelter: ver aplicaciones
    MyAdoptionApplicationsPage.tsx -- Owner: mis solicitudes
  components/
    AnimalCard.tsx                -- card reutilizable para animal en adopción
    AnimalDetailSheet.tsx         -- drawer lateral (patrón StoreDetailSheet)
    AdoptionMarker.tsx            -- pin del mapa (patrón StoreMarker)
    ApplyDrawer.tsx               -- formulario de solicitud en drawer
    FairCard.tsx                  -- card de feria de adopción
    AdoptionFiltersBar.tsx        -- filtros reutilizables
```

### 10.4 Rutas a agregar en `routes.tsx`

```typescript
// Añadir al archivo frontend/src/app/routes.tsx:

// Adopciones — público
const AdoptionDirectoryPage = lazy(() => import("@/features/adoptions/pages/AdoptionDirectoryPage"));
const AdoptionDetailPage    = lazy(() => import("@/features/adoptions/pages/AdoptionDetailPage"));
const AdoptionFairsPage     = lazy(() => import("@/features/adoptions/pages/AdoptionFairsPage"));

// Adopciones — Owner
const MyAdoptionApplicationsPage = lazy(() => import("@/features/adoptions/pages/MyAdoptionApplicationsPage"));

// Adopciones — Ally Shelter
const ShelterDashboardPage    = lazy(() => import("@/features/adoptions/pages/ShelterDashboardPage"));
const ShelterPublishPage      = lazy(() => import("@/features/adoptions/pages/ShelterPublishPage"));
const ShelterApplicationsPage = lazy(() => import("@/features/adoptions/pages/ShelterApplicationsPage"));

// En el array de rutas (PublicLayout):
{ path: "adopciones", element: <S><AdoptionDirectoryPage /></S> },
{ path: "adopciones/ferias", element: <S><AdoptionFairsPage /></S> },
{ path: "adopciones/:id", element: <S><AdoptionDetailPage /></S> },

// En AuthenticatedLayout (Owner):
{ path: "mis-adopciones", element: <S><MyAdoptionApplicationsPage /></S> },

// En AuthenticatedLayout con RoleGuard Ally:
{ path: "shelter/dashboard", element: <RoleGuard role="Ally"><S><ShelterDashboardPage /></S></RoleGuard> },
{ path: "shelter/publicar", element: <RoleGuard role="Ally"><S><ShelterPublishPage /></S></RoleGuard> },
{ path: "shelter/animales/:id/aplicaciones", element: <RoleGuard role="Ally"><S><ShelterApplicationsPage /></S></RoleGuard> },
```

---

## 11. Notificaciones — extensiones requeridas

### 11.1 `NotificationType` — agregar al enum existente

```csharp
// backend/src/PawTrack.Domain/Notifications/NotificationType.cs — AGREGAR:
AdoptionInterest,  // shelter recibe: alguien aplicó
AdoptionApproved,  // adoptante recibe: aplicación aprobada
AdoptionRejected,  // adoptante recibe: aplicación rechazada
AdoptionFairAlert, // usuarios cercanos: feria en la zona
```

### 11.2 `INotificationDispatcher` — agregar métodos

```csharp
// backend/src/PawTrack.Application/Common/Interfaces/INotificationDispatcher.cs — AGREGAR:

/// <summary>Notifica al shelter que un usuario aplicó para adoptar un animal.</summary>
Task DispatchAdoptionInterestAsync(
    Guid shelterUserId,
    string animalName,
    Guid applicationId,
    CancellationToken cancellationToken = default);

/// <summary>Notifica al solicitante que su aplicación fue aprobada.</summary>
Task DispatchAdoptionApprovedAsync(
    Guid applicantUserId,
    string animalName,
    Guid applicationId,
    CancellationToken cancellationToken = default);

/// <summary>Notifica al solicitante que su aplicación fue rechazada.</summary>
Task DispatchAdoptionRejectedAsync(
    Guid applicantUserId,
    string animalName,
    Guid applicationId,
    CancellationToken cancellationToken = default);

/// <summary>
/// Envía alertas geofenceadas (radio configurable) a usuarios con
/// push subscriptions activas cuando se crea una feria de adopción.
/// </summary>
Task DispatchAdoptionFairAlertAsync(
    Guid fairId,
    string fairTitle,
    double fairLat,
    double fairLng,
    int radiusMetres,
    DateTimeOffset fairStartsAt,
    CancellationToken cancellationToken = default);
```

---

## 12. WhatsApp Bot — intents de adopción

En `HandleWhatsAppWebhookCommandHandler.cs`, añadir al switch de intents:

```csharp
// AGREGAR al switch en HandleWhatsAppWebhookCommandHandler:

case "buscar_adopcion":
case "quiero_adoptar":
    // Responder con link al directorio de adopciones
    await whatsAppSender.SendTextAsync(from,
        "🐾 *PawTrack CR — Adopciones*\n\n" +
        "Visita nuestro directorio de animales en adopción:\n" +
        $"{publicUrlProvider.GetBaseUrl()}/adopciones\n\n" +
        "También puedes ver los próximos eventos de adopción:\n" +
        $"{publicUrlProvider.GetBaseUrl()}/adopciones/ferias", ct);
    break;

case "tengo_animales":
case "quiero_dar_en_adopcion":
    await whatsAppSender.SendTextAsync(from,
        "🏠 *Para dar animales en adopción*\n\n" +
        "Tu organización necesita:\n" +
        "1️⃣ Registrarse como Aliado Verificado (Shelter)\n" +
        "2️⃣ Ingresar al panel del aliado\n" +
        "3️⃣ Publicar el perfil del animal con fotos\n\n" +
        $"Regístrate aquí: {publicUrlProvider.GetBaseUrl()}/aliados/registro", ct);
    break;
```

---

## 13. Migraciones EF Core

```bash
# Desde el directorio backend/
dotnet ef migrations add AddAdoptionsModule \
  --project src/PawTrack.Infrastructure \
  --startup-project src/PawTrack.API \
  --output-dir Migrations

# Verificar que genera las 3 tablas:
# - AdoptableAnimals
# - AdoptionApplications
# - AdoptionFairs

# Aplicar en desarrollo:
dotnet ef database update \
  --project src/PawTrack.Infrastructure \
  --startup-project src/PawTrack.API
```

**Tablas que genera la migración:**

| Tabla | PK | Índices clave |
|---|---|---|
| `AdoptableAnimals` | `Id` (uniqueidentifier) | `OrganizationUserId`, `(Species, Status)` |
| `AdoptionApplications` | `Id` (uniqueidentifier) | `AdoptablePetId`, `ApplicantUserId`, `(ApplicantUserId, AdoptablePetId)` unique |
| `AdoptionFairs` | `Id` (uniqueidentifier) | `OrganizationUserId`, `StartsAt` |

---

## 14. DI Registration

### 14.1 `InfrastructureServiceCollectionExtensions.cs`

```csharp
// AGREGAR en el bloque de Repositories (tras los Store repos):
services.AddScoped<IAdoptionRepository, PawTrack.Infrastructure.Adoptions.AdoptionRepository>();
```

### 14.2 `AllyProfileRepository.cs`

```csharp
// AGREGAR el método GetByUserIdsAsync al repositorio existente:
public async Task<IReadOnlyList<AllyProfile>> GetByUserIdsAsync(
    IEnumerable<Guid> userIds, CancellationToken ct = default)
{
    var ids = userIds.ToList();
    return await db.AllyProfiles.AsNoTracking()
        .Where(a => ids.Contains(a.UserId))
        .ToListAsync(ct);
}
```

---

## 15. Tests requeridos

### 15.1 Domain tests

Crear: `backend/tests/PawTrack.UnitTests/Adoptions/AdoptablePetTests.cs`

```csharp
using FluentAssertions;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Adoptions;

public sealed class AdoptablePetTests
{
    private static AdoptablePet MakeAnimal() =>
        AdoptablePet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog,
            PetSize.Medium, AgeCategory.Young, "Es muy juguetón",
            9.93, -84.08, "San José");

    [Fact]
    public void NewAnimal_HasAvailableStatus()
    {
        var animal = MakeAnimal();
        animal.Status.Should().Be(AdoptionStatus.Available);
    }

    [Fact]
    public void AddPhoto_BeyondLimit_Throws()
    {
        var animal = MakeAnimal();
        for (var i = 0; i < 5; i++) animal.AddPhoto($"https://blob/photo{i}.jpg");
        var act = () => animal.AddPhoto("https://blob/extra.jpg");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAdopted_SetsStatusAndTimestamp()
    {
        var animal = MakeAnimal();
        animal.MarkInProcess();
        animal.MarkAdopted();
        animal.Status.Should().Be(AdoptionStatus.Adopted);
        animal.AdoptedAt.Should().NotBeNull();
    }

    [Fact]
    public void AdoptionFair_EndsBeforeStarts_Throws()
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => AdoptionFair.Create(Guid.NewGuid(), "Feria",
            "Parque La Sabana", 9.93, -84.08,
            startsAt: now.AddDays(2), endsAt: now.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }
}
```

### 15.2 Command handler tests

Crear: `backend/tests/PawTrack.UnitTests/Adoptions/PublishAdoptablePetCommandHandlerTests.cs`

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Adoptions;

public sealed class PublishAdoptablePetCommandHandlerTests
{
    private readonly IAllyProfileRepository _allies = Substitute.For<IAllyProfileRepository>();
    private readonly IAdoptionRepository _adoptions = Substitute.For<IAdoptionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private PublishAdoptablePetCommandHandler BuildHandler() => new(
        _allies, _adoptions, _uow, NullLogger<PublishAdoptablePetCommandHandler>.Instance);

    private static PublishAdoptablePetCommand MakeCommand(Guid orgUserId) => new(
        orgUserId, "Max", PetSpecies.Dog, PetSize.Medium, AgeCategory.Young,
        "Muy juguetón", 9.93, -84.08, "San José", null, null, null, null,
        false, false, false, false, false, false, false);

    [Fact]
    public async Task Handle_NotVerifiedShelter_ReturnsFailure()
    {
        _allies.GetVerifiedByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AllyProfile?)null);

        var result = await BuildHandler().Handle(MakeCommand(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(PublishAdoptablePetCommandHandler.NotVerifiedShelterError);
    }

    [Fact]
    public async Task Handle_NotShelterType_ReturnsFailure()
    {
        var profile = AllyProfile.Create(Guid.NewGuid(), "Org", AllyType.VeterinaryClinic,
            "San José", 9.93, -84.08, 5000);
        profile.Approve();
        _allies.GetVerifiedByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await BuildHandler().Handle(MakeCommand(profile.UserId), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidShelter_PublishesAnimal()
    {
        var userId = Guid.NewGuid();
        var profile = AllyProfile.Create(userId, "Refugio Esperanza",
            AllyType.Shelter, "San José", 9.93, -84.08, 5000);
        profile.Approve();
        _allies.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await BuildHandler().Handle(MakeCommand(userId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrganizationName.Should().Be("Refugio Esperanza");
        result.Value.Status.Should().Be("Available");
        await _adoptions.Received(1).AddAnimalAsync(Arg.Any<PawTrack.Domain.Adoptions.AdoptablePet>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

---

## 16. Monetización y planes

### Nuevo tier en `SubscriptionTier`

```csharp
// AGREGAR al enum SubscriptionTier:
ShelterBasic  = 300, // gratis — publicar hasta 5 animales, sin ferias
ShelterPlus   = 310, // ₡8,000/mes — ilimitado + ferias + pin destacado en mapa
```

| Plan | Precio | Incluye |
|---|---|---|
| `ShelterBasic` | Gratis | Hasta 5 animales activos, perfil en directorio, sin ferias |
| `ShelterPlus` | ₡8,000/mes | Animales ilimitados, crear ferias de adopción, pin destacado en mapa, estadísticas de visitas |

**Lógica de gating** (en handler `PublishAdoptablePetCommand`):

```csharp
// Comprobar límite de animales para ShelterBasic:
var activeCount = await adoptionRepository.CountByOrganizationAsync(request.OrganizationUserId, ct);
var hasPlusPlan = subscriptionService.IsActive(request.OrganizationUserId, SubscriptionTier.ShelterPlus);
if (!hasPlusPlan && activeCount >= 5)
    return Result.Failure<AdoptablePetDto>("shelter_basic_limit_reached");
```

---

## 17. Roadmap de sprints

### Sprint 1 — Core (2 semanas)
**Backend:**
- [ ] Crear archivos domain: `AdoptablePet.cs`, `AdoptionApplication.cs`, `AdoptionFair.cs`
- [ ] Extender `NotificationType` con 4 nuevos valores
- [ ] Crear `IAdoptionRepository` interface
- [ ] Crear `AdoptionCommands.cs` (Publish, Apply, Review, MarkAdopted)
- [ ] Crear `AdoptionQueries.cs` (GetAnimals paginado, GetMine)
- [ ] Crear `AdoptionRepository.cs` + `AdoptionConfiguration.cs`
- [ ] Registrar en DbContext + DI
- [ ] Ejecutar migración `AddAdoptionsModule`
- [ ] Crear `AdoptionsController.cs` (CRUD básico)
- [ ] Extender `IAllyProfileRepository.GetByUserIdsAsync`

**Frontend:**
- [ ] Crear `adoptionsApi.ts` con todos los tipos TypeScript
- [ ] Crear `useAdoptions.ts` con hooks básicos
- [ ] `AdoptionDirectoryPage.tsx` — listado público con paginación
- [ ] `AdoptionDetailPage.tsx` — perfil del animal
- [ ] `AnimalCard.tsx` — card reutilizable
- [ ] Añadir rutas en `routes.tsx`

**Tests:**
- [ ] `AdoptablePetTests.cs` — domain tests (estado, fotos, ferias)
- [ ] `PublishAdoptablePetCommandHandlerTests.cs`
- [ ] `ApplyToAdoptCommandHandlerTests.cs`

### Sprint 2 — Upload de fotos y gestión de aplicaciones (1 semana)
- [ ] `UploadAdoptionPhotoCommand` handler (blob storage)
- [ ] `GetApplicationsForAnimalQuery` + `ReviewAdoptionApplicationCommand`
- [ ] Extender `INotificationDispatcher` con los 4 métodos de adopción
- [ ] Implementar los dispatch en `NotificationDispatcher` (emails + push)
- [ ] `ShelterDashboardPage.tsx` + `ShelterPublishPage.tsx`
- [ ] `ShelterApplicationsPage.tsx`
- [ ] `MyAdoptionApplicationsPage.tsx`
- [ ] `ApplyDrawer.tsx` con formulario

### Sprint 3 — Ferias y mapa (1 semana)
- [ ] `CreateAdoptionFairCommand` handler + geofence alert
- [ ] `GetUpcomingFairsQuery`
- [ ] Extender `PublicMapPage.tsx` con pins de animales en adopción
- [ ] `AdoptionMarker.tsx` (pin especial para adopciones)
- [ ] `AdoptionFairsPage.tsx`
- [ ] `FairCard.tsx`
- [ ] Integrar `DispatchAdoptionFairAlertAsync` en `NotificationDispatcher`

### Sprint 4 — Monetización y WhatsApp (1 semana)
- [ ] Añadir `ShelterBasic` / `ShelterPlus` a `SubscriptionTier`
- [ ] Gating de límite de animales en `PublishAdoptablePetCommand`
- [ ] Intents de adopción en `HandleWhatsAppWebhookCommandHandler`
- [ ] `BillboardBanner placement="Adoption"` (nuevo placement en `BillboardPlacement`)
- [ ] Tests de integración para el flujo completo: publish → apply → review → mark-adopted

---

## Resumen de archivos a crear/modificar

### Crear nuevos
| Archivo | Tipo |
|---|---|
| `Domain/Adoptions/AdoptablePet.cs` | Domain entity |
| `Domain/Adoptions/AdoptionApplication.cs` | Domain entity |
| `Domain/Adoptions/AdoptionFair.cs` | Domain entity |
| `Application/Adoptions/AdoptionCommands.cs` | CQRS commands |
| `Application/Adoptions/AdoptionQueries.cs` | CQRS queries |
| `Application/Common/Interfaces/IAdoptionRepository.cs` | Interface |
| `Infrastructure/Adoptions/AdoptionRepository.cs` | EF Core repo |
| `Infrastructure/Adoptions/AdoptionConfiguration.cs` | EF Core config |
| `API/Controllers/AdoptionsController.cs` | REST controller |
| `frontend/features/adoptions/api/adoptionsApi.ts` | API client |
| `frontend/features/adoptions/hooks/useAdoptions.ts` | React hooks |
| `frontend/features/adoptions/pages/*.tsx` | 7 páginas |
| `frontend/features/adoptions/components/*.tsx` | 6 componentes |
| `tests/UnitTests/Adoptions/*.cs` | 3+ test files |

### Modificar existentes
| Archivo | Cambio |
|---|---|
| `Domain/Notifications/NotificationType.cs` | +4 valores al enum |
| `Domain/Subscriptions/SubscriptionTier.cs` | +2 valores al enum |
| `Infrastructure/Persistence/PawTrackDbContext.cs` | +3 DbSets |
| `Application/Common/Interfaces/INotificationDispatcher.cs` | +4 métodos |
| `Application/Common/Interfaces/IAllyProfileRepository.cs` | +1 método |
| `Infrastructure/Allies/AllyProfileRepository.cs` | Implementar GetByUserIdsAsync |
| `Infrastructure/InfrastructureServiceCollectionExtensions.cs` | Registrar IAdoptionRepository |
| `Application/Bot/HandleWhatsAppWebhookCommandHandler.cs` | +2 intents |
| `frontend/app/routes.tsx` | +7 rutas |
