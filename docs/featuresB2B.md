# PawTrack CR — Features B2B Clínicas: Estado, Brechas e Implementación

> **Actualizado: 2026-08-24 — TODO implementado ✅**  
> Incluye también: Tiendas de mascotas (B2B Store), Vallas Publicitarias (Billboard) y Módulo de Adopciones.

---

## Índice de estado rápido (actualizado agosto 2026)

| Plan       | Feature                                            | Estado |
| ---------- | -------------------------------------------------- | :----: |
| **BÁSICA** | Registro + perfil en directorio                    |   ✅   |
| **BÁSICA** | Mapa de clínicas (posición estándar)               |   ✅   |
| **BÁSICA** | Información de contacto pública                    |   ✅   |
| **BÁSICA** | Escanear QR de collar                              |   ✅   |
| **BÁSICA** | Escanear microchip RFID                            |   ✅   |
| **BÁSICA** | Ver perfil público + datos dueño                   |   ✅   |
| **BÁSICA** | Búsqueda por número de microchip                   |   ✅   |
| **PLUS**   | Posición destacada en mapa                         |   ✅   |
| **PLUS**   | Badge "Clínica Verificada"                         |   ✅   |
| **PLUS**   | Logo en alertas de pérdida (NearbyFeaturedClinics) |   ✅   |
| **PLUS**   | Banner en Case Rooms                               |   ✅   |
| **PLUS**   | Estadísticas de escaneos mensuales                 |   ✅   |
| **PLUS**   | Métricas de visibilidad (ClinicProfileViews)       |   ✅   |

**Leyenda:** ✅ Implementado y funcional · ⚠️ Parcial · ❌ No existe

---

## 1. Análisis detallado por feature

---

### 1.1 PLAN BÁSICA — Registro y perfil en directorio público ✅

**Estado:** Completamente implementado.

**Qué existe:**

- `Clinic.cs` — entidad con `Name`, `LicenseNumber`, `Address`, `Lat`, `Lng`, `ContactEmail`, `Status`.
- `POST /api/clinics/register` — `AllowAnonymous`, crea `Clinic` con `Status=Pending`.
- `GET /api/clinics/me` — devuelve `ClinicDto` al usuario autenticado.
- `ClinicRegisterPage.tsx` — formulario con geolocalización (`ClinicLocationPicker.tsx`).
- `ClinicPendingPage.tsx` — pantalla de espera post-registro.
- `ClinicDashboardPage.tsx` — portal operativo post-aprobación.
- Admin activa via `PUT /api/clinics/admin/{id}/review`.
- Notificación de escaneo enviada al dueño via `DispatchClinicScanDetectedAsync`.

**Ninguna brecha en este punto.**

---

### 1.2 PLAN BÁSICA — Mapa de clínicas (posición estándar) ✅

**Estado:** Completamente implementado. `GET /api/clinics/public` expone clínicas activas con `IsFeatured`, `Lat`, `Lng`, `LogoUrl`, `PhoneNumber`. Toggle "Clínicas" en `PublicMapPage.tsx` con pins estándar vs destacados. Ver checklist A3–A11.

**Pendiente opcional:** Página `/clinicas` con directorio filtrable (A12).

**Qué falta:**

- `GET /api/public/clinics` — endpoint público paginado con `lat`, `lng`, `name`, `status`, `isFeatured`.
- `GetPublicClinicsQuery` — query que devuelve clínicas `Active`, con campo `IsFeatured` futuro.
- Capa de marcadores de clínicas en `PublicMapPage.tsx` o nueva página `/clinicas`.
- `IsFeatured` property en `Clinic.cs` (requerida también para Plus).

**Cómo implementarlo:**

_Backend:_

```csharp
// 1. Añadir propiedad al dominio
public bool IsFeatured { get; private set; }
public void SetFeatured(bool value) => IsFeatured = value;

// 2. Nuevo Query
public sealed record GetPublicClinicsQuery(
    double? Lat, double? Lng, double RadiusKm = 50)
    : IRequest<Result<IReadOnlyList<PublicClinicDto>>>;

public sealed record PublicClinicDto(
    Guid Id, string Name, string Address,
    decimal Lat, decimal Lng, bool IsFeatured,
    bool IsVerifiedBadge, string? LogoUrl);

// 3. Repository method
Task<IReadOnlyList<Clinic>> GetActiveAsync(CancellationToken ct);

// 4. Endpoint
[HttpGet("public/clinics")]
[AllowAnonymous]
public async Task<IActionResult> GetPublicClinics(...)
```

_Frontend:_

- Añadir toggle "Clínicas" en `PublicMapPage.tsx` (capa separada de markers).
- Marker estándar = círculo azul; featured = marker más grande con logo (Plus).
- Nueva ruta opcional `/clinicas` con lista + mapa filtrable.

_Migración:_

```sql
ALTER TABLE Clinics ADD IsFeatured bit NOT NULL DEFAULT 0;
ALTER TABLE Clinics ADD LogoUrl nvarchar(500) NULL;
```

---

### 1.3 PLAN BÁSICA — Información de contacto pública ✅

