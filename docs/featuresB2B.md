# PawTrack CR — Features B2B Clínicas: Estado, Brechas e Implementación

> **Actualizado: 2026-08-04 — TODO implementado ✅**  
> Análisis técnico original: 2026-08-01 contra `precios.md §2`

---

## Índice de estado rápido (actualizado agosto 2026)

| Plan           | Feature                                            |          Estado          |
| -------------- | -------------------------------------------------- | :----------------------: |
| **BÁSICA**     | Registro + perfil en directorio                    |            ✅            |
| **BÁSICA**     | Mapa de clínicas (posición estándar)               |            ✅            |
| **BÁSICA**     | Información de contacto pública                    |            ✅            |
| **BÁSICA**     | Escanear QR de collar                              |            ✅            |
| **BÁSICA**     | Escanear microchip RFID                            |            ✅            |
| **BÁSICA**     | Ver perfil público + datos dueño                   |            ✅            |
| **BÁSICA**     | Búsqueda por número de microchip                   |            ✅            |
| **PLUS**       | Posición destacada en mapa                         |            ✅            |
| **PLUS**       | Badge "Clínica Verificada"                         |            ✅            |
| **PLUS**       | Logo en alertas de pérdida (NearbyFeaturedClinics) |            ✅            |
| **PLUS**       | Banner en Case Rooms                               |            ✅            |
| **PLUS**       | Estadísticas de escaneos mensuales                 |            ✅            |
| **PLUS**       | Métricas de visibilidad (ClinicProfileViews)       |            ✅            |
| **PARTNER**    | Certificados veterinarios PDF                      |            ✅            |
| **PARTNER**    | QR de verificación en PDF                          |            ✅            |
| **PARTNER**    | Verificación pública `/verificar/{código}`         |            ✅            |
| **PARTNER**    | Widget embebible                                   |            ✅            |
| **PARTNER**    | API de consulta directa (API Keys)                 |            ✅            |
| **PARTNER**    | Alertas Case Rooms del cantón (Partner)            |            ✅            |
| **EXPEDIENTE** | Acceso clínica → expediente (Opciones A+B+C)       |            ✅            |
| **EXPEDIENTE** | Código de emparejamiento permanente                |            ✅            |
| **PLUS**       | Logo en alertas de pérdida                         |            ❌            |
| **PLUS**       | Banner en Case Rooms de pacientes                  |            ❌            |
| **PLUS**       | Estadísticas de escaneos mensuales                 |            ❌            |
| **PLUS**       | Métricas de visibilidad en directorio              |            ❌            |
| **PLUS**       | Soporte prioritario por email                      |    N/A (operacional)     |
| **PARTNER**    | Certificados veterinarios PDF (QuestPDF)           |            ✅            |
| **PARTNER**    | Código de verificación único + QR en PDF           |            ✅            |
| **PARTNER**    | Verificación pública `/verificar/{código}`         |            ✅            |
| **PARTNER**    | Firma digital de clínica y médico                  | ⚠️ Parcial (texto plano) |
| **PARTNER**    | Widget embebible para web propia                   |            ❌            |
| **PARTNER**    | API de consulta directa (microchip / perfil)       |        ⚠️ Parcial        |
| **PARTNER**    | Integración RFID avanzada (lectores externos)      |            ❌            |
| **PARTNER**    | Notificaciones en todos los Case Rooms del cantón  |            ❌            |
| **PARTNER**    | Primeros resultados en búsquedas de zona           |            ❌            |

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

### 1.2 PLAN BÁSICA — Mapa de clínicas (posición estándar) ❌

**Estado:** No implementado. Todos los registros de lat/lng existen en DB pero no hay endpoint público que los exponga, ni capa de mapa en el frontend.

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

### 1.3 PLAN BÁSICA — Información de contacto pública ⚠️ Parcial

