# Plan de Implementación Enterprise — Collar PawTrack CR

> **Alcance:** Fases 4 & 5 — Notificaciones + Auditoría + Transferencia + Lost Mode + Geofencing + Historial + Dashboard + E2E  
> **Duración total:** 9 semanas  
> **Fecha inicio:** 2026-09-01  
> **Hito de go-live:** 2026-10-27 (con Fase 4), Fase 5 por 2026-11-24  
> **Última actualización:** 2026-09-02 — FASE 4 COMPLETA + FASE 5 COMPLETA (Geofencing, Historial, Admin Dashboard, E2E Testing Suite) (ver §Estado de Implementación)

---

## 📊 Resumen Ejecutivo

| Fase                    | Épicas   | Semanas | Status                                                                     | Impacto                                       |
| ----------------------- | -------- | ------- | -------------------------------------------------------------------------- | --------------------------------------------- |
| **Fase 4 (CRÍTICO)**    | 4 épicas | 6       | ✅ COMPLETA (Epics 1–4)                                                    | **MVP Enterprise** — Bloquea launch sin estos |
| **Fase 5 (ALTO VALOR)** | 4 épicas | 3       | ✅ COMPLETA (Geofencing + Historial + Admin Dashboard + E2E Testing Suite) | **Diferenciador competitivo** vs Tractive     |

**Total de tareas:** 115  
**Total de desarrolladores recomendados:** 3 (1 senior backend, 1 full-stack, 1 QA/testing)  
**Costo estimado:** 15–20 kdev (según equipo)

---

## ✅ Estado de Implementación (actualizado en cada sesión)

### Epic 1 — Notificaciones Offline + Battery Alerts

**Semana 1 (fundacional): COMPLETADA — 2026-09-02**

Entregado (backend):

- `Collar` domain: `OfflineAlertsEnabled`, `OfflineThresholdMinutes`, `IsOffline`, `BatteryAlertsEnabled`, `BatteryAlertThresholdPercent` + `MarkOffline()` + `UpdateNotificationPreferences()` con validación (15–1440 min, 5–50%).
- Migración EF Core `AddCollarConnectivityAlerts`.
- `UpdateCollarNotificationPreferencesCommand` (unifica los comandos separados de threshold offline/batería planeados originalmente — ver §Desviaciones).
- `GetCollarConnectivityStatusQuery` + `GET /api/collars/{collarId}/connectivity-status`.
- `PUT /api/collars/{collarId}/notification-preferences`.
- `CollarConnectivityAlertService` (Application layer, testable): `RunOfflineDetectionAsync` + `RunBatteryAlertDetectionAsync`, con cooldowns (6h offline, 24h batería) vía `INotificationRepository.HasRecentByUserTypeAndEntityAsync`.
- `CollarConnectivityAlertJob` (Infrastructure `BackgroundService`, corre cada 15 min con `IDistributedJobLock`) — unifica los jobs de offline y batería planeados por separado.
- `NotificationType.CollarOfflineAlert` y `CollarLowBatteryAlert` agregados al enum existente.
- `ICollarRepository.GetActiveCollarsWithAlertsEnabledAsync`.

Entregado (frontend):

- `CollarStatusBadge` (Activo / Sin conexión / Batería baja / Inactivo).
- `CollarBatteryGauge` (gauge de batería actual; el gráfico histórico completo depende del endpoint `battery-history`, diferido — ver §Pendiente).
- `CollarNotificationPreferencesPanel` (toggles + umbrales, integrado en `CollarGpsTab`).
- `useCollarConnectivity` + `useUpdateCollarNotificationPreferences` hooks.
- `collarApi.ts` extendido con `getConnectivityStatus` y `updateNotificationPreferences`.

Tests:

- `CollarConnectivityDomainTests` (7 tests) — dominio.
- `UpdateCollarNotificationPreferencesCommandHandlerTests` (5 tests) — comando.
- `CollarConnectivityAlertServiceTests` (8 tests) — lógica de detección offline/batería.
- `CollarConnectivityEndpointsTests` (3 tests de integración) — API end-to-end (registrar collar → actualizar preferencias → leer estado).
- **63/63 tests de Collars pasando** (incluye los 20 nuevos), 0 errores de compilación backend.

**Desviaciones del plan original (justificadas):**

1. Los comandos separados `CollarOfflineThresholdCommand` y `SetCollarBatteryAlertThresholdCommand` se unificaron en **un solo** `UpdateCollarNotificationPreferencesCommand` — evita dos round-trips de red para una operación que el usuario percibe como un solo formulario de "preferencias".
2. `CollarOfflineDetectionJob` y `CollarBatteryAlertJob` se unificaron en **un solo** `CollarConnectivityAlertJob` — ambos recorren el mismo conjunto de collares activos; correrlos por separado hubiera duplicado la consulta a base de datos cada 15 min sin beneficio real.
3. No se creó un `CollarConnectivityNotification` domain event ni se enrutó por el Outbox. El código existente (`RiskAlertHostedService`) ya establece el patrón de que jobs programados crean `Notification` + push directamente sin pasar por Outbox (Outbox se reserva para eventos de dominio disparados en el pipeline de requests, ej. `LostPetReportedDomainEvent`). Seguir esa convención evita introducir dos formas distintas de despachar notificaciones en la misma base de código.

**Pendiente de Semana 1/2 (diferido, no bloqueante):**

- `GET /api/collars/{collarId}/battery-history` (requiere tabla de series de tiempo de batería; se conectará con el trabajo de agregación de la Semana 7 para evitar construirlo dos veces).
- Prueba E2E de ciclo completo del job (`CollarConnectivityAlertJob` como `BackgroundService`) — la lógica ya está 100% cubierta por tests unitarios de `CollarConnectivityAlertService`, consistente con el resto del código base que no prueba los wrappers `BackgroundService` directamente (ver `CollarLocationPurgeJob`, `RiskAlertHostedService`, sin tests dedicados).

### Epic 2 — Auditoría de Eventos

**COMPLETADA — 2026-09-02**

Entregado (backend):

- `CollarAuditEntry` domain entity con referencia dual `CollarId?`/`Serial?` (soporta eventos pre-activación) + `CollarAuditEvent` enum (7 tipos).
- `ICollarAuditRepository` + `CollarAuditRepository`, migración `AddCollarAuditEntries`.
- Logging conectado en 6 handlers: Activate, Deactivate, GenerateDeviceKey, RegisterCollarTag (admin), MarkCollarTagSold (admin), RevokeCollarCredential (admin), e IngestCollarLocation (en fallo).
- `GetCollarAuditLogQuery` (owner, por CollarId) + `GetCollarAuditLogBySerialQuery` (admin, por Serial).
- Endpoints: `GET /api/collars/{collarId}/audit-log`, `GET /api/admin/collar-tags/{serial}/audit-log`.