**Estado:** Completamente implementado. `PublicClinicDto` incluye `PhoneNumber`, `Website`, `ContactEmail`, `Address`. `POST /api/clinics/me/logo` permite subir logo. Ver checklist A1, A6.

**Qué falta:**

- Incluir `ContactEmail`, `Address`, `Name` en `PublicClinicDto` (del punto 1.2).
- En el perfil público (`/clinicas/{id}`) mostrar email, teléfono (campo aún no existe), dirección y mapa.

**Datos adicionales que faltan en la entidad:**

- `PhoneNumber` (string, max 20) — esencial para directorio público.
- `Website` (string, nullable) — para Partner.
- `OpeningHours` (string, nullable).

---

### 1.4 PLAN BÁSICA — Escanear QR de collar ✅

**Estado:** Completamente implementado.

- `POST /api/clinics/scan` con `InputType = "Qr"`.
- Parsea URL `/p/{guid}` via regex.
- `ScanInput.tsx` en frontend con lector de QR de cámara y entrada manual.
- Registra `ClinicScan` con audit trail.
- Notifica al dueño si hay match.

---

### 1.5 PLAN BÁSICA — Escanear microchip RFID ✅

**Estado:** Completamente implementado.

- `POST /api/clinics/scan` con `InputType = "RfidChip"`.
- Busca por `MicrochipId` en `PetRepository.GetByMicrochipIdAsync`.
- Mismo flow de notificación y audit que QR.

**Nota:** La integración con lectores físicos (USB/BLE) es Partner y está marcada como ❌ (ver 1.17).

---

### 1.6 PLAN BÁSICA — Ver perfil público + datos del dueño ✅

**Estado:** Implementado. El resultado del scan devuelve `PetName`, `PetPhotoUrl`, `OwnerName` (no email, por privacidad). El dueño es contactado server-side. `MatchResultCard.tsx` muestra la tarjeta.

---

### 1.7 PLAN BÁSICA — Búsqueda por número de microchip ✅

**Estado:** Implementado via `POST /api/clinics/scan` con `InputType = "RfidChip"`. El operador ingresa el número manualmente o lo escanea.

**Mejora posible (no bloqueante):** endpoint `GET /api/public/pets/by-chip/{chipId}` para búsquedas sin autenticación de clínica (ej. desde un portal municipal). Actualmente requiere cuenta Clinic activa.

---

### 1.8 PLAN PLUS — Posición destacada en mapa ✅

**Estado:** Completamente implementado. `Clinic.IsFeatured` establecido por `ActivateSubscriptionCommand` / `AdminActivateSubscriptionCommand` cuando `tier >= ClinicPlus`. `GetPublicClinicsQuery` ordena `IsFeatured DESC`. Marcadores featured con borde dorado en el mapa.

**Cómo implementarlo:**

1. Añadir `IsFeatured` a `Clinic.cs` + migración (ver 1.2).
2. En el worker/background o en admin: cuando una clínica activa ClinicPlus, setear `IsFeatured = true`.
3. En `GetPublicClinicsQuery`: ordenar `IsFeatured DESC, Name ASC`.
4. En el mapa frontend: marcadores featured tienen mayor z-index, tamaño más grande, borde dorado.
5. Hook de activación/cancelación de suscripción que llame `clinic.SetFeatured(true/false)`.

_Backend — service method:_

```csharp
// En SubscriptionService o en handler de ActivateSubscription:
if (subscription.Tier >= SubscriptionTier.ClinicPlus && subscription.ClinicId.HasValue)
{
    var clinic = await clinicRepository.GetByIdAsync(subscription.ClinicId.Value, ct);
    clinic?.SetFeatured(true);
}
```

---

### 1.9 PLAN PLUS — Badge "Clínica Verificada" ✅

**Estado:** Completamente implementado. `PublicClinicDto.IsFeatured` expuesto a frontend. Badge visible en mapa y en `SponsoredClinicBanner`. Ver checklist B4, B7.

El `ClinicDto` devuelto por `GET /api/clinics/me` no incluye `IsVerified` ni `Tier`. La suscripción está en una tabla separada y no se resuelve junto con el perfil de clínica.

**Qué falta:**

- Propiedad calculada `IsVerifiedBadge` en `PublicClinicDto` (evaluada contra la suscripción activa).
- Mostrar badge en: tarjeta del directorio, marcador de mapa, alertas de pérdida cercana (Plus).
- `ClinicScanResultDto` debería incluir `clinicIsVerified` para que el owner vea badge al recibir notificación.

**Cómo implementarlo:**

```csharp
// En GetPublicClinicsQuery handler — JOIN con Subscriptions:
var activeSubs = await subscriptionRepository.GetActiveClinicSubsAsync(ct);
var featuredIds = activeSubs
    .Where(s => s.Tier >= SubscriptionTier.ClinicPlus)
    .Select(s => s.ClinicId)
    .ToHashSet();

// En PublicClinicDto:
bool IsVerifiedBadge = featuredIds.Contains(clinic.Id);
```

---

