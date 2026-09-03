# PawTrack CR — Collar GPS: Guía Completa

> **Única fuente de verdad** sobre hardware de collar, proveedores, integración de código, CollarTag (activación tipo AirTag) y sourcing.  
> Consolida: `collar.md`, `collar-china-sourcing.md`, `collarTag.md`.  
> Última actualización: 2026-09-03 — CollarTag **implementado** (fases 1–3); Fase 4 Enterprise **COMPLETA**: Alertas de conectividad, Auditoría de eventos, Transferencia segura (Handover) y Lost Mode **implementados**. Fase 5 **COMPLETA** (4/4): Geofencing (Safe Zones), Historial de ubicaciones + export, Admin Dashboard mejorado, y E2E Testing Suite (Playwright) **implementados**. Conversación activa con **Jimi IoT** (RFQ enviado, respuesta recibida 2026-09-03) — ver `docs/jimiiot.md`.

---

## 1. Estado actual del código

| Capa                                            | Archivo / componente                                                                                                                                                           | Estado                                                                                |
| ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------- |
| Dominio                                         | `Collar.cs`, `CollarLocation.cs`, `CollarProvider.cs`, `CollarTag.cs`, `CollarDeviceCredential.cs`                                                                             | ✅ Completo                                                                           |
| Repositorio                                     | `ICollarRepository`, `CollarRepository.cs`, `ICollarTagRepository`, `CollarTagRepository.cs`, `ICollarDeviceCredentialRepository`                                              | ✅ Completo                                                                           |
| Comandos / Queries                              | `RegisterCollarCommand`, `GetCollarStatusQuery`, `GetLocationHistoryQuery`, `ActivateCollarTagCommand`, `DeactivateCollarTagCommand`, `CheckCollarSerialQuery`, admin commands | ✅ Completo                                                                           |
| Seguridad / auth-device                         | `CollarDeviceKeyMiddleware`, ownership checks, 401/403 enforcement                                                                                                             | ✅ Completo                                                                           |
| Integración Tractive                            | `TractiveService.cs`, OAuth2, callback y sincronización                                                                                                                        | ✅ Completo                                                                           |
| Polling / limpieza                              | `TractivePollingJob.cs` (ciclo dual 30s/5min), `CollarLocationPurgeJob.cs` (>30 días)                                                                                          | ✅ Completo                                                                           |
| API REST                                        | `CollarsController`, `CollarTagsController`, `CollarTagAdminController`                                                                                                        | ✅ Completo                                                                           |
| Rate limiting                                   | `public-api` + `collar-serial-check` + `handover-verify` en endpoints sensibles                                                                                                | ✅ Completo                                                                           |
| Frontend GPS                                    | `CollarGpsTab.tsx`, `useCollar.ts`, `collarApi.ts`                                                                                                                             | ✅ Completo                                                                           |
| Frontend activación / inventario                | `ActivateCollarTagPage.tsx`, `CollarTagInventorySection.tsx`                                                                                                                   | ✅ Completo                                                                           |
| OAuth callback                                  | `GET /api/collars/tractive/callback`                                                                                                                                           | ✅ Completo                                                                           |
| **Alertas de conectividad (offline + batería)** | `CollarConnectivityAlertService`, `CollarConnectivityAlertJob` (cada 15 min), `UpdateCollarNotificationPreferencesCommand`, `CollarStatusBadge.tsx`, `CollarBatteryGauge.tsx`  | ✅ Completo — ver `docs/COLLAR_IMPLEMENTATION_PLAN.md` §Semana 1                      |
| **Auditoría de eventos**                        | `CollarAuditEntry`, `CollarAuditRepository`, logging en Activate/Deactivate/GenerateKey/Ingest/Admin, `CollarAuditLogTab.tsx`                                                  | ✅ Completo — ver `docs/COLLAR_IMPLEMENTATION_PLAN.md` §Semana 2/3                    |
| **Transferencia segura (Handover)**             | `CollarHandoverCode`, `GenerateCollarHandoverCodeCommand`, `RedeemCollarHandoverCodeCommand`, `CollarHandoverDialog.tsx`, `CollarHandoverRedeemPage.tsx`                       | ✅ Completo — ver `docs/COLLAR_IMPLEMENTATION_PLAN.md` §Semana 3                      |
| **Lost Mode (búsqueda activa)**                 | `Collar.IsLost`, `ActivateCollarLostModeCommand`, `LostPetEvent.UpdateLastSeenLocation`, `TractivePollingJob` (ciclo dual), `CollarLostModeToggle.tsx`                         | ✅ Completo — ver `docs/COLLAR_IMPLEMENTATION_PLAN.md` §Semana 4                      |
| **Geofencing (Safe Zones)**                     | `CollarSafeZone`, `GeoPolygon`, `CollarSafeZoneEvaluationService`, `CollarSafeZonesPanel.tsx`                                                                                  | ✅ Completo — ver `docs/COLLAR_IMPLEMENTATION_PLAN.md` §Semana 5                      |
| **Historial de ubicaciones + export**           | `GetCollarLocationHistoryRangeQuery`, export.csv, location-heatmap, `CollarLocationHistoryPanel.tsx`                                                                           | ✅ Completo — ver `docs/COLLAR_IMPLEMENTATION_PLAN.md` §Semana 7                      |
| **Admin Dashboard mejorado**                    | `GetCollarTagMetricsQuery`, `BulkMarkCollarTagsSoldCommand`, `BulkRevokeCollarTagsCommand`, `CollarTagInventorySection.tsx` (métricas + filtros + bulk)                        | ✅ Completo — ver `docs/COLLAR_IMPLEMENTATION_PLAN.md` §Semana 7                      |
| **E2E Testing Suite**                           | `frontend/e2e/*.spec.ts` (Playwright), `frontend/playwright.config.ts`, `.github/workflows/e2e.yml`                                                                            | ✅ Completo (alcance ajustado) — ver `docs/COLLAR_IMPLEMENTATION_PLAN.md` §Semana 8-9 |
| **CollarTag (activación + inventario)**         | `CollarTags`, `CollarDeviceCredentials`, bulk import, activate/deactivate, device key generation                                                                               | ✅ Completo (fases 1–3) — §6                                                          |
| **Kippy**                                       | `KippyService.cs`                                                                                                                                                              | ❌ Sin viabilidad en CR — ver §3.2                                                    |
| **Hardware propio**                             | PCB ESP32-S3 + SIM7080G                                                                                                                                                        | ❌ Roadmap futuro (fase 4)                                                            |

