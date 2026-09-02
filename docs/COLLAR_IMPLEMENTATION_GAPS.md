# Análisis de Gaps: Sistema de Collar Tipo Apple Tag — PawTrack CR

> **Evaluación de profundidad:** 2026-09-01  
> **Alcance:** Comparación entre la implementación actual (Fases 1–3) y un sistema tipo Apple AirTag/Find My Pet de nivel enterprise.

---

## Resumen Ejecutivo

**Estado Actual (✅ Implementado):**

- Activación por serial (QR + manual input) — **Fase 1–3 completa**
- Device key authentication (X-Collar-Key middleware)
- Admin inventory (register, mark-sold, revoke, bulk import)
- Location ingest endpoint para dispositivos
- Integración Tractive (OAuth2 + polling)
- Deactivation/transferencia básica

**Crítico (⚠️ Falta):**

1. **Notificaciones de conectividad** — sin alertas cuando collar va offline
2. **Auditoría de eventos** — no hay log de activaciones/desactivaciones/revokes
3. **Modo perdido/búsqueda activa** — sin coordinación con sightings + notificaciones
4. **Transferencia segura** — sin validación de hand-off entre propietarios
5. **Geofencing** — sin alertas por salida de zona segura
6. **Histórico de cambios** — sin tracking de quién activó/desactivó cuándo

**Alto valor (✨ Recomendado para MVP+1):**

1. Alertas de batería baja
2. Búsqueda remota (find my pet)
3. Nearby detection (Bluetooth/NFC)
4. Historial de propietarios anteriores
5. OTA firmware updates

**Roadmap futuro (🚀 Fase 4+):**

1. Collar hardware propio (ESP32-S3 + SIM7080G)
2. Sonido/vibración remota
3. Integración NFC
4. Modo de seguridad (anti-robo)
5. API webhooks para terceros

---

## 1. Notificaciones & Alertas (CRÍTICO)

### 1.1 Offline Detection

**Estado:** ❌ No implementado

**Qué falta:**

- Job que detecte cuando un collar no ha reportado posición > X minutos (configurable)
- Sistema de notificaciones al dueño: "Tu collar no responde desde hace 2 horas"
- Reclasificación automática de collar como "offline" en la UI
- Reintento de conectividad con backoff exponencial

**Impacto en UX:**

- Dueño no sabe si el collar está muerto, perdido, o sin señal
- Potencial falsa alarma si batería se agota sin aviso

**Endpoints necesarios:**

```
POST /api/collars/{collarId}/notifications/enable-offline-alerts
GET  /api/collars/{collarId}/connectivity-status
```

**Comando necesario:**

```csharp
public sealed record SetCollarOfflineThresholdCommand(
    Guid CollarId,
    TimeSpan OfflineAfter,  // e.g., 2 horas
    Guid OwnerId) : IRequest<Result<bool>>;
```

---

### 1.2 Battery Alerts

**Estado:** ❌ No implementado

**Qué falta:**

- Threshold configurable (default 20%, warning 10%)
- Push notification cuando batería < threshold
- Frecuencia de alertas (evitar spam — máx 1/día)
- Histórico de alertas en el collar detail

**Endpoints necesarios:**

```
POST /api/collars/{collarId}/battery-alert-threshold
GET  /api/collars/{collarId}/battery-history
```

---

### 1.3 Connectivity & Status Notifications

**Estado:** ⚠️ Parcial (existe sistema genérico de notificaciones, pero no aplicado a collares)

**Qué falta:**

- `CollarConnectivityNotification` domain event
- Mapping en `OutboxProcessor` para publicar eventos
- Template en `NotificationHub` para "Collar offline"
- User preference para habilitar/deshabilitar estas alertas

**Comando necesario:**

```csharp
public sealed record UpdateCollarNotificationPreferencesCommand(
    Guid UserId,
    bool EnableOfflineAlerts,
    bool EnableBatteryAlerts,
    int BatteryThresholdPercent = 20) : IRequest<Result<bool>>;
```

---

## 2. Auditoría & Historial (CRÍTICO)

### 2.1 Collar Event Audit Log