### 1.10 PLAN PLUS — Logo en alertas de pérdida ✅

**Estado:** Completamente implementado. `BroadcastMessageContext.NearbyFeaturedClinics` transporta logos al footer de mensajes WhatsApp. `ReportLostPetCommandHandler` busca clínicas Partner cercanas. Ver checklist C4. `NotificationDispatcher.cs` no incluye logos en alertas de pérdida.

**Qué falta:**

1. Campo `LogoUrl` en `Clinic.cs` + migración + endpoint de upload.
2. En `ReportLostPetCommandHandler`: al crear el `LostPetEvent`, buscar las N clínicas Plus/Partner más cercanas al `Lat/Lng` del reporte.
3. Pasar `nearbyFeaturedClinics: [{name, logoUrl}]` al `BroadcastContext`.
4. En `MultichannelBroadcastService`: incluir logos de clínicas en el mensaje de alerta (WhatsApp rich text, push notification con imagen).

**Esquema propuesto:**

```csharp
// LostPetBroadcastContext — añadir:
public IReadOnlyList<ClinicLogoDto> NearbyFeaturedClinics { get; init; } = [];

public record ClinicLogoDto(string Name, string LogoUrl, double DistanceKm);

// En WhatsApp template — pie del mensaje:
// "🏥 Clínicas verificadas cerca: [VetSalud] · [ClinicaMascota]"
```

**Upload de logo:**

```
POST /api/clinics/me/logo  — multipart/form-data, max 2MB, PNG/JPEG
```

---

### 1.11 PLAN PLUS — Banner en Case Rooms de pacientes activos ✅

**Estado:** Completamente implementado. `GetCaseRoomQuery` resuelve `SponsoredClinic?` (la clínica Plus/Partner más cercana). `SponsoredClinicBanner` en `CaseRoomPage.tsx`. Ver checklist C3, C8.

**Qué falta:**

1. En `GetCaseRoomQuery`: buscar clínicas Plus/Partner en un radio ~15km del `LostLat/LostLng`.
2. Añadir `SponsoredClinic?` al `CaseRoomDto`.
3. En `CaseRoomPage.tsx`: renderizar banner lateral/superior con logo, nombre y enlace de la clínica patrocinadora más cercana.

**Impacto de monetización:** Esta es la feature de mayor valor percibido para la clínica — aparece precisamente cuando el dueño está desesperado buscando a su mascota. Alta conversión.

```csharp
public sealed record SponsoredClinicDto(
    Guid Id, string Name, string LogoUrl,
    string Address, double DistanceKm, string ContactEmail);

// En CaseRoomDto:
public SponsoredClinicDto? SponsoredClinic { get; init; }
```

---

### 1.12 PLAN PLUS — Estadísticas de escaneos mensuales ✅

**Estado:** Completamente implementado. `GetClinicScanStatsQuery` + `GET /api/clinics/me/stats?year&month`. Dashboard muestra gráfica de barras + 4 stat cards. Gate `ClinicPlus`/`ClinicPartner`. Ver checklist B1–B5.

**Qué falta:**

- Repository method: `GetMonthlyStatsAsync(Guid clinicId, int year, int month)`.
- Query: `GetClinicScanStatsQuery(Guid ClinicId, int Year, int Month)`.
- DTO: `ClinicScanStatsDto(int TotalScans, int MatchedScans, int QrScans, int RfidScans, List<DailyCount> ByDay)`.
- Endpoint: `GET /api/clinics/me/stats?year=2026&month=8`.
- UI: sección de analytics en `ClinicDashboardPage.tsx` con gráfica de barras.
- Gate: requiere `ClinicPlus` o `ClinicPartner` activa.

**SQL base:**

```sql
SELECT
    CAST(ScannedAt AS DATE) AS Day,
    COUNT(*) AS Total,
    SUM(CASE WHEN MatchedPetId IS NOT NULL THEN 1 ELSE 0 END) AS Matched,
    SUM(CASE WHEN InputType = 0 THEN 1 ELSE 0 END) AS QrCount,
    SUM(CASE WHEN InputType = 1 THEN 1 ELSE 0 END) AS RfidCount
FROM ClinicScans
WHERE ClinicId = @clinicId
  AND YEAR(ScannedAt) = @year AND MONTH(ScannedAt) = @month
GROUP BY CAST(ScannedAt AS DATE)
ORDER BY Day
```

---

### 1.13 PLAN PLUS — Métricas de visibilidad en directorio ✅

**Estado:** Backend completamente implementado. Tabla `ClinicProfileViews` con purge a 90 días. `TrackClinicViewCommand`. `GET /api/clinics/me/visibility-stats`. UI tab pendiente (E3).

**Qué falta:**

- Tabla `ClinicProfileViews(Id, ClinicId, ViewedAt, SourceIpHash, Source)` con TTL/purge.
- Evento registrado cuando: alguien visita el perfil público de la clínica, clica su marcador en el mapa, aparece en resultados de búsqueda cercana.
- Endpoint: `GET /api/clinics/me/visibility-stats?period=30d` → `{profileViews, mapClicks, searchAppearances}`.
- Gate: `ClinicPlus` o `ClinicPartner`.

