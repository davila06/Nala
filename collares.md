# Guía de implementación: Microchip + Collar GPS

> Estado actual: **Microchip — domain layer completo, faltan 4 wires.**  
> Estado actual: **Collar GPS — nada existe aún, arquitectura aquí.**

---

## Parte 1 — Microchip ISO 11784

### Qué ya existe (no tocar)

| Capa | Archivo | Qué hace |
|------|---------|----------|
| Domain | `Pet.cs` | Propiedad `MicrochipId (string?)` + método `SetMicrochip(string)` que normaliza a uppercase |
| Infrastructure | `PetRepository.cs` | `GetByMicrochipIdAsync(string)` — busca mascota por chip |
| Infrastructure | `PetConfiguration.cs` | Columna `nvarchar(15)`, índice único filtrado `WHERE MicrochipId IS NOT NULL` |
| Interface | `IPetRepository.cs` | Expone `GetByMicrochipIdAsync` |
| Migration | `20260405152807_AddClinicsModule.cs` | Columna + índice ya aplicados en DB |

**La columna existe en la DB. No se necesita migración.**

---

### Cambios necesarios — 8 archivos

#### 1. `UpdatePetCommand.cs` — agregar parámetro

```csharp
// backend/src/PawTrack.Application/Pets/Commands/UpdatePet/UpdatePetCommand.cs
public sealed record UpdatePetCommand(
    Guid PetId,
    Guid RequestingUserId,
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly? BirthDate,
    byte[]? PhotoBytes,
    string? PhotoContentType,
    string? PhotoFileName,
    string? MicrochipId)   // ← nuevo
    : IRequest<Result<PetId>>;
```

#### 2. `UpdatePetCommandValidator.cs` — regla ISO 11784

```csharp
// agregar dentro del constructor, después de la regla de BirthDate:
RuleFor(x => x.MicrochipId)
    .Matches(@"^\d{10,15}$")
    .WithMessage("El microchip debe ser de 10 a 15 dígitos numéricos (ISO 11784).")
    .When(x => x.MicrochipId is not null);
```

#### 3. `UpdatePetCommandHandler.cs` — llamar SetMicrochip

```csharp
// después de pet.Update(...)
if (request.MicrochipId is not null)
    pet.SetMicrochip(request.MicrochipId);
```

**Nota:** `SetMicrochip("")` setea `null`, por lo que si el dueño quiere borrar el chip puede enviar `""`.

#### 4. `CreatePetCommand.cs` — agregar parámetro

```csharp
public sealed record CreatePetCommand(
    Guid OwnerId,
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly? BirthDate,
    byte[]? PhotoBytes,
    string? PhotoContentType,
    string? PhotoFileName,
    string? MicrochipId)   // ← nuevo
    : IRequest<Result<string>>;
```

#### 5. `CreatePetCommandHandler.cs` — SetMicrochip tras crear

```csharp
var pet = Pet.Create(request.OwnerId, request.Name, request.Species,
                     request.Breed, request.BirthDate);

if (!string.IsNullOrWhiteSpace(request.MicrochipId))
    pet.SetMicrochip(request.MicrochipId);
```

#### 6. `PetDto.cs` — exponer en respuesta

```csharp
public sealed record PetDto(
    string Id,
    string OwnerId,
    string Name,
    string Species,
    string? Breed,
    string? BirthDate,
    string? PhotoUrl,
    string Status,
    string? MicrochipId,   // ← nuevo
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PetDto FromDomain(Pet pet) => new(
        pet.Id.ToString(),
        pet.OwnerId.ToString(),
        pet.Name,
        pet.Species.ToString(),
        pet.Breed,
        pet.BirthDate?.ToString("yyyy-MM-dd"),
        pet.PhotoUrl,
        pet.Status.ToString(),
        pet.MicrochipId,   // ← nuevo
        pet.CreatedAt,
        pet.UpdatedAt);
}
```

#### 7. `PetsController.cs` — actualizar los request records y dispatch

