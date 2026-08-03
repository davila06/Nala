# PawTrack Collar GPS — Guía Técnica y de Adquisición

> Documento de referencia para el equipo de PawTrack CR.
> Aplica a la integración de collares GPS de terceros (Tractive, Kippy) y al hardware propio futuro.

---

## 1. Estado actual de implementación

| Capa                      | Archivo                                                 | Estado                         |
| ------------------------- | ------------------------------------------------------- | ------------------------------ |
| Dominio                   | `Collar.cs`, `CollarLocation.cs`, `CollarProvider.cs`   | ✅ Completo                    |
| Repositorio               | `ICollarRepository`, `CollarRepository.cs`              | ✅ Completo                    |
| Comandos                  | `RegisterCollarCommand` (requiere plan Plus)            | ✅ Completo                    |
| Queries                   | `GetCollarStatusQuery`, `GetLocationHistoryQuery`       | ✅ Completo                    |
| Integración Tractive      | `TractiveService.cs` (OAuth2 + location API)            | ✅ Completo                    |
| Polling job               | `TractivePollingJob.cs` (BackgroundService, cada 5 min) | ✅ Completo                    |
| Purge de historial        | `CollarLocationPurgeJob.cs` (datos >30 días)            | ✅ Completo                    |
| Controlador REST          | `CollarsController`                                     | ✅ Completo                    |
| Frontend                  | `CollarGpsTab.tsx`, `collarApi.ts`, `useCollar.ts`      | ✅ Completo                    |
| OAuth callback            | `GET /api/collars/tractive/callback`                    | ✅ Completo                    |
| **Kippy**                 | `KippyService.cs`                                       | ❌ Pendiente                   |
| **PawTrack Own hardware** | —                                                       | ❌ Pendiente (hardware futuro) |

---

## 2. Proveedores soportados

### 2.1 Tractive (implementado ✅)

**¿Qué es?**
Tractive es la marca líder mundial de rastreadores GPS para mascotas. Usada por más de 10 millones de mascotas. Disponible en Costa Rica vía Amazon y tiendas oficiales.

**Cómo funciona la integración:**

- Flujo OAuth2 Authorization Code
- El dueño conecta su cuenta Tractive desde la tab GPS de PawTrack
- PawTrack obtiene un token cifrado (AES-256-CBC con clave en Key Vault)
- `TractivePollingJob` consulta la API cada 5 minutos y guarda la posición en `CollarLocations`
- La tab GPS muestra el último punto + trayectoria de hasta 7 días

**Endpoints de la API Tractive usados:**
| Endpoint | Propósito |
|---|---|
| `GET /3/tracker/{id}/positions/recent` | Última posición |
| `POST /api/1/user/oauth/token` | Intercambio de authorization code por token |
| `GET /3/tracker/{id}` | Estado del dispositivo (batería, etc.) |

**Variables de configuración requeridas (Key Vault):**

```
Tractive:ClientId       → Client ID de tu app en developers.tractive.com
Tractive:ClientSecret   → Client Secret
Tractive:EncryptKey     → 32 bytes en base64 para cifrar el token OAuth (AES-256)
```

**URL de callback OAuth:** `{App:BaseUrl}/api/collars/tractive/callback`

---

### 2.2 Kippy (pendiente ❌)

Kippy es un rastreador GPS+salud con funciones de actividad. Popular en España/LatAm.

**API:** REST en `https://api.kippy.eu/v1/`
**Autenticación:** API Key por usuario (no OAuth)

**Para implementar:**

1. Crear `KippyService : ICollarService` similar a `TractiveService`
2. Registrar `HttpClient("Kippy")` en `InfrastructureServiceCollectionExtensions`
3. Extender `TractivePollingJob` o crear `KippyPollingJob` para el mismo patrón
4. Variable de config: `Kippy:ApiKey` por usuario (guardada cifrada como `ExternalTokenEncrypted`)

---

### 2.3 Hardware propio PawTrack (futuro)