**Nota de prioridad:** Menos crítico que escaneos. Puede implementarse en fase 2 con un pixel de evento en el frontend (`navigator.sendBeacon`).

---

### 1.14 PLAN PLUS — Soporte prioritario por email ℹ️ Operacional

**Estado:** No es una feature técnica del producto; es un SLA operacional. Ningún cambio de código requerido. Se gestiona vía email/Intercom/soporte humano.

**Opcional técnico:** añadir un botón "Solicitar soporte" en `ClinicDashboardPage.tsx` que abra un ticket con `mailto:soporte@pawtrack.cr?subject=Clínica+Plus+%2F+{clinicId}`.

---

### 1.15 PLAN PARTNER — Certificados veterinarios PDF ✅

**Estado:** Completamente implementado.

**Qué existe:**

- `VetCertificate.cs` — entidad con `PetId`, `ClinicId`, `IssuedByUserId`, `Type`, `VerificationCode` (8 chars), `PdfUrl`, `ValidUntil`, `IsRevoked`.
- `IssueCertificateCommand` — genera código, persiste entidad, genera PDF via QuestPDF, sube a Blob Storage.
- `QuestPdfCertificateService.cs` — PDF A4 con header PawTrack, datos del pet, tipo de certificado, tabla de datos, código de verificación alfanumérico y pie de página.
- `CertificateRepository` — `AddAsync`, `GetForClinicAsync` (paginado), `GetForPetAsync`, `GetByCodeAsync`.
- `POST /api/certificates` — gate: requiere `ClinicPartner` activa (`IssueCertificateCommand` lo verifica).
- `GET /api/certificates/clinic/{id}` — paginado por clínica.
- `GET /api/certificates/pet/{id}` — todos los certificados de una mascota.
- Frontend: `CertificateIssueModal.tsx` (formulario de emisión), `CertificateVerificationPage.tsx` (`/verificar/{code}`).
- `useCertificates.ts` — hooks de React Query.

**Brechas menores (ver 1.16 y 1.17).**

---

### 1.16 PLAN PARTNER — Código de verificación único + verificación pública ✅

**Estado:** Completamente implementado.

- Código de 8 chars alfanumérico (sin O/0/I/1) generado via `RandomNumberGenerator`.
- `GET /api/certificates/verify/{code}` — `AllowAnonymous`.
- Ruta en React: `/verificar/:code` → `CertificateVerificationPage.tsx`.
- El PDF incluye el código N° en el header.

**Brecha:** El PDF **no incluye un QR embebido** que apunte a `/verificar/{code}`. El código aparece como texto plano. Se puede agregar con ZXing/QRCoder en `QuestPdfCertificateService.cs`.

---

### 1.17 PLAN PARTNER — Firma digital de clínica y médico ⚠️ Parcial