```csharp
// Línea ~301 — UpdatePetRequest record
public sealed record UpdatePetRequest(
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly? BirthDate,
    IFormFile? Photo,
    string? MicrochipId);   // ← nuevo

// CreatePetRequest igual:
public sealed record CreatePetRequest(
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly? BirthDate,
    IFormFile? Photo,
    string? MicrochipId);   // ← nuevo

// En UpdatePet action — agregar al dispatch del command:
var command = new UpdatePetCommand(
    id, userId,
    request.Name, request.Species, request.Breed, request.BirthDate,
    photoBytes, contentType, fileName,
    request.MicrochipId);   // ← nuevo

// En CreatePet action — igual:
var command = new CreatePetCommand(
    userId,
    request.Name, request.Species, request.Breed, request.BirthDate,
    photoBytes, contentType, fileName,
    request.MicrochipId);   // ← nuevo
```

#### 8. `petsApi.ts` + `PetForm.tsx` — frontend

```typescript
// petsApi.ts — agregar a interfaces
export interface CreatePetRequest {
  name: string
  species: PetSpecies
  breed?: string
  birthDate?: string
  photo?: File
  microchipId?: string       // ← nuevo
}

export interface PetDetail {
  // ...existentes...
  microchipId: string | null  // ← nuevo
}
```

```typescript
// PetForm.tsx — agregar campo después de birthDate
// Agregar microchipId a PetFormValues:
export interface PetFormValues {
  name: string
  species: PetSpecies
  breed: string
  birthDate: string
  microchipId: string        // ← nuevo
  photo: File | null
}

// En handleSubmit:
const data: CreatePetRequest = {
  // ...
  microchipId: (fd.get('microchipId') as string).trim() || undefined,  // ← nuevo
}
```

```tsx
{/* Microchip — agregar en el JSX después de birthDate */}
<div className="space-y-1">
  <label htmlFor="pet-microchip" className="block text-sm font-medium text-sand-700">
    Número de microchip
    <span className="ml-1.5 text-xs text-sand-400 font-normal">(ISO 11784, opcional)</span>
  </label>
  <input
    id="pet-microchip"
    name="microchipId"
    type="text"
    inputMode="numeric"
    maxLength={15}
    pattern="\d{10,15}"
    defaultValue={defaultValues?.microchipId}
    placeholder="Ej. 985141000123456"
    className="block w-full rounded-xl border border-sand-300 px-3.5 py-2.5 text-sm shadow-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200"
  />
  <p className="text-xs text-sand-400">10–15 dígitos. Lo registra tu veterinaria.</p>
</div>
```

---

### Endpoint extra: búsqueda por microchip para clínicas

Cuando una veterinaria escanea un chip con su lector, necesita saber a quién pertenece la mascota. La repo ya tiene `GetByMicrochipIdAsync` — solo falta el command/query y el endpoint.

**Nuevo query:**

```csharp
// Application/Pets/Queries/GetPetByMicrochip/GetPetByMicrochipQuery.cs
public sealed record GetPetByMicrochipQuery(string ChipId)
    : IRequest<Result<PetDto>>;

// Handler
public sealed class GetPetByMicrochipQueryHandler(IPetRepository repo)
    : IRequestHandler<GetPetByMicrochipQuery, Result<PetDto>>
{
    public async Task<Result<PetDto>> Handle(
        GetPetByMicrochipQuery request, CancellationToken ct)
    {
        var normalized = request.ChipId.Trim().ToUpperInvariant();
        var pet = await repo.GetByMicrochipIdAsync(normalized, ct);
        return pet is null
            ? Result.Failure<PetDto>("Microchip no registrado.")
            : Result.Success(PetDto.FromDomain(pet));
    }
}
```

**Nuevo endpoint en `PetsController.cs`:**