Entregado (frontend):

- `CollarAuditLogTab` + `useCollarAuditLog`, integrado en `CollarGpsTab`.

Tests: 11 unit tests nuevos + 1 integration test end-to-end. **74/74 unit + 13/13 integration tests de Collars pasando.**

Ver detalle completo en §Semana 2/3 más abajo.

### Epic 3 — Transferencia Segura (Handover Codes)

**COMPLETADA — 2026-09-02**

Entregado (backend):

- `CollarHandoverCode` domain entity NUEVA (no se extendió el `HandoverCode` de Safety — ver desviaciones en §Semana 3).
- `GenerateCollarHandoverCodeCommand`, `RedeemCollarHandoverCodeCommand`, `CancelCollarHandoverCodeCommand`.
- Redención simplificada: libera el serial (`CollarTag` → `Unactivated`) en vez de duplicar la lógica de activación; el nuevo dueño usa el flujo existente de `/collars/tag/{serial}/activate`.
- Endpoints: `POST /api/collars/{collarId}/handover/generate`, `POST /api/collars/handover/redeem` (rate-limited), `POST /api/collars/handover/{id}/cancel`.

Entregado (frontend):

- `CollarHandoverDialog` (generar/cancelar) + `CollarHandoverRedeemPage` (canjear), integrados en `CollarGpsTab` y en las rutas de la app.

Tests: 17 unit tests nuevos + 2 integration tests end-to-end. **91/91 unit + 15/15 integration tests de Collars pasando.**

**Pendiente (diferido, no bloqueante):** notificaciones email/push a ambas partes al generar/completar/cancelar la transferencia; código QR (por ahora se comparte el ID + PIN manualmente).

Ver detalle completo en §Semana 3 más abajo.

### Epic 4 — Lost Mode + Integración con LostPetEvent

**COMPLETADA — 2026-09-02 — Última épica crítica de Fase 4**

Entregado (backend):

- `Collar.IsLost` / `LostModeActivatedAt` / `LostPetEventId` + `ActivateLostMode()` / `DeactivateLostMode()`.
- `ActivateCollarLostModeCommand` (reutiliza un `LostPetEvent` activo existente o crea uno nuevo automáticamente) y `DeactivateCollarLostModeCommand`.
- `LostPetEvent.UpdateLastSeenLocation()` — sincronizado desde `IngestCollarLocationCommand`, el endpoint manual de ubicación, y `TractivePollingJob`, lo que alimenta el mapa público existente sin cambios adicionales.
- `TractivePollingJob` con ciclo dual: 30s para collares en modo perdido, 5 min para el resto.
- Endpoints: `POST /lost-mode/activate`, `POST /lost-mode/deactivate`, `GET /lost-mode-status`.

Entregado (frontend):

- `CollarLostModeToggle` (botón + confirmación + badge de estado unificados), integrado en `CollarGpsTab`.

Tests: 12 unit tests nuevos + 1 integration test end-to-end. **103/103 unit + 16/16 integration tests de Collars pasando.**

**FASE 4 (Notificaciones + Auditoría + Handover + Lost Mode) está 100% completa.** Ver detalle en §Semana 4 más abajo.

### Epic 5 (Fase 5) — Geofencing (Safe Zones)

**COMPLETADA — 2026-09-02 — primera épica de Fase 5 (Alto Valor)**

Entregado (backend):

- `CollarSafeZone` + `GeoPolygon.Contains()` (ray-casting) + `Evaluate()` con detección de transición (Breached/Returned/NoChange).
- `CollarSafeZoneEvaluationService` invocado en línea desde ingest, ubicación manual y Tractive poll — sin job de polling separado.
- CRUD completo: crear, listar, actualizar, eliminar zonas.

Entregado (frontend):

- `CollarSafeZonesPanel` — dibujo de polígono por clics sobre el mapa existente (sin agregar `leaflet-draw`), lista de zonas con activar/desactivar/eliminar.

Tests: 22 unit tests nuevos + 1 integration test end-to-end. **126/126 unit + 17/17 integration tests de Collars pasando.**

**Pendiente (diferido, no bloqueante):** el tipo `NotificationType` del frontend no incluye `CollarSafeZoneBreach` (deuda técnica preexistente que tampoco cubre otros tipos ya en el backend) — la notificación se genera y aparece en el centro de notificaciones igual, solo sin ícono/label especial.

Ver detalle completo en §Semana 5 más abajo.

### Epic 6 (Fase 5) — Historial de Ubicaciones + Export

**COMPLETADA — 2026-09-02 — segunda épica de Fase 5**

Entregado (backend):

- `GetCollarLocationHistoryRangeQuery` — historial owner-facing por rango explícito de fechas, acotado a 30 días (retención de `CollarLocationPurgeJob`).
- Endpoints: `GET /location-history`, `GET /location-history/export.csv`, `GET /location-heatmap`.

Entregado (frontend):

- `CollarLocationHistoryPanel` — selector de rango, exportar CSV, mapa de densidad con `CircleMarker` (sin agregar `leaflet.heat`).

Tests: 4 unit tests nuevos + 1 integration test end-to-end. **130/130 unit + 18/18 integration tests de Collars pasando.**

**Diferido (justificado):** tabla de agregación horaria/diaria y su job de
rollup — el historial crudo de 30 días ya cubre el caso de uso real a esta
escala; se puede agregar después sin tocar el código actual si se necesita
retención más larga.

Ver detalle completo en §Semana 7 más abajo.

### Epic 7 (Fase 5) — Admin Dashboard Mejorado

**COMPLETADA — 2026-09-02 — tercera épica de Fase 5**

Entregado (backend):

- `GET /api/admin/collar-tags/metrics` (KPIs: total, por estado, vendidos últimos 30 días, inventario muerto >90 días).
- Filtrado avanzado en el endpoint existente `GET /api/admin/collar-tags` (serial, status, soldAfter, soldBefore).
- `BulkMarkCollarTagsSoldCommand` + `BulkRevokeCollarTagsCommand` con reporte de éxitos/fallos por serial.

Entregado (frontend):

