# PawTrack CR — Collar GPS: Guía Completa

> **Estado: HISTORICO/DETALLE TECNICO.** Consultar
> [COLLAR_CURRENT_STATE.md](COLLAR_CURRENT_STATE.md) para el estado actual.

> **Única fuente de verdad** sobre hardware de collar, proveedores, integración de código, CollarTag (activación tipo AirTag) y sourcing.  
> Consolida: `collar.md`, `collar-china-sourcing.md`, `collarTag.md`.  
> Última actualización: 2026-09-07 — CollarTag **implementado** (fases 1–3); Fase 4 Enterprise **COMPLETA**: Alertas de conectividad, Auditoría de eventos, Transferencia segura (Handover) y Lost Mode **implementados**. Fase 5 **COMPLETA** (4/4): Geofencing (Safe Zones), Historial de ubicaciones + export, Admin Dashboard mejorado, y E2E Testing Suite (Playwright) **implementados**. La plataforma base para telemetría HTTP autenticada existe; la infraestructura IoT de producción para hardware propio se define en §3.4. Conversación activa con **Jimi IoT** (RFQ enviado, respuesta recibida 2026-09-03) — ver `docs/jimiiot.md`.

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
| **Hardware propio: activación e ingesta HTTP**  | `CollarProvider.Own`, CollarTag, `CollarDeviceKeyMiddleware`, `POST /api/collars/pet/{petId}/location`                                                                         | ✅ Base de plataforma completa                                                        |
| **Hardware propio: flota IoT de producción**    | Firmware, DPS, Azure IoT Hub, procesador de telemetría, comandos y OTA                                                                                                         | ❌ Pendiente — diseño en §3.4                                                         |

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

### 3.3 OEM China — Fabricante

> **Collar base y única opción actual de PawTrack: Jimi IoT AL600.**

Fabricante seleccionado para el collar de marca PawTrack:

| Fabricante    | Modelo ref.                     | API                                                                                                             | MOQ               | Fortaleza                                                                                                                         | Prioridad            |
| ------------- | ------------------------------- | --------------------------------------------------------------------------------------------------------------- | ----------------- | --------------------------------------------------------------------------------------------------------------------------------- | -------------------- |
| **Jimi IoT**  | AL600 (recomendado para piloto) | TrackSolid Pro (REST, confirmada) — AL600 estándar NO soporta MQTT/HTTPS API propio, requiere desarrollo custom | 100 u. (estándar) | Respuesta estructurada completa al RFQ recibida 2026-09-10 — ver `docs/jimiiot.md` §8; línea propia de pet wearables ya existente | **1° — collar base** |
| **Queclink**  | GL300 miniatura                 | REST + binario propio                                                                                           | 50 u.             | Hardware robusto y compacto                                                                                                       | 2°                   |
| **ThinkRace** | TK115 pet-specific              | REST + WebSocket                                                                                                | 100 u.            | Diseño pensado para collar                                                                                                        | 3°                   |

**Proceso de importación China → CR (Jimi IoT AL600, MOQ 100 u.):**

```
Semana 1   → Pedir muestras ($50–100 + DHL $30), validar GPS/batería/waterproof
Semana 2–3 → Integrar API TrackSolid Pro, confirmar polling funciona
Semana 4   → Confirmar orden 100 u. (T/T 30% adelanto / 70% antes embarque)
             Producción: 15–20 días
Semana 6–7 → DHL Shenzhen → SJO 3–5 días; agente aduanal obligatorio >$1,000 CIF
             Código arancelario: 8526.91.00 | Impuestos: ver desglose de aduanas abajo
Semana 8   → QA (testear 5–10% unidades), activar dispositivos en TrackSolid Pro, configurar endpoint
```

Agentes aduanales en CR (referencia): Grupo Logístico Aduanero (`logisticaaduanera.cr`), costo ~$80–$120/trámite.