```csharp
// GET /api/pets/by-microchip/{chipId}
[HttpGet("by-microchip/{chipId}")]
[Authorize]                               // Requiere autenticación; en Sprint 2 restringir a rol Clinic
[EnableRateLimiting("public-api")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetByMicrochip(
    string chipId, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(chipId) || chipId.Length > 15)
        return BadRequest(new ProblemDetails { Title = "Chip ID inválido", Status = 400 });

    var result = await sender.Send(
        new GetPetByMicrochipQuery(chipId), cancellationToken);

    return result.IsFailure
        ? NotFound(new ProblemDetails { Title = "Microchip no registrado", Status = 404 })
        : Ok(result.Value);
}
```

---

### Checklist completo — Microchip

```
[ ] UpdatePetCommand.cs        — agregar string? MicrochipId
[ ] UpdatePetCommandValidator  — regla \d{10,15}
[ ] UpdatePetCommandHandler    — llamar pet.SetMicrochip(request.MicrochipId)
[ ] CreatePetCommand.cs        — agregar string? MicrochipId
[ ] CreatePetCommandHandler    — SetMicrochip si no es null
[ ] PetDto.cs                  — agregar MicrochipId en record + FromDomain
[ ] PetsController.cs          — UpdatePetRequest + CreatePetRequest + dispatch
[ ] GetPetByMicrochipQuery     — query + handler (3 archivos nuevos)
[ ] PetsController.cs          — endpoint GET /api/pets/by-microchip/{chipId}
[ ] petsApi.ts                 — microchipId en CreatePetRequest + PetDetail
[ ] PetForm.tsx                — campo input microchipId
[ ] PetDetailPage.tsx          — mostrar el chip si existe
```

**Estimado:** 1.5 días de trabajo, sin pruebas. Con pruebas: 3 días.

---

---

## Parte 2 — Collar GPS

### Contexto del mercado y decisión de arquitectura

#### Opción A: Integrar API de tracker existente (recomendado para MVP)

| Tracker | API pública | Precio HW | Disponible CR |
|---------|------------|----------|--------------|
| **Tractive** | Sí — REST + WebSockets | $50–70 | Sí (importado) |
| **Kippy** | Sí — REST | $80–100 | Sí |
| **Whistle** | Limitada | $80 | Solo USA |
| **Fi Collar** | No pública | $150 | No |

**Tractive** es la opción correcta para arrancar: tienen API documentada, webhooks de posición, mercado de expats en CR ya los usa.

El flujo con Tractive:
1. Dueño ya tiene collar Tractive
2. En PawTrack: conecta su cuenta Tractive (OAuth2)
3. PawTrack almacena el access token cifrado en Key Vault
4. PawTrack consulta posición vía Tractive API en background job cada 5 min
5. La posición se muestra en el mapa del perfil de la mascota

**Ventaja**: cero hardware, cero firmware, validación del mercado en ~3 semanas.

---

#### Opción B: Collar propio con firmware PawTrack

Para cuando haya tracción real. Hardware mínimo viable:

| Componente | Modelo | Precio unitario |
|------------|--------|----------------|
| Microcontrolador + modem | SIMCom A7670E (LTE Cat-M1 + GPS integrado) | $18–22 |
| Batería LiPo | 1400mAh flat | $6–8 |
| PCB + enclosure | Diseño custom, impresión 3D o molde | $8–15 |
| SIM (KORE/Twilio IoT) | eSIM + plan datos IoT CR | $2–4/mes/unidad |
| **Total BOM** | | **~$35–50 + $3/mes** |

Precio de venta sugerido: **$89–99** con 6 meses de datos incluidos.

En CR, Kolbi y Claro tienen cobertura LTE-M desde 2024 para IoT.

---

### Modelo de datos — Módulo `Collars`

> **Principio**: las localizaciones son time series de alta frecuencia. No usar EF tracking completo para inserts. Insertar con `ExecuteSqlRawAsync` o via tabla particionada.

#### Entidades