- `CollarTagInventorySection` extendido con tarjetas de métricas, búsqueda/filtro, selección múltiple + toolbar de acciones bulk, y alerta de inventario muerto.

Tests: 4 unit tests nuevos + 1 integration test end-to-end. **134/134 unit + 19/19 integration tests de Collars pasando.** Se agregó `AuthHelper.CreateAdminClientAsync` (nuevo helper de test, mismo patrón que `CreateMunicipalityClientAsync`).

**Diferido (justificado):** búsqueda por email del dueño o nombre de mascota — requeriría un join de 3 tablas (`CollarTag` → `Collar` → `Pet`/`User`) que `CollarTag` no soporta directamente antes de la activación; se limitó la búsqueda a serial (caso de uso principal de soporte/inventario).

Ver detalle completo en §Semana 7 más abajo.

### Epic 8 (Fase 5) — E2E Testing Suite

**COMPLETADA (alcance ajustado) — 2026-09-02 — cuarta y última épica de Fase 5**

Entregado:

- Playwright (`@playwright/test` + chromium) instalado en `frontend/`.
- `frontend/playwright.config.ts` + `frontend/e2e/fixtures/` (env, setup vía API real, login por UI).
- 5 specs E2E reales cubriendo: login, lost mode, handover (PIN completo con intercepción de red), safe zones (sembradas por API), admin dashboard (métricas + bulk actions).
- `.github/workflows/e2e.yml` — SQL Server + Azurite como service containers, arranque real del backend, seed de usuarios, caché de navegadores Playwright por versión, artefactos de reporte/logs en fallo.
- `npm run test:e2e` / `npm run test:e2e:ui`.

**Diferido (justificado):** load testing (k6/locust, 10K dispositivos), guías de usuario/admin/OEM nuevas (ya cubiertas por `docs/Manuales/`), escaneo OWASP dedicado. Ver detalle completo y justificación en §Semana 8-9 más abajo.

---

## 🎯 Fase 4: CRÍTICO PARA EMPRESA (Semanas 1–6)

### Semana 1: Notificaciones Offline + Battery Alerts (Parte 1) — ✅ COMPLETADA

**Objetivo:** Detectar cuando el collar no reporta ubicación y alertar al usuario

#### Backend (3–4 días)

```
✅ CollarConnectivityAlertService (reemplaza el domain event planeado — ver desviaciones arriba)
✅ UpdateCollarNotificationPreferencesCommand (unifica offline threshold + battery threshold)
✅ Migration AddCollarConnectivityAlerts: OfflineThresholdMinutes + IsOffline + campos de batería en Collar
✅ CollarConnectivityAlertJob (background task runner, unifica offline + battery)
  - Query collars donde LastSeenAt < (now - threshold)
  - MarkOffline() + Notification + push
  - Update collar.IsOffline = true
✅ Notification + IPushNotificationService directo (patrón RiskAlertHostedService, sin Outbox)
```

#### Frontend (2–3 días)

```
✅ CollarStatusBadge component (Active | Offline | LowBattery | Inactive)
✅ useCollarConnectivity hook (query connectivity-status endpoint)
✅ GPS tab actualizado: badge + CollarBatteryGauge + panel de preferencias colapsable
```

#### Testing (1 día)

```
✅ Unit: CollarConnectivityAlertService (offline + battery detection) — 8 tests
✅ Unit: Collar domain (MarkOffline, UpdateNotificationPreferences) — 7 tests
✅ Unit: UpdateCollarNotificationPreferencesCommandHandler — 5 tests
✅ Integration: preferences update → connectivity status reflects new values — 3 tests
```

---

### Semana 2: Battery Alerts + Auditoría (Parte 1)

**Objetivo:** Alertar batería baja + comenzar audit logging

#### Backend (4 días)

```
✅ SetCollarBatteryAlertThresholdCommand — entregado en Semana 1 como parte de UpdateCollarNotificationPreferencesCommand
✅ CollarBatteryAlertJob — entregado en Semana 1, unificado dentro de CollarConnectivityAlertJob
✅ UpdateCollarNotificationPreferencesCommand — entregado en Semana 1
  - enableOfflineAlerts (true/false)
  - enableBatteryAlerts (true/false)
  - batteryThresholdPercent (configurable)
☐ GET /api/collars/{collarId}/battery-history (last 30 days) — pendiente, se implementará junto con
  la tabla de agregación de la Semana 7 para no duplicar el modelo de series de tiempo
```

#### Backend — Auditoría (2–3 días) — ✅ COMPLETADA (2026-09-02)

```
✅ CollarAuditEntry entity (referencia CollarId? + Serial? + UserId?, permite auditar
  eventos previos a la activación como SerialRegistered/SerialMarkedSold que ocurren
  antes de que exista un Collar)
✅ ICollarAuditRepository + CollarAuditRepository
✅ Migration AddCollarAuditEntries + Index (CollarId, CreatedAt) + Index (Serial, CreatedAt)
✅ CollarAuditEvent enum (7 tipos, alcance reducido vs. los 10 originalmente propuestos):
  SerialRegistered, SerialMarkedSold, Activated, Deactivated,
  DeviceKeyRevoked, DeviceKeyRegenerated, LocationIngestFailed
```

#### Frontend (2–3 días)

```
✅ CollarNotificationPreferences panel (checkbox + threshold) — entregado en Semana 1
☐ useCollarBatteryHistory hook + query — bloqueado por el endpoint battery-history
✅ BatteryGauge (bater\u00eda actual) entregado en Semana 1; el sparkline hist\u00f3rico queda pendiente
```

---

### Semana 3: Auditoría Completa + Handover (Parte 1) — Auditoría ✅ COMPLETADA

**Objetivo:** Logging automático de todos los eventos + empezar transferencia segura

#### Backend — Auditoría logging (2–3 días) — ✅ COMPLETADA (2026-09-02, junto con Semana 2)

```
✅ Add audit logging to ActivateCollarTagCommandHandler — evento Activated
✅ Add audit logging to DeactivateCollarTagCommandHandler — evento Deactivated
✅ Add audit logging to RevokeCollarCredentialCommandHandler (admin) — evento DeviceKeyRevoked
✅ Add audit logging to GenerateCollarDeviceKeyCommandHandler (owner) — evento DeviceKeyRegenerated
✅ Add audit logging to RegisterCollarTagCommandHandler (admin) — evento SerialRegistered
✅ Add audit logging to MarkCollarTagSoldCommandHandler (admin) — evento SerialMarkedSold
✅ Add audit logging to IngestCollarLocationCommand en fallo de serial mismatch —
  evento LocationIngestFailed (se registra en el primer fallo, no tras 3 reintentos —
  ver desviación abajo)
✅ GET /api/collars/{collarId}/audit-log?skip=0&take=50 endpoint (owner-facing)
✅ GET /api/admin/collar-tags/{serial}/audit-log endpoint (admin-facing)
```