**Concepto:** collar IoT con módulo LTE-M/NB-IoT o SIM nativa, publicando a MQTT → Azure IoT Hub → PawTrack.

**Arquitectura recomendada:**

```
Collar (ESP32 + SIM7080) → MQTT → Azure IoT Hub → Azure Function → POST /api/collars/ingest
```

**Variable `CollarProvider.Own = 0`** ya reservada en el dominio.

---

## 3. Modelo de datos

```
Collar
├── Id (Guid v7)
├── PetId → Pets.Id
├── OwnerId → Users.Id
├── Provider (0=Own, 1=Tractive, 2=Kippy, 99=Generic)
├── ExternalDeviceId (string, nullable) → ID del tracker en la plataforma externa
├── ExternalTokenEncrypted (string, nullable) → token OAuth cifrado con AES-256
├── BatteryPercent (int?, 0-100)
├── LastLat / LastLng (double?)
├── LastSeenAt (DateTimeOffset?)
├── IsActive (bool)
└── RegisteredAt

CollarLocation (write-heavy, purge >30 días)
├── Id (Guid v7)
├── CollarId → Collars.Id
├── Lat / Lng
├── RecordedAt
└── INDEX (CollarId, RecordedAt DESC)
```

---

## 4. Flujo del usuario (con Tractive)

```
1. Dueño abre PetDetailPage → tab GPS 📡
2. Click "Conectar Tractive"
3. Frontend llama GET /api/collars/tractive/auth-url?petId=...
4. Backend retorna URL de Tractive OAuth + state JWT (incluye petId)
5. Dueño autoriza en tractive.com
6. Tractive redirige a /api/collars/tractive/callback?code=...&state=...
7. Backend valida state, intercambia code → token, cifra, crea Collar en DB
8. Frontend recarga la tab GPS — ahora muestra el mapa con la posición actual
9. Cada 5 minutos TractivePollingJob actualiza la posición
```

---

## 5. Cómo conseguir los collares para vender/distribuir

### Opción A — Reventa Tractive (más simple, sin hardware propio)

**Programa de afiliados Tractive:**

- URL: https://go.tractive.com/affiliate/
- Comisión: 15% por venta referenciada
- Implementación: añadir `?utm_source=pawtrack` al redirect de OAuth
- Precio retail: USD $49.99 (tracker) + USD $5/mes (plan básico) o USD $8/mes (premium)

**Para Costa Rica:**

- Los usuarios pueden comprar en Amazon.com (envío con Aerocasillas o similar)
- También disponible en Mr Lee (San José) y algunas tiendas PetSmart

**Modelo de negocio sugerido:**

- Bundle: "Collar GPS PawTrack Plus" = Tractive tracker + suscripción PawTrack Plus 1 año
- Precio sugerido: ₡45,000 (incluye tracker importado + envío + 1 año PawTrack)
- Margen estimado: ₡8,000–12,000 por unidad

---

### Opción B — Hardware propio (3-4 meses de desarrollo)

**BOM (Bill of Materials) estimada por unidad:**

| Componente           | Modelo sugerido                       | USD                   |
| -------------------- | ------------------------------------- | --------------------- |
| MCU + GPS            | SiMCOM A7680C (LTE-M + GPS integrado) | $12                   |
| Batería LiPo         | 3.7V 800mAh                           | $4                    |
| PCB + case           | Diseño custom, impresión 3D inicial   | $8                    |
| SIM Emnify/Hologram  | Conectividad IoT global               | $2/mes                |
| Manufactura (100u)   | ~$6/unit                              | $6                    |
| **Total por unidad** |                                       | **≈$30 + $2/mes SIM** |

**Stack tecnológico para hardware propio:**

```
MCU: ESP32-S3 + A7680C (GPS + LTE-M)
Firmware: ESP-IDF o Arduino framework
Protocolo: MQTT over TLS → Azure IoT Hub
Frecuencia: reporta cada 30s cuando en movimiento, cada 5min cuando quieto
Geofencing: alerta configurable por radio (implementar en Azure Stream Analytics)
```