**Estado:** ❌ No implementado — Solo existe `AuditLogEntry` genérico sin collar-specific events

**Qué falta:**

- `CollarTagAuditEntry` o extender `AuditLogEntry` con collar-specific events
- Events a registrar:
  - `Serial registered by admin` (quién, cuándo, firmware version)
  - `Serial marked as sold` (cuándo)
  - `Collar activated` (usuario, mascota, fecha)
  - `Collar deactivated` (usuario, fecha, razón)
  - `Device key revoked` (por qué — stolen, compromised, etc.)
  - `Device key regenerated`
  - `Firmware updated` (versión anterior → nueva)
  - `Location ingest failed` (N failed attempts)

**Tabla requerida:**

```csharp
public sealed class CollarAuditEntry
{
    public Guid Id { get; private set; }
    public Guid CollarId { get; private set; }
    public Guid? UserId { get; private set; }  // null si admin desde backend job
    public CollarAuditEvent Event { get; private set; }
    public string Details { get; private set; }  // JSON payload
    public DateTimeOffset CreatedAt { get; private set; }

    // INDEX (CollarId, CreatedAt DESC)
}

public enum CollarAuditEvent
{
    SerialRegistered,
    SerialMarkedSold,
    Activated,
    Deactivated,
    DeviceKeyRevoked,
    DeviceKeyRegenerated,
    FirmwareUpdated,
    LocationIngestFailed,
    OfflineAlertSent,
    BatteryAlertSent,
}
```

**Endpoints necesarios:**

```
GET /api/collars/{collarId}/audit-log?skip=0&take=50
GET /api/admin/collar-tags/{serial}/audit-log
```

---

### 2.2 Ownership Transfer History

**Estado:** ❌ No implementado

**Qué falta:**

- Historial de todos los propietarios previos de un serial
- Cuándo fue activado/desactivado para cada propietario
- Razón de desactivación (transferencia, revoke, etc.)

**Tabla requerida:**

```csharp
public sealed class CollarOwnershipTransfer
{
    public Guid Id { get; private set; }
    public Guid CollarTagId { get; private set; }  // ref a CollarTag.Serial
    public Guid? PreviousOwnerId { get; private set; }
    public Guid? CurrentOwnerId { get; private set; }
    public DateTimeOffset TransferredAt { get; private set; }
    public string Reason { get; private set; }  // "Resale", "Lost", "Replaced", etc.
}
```

---

## 3. Modo Perdido & Búsqueda Activa (CRÍTICO)

### 3.1 "Find My Pet" - Búsqueda Remota

**Estado:** ❌ No implementado

**Qué falta:**

- Modo "perdido" en el collar (`Collar.IsLost` flag)
- Cuando se activa:
  - El collar reporta más frecuente (ej. cada 30 seg en lugar de cada 5 min)
  - Notificaciones push a suscriptores cercanos (community help)
  - Integración con LostPetEvent existente
  - Broadcast automático en el mapa público

**Comando necesario:**

```csharp
public sealed record ActivateCollarLostModeCommand(
    Guid CollarId,
    Guid OwnerId,
    string? LostEventId = null) : IRequest<Result<bool>>;

public sealed record DeactivateCollarLostModeCommand(
    Guid CollarId,
    Guid OwnerId,
    string? Reason = null) : IRequest<Result<bool>>;
```

**Cambio en el domain:**

```csharp
public sealed class Collar
{
    public bool IsLost { get; private set; }
    public DateTimeOffset? LostModeActivatedAt { get; private set; }

    public void ActivateLostMode()
    {
        IsLost = true;
        LostModeActivatedAt = DateTimeOffset.UtcNow;
    }

    public void DeactivateLostMode()
    {
        IsLost = false;
        LostModeActivatedAt = null;
    }
}
```

**Endpoints necesarios:**

```
POST /api/collars/{collarId}/lost-mode/activate
POST /api/collars/{collarId}/lost-mode/deactivate
GET  /api/collars/{collarId}/lost-mode-status
```

---

### 3.2 Integración con LostPetEvent & Broadcast