**Costeo confirmado — Jimi IoT AL600 (2026-09-10, sobre 100 u. — su MOQ estándar):**

> ⚠️ Estimación conservadora: Jimi IoT solo cotizó el **precio de muestra** ($32/u).
> El precio en volumen a 100 u. aún no está confirmado — usar $32/u como techo
> conservador hasta recibir la cotización de lote real (pendiente en el correo de
> seguimiento del 2026-09-10, ver `docs/jimiiot.md` §8). Flete DHL para 100 u. y el
> costo de renovación de datos año 2+ tampoco están confirmados — se estiman
> por defecto de mercado, marcados abajo.

**Desglose de aduanas Costa Rica (partida 8526.91.00.00):**

| Concepto                                                                                               | Costo USD (año 1)                                           |
| ------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------- |
| FOB unidad + 1 año de datos (30MB/mes) — precio muestra                                                | $32.00 (bulk 100u aún sin cotizar)                          |
| Flete DHL (100 u.) — _estimado, no cotizado_                                                           | $3.50/u (estimado)                                          |
| Seguro de carga (~1% FOB+flete)                                                                        | $0.36/u                                                     |
| **CIF (base aduanera)**                                                                                | **$35.86/u**                                                |
| DAI — Derecho Arancelario de Importación (15% conservador, sin Certificado de Origen del TLC CR-China) | $5.38/u                                                     |
| Ley 6946 — Timbre de Educación y Cultura (1% CIF)                                                      | $0.36/u                                                     |
| IVA (13% sobre CIF + DAI + Ley 6946)                                                                   | $5.41/u                                                     |
| **Total impuestos**                                                                                    | **$11.15/u**                                                |
| Agente aduanal (obligatorio >$1,000 CIF, prorateado ~$100/trámite ÷ 100 u.)                            | $1.00/u                                                     |
| **Total landed CR (hardware + impuestos + agente)**                                                    | **~$48.01/u**                                               |
| Licencia de plataforma TrackSolid Pro (año 1 — servicio, no aduanable, no forma parte del CIF)         | $3.50/u                                                     |
| **TOTAL COSTO UNITARIO AÑO 1 (conservador)**                                                           | **~$51.50/u**                                               |
| Licencia de plataforma (renovación año 2+)                                                             | $6.00/u/año — **no incluye renovación de datos, monto TBD** |

> 🇨🇷 **Nota de aduanas:** Costa Rica tiene un TLC vigente con China desde 2011. Si
> Jimi IoT emite un **Certificado de Origen** válido bajo ese TLC, el DAI podría
> bajar a 0%, reduciendo el costo total unitario a **~$45.40/u** (ahorro de ~$6/u).
> Esto está pendiente de gestionar con Jimi IoT — agregarlo a la lista de
> preguntas del correo de seguimiento (`docs/jimiiot.md` §8). Hasta confirmarlo,
> se usa el escenario conservador (sin certificado, DAI 15%) para no sobre-prometer
> margen. Verificar también con un agente aduanal la tarifa DAI vigente exacta
> para la partida 8526.91.00.00, ya que puede variar por actualización arancelaria.

**Decisión (2026-09-10): se sube el precio de venta al público a ₡35,000 (~$67)**
para garantizar un margen neto mínimo de **$15/u** incluso en el escenario más caro
y más realista (Jimi IoT AL600, $51.50/u landed + licencia, con el desglose completo
de aduanas de Costa Rica). Esto reemplaza el precio anterior de ₡31,000 (que solo
cubría un estimado simplificado de impuestos al 15% plano, sin DAI, Ley 6946 ni
seguro desglosados).

**Decisión (2026-09-10, revisión): la renovación de licencia año 2+ ($6.00/u/año) NO
se mezcla en el recurrente mensual.** En su lugar, todo paquete que incluya un collar
físico (Jimi IoT o Hardware PawTrack) cobra una **renovación anual de
activación del collar de $8 USD/u/año** (~₡4,200), separada de la suscripción Plus/
Familia. Para Jimi IoT esto cubre el $6.00/u/año de licencia con ~$2/año de buffer.
Ver §10 para las tablas actualizadas.