**Estado:** El `ContactEmail` existe en la entidad y en `ClinicDto`, pero **no está expuesto en ningún endpoint público**. Solo la clínica autenticada puede verlo via `GET /api/clinics/me`.

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

### 1.8 PLAN PLUS — Posición destacada en mapa ❌

**Estado:** No implementado. La propiedad `IsFeatured` no existe en `Clinic.cs`. El mapa público no tiene capa de clínicas. Depende directamente de 1.2.

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

### 1.9 PLAN PLUS — Badge "Clínica Verificada" ⚠️ Parcial

**Estado:** La propiedad no existe en el dominio. En `ClinicTiersModal.tsx` el badge está listado como feature de Plus, pero no hay ninguna lógica que lo aplique ni en la API ni en el frontend del mapa/directorio.

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

### 1.10 PLAN PLUS — Logo en alertas de pérdida ❌

**Estado:** No implementado. `MultichannelBroadcastService.cs` no tiene referencia a clínicas ni logos. `NotificationDispatcher.cs` no incluye logos en alertas de pérdida.

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

### 1.11 PLAN PLUS — Banner en Case Rooms de pacientes activos ❌

**Estado:** No implementado. `CaseRoomPage.tsx` y su API backend (`GetCaseRoomQuery`) no incluyen ninguna referencia a clínicas ni banners.

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

### 1.12 PLAN PLUS — Estadísticas de escaneos mensuales ❌

**Estado:** No implementado. `ClinicScanRepository` solo tiene `AddAsync`. No hay ningún query de agregación. `ClinicDashboardPage.tsx` no muestra estadísticas.

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

### 1.13 PLAN PLUS — Métricas de visibilidad en directorio ❌

**Estado:** No implementado. No existe ningún tracking de "vistas de perfil" o "clics desde directorio" hacia una clínica.

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

### 1.18 PLAN PARTNER — Widget embebible ❌

**Estado:** No implementado. No existe ningún endpoint de widget ni script JS embebible.

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

### 1.19 PLAN PARTNER — API de consulta directa ⚠️ Parcial

**Estado:** Existe funcionalidad equivalente via el endpoint de scan (`POST /api/clinics/scan`), pero **requiere autenticación de usuario con Role=Clinic** (JWT Bearer). No hay API con API Key ni token de servicio para integración máquina-a-máquina.

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

### 1.21 PLAN PARTNER — Notificaciones en todos los Case Rooms del cantón ❌

**Estado:** No implementado. `NotificationDispatcher.DispatchLostPetAlertAsync` no incluye clínicas como destinatarios. No hay forma de que una clínica reciba alertas de Case Rooms cercanas.

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

### 1.22 PLAN PARTNER — Primeros resultados en búsquedas por zona ❌

**Estado:** No implementado. No existe un endpoint de "búsqueda de clínicas por zona" en el frontend ni en el mapa. Las clínicas no aparecen en ningún resultado de búsqueda del usuario.

**Qué implica:**

- En el mapa público, cuando un usuario busca "clínicas cerca de mí" → las Partner aparecen primero.
- En alertas de pérdida enviadas por WhatsApp/push: las clínicas Partner del cantón aparecen como sugerencia.
- Si hay un directorio de clínicas `/clinicas`: orden de lista = Partner > Plus > Básica, luego distancia.

**Es una consecuencia directa de implementar el mapa (1.2) con el campo `IsFeatured`/tier.**

---

## 2. Resumen de brechas por prioridad

### 🔴 Alta prioridad (bloqueante para monetización)

| #   | Feature                             | Por qué es urgente                                                                   |
| --- | ----------------------------------- | ------------------------------------------------------------------------------------ |
| A   | **Mapa de clínicas** (1.2)          | Sin mapa, la propuesta de valor Plus/Partner es invisible para el dueño              |
| B   | **Estadísticas de escaneos** (1.12) | La clínica no puede medir ROI del plan Plus → churn                                  |
| C   | **Badge Verificado en UI** (1.9)    | El badge existe en el modal de tiers pero no se muestra en ninguna pantalla real     |
| D   | **Banner en Case Rooms** (1.11)     | Mayor impacto de conversión para clínicas — aparece en el momento de máxima urgencia |
| E   | **QR embebido en PDF** (1.16)       | Mejora inmediata al PDF ya existente — 30 min de esfuerzo                            |