---

### Opción C — OEM chino (solución rápida a mediano plazo)

**Proveedor recomendado:** Shenzhen Concox Information Technology

- Modelo: AT4 (collar GPS + WiFi + LTE)
- MOQ: 50 unidades
- Precio: USD $18/unidad (FCA Shenzhen)
- API: REST propietaria con clave de API por dispositivo → fácil de integrar como `CollarProvider.Generic`
- Tiempo de entrega a CR: 25-35 días (DHL Express)

**Contacto:** sales@concox.com
**Certificaciones:** FCC, CE, ROHS (necesario para venta en CR)
**SINPE importación:** Tramitar ante Hacienda con código arancelario 8526.91.00

---

## 6. Implementar Kippy (estimado: 1-2 días)

```csharp
// 1. Crear PawTrack.Infrastructure/Collars/KippyService.cs
public sealed class KippyService(IHttpClientFactory factory, IConfiguration config) : ICollarService
{
    private const string ApiBase = "https://api.kippy.eu/v1";

    public async Task<CollarPosition?> GetLatestPositionAsync(string encryptedApiKey, string deviceId, CancellationToken ct)
    {
        var apiKey = Decrypt(encryptedApiKey); // reusar TractiveService.Decrypt
        var client = factory.CreateClient("Kippy");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await client.GetFromJsonAsync<KippyPositionResponse>(
            $"{ApiBase}/pet/{deviceId}/location", ct);

        return response is null ? null : new CollarPosition(response.Lat, response.Lng, response.Battery);
    }
}

// 2. Registrar en InfrastructureServiceCollectionExtensions.cs
services.AddHttpClient("Kippy").ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });

// 3. Extender TractivePollingJob para incluir Kippy
// ── En PollAllActiveCollarsAsync, añadir:
var kippyCollars = await db.Collars
    .Where(c => c.IsActive && c.Provider == CollarProvider.Kippy && ...)
    .ToListAsync(ct);
// ... misma lógica que Tractive
```

---

## 7. Variables de entorno de producción (Azure Key Vault)

```bash
# Tractive OAuth
Tractive__ClientId=<app client id de developers.tractive.com>
Tractive__ClientSecret=<secret>
Tractive__EncryptKey=<32 bytes en base64 generados con: openssl rand -base64 32>

# Kippy (cuando se implemente)
Collar__KippyEnabled=true

# Hardware propio (cuando se implemente)
Azure__IoTHubConnectionString=<connection string del Azure IoT Hub>
```

---

## 8. Checklist para activar en producción

- [ ] Crear app OAuth en [developers.tractive.com](https://developers.tractive.com)
  - Redirect URI: `https://pawtrack.cr/api/collars/tractive/callback`
  - Scopes requeridos: `activity device_info`
- [ ] Configurar `Tractive:ClientId`, `Tractive:ClientSecret`, `Tractive:EncryptKey` en Key Vault
- [ ] Verificar que `TractivePollingJob` está activo en el Container App (revisar logs al inicio)
- [ ] Configurar `App:BaseUrl=https://pawtrack.cr` en producción
- [ ] Probar flujo completo con un Tractive físico en staging

---

## 9. Precio de suscripción y posicionamiento

| Plan                 | Collar GPS                   | Historial |
| -------------------- | ---------------------------- | --------- |
| Explorador (gratis)  | ❌ (tab visible, CTA a Plus) | —         |
| Plus (₡2,990/mes)    | ✅ Tractive, Kippy, Generic  | 7 días    |
| Familia (₡4,990/mes) | ✅ Todos los providers       | 7 días    |

El collar GPS es **el diferenciador de conversión más fuerte** del plan Plus — muestra valor inmediato y recurrente para el dueño.

---

_PawTrack CR · Documento técnico collares GPS · Agosto 2026_