---

### 3.3.1 TrackSolid Pro / Jimi Life — Análisis de viabilidad (2026-09-04)

Jimi IoT opera dos productos con propósitos opuestos — solo uno es viable como ruta de integración:

| Producto           | Qué es                                                                                                                                                                     | ¿Viable para PawTrack?                                                                                                             |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| **TrackSolid Pro** | Plataforma SaaS B2B con **Open API documentada** (`tracksolidprodocs.jimicloud.com`)                                                                                       | ✅ Sí — ver detalle abajo                                                                                                          |
| **Jimi Life**      | App de consumidor final (iOS/Android, `com.jimi.life`) — el equivalente de "la app de Tractive" para dispositivos Jimi IoT (dashcam, smart tag, mascotas, adultos mayores) | ❌ No — no tiene API propia; usarla obligaría al dueño a usar dos apps (Jimi Life + PawTrack), sin acceso programático a los datos |

**Hallazgos clave de TrackSolid Pro (confirmados contra la documentación oficial):**

- El modelo de dispositivo tiene un campo `mcTypeUseScope` que acepta explícitamente el valor **`"pet"`** — Jimi IoT ya modela collares de mascota como categoría de primera clase en su plataforma, no un caso límite.
- **Camino A (polling)** soportado nativamente: `jimi.device.location.get` / `jimi.user.device.location.list`, hasta 100 IMEIs por llamada — mismo patrón que `TractivePollingJob` ya en producción.
- **Camino B (push directo) soportado nativamente**, sin necesitar firmware OEM custom: sección "Webhook Push Function" de su API expone `/location/push` (posición GPS), `/api/v1/tag/data/push` (dispositivos tipo Tag) y push de alarmas/estado — todos configurables hacia una URL propia (la nuestra). **Esto responde la pregunta que dejamos abierta en `jimiiot.md` §8** sobre si Jimi IoT podía soportar push directo a nuestro backend.
- Autenticación: token tipo OAuth (usuario/password + `appKey`/`appSecret`, firma MD5, token válido ~2h) — mismo nivel de complejidad que la integración Tractive ya implementada.
- Nodos regionales: US/EU/HK-SG — el nodo US es el candidato natural por latencia desde Costa Rica.
- Alternativa de middleware (no evaluada a fondo): `flespi.com/manufacturers/jimi-iot` normaliza el protocolo nativo de Jimi IoT a JSON/MQTT sin pasar por TrackSolid — opción de respaldo si TrackSolid Pro no cubre algún caso.

**Recomendación:** usar TrackSolid Pro (no Jimi Life) como ruta de integración con hardware Jimi IoT existente, en paralelo a la conversación OEM/ODM de largo plazo ya en curso (`jimiiot.md` §8). Empezar con Camino A (clonar `TractivePollingJob`, ~1–2 días de esfuerzo); evaluar Camino B una vez resuelto cómo verificar el origen del webhook (Jimi IoT no documenta firma de webhook — mitigar con allowlist de IP o un token acordado en el onboarding).

### 3.3.2 Respuesta estructurada de Jimi IoT confirmada (2026-09-10)

Jimi IoT respondió punto por punto al RFQ + al correo de seguimiento sobre TrackSolid Pro. Confirma lo investigado de forma independiente y añade datos concretos de producto. Detalle completo de la respuesta en `docs/jimiiot.md` §8.

**Confirmaciones clave:**