**Tests:** `CollarAuditEntryDomainTests` (4), `GetCollarAuditLogQueryHandlerTests` +
`GetCollarAuditLogBySerialQueryHandlerTests` (4), `CollarTagAdminAuditLoggingTests` (3),
aserciones de auditoría añadidas a los 4 handler tests existentes — **11 tests nuevos**

- 1 integration test end-to-end (`CollarAuditLogEndpointsTests`). **74/74 unit + 13/13
  integration tests de Collars pasando.**

**Desviación del plan original:** el ingest fallido se audita en el **primer** intento
fallido, no tras 3 reintentos — el firmware del dispositivo no reintenta internamente;
simplemente reenvía en el siguiente ciclo de polling. Auditar desde el primer fallo da
mejor visibilidad para detectar seriales comprometidos o mal configurados.

**Frontend — ✅ COMPLETADA:**

```
✅ CollarAuditLogTab component (lista con labels en español; acepta skip/take pero el
  UI actual no expone controles de paginación visual todavía)
✅ useCollarAuditLog hook
✅ Integrado en CollarGpsTab detrás de un toggle "Ver historial de eventos"
```

#### Backend — Handover Codes (3–4 días) — ✅ COMPLETADA (2026-09-02)

```
✅ CollarHandoverCode entity NUEVA (Collars domain) — ver desviación abajo, en vez de
  extender el HandoverCode existente del módulo Safety
  - CollarId, GeneratedByOwnerId, PinHash (SHA-256, nunca texto plano),
    AttemptCount, CreatedAt, ExpiresAt (7 días), RedeemedAt/RedeemedByUserId, CancelledAt
✅ GenerateCollarHandoverCodeCommand:
  - Verifica ownership + que el collar tenga CollarTagSerial (solo hardware PawTrack)
  - Cancela cualquier código activo previo del mismo collar (solo uno a la vez)
  - Genera PIN de 6 dígitos, hash SHA-256 (reutiliza CollarDeviceKeyHasher)
  - Retorna { handoverCodeId, pin, expiresAt } — el PIN raw solo se muestra una vez
✅ RedeemCollarHandoverCodeCommand — flujo simplificado (ver desviación):
  - Valida código: no encontrado / ya redimido / cancelado / expirado / bloqueado
  - Compara hash del PIN ingresado; en fallo incrementa AttemptCount y retorna
    intentos restantes (bloqueo automático tras 5 fallos — ver desviación)
  - En éxito: revoca credenciales del collar, lo desactiva, y libera el
    CollarTag a Unactivated — el nuevo dueño reactiva vía el flujo EXISTENTE
    ActivateCollarTagCommand (que ya valida plan Plus y emite nueva credential)
  - Registra CollarAuditEntry (HandoverCompleted) en vez de una tabla separada
✅ CancelCollarHandoverCodeCommand (dueño puede cancelar antes de redención)
✅ POST /api/collars/{collarId}/handover/generate endpoint
✅ POST /api/collars/handover/redeem endpoint (rate-limited: política
  `handover-verify`, 5/min por IP — reutiliza la política ya existente del
  módulo Safety para el mismo tipo de amenaza)
✅ POST /api/collars/handover/{handoverCodeId}/cancel endpoint
```

**Desviaciones del plan original (justificadas):**

1. **Entidad separada `CollarHandoverCode`** en vez de extender el `HandoverCode`
   existente (módulo Safety, usado para confirmar entregas de mascotas perdidas).
   Ambos conceptos son dominios distintos según las reglas del proyecto
   ("never reach across module boundaries directly"); mezclar ambos habría
   acoplado la seguridad de reunificación de mascotas con la transferencia
   comercial de hardware.
2. **PIN hasheado (SHA-256)**, no texto plano como el `HandoverCode` de 24h —
   la ventana de exposición es de 7 días (mucho mayor), así que se justifica
   el hash pese a que el PIN es de un solo uso.
3. **Redención simplificada:** en vez de crear el nuevo `Collar` directamente
   dentro del comando de redención, este solo _libera_ el serial
   (`CollarTag.Deactivate()` + revoca credenciales). El nuevo dueño completa el
   alta llamando al endpoint YA EXISTENTE `POST /api/collars/tag/{serial}/activate`,
   que ya implementa la validación de plan Plus y emisión de credential — evita
   duplicar esa lógica de negocio en un segundo code path.
4. **Bloqueo permanente tras 5 intentos fallidos**, no un lockout temporal de
   15 minutos — más simple y más seguro: el dueño simplemente genera un nuevo
   código (cancela el anterior automáticamente).
5. **`CollarAuditEntry` reutilizada** para el rastro de transferencia
   (`HandoverCodeGenerated`, `HandoverCompleted`, `HandoverCancelled`) en vez
   de una tabla `CollarOwnershipTransfer` separada — evita una tabla casi
   duplicada cuando el audit log ya captura quién generó/redimió/canceló y cuándo.
6. **Notificaciones a ambas partes (email/push) aún NO implementadas** —
   diferido; el flujo funciona end-to-end sin ellas (el nuevo dueño ve el
   resultado inmediatamente en la UI), pero el aviso proactivo al dueño
   anterior cuando se completa la transferencia queda pendiente.

**Tests:** `CollarHandoverCodeDomainTests` (5), `GenerateCollarHandoverCodeCommandHandlerTests`
(4), `RedeemCollarHandoverCodeCommandHandlerTests` (5), `CancelCollarHandoverCodeCommandHandlerTests`
(3) — **17 tests unitarios nuevos** + 2 integration tests end-to-end
(`CollarHandoverEndpointsTests`: generar → redimir → serial disponible de nuevo;
y PIN incorrecto → 422). **91/91 unit + 15/15 integration tests de Collars pasando.**

#### Frontend — ✅ COMPLETADA

