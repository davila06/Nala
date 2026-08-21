# PawTrack CR — Adopciones: Lista de Tareas de Implementación

> Spec de referencia: `docs/adopciones.md`
> Stack: .NET 9 · React 19 · EF Core 9 · MediatR 12 · Azure SQL
> Convenciones: Clean Architecture + CQRS + `Result<T>` + FluentValidation pipeline

---

## Sprint 1 — Domain, CQRS Core y CRUD básico (2 semanas)

### 1.1 Domain Layer

- [ ] Crear `backend/src/PawTrack.Domain/Adoptions/` (directorio)
- [ ] Crear `AdoptablePet.cs`
  - [ ] Enums: `PetSize`, `AdoptionStatus`, `AgeCategory` (en el mismo archivo)
  - [ ] Private EF constructor `private AdoptablePet() { }`
  - [ ] `private readonly List<string> _photoUrls = []`
  - [ ] Todas las propiedades con `private set`
  - [ ] Factory method `Create(...)` usando `Guid.CreateVersion7()`
  - [ ] `AddPhoto(url)` — guard máximo 5 fotos, lanza `InvalidOperationException`
  - [ ] `RemovePhoto(url)`
  - [ ] `MarkInProcess()`, `MarkAdopted()`, `Pause()`, `Republish()`, `Remove()`
  - [ ] `UpdateDetails(...)` para editar campos de texto
  - [ ] `IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly()`
- [ ] Crear `AdoptionApplication.cs`
  - [ ] Enum `ApplicationStatus` (Pending, UnderReview, Approved, Rejected, Withdrawn)
  - [ ] Private EF constructor
  - [ ] Factory `Create(adoptablePetId, applicantUserId, applicantNote)`
  - [ ] `StartReview()`, `Approve(note?)`, `Reject(note?)`, `Withdraw()`
- [ ] Crear `AdoptionFair.cs`
  - [ ] Enum `FairStatus` (Upcoming, Active, Finished, Cancelled)
  - [ ] Private EF constructor
  - [ ] `private readonly List<Guid> _animalIds = []`
  - [ ] Factory `Create(...)` — guard `endsAt <= startsAt` lanza `ArgumentException`
  - [ ] `AddAnimal(id)` — sin duplicados
  - [ ] `RemoveAnimal(id)`
  - [ ] `Activate()`, `Finish()`, `Cancel()`
  - [ ] `bool IsCurrentlyActive` computed property
  - [ ] `IReadOnlyList<Guid> AnimalIds => _animalIds.AsReadOnly()`
- [ ] Extender `NotificationType.cs` — añadir al enum existente (no reemplazar):
  - [ ] `AdoptionInterest`
  - [ ] `AdoptionApproved`
  - [ ] `AdoptionRejected`
  - [ ] `AdoptionFairAlert`

### 1.2 Interface de Repositorio

- [ ] Crear `Application/Common/Interfaces/IAdoptionRepository.cs`
  - [ ] `GetAnimalByIdAsync(Guid id)`
  - [ ] `GetByOrganizationAsync(orgUserId, skip, take)`
  - [ ] `CountByOrganizationAsync(orgUserId)`
  - [ ] `GetAvailablePagedAsync(species?, size?, ageCategory?, isVaccinated?, isSterilized?, okWithKids?, okWithDogs?, nearLat?, nearLng?, radiusKm, skip, take)` → `(IReadOnlyList<AdoptablePet>, int Total)`
  - [ ] `GetAvailableAllAsync()` — para el mapa (sin filtros, max 500)
  - [ ] `AddAnimalAsync(animal)`
  - [ ] `void UpdateAnimal(animal)`
  - [ ] `GetApplicationByIdAsync(Guid id)`
  - [ ] `GetApplicationByApplicantAndAnimalAsync(applicantUserId, animalId)`
  - [ ] `GetApplicationsByAnimalAsync(animalId)`
  - [ ] `GetApplicationsByApplicantAsync(applicantUserId, skip, take)`
  - [ ] `CountApplicationsByApplicantAsync(applicantUserId)`
  - [ ] `AddApplicationAsync(application)`
  - [ ] `void UpdateApplication(application)`
  - [ ] `GetFairByIdAsync(Guid id)`
  - [ ] `GetUpcomingFairsAsync(nearLat?, nearLng?, radiusKm)`
  - [ ] `AddFairAsync(fair)`
  - [ ] `void UpdateFair(fair)`
- [ ] Extender `IAllyProfileRepository.cs` — añadir:
  - [ ] `GetByUserIdsAsync(IEnumerable<Guid> userIds)` — batch query para nombres de org

### 1.3 Application Layer — Commands