```csharp
// Domain/Collars/Collar.cs
public sealed class Collar
{
    private Collar() { }

    public Guid Id { get; private set; }          // PK
    public Guid PetId { get; private set; }        // FK → Pets
    public string DeviceId { get; private set; }   // IMEI o Tractive device ID
    public CollarProvider Provider { get; private set; }  // Own, Tractive, Kippy
    public string? DeviceKeyHash { get; private set; }    // PBKDF2 del secreto del firmware (solo si Provider == Own)
    public string? ExternalToken { get; private set; }    // OAuth token cifrado (Tractive/Kippy)
    public byte? BatteryPercent { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public string? FirmwareVersion { get; private set; }  // solo collar propio

    // Métodos de dominio
    public static Collar Register(Guid petId, string deviceId, CollarProvider provider) { ... }
    public void UpdateBattery(byte percent) { ... }
    public void UpdateLastSeen() { ... }
    public void Deactivate() { ... }
    public bool MatchesDeviceKey(string plainKey) { ... }  // PBKDF2 verify
}

public enum CollarProvider { Own, Tractive, Kippy }
```

```csharp
// Domain/Collars/CollarLocation.cs
// NOTA: esta entidad es write-heavy. No rastrear con EF Change Tracker.
public sealed class CollarLocation
{
    public Guid Id { get; private set; }
    public Guid CollarId { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public float? AccuracyMeters { get; private set; }
    public float? SpeedKmh { get; private set; }
    public byte? BatteryAtCapture { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }   // índice principal
}
```

#### Configuración EF Core

```csharp
// Infrastructure/Persistence/Configurations/CollarConfiguration.cs
builder.ToTable("Collars");
builder.HasIndex(c => c.DeviceId).IsUnique();
builder.HasIndex(c => c.PetId);
builder.Property(c => c.ExternalToken).HasMaxLength(2048);  // token cifrado

// CollarLocationConfiguration.cs
builder.ToTable("CollarLocations");
builder.HasIndex(l => new { l.CollarId, l.RecordedAt })   // consultas por rango de tiempo
       .IsDescending(false, true);                         // RecordedAt DESC
builder.Property(l => l.Latitude).HasPrecision(10, 7);
builder.Property(l => l.Longitude).HasPrecision(10, 7);
// Sin FK navegacional hacia Collar — evitar joins costosos en lecturas de historial
```

#### SQL de mantenimiento (purge automático)

```sql
-- Job diario: eliminar localizaciones > 30 días
DELETE FROM CollarLocations
WHERE RecordedAt < DATEADD(DAY, -30, GETUTCDATE());

-- Si a futuro se quiere retención larga: mover a Azure Table Storage
-- y dejar solo las ultimas 100 filas en SQL para lecturas rápidas
```

---

### Estructura del módulo Application

```
Application/Collars/
│
├── Commands/
│   ├── RegisterCollar/
│   │   ├── RegisterCollarCommand.cs         ← dueño vincula collar por deviceId
│   │   ├── RegisterCollarCommandHandler.cs
│   │   └── RegisterCollarCommandValidator.cs
│   │
│   ├── RecordCollarLocation/
│   │   ├── RecordCollarLocationCommand.cs   ← llamado por firmware / job de Tractive
│   │   └── RecordCollarLocationHandler.cs   ← insert via ExecuteSqlRawAsync (no tracking)
│   │
│   └── DeactivateCollar/
│       ├── DeactivateCollarCommand.cs
│       └── DeactivateCollarCommandHandler.cs
│
├── Queries/
│   ├── GetCollarStatus/
│   │   ├── GetCollarStatusQuery.cs          ← última posición + batería
│   │   └── GetCollarStatusQueryHandler.cs
│   │
│   └── GetLocationHistory/
│       ├── GetLocationHistoryQuery.cs       ← últimas N horas, paginado
│       └── GetLocationHistoryQueryHandler.cs
│
└── DTOs/
    ├── CollarStatusDto.cs
    └── CollarLocationDto.cs
```

---

### API Endpoints

#### CollarController (collar propio / firmware)