- **JimiLife (su app de consumidor) no soporta APIs — TrackSolid Pro sí.** Confirma exactamente el hallazgo de la §3.3.1: JimiLife no es ruta de integración, TrackSolid Pro es la única viable de sus plataformas existentes.
- **El modelo recomendado para el piloto (AL600) NO soporta MQTT ni HTTPS API en firmware estándar.** Sí permite configurar un servidor propio (IP/Dominio + Puerto), pero eso solo redirige su protocolo binario propietario — para hablar JSON/HTTPS o MQTT directo con nuestro backend (Camino B "puro") se necesita desarrollo de firmware custom, no viene de fábrica. **Esto ajusta la recomendación: para el piloto con AL600 estándar, la única ruta realista de corto plazo es TrackSolid Pro (Camino A/B vía su API), no un push binario directo a `POST /api/collars/ingest`.**
- Mencionan una plataforma adicional, **TurboHive**, que "facilita la integración con la plataforma del cliente" — no evaluada aún, pendiente de más detalle.
- SDK propio disponible (costo adicional) si en el futuro se quiere una app 100% propia en vez de JimiLife/TrackSolid.

**Datos de producto — AL600 (modelo recomendado para el piloto de 50 u.):**

| Ítem                             | Dato                                                                                                                                                       |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Conectividad                     | LTE Cat.1 (**sin LTE-M/NB-IoT**), SIM embebida (eSIM)                                                                                                      |
| Posicionamiento                  | GPS + BDS, Wi-Fi, LBS, Bluetooth, A-GPS (híbrido)                                                                                                          |
| Batería                          | USB-C · hasta 10 días en seguimiento activo, hasta 60 días en standby                                                                                      |
| Certificaciones                  | IP67, 0–45 °C operación, FCC/CE según región                                                                                                               |
| Historial / geocercas            | Historial de ubicaciones 7 días, triple geocerca con alertas, búsqueda sonoro-lumínica                                                                     |
| **MOQ producto estándar**        | **100 unidades** (más alto que las 50 u. planeadas para el piloto — a negociar o ajustar)                                                                  |
| MOQ personalización básica       | 500 u. (empaque, grabado láser, etiqueta)                                                                                                                  |
| MOQ serigrafía en carcasa        | ≥1,000 u.                                                                                                                                                  |
| MOQ cambio de color              | ≥3,000 u.                                                                                                                                                  |
| Costo muestreo de carcasa        | $200 USD/modelo                                                                                                                                            |
| Precio muestra                   | $32 USD c/u — incluye 1 año de datos (30 MB/mes), **no incluye licencia de plataforma**                                                                    |
| **Licencia plataforma JimiLife** | $3.50 USD/dispositivo primer año, $6.00 USD/dispositivo renovación anual — **pendiente confirmar si TrackSolid Pro tiene el mismo esquema o uno distinto** |
| Lead time (stock)                | 3 días                                                                                                                                                     |
| Lead time (sin stock)            | 30–45 días                                                                                                                                                 |
| Lead time custom (muestra)       | ~10 días tras confirmar requisitos/documentación                                                                                                           |
| Lead time custom (producción)    | ~45 días tras aprobación de muestra                                                                                                                        |

**OEM confirmado:** logo, empaque, manuales, nombre de dispositivo, QR, formato de seriales — todo disponible de forma estándar. Personalización de firmware requiere evaluación previa de su equipo de ingeniería (no es automático ni gratis).

### 3.3.3 Datasheet oficial AL600 — specs confirmadas (adjuntos del correo, 2026-09-10)

Jimi IoT envió dos PDFs junto con su respuesta: un deck de producto (marketing,
8 páginas) y un datasheet técnico de una página ("Standard Configuration"),
este último ya con **marca blanca de ejemplo** ("PawBasis") — buena señal de
que el proceso de white-label que necesitamos ya es rutina para ellos. El
datasheet precisa/corrige algunas cifras que veníamos usando de su correo:

| Ítem                      | Dato del datasheet oficial                                                                                     |
| ------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Posicionamiento           | GPS/BDS/Wi-Fi/LBS/BT/A-GPS                                                                                     |
| Comunicación              | 4G LTE Cat 1                                                                                                   |
| **Bandas LTE soportadas** | **B1/B3/B5/B7/B8/B20/B28/B38/B40/B41**                                                                         |
| IP Rating                 | IP67                                                                                                           |
| Batería                   | 530 mAh recargable                                                                                             |
| Duración de batería       | 10+ días (seguimiento activo); 60+ días (standby/sleep)                                                        |
| Carga                     | USB Type-C                                                                                                     |
| Dimensiones               | 51 × 32 × 14 mm                                                                                                |
| **Temperatura operativa** | **-20 °C a +60 °C** (más amplio que los 0–45 °C mencionados en el correo — el datasheet es la fuente correcta) |

**⚠️ Hallazgo crítico a validar:** las bandas LTE listadas (B1/B3/B5/B7/B8/B20/B28/B38/B40/B41)
**no incluyen B2 ni B4** — las bandas primarias históricas de operadores
latinoamericanos (PCS 1900 / AWS 1700-2100). Sí incluye **B28** (700 MHz) y
**B7** (2600 MHz), que Kölbi/Movistar/Claro también usan para LTE en Costa
Rica, así que es probable que funcione, pero **hay que confirmarlo
explícitamente antes de comprometer el piloto** — no asumir compatibilidad
solo por ser "4G LTE". Este es el ítem más importante de los pendientes de la
§3.3.2, más concreto ahora que tenemos la lista exacta de bandas para
preguntarle directamente a cada operador o a Jimi IoT.

**ODM (roadmap de hardware propio, largo plazo):** Jimi IoT opera un modelo ODM integral (diseño + hardware + firmware + certificaciones + manufactura propia, 300+ ingenieros I+D, 8M dispositivos/año). **Ya tienen una línea propia de "smart pet collar" wearable con IA de cuidados** — precedente directo relevante para nuestro roadmap de hardware propio. MOQ típico ODM desde 1,000 u. (desarrollos sobre plataforma existente permiten umbrales menores). Proceso: requisitos conjuntos → propuesta de diseño → prototipo/validación → refinamiento → preparación de producción/QC → producción en serie → soporte continuo. **Ofrecen firmar NDA** para compartir documentación técnica detallada del protocolo y requisitos de plataforma — paso pendiente antes de profundizar en integración de bajo nivel.

**Decisiones pendientes:**

- [ ] Negociar MOQ 100 → 50 para el piloto, o ajustar el piloto a 100 unidades.
- [ ] Confirmar si el costo de licencia de plataforma ($3.50–$6.00/dispositivo/año) aplica igual bajo TrackSolid Pro o es exclusivo de JimiLife.
- [ ] Evaluar si firmar el NDA de Jimi IoT para acceder a documentación de protocolo más profunda (relevante solo si se explora firmware custom o ODM, no bloquea el piloto vía TrackSolid Pro).
- [ ] LTE Cat.1 (no NB-IoT/LTE-M) — validar cobertura/costo real con Kölbi/Movistar/Claro antes de comprometer el piloto.

---

### 3.4 Hardware propio PawTrack (roadmap futuro)

`CollarProvider.Own = 0` ya se usa durante la activación de un CollarTag. La plataforma actual genera una clave única por dispositivo (solo se persiste su hash), la acepta mediante `X-Collar-Key` y autoriza la publicación HTTP de ubicación en `POST /api/collars/pet/{petId}/location`. Esto permite prototipos y equipos OEM compatibles, pero no sustituye una plataforma de flota IoT.

#### Arquitectura de producción objetivo

```text
Collar GPS + firmware
  -> MQTT sobre TLS
  -> Azure Device Provisioning Service (DPS)
  -> Azure IoT Hub
  -> procesador de telemetría (Azure Function o Container App)
  -> comandos y servicios de Application de PawTrack
  -> Azure SQL: estado actual + historial de ubicaciones

PawTrack API -> mensajes cloud-to-device / device twin -> collar
```