- [ ] Crear `Application/Adoptions/` (directorio)
- [ ] Crear `AdoptionCommands.cs`
  - [ ] DTOs: `AdoptablePetDto`, `AdoptionApplicationDto`, `AdoptionFairDto` con métodos `FromDomain`
  - [ ] `PublishAdoptablePetCommand` + `Validator` + `Handler`
    - [ ] Validar que el usuario es Ally verificado con `AllyType.Shelter`
    - [ ] Llamar `AdoptablePet.Create(...)` y persistir
    - [ ] Retornar `AdoptablePetDto.FromDomain(animal, ally.OrganizationName)`
    - [ ] `internal const string NotVerifiedShelterError = "not_verified_shelter"` (para tests)
  - [ ] `ApplyToAdoptCommand` + `Validator` + `Handler`
    - [ ] Guard: animal existe y está `Available`
    - [ ] Guard: no hay aplicación `Pending` duplicada del mismo usuario
    - [ ] Llamar `DispatchAdoptionInterestAsync` de forma fire-and-forget con `.ContinueWith` para logging de errores
    - [ ] `internal const` para cada error string
  - [ ] `ReviewAdoptionApplicationCommand` + `Validator` + `Handler`
    - [ ] Verificar ownership: animal.OrganizationUserId == requestingUserId
    - [ ] Approve: llamar `application.Approve()` + `animal.MarkInProcess()`
    - [ ] Reject: llamar `application.Reject()`
    - [ ] Fire-and-forget `DispatchAdoptionApprovedAsync` / `DispatchAdoptionRejectedAsync`
  - [ ] `MarkAdoptedCommand` + `Handler` (sin validator — solo GUID input)
    - [ ] Ownership check
    - [ ] `animal.MarkAdopted()`
  - [ ] `UpdateAdoptablePetCommand` + `Validator` + `Handler`
    - [ ] Solo campos de texto: name, story, requirements, medicalNotes, boolean flags
    - [ ] Ownership check
  - [ ] `UploadAdoptionPhotoCommand` + `Handler` (Sprint 2)
  - [ ] `CreateAdoptionFairCommand` + `Validator` + `Handler` (Sprint 3)
  - [ ] `WithdrawApplicationCommand` + `Handler`
    - [ ] Solo el propio aplicante puede retirar su solicitud
    - [ ] Guard: solo si status == Pending o UnderReview

### 1.4 Application Layer — Queries

- [ ] Crear `AdoptionQueries.cs`
  - [ ] `GetAdoptablePetsQuery` + `Handler` (filtros + geo + paginación)
    - [ ] Batch load de org names via `GetByUserIdsAsync`
    - [ ] Retornar `PagedResult<AdoptablePetDto>`
  - [ ] `GetAdoptablePetByIdQuery` + `Handler`
    - [ ] Retornar 404-equivalent si no existe o status == Removed
  - [ ] `GetMyAdoptionAnimalsQuery` + `Handler` (shelter view, paginado)
  - [ ] `GetApplicationsForAnimalQuery` + `Handler`
    - [ ] Verificar ownership antes de exponer
  - [ ] `GetMyAdoptionApplicationsQuery` + `Handler` (applicant view, paginado)
  - [ ] `GetUpcomingFairsQuery` + `Handler` (público, geo-filtrado)
  - [ ] `GetAdoptionStatsQuery` + `Handler` (para admin/shelter dashboard)
    - [ ] Total published, total adopted, total applications, conversion rate

### 1.5 Infrastructure — EF Core Configuration

- [ ] Crear `Infrastructure/Adoptions/` (directorio)
- [ ] Crear `AdoptionConfiguration.cs` con 3 `IEntityTypeConfiguration<T>`:
  - [ ] `AdoptablePetConfiguration`
    - [ ] `b.ToTable("AdoptableAnimals")`
    - [ ] `HasMaxLength` en todos los strings
    - [ ] `HasColumnType("decimal(9,6)")` para lat/lng
    - [ ] `HasConversion<string>()` para `Species`, `Size`, `AgeCategory`, `Status`
    - [ ] JSON converter para `_photoUrls` (private backing field via `HasField`)
    - [ ] Índices: `OrganizationUserId`, `Status`, `(Species, Status)` compuesto
  - [ ] `AdoptionApplicationConfiguration`
    - [ ] `b.ToTable("AdoptionApplications")`
    - [ ] `HasConversion<string>()` para `Status`
    - [ ] Índice único: `(ApplicantUserId, AdoptablePetId)`
    - [ ] Índices: `AdoptablePetId`, `ApplicantUserId`
  - [ ] `AdoptionFairConfiguration`
    - [ ] `b.ToTable("AdoptionFairs")`
    - [ ] `HasConversion<string>()` para `Status`
    - [ ] JSON converter para `_animalIds`
    - [ ] Índices: `OrganizationUserId`, `Status`, `StartsAt`

### 1.6 Infrastructure — Repositorios

- [ ] Crear `Infrastructure/Adoptions/AdoptionRepository.cs` implementando `IAdoptionRepository`
  - [ ] `GetAnimalByIdAsync` — sin `AsNoTracking()` (para mutaciones)
  - [ ] `GetByOrganizationAsync` — con `AsNoTracking()`, paginado
  - [ ] `CountByOrganizationAsync`
  - [ ] `GetAvailablePagedAsync` — bounding box geo en SQL, filtros opcionales, CountAsync + ToListAsync separados
  - [ ] `GetAvailableAllAsync` — cap a 500, sin filtros
  - [ ] `GetApplicationByIdAsync` — sin `AsNoTracking()`
  - [ ] `GetApplicationByApplicantAndAnimalAsync`
  - [ ] `GetApplicationsByAnimalAsync` — `AsNoTracking()`, ordenado desc por fecha
  - [ ] `GetApplicationsByApplicantAsync` — `AsNoTracking()`, paginado
  - [ ] `CountApplicationsByApplicantAsync`
  - [ ] `GetUpcomingFairsAsync` — filtrar `EndsAt > now`, bounding box, ordenar `StartsAt` asc
  - [ ] Todos los métodos Add usando `await db.X.AddAsync()`
  - [ ] Todos los Update usando `db.X.Update(entity)`