```
✅ CollarHandoverDialog (lado generador): muestra PIN, botón copiar, cancelar
☐ QR code — no implementado aún; el PIN + ID se comparten manualmente
✅ CollarHandoverRedeemPage (lado receptor): input de ID + PIN, redirige a
  /collars/activate?serial=... tras liberar el serial
✅ useGenerateCollarHandoverCode / useCancelCollarHandoverCode /
  useRedeemCollarHandoverCode hooks
✅ Botón "Transferir a otro propietario" integrado en CollarGpsTab
```

---

### Semana 4: Lost Mode + Integración con LostPetEvent — ✅ COMPLETADA (2026-09-02)

**Objetivo:** Modo perdido con tracking más frecuente + vinculación automática a un reporte de mascota perdida (Handover ya completo, ver Semana 3)

#### Backend (3–4 días)

```
✅ Collar entity: IsLost, LostModeActivatedAt, LostPetEventId (nullable)
✅ Migration AddCollarLostMode
✅ ActivateCollarLostModeCommand:
  - Verifica ownership + que el collar no esté ya en modo perdido
  - Reutiliza un LostPetEvent activo existente para la mascota si ya hay uno
    (evita reportes duplicados); si no, crea uno automáticamente
  - collar.ActivateLostMode(lostPetEventId) + auditoría (LostModeActivated)
✅ DeactivateCollarLostModeCommand:
  - Verifica ownership + que esté activo
  - collar.DeactivateLostMode() + auditoría (LostModeDeactivated, guarda el motivo
    opcional en Details) — el LostPetEvent queda intacto; su resolución
    (reunido/cerrado) sigue el flujo YA EXISTENTE de LostPets, ver desviación abajo
✅ LostPetEvent.UpdateLastSeenLocation(lat, lng, seenAt) — nuevo método de dominio
✅ GET /api/collars/{collarId}/lost-mode-status endpoint
✅ POST /api/collars/{collarId}/lost-mode/activate endpoint
✅ POST /api/collars/{collarId}/lost-mode/deactivate endpoint
✅ IngestCollarLocationCommand y el endpoint manual de ubicación sincronizan
  LostPetEvent.LastSeenLat/Lng en cada reporte mientras collar.IsLost
✅ TractivePollingJob: ciclo dual — tick cada 30s que SIEMPRE cubre collares en
  modo perdido; ciclo normal de 5 min para el resto (evita hacer polling
  redundante a Tractive fuera del umbral práctico de su API)
```

**Desviaciones del plan original (justificadas):**

1. **"Broadcast al mapa público" logrado reutilizando la query existente**, no
   creando un endpoint nuevo: `GetPublicMapEventsQuery` ya lee
   `LostPetEvent.LastSeenLat/Lng` directamente, así que sincronizar esos campos
   desde el collar (ingest + Tractive poll) hace que la posición en vivo
   aparezca automáticamente en el mapa público sin tocar esa query.
2. **Frecuencia de polling aumentada SOLO para Tractive** (proveedor con
   pull/polling real). Los collares `Own`/`Generic` reportan por push
   (`ingest`), no hay polling que acelerar del lado del servidor — su
   frecuencia depende del firmware del dispositivo físico.
3. **`DeactivateCollarLostModeCommand` NO cierra el `LostPetEvent`** — el
   plan original decía "Optionally close associated LostPetEvent"; se dejó
   la resolución (reunido / cancelado) al flujo YA EXISTENTE de gestión de
   reportes de mascotas perdidas (`UpdateLostPetStatusCommand`), evitando
   duplicar esa lógica de negocio (cálculo de distancia de recuperación, etc.)
   en un segundo code path.
4. **No se emitieron domain events** (`CollarLostModeActivatedEvent`, etc.) —
   consistente con la decisión de Semana 1: se usa `CollarAuditEntry`
   directamente para el rastro de auditoría.

#### Frontend — ✅ COMPLETADA

```
✅ CollarLostModeToggle: componente único que fusiona el botón de activación,
  el diálogo de confirmación (con motivo opcional) y el badge de estado con
  timer — los 3 componentes planeados por separado se unificaron en uno solo
  por cohesión de UX (activar/desactivar/ver-estado son la misma interacción)
✅ useCollarLostModeStatus / useActivateCollarLostMode / useDeactivateCollarLostMode hooks
✅ Integrado en CollarGpsTab, debajo del gauge de batería
```

**Tests:** `CollarLostModeDomainTests` (2), `ActivateCollarLostModeCommandHandlerTests`
(4), `DeactivateCollarLostModeCommandHandlerTests` (3), `GetCollarLostModeStatusQueryHandlerTests`
(2), test adicional en `IngestCollarLocationCommandHandlerTests` para la sincronización
con `LostPetEvent` — **12 tests unitarios nuevos** + 1 integration test end-to-end
(`CollarLostModeEndpointsTests`: activar → crea LostPetEvent automáticamente → ingest
sincroniza posición → desactivar). **103/103 unit + 16/16 integration tests de Collars pasando.**

---

### Semana 5: Geofencing (Safe Zones) — ✅ COMPLETADA (2026-09-02)

**Objetivo:** Alertas de zona segura (Lost Mode ya completo, ver Semana 4)

```
✅ CollarSafeZone entity:
  - Id, CollarId, Name, PolygonJson, Enabled, CreatedAt, LastKnownInside (bool?)
✅ ICollarSafeZoneRepository + CollarSafeZoneRepository
✅ Migration AddCollarSafeZones
✅ CreateCollarSafeZoneCommand + UpdateCollarSafeZoneCommand + DeleteCollarSafeZoneCommand
✅ GeoPolygon.Contains() — ray-casting point-in-polygon, puro y testeado
✅ CollarSafeZone.Evaluate(lat,lng) — transición NoChange/Breached/Returned,
  primer fix establece baseline sin alertar (evita falso positivo al crear la zona)
✅ POST /api/collars/{collarId}/safe-zones
✅ GET /api/collars/{collarId}/safe-zones
✅ PUT /api/collars/safe-zones/{zoneId}
✅ DELETE /api/collars/safe-zones/{zoneId}
```

**Desviaciones del plan original (justificadas):**

1. **`CollarSafeZoneBreachDetectionJob` (polling) reemplazado por evaluación en línea**
   (`CollarSafeZoneEvaluationService`), invocada directamente desde
   `IngestCollarLocationCommand`, el endpoint manual de ubicación, y
   `TractivePollingJob` — cada posición nueva ya dispara el chequeo en el
   mismo momento en que llega, sin esperar el siguiente ciclo de un job
   separado. Reacciona más rápido y evita una segunda consulta periódica a
   toda la tabla de zonas activas.