**Estado:** ⚠️ Parcial (existe LostPetEvent pero sin collar awareness)

**Qué falta:**

- Enlace bidireccional: `LostPetEvent ↔ Collar.Id`
- Cuando se activa lost mode:
  - Auto-crear `LostPetEvent` si no existe
  - Incrementar frecuencia de polling
  - Notificar al coordinador de búsqueda
- Cuando se reúne la mascota:
  - Auto-desactivar lost mode
  - Cerrar LostPetEvent

---

## 4. Transferencia Segura Entre Propietarios (CRÍTICO)

### 4.1 Handover Code (tipo Apple)

**Estado:** ⚠️ Parcial (existe `HandoverCode` pero no para collares)

**Qué falta:**

- Extender `HandoverCode` para soportar collar tags
- Flow:
  1. Propietario A toca "Transfer collar" → genera código de 6 dígitos + QR
  2. Propietario B escanea QR + ingresa código + selecciona su mascota
  3. Backend valida y transfiere automáticamente
  4. Collar queda en status "Unactivated" pero con ownership audit
  5. Propietario B puede reactivarlo o marcarlo como vendido

**Comando necesario:**

```csharp
public sealed record GenerateCollarHandoverCodeCommand(
    Guid CollarId,
    Guid OwnerId) : IRequest<Result<HandoverCodeDto>>;

public sealed record RedeemCollarHandoverCodeCommand(
    string HandoverCode,
    int Pin,
    Guid NewOwnerId,
    Guid NewPetId) : IRequest<Result<bool>>;
```

---

### 4.2 Validaciones necesarias

- Propietario A debe ser owner de la mascota vinculada
- Propietario B debe tener Plus plan
- Código válido por 7 días
- Máx 5 intentos de PIN antes de bloqueo
- Ambos propietarios reciben notificación del handover

---

## 5. Geofencing & Alertas de Zona (ALTO VALOR)

### 5.1 Safe Zone (Cercas virtuales)

**Estado:** ❌ No implementado

**Qué falta:**

- `CollarSafeZone` entity (polygon, centro+radio, o lista de direcciones)
- Geofence algo (punto en polígono, distancia a punto, etc.)
- Job que verifica si collar salió/entró a zona
- Notificación al dueño: "Tu mascota salió de la zona segura"

**Tabla requerida:**

```csharp
public sealed class CollarSafeZone
{
    public Guid Id { get; private set; }
    public Guid CollarId { get; private set; }
    public string Name { get; private set; }  // "Casa", "Parque cercano"
    public string GeoJsonPolygon { get; private set; }
    public bool Enabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
```

**Endpoints necesarios:**

```
POST   /api/collars/{collarId}/safe-zones
GET    /api/collars/{collarId}/safe-zones
PUT    /api/collars/{collarId}/safe-zones/{zoneId}
DELETE /api/collars/{collarId}/safe-zones/{zoneId}
POST   /api/collars/{collarId}/safe-zones/{zoneId}/toggle
```

---

## 6. Historial de Ubicaciones (CRÍTICO PARA ENTERPRISE)

### 6.1 Query Performance & Retention Policy

**Estado:** ⚠️ Parcial (existe `CollarLocation` y purge job, pero sin optimizaciones)

**Qué falta:**

- Índices específicos para rango de fechas + CollarId (✅ existe)
- Agregación por hora/día (rolling up raw data para larga retención)
- Endpoint de descarga de histórico (CSV/JSON)
- Retención diferenciada:
  - Raw: 30 días (ya existe)
  - Hourly rollup: 1 año
  - Daily rollup: 5 años
- Cifrado en reposo si es sensible

**Endpoints necesarios:**

```
GET /api/collars/{collarId}/location-history?from=2026-01-01&to=2026-01-31&granularity=hourly
GET /api/collars/{collarId}/location-history/export.csv
GET /api/collars/{collarId}/location-heatmap
```

---

## 7. Firmware & OTA Updates (ROADMAP FUTURO)

### 7.1 Firmware Management

**Estado:** ❌ No implementado

**Qué falta:**