- [ ] Extender `Infrastructure/Allies/AllyProfileRepository.cs`
  - [ ] Implementar `GetByUserIdsAsync` — `Where(a => ids.Contains(a.UserId))` + `AsNoTracking()`

### 1.7 DbContext

- [ ] Editar `Infrastructure/Persistence/PawTrackDbContext.cs`
  - [ ] Añadir `using PawTrack.Domain.Adoptions;`
  - [ ] Añadir `DbSet<AdoptablePet> AdoptableAnimals => Set<AdoptablePet>();`
  - [ ] Añadir `DbSet<AdoptionApplication> AdoptionApplications => Set<AdoptionApplication>();`
  - [ ] Añadir `DbSet<AdoptionFair> AdoptionFairs => Set<AdoptionFair>();`

### 1.8 DI Registration

- [ ] Editar `Infrastructure/InfrastructureServiceCollectionExtensions.cs`
  - [ ] `services.AddScoped<IAdoptionRepository, AdoptionRepository>();`

### 1.9 Migración EF Core

- [ ] Ejecutar: `dotnet ef migrations add AddAdoptionsModule --project src/PawTrack.Infrastructure --startup-project src/PawTrack.API --output-dir Migrations`
- [ ] Verificar que el migration generado crea las 3 tablas correctamente
- [ ] Verificar que los índices compuestos están presentes en el migration
- [ ] Ejecutar `dotnet ef database update` en el entorno de desarrollo

### 1.10 API Controller — CRUD básico

- [ ] Crear `API/Controllers/AdoptionsController.cs`
  - [ ] `[ApiController]`, `[Route("api/adoptions")]`
  - [ ] Constructor: `ISender sender` solamente (foto upload en Sprint 2)
  - [ ] `private bool TryGetUserId(out Guid userId)` — patrón del repo
  - [ ] `GET /animals` — público, `[EnableRateLimiting("public-api")]`, filtros por query string
    - [ ] `pageSize = Math.Clamp(pageSize, 1, 50)`
  - [ ] `GET /animals/map` — público, retorna lista plana (no paginada)
  - [ ] `GET /animals/{id:guid}` — público
  - [ ] `GET /fairs` — público, geo-filtrado
  - [ ] `POST /animals` — `[Authorize(Roles = "Ally")]`, `[RequestSizeLimit(16_384)]`
  - [ ] `PATCH /animals/{id:guid}` — `[Authorize(Roles = "Ally")]`, `[RequestSizeLimit(8192)]`
  - [ ] `GET /animals/mine` — `[Authorize(Roles = "Ally")]`
  - [ ] `GET /animals/{id:guid}/applications` — `[Authorize(Roles = "Ally")]`
  - [ ] `PATCH /applications/{id:guid}/review` — `[Authorize(Roles = "Ally")]`, `[RequestSizeLimit(4096)]`
  - [ ] `PATCH /animals/{id:guid}/mark-adopted` — `[Authorize(Roles = "Ally")]`
  - [ ] `POST /animals/{id:guid}/apply` — `[Authorize(Roles = "Owner")]`, `[RequestSizeLimit(4096)]`
  - [ ] `DELETE /applications/{id:guid}` — `[Authorize(Roles = "Owner")]` (withdraw)
  - [ ] `GET /applications/mine` — `[Authorize]`
  - [ ] `POST /fairs` — `[Authorize(Roles = "Ally")]`, `[RequestSizeLimit(8192)]`
  - [ ] Request body records: `ApplyToAdoptRequest`, `ReviewApplicationRequest`, `PublishAdoptablePetRequest`, `UpdateAdoptablePetRequest`, `CreateAdoptionFairRequest`
  - [ ] Todos los errores retornan `BadRequest(new ProblemDetails { ... })` con `Detail = string.Join("; ", result.Errors)`

### 1.11 Tests — Sprint 1

- [ ] Crear directorio `tests/PawTrack.UnitTests/Adoptions/`
- [ ] `AdoptablePetTests.cs`
  - [ ] `NewAnimal_HasAvailableStatus`
  - [ ] `AddPhoto_BeyondLimit_Throws`
  - [ ] `AddPhoto_UpToLimit_Succeeds`
  - [ ] `MarkAdopted_SetsStatusAndTimestamp`
  - [ ] `MarkAdopted_FromAvailable_Succeeds` (via MarkInProcess → MarkAdopted)
  - [ ] `Republish_FromPaused_SetsAvailable`
  - [ ] `UpdateDetails_ChangesFields`
- [ ] `AdoptionApplicationTests.cs`
  - [ ] `NewApplication_HasPendingStatus`
  - [ ] `Approve_SetsApprovedAndTimestamp`
  - [ ] `Reject_WithNote_SetsNote`
  - [ ] `Withdraw_SetsWithdrawn`