---

## 2. Modelo de datos

```
Collar (existe)
├── Id (Guid v7)
├── PetId → Pets.Id
├── OwnerId → Users.Id
├── Provider (0=Own, 1=Tractive, 2=Kippy, 99=Generic)
├── ExternalDeviceId (string?)   — IMEI o deviceId del proveedor externo
├── ExternalTokenEncrypted (string?)   — token OAuth cifrado AES-256
├── BatteryPercent (int?)
├── LastLat / LastLng (double?)
├── LastSeenAt (DateTimeOffset?)
├── CollarTagSerial (string?)   — ✅ implementado: vincula al serial físico
├── IsActive (bool)
└── RegisteredAt

CollarLocation (existe, write-heavy, purge >30 días)
├── Id (Guid v7)
├── CollarId → Collars.Id
├── Lat / Lng
├── RecordedAt
└── INDEX (CollarId, RecordedAt DESC)

CollarTag (✅ implementado — migración AddCollarTags)
├── Id (Guid v7)
├── Serial (NVARCHAR(30), único) — PT-[4 hex]-[7 dígitos], grabado láser
├── CollarId (Guid?)   — null = no activado
├── Status (Unactivated / Activated / Deactivated / Replaced)
├── FirmwareVersion (string)
├── ManufacturedAt / SoldAt / ActivatedAt / LastPingAt

CollarDeviceCredential (✅ implementado — migración AddCollarTags)
├── Id (Guid v7)
├── CollarId → Collars.Id
├── KeyHash (SHA-256, NVARCHAR(64))   — nunca raw
├── CreatedAt / RevokedAt / LastUsedAt
└── INDEX (KeyHash)   — para búsqueda O(1) en cada ingest
```

---

## 3. Proveedores

### 3.1 Tractive (implementado ✅)

Líder mundial de GPS para mascotas, +10M dispositivos. Disponible en CR vía Amazon + Aerocasillas.

**Precios (2026):**

| Producto                    | USD      | CRC aprox. |
| --------------------------- | -------- | ---------- |
| Tractive DOG 6 / CAT 6 Mini | $79      | ₡41,000    |
| Plan 1 año                  | $120/año | ₡62,400    |
| Plan 2 años                 | $168     | ₡87,360    |
| Plan 5 años                 | $300     | ₡156,000   |

> La suscripción Tractive la paga el usuario **directamente a Tractive**. PawTrack no la intermedia.

**Programa de afiliados (único canal comercial):**