| Capa                     | Responsabilidad de producción                                                                                                         | Estado                |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------- | --------------------- |
| Collar y firmware        | GPS, LTE-M/NB-IoT/4G, buffer offline, reintentos con backoff, timestamp UTC, contador secuencial, batería y versión de firmware       | Pendiente             |
| Identidad de dispositivo | Serial, IMEI/eSIM, lote e identidad criptográfica única creados en fábrica; DPS asigna el dispositivo al IoT Hub del ambiente         | Pendiente             |
| Transporte               | MQTT sobre TLS hacia IoT Hub; certificados X.509 o claves simétricas rotables por dispositivo                                         | Pendiente             |
| Ingesta                  | Validar esquema, deduplicar por contador, rechazar posiciones imposibles y persistir mediante el módulo `Collars`                     | Pendiente             |
| Estado y alertas         | Actualizar última posición/batería y disparar evaluaciones de zona segura, conectividad y modo perdido ya implementadas               | Integración pendiente |
| Comandos                 | Frecuencia GPS, solicitud de posición, modo perdido y configuración mediante cloud-to-device y device twin, con acuse por `commandId` | Pendiente             |
| OTA                      | Paquetes firmados, despliegue gradual, rollback y reporte de versión                                                                  | Pendiente             |
| Observabilidad           | Métricas de conexión, retraso de ingesta, batería, comandos fallidos y dispositivos silenciosos en Azure Monitor/Application Insights | Pendiente             |

**Límites de seguridad:** la app móvil nunca se conecta directamente al IoT Hub; consulta el API PawTrack. Cada mensaje debe incluir identidad del dispositivo, instante UTC y contador monotónico. El procesador valida el contrato y opera mediante comandos o servicios del módulo `Collars`, sin escribir tablas ajenas directamente.

**Orden recomendado de entrega:** primero un piloto MQTT con DPS, IoT Hub, telemetría y alerta de desconexión; después comandos bidireccionales; finalmente OTA y operación de flota. Mantener la ingesta HTTP actual como herramienta de prototipo, diagnóstico o integración OEM que no soporte MQTT.

#### Referencia de prototipo físico

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

### Camino B — Push directo HTTP (implementado para prototipos y OEM)

El collar (o gateway del proveedor) hace `POST /api/collars/pet/{petId}/location` directamente.

**Implementación actual:**

1. `CollarDeviceCredentials` persiste `CollarId`, hash SHA-256, creación, revocación y último uso; la clave en texto se muestra una sola vez.
2. `CollarDeviceKeyMiddleware` valida el header `X-Collar-Key` y agrega el claim `CollarId` a la identidad del dispositivo.
3. El endpoint exige que el claim corresponda al collar activo de la mascota; un propietario autenticado también puede registrar una ubicación manualmente. Otro usuario recibe `403`.

**Límite:** HTTP autenticado es suficiente para pruebas, diagnósticos y ciertos OEM, pero MQTT/TLS con DPS e IoT Hub es el canal previsto para una flota de hardware propio en producción.

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

| Módulo                    | Por qué                                                        |
| ------------------------- | -------------------------------------------------------------- |
| **Jimi IoT AL600**        | API TrackSolid Pro documentada, MOQ 100 u., collar base actual |
| **SIM7080G (PCB propio)** | Para firmware totalmente propio con JLCPCB                     |

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

- [ ] Contactar Jimi IoT para muestras AL600 con acceso TrackSolid Pro
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

| Opción                       | Inversión inicial      | Costo/unidad                   | Precio venta sugerido | Margen neto          | Tiempo al mercado |
| ---------------------------- | ---------------------- | ------------------------------ | --------------------- | -------------------- | ----------------- |
| **A — Afiliado Tractive**    | $0                     | N/A                            | $79 USD (Amazon)      | $20 USD fijo/tracker | **Inmediato**     |
| **B — Hardware propio**      | $3,000+ USD            | ~$30 USD                       | $60–80 USD            | $30–50 USD           | 3–4 meses         |
| **C — OEM Jimi IoT (AL600)** | ~$5,150 USD (100 u.)\* | ~$51.50 USD landed CR + lic.\* | ₡35,000 (~$67)        | ~$15 USD\*           | 2–3 meses         |