- [ ] `AdoptionFairTests.cs`
  - [ ] `CreateFair_EndsBeforeStarts_Throws`
  - [ ] `CreateFair_ValidDates_HasUpcomingStatus`
  - [ ] `AddAnimal_NoDuplicates`
  - [ ] `IsCurrentlyActive_WhenActiveAndInTimeRange_ReturnsTrue`
- [ ] `PublishAdoptablePetCommandHandlerTests.cs`
  - [ ] `Handle_NotVerifiedShelter_ReturnsFailure`
  - [ ] `Handle_VerifiedShelterWrongType_ReturnsFailure`
  - [ ] `Handle_ValidShelter_PublishesAndSaves`
  - [ ] `Handle_ValidShelter_ReturnsCorrectOrgName`
- [ ] `ApplyToAdoptCommandHandlerTests.cs`
  - [ ] `Handle_AnimalNotFound_ReturnsFailure`
  - [ ] `Handle_AnimalNotAvailable_ReturnsFailure`
  - [ ] `Handle_DuplicatePendingApplication_ReturnsFailure`
  - [ ] `Handle_ValidApplication_SavesAndNotifiesFireAndForget`
- [ ] `ReviewAdoptionApplicationCommandHandlerTests.cs`
  - [ ] `Handle_NotVerifiedShelter_ReturnsFailure`
  - [ ] `Handle_ApplicationNotFound_ReturnsFailure`
  - [ ] `Handle_WrongOrganization_ReturnsFailure`
  - [ ] `Handle_Approve_SetsInProcessAndNotifies`
  - [ ] `Handle_Reject_SetsRejectedAndNotifies`

---

## Sprint 2 — Upload de fotos, notificaciones y gestión de aplicaciones (1 semana)

### 2.1 Upload de fotos

- [ ] Crear `UploadAdoptionPhotoCommand` + `Handler` en `AdoptionCommands.cs`
  - [ ] Aceptar `Guid OrganizationUserId`, `Guid AdoptablePetId`, `IFormFile Photo`
  - [ ] Ownership check: animal.OrganizationUserId == requestingUserId
  - [ ] Usar `BlobHelper.SanitizeFileName(photo.FileName)`
  - [ ] Path: `adoption-photos/{animalId}/{Guid.CreateVersion7()}-{sanitized}`
  - [ ] Llamar `IBlobStorageService.UploadAsync(blobName, stream, contentType, ct)`
  - [ ] Llamar `animal.AddPhoto(url)` — guard de 5 fotos ya está en el dominio
  - [ ] Retornar la URL del blob
- [ ] Añadir `POST /animals/{id:guid}/photos` al controller
  - [ ] `[Authorize(Roles = "Ally")]`
  - [ ] `[RequestSizeLimit(5_242_880)]` (5 MB)
  - [ ] Inyectar `IBlobStorageService` en el constructor del controller
  - [ ] Validar que `photo` no es null y `photo.Length > 0`
- [ ] Crear `DeleteAdoptionPhotoCommand` + `Handler`
  - [ ] Ownership check
  - [ ] Llamar `IBlobStorageService.DeleteAsync(blobPath, ct)`
  - [ ] Llamar `animal.RemovePhoto(url)`
- [ ] Añadir `DELETE /animals/{id:guid}/photos` al controller

### 2.2 Notificaciones — extender interfaz e implementación

- [ ] Extender `INotificationDispatcher.cs` — añadir los 4 métodos nuevos:
  - [ ] `DispatchAdoptionInterestAsync(shelterUserId, animalName, applicationId)`
  - [ ] `DispatchAdoptionApprovedAsync(applicantUserId, animalName, applicationId)`
  - [ ] `DispatchAdoptionRejectedAsync(applicantUserId, animalName, applicationId)`
  - [ ] `DispatchAdoptionFairAlertAsync(fairId, fairTitle, lat, lng, radiusMetres, fairStartsAt)`
- [ ] Implementar los 4 métodos en `Infrastructure/Notifications/NotificationDispatcher.cs`
  - [ ] `DispatchAdoptionInterestAsync`:
    - [ ] Crear in-app `Notification.Create(shelterUserId, NotificationType.AdoptionInterest, ...)`
    - [ ] Enviar email al shelter via `IEmailSender`
    - [ ] `notificationRepository.AddAsync(notification)`
    - [ ] `unitOfWork.SaveChangesAsync()`
  - [ ] `DispatchAdoptionApprovedAsync`:
    - [ ] Crear in-app notification para el adoptante
    - [ ] Enviar email al adoptante
    - [ ] Push notification si tiene suscripción activa
  - [ ] `DispatchAdoptionRejectedAsync`:
    - [ ] In-app + email al adoptante
  - [ ] `DispatchAdoptionFairAlertAsync`:
    - [ ] Obtener usuarios con push subscription activa en el bounding box
    - [ ] Aplicar rate limiting via `INotificationRateLimitService`
    - [ ] Enviar push notifications a cada usuario elegible
    - [ ] Registrar en `GeofencedAlertLog` la cantidad de usuarios notificados