- Versionado de firmware (semver: 1.0.0, 1.0.1, etc.)
- Tabla `CollarFirmwareVersion` con changelog
- Endpoint para descargar firmware binario (con validación)
- Device puede reportar su versión en cada ingest
- Admin puede "promote" una versión a "stable" o "rollback"

**Tabla requerida:**

```csharp
public sealed class CollarFirmwareVersion
{
    public Guid Id { get; private set; }
    public string Version { get; private set; }  // "1.0.0"
    public string Changelog { get; private set; }
    public string BinaryUrl { get; private set; }  // Blob Storage URL
    public string BinaryHashSha256 { get; private set; }
    public bool IsStable { get; private set; }
    public bool IsDeprecated { get; private set; }
    public DateTimeOffset ReleasedAt { get; private set; }
}
```

**Endpoints necesarios:**

```
GET    /api/collars/firmware/latest
GET    /api/collars/firmware/{version}/download
POST   /api/admin/collar-firmware
PUT    /api/admin/collar-firmware/{version}/promote
DELETE /api/admin/collar-firmware/{version}/deprecate
```

---

## 8. Seguridad & Anti-Fraude (CRÍTICO)

### 8.1 Rate Limiting Mejorado

**Estado:** ⚠️ Parcial (rate limit básico en `/tag/{serial}`)

**Qué falta:**

- Rate limiting por IP en `/collars/ingest` (evitar spam de fake locations)
- Rate limiting por device key en `/collars/ingest` (detect replay attacks)
- Alertas si un serial ve > 10 ubicaciones/min (indicador de spoofing)

**Config recomendada:**

```json
{
  "RateLimits": {
    "collar-serial-check": "5 per minute per IP",
    "collar-ingest": "60 per minute per device-key",
    "collar-ingest-per-ip": "1000 per minute per IP"
  }
}
```

---

### 8.2 Device Key Rotation

**Estado:** ⚠️ Parcial (existe revoke, pero sin rotación automática)

**Qué falta:**

- Endpoint para rotar key manualmente (owner o admin)
- Policy de rotación automática cada 90 días (opcional)
- Notification al dueño: "La clave de tu collar fue rotada. Reconfigura tu dispositivo."
- Historial de rotaciones

**Comando necesario:**

```csharp
public sealed record RotateCollarDeviceKeyCommand(
    Guid CollarId,
    Guid OwnerId) : IRequest<Result<RotateKeyResultDto>>;

public sealed record RotateKeyResultDto(string NewKey, string OldKeyRevokedAt);
```

---

### 8.3 Validación de Payload

**Estado:** ⚠️ Parcial (existe validación básica, pero sin checksums)

**Qué falta:**

- HMAC-SHA256 signature en cada ingest payload
  - Header: `X-Collar-Signature: hmac-sha256=...`
  - Previene man-in-the-middle
- Validación de timestamp (no más de 5 min de diferencia)
- Secuencia monotónica de ubicaciones (detectable si se cambia orden)

---

## 9. Testing & Observabilidad (MUST-HAVE)

### 9.1 End-to-End Tests

**Estado:** ⚠️ Parcial (existe test básico de activation → ingest)

**Qué falta:**

- E2E: Activation → Offline detection → Reconnection → Battery alert
- E2E: Lost mode activation → Broadcast integration
- E2E: Handover code generation → Redemption → New activation
- E2E: Geofence trigger → Notification
- Load test: 10K devices reporting 1 location/min = 166 req/sec

---

### 9.2 Observabilidad

**Estado:** ⚠️ Parcial (existe Application Insights, pero sin collar-specific metrics)

**Qué falta:**

- Métrica: "Active collars" (segmentado por provider)
- Métrica: "Average ingest latency" (ms)
- Métrica: "Offline collar count" (por umbral)
- Métrica: "Handover completion rate" (%)
- Alert: "Ingest failure rate > 5%"
- Dashboard: Collar health overview

**Queries KQL recomendadas:**

```kusto
// Ingest success rate last 24h
customMetrics
| where name == "CollarIngestSuccess"
| summarize success = sum(value), total = dcount(customDimensions["collarId"])
| extend rate = success * 100.0 / total

// Offline collar detection
customEvents
| where name == "CollarWentOffline"
| summarize count() by bin(timestamp, 1h)
```