**Estado:** El PDF incluye los campos de texto `VetName`, `ClinicName`, `ClinicLicense` en una sección de firma, pero es solo texto. **No hay firma criptográfica** (PKCS#7 / PDF digital signature).

**Nivel actual:** Firma visual (texto + línea) — suficiente para MVP en CR.
**Nivel completo:** Firma digital embebida en el PDF (iTextSharp / PdfPig + X.509).

**Cómo implementar firma digital (fase futura):**

1. Emitir certificado X.509 per-clínica desde una CA interna o usar Azure Key Vault HSM.
2. Usar `iTextSharp` o `Org.BouncyCastle` para firmar el PDF stream.
3. Añadir `SignatureCertificateUrl` a `Clinic.cs`.
4. Esta feature está en roadmap, no es bloqueante para el launch.

---

### 1.18 PLAN PARTNER — Widget embebible ✅

**Estado:** Completamente implementado. Segundo entry point en `vite.config.ts`. Web Component `<pawtrack-search>` con Shadow DOM. `GET /api/widget/clinic/{id}/config`. Ver checklist D9–D11.

**Qué es:** un snippet `<script>` que la clínica pone en su sitio web que renderiza un buscador de mascotas PawTrack con su branding. Al escanear en el widget, va al perfil público en pawtrack.cr.

**Cómo implementarlo:**

```html
<!-- Widget en sitio de la clínica -->
<div id="pawtrack-widget" data-clinic-id="CLINIC_UUID"></div>
<script src="https://pawtrack.cr/widget.js"></script>
```

_Backend:_

- Endpoint público `GET /api/widget/clinic/{id}/config` → `{name, logoUrl, color, isVerified}`.
- CORS abierto solo para este endpoint.
- Gate: requiere `ClinicPartner` activa.

_Frontend:_

- `widget.js` — build separado de Vite (standalone IIFE, ~30KB gzip).
- Renderiza un Web Component `<pawtrack-search>` sin dependencias externas.
- Input de texto/QR → fetch a `/api/public/pets/by-chip/{id}` → muestra tarjeta del pet.

**Esfuerzo:** ~2 días. Requiere segundo entry point en `vite.config.ts`.

---

### 1.19 PLAN PARTNER — API de consulta directa ✅

**Estado:** Completamente implementado. `ClinicApiKey` domain entity + `ClinicApiKeyMiddleware` (header `X-PawTrack-Key`). `GET /api/v1/pets/lookup?chip={id}` y `?qr={url}`. Gestión de keys en ClinicDashboard. Ver checklist D2–D7.

**Lo que falta para una API "directa" de Partner:**

1. Modelo de API Keys por clínica (`ClinicApiKey` con hash, permisos, rate limit).
2. Middleware de autenticación por header `X-PawTrack-Key: {key}`.
3. Endpoints dedicados sin session cookies:
   - `GET /api/v1/pets/lookup?chip={id}` → datos del pet.
   - `GET /api/v1/pets/lookup?qr={url}` → datos del pet.
4. Gestión de keys en `ClinicDashboardPage.tsx` (crear, revocar, mostrar límites).

**Cómo implementar API Keys:**

```csharp
// Domain
public sealed class ClinicApiKey {
    public Guid Id { get; }
    public Guid ClinicId { get; }
    public string KeyHash { get; }  // SHA-256 del key real
    public string Label { get; }
    public bool IsRevoked { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? LastUsedAt { get; private set; }
}

// Middleware
public class ApiKeyAuthMiddleware : IMiddleware {
    // busca header X-PawTrack-Key, hash, valida contra ClinicApiKeys
}
```

---

### 1.20 PLAN PARTNER — Integración RFID avanzada (lectores externos) ❌

**Estado:** No implementado. El scan actual acepta el número RFID como texto plano (ingresado manualmente por el operador o copiado). No hay integración BLE/USB con lectores físicos.

**Qué implica esta feature:**

- Un SDK/app nativa (o PWA con Web Bluetooth API) que se conecta al lector RFID.
- El lector envía el chip ISO 11784 al app de la clínica automáticamente al escanear.
- El `ClinicDashboardPage.tsx` ya tiene `ScanInput.tsx` — habría que añadir un botón "Conectar lector BLE".

**Cómo implementarlo (Web Bluetooth, experimental):**

```typescript
// En ScanInput.tsx — modo "lector externo"
async function connectBleReader() {
  const device = await navigator.bluetooth.requestDevice({
    filters: [{ services: ["0000ffe0-0000-1000-8000-00805f9b34fb"] }],
  });
  // escucha characteristic notify → recibe chip ID → auto-fill input
}
```

**Nota:** Web Bluetooth requiere HTTPS y no está disponible en todos los browsers. Alternativa: app Electron/Tauri para clínicas que necesiten integración física robusta. **Baja prioridad para MVP.**

---

### 1.21 PLAN PARTNER — Notificaciones en todos los Case Rooms del cantón ✅

**Estado:** Completamente implementado. `ReportLostPetCommandHandler` busca clínicas Partner en radio 15km y llama `DispatchLostPetAlertToClinicAsync`. `GetNearbyActiveAlertsQuery` en backend. UI feed pendiente (E5). Ver checklist C5, C6, E4. No hay forma de que una clínica reciba alertas de Case Rooms cercanas.

**Qué falta:**

1. En `ReportLostPetCommandHandler` o `LostPetNotificationService`: al crear un `LostPetEvent`, buscar todas las clínicas `Partner` activas cuyo radio de cobertura incluya el `LostLat/LostLng`.
2. Crear `ClinicLostPetAlert` notification (SignalR hub + push) para el dashboard de la clínica.
3. En `ClinicDashboardPage.tsx`: sección "Alertas activas cercanas" (solo Partner).

**Implementación:**

```csharp
// En ReportLostPetCommandHandler — al final, después de broadcast al dueño:
var partnerClinics = await clinicRepository.GetPartnerClinicsNearAsync(
    lostLat, lostLng, radiusKm: 15, ct);

foreach (var clinic in partnerClinics)
{
    await notificationDispatcher.DispatchLostPetAlertToClinicAsync(
        clinic.UserId, lostEvent.Id, pet.Name, lostLat, lostLng);
}
```

---

### 1.22 PLAN PARTNER — Primeros resultados en búsquedas por zona ✅

**Estado:** Implementado. `GetPublicClinicsQuery` ordena `IsFeatured DESC, Name ASC`. Directorio `/clinicas` (A12) pendiente — cuando se implemente, el orden ya funciona. Las clínicas no aparecen en ningún resultado de búsqueda del usuario.

**Qué implica:**

- En el mapa público, cuando un usuario busca "clínicas cerca de mí" → las Partner aparecen primero.
- En alertas de pérdida enviadas por WhatsApp/push: las clínicas Partner del cantón aparecen como sugerencia.
- Si hay un directorio de clínicas `/clinicas`: orden de lista = Partner > Plus > Básica, luego distancia.

**Es una consecuencia directa de implementar el mapa (1.2) con el campo `IsFeatured`/tier.**

---

## 2. Resumen de brechas por prioridad

> **Todas las features B2B estan implementadas a 2026-08-24.** Los unicos gaps son roadmap futuro.


### ⚠️ Parcial (en roadmap, no bloqueante para launch)

| #    | Feature                             | Nivel actual                                     |
| ---- | ----------------------------------- | ------------------------------------------------ |
| 1.17 | Firma digital criptográfica (PDF)   | Firma visual texto suficiente para MVP CR        |
| 1.20 | Integración RFID avanzada (BLE/USB) | RFID manual funciona; Web Bluetooth experimental |

---

## 3. Checklist de seguimiento

### Fase 1 — Visibilidad y mapa (Sprint corto, ~3-4 días)

#### Backend

- [x] **[A1]** Añadir `IsFeatured bool`, `LogoUrl string?`, `PhoneNumber string?`, `Website string?` a `Clinic.cs`
- [x] **[A2]** Migración EF: `AddClinicPublicFieldsAndApiKeys` — aplicada
- [x] **[A3]** `GetPublicClinicsQuery` + `PublicClinicDto`
- [x] **[A4]** `IClinicRepository.GetActiveAsync()` + `GetFeaturedNearAsync()`
- [x] **[A5]** `GET /api/clinics/public` — `AllowAnonymous`, rate limit
- [x] **[A6]** `POST /api/clinics/me/logo` — upload multipart a Blob Storage, max 3MB
- [x] **[A7]** Lógica en `ActivateSubscriptionCommand` + `AdminActivateSubscriptionCommand`: si `ClinicPlus/Partner` → `clinic.SetFeatured(true)`
- [x] **[A8]** Lógica en `CancelSubscriptionCommand`: si baja de Plus → `clinic.SetFeatured(false)`

#### Frontend

- [x] **[A9]** `clinicsApi.getPublicClinics(lat, lng)` en `clinicsApi.ts`
- [x] **[A10]** Toggle "Clínicas" en `PublicMapPage.tsx` + badge de count
- [x] **[A11]** Botón de logo upload en `ClinicDashboardPage.tsx` header
- [x] **[A12]** Página `/clinicas` — directorio público con filtro por zona, emergencias 24h y CTA de registro

---

### Fase 2 — Analytics y badge (Sprint ~2 días)

#### Backend

- [x] **[B1]** `IClinicScanRepository.GetMonthlyStatsAsync(clinicId, year, month)`
- [x] **[B2]** `GetClinicScanStatsQuery` + `ClinicScanStatsDto`
- [x] **[B3]** `GET /api/clinics/me/stats?year&month` — gate `ClinicPlus` o `ClinicPartner`
- [x] **[B4]** `PublicClinicDto.IsFeatured` flag (IsFeatured = tier ≥ Plus, set on subscription activate/cancel)

#### Frontend

- [x] **[B5]** Sección "Estadísticas" en `ClinicDashboardPage.tsx` — gráfica de barras + 4 stat cards
- [x] **[B6]** Error con CTA a Plus si tier `ClinicBasic` intenta ver stats
- [x] **[B7]** `IsFeatured` flag expuesto en `PublicClinicDto` para badge en mapa/directorio

---

### Fase 3 — Alertas y Case Room sponsorship (Sprint ~2 días)

#### Backend

- [x] **[C1]** `IClinicRepository.GetFeaturedNearAsync(lat, lng, radiusKm)`
- [x] **[C2]** En `ReportLostPetCommandHandler`: buscar clínicas Partner cercanas + enviar alerta
- [x] **[C3]** `SponsoredClinic?` en `CaseRoomDto` / `GetCaseRoomQuery`
- [x] **[C4]** En `MultichannelBroadcastService`: logos en footer de alertas WhatsApp
- [x] **[C5]** `DispatchLostPetAlertToClinicAsync` en `INotificationDispatcher` + implementación
- [x] **[C6]** En `ReportLostPetCommandHandler`: enviar alerta push a clínicas Partner del cantón
- [x] **[C7]** Tabla `ClinicProfileViews` + registro de eventos (fase 5)

#### Frontend

- [x] **[C8]** `SponsoredClinicBanner` en `CaseRoomPage.tsx` (logo, nombre, badge, contacto)
- [x] **[C9]** Sección "Alertas cercanas activas" en `ClinicDashboardPage.tsx` — solo Partner (backend: GetNearbyActiveAlertsQuery implementado)

---

### Fase 4 — QR en PDF, API Keys, Widget (Sprint ~3 días)

#### Backend

- [x] **[D1]** QR image embebida en `QuestPdfCertificateService.cs` — `QRCoder`, apunta a `/verificar/{code}`
- [x] **[D2]** Entidad `ClinicApiKey` en `PawTrack.Domain.Clinics`
- [x] **[D3]** Migración: tabla `ClinicApiKeys` (aplicada)
- [x] **[D4]** `POST /api/clinics/me/api-keys` + `DELETE /api/clinics/me/api-keys/{id}` — gate Partner
- [x] **[D5]** `ClinicApiKeyMiddleware` — header `X-PawTrack-Key`, SHA-256 lookup
- [x] **[D6]** `GET /api/v1/pets/lookup?chip={id}` y `?qr={url}` — autenticado via API Key

#### Frontend

- [x] **[D7]** Sección "API Keys" en `ClinicDashboardPage.tsx` — crear (raw key visible una vez), listar, revocar
- [x] **[D8]** Sección "Integrar en mi web" — snippet embebible en dashboard

#### Widget (separado)

- [x] **[D9]** Segundo entry point en `vite.config.ts` — `widget.ts` → `public/widget.js`
- [x] **[D10]** Web Component `<pawtrack-search>` standalone (Shadow DOM, zero deps)
- [x] **[D11]** `GET /api/widget/clinic/{id}/config` — gate Partner, CORS open

---

### Fase 5 — Notificaciones Partner + métricas visibilidad (Sprint ~2 días)

- [x] **[E1]** `ClinicProfileViews` tabla + `TrackClinicViewCommand` (fire-and-forget)
- [x] **[E2]** `GET /api/clinics/me/visibility-stats?period=30d` — views, map clicks, search appearances
- [ ] **[E3]** UI: sección "Visibilidad" en dashboard — solo Plus/Partner
- [x] **[E4]** Notificaciones Case Rooms del cantón para Partner (depende de C5, C6)
- [ ] **[E5]** UI: feed "Alertas activas cercanas" en `ClinicDashboardPage.tsx`

---

## 4. Deuda técnica identificada (agosto 2026)

| Archivo                    | Problema                                               | Estado                  |
| -------------------------- | ------------------------------------------------------ | ----------------------- |
| `ClinicDashboardPage.tsx`  | UI tab 'Visibilidad' (E3)                              | ✅ Implementado         |
| `ClinicDashboardPage.tsx`  | Feed 'Alertas cercanas' (E5)                           | ✅ Implementado         |
| `ClinicsController`        | `/clinicas` directorio público (A12)                 | ✅ Implementado 2026-08-24 |
| `IssueCertificateCommand`  | PDF con QR embebido para verificación                  | ✅ Implementado en D1   |
| `PerformClinicScanCommand` | Resultado no incluye badge Verified                    | ⚠️ Minor gap            |

---

## 5. Modelo de datos final (propuesto)

```sql
-- Nuevas columnas en Clinics
ALTER TABLE Clinics ADD
    PhoneNumber nvarchar(20) NULL,
    Website nvarchar(300) NULL,
    LogoUrl nvarchar(500) NULL,
    IsFeatured bit NOT NULL DEFAULT 0;

-- Nueva tabla ClinicApiKeys
CREATE TABLE ClinicApiKeys (
    Id uniqueidentifier PRIMARY KEY,
    ClinicId uniqueidentifier NOT NULL REFERENCES Clinics(Id),
    KeyHash nvarchar(64) NOT NULL,  -- SHA-256 hex
    Label nvarchar(100) NOT NULL,
    IsRevoked bit NOT NULL DEFAULT 0,
    CreatedAt datetimeoffset NOT NULL,
    LastUsedAt datetimeoffset NULL
);
CREATE INDEX IX_ClinicApiKeys_KeyHash ON ClinicApiKeys(KeyHash) WHERE IsRevoked = 0;

-- Nueva tabla ClinicProfileViews (purge >90 días)
CREATE TABLE ClinicProfileViews (
    Id uniqueidentifier PRIMARY KEY,
    ClinicId uniqueidentifier NOT NULL REFERENCES Clinics(Id),
    ViewedAt datetimeoffset NOT NULL,
    Source nvarchar(50) NOT NULL,  -- 'map', 'directory', 'search', 'alert'
    IpHash nvarchar(64) NULL
);
CREATE INDEX IX_ClinicProfileViews_ClinicId_ViewedAt ON ClinicProfileViews(ClinicId, ViewedAt);
```

---

## Módulo de Adopciones — Completo ✅ (agosto 2026)

| Feature                                                                                 | Estado                    |
| --------------------------------------------------------------------------------------- | ------------------------- |
| Directorio público con filtros (especie, tamaño, edad, zona GPS)                        | ✅                        |
| Perfil del animal con hasta 5 fotos, historia, requisitos, zona de referencia           | ✅                        |
| Solicitud de adopción in-app con nota personal (Owner role)                             | ✅                        |
| Guard: un solo pending por aplicante por animal                                         | ✅                        |
| Gestión de solicitudes para el shelter (aprobar/rechazar con nota)                      | ✅                        |
| Chat enmascarado adoptante ↔ organización                                               | ✅ (ChatThread existente) |
| Marcar como adoptado + estado: Available/InProcess/Adopted/Paused/Removed               | ✅                        |
| Upload/delete de fotos a Azure Blob `adoption-photos` (hasta 5 por animal)              | ✅                        |
| Toggle "Adopciones" en el mapa público con pins diferenciados                           | ✅                        |
| Ferias de adopción con fecha, lugar GPS y lista de animales                             | ✅ ShelterPlus            |
| Alertas push geofenceadas para ferias (radio 10km, rate-limited)                        | ✅ ShelterPlus            |
| Notificaciones: AdoptionInterest, AdoptionApproved, AdoptionRejected, AdoptionFairAlert | ✅                        |
| Bot WhatsApp: "adoptar", "quiero adoptar" → link directorio                             | ✅                        |
| Bot WhatsApp: "dar en adopcion", "shelter" → link registro Ally                         | ✅                        |
| Panel del shelter (Ally Shelter) — listado paginado con acciones                        | ✅                        |
| Publicación inline con formulario 2 pasos (info + fotos)                                | ✅                        |
| Admin panel tab "Adopciones" con stats + moderación (remover/pausar/restaurar)          | ✅                        |
| Audit log para acciones de moderación admin                                             | ✅                        |
| Gating ShelterBasic: máximo 5 animales activos simultáneos                              | ✅                        |
| Gating ShelterPlus: animales ilimitados + ferias                                        | ✅ ₡8,000/mes             |

### Planes de adopción

| Plan           | Precio     | Límite                                      |
| -------------- | ---------- | ------------------------------------------- |
| `ShelterBasic` | Gratis     | 5 animales activos; sin ferias              |
| `ShelterPlus`  | ₡8,000/mes | Ilimitados + ferias + pin destacado en mapa |

### API endpoints de adopciones

| Método | Endpoint                                     | Auth        | Descripción                                |
| ------ | -------------------------------------------- | ----------- | ------------------------------------------ |
| GET    | `/api/adoptions/animals`                     | —           | Directorio público filtrable y paginado    |
| GET    | `/api/adoptions/animals/map`                 | —           | Todos los disponibles (cap 500, para mapa) |
| GET    | `/api/adoptions/animals/{id}`                | —           | Perfil completo del animal                 |
| GET    | `/api/adoptions/fairs`                       | —           | Ferias próximas, geo-filtradas             |
| POST   | `/api/adoptions/animals`                     | Ally        | Publicar animal                            |
| PATCH  | `/api/adoptions/animals/{id}`                | Ally        | Editar detalles                            |
| POST   | `/api/adoptions/animals/{id}/photos`         | Ally        | Subir foto (max 5MB)                       |
| DELETE | `/api/adoptions/animals/{id}/photos`         | Ally        | Borrar foto específica                     |
| GET    | `/api/adoptions/animals/mine`                | Ally        | Animales del shelter (paginado)            |
| GET    | `/api/adoptions/animals/{id}/applications`   | Ally        | Ver solicitudes                            |
| PATCH  | `/api/adoptions/applications/{id}/review`    | Ally        | Aprobar/rechazar                           |
| PATCH  | `/api/adoptions/animals/{id}/mark-adopted`   | Ally        | Marcar como adoptado                       |
| POST   | `/api/adoptions/fairs`                       | Ally (Plus) | Crear feria                                |
| POST   | `/api/adoptions/animals/{id}/apply`          | Owner       | Aplicar para adoptar                       |
| DELETE | `/api/adoptions/applications/{id}`           | Owner       | Retirar solicitud                          |
| GET    | `/api/adoptions/applications/mine`           | Auth        | Mis solicitudes                            |
| GET    | `/api/admin/adoptions/stats`                 | Admin       | Estadísticas globales                      |
| GET    | `/api/admin/adoptions/animals`               | Admin       | Listado admin con filtros                  |
| PATCH  | `/api/admin/adoptions/animals/{id}/moderate` | Admin       | Remover/pausar/restaurar                   |

---

## Tiendas de Mascotas B2B — Completo ✅ (agosto 2026)

| Feature                                      | Estado                               |
| -------------------------------------------- | ------------------------------------ |
| Registro de tienda con ubicación en mapa     | ✅                                   |
| Catálogo de productos (7 categorías)         | ✅                                   |
| Imágenes de productos (upload, resize 800px) | ✅                                   |
| Pedidos in-app con SINPE Móvil               | ✅ StorePartner/StorePlus únicamente |
| Estado máquina de pedidos (8 estados)        | ✅                                   |
| Panel de dashboard del dueño de tienda       | ✅                                   |
| Directorio público `/tiendas` + mapa         | ✅                                   |
| Featured stores (orden prioritario en mapa)  | ✅ IsFeatured DESC                   |
| Aprobación admin de tiendas                  | ✅                                   |
| Notificación push al recibir pedido          | ✅                                   |
| Mis pedidos con historial paginado           | ✅                                   |

---

## Vallas Publicitarias (Billboard) — Completo ✅ (agosto 2026)

| Feature                                       | Estado |
| --------------------------------------------- | ------ |
| 4 placements: Map, Dashboard, Directory, Feed | ✅     |
| Estado máquina: Draft→Active↔Paused→Expired   | ✅     |
| Prioridad por placement (0-100)               | ✅     |
| Upload de imagen (5MB, resize 1200px)         | ✅     |
| CTA con validación de URL same-origin/HTTPS   | ✅     |
| Dismissal con TTL 24h (localStorage)          | ✅     |
| Admin CRUD + activar/pausar/imagen            | ✅     |
| Paginación en admin list                      | ✅     |

---

_Generado 2026-08-01 · actualizado 2026-08-24 · PawTrack CR · fuente: inspección directa del código_