### 2.3 Frontend — Shelter Dashboard (gestión de animales y aplicaciones)

- [ ] Crear `frontend/src/features/adoptions/` (estructura de directorios)
- [ ] Crear `api/adoptionsApi.ts`
  - [ ] Tipos: `PetSpecies`, `PetSize`, `AgeCategory`, `AdoptionStatus`, `ApplicationStatus`, `FairStatus`
  - [ ] Interfaces: `AdoptablePetDto`, `AdoptionApplicationDto`, `AdoptionFairDto`, `AdoptionFilters`, `PagedAdoptions`
  - [ ] Funciones: `getAnimals`, `getAnimalsForMap`, `getAnimal`, `publishAnimal`, `updateAnimal`, `uploadPhoto`, `deletePhoto`, `getMyAnimals`, `applyToAdopt`, `withdrawApplication`, `getApplicationsForAnimal`, `reviewApplication`, `markAdopted`, `getMyApplications`, `getFairs`, `createFair`
- [ ] Crear `hooks/useAdoptions.ts`
  - [ ] `useAdoptableAnimals(filters)` — staleTime 3 min
  - [ ] `useAdoptableAnimalsForMap()` — staleTime 5 min
  - [ ] `useAdoptableAnimal(id)` — staleTime 2 min
  - [ ] `useMyAdoptionAnimals(page, pageSize)` — staleTime 1 min
  - [ ] `usePublishAnimal()` — invalida `["adoptions", "mine"]`
  - [ ] `useUpdateAnimal()` — invalida el animal específico
  - [ ] `useUploadAdoptionPhoto()` — invalida el animal específico
  - [ ] `useDeleteAdoptionPhoto()` — invalida el animal específico
  - [ ] `useApplyToAdopt()` — invalida `["adoptions", "applications", "mine"]`
  - [ ] `useWithdrawApplication()` — invalida aplicaciones
  - [ ] `useApplicationsForAnimal(animalId)` — staleTime 30s
  - [ ] `useReviewApplication()` — invalida aplicaciones
  - [ ] `useMarkAdopted()` — invalida mine + animals
  - [ ] `useMyAdoptionApplications(page)` — staleTime 1 min
  - [ ] `useUpcomingFairs(lat?, lng?, radiusKm?)` — staleTime 5 min
  - [ ] `useCreateFair()` — invalida fairs

- [ ] Crear `pages/ShelterDashboardPage.tsx`
  - [ ] Tabs: "Mis animales" | "Aplicaciones" | "Ferias"
  - [ ] Tab "Mis animales": listado paginado con status badge, botón editar, fotos
  - [ ] Botón "Publicar nuevo animal" → navega a ShelterPublishPage
  - [ ] Actions por animal: Pausar / Republicar / Marcar Adoptado
- [ ] Crear `pages/ShelterPublishPage.tsx`
  - [ ] Form multi-paso: (1) Info básica, (2) Características, (3) Fotos
  - [ ] Step 1: nombre, especie, raza, tamaño, categoría de edad, meses aproximados
  - [ ] Step 2: historia, requisitos, notas médicas, checkboxes (vacunado, castrado, microchip, OK con niños/perros/gatos, necesita patio)
  - [ ] Step 3: Coordenadas de referencia (map picker), etiqueta de zona
  - [ ] Submit llama `usePublishAnimal()` → redirect a ShelterDashboardPage
  - [ ] Fotos: upload después de crear el animal con `useUploadAdoptionPhoto()`
- [ ] Crear `pages/ShelterApplicationsPage.tsx`
  - [ ] Lista de aplicaciones de un animal específico
  - [ ] Card por aplicante: nota, estado, fecha
  - [ ] Botones: Aprobar / Rechazar con campo de nota opcional
  - [ ] Llamar `useReviewApplication()`

### 2.4 Frontend — Aplicante (Owner)

- [ ] Crear `pages/MyAdoptionApplicationsPage.tsx`
  - [ ] Listado de mis aplicaciones con estado, animal, fecha
  - [ ] Botón "Retirar solicitud" si status == Pending
  - [ ] Link al perfil del animal
- [ ] Crear `components/ApplyDrawer.tsx`
  - [ ] Drawer lateral (patrón `CartDrawer.tsx`)
  - [ ] Campo de texto "¿Por qué quieres adoptar a este animal?"
  - [ ] Botón submit llama `useApplyToAdopt()`
  - [ ] Deshabilitar si ya hay aplicación activa del usuario

### 2.5 Tests — Sprint 2

- [ ] `UploadAdoptionPhotoCommandHandlerTests.cs`
  - [ ] `Handle_OwnershipFails_ReturnsFailure`
  - [ ] `Handle_MaxPhotosReached_ReturnsFailure`
  - [ ] `Handle_Valid_UploadsToBlobAndSaves`
- [ ] `ReviewAdoptionApplicationCommandHandlerTests.cs` (completar)
  - [ ] `Handle_Approve_MarksAnimalInProcess`
  - [ ] `Handle_Approve_FiresNotification`

---

## Sprint 3 — Directorio público, mapa y ferias (1 semana)

### 3.1 Frontend — Directorio público

