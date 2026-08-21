# PawTrack CR — Módulo de Adopciones: Especificación Técnica Completa

> **Versión:** 1.0 | **Fecha:** 2026-08-20  
> **Estado:** Propuesta de implementación — listo para sprint  
> **Audiencia:** Desarrolladores, PO, diseñadores  
> **Repositorio:** `C:\Nala` | Stack: .NET 9 · React 19 · Azure

---

## Tabla de contenidos

1. [Contexto y justificación](#1-contexto-y-justificación)
2. [Casos de uso principales](#2-casos-de-uso-principales)
3. [Roles y actores](#3-roles-y-actores)
4. [Infraestructura existente reutilizable](#4-infraestructura-existente-reutilizable)
5. [Domain model](#5-domain-model)
6. [API — endpoints requeridos](#6-api--endpoints-requeridos)
7. [Application layer — Commands y Queries](#7-application-layer--commands-y-queries)
8. [Infrastructure — Repositorios y Configuración EF Core](#8-infrastructure--repositorios-y-configuración-ef-core)
9. [Frontend — Páginas y componentes](#9-frontend--páginas-y-componentes)
10. [Integración con el mapa](#10-integración-con-el-mapa)
11. [Notificaciones](#11-notificaciones)
12. [WhatsApp bot — extensión](#12-whatsapp-bot--extensión)
13. [Monetización y planes](#13-monetización-y-planes)
14. [Migraciones EF Core](#14-migraciones-ef-core)
15. [Tests requeridos](#15-tests-requeridos)
16. [Roadmap de implementación](#16-roadmap-de-implementación)
17. [Wireframes y UX](#17-wireframes-y-ux)

---

## 1. Contexto y justificación

### El problema

Costa Rica tiene entre **800,000 y 1,200,000 animales en situación de calle**. Las organizaciones de rescate operan con recursos limitados y actualmente gestionan sus campañas de adopción por:

- Grupos de Facebook (no persistentes, sin estructura)
- WhatsApp (masivo e imposible de rastrear)
- Ferias de adopción presenciales sin seguimiento digital

### La oportunidad para PawTrack CR

PawTrack CR ya tiene:

- **Una red de Aliados verificados** (refugios, rescatistas) que son exactamente los actores de adopción
- **Mapa interactivo** visto por dueños de mascotas (audiencia objetivo perfecta)
- **Notificaciones geofenceadas** para alertar por zona
- **Sistema de chat enmascarado** que protege la privacidad del rescatador
- **Perfiles públicos** con QR y foto
- **Bot de WhatsApp** para captura conversacional

El módulo de adopciones no requiere construir nueva infraestructura — es una **capa de features sobre lo que existe**.

---

## 2. Casos de uso principales

### UC-01: Publicar un animal en adopción

Un rescatista/shelter publica un animal con:

- Nombre, especie, raza estimada, edad aproximada, peso
- Foto principal + galería (hasta 5 fotos)
- Historia: cómo llegó, personalidad, necesidades especiales
- Requisitos para adoptantes (patio, niños OK, otros animales, etc.)
- Ubicación de referencia (sin dirección exacta)
- Estado: Disponible / En proceso / Adoptado

### UC-02: Buscar animales en adopción

Cualquier persona (sin cuenta) puede:

- Buscar por especie, tamaño, zona, requisitos
- Ver en mapa los pines de animales disponibles
- Filtrar por "eventos de adopción" (ferias)
- Ver el perfil completo del animal

### UC-03: Registrar interés / Aplicar para adoptar

Un usuario autenticado puede:

- Marcar "Me interesa este animal"
- Completar un formulario de pre-adopción
- Chatear con la organización (canal enmascarado existente)
- Recibir notificaciones de actualizaciones del proceso

### UC-04: Gestionar aplicaciones (organizaciones)

La organización ve:

- Lista de interesados por animal
- Estado de cada proceso
- Historial de adopciones realizadas
- Estadísticas de campaña

### UC-05: Feria de adopción (evento)

Crear un evento temporal con:

- Fecha, hora, lugar (con coordenadas GPS)
- Lista de animales que estarán presentes
- Notificación geofenceada a usuarios en la zona
- Pin especial en el mapa
- QR del evento para compartir

### UC-06: Seguimiento post-adopción

Opcional (fase 2):

- El adoptante puede registrar actualizaciones del animal
- La organización puede hacer un check-in a los 30, 90, 365 días
- Genera datos de éxito para el módulo de incentivos

---

## 3. Roles y actores

| Actor                               | Rol en el sistema | Capacidades de adopción                                     |
| ----------------------------------- | ----------------- | ----------------------------------------------------------- |
| **Visitante anónimo**               | —                 | Ver perfil de animales, ver mapa, buscar                    |
| **Usuario registrado** (Explorador) | Owner             | Marcar interés, chatear, aplicar para adoptar               |
| **Aliado verificado** (Shelter)     | Ally              | Publicar animales, gestionar aplicaciones, crear ferias     |
| **Admin**                           | Admin             | Moderar contenido, estadísticas globales, destacar campañas |

> **Nota clave:** Los **Aliados verificados** ya existen con `AllyType.Shelter`. El módulo de adopciones se construye sobre este rol existente. No requiere un rol nuevo.

---

## 4. Infraestructura existente reutilizable

### 4.1 Backend — 100% reutilizable

| Componente                    | Archivo existente                              | Cómo se reutiliza                                             |
| ----------------------------- | ---------------------------------------------- | ------------------------------------------------------------- |
| `IImageProcessor`             | `Infrastructure/AI/ImageSharpProcessor.cs`     | Resize de fotos de animales                                   |
| `IBlobStorageService`         | `Infrastructure/Storage/BlobStorageService.cs` | Almacenar fotos en `adoption-photos` container                |
| `BlobHelper.SanitizeFileName` | `Application/Common/BlobHelper.cs`             | Sanear nombres de archivos                                    |
| `INotificationDispatcher`     | `Application/Common/Interfaces/`               | Nuevos métodos para alertas de adopción                       |
| `NotificationType`            | `Domain/Notifications/NotificationType.cs`     | Añadir `AdoptionInterest`, `AdoptionApproved`, `AdoptionFair` |
| `IUserLocationRepository`     | Existing                                       | Alertas geofenceadas para ferias de adopción                  |
| `IPiiScrubber`                | Existing                                       | Scrubbing de notas en chat de adopción                        |
| `ChatMessage`                 | `Domain/Chat/ChatMessage.cs`                   | Chat entre adoptante y organización                           |
| `PetSpecies`                  | `Domain/Pets/PetSpecies.cs`                    | Reusar enum de especies                                       |
| `GeoHelper.DistanceMetres`    | Existing                                       | Cálculo de distancia para alertas                             |
| `RateLimiter policies`        | `API/Program.cs`                               | Reusar `public-api` policy                                    |
| `Result<T>`                   | `Domain/Common/`                               | Patrón de errores                                             |
| `FluentValidation pipeline`   | Existing                                       | Validación de comandos                                        |

### 4.2 Frontend — 100% reutilizable

| Componente                | Archivo                                               | Reutilización                           |
| ------------------------- | ----------------------------------------------------- | --------------------------------------- |
| `MapContainer`            | `features/map/components/MapContainer.tsx`            | Añadir prop `adoptions` para pins       |
| `BillboardBanner`         | `features/advertising/components/BillboardBanner.tsx` | Placement `Adoption` futuro             |
| `StoreDetailSheet`        | `features/stores/components/StoreDetailSheet.tsx`     | Patrón para `AnimalDetailSheet`         |
| `CartDrawer` pattern      | Stores feature                                        | Patrón de drawer lateral                |
| `Modal`                   | `shared/ui/Modal.tsx`                                 | Confirmaciones y formularios            |
| `useBillboards` pattern   | Advertising                                           | Patrón para `useAdoptionCampaigns`      |
| `usePublicStores` pattern | Stores                                                | Patrón para `useAdoptablePets`          |
| `useMyOrders` pattern     | Stores                                                | Patrón para `useMyAdoptionApplications` |

---

## 5. Domain model

### 5.1 `AdoptablePet` — el animal en adopción

```csharp
// backend/src/PawTrack.Domain/Adoptions/AdoptablePet.cs

namespace PawTrack.Domain.Adoptions;

public enum PetSize { XSmall, Small, Medium, Large, XLarge }
public enum AdoptionStatus { Available, InProcess, Adopted, Paused, Removed }
public enum AgeCategory { Puppy, Young, Adult, Senior } // < 1y, 1-3y, 3-8y, 8y+

public sealed class AdoptablePet
{
    private AdoptablePet() { } // EF Core
    private readonly List<string> _photoUrls = [];

    public Guid Id { get; private set; }
    /// <summary>FK to the Ally (shelter/rescue) who posted this animal.</summary>
    public Guid OrganizationUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public PetSize Size { get; private set; }
    public AgeCategory AgeCategory { get; private set; }
    public int? AgeMonthsApprox { get; private set; }
    public string? WeightKg { get; private set; }
    public string Story { get; private set; } = string.Empty; // personalidad, historia, necesidades
    public string? Requirements { get; private set; } // requisitos para el adoptante
    public string? MedicalNotes { get; private set; } // vacunas, castrado, chip
    public bool IsVaccinated { get; private set; }
    public bool IsSterilized { get; private set; }
    public bool IsMicrochipped { get; private set; }
    public bool OkWithKids { get; private set; }
    public bool OkWithDogs { get; private set; }
    public bool OkWithCats { get; private set; }
    public string? MainPhotoUrl { get; private set; }
    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();
    public double? LocationLat { get; private set; }
    public double? LocationLng { get; private set; }
    public string? LocationLabel { get; private set; } // canton/barrio, sin dirección exacta
    public AdoptionStatus Status { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? AdoptedAt { get; private set; }

    public static AdoptablePet Create(
        Guid organizationUserId,
        string name,
        PetSpecies species,
        PetSize size,
        AgeCategory ageCategory,
        string story,
        double? locationLat,
        double? locationLng,
        string? locationLabel)
    {
        return new AdoptablePet
        {
            Id = Guid.CreateVersion7(),
            OrganizationUserId = organizationUserId,
            Name = name.Trim(),
            Species = species,
            Size = size,
            AgeCategory = ageCategory,
            Story = story.Trim(),
            LocationLat = locationLat,
            LocationLng = locationLng,
            LocationLabel = locationLabel?.Trim(),
            Status = AdoptionStatus.Available,
            PublishedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetMainPhoto(string url) { MainPhotoUrl = url; UpdatedAt = DateTimeOffset.UtcNow; }
    public void AddPhoto(string url) { if (_photoUrls.Count < 5) { _photoUrls.Add(url); UpdatedAt = DateTimeOffset.UtcNow; } }
    public void RemovePhoto(string url) { _photoUrls.Remove(url); UpdatedAt = DateTimeOffset.UtcNow; }
    public void Pause() { Status = AdoptionStatus.Paused; UpdatedAt = DateTimeOffset.UtcNow; }
    public void MarkInProcess() { Status = AdoptionStatus.InProcess; UpdatedAt = DateTimeOffset.UtcNow; }
    public void MarkAdopted() { Status = AdoptionStatus.Adopted; AdoptedAt = DateTimeOffset.UtcNow; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Restore() { Status = AdoptionStatus.Available; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Remove() { Status = AdoptionStatus.Removed; UpdatedAt = DateTimeOffset.UtcNow; }
    public void UpdateDetails(string name, string story, string? requirements, string? medicalNotes,
        bool vaccinated, bool sterilized, bool microchipped,
        bool okKids, bool okDogs, bool okCats, string? breed, PetSize size,
        AgeCategory age, int? ageMonths, string? weight)
    {
        Name = name.Trim();
        Story = story.Trim();
        Requirements = requirements?.Trim();
        MedicalNotes = medicalNotes?.Trim();
        IsVaccinated = vaccinated; IsSterilized = sterilized; IsMicrochipped = microchipped;
        OkWithKids = okKids; OkWithDogs = okDogs; OkWithCats = okCats;
        Breed = breed?.Trim(); Size = size; AgeCategory = age;
        AgeMonthsApprox = ageMonths; WeightKg = weight?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

### 5.2 `AdoptionApplication` — solicitud de adopción

```csharp
// backend/src/PawTrack.Domain/Adoptions/AdoptionApplication.cs

public enum ApplicationStatus { Pending, Reviewing, Approved, Rejected, Withdrawn }

public sealed class AdoptionApplication
{
    private AdoptionApplication() { }

    public Guid Id { get; private set; }
    public Guid AdoptablePetId { get; private set; }
    public Guid ApplicantUserId { get; private set; }
    public string ApplicantMessage { get; private set; } = string.Empty;
    /// <summary>Respuestas al formulario de pre-adopción (JSON estructurado).</summary>
    public string? FormResponsesJson { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    /// <summary>ThreadId del chat mascarado abierto entre adoptante y organización.</summary>
    public Guid? ChatThreadId { get; private set; }

    public static AdoptionApplication Create(Guid petId, Guid applicantUserId, string message, string? formJson) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            AdoptablePetId = petId,
            ApplicantUserId = applicantUserId,
            ApplicantMessage = message.Trim(),
            FormResponsesJson = formJson,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTimeOffset.UtcNow,
        };

    public void Approve(string? note) { Status = ApplicationStatus.Approved; ReviewNote = note?.Trim(); ReviewedAt = DateTimeOffset.UtcNow; }
    public void Reject(string? note) { Status = ApplicationStatus.Rejected; ReviewNote = note?.Trim(); ReviewedAt = DateTimeOffset.UtcNow; }
    public void StartReview() { Status = ApplicationStatus.Reviewing; ReviewedAt = DateTimeOffset.UtcNow; }
    public void Withdraw() { Status = ApplicationStatus.Withdrawn; }
    public void LinkChatThread(Guid threadId) { ChatThreadId = threadId; }
}
```

### 5.3 `AdoptionFair` — feria/evento de adopción

```csharp
// backend/src/PawTrack.Domain/Adoptions/AdoptionFair.cs

public enum FairStatus { Upcoming, Active, Ended, Cancelled }

public sealed class AdoptionFair
{
    private AdoptionFair() { }

    public Guid Id { get; private set; }
    public Guid OrganizationUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public string LocationLabel { get; private set; } = string.Empty;
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public FairStatus Status { get; private set; }
    public int AlertRadiusMetres { get; private set; } = 10_000; // 10 km default
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsActive => Status == FairStatus.Active
        && DateTimeOffset.UtcNow >= StartsAt
        && DateTimeOffset.UtcNow < EndsAt;

    public static AdoptionFair Create(Guid orgUserId, string title, string? description,
        double lat, double lng, string locationLabel,
        DateTimeOffset startsAt, DateTimeOffset endsAt, int alertRadiusMetres = 10_000)
    {
        if (endsAt <= startsAt) throw new ArgumentException("EndsAt must be after StartsAt.");
        return new()
        {
            Id = Guid.CreateVersion7(),
            OrganizationUserId = orgUserId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Lat = lat, Lng = lng,
            LocationLabel = locationLabel.Trim(),
            StartsAt = startsAt, EndsAt = endsAt,
            Status = FairStatus.Upcoming,
            AlertRadiusMetres = alertRadiusMetres,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Activate() { Status = FairStatus.Active; }
    public void End() { Status = FairStatus.Ended; }
    public void Cancel() { Status = FairStatus.Cancelled; }
}
```

---

## 6. API — endpoints requeridos

### 6.1 Adopciones públicas (sin auth)

```
GET  /api/adoptions?species=&size=&location=&page=&pageSize=   → lista paginada
GET  /api/adoptions/{id}                                        → perfil del animal
GET  /api/adoptions/map?north=&south=&east=&west=              → pins para el mapa
GET  /api/adoptions/fairs                                       → ferias activas/próximas
GET  /api/adoptions/fairs/{id}                                  → detalle de feria
```

### 6.2 Aplicaciones (auth requerida)

```
POST /api/adoptions/{id}/applications                           → aplicar para adoptar
GET  /api/adoptions/applications/mine                          → mis aplicaciones
DELETE /api/adoptions/applications/{id}                         → retirar aplicación
```

### 6.3 Gestión de organización (Ally auth)

```
GET  /api/adoptions/mine                                        → mis animales publicados
POST /api/adoptions                                             → publicar animal
PUT  /api/adoptions/{id}                                        → actualizar animal
POST /api/adoptions/{id}/photos                                 → subir foto (multipart)
DELETE /api/adoptions/{id}/photos/{photoIndex}                  → eliminar foto
PATCH /api/adoptions/{id}/status                               → cambiar estado
GET  /api/adoptions/{id}/applications                           → ver aplicaciones
PUT  /api/adoptions/{id}/applications/{appId}/review            → aprobar/rechazar
POST /api/adoptions/fairs                                       → crear feria
PUT  /api/adoptions/fairs/{id}                                  → actualizar feria
PATCH /api/adoptions/fairs/{id}/status                         → activar/cancelar feria
```

### 6.4 Admin

```
GET  /api/admin/adoptions                                       → listado paginado
PATCH /api/admin/adoptions/{id}/feature                        → destacar en mapa
DELETE /api/admin/adoptions/{id}                               → eliminar contenido inapropiado
```

---

## 7. Application layer — Commands y Queries

### 7.1 Estructura de archivos

```
PawTrack.Application/
└── Adoptions/
    ├── Commands/
    │   ├── PublishAdoptablePet/
    │   │   ├── PublishAdoptablePetCommand.cs
    │   │   ├── PublishAdoptablePetCommandHandler.cs
    │   │   └── PublishAdoptablePetCommandValidator.cs
    │   ├── UpdateAdoptablePet/
    │   ├── UploadAdoptionPhoto/
    │   ├── SetAdoptionStatus/
    │   ├── ApplyForAdoption/
    │   ├── ReviewAdoptionApplication/
    │   ├── WithdrawAdoptionApplication/
    │   ├── CreateAdoptionFair/
    │   └── SetFairStatus/
    ├── Queries/
    │   ├── GetAdoptablePets/
    │   ├── GetAdoptablePetDetail/
    │   ├── GetAdoptionsMap/
    │   ├── GetMyAdoptablePets/
    │   ├── GetPetApplications/
    │   ├── GetMyApplications/
    │   ├── GetAdoptionFairs/
    │   └── GetAdoptionFairDetail/
    ├── DTOs/
    │   ├── AdoptablePetDto.cs
    │   ├── AdoptablePetDetailDto.cs
    │   ├── AdoptionApplicationDto.cs
    │   ├── AdoptionFairDto.cs
    │   └── AdoptionMapPinDto.cs
    └── Interfaces/
        └── IAdoptionRepository.cs
```

### 7.2 `PublishAdoptablePetCommand` — detalle completo

```csharp
public sealed record PublishAdoptablePetCommand(
    Guid OrganizationUserId,
    string Name,
    PetSpecies Species,
    PetSize Size,
    AgeCategory AgeCategory,
    int? AgeMonthsApprox,
    string Story,
    string? Breed,
    string? Requirements,
    string? MedicalNotes,
    bool IsVaccinated,
    bool IsSterilized,
    bool IsMicrochipped,
    bool OkWithKids,
    bool OkWithDogs,
    bool OkWithCats,
    double? LocationLat,
    double? LocationLng,
    string? LocationLabel,
    byte[]? MainPhotoBytes,
    string? MainPhotoContentType) : IRequest<Result<AdoptablePetDto>>;

public sealed class PublishAdoptablePetCommandValidator : AbstractValidator<PublishAdoptablePetCommand>
{
    public PublishAdoptablePetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Story).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Requirements).MaximumLength(500);
        RuleFor(x => x.MedicalNotes).MaximumLength(500);
        RuleFor(x => x.LocationLat).InclusiveBetween(-90, 90).When(x => x.LocationLat.HasValue);
        RuleFor(x => x.LocationLng).InclusiveBetween(-180, 180).When(x => x.LocationLng.HasValue);
        // Require location or at least a label
        RuleFor(x => x)
            .Must(x => x.LocationLat.HasValue || !string.IsNullOrEmpty(x.LocationLabel))
            .WithMessage("Debe indicar una ubicación aproximada.");
    }
}

public sealed class PublishAdoptablePetCommandHandler(
    IAdoptionRepository repo,
    IAllyRepository allyRepo,           // verify the org is a verified ally
    IBlobStorageService blobStorage,
    IImageProcessor imageProcessor,
    IUnitOfWork uow)
    : IRequestHandler<PublishAdoptablePetCommand, Result<AdoptablePetDto>>
{
    private const string Container = "adoption-photos";

    public async Task<Result<AdoptablePetDto>> Handle(
        PublishAdoptablePetCommand request, CancellationToken ct)
    {
        // Verify the publisher is a verified Ally (Shelter type preferred but not enforced)
        var ally = await allyRepo.GetByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.VerificationStatus != AllyVerificationStatus.Verified)
            return Result.Failure<AdoptablePetDto>("Solo aliados verificados pueden publicar animales en adopción.");

        var pet = AdoptablePet.Create(
            request.OrganizationUserId, request.Name, request.Species,
            request.Size, request.AgeCategory, request.Story,
            request.LocationLat, request.LocationLng, request.LocationLabel);

        pet.UpdateDetails(request.Name, request.Story, request.Requirements,
            request.MedicalNotes, request.IsVaccinated, request.IsSterilized,
            request.IsMicrochipped, request.OkWithKids, request.OkWithDogs,
            request.OkWithCats, request.Breed, request.Size, request.AgeCategory,
            request.AgeMonthsApprox, null);

        if (request.MainPhotoBytes is { Length: > 0 })
        {
            var resized = await imageProcessor.ResizeAsync(request.MainPhotoBytes, 800, ct);
            var blobName = $"{pet.Id}/main/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.jpg";
            using var stream = new MemoryStream(resized);
            var url = await blobStorage.UploadAsync(Container, blobName, stream, "image/jpeg", ct);
            pet.SetMainPhoto(url);
        }

        await repo.AddAsync(pet, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(AdoptablePetDto.FromDomain(pet));
    }
}
```

### 7.3 `ApplyForAdoptionCommand` — con creación de chat

```csharp
public sealed record ApplyForAdoptionCommand(
    Guid ApplicantUserId,
    Guid AdoptablePetId,
    string Message,
    string? FormResponsesJson) : IRequest<Result<AdoptionApplicationDto>>;

public sealed class ApplyForAdoptionCommandValidator : AbstractValidator<ApplyForAdoptionCommand>
{
    public ApplyForAdoptionCommandValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(600)
            .WithMessage("El mensaje no puede estar vacío y no debe exceder 600 caracteres.");
        RuleFor(x => x.AdoptablePetId).NotEmpty();
    }
}

// Handler: crea la aplicación + abre un ChatThread usando la infraestructura de chat existente
// El chat es entre el applicantUserId y el organizationUserId del AdoptablePet
// Reutiliza el mismo IChatRepository y ChatThread.Open() del módulo de LostPets
```

---

## 8. Infrastructure — Repositorios y Configuración EF Core

### 8.1 `IAdoptionRepository`

```csharp
public interface IAdoptionRepository
{
    // AdoptablePet
    Task<AdoptablePet?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptablePet>> GetAvailableAsync(AdoptionFilter filter, int skip, int take, CancellationToken ct = default);
    Task<int> CountAvailableAsync(AdoptionFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptablePet>> GetInBBoxAsync(double north, double south, double east, double west, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptablePet>> GetByOrganizationAsync(Guid orgUserId, CancellationToken ct = default);
    Task AddAsync(AdoptablePet pet, CancellationToken ct = default);
    void Update(AdoptablePet pet);

    // AdoptionApplication
    Task<AdoptionApplication?> GetApplicationByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasAppliedAsync(Guid applicantUserId, Guid petId, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionApplication>> GetByPetAsync(Guid petId, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionApplication>> GetByApplicantAsync(Guid applicantUserId, CancellationToken ct = default);
    Task AddApplicationAsync(AdoptionApplication app, CancellationToken ct = default);
    void UpdateApplication(AdoptionApplication app);

    // AdoptionFair
    Task<AdoptionFair?> GetFairByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionFair>> GetUpcomingFairsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionFair>> GetActiveFairsAsync(CancellationToken ct = default);
    Task AddFairAsync(AdoptionFair fair, CancellationToken ct = default);
    void UpdateFair(AdoptionFair fair);
}

public sealed record AdoptionFilter(
    PetSpecies? Species = null,
    PetSize? Size = null,
    AgeCategory? AgeCategory = null,
    bool? OkWithKids = null,
    bool? OkWithDogs = null,
    bool? OkWithCats = null,
    string? LocationLabel = null);
```

### 8.2 EF Core Configuration

```csharp
// AdoptablePetConfiguration.cs
builder.ToTable("AdoptablePets");
builder.HasKey(x => x.Id);
builder.Property(x => x.OrganizationUserId).IsRequired();
builder.Property(x => x.Name).IsRequired().HasMaxLength(80);
builder.Property(x => x.Story).IsRequired().HasMaxLength(2000);
builder.Property(x => x.Requirements).HasMaxLength(500);
builder.Property(x => x.MedicalNotes).HasMaxLength(500);
builder.Property(x => x.Breed).HasMaxLength(100);
builder.Property(x => x.WeightKg).HasMaxLength(20);
builder.Property(x => x.MainPhotoUrl).HasMaxLength(500);
builder.Property(x => x.LocationLabel).HasMaxLength(150);
builder.Property(x => x.Species).IsRequired().HasConversion<int>();
builder.Property(x => x.Size).IsRequired().HasConversion<int>();
builder.Property(x => x.AgeCategory).IsRequired().HasConversion<int>();
builder.Property(x => x.Status).IsRequired().HasConversion<int>();
// Backing field for photo URLs list (stored as JSON array)
builder.Property<string>("_photoUrlsJson")
    .HasColumnName("PhotoUrlsJson").HasMaxLength(2500);
// Indexes
builder.HasIndex(x => new { x.Status, x.Species });
builder.HasIndex(x => x.OrganizationUserId);
builder.HasIndex(x => new { x.LocationLat, x.LocationLng })
    .HasFilter("LocationLat IS NOT NULL AND LocationLng IS NOT NULL");
```

### 8.3 `PawTrackDbContext` — agregar DbSets

```csharp
public DbSet<AdoptablePet> AdoptablePets => Set<AdoptablePet>();
public DbSet<AdoptionApplication> AdoptionApplications => Set<AdoptionApplication>();
public DbSet<AdoptionFair> AdoptionFairs => Set<AdoptionFair>();
```

---

## 9. Frontend — Páginas y componentes

### 9.1 Estructura de archivos

```
frontend/src/features/adoptions/
├── api/
│   └── adoptionsApi.ts          ← API client (axios)
├── hooks/
│   ├── useAdoptions.ts          ← useAdoptablePets, useAdoptionDetail, useMyAdoptions
│   ├── useAdoptionApplications.ts
│   └── useAdoptionFairs.ts
├── components/
│   ├── AdoptionCard.tsx          ← tarjeta en grid (foto + nombre + badges)
│   ├── AnimalDetailSheet.tsx     ← drawer con perfil completo (como StoreDetailSheet)
│   ├── AdoptionFilters.tsx       ← filtros: especie, tamaño, edad, zona, necesidades
│   ├── AnimalMapMarker.tsx       ← pin de corazón 🐾 en el mapa
│   ├── FairMapMarker.tsx         ← pin de feria 🎪 en el mapa
│   ├── AdoptionApplicationForm.tsx ← formulario de solicitud
│   └── AdoptionFairCard.tsx     ← tarjeta de feria con countdown
└── pages/
    ├── AdoptionsPage.tsx         ← /adopciones — grid + filtros
    ├── AnimalProfilePage.tsx     ← /adopciones/:id — perfil público completo
    ├── MyApplicationsPage.tsx    ← /mis-adopciones — mis solicitudes
    ├── OrgAdoptionsPage.tsx      ← /adopciones/portal — panel de organización
    └── CreateAdoptionPage.tsx    ← /adopciones/nuevo — formulario publicar animal
```

### 9.2 Rutas a agregar en `routes.tsx`

```tsx
// Públicas (sin auth)
{ path: "/adopciones",        element: <AdoptionsPage /> }
{ path: "/adopciones/:id",    element: <AnimalProfilePage /> }

// Autenticadas
{ path: "/mis-adopciones",    element: <MyApplicationsPage /> }

// Solo Ally verificado
{ path: "/adopciones/portal", element: <OrgAdoptionsPage /> }
{ path: "/adopciones/nuevo",  element: <CreateAdoptionPage /> }
{ path: "/adopciones/editar/:id", element: <EditAdoptionPage /> }
```

### 9.3 `AnimalDetailSheet` — drawer principal del mapa

```
┌──────────────────────────────────────┐
│ [foto principal]                      │
│ Milo · Perro · Mediano · Adulto      │
│ 📍 Desamparados, San José            │
│                                       │
│ 💉 Vacunado  ✂️ Castrado  📡 Chip   │
│ 👶 OK niños  🐕 OK perros           │
│                                       │
│ Historia:                             │
│ "Milo llegó a nosotros en febrero..." │
│                                       │
│ Requisitos: Patio, hogar sin gatos   │
│                                       │
│ [Galería de fotos]                    │
│                                       │
│ Publicado por: Refugio Animal CR      │
│ ─────────────────────────────────── │
│ [ Quiero adoptarlo → ]               │
└──────────────────────────────────────┘
```

### 9.4 `AdoptionsPage` — grid con filtros

```
┌─ 🐾 Animales en adopción ────────────┐
│ [Perros] [Gatos] [Todos] [Ferias 🎪] │
│                                       │
│ Tamaño: [S][M][L]  Zona: [San José▼] │
│ ☑ OK con niños  ☑ OK con perros      │
│                                       │
│ 48 animales disponibles               │
│                                       │
│ ┌──────┐ ┌──────┐ ┌──────┐          │
│ │ foto │ │ foto │ │ foto │          │
│ │ Milo │ │ Luna │ │ Roco │          │
│ │ ♂ 3y │ │ ♀ 1y │ │ ♂ 5y │          │
│ └──────┘ └──────┘ └──────┘          │
│                                       │
│            [Cargar más]               │
└───────────────────────────────────────┘
```

### 9.5 Tipos TypeScript

```typescript
// adoptionsApi.ts

export type PetSize = "XSmall" | "Small" | "Medium" | "Large" | "XLarge";
export type AgeCategory = "Puppy" | "Young" | "Adult" | "Senior";
export type AdoptionStatus =
  | "Available"
  | "InProcess"
  | "Adopted"
  | "Paused"
  | "Removed";
export type ApplicationStatus =
  | "Pending"
  | "Reviewing"
  | "Approved"
  | "Rejected"
  | "Withdrawn";
export type FairStatus = "Upcoming" | "Active" | "Ended" | "Cancelled";

export interface AdoptablePetDto {
  id: string;
  organizationUserId: string;
  organizationName: string; // denormalizado del AllyProfile
  name: string;
  species: string;
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
  mainPhotoUrl: string | null;
  photoUrls: string[];
  locationLat: number | null;
  locationLng: number | null;
  locationLabel: string | null;
  status: AdoptionStatus;
  publishedAt: string;
}
```

---

## 10. Integración con el mapa

### 10.1 Nuevo tipo de pin en el mapa público

Añadir `adoptions` como nueva capa togglable en `PublicMapPage`:

```tsx
// En PublicMapPage.tsx
const [showAdoptions, setShowAdoptions] = useState(false);
const { data: adoptionPins = [] } = useAdoptionMapPins(showAdoptions);
// toggle: 🐾 Adopciones
```

```tsx
// AnimalMapMarker.tsx — pin de corazón púrpura
const adoptionIcon = divIcon({
  className: "",
  html: `<div style="
    width:30px;height:30px;border-radius:50%;
    background:#7c3aed;border:2px solid #fff;
    display:flex;align-items:center;justify-content:center;
    font-size:15px;box-shadow:0 1px 4px rgba(0,0,0,.3);
  ">🐾</div>`,
  iconSize: [30, 30],
  iconAnchor: [15, 15],
  popupAnchor: [0, -17],
});
```

### 10.2 Endpoint de mapa para adopciones

```csharp
// En GetPublicMapEventsQueryHandler — extender con adopciones:
// O crear endpoint separado: GET /api/adoptions/map?north=&south=&east=&west=
// Retorna solo animales Available con coordenadas (lat, lng, name, species, mainPhotoUrl, id)
```

### 10.3 Deep-link desde directorio al mapa

```
/mapa?adoptionId=X → abre AnimalDetailSheet directamente
```

---

## 11. Notificaciones

### 11.1 Nuevos tipos de notificación

```csharp
// Añadir a NotificationType enum:
AdoptionInterest,   // org recibe: alguien aplicó para adoptar
AdoptionApproved,   // adoptante recibe: aplicación aprobada
AdoptionRejected,   // adoptante recibe: aplicación rechazada
AdoptionFairNearby, // geofenceada: hay una feria de adopción cerca
```

### 11.2 Flujo de notificaciones

```
Adoptante aplica
    ↓
NotifyAdoptionInterestAsync(orgUserId, petName, applicantName)
    → In-app notification a la organización
    → Push notification (opcional, si la org tiene push subscription)

Organización aprueba
    ↓
NotifyAdoptionApprovedAsync(applicantUserId, petName, orgName)
    → In-app + Push al adoptante

Feria próxima (job nocturno)
    ↓
Para cada feria en las próximas 48h:
    → GetNearbyAlertSubscribersAsync(fair.Lat, fair.Lng, fair.AlertRadiusMetres)
    → Enviar push geofenceada con "🎪 Feria de adopción el sábado en [lugar]"
    → Respetar quiet hours del usuario
```

### 11.3 Métodos nuevos en `INotificationDispatcher`

```csharp
Task DispatchAdoptionInterestAsync(
    Guid orgUserId, string petName, string applicantName,
    string applicationId, CancellationToken ct = default);

Task DispatchAdoptionDecisionAsync(
    Guid applicantUserId, string petName, string orgName,
    bool approved, string? note, CancellationToken ct = default);

Task DispatchAdoptionFairNearbyAsync(
    Guid userId, string fairTitle, string locationLabel,
    DateTimeOffset startsAt, string fairId, CancellationToken ct = default);
```

---

## 12. WhatsApp bot — extensión

### 12.1 Flujo conversacional nuevo: "Quiero adoptar"

Cuando alguien escribe "adoptar" o "adopción" al bot:

```
BOT: "¡Tenemos animales esperando un hogar! 🐾
¿Buscas un perro o un gato?
→ Responde: PERRO | GATO | CUALQUIERA"

USER: "PERRO"

BOT: "¿En qué zona de Costa Rica estás?
→ Responde el nombre del cantón (ej: Desamparados)"

USER: "Alajuela"

BOT: "Encontré 3 perritos disponibles cerca de Alajuela:
1. Milo — Mediano, 3 años 🟣 Ver perfil: pawtrack.cr/adopciones/[id1]
2. Rocco — Grande, 5 años 🟣 Ver perfil: pawtrack.cr/adopciones/[id2]
3. Lola — Pequeña, 1 año 🟣 Ver perfil: pawtrack.cr/adopciones/[id3]

¿Te interesa alguno? Responde el número o escribe TODOS para ver más."
```

### 12.2 Flujo para organizaciones: "Publicar animal"

```
Si la sesión corresponde a un Aliado verificado:

BOT: "Hola [OrgName] 👋 ¿Qué deseas hacer?
→ NUEVO: publicar un animal en adopción
→ LISTA: ver tus animales publicados"

USER: "NUEVO"

BOT: "¿Cuál es el nombre del animal?"
USER: "Milo"
BOT: "¿Es perro, gato, pájaro, conejo u otro?"
... (flujo guiado paso a paso)
BOT: "¡Listo! Milo ya está publicado: pawtrack.cr/adopciones/[id]
Envíame una foto para subir la imagen principal."
```

---

## 13. Monetización y planes

### 13.1 Tiers de acceso para organizaciones

| Feature                              | Aliado (gratis) |   Ally+ (futuro)    |
| ------------------------------------ | :-------------: | :-----------------: |
| Publicar animales                    |    Hasta 10     |      Ilimitado      |
| Fotos por animal                     |        3        |          5          |
| Crear ferias                         |    1 activa     |     Ilimitadas      |
| Estadísticas de adopción             |     Básicas     |      Avanzadas      |
| Posición prioritaria en mapa         |       ❌        |         ✅          |
| Badge "Organización verificada"      |       ✅        |    ✅ + Featured    |
| Notificaciones geofenceadas de feria |       ✅        | ✅ + radio ampliado |

### 13.2 Nuevas líneas de ingreso

| Línea                                  | Modelo              | Estimado          |
| -------------------------------------- | ------------------- | ----------------- |
| Billboard placement `Adoption` (nuevo) | Tarifa fija/semana  | ₡15,000-30,000    |
| Plan Ally+ con features premium        | Suscripción mensual | ₡8,000-15,000/mes |
| Feria destacada en mapa                | One-time            | ₡10,000/evento    |

### 13.3 Plan `AdoptionCampaign` — nuevo tier de suscripción

```csharp
// Añadir a SubscriptionTier enum:
AllyBasic = 300,    // Aliado verificado con adopciones básicas (ya incluido en Ally)
AllyPlus = 310,     // ₡10,000/mes — adopciones ilimitadas + estadísticas + featured
```

---

## 14. Migraciones EF Core

```bash
# Desde backend/
dotnet ef migrations add AddAdoptions \
  --project src/PawTrack.Infrastructure \
  --startup-project src/PawTrack.API \
  --context PawTrackDbContext
```

**Tablas que se crean:**

- `AdoptablePets` — animales en adopción
- `AdoptionApplications` — solicitudes de adopción
- `AdoptionFairs` — ferias y eventos de adopción

**Nuevo contenedor Blob Storage:**

- `adoption-photos` — privado (acceso vía URL firmada o URL directa en producción)

---

## 15. Tests requeridos

### 15.1 Tests unitarios (xUnit + NSubstitute)

```
PawTrack.UnitTests/
└── Adoptions/
    ├── AdoptablePetDomainTests.cs
    │   ├── Create_ValidData_SetsAvailableStatus
    │   ├── MarkAdopted_SetsAdoptedAt
    │   ├── AddPhoto_CapAt5_ThrowsOrIgnores
    │   └── Remove_SetsRemovedStatus
    ├── AdoptionApplicationDomainTests.cs
    │   ├── Create_SetsPending
    │   ├── Approve_SetsApproved_WithNote
    │   └── Reject_SetsRejected
    └── Handlers/
        ├── PublishAdoptablePetCommandHandlerTests.cs
        │   ├── Handle_VerifiedAlly_PublishesSuccessfully
        │   ├── Handle_NonAlly_ReturnsFailure
        │   └── Handle_WithPhoto_UploadsAndSetsMainPhoto
        ├── ApplyForAdoptionCommandHandlerTests.cs
        │   ├── Handle_ValidApplication_Creates
        │   ├── Handle_DuplicateApplication_ReturnsFailure
        │   └── Handle_PetNotAvailable_ReturnsFailure
        └── ReviewAdoptionApplicationTests.cs
            ├── Handle_Approve_NotifiesApplicant
            └── Handle_Reject_NotifiesApplicant
```

### 15.2 Tests de integración (WebApplicationFactory)

```
PawTrack.IntegrationTests/
└── Adoptions/
    ├── POST /api/adoptions — ally can publish
    ├── GET  /api/adoptions — public can list
    ├── GET  /api/adoptions/:id — public can view
    ├── POST /api/adoptions/:id/applications — authenticated user can apply
    └── PUT  /api/adoptions/:id/applications/:appId/review — org can approve
```

---

## 16. Roadmap de implementación

### Sprint 1 — Fundamentos (Semana 1-2)

**Backend:**

- [ ] Domain: `AdoptablePet`, `AdoptionApplication`, `AdoptionFair`
- [ ] Migración EF Core `AddAdoptions`
- [ ] `IAdoptionRepository` + implementación
- [ ] `PublishAdoptablePetCommand` + handler + validator
- [ ] `ApplyForAdoptionCommand` + handler
- [ ] `GetAdoptablePets` y `GetAdoptablePetDetail` queries
- [ ] `AdoptionsController` con endpoints públicos y de organización

**Frontend:**

- [ ] `adoptionsApi.ts` con todos los métodos
- [ ] `useAdoptions.ts`, `useAdoptionApplications.ts`
- [ ] `AdoptionsPage.tsx` con grid básica
- [ ] `AnimalProfilePage.tsx` con perfil completo

### Sprint 2 — Mapa y notificaciones (Semana 3-4)

**Backend:**

- [ ] `GetAdoptionsMapQuery` con bbox filtering
- [ ] Nuevos tipos en `NotificationType`
- [ ] `DispatchAdoptionInterestAsync` en NotificationDispatcher
- [ ] `DispatchAdoptionDecisionAsync`
- [ ] `ReviewAdoptionApplicationCommand`

**Frontend:**

- [ ] `AnimalMapMarker.tsx`
- [ ] Capa "Adopciones" en `PublicMapPage` (toggle 🐾)
- [ ] `AnimalDetailSheet.tsx` con formulario integrado
- [ ] `MyApplicationsPage.tsx`

### Sprint 3 — Ferias y organización (Semana 5-6)

**Backend:**

- [ ] `AdoptionFair` CRUD completo
- [ ] Job de notificaciones geofenceadas para ferias
- [ ] `GetUpcomingFairs` query
- [ ] Estadísticas básicas para organizaciones

**Frontend:**

- [ ] `FairMapMarker.tsx`
- [ ] `AdoptionFairCard.tsx` con countdown
- [ ] `OrgAdoptionsPage.tsx` — panel de gestión
- [ ] `CreateAdoptionPage.tsx` — publicar animal
- [ ] Foto upload con galería (hasta 5)

### Sprint 4 — WhatsApp bot y monetización (Semana 7-8)

- [ ] Extensión del WhatsApp bot para búsqueda y publicación
- [ ] Sistema de planes Ally+
- [ ] Billboard placement `Adoption` en AdminBillboardsTab
- [ ] Tests unitarios completos (todas las suites del §15.1)
- [ ] Tests de integración del §15.2

---

## 17. Wireframes y UX

### 17.1 Flujo del adoptante

```
Home / Mapa
  ↓ click toggle 🐾 Adopciones
Pines en el mapa
  ↓ click en pin
AnimalDetailSheet (drawer)
  ↓ "Quiero adoptarlo →"
Formulario de solicitud
  ↓ submit
Confirmación → Chat abierto con la organización
  ↓ organización revisa
Notificación: Aprobado / Necesita más info
```

### 17.2 Flujo de la organización

```
/adopciones/portal (OrgAdoptionsPage)
├── Lista de mis animales (con estado + contador de aplicaciones)
├── Botón "+ Publicar animal" → CreateAdoptionPage
│   ├── Formulario multi-step:
│   │   Step 1: Datos básicos (especie, tamaño, nombre)
│   │   Step 2: Historia y personalidad
│   │   Step 3: Salud (vacunas, castrado, chip)
│   │   Step 4: Requisitos para adoptantes
│   │   Step 5: Fotos (drag & drop, hasta 5)
│   │   Step 6: Ubicación (mapa interactivo)
│   └── Preview → Publicar
└── Lista de aplicaciones por animal
    ├── Aplicación #1 — Juan Pérez — [En revisión]
    │   ├── Ver mensaje del solicitante
    │   ├── Ver formulario de pre-adopción
    │   ├── Abrir chat con solicitante
    │   └── [Aprobar] [Rechazar]
    └── ...
```

### 17.3 Página pública del animal (`/adopciones/:id`)

```
┌─────────────────────────────────────┐
│ [foto principal 100% ancho]          │
│                      [◀ 1/3 ▶]     │  ← galería navegable
├─────────────────────────────────────┤
│ Milo                                 │
│ 🐕 Perro mediano · Adulto (3 años)  │
│ 📍 Desamparados, San José            │
│                                      │
│ ┌─────────────────────────────────┐ │
│ │ 💉 Vacunado   ✂️ Castrado        │ │
│ │ 📡 Microchip  👶 OK con niños    │ │
│ │ 🐕 OK con perros                │ │
│ └─────────────────────────────────┘ │
│                                      │
│ Su historia                          │
│ "Milo llegó a nosotros en febrero   │
│ después de ser encontrado en la     │
│ calle. Es un perro muy cariñoso..." │
│                                      │
│ Requisitos                           │
│ • Hogar con patio                   │
│ • No apto para apartamento          │
│ • Sin gatos en el hogar             │
│                                      │
│ Publicado por                        │
│ 🤝 Refugio Animal CR (verificado)   │
│                                      │
│ ┌─────────────────────────────────┐ │
│ │   💜 Quiero adoptar a Milo →    │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

---

## Notas finales de arquitectura

- **Sin entidad `AdoptablePet` en el `PetRepository`** — los animales en adopción son entidades independientes del módulo `Pets`. Un animal adoptado puede _luego_ ser registrado por el adoptante como `Pet` normal si quiere QR, pero no es obligatorio.
- **Chat reutilizado directamente** — `ChatThread.Open()` acepta `(lostPetEventId, initiatorUserId)`. Para adopciones, se puede crear un `ChatThread` con `adoptionApplicationId` como contexto (o extender `ChatThread` con un campo `ContextType`).
- **Fotos en Blob Storage** — contenedor `adoption-photos`, mismo patrón que `pet-photos`. El blob se borra cuando el animal es marcado como `Removed`.
- **Privacidad de la organización** — igual que con el chat de pérdida, la dirección exacta del refugio no se expone. Solo se muestra el cantón/barrio.
- **Moderación** — Admin puede marcar un animal como `Removed` desde el panel de admin. Los reportes de contenido inapropiado usan el `FraudReport` existente.

---

_PawTrack CR — Módulo de Adopciones v1.0 · 2026-08-20_