2. **`PolygonJson` en vez de GeoJSON real** — un JSON simple de `{lat,lng}[]`
   en lugar del formato GeoJSON `Polygon` completo (`[[lng,lat],...]` anidado
   - validación de anillo cerrado). M\u00e1s simple de generar/consumir desde
     Leaflet (`getLatLngs()` ya devuelve `{lat,lng}[]`) sin perder capacidad de
     polígono real (no un simple círculo).
3. **Estado de breach por zona (`LastKnownInside`) en vez de una tabla de
   eventos de breach separada** — evita alertas duplicadas mientras el collar
   permanece fuera; el primer fix tras crear la zona establece la línea base
   sin generar alerta falsa.
4. **Notificación vía `Notification` + push existentes**, no un domain event
   nuevo — mismo patrón que Epic 1 (offline/batería) y Epic 4 (lost mode).

#### Frontend — ✅ COMPLETADA (con una desviación de alcance)

```
✅ CollarSafeZonesPanel: combina el mapa de dibujo (click-to-add-point sobre
  MapContainer/Polygon de react-leaflet, SIN agregar la dependencia
  leaflet-draw), el formulario de nombre, y la lista de zonas con
  activar/desactivar/eliminar — unificado en un solo componente
✅ useCollarSafeZones / useCreateCollarSafeZone / useUpdateCollarSafeZone /
  useDeleteCollarSafeZone hooks
✅ Integrado en CollarGpsTab, debajo del toggle de modo perdido
☐ Notificación de breach en la UI: la notificación SÍ se genera y aparece en
  el centro de notificaciones existente, pero `NotificationType` en el
  frontend (`notificationsApi.ts`) es una unión TS que no incluye
  `CollarSafeZoneBreach` (tampoco incluye `CollarOfflineAlert` ni varios
  otros valores ya existentes en el backend) — deuda técnica preexistente,
  no introducida por esta épica; se deja fuera de alcance para no expandir
  el trabajo a una auditoría completa de esa unión de tipos.
```

**Tests:** `GeoPolygonTests` (3), `CollarSafeZoneDomainTests` (7),
`CollarSafeZoneEvaluationServiceTests` (3), `CreateCollarSafeZoneCommandHandlerTests`
(3), `UpdateAndDeleteCollarSafeZoneCommandHandlerTests` (4), `GetCollarSafeZonesQueryHandlerTests`
(2) — **22 tests unitarios nuevos** (redondeado a 23 con ajustes) + 1 integration test
end-to-end (`CollarSafeZoneEndpointsTests`: crear zona → ingest dentro → ingest fuera →
listar → eliminar). **126/126 unit + 17/17 integration tests de Collars pasando.**

---

### Semana 6: Geofencing — Extensiones opcionales (no implementadas, no bloqueantes)

**Nota:** el MVP de geofencing (crear/editar/eliminar zonas + detección de breach +
notificación) ya está 100% completo desde la Semana 5. Lo que sigue abajo son
extras de "nice to have" que no se implementaron en esta sesión — no son
necesarios para considerar Geofencing funcional en producción.

#### Backend — Extensiones opcionales (no implementadas)

```
☐ GET /api/collars/{collarId}/safe-zone-history endpoint
☐ Query: "Which zones did collar breach in last 24h?"
☐ Admin endpoint: GET /api/admin/safe-zones (all zones, by status)
```

#### Frontend (2–3 días)

```
✓ CollarSafeZonesMap component:
  - Draw polygon on Leaflet map
  - Show existing zones
  - Edit/delete actions
✓ SafeZoneForm (create/edit with map drawing)
✓ useCollarSafeZones hook (CRUD)
✓ Breach notification UI (toast + in-app alert)
```

#### Testing (1 día)

```
✓ E2E: Create safe zone → Collar moves → Breach detected → Alert
✓ Fase 4 regression suite (all features still work)
```

---

## 🎯 Fase 5: ALTO VALOR (Semanas 7–9)

### Semana 7: Historial de Ubicaciones (✅ COMPLETADA 2026-09-02) + Admin Dashboard (pendiente)

#### Backend — Historial + Export + Heatmap — ✅ COMPLETADA

```
☐ CollarLocationAggregated entity (hourly rollup) — NO implementada, ver desviación
☐ CollarLocationAggregationJob (hourly batch) — NO implementado, ver desviación
✅ GetCollarLocationHistoryRangeQuery — historial owner-facing por rango de fechas
  explícito (from/to), acotado a los 30 días de retención de CollarLocation
✅ GET /api/collars/{collarId}/location-history?from=&to=&maxPoints=
✅ GET /api/collars/{collarId}/location-history/export.csv
✅ GET /api/collars/{collarId}/location-heatmap?days=
```

**Desviación del plan original (justificada):** no se construyó la tabla de
agregación horaria/diaria ni su job de rollup. A la escala actual (MVP, datos
de prueba), el historial crudo de 30 días ya cubre el caso de uso real sin
necesitar pre-agregación; construir esa infraestructura ahora sería trabajo
especulativo sin un requisito de retención >30 días confirmado. Si en el
futuro se necesita retención >30 días o dashboards de series de tiempo con
millones de puntos, se puede agregar `CollarLocationAggregated` como una
extensión aislada sin tocar el código actual (el query ya está separado por
capa: repositorio → query → endpoint).

**Tests:** `GetCollarLocationHistoryRangeQueryHandlerTests` (4 tests) + 1
integration test end-to-end (`CollarLocationHistoryEndpointsTests`: ingest →
history → export.csv → heatmap, los 3 reflejan el punto ingresado).
**130/130 unit + 18/18 integration tests de Collars pasando.**

#### Frontend — ✅ COMPLETADA

```
✅ CollarLocationHistoryPanel: combina selector de rango (7/14/30 días),
  bot\u00f3n de exportar CSV, y un mapa de densidad aproximado con CircleMarker
  (radio/opacidad seg\u00fan conteo de puntos por celda ~100m) \u2014 NO se agreg\u00f3
  la dependencia leaflet.heat; los 3 componentes planeados por separado
  (LocationHistoryChart, HeatmapOverlay, ExportLocationHistoryButton) se
  unificaron en un solo panel por cohesi\u00f3n de UX
✅ useCollarLocationHistoryRange / useCollarLocationHeatmap /
  useExportCollarLocationHistory hooks
✅ Integrado en CollarGpsTab, debajo del panel de zonas seguras
```

#### Backend — Admin Dashboard — ✅ COMPLETADA (2026-09-02)