- [ ] Crear `components/AnimalCard.tsx`
  - [ ] Foto principal, nombre, especie, tamaño, edad, zona de referencia
  - [ ] Badges: "Vacunado", "Castrado", "OK con niños" (si aplica)
  - [ ] Badge de estado: `Available` = verde, `InProcess` = amarillo, `Adopted` = gris
  - [ ] Click → navega a `/adopciones/:id`
  - [ ] Hover: elevación suave (patrón `StoreDetailSheet`)
- [ ] Crear `components/AdoptionFiltersBar.tsx`
  - [ ] Select: Especie (Todos/Perro/Gato/Conejo/Pájaro/Otro)
  - [ ] Select: Tamaño (Todos/XS/S/M/L/XL)
  - [ ] Select: Edad (Todos/Cachorro/Joven/Adulto/Senior)
  - [ ] Checkboxes: Vacunado, Castrado, OK con niños, OK con perros
  - [ ] Botón "Mi zona" — usa GPS del browser, llama a `navigator.geolocation`
  - [ ] Input de radio en km (por defecto 50)
  - [ ] Botón "Limpiar filtros"
- [ ] Crear `pages/AdoptionDirectoryPage.tsx`
  - [ ] `AdoptionFiltersBar` en el top
  - [ ] Grid de `AnimalCard` components
  - [ ] Paginación: Anterior / Siguiente + contador "X de Y animales"
  - [ ] Loading skeleton (patrón existente `Skeleton`)
  - [ ] Estado vacío: "No encontramos animales con estos filtros"
  - [ ] URL params: sincronizar filtros con `?species=Dog&size=Medium&page=2`
- [ ] Crear `pages/AdoptionDetailPage.tsx`
  - [ ] Galería de fotos (thumbnails + imagen grande)
  - [ ] Sección de info: nombre, especie, raza, tamaño, edad, pesos (si aplica)
  - [ ] Historia y personalidad
  - [ ] Características: badges de salud, comportamiento
  - [ ] Requisitos para el adoptante
  - [ ] Organización: nombre del shelter
  - [ ] Zona de referencia en mapa pequeño (Leaflet)
  - [ ] Botón "Quiero adoptarlo" → abre `ApplyDrawer`
  - [ ] Botón deshabilitado si ya hay aplicación pendiente del usuario
  - [ ] Si usuario no autenticado: "Inicia sesión para aplicar"

### 3.2 Mapa público — integración de pines de adopción

- [ ] Crear `components/AdoptionMarker.tsx` (patrón `StoreMarker.tsx`)
  - [ ] Pin de color diferente a los de mascotas perdidas (ej. verde/morado)
  - [ ] Ícono distinto (🐾 o icono de corazón)
  - [ ] `eventHandlers.click` → callback `onAdoptionClick(animalId)`
  - [ ] Tooltip con nombre del animal + especie
- [ ] Extender `PublicMapPage.tsx`
  - [ ] Añadir toggle "Adopciones" junto a "Tiendas"
  - [ ] `useAdoptableAnimalsForMap(showAdoptions)` — solo cargar cuando toggle activo
  - [ ] Renderizar `AdoptionMarker` para cada animal
  - [ ] Al hacer click → abrir `AnimalDetailSheet` o navegar a `/adopciones/:id`
  - [ ] Añadir `BillboardBanner placement="Adoption"` cuando toggle activo (Sprint 4)
- [ ] Añadir `Adoption` al enum `BillboardPlacement` en el backend (Sprint 4)

### 3.3 Ferias de adopción

- [ ] Crear `components/FairCard.tsx`
  - [ ] Título, organización, fecha y hora
  - [ ] Lugar (badge de zona)
  - [ ] Número de animales que estarán presentes
  - [ ] Badge de estado: Próxima / En progreso / Finalizada
  - [ ] Botón "Ver en mapa" → navega a mapa con pin del evento
- [ ] Crear `pages/AdoptionFairsPage.tsx`
  - [ ] Lista de ferias próximas ordenadas por fecha
  - [ ] Filtro por zona (GPS o selección manual)
  - [ ] `FairCard` por feria
  - [ ] Estado vacío: "No hay ferias de adopción próximas en tu zona"
- [ ] Extender `ShelterDashboardPage.tsx`
  - [ ] Tab "Ferias": lista de ferias creadas por el shelter
  - [ ] Botón "Crear nueva feria"
  - [ ] Form de creación: título, descripción, lugar (map picker), fecha inicio, fecha fin, selección de animales del shelter

### 3.4 API — endpoints de feria

- [ ] Añadir al controller:
  - [ ] `PATCH /fairs/{id:guid}/activate` — `[Authorize(Roles = "Ally")]`
  - [ ] `PATCH /fairs/{id:guid}/finish` — `[Authorize(Roles = "Ally")]`
  - [ ] `PATCH /fairs/{id:guid}/cancel` — `[Authorize(Roles = "Ally")]`
  - [ ] `PATCH /fairs/{id:guid}/animals` — añadir/quitar animales de la feria
- [ ] Crear `ActivateFairCommand`, `FinishFairCommand`, `CancelFairCommand` + handlers
- [ ] Crear `UpdateFairAnimalsCommand` + handler

### 3.5 Routing