| Dato       | Valor                                                                     |
| ---------- | ------------------------------------------------------------------------- |
| Comisión   | $20 USD fijo por tracker vendido                                          |
| Cookie     | 30 días                                                                   |
| Plataforma | [Impact.com](https://app.impact.com/campaign-promo-signup/Tractive.brand) |
| Registro   | [tractive.com/landing/affiliate](https://tractive.com/landing/affiliate)  |

Tractive rechaza: cupones, cashback, subnetworks y pujas en sus keywords de marca.

**Flujo de integración (OAuth2):**

1. Dueño abre tab GPS → "Conectar Tractive"
2. Frontend llama `GET /api/collars/tractive/connect?petId=...`
3. Backend genera la URL OAuth con `state = "{userId}:{petId}"` y redirige al usuario
4. Dueño autoriza en tractive.com → redirige a `/api/collars/tractive/callback`
5. Backend intercambia `code` por token, cifra con AES-256, actualiza el collar asociado y vuelve al perfil del pet
6. `TractivePollingJob` actualiza posición cada 5 min

**Endpoints Tractive usados:**

| Endpoint                               | Propósito                     |
| -------------------------------------- | ----------------------------- |
| `POST /api/1/user/oauth/token`         | Intercambio de code por token |
| `GET /3/tracker/{id}/positions/recent` | Última posición               |
| `GET /3/tracker/{id}`                  | Estado (batería, etc.)        |

**Variables Key Vault requeridas:**

```
Tractive:ClientId       — app en developers.tractive.com
Tractive:ClientSecret
Tractive:EncryptKey     — 32 bytes base64 para AES-256
```

---

### 3.2 Kippy (código reservado, no viable en CR)

Tracker GPS + salud de Datamars (Suiza/Italia). Popular en Europa.

**Cobertura: solo Europa.** La SIM integrada conecta únicamente en AT, BE, HR, DK, ES, FR, DE, GR, HU, IE, IT, NL, NO, PL, PT, RO, RS, SE, CH, GB + Sudáfrica. **Costa Rica no está en la lista.**

| Característica  | Valor                                                                                |
| --------------- | ------------------------------------------------------------------------------------ |
| Precio hardware | €41.99 (~$46)                                                                        |
| Suscripción     | desde €3.33/mes                                                                      |
| Batería         | hasta 12 días                                                                        |
| IP              | IP67                                                                                 |
| API             | Interna sin docs públicas (`https://api.kippy.eu/v1/`) — riesgo de cambios sin aviso |

**Veredicto:** `CollarProvider.Kippy = 2` está reservado en el dominio para una eventual expansión a España. No implementar hasta tener usuarios en mercados de cobertura Kippy.

**Cuando aplique (estimado 1–2 días):**

```csharp
public sealed class KippyService(IHttpClientFactory factory, IConfiguration config) : ICollarService
{
    private const string ApiBase = "https://api.kippy.eu/v1";

    public async Task<CollarPosition?> GetLatestPositionAsync(string encryptedApiKey, string deviceId, CancellationToken ct)
    {
        var apiKey = Decrypt(encryptedApiKey);
        var client = factory.CreateClient("Kippy");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        var response = await client.GetFromJsonAsync<KippyPositionResponse>($"{ApiBase}/pet/{deviceId}/location", ct);
        return response is null ? null : new CollarPosition(response.Lat, response.Lng, response.Battery);
    }
}
```

---

### 3.3 OEM China — Fabricantes candidatos

Para el collar de marca PawTrack, cuatro fabricantes candidatos clasificados por prioridad:

| Fabricante    | Modelo ref.        | API                     | MOQ    | Fortaleza                                  | Prioridad |
| ------------- | ------------------ | ----------------------- | ------ | ------------------------------------------ | --------- |
| **Concox**    | AT4 (GPS+WiFi+LTE) | REST propia documentada | 50 u.  | `sales@concox.com` verificado, FCC/CE/ROHS | **1°**    |
| **Jimi IoT**  | JM-VL01 / LL01     | REST + MQTT             | 50 u.  | Push (MQTT) + polling — más flexible; **en conversación activa, respondieron RFQ 2026-09-03, ver `docs/jimiiot.md` §8** | **1°** (promovido) |
| **Queclink**  | GL300 miniatura    | REST + binario propio   | 50 u.  | Hardware robusto y compacto                | 3°        |
| **ThinkRace** | TK115 pet-specific | REST + WebSocket        | 100 u. | Diseño pensado para collar                 | 4°        |

**Precios detallados (Concox AT4 como referencia):**

| Concepto                       | Costo USD                 |
| ------------------------------ | ------------------------- |
| Unidad FCA Shenzhen            | $18                       |
| Flete DHL (50 u.)              | $4/u                      |
| Impuestos CR (~15%)            | $2.70/u                   |
| SIM IoT mensual                | $2/mes/u                  |
| **Total landed CR (hardware)** | **~$24.70/u**             |
| Precio venta sugerido          | ₡20,000–25,000 (~$38–$48) |
| Margen bruto hardware          | ~$13–$23/u                |

**Inversión mínimo viable (50 u.):**

- $900 hardware + $200 flete + $135 impuestos ≈ **$1,235 USD**
- SIM activación 50 collares: ~$100 primer mes
- **Total primer lote: ~$1,335 USD**

**SIMs IoT recomendadas para CR:**

| Proveedor    | Cobertura CR     | USD/mes/SIM | Dashboard   |
| ------------ | ---------------- | ----------- | ----------- |
| **Emnify**   | Movistar + Kölbi | $1.50–$2.50 | REST API ✅ |
| **Hologram** | Claro + Kölbi    | $1.00–$2.00 | REST API ✅ |

**Proceso de importación China → CR:**

```
Semana 1   → Pedir muestras ($50–100 + DHL $30), validar GPS/batería/waterproof
Semana 2–3 → Integrar API como CollarProvider.Generic, confirmar polling funciona
Semana 4   → Confirmar orden 50 u. (T/T 30% adelanto / 70% antes embarque)
             Producción: 15–20 días
Semana 6–7 → DHL Shenzhen → SJO 3–5 días; agente aduanal obligatorio >$1,000 CIF
             Código arancelario: 8526.91.00 | Impuestos: ~15% CIF
Semana 8   → QA (testear 5–10% unidades), activar SIMs, configurar endpoint
```

Agentes aduanales en CR (referencia): Grupo Logístico Aduanero (`logisticaaduanera.cr`), costo ~$80–$120/trámite.

---

### 3.4 Hardware propio PawTrack (roadmap futuro)

`CollarProvider.Own = 0` reservado. Arquitectura recomendada:

```
ESP32-S3 + SIM7080G → MQTT/TLS → Azure IoT Hub → Azure Function → POST /api/collars/ingest
```

**BOM por unidad:**

| Componente    | Modelo                    | USD   |
| ------------- | ------------------------- | ----- |
| MCU           | ESP32-S3 (dual-core, BLE) | $4    |
| Celular + GPS | SIM7080G (LTE-M + GNSS)   | $12   |
| Acelerómetro  | ADXL345                   | $0.80 |
| Batería       | LiPo 3.7V 1000mAh         | $3.50 |
| PCB           | JLCPCB 5 prototipos + SMT | $2–15 |
| Case          | TPU flexible impreso 3D   | $5–15 |

**Estrategia de batería (objetivo 3–5 días):**

| Estado                          | Condición                            | Consumo   | Duración aprox. |
| ------------------------------- | ------------------------------------ | --------- | --------------- |
| Activo (movimiento)             | GPS hot fix cada 30s, MQTT burst     | ~250 mA   | —               |
| Light sleep (quieto >2 min)     | ADXL345 wake-on-motion, timer 10 min | ~1–3 mA   | —               |
| Deep sleep (quieto >30 min)     | ESP32 10–15 µA, SIM PSM 0.4 mA       | ~0.5–1 mA | —               |
| **Mascota en casa (2% activo)** | —                                    | ~5 mA     | **~8 días**     |
| **Mascota activa (2h/día)**     | —                                    | ~43 mA    | **~23 horas**   |

> La clave es ADXL345 como interrupt source — sin acelerómetro, el timer forzado consume el 80% de la batería en wakups innecesarios.

**GPS cold fix vs hot fix:** Usar A-GPS (efemérides via LTE en SIM7080G) para pasar de 30–90s (cold) a 3–8s (hot). Sin esto, cada despertar del deep sleep es un cold fix de 100 mA por hasta 90s.

---

## 4. Integración de proveedores OEM (Caminos A y B)

### Camino A — Polling REST (recomendado para el primer lote)

Clonar `TractivePollingJob` para el proveedor elegido. Esfuerzo: ~1–2 días.

**Pasos:**

1. Confirmar en el RFQ si el proveedor usa OAuth2 o API key estática (cambia si hay intercambio de token).
2. Registrar el `deviceId`/IMEI del collar en la plataforma del proveedor (manual o vía API de aprovisionamiento).
3. `ExternalDeviceId` en `Collar` guarda el IMEI — mismo campo que Tractive, sin cambio de esquema.
4. Crear `I{Proveedor}Service` + job que filtra `Collar.Provider == CollarProvider.Generic`.
5. Normalizar la respuesta a `CollarPosition(Lat, Lng, BatteryPercent)`.
6. Revisar rate limits del proveedor: con 50+ collares cada 5 min puede necesitarse un endpoint "bulk".

**Lo que puede aumentar el esfuerzo:** firma HMAC manual, o suscripción previa a webhook del proveedor para habilitar el polling.

### Camino B — Push directo HTTP (gap de seguridad pendiente)

El collar (o gateway del proveedor) hace `POST /api/collars/ingest` directamente.

**Gap a cerrar antes de producción:** el endpoint actual hereda `[Authorize]` y exige JWT de usuario. Solución:

1. Tabla `CollarDeviceCredentials` (`CollarId`, `KeyHash`, `CreatedAt`, `RevokedAt`).
2. Endpoint con `[AllowAnonymous]` + `CollarDeviceKeyMiddleware` validando header `X-Collar-Key`.
3. Reutilizar el patrón de `ClinicApiKeyMiddleware` (secreto hash SHA-256, nunca en texto plano en DB).

> **Recomendación:** arrancar con Camino A en el primer lote; evaluar Camino B solo con hardware 100% propio.

---

## 5. Variantes de producto a cotizar

Solicitar todas las variantes en el mismo RFQ para comparar costo incremental real:

| Variante                         | Qué incluye            | Impacto backend                                      | Impacto batería                        |
| -------------------------------- | ---------------------- | ---------------------------------------------------- | -------------------------------------- |
| **V1 — GPS base**                | GPS/LTE-M              | Ninguno — exactamente `CollarProvider.Generic`       | Referencia base (~3–5 días)            |
| **V2 — GPS + cámara**            | GPS + cámara baja res. | Endpoint nuevo de ingesta de imagen + Blob Storage   | Alto — pedir consumo real por captura  |
| **V3 — GPS + pantalla e-ink**    | GPS + e-ink pequeña    | Ninguno adicional si la pantalla es solo informativa | Bajo si e-ink (~0 mA reposo)           |
| **V4 — GPS + cámara + pantalla** | Combinación completa   | Suma V2 + V3                                         | El más alto — validar con muestra real |

**Para el RFQ, pedir específicamente por variante:**

- **V2:** resolución, formato (JPEG), tamaño de archivo, frecuencia máxima de captura sostenible, cómo se descarga la imagen (API propia vs nube del proveedor).
- **V3:** tipo de pantalla (solo aceptar e-ink, rechazar OLED), consumo reposo/refresco, si el contenido puede fijarse en fábrica.
- **V4:** estimación de batería real bajo escenario concreto (ej. "GPS cada 10 min + 1 foto/día + pantalla estática").

---

## 6. CollarTag — Activación tipo AirTag

> ✅ **Implementado** (2026-09-01) — Fases 1–3 completas. Suite: 1049/1049 unit tests, 75/75 integration tests.

### 6.1 Entidades implementadas

Archivos en `backend/src/PawTrack.Domain/Collars/`:

**CollarTag:**

```csharp
public sealed class CollarTag
{
    public Guid Id { get; private set; }
    public string Serial { get; private set; } = string.Empty;  // PT-A3F9-0001234
    public Guid? CollarId { get; private set; }
    public CollarTagStatus Status { get; private set; }
    public string FirmwareVersion { get; private set; } = string.Empty;
    public DateTimeOffset ManufacturedAt { get; private set; }
    public DateTimeOffset? SoldAt { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? LastPingAt { get; private set; }

    public static CollarTag CreateFromFactory(string serial, string firmwareVersion);
    public void MarkSold();
    public void Activate(Guid collarId);
    public void Deactivate();
    public void UpdateLastPing();
}

public enum CollarTagStatus { Unactivated, Activated, Deactivated, Replaced }
```

**CollarDeviceCredential:**

```csharp
public sealed class CollarDeviceCredential
{
    public Guid Id { get; private set; }
    public Guid CollarId { get; private set; }
    public string KeyHash { get; private set; } = string.Empty;  // SHA-256, nunca raw
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public bool IsRevoked => RevokedAt.HasValue;

    public static CollarDeviceCredential Create(Guid collarId, string keyHash);
    public void Revoke();
    public void RecordUsage();
}
```

### 6.2 Flujo completo de activación

```
FÁBRICA
  Serial único grabado láser en el enclosure (PT-A3F9-0001234)
  Serial quemado en firmware del MCU
  Registrado en CollarTags con Status = Unactivated

COMPRA
  Admin marca serial como Sold en el dashboard
  Collar llega al cliente en caja sellada

ACTIVACIÓN (app del cliente)
  1. Mascota → tab GPS → "Activar CollarTag"
  2. Escanea QR del collar  ──O──  ingresa serial manualmente
  3. App: GET /api/collars/tag/{serial} → valida disponibilidad
  4. Cliente elige mascota a vincular
  5. App: POST /api/collars/tag/{serial}/activate  { petId }
     Backend: vincula serial → petId → genera CollarDeviceCredential → retorna raw key UNA SOLA VEZ
  6. Collar queda activo en tab GPS ✅

OPERACIÓN NORMAL
  Firmware: POST /api/collars/ingest  { serial, lat, lng, battery, timestamp }
  Authorization: X-Collar-Key: {collarApiKey}
  Backend: valida key → resuelve CollarTag → Collar → guarda en CollarLocations

TRANSFERENCIA / REVENTA
  Dueño toca "Desvincular collar"
  CollarTag vuelve a Status = Unactivated
  Nuevo dueño puede reactivarlo
```

### 6.3 Endpoints implementados

| Método   | Ruta                                                  | Auth                                    | Descripción                                                             |
| -------- | ----------------------------------------------------- | --------------------------------------- | ----------------------------------------------------------------------- |
| `GET`    | `/api/collars/tag/{serial}`                           | JWT usuario                             | Verificar disponibilidad del serial                                     |
| `POST`   | `/api/collars/tag/{serial}/activate`                  | JWT usuario                             | Vincular serial → mascota del usuario                                   |
| `DELETE` | `/api/collars/tag/{serial}/deactivate`                | JWT usuario                             | Desvincular para transferencia                                          |
| `POST`   | `/api/collars/ingest`                                 | `X-Collar-Key` header                   | Recibir posición desde el dispositivo                                   |
| `POST`   | `/api/collars/{collarId}/generate-key`                | JWT usuario                             | Generar llave de dispositivo para hardware genérico                     |
| `POST`   | `/api/collars/pet/{petId}/location`                   | JWT usuario                             | Registrar ubicación manual del collar                                   |
| `GET`    | `/api/admin/collar-tags`                              | JWT admin                               | Inventario de dispositivos                                              |
| `POST`   | `/api/admin/collar-tags`                              | JWT admin                               | Registrar serial en inventario                                          |
| `POST`   | `/api/admin/collar-tags/{serial}/mark-sold`           | JWT admin                               | Marcar como vendido                                                     |
| `POST`   | `/api/admin/collar-tags/{serial}/revoke`              | JWT admin                               | Revocar acceso de un collar robado o comprometido                       |
| `GET`    | `/api/admin/collar-tags/metrics`                      | JWT admin                               | KPIs de inventario (total, por estado, vendidos 30d, inventario muerto) |
| `POST`   | `/api/admin/collar-tags/bulk-mark-sold`               | JWT admin                               | Marcar múltiples seriales como vendidos                                 |
| `POST`   | `/api/admin/collar-tags/bulk-revoke`                  | JWT admin                               | Revocar credenciales de múltiples seriales                              |
| `GET`    | `/api/collars/{collarId}/connectivity-status`         | JWT usuario                             | Estado offline/batería + preferencias de alerta                         |
| `PUT`    | `/api/collars/{collarId}/notification-preferences`    | JWT usuario                             | Actualizar umbrales de alerta offline/batería                           |
| `GET`    | `/api/collars/{collarId}/audit-log`                   | JWT usuario                             | Historial de eventos del collar (activación, revocaciones, etc.)        |
| `GET`    | `/api/admin/collar-tags/{serial}/audit-log`           | JWT admin                               | Historial completo del serial, incluye eventos pre-activación           |
| `POST`   | `/api/collars/{collarId}/handover/generate`           | JWT usuario (dueño)                     | Genera PIN de 6 dígitos para transferir el collar                       |
| `POST`   | `/api/collars/handover/{id}/cancel`                   | JWT usuario (dueño)                     | Cancela un código de transferencia antes de canjearlo                   |
| `POST`   | `/api/collars/handover/redeem`                        | JWT usuario (nuevo dueño), rate-limited | Canjea el PIN y libera el serial para reactivación                      |
| `POST`   | `/api/collars/{collarId}/lost-mode/activate`          | JWT usuario (dueño)                     | Activa modo perdido: tracking más frecuente + vincula/crea reporte      |
| `POST`   | `/api/collars/{collarId}/lost-mode/deactivate`        | JWT usuario (dueño)                     | Desactiva modo perdido (no cierra el reporte)                           |
| `GET`    | `/api/collars/{collarId}/lost-mode-status`            | JWT usuario (dueño)                     | Estado actual del modo perdido                                          |
| `POST`   | `/api/collars/{collarId}/safe-zones`                  | JWT usuario (dueño)                     | Crear zona segura (polígono de puntos lat/lng)                          |
| `GET`    | `/api/collars/{collarId}/safe-zones`                  | JWT usuario (dueño)                     | Listar zonas seguras del collar                                         |
| `PUT`    | `/api/collars/safe-zones/{zoneId}`                    | JWT usuario (dueño)                     | Actualizar nombre/polígono/estado de una zona                           |
| `DELETE` | `/api/collars/safe-zones/{zoneId}`                    | JWT usuario (dueño)                     | Eliminar una zona segura                                                |
| `GET`    | `/api/collars/{collarId}/location-history`            | JWT usuario (dueño)                     | Historial por rango de fechas (from/to/maxPoints)                       |
| `GET`    | `/api/collars/{collarId}/location-history/export.csv` | JWT usuario (dueño)                     | Descarga el historial como CSV                                          |
| `GET`    | `/api/collars/{collarId}/location-heatmap`            | JWT usuario (dueño)                     | Puntos para mapa de densidad (hasta 30 días)                            |

### 6.4 Endpoint de ingest (crítico)

```csharp
// POST /api/collars/ingest — usa X-Collar-Key, NO JWT de usuario
// CollarDeviceKeyMiddleware (patrón de ClinicApiKeyMiddleware):
//   1. Lee X-Collar-Key header
//   2. Computa SHA-256
//   3. Busca en CollarDeviceCredentials por hash (excluye revocados)
//   4. Inyecta CollarId en el contexto
//   5. Si no encuentra → 401

public sealed record IngestLocationRequest(
    string Serial,
    double Lat,
    double Lng,
    int? BatteryPercent,
    DateTimeOffset Timestamp,
    int? AccuracyMeters);
```

### 6.5 Comando de activación

```csharp
public sealed record ActivateCollarTagCommand(
    string Serial, Guid PetId, Guid OwnerId) : IRequest<Result<ActivateCollarTagResultDto>>;

public sealed record ActivateCollarTagResultDto(
    Guid CollarId, string Serial,
    string CollarApiKey);  // raw key — mostrar UNA SOLA VEZ

// Handler:
// 1. Verificar Serial existe en CollarTags y está Unactivated
// 2. Verificar PetId pertenece a OwnerId
// 3. Verificar OwnerId tiene plan Plus activo
// 4. Deactivar collar previo del pet si existe
// 5. Crear Collar con Provider = Own
// 6. Vincular CollarTag.CollarId
// 7. Generar CollarDeviceCredential (SHA-256)
// 8. Retornar raw key una sola vez
// 9. Todo en la misma transacción
```

### 6.6 Frontend de activación

**Ruta:** `/collars/activate` o desde `PetDetailPage → tab GPS`

- Paso 1: Escáner QR de la cámara + fallback manual; validación instantánea con `GET /api/collars/tag/{serial}`
- Paso 2: Selector de mascota (preselecciona si el usuario tiene solo una)
- Paso 3: Confirmación → `POST activate` → muestra raw key con advertencia "Solo se muestra una vez"
- Paso 4: Animación de éxito → redirige a `PetDetailPage → tab GPS`

**Cambios en `CollarGpsTab`:** agregar opción "Activar CollarTag PawTrack" junto a "Conectar Tractive"; mostrar serial y firmware version cuando `provider === "Own"`; opción "Desvincular collar" con confirmación.

**Cambios en `collarApi.ts`:**

```typescript
checkSerial: (serial: string): Promise<{ available: boolean; status: string }> =>
    apiClient.get(`/collars/tag/${serial}`).then(r => r.data),

activate: (serial: string, petId: string): Promise<{ collarId: string; collarApiKey: string }> =>
    apiClient.post(`/collars/tag/${serial}/activate`, { petId }).then(r => r.data),

deactivate: (serial: string): Promise<void> =>
    apiClient.delete(`/collars/tag/${serial}/deactivate`).then(() => undefined),
```

### 6.7 Dashboard admin de inventario

| Acción                 | Descripción                                                |
| ---------------------- | ---------------------------------------------------------- |
| Ver todos los seriales | Lista con estado, fecha activación, última conexión, dueño |
| Registrar seriales     | Bulk import desde CSV                                      |
| Marcar como vendido    | Indicar que salió del inventario                           |
| Revocar acceso         | Para dispositivos reportados robados                       |
| Métricas               | Collares activos, sin ping >24h, firmware versions         |

---

## 7. Firmware del dispositivo

Para el primer lote: usar módulo OEM (§3.3) en **Camino A (polling)** mientras no hay firmware propio. Pasar a **Camino B (push directo)** con el endpoint de ingest listo.

**Módulos recomendados:**

| Módulo                    | Por qué                                                      |
| ------------------------- | ------------------------------------------------------------ |
| **Concox AT4**            | API REST documentada, MOQ 50 u., FCC/CE, contacto verificado |
| **SIM7080G (PCB propio)** | Para firmware totalmente propio con JLCPCB                   |

**Pseudofirmware para el ingest:**

```c
void reportLocation() {
    GPSFix fix = gps_get_hot_fix();
    int battery = battery_read_percent();

    char body[256];
    snprintf(body, sizeof(body),
        "{\"serial\":\"%s\",\"lat\":%.6f,\"lng\":%.6f,"
        "\"battery\":%d,\"timestamp\":\"%s\"}",
        DEVICE_SERIAL, fix.lat, fix.lng, battery, iso8601_now());

    http_post("https://pawtrack.cr/api/collars/ingest",
              "X-Collar-Key: " COLLAR_API_KEY,
              body);
}
// COLLAR_API_KEY llega al dispositivo por BLE al activarse — nunca quemada de fábrica en texto plano
```

**Quema de la API key en el firmware:**

- **Opción A — BLE (recomendada):** Collar en modo pairing al encender por primera vez. App envía `{ collarApiKey, serverUrl }` vía GATT Write. Firmware guarda en NVS del ESP32.
- **Opción B — QR en caja:** Key generada en fábrica, impresa en papel dentro de la caja. Usuario la introduce manualmente. Más fácil de implementar, peor UX.

---

## 8. Seguridad

| Riesgo                                    | Mitigación                                                                                              |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| Serial guessable                          | Agregar componente hex aleatorio: `PT-[4 hex]-[secuencia]`. No exponer el patrón en la API pública.     |
| Brute force serial en `GET /tag/{serial}` | Rate limiting: 5 intentos/min por IP. Captcha tras 3 fallos.                                            |
| Collar robado sigue reportando            | Admin revoca `CollarDeviceCredential` desde el dashboard.                                               |
| Key en firmware                           | En DB solo SHA-256. En NVS del MCU está en texto plano — limitación de hardware aceptable.              |
| Suplantación de serial                    | El servidor valida que `body.serial` coincide con el `CollarTag.Serial` asociado a la credential usada. |
| Endpoint ingest sin JWT                   | `X-Collar-Key` header + `CollarDeviceKeyMiddleware` (patrón de `ClinicApiKeyMiddleware`).               |

---

## 9. Plan de implementación CollarTag por fases

### Fase 1 — Backend mínimo viable ✅

- [x] Entidad `CollarTag` + migración EF Core
- [x] Entidad `CollarDeviceCredential` + migración
- [x] `ActivateCollarTagCommand` (serial → Collar + key)
- [x] `DeactivateCollarTagCommand` (reset a Unactivated)
- [x] `CollarDeviceKeyMiddleware` (patrón de `ClinicApiKeyMiddleware`)
- [x] `POST /api/collars/ingest` con auth por device key
- [x] `POST /api/collars/{collarId}/generate-key` para hardware genérico/propio
- [x] `POST /api/collars/pet/{petId}/location` para ubicaciones manuales
- [x] `GET /api/collars/tag/{serial}` (check disponibilidad)
- [x] `POST /api/admin/collar-tags/bulk-import` (CSV de seriales)
- [x] `CollarTagSerial` en la entidad `Collar`
- [x] Rate limiting en `/api/collars/tag/{serial}` (`collar-serial-check`) y endpoints sensibles
- [x] Tests unitarios del dominio + handler de activación

**Entregable:** Postman activa un serial y el backend acepta posiciones con `X-Collar-Key`. ✅

### Fase 2 — Frontend de activación ✅

- [x] `ActivateCollarTagPage` con escáner QR + fallback manual
- [x] Integrar opción "Activar CollarTag" en `CollarGpsTab`
- [x] Mostrar serial y firmware version cuando `provider === "Own"`
- [x] Flujo de desvinculación con confirmación
- [x] `checkSerial`, `activate`, `deactivate` en `collarApi.ts`
- [x] Pantalla de éxito con animación y redirección a tab GPS

**Entregable:** Usuario activa un collar desde la app en el emulador. ✅

### Fase 3 — Dashboard admin de inventario ✅

- [x] Vista de inventario en `AdminPage` con tabla de CollarTags (tab "CollarTags")
- [x] Bulk import de seriales desde CSV
- [x] Marcar como vendido / revocar acceso
- [x] Métricas de salud (sin ping >24h, firmware versions)

**Entregable:** Admin gestiona inventario sin acceso directo a DB. ✅

### Fase 4 — Hardware físico + firmware (pendiente)

- [ ] Contactar Concox para muestras AT4 con API REST
- [ ] O diseñar PCB ESP32-S3 + SIM7080G para hardware propio
- [ ] Implementar provisioning por BLE o QR paper
- [ ] Prueba de campo (movimiento, batería, edge cases)
- [ ] Validar endpoint de ingest con datos reales

**Entregable:** Un collar físico reporta su posición en el mapa del dueño.

### Fase 5 — Producto comercial (1 semana)

- [ ] Bundle `BundleProductType.CollarTagGps` (estructura de bundles ya existe)
- [ ] Flujo de compra en la app
- [ ] Notificación "Tu CollarTag fue enviado" + tracking number

---

## 10. Modelo comercial y precios para Costa Rica

### Opciones de distribución

| Opción                    | Inversión inicial   | Costo/unidad       | Precio venta sugerido     | Margen neto          | Tiempo al mercado |
| ------------------------- | ------------------- | ------------------ | ------------------------- | -------------------- | ----------------- |
| **A — Afiliado Tractive** | $0                  | N/A                | $79 USD (Amazon)          | $20 USD fijo/tracker | **Inmediato**     |
| **B — Hardware propio**   | $3,000+ USD         | ~$30 USD           | $60–80 USD                | $30–50 USD           | 3–4 meses         |
| **C — OEM Concox/Jimi**   | ~$1,335 USD (50 u.) | ~$27 USD landed CR | ₡20,000–25,000 (~$38–$48) | $10–20 USD           | 2–3 meses         |

**Recomendación:** arrancar con Opción A (cero riesgo) y pivotar a C cuando haya >100 suscriptores Plus que justifiquen el MOQ de 50 unidades.

### Comparativa de productos disponibles en CR

| Opción                   | Precio inicial  | Total/mes | QR                | GPS        | Cuentas requeridas                  |
| ------------------------ | --------------- | --------- | ----------------- | ---------- | ----------------------------------- |
| 🏷️ Placa QR + Explorador | ₡1,500–4,500    | ₡0        | ✅                | ❌         | Solo PawTrack                       |
| 🏷️ Placa QR + Plus       | ₡1,500–4,500    | ₡2,990    | ✅                | ❌         | Solo PawTrack                       |
| 📡 OEM Concox + QR láser | ₡22,000–26,000  | ~₡4,030   | ✅ grabado        | ✅ Básico  | PawTrack + Emnify/Hologram          |
| ⭐ Tractive DOG 6        | ₡41,000 + placa | ~₡8,190   | ⚠️ pieza separada | ✅ Premium | PawTrack + **Tractive obligatorio** |
| 🔧 Hardware PawTrack     | ₡35,000–50,000  | ~₡4,030   | ✅ integrado      | ✅ Custom  | PawTrack + SIM gestionada           |

> Tractive es el único segmento donde el usuario **debe** abrir y pagar una suscripción externa obligatoria (₡5,200/mes directos a Tractive). Aclararlo en el onboarding.

### Plan de suscripción PawTrack

| Plan                 | Collar GPS                       | Historial |
| -------------------- | -------------------------------- | --------- |
| Explorador (gratis)  | ❌ (tab visible, CTA a Plus)     | —         |
| Plus (₡2,990/mes)    | ✅ Tractive, Kippy, Generic, Own | 7 días    |
| Familia (₡4,990/mes) | ✅ Todos los providers           | 7 días    |

**El collar GPS es el diferenciador de conversión más fuerte del plan Plus.**

### Posicionamiento correcto

- Vender PawTrack por su red de avistamientos y QR — diferenciador único, sin hardware.
- El GPS es el upsell para quienes ya tienen o quieren un tracker.
- El afiliado Tractive genera ₡10,400 una sola vez por usuario, sin costo operativo.
- OEM con SIM IoT gestionada por PawTrack (Emnify) elimina la fricción de cuenta externa y habilita el CollarTag como producto propio.

---

## 11. Checklist de configuración de producción

- [ ] Crear app OAuth en [developers.tractive.com](https://developers.tractive.com) → Redirect URI: `https://pawtrack.cr/api/collars/tractive/callback`
- [ ] Configurar `Tractive:ClientId`, `Tractive:ClientSecret`, `Tractive:EncryptKey` en Key Vault
- [ ] Verificar que `TractivePollingJob` arranca en el Container App (revisar logs al inicio)
- [ ] Configurar `App:BaseUrl=https://pawtrack.cr` en producción
- [ ] Probar flujo completo con un Tractive físico en staging
- [ ] (Cuando aplique) `Collar:KippyEnabled=true` en Key Vault
- [ ] (Cuando aplique) `Azure:IoTHubConnectionString` en Key Vault para hardware propio

---

## 12. RFQ — Plantilla de correo para fabricantes

Enviar el mismo correo a los 4 fabricantes (§3.3), cambiando nombre y modelo referenciado.

```
Subject: RFQ — GPS Pet Tracker Collar, Multiple Product Variants (OEM/Custom Branding) — PawTrack CR

Hello [Contact Name],

We are PawTrack CR, a pet-identification and lost-pet recovery platform based in
Costa Rica. We are evaluating manufacturing partners for a GPS pet tracker collar
and would like to request a formal quotation plus technical documentation for
your [Model Name, e.g. AT4 / JM-VL01 / GL300 / TK115].

We are interested in comparing multiple product variants built on the same base
platform, so please quote and document each one separately:

  - V1: GPS + LTE tracker only (base variant)
  - V2: GPS + LTE tracker + onboard camera
  - V3: GPS + LTE tracker + e-ink/e-paper display
  - V4: GPS + LTE tracker + camera + e-ink display (full combo)

Could you please share the following for each variant:

1. PRODUCT & API
   - Is the API REST (HTTP/JSON) or proprietary binary (GT06, JT808)?
     Please share full API documentation.
   - Do you support server-to-server webhooks/push to our own HTTPS endpoint,
     or is polling against your cloud the only option?
   - Do you offer white-label/OEM firmware configurable to report to our server?

2. CAMERA MODULE (V2 and V4 only)
   - Image resolution, format (JPEG), typical file size per photo.
   - How is the image delivered (pushed to our server, pulled via API, or only
     through your platform/app)?
   - Maximum sustainable capture frequency without draining battery in <24h.

3. E-INK DISPLAY (V3 and V4 only)
   - Available display sizes, refresh time.
   - Power draw at rest vs. refresh cycle.
   - Can content be fixed at the factory (static QR), or requires BLE/firmware update?

4. POWER & CONNECTIVITY
   - Estimated battery life per variant under GPS reporting every 5–10 min
     (concrete usage scenario, not marketing estimate).
   - LTE-M / NB-IoT bands compatible with Costa Rican carriers
     (Kölbi, Movistar, Claro)?
   - Pre-activated eSIM or do we source our own IoT SIM?

5. CERTIFICATIONS & QUALITY
   - Current certifications (FCC, CE, ROHS) and IP rating.

6. COMMERCIAL TERMS (per variant)
   - MOQ and unit price at 50 / 100 / 500 units (FCA Shenzhen), per variant.
   - Cost and lead time for 2–3 samples per variant.
   - Standard production lead time after order confirmation.
   - Custom branding (logo, packaging)?

We are planning an initial pilot of ~50 units (V1), scaling to 500+ and
adding camera/display variants within year one if the pilot performs well.

Best regards,
[Your Name]
PawTrack CR — [Email] | [Phone/WhatsApp]
```

---

## 13. Referencias de código

| Archivo                                                                                        | Descripción                                                     |
| ---------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| `backend/src/PawTrack.Domain/Collars/CollarTag.cs`                                             | Entidad CollarTag + validación de serial                        |
| `backend/src/PawTrack.Domain/Collars/CollarDeviceCredential.cs`                                | Credencial de dispositivo                                       |
| `backend/src/PawTrack.Application/Collars/Commands/ActivateCollarTag/`                         | Handler de activación (9 pasos)                                 |
| `backend/src/PawTrack.Application/Collars/Commands/DeactivateCollarTag/`                       | Handler de desactivación                                        |
| `backend/src/PawTrack.Application/Collars/Commands/IngestCollarLocation/`                      | Handler de ingest                                               |
| `backend/src/PawTrack.Application/Collars/Commands/Admin/CollarTagAdminCommands.cs`            | Register, MarkSold, BulkImport, Revoke                          |
| `backend/src/PawTrack.Application/Collars/CollarDeviceKeyHasher.cs`                            | SHA-256 del raw key                                             |
| `backend/src/PawTrack.Application/Common/Interfaces/ICollarTagRepository.cs`                   | Interfaz del repositorio de tags                                |
| `backend/src/PawTrack.Application/Common/Interfaces/ICollarDeviceCredentialRepository.cs`      | Interfaz del repositorio de credenciales                        |
| `backend/src/PawTrack.Infrastructure/Collars/CollarTagRepository.cs`                           | Implementación (2 repos en 1 archivo)                           |
| `backend/src/PawTrack.Infrastructure/Persistence/Configurations/CollarConfiguration.cs`        | Configs EF Core (CollarTag + Credential)                        |
| `backend/src/PawTrack.Infrastructure/Migrations/20260901194315_AddCollarTags.cs`               | Migración: CollarTags, CollarDeviceCredentials, CollarTagSerial |
| `backend/src/PawTrack.API/Middleware/CollarDeviceKeyMiddleware.cs`                             | Auth de dispositivo por X-Collar-Key                            |
| `backend/src/PawTrack.API/Controllers/CollarTagsController.cs`                                 | GET check, POST activate, DELETE deactivate, POST ingest        |
| `backend/src/PawTrack.API/Controllers/CollarTagAdminController.cs`                             | Admin: inventario + bulk-import + revoke                        |
| `backend/src/PawTrack.Domain/Collars/CollarProvider.cs`                                        | Enum de proveedores                                             |
| `backend/src/PawTrack.Infrastructure/Collars/TractivePollingJob.cs`                            | Patrón a clonar para OEM                                        |
| `backend/src/PawTrack.Infrastructure/Collars/TractiveService.cs`                               | Referencia de autenticación OAuth2                              |
| `backend/src/PawTrack.API/Controllers/CollarsController.cs`                                    | Endpoints Tractive existentes                                   |
| `backend/tests/PawTrack.UnitTests/Collars/CollarTagDomainTests.cs`                             | Tests dominio (14 casos)                                        |
| `backend/tests/PawTrack.UnitTests/Collars/Handlers/ActivateCollarTagCommandHandlerTests.cs`    | Tests handler activación (5 casos)                              |
| `backend/tests/PawTrack.UnitTests/Collars/Handlers/DeactivateCollarTagCommandHandlerTests.cs`  | Tests handler desactivación                                     |
| `backend/tests/PawTrack.UnitTests/Collars/Handlers/IngestCollarLocationCommandHandlerTests.cs` | Tests handler ingest                                            |
| `backend/tests/PawTrack.UnitTests/Collars/CollarDeviceKeyMiddlewareTests.cs`                   | Tests middleware (3 casos)                                      |
| `backend/tests/PawTrack.IntegrationTests/Collars/CollarTagActivationTests.cs`                  | Integration test: activación + ingest completo                  |
| `frontend/src/features/pets/pages/ActivateCollarTagPage.tsx`                                   | Página de activación (4 pasos)                                  |
| `frontend/src/features/pets/components/CollarGpsTab.tsx`                                       | Tab GPS actualizada                                             |
| `frontend/src/features/pets/api/collarApi.ts`                                                  | API client con checkSerial/activate/deactivate                  |
| `frontend/src/features/admin/components/CollarTagInventorySection.tsx`                         | Admin: tabla paginada + CSV + acciones                          |