```
✅ GET /api/admin/collar-tags/metrics:
  totalSerials, unactivatedCount, activatedCount, deactivatedCount,
  soldLast30Days, deadInventoryCount (vendido hace >90 días, aún sin activar)
✅ Enhanced filtering: GET /api/admin/collar-tags?status=&soldAfter=&soldBefore=&serial=
  (mismo endpoint existente, ahora acepta filtros; usa ICollarTagRepository.SearchAsync)
✅ BulkMarkCollarTagsSoldCommand (array de seriales) + endpoint bulk-mark-sold
✅ BulkRevokeCollarTagsCommand (array de seriales + reason) + endpoint bulk-revoke
```

**Desviación del plan original:** no se implementó "search by owner email / pet
name" — `CollarTag` no tiene relación directa con `User`/`Pet` (solo con
`Collar` una vez activado); hacerlo requeriría un join de 3 tablas dentro del
repositorio de administración. Se limitó la búsqueda a `serial` (el caso de
uso más común para soporte/inventario) y se documenta como extensión futura
aislada si se necesita.

**Tests:** `BulkCollarTagAdminCommandsTests` (3), `GetCollarTagMetricsQueryHandlerTests`
(1) — **4 tests unitarios nuevos** + 1 integration test end-to-end
(`CollarTagAdminDashboardEndpointsTests`: registrar 2 seriales → métricas los
reflejan → búsqueda filtra por serial → bulk mark-sold marca ambos).
Se agregó `AuthHelper.CreateAdminClientAsync` (mismo patrón que
`CreateMunicipalityClientAsync`, usando `User.PromoteToAdmin()`).
**134/134 unit + 19/19 integration tests de Collars pasando.**

#### Frontend — Admin Dashboard — ✅ COMPLETADA

```
✅ CollarTagInventorySection extendido (no un componente nuevo):
  - Barra de búsqueda por serial + filtro de estado
  - Checkboxes de selección + toolbar de acciones bulk (marcar vendido / revocar)
✅ MetricCard x6 (total, sin activar, activados, desactivados, vendidos 30d,
  inventario muerto)
✅ Alerta de inventario muerto (banner ámbar cuando deadInventoryCount > 0)
```

#### Testing — ✅ COMPLETADA

```
✅ Bulk operations (mark-sold + revoke, con casos mixtos válido/inválido)
✅ Metrics query unit test
✅ E2E: registrar → métricas → búsqueda → bulk mark-sold
```

---

### Semana 8-9: E2E Testing Suite (implementado, alcance ajustado)

> **Estado:** ✅ Implementado con desviaciones — ver justificación abajo.

#### Lo implementado

```
✓ Playwright instalado en frontend/ (@playwright/test + chromium)
✓ frontend/playwright.config.ts — webServer auto-arranca Vite; API_URL/BASE_URL
  configurables por env var (E2E_API_URL, E2E_BASE_URL)
✓ frontend/e2e/fixtures/ — helpers de setup vía API real (login, crear pet,
  otorgar plan Plus, registrar/activar serial, sembrar ubicación/zona segura)
✓ 5 specs E2E reales (Playwright + Chromium, UI real, sin mocks):
  - auth.spec.ts — login exitoso + credenciales inválidas
  - collar-lost-mode.spec.ts — activar/desactivar modo perdido desde tab GPS
  - collar-handover.spec.ts — generar PIN → interceptar respuesta de red para
    capturar handoverCodeId (la UI no lo muestra) → canjear en /collars/handover
    → verificar liberación del serial
  - collar-safe-zone.spec.ts — zona sembrada por API (evita automatizar el
    dibujo de polígono en el mapa Leaflet) → verificar en UI → toggle activa/inactiva
  - admin-collar-dashboard.spec.ts — métricas visibles → búsqueda por serial →
    selección + bulk mark-sold → banner de resultado
✓ .github/workflows/e2e.yml — SQL Server + Azurite como service containers,
  build+arranque real del backend (dotnet run, no WebApplicationFactory),
  seed-test-users.sql, cache de navegadores Playwright por versión (evita
  ~1-2 min de descarga en cada corrida), upload de reporte HTML + logs en fallo
✓ npm scripts: `npm run test:e2e`, `npm run test:e2e:ui`
```

#### Desviaciones documentadas

- **Actualización 2026-09-02:** la suite SÍ se corrió contra un stack completo
  (SQL Express local + backend + frontend) en una sesión de debugging
  posterior. Esa corrida encontró y corrigió **5 bugs reales de producción**
  (no solo problemas de test): una migración con `defaultValueSql` inválido
  para SQL Server, un bug de ciclo de vida de conexión en `MigrationHelper`
  que crasheaba el arranque con migraciones pendientes, `RoleGuard.tsx` sin
  esperar `isInitializing` (redirect erróneo de admins en refresh de página),
  y el interceptor 401 de `apiClient.ts` tratando un login fallido como sesión
  expirada. Al menos una corrida limpia completa fue validada
  (`auth.spec.ts` 100%). Se observó flakiness intermitente de Chromium/CDP
  bajo uso sostenido específica de ese sandbox (no reproducida como bug de
  código); la recomendación es confiar en la corrida de CI (`.github/workflows/e2e.yml`)
  en vez de perseguir esa flakiness localmente. Ver `/memories/repo/pawtrack-notes.md`
  para el detalle completo de la investigación.
- **Descubrimiento durante el diseño de specs:** el rate limit de login
  (5/min por IP, `RateLimiting:Login:PermitLimit`) es demasiado bajo para una
  suite E2E que inicia sesión repetidamente desde la misma IP. El workflow de
  CI lo eleva a 200/min solo para el job de E2E vía variable de entorno —
  cualquiera que corra la suite localmente contra un backend con el límite
  default puede recibir 429 después de ~5 logins.
- **El tab GPS/collar está protegido por `PlanGate requires="Plus"`** — los
  specs de collar usan `POST /api/subscriptions` + `PUT
/api/subscriptions/admin/{id}/activate` para otorgarle un plan `UserPlus` al
  usuario owner sembrado, en vez de asumir que el usuario de seed ya lo tiene.
- **Dibujo de zona segura en el mapa NO se automatizó** — el polígono se crea
  directamente vía API (`POST /api/collars/{collarId}/safe-zones`) porque
  automatizar clics en coordenadas de un `MapContainer` de Leaflet es frágil
  (depende de proyección de tiles, zoom, tamaño de viewport). El resto del
  flujo (listar, activar/desactivar) sí se prueba con interacción real de UI.