> \* Costo recalculado con desglose completo de aduanas CR (DAI 15% conservador,
> Ley 6946, IVA, seguro, agente aduanal — ver `docs/collarFinal.md` §3.3 "Costeo
> confirmado — Jimi IoT AL600"). Jimi IoT solo cotizó precio de muestra, no de lote
> a 100 u. **El precio de venta al público se fija en ₡35,000 (~$67) para
> garantizar un margen mínimo de $15/u.** Si Jimi IoT confirma Certificado de
> Origen del TLC CR-China (DAI 0%), el costo baja a ~$45.40/u y el margen sube a
> ~$21/u al mismo precio de venta.

**Recomendación:** el collar base y única opción actual de PawTrack es el **Jimi IoT AL600**, con margen garantizado ~$15 USD/u (conservador) a ~$21 USD/u (si se confirma Certificado de Origen del TLC CR-China) al precio de ₡35,000. Arrancar con Opción A (cero riesgo) y pivotar a Jimi IoT AL600 cuando haya >100 suscriptores Plus que justifiquen el MOQ de 100 unidades.

### Comparativa de productos disponibles en CR

| Opción                           | Precio inicial  | Total/mes | Renovación anual collar\* | QR                | GPS        | Cuentas requeridas                  |
| -------------------------------- | --------------- | --------- | ------------------------- | ----------------- | ---------- | ----------------------------------- |
| 🏷️ Placa QR + Explorador         | ₡1,500–4,500    | ₡0        | —                         | ✅                | ❌         | Solo PawTrack                       |
| 🏷️ Placa QR + Plus               | ₡1,500–4,500    | ₡2,990    | —                         | ✅                | ❌         | Solo PawTrack                       |
| 📡 OEM Jimi IoT AL600 + QR láser | ₡37,000         | ~₡2,990   | ₡4,200 (~$8)              | ✅ grabado        | ✅ Básico  | PawTrack + cuenta TrackSolid Pro    |
| ⭐ Tractive DOG 6                | ₡41,000 + placa | ~₡8,190   | —                         | ⚠️ pieza separada | ✅ Premium | PawTrack + **Tractive obligatorio** |
| 🔧 Hardware PawTrack             | ₡35,000–50,000  | ~₡4,030   | ₡4,200 (~$8)              | ✅ integrado      | ✅ Custom  | PawTrack + SIM gestionada           |

> \* Renovación anual de activación del collar (nueva, 2026-09-10) — separada de
> la suscripción Plus/Familia, cobrada una vez al año a cualquier paquete con
> collar físico. Cubre el costo confirmado de licencia de plataforma de Jimi IoT
> ($6.00/u/año) con ~$2/año de buffer; en Hardware PawTrack (sin licencia
> recurrente de plataforma) es margen adicional puro. La renovación de datos año
> 2+ de Jimi IoT sigue sin cotizar ("monto TBD", ver §3.3); si resulta mayor a lo
> estimado, este fee anual podría ajustarse. El costo de importación real de Jimi
> IoT ya incluye el desglose completo de aduanas CR (DAI, Ley 6946, IVA, seguro,
> agente aduanal — ver §3.3).
>
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
- El AL600 de Jimi IoT ya trae el dato incluido en el precio del collar (sin SIM gestionada aparte) — solo requiere cuenta TrackSolid Pro. Una futura línea de hardware 100% propio (§7, PCB ESP32-S3 + SIM7080G) sí necesitaría SIM IoT gestionada por PawTrack (Emnify/Hologram).

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