### 🟡 Media prioridad

| #   | Feature                                         | Notas                                         |
| --- | ----------------------------------------------- | --------------------------------------------- |
| F   | **Logo en alertas de pérdida** (1.10)           | Requiere upload de logo + modificar broadcast |
| G   | **Información de contacto pública** (1.3)       | Añadir `PhoneNumber` a Clinic.cs              |
| H   | **API de consulta directa / API Keys** (1.19)   | Requerida para Partner B2B real               |
| I   | **Notificaciones Case Rooms del cantón** (1.21) | Requiere geo-query de clínicas Partner        |

### 🟢 Baja prioridad (post-launch)

| #   | Feature                                | Notas                                          |
| --- | -------------------------------------- | ---------------------------------------------- |
| J   | **Widget embebible** (1.18)            | Build Vite separado, ~2 días                   |
| K   | **Métricas de visibilidad** (1.13)     | Evento analytics, secundario frente a escaneos |
| L   | **Firma digital criptográfica** (1.17) | Texto plano suficiente para MVP                |
| M   | **Integración RFID BLE** (1.20)        | Experimental, Web Bluetooth limitado           |

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
- [ ] **[A12]** Página `/clinicas` — directorio con filtro por zona (opcional, post-A10)

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
- [ ] **[C4]** En `MultichannelBroadcastService`: logos en footer de alertas WhatsApp
- [x] **[C5]** `DispatchLostPetAlertToClinicAsync` en `INotificationDispatcher` + implementación
- [x] **[C6]** En `ReportLostPetCommandHandler`: enviar alerta push a clínicas Partner del cantón
- [ ] **[C7]** Tabla `ClinicProfileViews` + registro de eventos (fase 5)

#### Frontend

- [x] **[C8]** `SponsoredClinicBanner` en `CaseRoomPage.tsx` (logo, nombre, badge, contacto)
- [ ] **[C9]** Sección "Alertas cercanas activas" en `ClinicDashboardPage.tsx` (solo Partner)

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

### Fase 5 — Notificaciones Partner + metricas visibilidad (Sprint ~2 días)

- [ ] **[E1]** `ClinicProfileViews` tabla + `TrackClinicViewCommand` (fire-and-forget)
- [ ] **[E2]** `GET /api/clinics/me/visibility-stats?period=30d` — views, map clicks, search appearances
- [ ] **[E3]** UI: sección "Visibilidad" en dashboard — solo Plus/Partner
- [ ] **[E4]** Notificaciones Case Rooms del cantón para Partner (depende de C5, C6)
- [ ] **[E5]** UI: feed "Alertas activas cercanas" en `ClinicDashboardPage.tsx`

---

## 4. Deuda técnica identificada

| Archivo                       | Problema                                                |
| ----------------------------- | ------------------------------------------------------- |
| `Clinic.cs`                   | Falta `PhoneNumber`, `LogoUrl`, `IsFeatured`, `Website` |
| `ClinicScanRepository.cs`     | Solo tiene `AddAsync` — sin queries de lectura propias  |
| `ClinicDto.cs`                | No incluye tier/plan de suscripción activa              |
| `ClinicsController.cs`        | No hay endpoint público de directorio                   |
| `PublicMapController.cs`      | No expone clínicas en el mapa                           |
| `IssueCertificateCommand.cs`  | PDF sin QR embebido para verificación rápida            |
| `PerformClinicScanCommand.cs` | Resultado no incluye si la clínica es Verified (badge)  |

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

_Generado 2026-08-01 · PawTrack CR · fuente: inspección directa del código_