```csharp
[ApiController]
[Route("api/collars")]
public sealed class CollarsController(ISender sender) : ControllerBase
{
    // POST /api/collars — dueño registra su collar
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] RegisterCollarRequest req, ...) { }

    // GET /api/collars/{petId}/status — última posición
    [HttpGet("{petId:guid}/status")]
    [Authorize]
    public async Task<IActionResult> GetStatus(Guid petId, ...) { }

    // GET /api/collars/{petId}/history?hours=24
    [HttpGet("{petId:guid}/history")]
    [Authorize]
    public async Task<IActionResult> GetHistory(Guid petId, int hours = 24, ...) { }

    // DELETE /api/collars/{id} — desactivar collar
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Deactivate(Guid id, ...) { }
}
```

#### Endpoint especial para firmware (sin JWT)

```csharp
[ApiController]
[Route("api/device")]
public sealed class DeviceIngestController(ISender sender) : ControllerBase
{
    // POST /api/device/location
    // Auth: X-Device-Id + X-Device-Key headers (verificados contra DeviceKeyHash en DB)
    [HttpPost("location")]
    [AllowAnonymous]
    [EnableRateLimiting("device-ingest")]   // rate limit por IP + Device-Id
    public async Task<IActionResult> RecordLocation(
        [FromHeader(Name = "X-Device-Id")] string deviceId,
        [FromHeader(Name = "X-Device-Key")] string deviceKey,
        [FromBody] DeviceLocationRequest req,
        CancellationToken ct)
    {
        // 1. Buscar collar por DeviceId
        // 2. Verificar deviceKey contra DeviceKeyHash (PBKDF2)
        // 3. Si OK → dispatch RecordCollarLocationCommand
        // NUNCA retornar detalles del error de auth (timing attack)
    }
}
```

**Seguridad del endpoint de firmware:**
- No usar JWT — los dispositivos embedded no manejan expiración de tokens
- `DeviceKeyHash` almacenado con **PBKDF2-SHA256, 100k iteraciones** — igual que contraseñas
- Rate limit estricto: 5 req/min por `X-Device-Id` (frecuencia de reporte máxima razonable)
- El endpoint retorna siempre HTTP 204 (éxito) o HTTP 401 (fallo auth) — sin detalles adicionales

---

### Integración Tractive (Opción A — MVP)

```
Flujo OAuth2:
1. GET /api/collars/tractive/connect → redirect a Tractive OAuth
2. Tractive callback → POST /api/collars/tractive/callback con code
3. Exchange code → access_token + refresh_token
4. Cifrar tokens con Azure Key Vault (EncryptAsync/DecryptAsync)
5. Guardar en Collar.ExternalToken (cifrado)

Background Job (Hangfire o Azure Function Timer):
- Cada 5 minutos: para cada Collar activo con Provider == Tractive
  - Llamar Tractive API: GET /device_hw_report/{deviceId}
  - Parsear lat/lng/battery
  - Dispatch RecordCollarLocationCommand
  - Actualizar Collar.LastSeenAt + BatteryPercent
```

```csharp
// Application/Common/Interfaces/ITrackerProviderService.cs
public interface ITrackerProviderService
{
    Task<CollarLocationData?> GetCurrentLocationAsync(
        string deviceId, string encryptedToken, CancellationToken ct);
}

// Infrastructure/Collars/TractriveProviderService.cs — implementación concreta
```

---

### Frontend — UI del collar

#### En `PetDetailPage.tsx`

```tsx
{/* Sección GPS — mostrar si la mascota tiene collar activo */}
{collar && (
  <section>
    <h3>📍 Ubicación GPS</h3>
    <CollarStatusCard
      lastSeen={collar.lastSeenAt}
      battery={collar.batteryPercent}
      lat={collar.latitude}
      lng={collar.longitude}
    />
    {/* Reusar el mismo componente de mapa que tienen las alertas */}
    <PetLocationMap lat={collar.latitude} lng={collar.longitude} />
  </section>
)}
```

#### Componentes nuevos necesarios