- [ ] Editar `frontend/src/app/routes.tsx`
  - [ ] Importaciones lazy para las 7 páginas nuevas
  - [ ] Rutas en `PublicLayout`: `/adopciones`, `/adopciones/ferias`, `/adopciones/:id`
  - [ ] Ruta en `AuthenticatedLayout`: `/mis-adopciones`
  - [ ] Rutas en `AuthenticatedLayout` con `RoleGuard role="Ally"`: `/shelter/dashboard`, `/shelter/publicar`, `/shelter/animales/:id/aplicaciones`

### 3.6 Tests — Sprint 3

- [ ] `CreateAdoptionFairCommandHandlerTests.cs`
  - [ ] `Handle_NotShelter_ReturnsFailure`
  - [ ] `Handle_InvalidDates_ValidationFails`
  - [ ] `Handle_Valid_CreatesAndDispatchesGeofenceAlert`
- [ ] `GetAdoptablePetsQueryHandlerTests.cs`
  - [ ] `Handle_NoFilters_ReturnsPagedResult`
  - [ ] `Handle_SpeciesFilter_FiltersCorrectly`
  - [ ] `Handle_GeoFilter_AppliesBoundingBox`
  - [ ] `Handle_BatchLoadsOrgNames`

---

## Sprint 4 — Monetización, WhatsApp y polish (1 semana)

### 4.1 Subscriptions — nuevos tiers

- [ ] Extender `SubscriptionTier.cs` — añadir al enum:
  - [ ] `ShelterBasic = 300`
  - [ ] `ShelterPlus = 310`
- [ ] Actualizar tabla de pricing en la documentación de subscriptions si existe
- [ ] Definir precios en `SubscriptionService` o constantes:
  - [ ] `ShelterBasic` = ₡0 (gratis)
  - [ ] `ShelterPlus` = ₡8,000/mes

### 4.2 Feature gating en comandos

- [ ] Inyectar `ISubscriptionService` en `PublishAdoptablePetCommandHandler`
- [ ] Añadir gating en `PublishAdoptablePetCommandHandler.Handle()`:
  ```
  var activeCount = await adoptionRepository.CountByOrganizationAsync(...);
  var hasPlus = subscriptionService.IsActive(orgUserId, SubscriptionTier.ShelterPlus);
  if (!hasPlus && activeCount >= 5) return Failure("shelter_basic_limit_reached");
  ```
- [ ] Añadir gating en `CreateAdoptionFairCommandHandler.Handle()`:
  - [ ] Solo `ShelterPlus` puede crear ferias
  - [ ] Retornar `"shelter_plus_required"` si no tiene plan
- [ ] Añadir `"shelter_basic_limit_reached"` y `"shelter_plus_required"` a los `internal const` de cada handler

### 4.3 WhatsApp Bot

- [ ] Editar `Application/Bot/HandleWhatsAppWebhookCommandHandler.cs`
- [ ] Añadir intent `"buscar_adopcion"` / `"quiero_adoptar"`:
  - [ ] Responder con URL del directorio + URL de ferias
  - [ ] Texto en español, con emojis apropiados
- [ ] Añadir intent `"tengo_animales"` / `"quiero_dar_en_adopcion"`:
  - [ ] Explicar el proceso de registro como Shelter Ally
  - [ ] Dar URL de registro de aliados
- [ ] Añadir al entrenamiento del bot (si hay archivo de intents) los nuevos patrones de reconocimiento
- [ ] Verificar que `IPublicAppUrlProvider.GetBaseUrl()` retorna la URL correcta según entorno

### 4.4 Billboard placement para adopciones

- [ ] Extender `BillboardPlacement` enum — añadir: `Adoption = 4`
- [ ] Crear migración para el nuevo valor si el enum está guardado como int en DB
- [ ] Actualizar `AdoptionDirectoryPage.tsx` — añadir `<BillboardBanner placement="Adoption" />`
- [ ] Crear admin UI para gestionar vallas de adopción (si no se reutiliza el panel existente)

### 4.5 Panel de administración

- [ ] Crear `GetAdoptionStatsQuery` + handler (admin):
  - [ ] Total animales publicados (all time)
  - [ ] Total adoptados
  - [ ] Total aplicaciones
  - [ ] Tasa de conversión (adoptados / publicados)
  - [ ] Ferias realizadas
- [ ] Añadir tab "Adopciones" en `AdminPage.tsx` o similar
  - [ ] Stats cards
  - [ ] Lista de todos los animales con filtros por status
  - [ ] Botón "Eliminar" / "Suspender" para moderación
  - [ ] Lista de shelters activos con conteos

### 4.6 Seguridad — revisión final

- [ ] Verificar BOLA en todos los handlers que mutam estado:
  - [ ] `PublishAdoptablePetCommand`: shelter check via `GetVerifiedByUserIdAsync`
  - [ ] `UpdateAdoptablePetCommand`: `animal.OrganizationUserId == requestingUserId`
  - [ ] `UploadAdoptionPhotoCommand`: ownership check
  - [ ] `ReviewAdoptionApplicationCommand`: ownership check
  - [ ] `MarkAdoptedCommand`: ownership check
  - [ ] `WithdrawApplicationCommand`: `application.ApplicantUserId == requestingUserId`