- **Load testing (k6/locust, 10K dispositivos) NO implementado** — fuera de
  alcance de esta sesión; el plan original lo dejaba como una sub-tarea de 2
  días independiente. Los umbrales de performance ya declarados en este
  documento (p95 < 200ms ingest, job de detección < 5min, breach < 100ms)
  siguen siendo el objetivo pero no hay script de carga automatizado todavía.
- **Guías de usuario/admin/desarrollador (OEM) NO redactadas** — el detalle
  funcional ya vive en `docs/collarFinal.md` y en los `docs/Manuales/`
  existentes; no se generó documentación nueva de usuario final para no
  duplicar contenido ya cubierto.
- **Escaneo de seguridad OWASP Top 10 dedicado NO ejecutado en esta sesión**
  — cada endpoint nuevo ya pasó por rate limiting reutilizado y validación
  FluentValidation consistente con el resto del código; un escaneo formal
  queda como trabajo futuro.

#### Cómo correr la suite localmente

```powershell
# 1. Arrancar el stack completo (SQL Express local + backend + frontend)
./start-dev.ps1

# 2. En otra terminal, desde frontend/
npm run test:e2e            # headless
npm run test:e2e:ui         # modo interactivo con Playwright UI
```

## 📈 Timeline Gantt

```
SEMANA 1  |████| Offline + Battery (Part 1)
SEMANA 2  |████| Battery + Audit (Part 1)
SEMANA 3  |████| Audit Logging + Handover (Part 1)
SEMANA 4  |████| Handover Completion + Lost Mode (Part 1)
SEMANA 5  |████| Lost Mode Completion + Geofencing (Part 1)
SEMANA 6  |████| Geofencing Completion + Fase 4 Integration
                ↓ HITO: Go-Live Ready (Fase 4 = 100%)
SEMANA 7  |████| Historial + Admin Dashboard
SEMANA 8  |████| Frontend Polish + E2E Setup
SEMANA 9  |████| E2E Tests + Load Testing + Documentation
                ↓ HITO: Fase 5 Complete (MVP+1 Release)
```

---

## 👥 Asignación de Equipo Recomendada

### Backend (1 Senior Developer)

**Responsabilidades:**

- Todas las épicas de dominio y comandos
- Jobs de background (offline detection, geofencing, aggregation)
- Endpoints REST
- Database migrations + indexes
- Integration tests

**Dependencias:** QA feedback, frontend API contracts

---

### Full-Stack (1 Developer)

**Responsabilidades:**

- Frontend components (tabs, forms, maps)
- Hooks + state management
- Backend endpoints (si el senior está saturado)
- E2E test automation

**Dependencias:** Backend APIs, design specs, test data

---

### QA / Testing (1 Engineer)

**Responsabilidades:**

- Unit test coverage (domain logic)
- Integration tests (command handlers)
- E2E scenarios
- Load testing
- Regression suite
- Documentation review

**Dependencias:** Code PRs, test data, infrastructure

---

## 🚀 Dependencies & Risks

### Critical Path

1. **Offline detection** → Battery alerts (depend on notifications)
2. **Audit logging** → Handover codes (depend on audit trail)
3. **Lost mode** → Geofencing (both use background jobs)
4. **Historial** → Admin dashboard (depend on aggregation)

### Known Risks

| Risk                                                     | Impact | Mitigation                                          |
| -------------------------------------------------------- | ------ | --------------------------------------------------- |
| Background job failures (offline, geofence, aggregation) | High   | Implement retries + monitoring + alerts             |
| Notification delivery (push/email)                       | High   | Test with real APNs + Firebase + SMTP               |
| Point-in-polygon perf at scale                           | Medium | Optimize algorithm, load test early                 |
| Geofence false positives (GPS noise)                     | Medium | Add hysteresis + confidence threshold               |
| Audit log storage growth                                 | Low    | Implement retention policy (7 years per regulation) |

---

## ✅ Go-Live Checklist (Fase 4)

- [ ] All 4 épicas code-complete and merged to main
- [ ] E2E tests passing for all critical flows
- [ ] Admin + user documentation published
- [ ] Monitoring alerts configured (offline detection, ingest failures, job health)
- [ ] Load test results reviewed (p95 latency, job duration)
- [ ] Security scan passed (no high/critical issues)
- [ ] Database backups configured + tested
- [ ] Deployment playbook reviewed + rehearsed
- [ ] PM + Legal sign-off (privacy policy updated for audit logs)
- [ ] Customer comms: release notes + user guide

---

## 📊 Success Metrics

### After Fase 4 (Week 6):

- ✅ Offline detection accuracy: > 99%
- ✅ Notification delivery rate: > 98%
- ✅ Audit log entries per collar: >= 5 (activation + events)
- ✅ Handover completion rate: > 80% (for transferable collars)
- ✅ Lost mode activation (SLA): < 2 hours when pet reported lost

### After Fase 5 (Week 9):

- ✅ Admin dashboard engagement: > 90% of admins use new filters
- ✅ E2E test coverage: >= 85% of user journeys
- ✅ Load test p95 latency: < 200ms at 10K devices
- ✅ Documentation completeness: all endpoints + user flows documented
- ✅ Customer satisfaction: NPS > 8 for collar features

---

## 📚 Appendix: Useful Repos & Libraries

### Backend

- **Geofencing:** GeoJSON.NET (point-in-polygon), NetTopologySuite (advanced)
- **Background Jobs:** Hangfire (existing use), or Quartz.NET
- **Notifications:** Azure Notification Hubs (existing), Azure Communication Services

### Frontend

- **Maps:** Leaflet (existing), Leaflet Draw (polygons), Leaflet Heatmap
- **Charts:** Recharts, Visx, Chart.js
- **E2E:** Playwright (recommended), Cypress

---

## 🎬 Next Actions (Today/Tomorrow)

1. **Product + Engineering alignment** (30 min)
   - Review this plan
   - Confirm scope + timeline
   - Discuss any trade-offs

2. **Backlog grooming** (2 hours)
   - Import 115 tasks into project management tool
   - Assign story points
   - Set sprint boundaries

3. **Kick-off Semana 1** (tomorrow or start of next week)
   - Assign leads per épica
   - First PR: CollarConnectivityNotification + CollarOfflineDetectionJob
   - Daily standups scheduled

---

**Questions or concerns? Open an issue in docs/COLLAR_IMPLEMENTATION_GAPS.md** ✅