```
frontend/src/features/collars/
├── api/
│   └── collarsApi.ts          ← endpoints de collar
├── hooks/
│   ├── useCollarStatus.ts     ← React Query, polling cada 30s si mascota perdida
│   └── useRegisterCollar.ts
└── components/
    ├── CollarStatusCard.tsx   ← batería + última posición + tiempo
    ├── PetLocationMap.tsx     ← wrapper del mapa con pin GPS
    └── RegisterCollarModal.tsx
```

---

### Consideraciones legales y de privacidad

| Tema | Aplica | Acción |
|------|--------|--------|
| Ley 8968 (Protección de datos CR) | Sí — datos de ubicación son datos personales | Incluir en política de privacidad que se recopilan coordenadas GPS del animal |
| Retención de datos | Guías: no más de lo necesario | Purge automático a 30 días implementado en SQL job |
| Términos de uso de Tractive API | Sí | Revisar TOS de Tractive; no re-vender datos de ubicación de su plataforma |
| Tracking sin consentimiento | Mitigado — el collar es del dueño | Solo el dueño autenticado puede ver el historial de su mascota |

---

### Plan de implementación recomendado

#### Fase 1: Microchip (Semana 1)

| Día | Tarea |
|----|------|
| Lunes | `UpdatePetCommand` + `CreatePetCommand` + handlers |
| Martes | `PetDto` + `GetPetByMicrochipQuery` + endpoint |
| Miércoles | Frontend: `PetForm.tsx` + `petsApi.ts` + `PetDetailPage` |
| Jueves | Pruebas + fix bugs |
| Viernes | QA manual con un lector NFC en el teléfono |

#### Fase 2: Collar GPS — Tractive integration (Semanas 2–4)

| Semana | Tarea |
|--------|------|
| 2 | Entidad `Collar` + migration + `RegisterCollar` command + endpoint |
| 3 | Integración Tractive OAuth + background job de polling |
| 4 | Frontend: `CollarStatusCard` + mapa en `PetDetailPage` |

#### Fase 3: Collar propio (Semanas 5–12, solo si hay demanda validada)

| Semana | Tarea |
|--------|------|
| 5–6 | `DeviceIngestController` + `RecordCollarLocation` + rate limiting |
| 7–8 | Diseño PCB / BOM / proveedor |
| 9–10 | Firmware (SIM7600 + GPS + MQTT o HTTP) |
| 11–12 | QA end-to-end con dispositivo físico |

---

### Checklist completo — Collar GPS

```
Integración Tractive:
[ ] Collar entidad + CollarLocation entidad + EF configuraciones
[ ] Migración EF Core para tablas Collars + CollarLocations
[ ] IPetCollarRepository interfaz
[ ] RegisterCollar command/handler/validator
[ ] GetCollarStatus query/handler/DTO
[ ] GetLocationHistory query/handler (paginado, máx 100 puntos por request)
[ ] ITrackerProviderService interfaz + TractiveProviderService
[ ] Tractive OAuth flow (connect + callback endpoints)
[ ] Background job de polling (Azure Function Timer o Hangfire)
[ ] CollarsController (register, status, history, deactivate)
[ ] collarsApi.ts + useCollarStatus.ts + useRegisterCollar.ts
[ ] CollarStatusCard.tsx + PetLocationMap.tsx
[ ] CollarStatusCard en PetDetailPage

Collar propio (adicional):
[ ] DeviceIngestController con auth por X-Device-Id + X-Device-Key
[ ] Rate limiter "device-ingest" en Program.cs
[ ] PBKDF2 key hashing en Collar.Register()
[ ] Firmware SIM7600 (fuera del repo de PawTrack — repo separado)
[ ] SQL purge job para CollarLocations > 30 días
```

---

### Resumen de dependencias entre módulos

```
Pets module  ←──────────────────────── Collars module
  Pet.Id                                 Collar.PetId (FK)
  IPetRepository                         ICollarRepository
  PetDto                                 CollarStatusDto

Collar GPS ←──── posición ──────────→  LostPets module
  Si mascota está Lost y tiene collar,  el mapa de búsqueda muestra
  la última posición GPS como punto     de partida para aliados
```