- [ ] Verificar que coordenadas de referencia del shelter NO exponen la dirección exacta
- [ ] Verificar que `AdoptionApplicationDto` NO expone el email del aplicante al shelter
- [ ] Verificar que el directorio público NO filtra por campos internos (solo campos explícitamente expuestos)
- [ ] `[RequestSizeLimit]` en todos los endpoints con body

### 4.7 Performance

- [ ] Verificar que `GetAvailablePagedAsync` usa bounding box antes de `CountAsync` (evitar full scan)
- [ ] Añadir índice filtrado en SQL para `Status = 'Available'` si el volumen de adoptados crece:
  - [ ] Agregar como índice en `AdoptablePetConfiguration`: `b.HasIndex(a => a.Status).HasFilter("[Status] = 'Available'")`
- [ ] Verificar que `GetAvailableAllAsync` tiene un hard cap de 500 registros
- [ ] `staleTime` apropiados en todos los hooks (directorio: 3 min, detalle: 2 min, mapa: 5 min)

### 4.8 Tests — Sprint 4

- [ ] `MarkAdoptedCommandHandlerTests.cs`
  - [ ] `Handle_OwnershipFails_ReturnsFailure`
  - [ ] `Handle_Valid_SetsAdoptedAndSaves`
- [ ] `WithdrawApplicationCommandHandlerTests.cs`
  - [ ] `Handle_WrongApplicant_ReturnsFailure`
  - [ ] `Handle_AlreadyApproved_ReturnsFailure`
  - [ ] `Handle_Valid_SetsWithdrawn`
- [ ] Tests de integración: `AdoptionFlowIntegrationTests.cs`
  - [ ] Flujo completo: publish → apply → review approve → mark adopted
  - [ ] Flujo rechazo: publish → apply → review reject → applicant withdraws
  - [ ] Límite de plan: shelter basic intenta publicar 6+ animales

---

## Transversal — Aplica a todos los sprints

### Infraestructura y configuración

- [ ] Crear contenedor Azure Blob Storage: `adoption-photos` (si no existe ya en el mismo storage account)
- [ ] Verificar que la cadena de conexión del Blob Storage en `appsettings.Development.json` permite el nuevo contenedor
- [ ] Configurar CORS en el storage account si las fotos se sirven directamente desde el CDN
- [ ] Configurar Content-Type correcto para uploads de imagen en `IBlobStorageService`

### Documentación técnica

- [ ] Actualizar `docs/MANUAL_ALIADOS.md` — sección "Módulo de Adopciones"
  - [ ] Cómo registrarse como shelter
  - [ ] Cómo publicar un animal con fotos
  - [ ] Cómo gestionar aplicaciones
  - [ ] Cómo crear ferias
- [ ] Actualizar `docs/MANUAL_USUARIO.md` — sección "Adoptar una mascota"
  - [ ] Cómo buscar animales en adopción
  - [ ] Cómo aplicar para adoptar
  - [ ] Qué esperar después de aplicar
- [ ] Actualizar `docs/MANUAL_ADMINISTRADOR.md` — sección "Gestión de Adopciones"
  - [ ] Moderación de contenido
  - [ ] Ver estadísticas
- [ ] Actualizar `docs/MANUAL_TECNICO.md` — añadir módulo Adoptions en la arquitectura
- [ ] Actualizar `docs/FEATURES.md` — añadir sección "Módulo de Adopciones"

### Git / CI

- [ ] Rama feature: `feature/adopciones-core` (Sprint 1)
- [ ] Rama feature: `feature/adopciones-fotos-notificaciones` (Sprint 2)
- [ ] Rama feature: `feature/adopciones-directorio-mapa` (Sprint 3)
- [ ] Rama feature: `feature/adopciones-monetizacion` (Sprint 4)
- [ ] Verificar que los tests corren en CI sin fallo en cada PR
- [ ] Merge a `main` al final de cada sprint con PR review

---

## Checklist de definición de "hecho" (DoD) por tarea backend

Cada command/query handler se considera completo cuando:

- [ ] Compila sin errores (0 CS warnings relevantes)
- [ ] Tiene validator con FluentValidation si acepta input de usuario
- [ ] Tiene tests unitarios con NSubstitute que cubren el happy path y al menos 2 casos de error
- [ ] Los string de error son `internal const` (para tests que verifican el mensaje exacto)
- [ ] Los efectos secundarios (notificaciones, emails) son fire-and-forget con `.ContinueWith` para logging de fallos
- [ ] No hay lógica de negocio en el controller — solo dispatch + mapeo de Result a HTTP

## Checklist de definición de "hecho" por tarea frontend

Cada página/componente se considera completo cuando:

- [ ] TypeScript strict: 0 errores `tsc`
- [ ] Loading state con `Skeleton` component
- [ ] Error state con mensaje amigable
- [ ] Empty state con mensaje descriptivo
- [ ] Hooks usan `queryKey` arrays correctos para invalidación granular
- [ ] Mutaciones muestran toast de éxito/error via `sonner`
- [ ] Funciona en mobile (responsive)
- [ ] Lazy import registrado en `routes.tsx`