---

## 10. Frontend UI Gaps (CRITICAL UX)

### 10.1 Collar Detail Page

**Estado:** ⚠️ Parcial (existe tab GPS, pero falta presentación completa)

**Qué falta:**

- Status badge actual (Active / Offline / Lost / Low Battery)
- Last seen timestamp + accuracy
- Battery percentage + history chart
- Firmware version + update available indicator
- Audit log tab (mostra quién activó/desactivó/cuándo)
- Safe zones tab
- Transfer/Handover button (QR + PIN)
- Revoke credentials button (admin only)

**Components necesarios:**

```tsx
<CollarStatusBadge collar={collar} />
<CollarBatteryChart collarId={collarId} days={7} />
<CollarAuditLogTab collarId={collarId} />
<CollarSafeZonesMap collarId={collarId} />
<CollarHandoverDialog collarId={collarId} onSuccess={refresh} />
```

---

### 10.2 Admin Inventory Dashboard

**Estado:** ⚠️ Parcial (existe tabla básica)

**Qué falta:**

- Filtros: Status (Unactivated/Activated/Deactivated), Sold status, Active > X days
- Búsqueda: por serial, dueño, mascota, fecha
- Bulk actions: Mark multiple as sold, Bulk revoke, Bulk firmware update
- Métricas: Total seriales, activados, tasa de activación, ingresos
- Alertas: Seriales no activados hace > 90 días (dead inventory)

---

## 11. Integración con Sightings (FUTURE LINKAGE)

### 11.1 Automatic Sighting Match

**Estado:** ❌ No implementado

**Qué falta:**

- Cuando mascota está en Lost mode + collar reporta ubicación → auto-crear Sighting
- Notificar al reportador original
- Presencia de collar aumenta confianza en match

---

## Priorización Recomendada

### Fase 4 (Enterprise Release) — 4–6 semanas

1. **Notificaciones offline + battery alerts** (user-facing, crítico)
2. **Auditoría de eventos** (compliance, legal)
3. **Transferencia segura (handover codes)** (retail readiness)
4. **Modo perdido + integración LostPetEvent** (core feature)

### Fase 5 (MVP+1) — 2–3 semanas

5. Geofencing (alertas de zona)
6. Historial mejorado + export
7. Dashboard admin mejorado
8. E2E testing

### Roadmap futuro (Fase 6+)

9. Firmware OTA
10. Device key rotation
11. Collar hardware propio
12. Sightings auto-match

---

## Checksum de Implementación Actual

| Feature           | Backend | Frontend | Tests | Docs |
| ----------------- | ------- | -------- | ----- | ---- |
| Serial activation | ✅      | ✅       | ✅    | ✅   |
| Device key auth   | ✅      | ✅       | ✅    | ✅   |
| Location ingest   | ✅      | ✅       | ✅    | ✅   |
| Offline detection | ❌      | ❌       | ❌    | ❌   |
| Battery alerts    | ❌      | ❌       | ❌    | ❌   |
| Audit logging     | ❌      | ❌       | ❌    | ❌   |
| Lost mode         | ❌      | ❌       | ❌    | ❌   |
| Handover codes    | ❌      | ❌       | ❌    | ❌   |
| Geofencing        | ❌      | ❌       | ❌    | ❌   |
| Firmware OTA      | ❌      | ❌       | ❌    | ❌   |
| Key rotation      | ❌      | ❌       | ❌    | ❌   |
| E2E tests         | ⚠️      | ⚠️       | ⚠️    | ⚠️   |

---

## Epics sugeridos para crear en backlog

1. `epic/collar-connectivity-notifications` — Offline + battery alerts
2. `epic/collar-audit-trail` — Event logging + compliance
3. `epic/collar-secure-transfer` — Handover codes
4. `epic/collar-lost-mode` — Find my pet integration
5. `epic/collar-geofencing` — Safe zones
6. `epic/collar-firmware-management` — OTA updates
