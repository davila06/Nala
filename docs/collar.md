# PawTrack Collar GPS — Guía Técnica y de Adquisición

> Documento de referencia para el equipo de PawTrack CR.  
> Última actualización: 2026-08-19  
> Aplica a la integración de collares GPS de terceros (Tractive, Kippy) y al hardware propio futuro.

---

## 1. Estado actual de implementación

| Capa                      | Archivo                                                        | Estado                         |
| ------------------------- | -------------------------------------------------------------- | ------------------------------ |
| Dominio                   | `Collar.cs`, `CollarLocation.cs`, `CollarProvider.cs`          | ✅ Completo                    |
| Repositorio               | `ICollarRepository`, `CollarRepository.cs`                     | ✅ Completo                    |
| Comandos                  | `RegisterCollarCommand` (requiere plan Plus + ownership check) | ✅ Completo                    |
| Queries                   | `GetCollarStatusQuery`, `GetLocationHistoryQuery`              | ✅ Completo                    |
| Seguridad BOLA            | Ownership check en GetCollarStatus y GetLocationHistory        | ✅ Completo                    |
| Integración Tractive      | `TractiveService.cs` (OAuth2 + location API)                   | ✅ Completo                    |
| Polling job               | `TractivePollingJob.cs` (BackgroundService, cada 5 min)        | ✅ Completo                    |
| Purge de historial        | `CollarLocationPurgeJob.cs` (datos >30 días)                   | ✅ Completo                    |
| Rate limiting             | `[EnableRateLimiting("public-api")]` en todos los endpoints    | ✅ Completo                    |
| Controlador REST          | `CollarsController`                                            | ✅ Completo                    |
| Frontend                  | `CollarGpsTab.tsx`, `collarApi.ts`, `useCollar.ts`             | ✅ Completo                    |
| OAuth callback            | `GET /api/collars/tractive/callback`                           | ✅ Completo                    |
| **Kippy**                 | `KippyService.cs`                                              | ❌ Pendiente                   |
| **PawTrack Own hardware** | —                                                              | ❌ Pendiente (hardware futuro) |

### Nota de seguridad importante

`GetCollarStatusQuery` y `GetLocationHistoryQuery` verifican ownership del pet antes de retornar datos. Cualquier usuario autenticado que no sea el dueño de la mascota recibe `Access denied.` (HTTP 403). Esto previene BOLA (Broken Object Level Authorization) donde un atacante con el `petId` podría ver la ubicación GPS histórica de una mascota ajena.

---

## 2. Proveedores soportados

### 2.1 Tractive (implementado ✅)

**¿Qué es Tractive?**
Tractive es la marca líder mundial de rastreadores GPS para mascotas. Usada por más de 10 millones de mascotas. Disponible en Costa Rica vía Amazon y tiendas oficiales.

**Precios actuales (Agosto 2026):**

| Producto                      | Precio USD             | Precio CRC aprox. | Dónde comprar                       |
| ----------------------------- | ---------------------- | ----------------- | ----------------------------------- |
| Tractive DOG 6 (tracker)      | **$79.00**             | ₡41,000           | Amazon.com + Aerocasillas           |
| Tractive CAT 6 Mini (tracker) | ~$79.00                | ₡41,000           | Amazon.com + Aerocasillas           |
| Plan Tractive 1 año           | $10/mes ($120/año)     | ₡62,400/año       | Tractive.com (usuario paga directo) |
| Plan Tractive 2 años          | $7/mes ($168 c/2 años) | ₡87,360 c/2 años  | Tractive.com                        |
| Plan Tractive 5 años          | $5/mes ($300 c/5 años) | ₡156,000 c/5 años | Tractive.com                        |

> ⚠️ El plan de Tractive lo paga el usuario **directamente a Tractive**. PawTrack no intermedia ese cobro.

**Programa de afiliados — el único canal comercial que ofrece Tractive:**

Tractive **no tiene programa de reventa ni descuentos por volumen**. Solo existe el programa de afiliados:

| Dato           | Valor                                                                     |
| -------------- | ------------------------------------------------------------------------- |
| Comisión       | **$20 USD fijo por tracker** (cualquier modelo)                           |
| Cookie         | 30 días                                                                   |
| Pago           | 30 días después del cierre del mes                                        |
| Plataforma     | [Impact.com](https://app.impact.com/campaign-promo-signup/Tractive.brand) |
| Aprobación     | Solicitud → revisión en pocos días hábiles                                |
| Tracker gratis | Posible pedirlo, caso por caso, sin garantía                              |

> Tractive rechaza: sitios de cupones, cashback, subnetworks de afiliados y quienes pujen en sus keywords de marca.

**Nuestros bundles propuestos:**

| Bundle                | Qué incluye                                  | Precio sugerido (CRC) | Nuestro ingreso            |
| --------------------- | -------------------------------------------- | --------------------- | -------------------------- |
| Solo suscripción Plus | PawTrack Plus 1 mes                          | ₡2,990                | ₡2,990                     |
| Pack GPS Link         | PawTrack Plus 1 mes + link afiliado DOG 6    | ₡2,990 + afiliado     | ₡2,990 + ₡10,400 comisión  |
| Pack GPS Anual        | PawTrack Plus 12 meses + link afiliado DOG 6 | ₡35,880 + afiliado    | ₡35,880 + ₡10,400 comisión |

**Para activar el programa:**

1. Solicitar en [tractive.com/landing/affiliate](https://tractive.com/landing/affiliate)
2. Aplicar via Impact.com → esperar aprobación
3. Generar link único en el dashboard de Impact
4. El backend ya agrega `?utm_source=pawtrack` al redirect OAuth

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

### 2.2 Kippy (pendiente ❌ — no viable para CR)

**¿Qué es Kippy?**
Kippy es un rastreador GPS + salud para mascotas fabricado por **Datamars Digital Solutions SA** (Suiza/Italia). Popular en España y Europa. Incluye monitoreo de actividad, frecuencia cardíaca y zonas seguras.

**Especificaciones del hardware:**

| Característica      | Kippy DOG / CAT                                              |
| ------------------- | ------------------------------------------------------------ |
| Precio hardware     | €41.99 (~$46 USD)                                            |
| Suscripción         | Desde €3.33/mes                                              |
| Batería             | Hasta 12 días (condiciones óptimas)                          |
| Resistencia al agua | IP67 — sumergible 1m/30min                                   |
| Extras              | Sonido de llamada, LED parpadeante, monitoreo de salud       |
| Distribución        | Solo online en Europa (kippy.eu) — sin distribución en LatAm |

**Cobertura: solo Europa** ⚠️

El dispositivo usa una **SIM integrada** que solo conecta en estos países:

> Austria, Bélgica, Croacia, Dinamarca, España, Francia, Alemania, Grecia, Hungría, Irlanda, Italia, Países Bajos, Noruega, Polonia, Portugal, Rumanía, Serbia, Suecia, Suiza, Reino Unido + Sudáfrica y algunos territorios franceses de ultramar.

**Costa Rica no está en la lista. El tracker no funciona en CR.**

**Por qué está en el código de todos modos:**

El dominio tiene `CollarProvider.Kippy = 2` reservado y la integración está diseñada para usuarios que vengan a PawTrack desde España o Europa. Si PawTrack eventualmente se expande a España/LatAm con cobertura Kippy, la integración está lista para activar en ~1-2 días.

**API de Kippy:**

Kippy no tiene una API pública documentada. El endpoint `https://api.kippy.eu/v1/` referenciado en el código es una API interna que funciona con una API Key generada desde la app del usuario — no un programa oficial de integración. **Usar bajo riesgo**: Kippy puede cambiar o deprecar sin aviso.

**Veredicto para PawTrack CR:**

- ❌ No funciona en Costa Rica (cobertura solo Europa)
- ❌ Sin API pública documentada
- ❌ Sin distribución en CR
- ✅ Código listo para activar si se expande a España
- **Prioridad: ninguna** — no implementar hasta que haya usuarios en mercados de cobertura Kippy

**API y autenticación cuando aplique:**

```
Base URL: https://api.kippy.eu/v1/
Auth:     Bearer {apiKey}  ← generado desde cuenta Kippy del usuario
Endpoint clave: GET /pet/{deviceId}/location
```

---

### 2.3 Hardware propio PawTrack (futuro)

**Concepto:** collar IoT con módulo LTE-M/NB-IoT o SIM nativa, publicando a MQTT → Azure IoT Hub → PawTrack.

**Arquitectura recomendada:**

```
Collar (ESP32-S3 + SIM7080G) → MQTT/TLS → Azure IoT Hub → Azure Function → POST /api/collars/ingest
```

**Variable `CollarProvider.Own = 0`** ya reservada en el dominio.

#### Componentes recomendados

| Componente           | Modelo                                        | Dónde comprar            | Costo aprox |
| -------------------- | --------------------------------------------- | ------------------------ | ----------- |
| MCU                  | ESP32-S3 (dual-core, BLE)                     | DigiKey / Mouser         | $4          |
| Módulo celular + GPS | SIM7080G (LTE-M + GNSS integrado)             | SIMCOM directo / DigiKey | $12         |
| Acelerómetro         | ADXL345 (detección de movimiento)             | AliExpress               | $0.80       |
| Batería              | LiPo 3.7V 1000mAh (plana, 50×34×5mm)          | AliExpress               | $3.50       |
| PCB                  | JLCPCB (5 prototipos ~$2 + SMT assembly)      | jlcpcb.com               | $2–15       |
| Case                 | Impresión 3D TPU flexible (resistente a agua) | Local o Shapeways        | $5–15       |

#### Gestión de batería y firmware — el problema real

**Sin optimización:** ESP32 + SIM7080G activos consumen ~200–350 mA. Con LiPo 1000 mAh eso da **2–4 horas**. Inutilizable.

**Estrategia de sleep por capas (objetivo: 3–5 días):**

```
┌─────────────────────────────────────────────────────┐
│  ESTADO: Movimiento detectado                        │
│  • GPS hot fix cada 30 segundos                     │
│  • MQTT transmit + sleep corto (10s)                │
│  • Consumo: ~250 mA promedio en burst               │
└──────────────────────┬──────────────────────────────┘
                       │ Sin movimiento > 2 min
┌──────────────────────▼──────────────────────────────┐
│  ESTADO: Quieto (Light Sleep)                        │
│  • ADXL345 en modo interrupt (wake on motion)       │
│  • Timer wake cada 10 min → heartbeat MQTT          │
│  • GPS apagado, SIM en PSM (Power Saving Mode)      │
│  • Consumo: ~1–3 mA promedio                        │
└──────────────────────┬──────────────────────────────┘
                       │ Sin movimiento > 30 min
┌──────────────────────▼──────────────────────────────┐
│  ESTADO: Dormido (Deep Sleep ESP32)                  │
│  • ESP32 a 10–15 µA                                 │
│  • SIM7080G en PSM: ~0.4 mA (wake on demand)       │
│  • Wake por interrupción ADXL345 o timer 30 min     │
│  • Consumo: ~0.5–1 mA promedio                      │
└─────────────────────────────────────────────────────┘
```

**Matemáticas de batería (1000 mAh LiPo, mascota típica):**

| Escenario                        | % tiempo activo | Consumo promedio | Duración      |
| -------------------------------- | --------------- | ---------------- | ------------- |
| Sin sleep (malo)                 | 100%            | 280 mA           | ~3.5 horas    |
| Con Light Sleep solo             | 10% activo      | ~30 mA           | ~33 horas     |
| Con Deep Sleep (mascota en casa) | 2% activo       | ~5 mA            | **~8 días**   |
| Mascota activa (caminata 2h/día) | 15% activo      | ~43 mA           | **~23 horas** |

**La clave práctica:** el ADXL345 como interrupt source para el wake es lo que marca la diferencia. Sin acelerómetro, el timer forzado consume el 80% de la batería en wakups innecesarios.

**Código de referencia (pseudofirmware):**

```c
// Loop principal simplificado
void loop() {
    if (adxl345_motion_detected()) {
        gps_wakeup();
        sim_exit_psm();

        CollarPosition pos = gps_hot_fix(timeout_ms: 5000);
        mqtt_publish(pos);

        stationary_seconds = 0;
    } else {
        stationary_seconds += sleep_interval;

        if (stationary_seconds > 1800) {  // 30 min quieto
            esp32_deep_sleep(wake_after_seconds: 1800,
                             wake_on_interrupt: ADXL345_INT_PIN);
        } else if (stationary_seconds > 120) {  // 2 min quieto
            mqtt_publish_heartbeat(last_known_pos);
            sim_enter_psm();
            esp32_light_sleep(wake_after_seconds: 600,  // 10 min
                              wake_on_interrupt: ADXL345_INT_PIN);
        }
    }
}
```

**GPS cold fix vs hot fix — problema crítico al despertar:**

- Cold fix (primera vez o sin asistencia): 30–90 segundos, consume ~100 mA todo ese tiempo
- Hot fix (GNSS assistance, datos de satélites en caché): 3–8 segundos
- **Solución:** usar A-GPS (Assisted GPS) descargando datos de efemérides al SIM7080G via LTE antes de pedirle el fix. SIM7080G soporta esto nativamente con `AT+CGNSSINFO`

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

### Resumen ejecutivo de precios

| Opción                    | Inversión inicial   | Costo/unidad                | Precio venta sugerido        | Margen neto                  | Tiempo al mercado |
| ------------------------- | ------------------- | --------------------------- | ---------------------------- | ---------------------------- | ----------------- |
| **A — Afiliado Tractive** | **$0**              | N/A (no tenemos inventario) | $79 USD DOG 6 (Amazon)       | **$20 USD fijo** por tracker | **Inmediato**     |
| **B — Hardware propio**   | $3,000+ USD         | ~$30 USD                    | $60–80 USD                   | $30–50 USD                   | 3–4 meses         |
| **C — OEM Concox**        | ~$1,350 USD (50 u.) | ~$27 USD landed en CR       | $38–48 USD (~₡20,000–25,000) | $10–20 USD                   | 2–3 meses         |

**Recomendación:** arrancar con Opción A (cero riesgo, cero inventario) y pivotar a C cuando haya >100 suscriptores Plus que justifiquen el MOQ de 50 unidades.

---

### Opción A — Afiliado Tractive (más simple, sin hardware propio)

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

### Opción C — OEM China (solución rápida con marca propia)

**Proveedores OEM verificados con API REST documentada:**

| Proveedor     | Modelo               | MOQ    | Precio FCA Shenzhen | API                      | Cert.         |
| ------------- | -------------------- | ------ | ------------------- | ------------------------ | ------------- |
| **Concox**    | AT4 (GPS+WiFi+LTE)   | 50 u.  | ~$18                | REST propietaria         | FCC, CE, ROHS |
| **Jimi IoT**  | JM-VL01 / LL01       | 50 u.  | ~$15–22             | REST + MQTT              | FCC, CE       |
| **Queclink**  | GL300 (miniatura)    | 50 u.  | ~$18–20             | REST + protocolo binario | FCC, CE, ROHS |
| **ThinkRace** | TK115 (pet-specific) | 100 u. | ~$12–16             | REST + WebSocket         | CE            |

> Contactar siempre a `sales@[proveedor].com` pidiendo **API docs + 2 muestras** antes de confirmar orden. Las muestras cuestan $50–100 y llegan en 5–7 días.

**Precios detallados (Concox AT4 como referencia):**

| Concepto                                | Costo USD                      |
| --------------------------------------- | ------------------------------ |
| Unidad Concox AT4 (FCA Shenzhen)        | $18.00                         |
| Flete DHL Express Shenzhen → CR (50 u.) | ~$200 / 50 = $4.00/u           |
| Impuestos importación CR (~15%)         | ~$2.70/u                       |
| SIM IoT mensual (Emnify/Hologram)       | $2.00/mes/u                    |
| **Costo total landed CR (hardware)**    | **~$24.70/u**                  |
| **Precio venta sugerido**               | **₡20,000–₡25,000 (~$38–$48)** |
| **Margen bruto hardware**               | **~$13–$23/u**                 |

**Inversión mínima para arrancar:**

- MOQ 50 unidades: $900 hardware + $200 flete + $135 impuestos = **~$1,235 USD**
- SIM activación para 50 collares: $100 (primer mes)
- **Total para primer lote**: ~$1,335 USD (~₡694,000)

**Proceso completo de importación China → Costa Rica:**

```
SEMANA 1
  → Contactar proveedor, pedir API docs y datasheet
  → Pedir 2–3 muestras ($50–100 + DHL ~$30)
  → Validar localmente: GPS fix, conectividad, batería, waterproof

SEMANA 2–3
  → Integrar API del proveedor como CollarProvider.Generic en backend
  → Confirmar que los endpoints de posición funcionan con el firmware

SEMANA 4
  → Confirmar orden MOQ 50 u.
  → Pago: wire transfer (T/T) 30% adelanto, 70% antes de embarque
  → Producción: 15–20 días en fábrica

SEMANA 6–7
  → Embarque DHL Express Shenzhen → SJO: 3–5 días hábiles
  → Contratar agente aduanal (obligatorio en CR para mercancía >$1,000 CIF)
  → Código arancelario: 8526.91.00
  → Impuestos: ~15% del valor CIF

SEMANA 8
  → Recepción, QA (probar 5–10% de unidades)
  → Activar SIMs (Emnify o Hologram — dashboard web)
  → Configurar SIMs para apuntar al MQTT de PawTrack
```

**SIM IoT recomendada para CR:**

| Proveedor    | Cobertura CR     | Precio/SIM/mes     | Dashboard | API gestión |
| ------------ | ---------------- | ------------------ | --------- | ----------- |
| **Emnify**   | Movistar + Kölbi | $1.50–$2.50 (5 MB) | ✅ Web    | ✅ REST     |
| **Hologram** | Claro + Kölbi    | $1.00–$2.00 (1 MB) | ✅ Web    | ✅ REST     |

**Agentes aduanales en CR (referencia):**

- Grupo Logístico Aduanero (logisticaaduanera.cr)
- Costo estimado: $80–$120 por trámite

**Contacto Concox:**

- Email: sales@concox.com | Modelo: AT4 | Cert: FCC, CE, ROHS

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

## 10. Costo total para el usuario final

### El GPS vivo es opcional — el QR funciona solo con PawTrack

La integración Tractive es un **añadido premium**, no un requisito. La mayoría de los usuarios usan PawTrack sin GPS y obtienen todo el valor core de la plataforma.

### Escenario A — Solo QR (sin GPS)

| Concepto                   | Costo                      |
| -------------------------- | -------------------------- |
| PawTrack Explorador        | **Gratis**                 |
| PawTrack Plus              | ₡2,990/mes                 |
| Placa QR física (opcional) | ₡1,500–₡4,500 una sola vez |
| **Total mensual**          | **₡0 – ₡2,990/mes**        |

Identificación de mascota, alertas de pérdida, avistamientos, mapa en vivo, chat anónimo → **todo funciona sin Tractive**.

### Escenario B — QR + GPS en vivo (Tractive)

| Concepto                                       | Costo                                     | Frecuencia       |
| ---------------------------------------------- | ----------------------------------------- | ---------------- |
| Tractive DOG 6 (hardware)                      | $79 (~₡41,000)                            | **Una sola vez** |
| Tractive suscripción (plan 1 año)              | $10/mes (~₡5,200) facturado como $120/año | **Anual**        |
| PawTrack Plus                                  | ₡2,990/mes                                | Mensual          |
| **Total recurrente mensual**                   | **~₡8,190/mes**                           | —                |
| **Inversión primer año (incluyendo hardware)** | **~₡161,000**                             | —                |

> ⚠️ La suscripción de Tractive se paga **directamente a Tractive** — PawTrack no la cobra ni la intermedia.

### Conclusión de pricing

El costo de ₡8,190/mes es elevado para el mercado CR. Por eso el posicionamiento correcto es:

- **Vender PawTrack por su red de avistamientos y QR** — diferenciador único que no requiere hardware
- **El GPS es el upsell** para quienes ya tienen o quieren un Tractive — no la razón de compra inicial
- **El afiliado nos genera ₡10,400 una sola vez** por cada usuario que compra Tractive via nuestro link, sin costo operativo

---

## 11. Comparativo de opciones — mercado Costa Rica

Tener varias opciones de precio es correcto: diferentes segmentos tienen diferentes presupuestos y necesidades. El QR integrado al collar (no como accesorio separado) es el estándar de calidad que debe cumplir toda opción que PawTrack recomiende o venda.

### Tabla comparativa completa

> 🔑 = Requiere cuenta/suscripción con un tercero además de PawTrack

| Opción                        | Precio inicial                | Desglose mensual              |  Total/mes  |        QR         |    GPS     | Cuentas requeridas                       |
| ----------------------------- | ----------------------------- | ----------------------------- | :---------: | :---------------: | :--------: | ---------------------------------------- |
| **🏷️ Placa QR sola**          | ₡1,500–₡4,500                 | ₡2,990 PawTrack Plus          | **₡2,990**  |    ✅ ES el QR    |     ❌     | Solo PawTrack                            |
| **🏷️ Placa QR sola (gratis)** | ₡1,500–₡4,500                 | ₡0 PawTrack Explorador        |   **₡0**    |    ✅ ES el QR    |     ❌     | Solo PawTrack                            |
| **📡 OEM Concox + QR láser**  | ₡22,000–₡26,000               | ₡2,990 Plus + ₡1,040 SIM IoT  | **~₡4,030** |    ✅ Grabado     | ✅ Básico  | PawTrack + 🔑 Emnify/Hologram (SIM)      |
| **⭐ Tractive DOG 6**         | ₡41,000 + placa ₡2,000–₡4,500 | ₡2,990 Plus + ₡5,200 Tractive | **~₡8,190** | ⚠️ Pieza separada | ✅ Premium | PawTrack + 🔑 Tractive (obligatorio)     |
| **🔧 Hardware PawTrack**      | ₡35,000–₡50,000               | ₡2,990 Plus + ₡1,040 SIM IoT  | **~₡4,030** |   ✅ Integrado    | ✅ Custom  | PawTrack + SIM (gestionada por PawTrack) |

**Leyenda de terceros:**

| Cuenta tercero                 | Quién la crea                  | Quién la paga                   | Control de PawTrack                                              |
| ------------------------------ | ------------------------------ | ------------------------------- | ---------------------------------------------------------------- |
| 🔑 **Tractive**                | El usuario en tractive.com     | El usuario (directo a Tractive) | Ninguno — relación 100% del usuario con Tractive                 |
| 🔑 **Emnify / Hologram (SIM)** | El usuario o PawTrack gestiona | El usuario o PawTrack cobra     | PawTrack puede gestionar las SIMs centralmente y cobrar incluido |

> ⚠️ **El segmento Tractive es el único donde el usuario debe abrir y pagar una suscripción externa obligatoria** (₡5,200/mes directo a Tractive) sin que PawTrack pueda intervenir ni incluirla en su facturación. Esto debe quedar claro en el onboarding.

### Segmento 1 — El básico (₡0 o ₡2,990/mes)

**Producto:** Placa QR física + PawTrack Explorador o Plus

```
Costo inicial:    ₡1,500–₡4,500 (placa grabada o tag de silicona)
Costo mensual:    ₡0 (Explorador) o ₡2,990 (Plus)
QR:               ✅ La placa ES el QR — no hay nada que perder ni cargar
GPS:              ❌ Sin tracking en tiempo real
Cuentas:          Solo PawTrack — sin terceros
Para quién:       El 80% del mercado — quieren protección básica sin costo elevado
```

**Cómo venderlo:** "Si tu mascota se pierde, cualquier persona que la encuentre escanea el collar con el celular y te llega una notificación en segundos. Sin instalar apps."

Materiales para la placa:
| Material | Durabilidad | Precio aprox. | Dónde hacer en CR |
| -------- | ----------- | ------------- | ----------------- |
| Aluminio grabado | 5+ años | ₡3,000–₡4,500 | Publigráfica, imprentas con láser |
| Acrílico con QR impreso UV | 2–3 años | ₡1,500–₡2,500 | Imprenta digital local |
| Tag silicona importado | 2–3 años | ₡2,000–₡3,500 | AliExpress MOQ 50 |
| Tag NFC + QR combo | 3–4 años | ₡4,000–₡6,000 | AliExpress NTAG213 |

---

### Segmento 1b — Collar físico personalizado desde China + impresión rápida en sitio

Este modelo cubre dos necesidades complementarias:

1. **Collar físico con soporte QR** fabricado en China a bajo costo — el collar, el QR y PawTrack son un solo paquete
2. **Impresión en sitio** para ferias, campañas de venta o eventos veterinarios: el usuario registra a su mascota y sale con el collar puesto en 3 minutos

#### Diseños de collar con soporte QR (sin GPS)

| Diseño                                         | Descripción                                                         | Costo aprox. China | QR              | MOQ    |
| ---------------------------------------------- | ------------------------------------------------------------------- | ------------------ | --------------- | ------ |
| **Collar nylon + porta-placa metálica**        | Rivet o clip para placa 3×2 cm, QR impreso en etiqueta vinilo       | $1.20–$2.00        | 🔄 Reemplazable | 100 u. |
| **Collar nylon + ventana TPU**                 | Ventana transparente sellada donde se inserta el QR en papel/vinilo | $0.80–$1.50        | 🔄 Reemplazable | 200 u. |
| **Collar + tag silicona con QR grabado láser** | Tag de silicona con QR único grabado, anilla al collar              | $1.50–$2.50        | ✅ Permanente   | 100 u. |
| **Collar con placa aluminio encastrada**       | Placa grabada en fábrica con QR único por collar                    | $2.50–$4.00        | ✅ Permanente   | 50 u.  |

**Proveedor recomendado para buscar en Alibaba:**

- Búsqueda: `custom dog collar QR code plate` o `pet ID collar tag window`
- Filtrar: BSCI certified, Guangzhou/Dongguan, Min. 100 units
- Pedir muestra antes de confirmar orden: $15–30 por muestra + DHL
- Contactar directamente fábricas con la palabra "OEM welcome"

**Proveedores específicos a explorar:**

- **Guangzhou Maichen Leather** — collares con soporte para placa metálica
- **Dongguan Yiwu Pet Products** — collares nylon con ventana TPU
- Búsqueda directa: alibaba.com → `pet collar ID tag holder custom`

**Costo total landed CR (collar sin GPS, lote 200 unidades):**

| Concepto                               | Costo USD              |
| -------------------------------------- | ---------------------- |
| 200 collares con porta-placa ($1.50/u) | $300                   |
| Flete DHL Shenzhen → CR                | ~$120                  |
| Impuestos CR (~15%)                    | ~$63                   |
| **Total landed**                       | **~$483 (~₡251,000)**  |
| **Costo por collar**                   | **~$2.42/u (~₡1,260)** |
| **Precio venta sugerido**              | **₡4,500–₡6,000**      |
| **Margen por collar**                  | **~₡3,000–₡4,500**     |

---

#### Infraestructura de impresión rápida en sitio

Para campañas y ferias donde el usuario registra su mascota y recibe el collar en el momento.

**Hardware recomendado:**

| Opción              | Modelo               | Precio    | Velocidad      | Conectividad    | Formato QR               |
| ------------------- | -------------------- | --------- | -------------- | --------------- | ------------------------ |
| **⭐ Mejor opción** | Brother QL-820NWB    | ~$180 USD | 2 seg/etiqueta | WiFi + BT + USB | DK-22251 vinilo continuo |
| Económica           | DYMO LabelWriter 450 | ~$80 USD  | 3 seg/etiqueta | USB             | 30x57mm labels           |
| Industrial          | Zebra ZD220          | ~$150 USD | 1 seg/etiqueta | USB + BT        | ZPL, alta durabilidad    |

**Brother QL-820NWB es la opción correcta porque:**

- Imprime desde tablet/smartphone via Bluetooth o WiFi — sin laptop
- Funciona con batería recargable — no necesita tomacorriente en ferias
- Etiqueta DK-22251 es vinilo laminado resistente al agua y al sol
- Costo etiqueta: ~₡20 por label (rollo de 400 etiquetas ~$15 USD)

**Flujo de campaña en sitio (3 minutos por mascota):**

```
PASO 1 — Registro (1 min)
  → Staff abre PawTrack en tablet/celular
  → Registra: nombre mascota, especie, foto rápida, teléfono dueño
  → Sistema genera petId y QR único al instante

PASO 2 — Impresión (10 segundos)
  → PawTrack genera imagen QR (ya existe: GET /api/pets/{id}/qr)
  → Staff envía a impresora via BT
  → Impresora imprime etiqueta QR vinilo

PASO 3 — Ensamble (30 segundos)
  → Staff inserta/adhiere etiqueta en porta-placa del collar
  → Coloca collar en mascota y ajusta tamaño

PASO 4 — Verificación (30 segundos)
  → Escanear el collar con la cámara → debe abrir perfil de la mascota
  → Listo — dueño sale con collar puesto y mascota registrada
```

**Lo que necesita PawTrack app para soportar esto:**

- `GET /api/pets/{id}/qr` ya existe y retorna imagen SVG/PNG ✅
- Interfaz de registro simplificada para staff (flujo de 3 pasos, tablet-optimizado) → pendiente
- Conexión BT/WiFi a Brother QL desde la app → requiere integración Brother SDK o web print

**Inversión para una campaña:**

| Ítem                                     | Costo                     |
| ---------------------------------------- | ------------------------- |
| Impresora Brother QL-820NWB              | ~$180 USD                 |
| 2 rollos vinilo DK-22251 (800 etiquetas) | ~$30 USD                  |
| 200 collares con porta-placa             | ~$483 USD                 |
| **Total para campaña de 200 mascotas**   | **~$693 USD (~₡360,000)** |
| Ingreso (200 ventas × ₡5,000 collar)     | ₡1,000,000                |
| **Margen bruto campaña**                 | **~₡640,000**             |

> La impresora se amortiza desde la primera campaña y sirve para todas las siguientes.

---

### Segmento 2 — El activo con GPS (₡4,030/mes)

**Producto:** Collar OEM (Concox AT4 / Jimi / Queclink) con QR grabado láser + PawTrack Plus

```
Costo inicial:    ₡22,000–₡26,000 (hardware landed CR)
Costo mensual:    ₡2,990 PawTrack Plus
                + ₡1,040 SIM IoT (Emnify ~$2/mes — Movistar/Kölbi CR)
                = ~₡4,030/mes total
QR:               ✅ Grabado con láser en el enclosure (+$0.50–$1.00 en fábrica)
GPS:              ✅ LTE, actualización cada 30s–5min según movimiento
Batería:          3–7 días con sleep optimizado
Cuentas:          PawTrack + 🔑 Emnify/Hologram (SIM IoT)
                  → PawTrack PUEDE gestionar las SIMs centralmente
                  → Opción: incluir el costo SIM en el precio del hardware
Para quién:       Dueños que quieren GPS sin el costo de Tractive
```

**Ventaja de la SIM IoT vs Tractive:** PawTrack puede gestionar las SIMs (Emnify tiene API REST para activar/desactivar), cobrarla incluida en el precio del collar o en el plan, y el usuario nunca necesita crear una cuenta externa.

**Por qué es la opción más atractiva para CR:**

- Un solo objeto en el collar (QR + GPS integrados)
- Costo mensual 50% menor que Tractive
- Hardware PawTrack-branded (diferenciador)
- El margen de hardware (~₡13,000–₡23,000) va directamente a PawTrack

**Lo que necesita para funcionar:**

- Integrar API del proveedor como `CollarProvider.Generic` (~3–5 días desarrollo)
- SIM IoT activada (Emnify: $1.50–2.50/mes, cobertura Movistar+Kölbi en CR)
- Pedido mínimo: 50 unidades (~$1,335 USD inversión total)

---

### Segmento 3 — El premium con Tractive (₡8,190/mes)

**Producto:** Tractive DOG 6 + placa QR separada + PawTrack Plus

```
Costo inicial:    ₡41,000 tracker + ₡2,000–₡4,500 placa QR separada
Costo mensual:    ₡2,990 PawTrack Plus
                + ₡5,200 Tractive suscripción ($10/mes — pago DIRECTO a Tractive)
                = ~₡8,190/mes total
QR:               ⚠️ Placa QR adicional — dos piezas en el collar
GPS:              ✅ Premium (worldwide, health monitoring, 12-day battery)
Cuentas:          PawTrack + 🔑 Tractive (OBLIGATORIO — el usuario debe crearse
                  cuenta en tractive.com y pagar directo a ellos)
Para quién:       Dueños con perros activos/adventures o que ya tienen Tractive
```

> ⚠️ **El usuario debe pagar ₡5,200/mes directamente a Tractive.** PawTrack no puede incluir este costo en su facturación, no puede cancelarlo, ni tiene control sobre él. Si Tractive sube precios o cambia términos, el usuario lo enfrenta directamente. Esto debe comunicarse claramente antes de que el usuario compre el tracker.

**La limitación del QR con Tractive:** el tracker Tractive no tiene espacio para QR grabado porque no vendemos el hardware. El usuario necesita un segundo accesorio (placa QR separada). Esto es inferior a la experiencia de un collar integrado.

**Cuándo tiene sentido recomendar Tractive:**

- El usuario ya tiene un Tractive y solo quiere agregar PawTrack
- Tiene perro grande activo que necesita tracking en exteriores intensivos
- No le importa el costo mensual elevado
- Nuestro ingreso: ₡10,400 comisión de afiliado (una sola vez)

---

### Segmento 4 — Hardware PawTrack propio (futuro)

**Producto:** Collar diseñado por PawTrack con QR grabado en enclosure + Plus

```
Costo inicial:    ₡35,000–₡50,000 (estimado, con margen)
Costo mensual:    ₡2,990 PawTrack Plus
                + ₡1,040 SIM IoT (incluida en el precio del hardware o cobrada aparte)
                = ~₡4,030/mes
QR:               ✅ Integrado desde el diseño — el mejor UX posible
GPS:              ✅ Optimizado para CR (LTE-M Movistar/Kölbi)
Cuentas:          Solo PawTrack — la SIM es gestionada por PawTrack, no el usuario
Para quién:       Todos — es el producto final ideal de PawTrack
```

---

### Recomendación de roadmap por volumen de usuarios

```
0–100 usuarios Plus
  → Ofrecer solo placa QR + afiliado Tractive
  → Cero inventario, cero riesgo

100–500 usuarios Plus
  → Lanzar collar OEM (Concox) con QR grabado
  → MOQ 50 unidades: ~$1,335 USD
  → Primera fuente de ingreso por hardware

500+ usuarios Plus
  → Estudiar hardware propio PawTrack
  → Diferenciador de marca, mayor margen, mejor UX

Expansión internacional (España)
  → Activar integración Kippy (ya está en el código)
  → Europa tiene cobertura nativa de Kippy
```

### El estándar mínimo para cualquier collar PawTrack

> **Todo collar que PawTrack recomiende, venda o distribuya DEBE tener el QR integrado en el mismo objeto** — grabado en enclosure, placa encastrada o material resistente al agua. Dos objetos separados es experiencia fragmentada y mayor probabilidad de que el usuario pierda el QR cuando más lo necesita.

---

## 12. Collar PawTrack integrado — QR + GPS en un solo dispositivo

**El concepto:** un único accesorio que sirve como identificador QR estático Y como tracker GPS activo. Elimina la necesidad de dos elementos separados en el collar.

### El problema de dos piezas

Hoy un usuario Plus necesita:

1. Placa QR (plástico/metal grabado, ~₡2,000–4,500) — estático, sin batería
2. Tracker Tractive ($79 USD) — GPS activo, batería, suscripción

Dos elementos físicos en el collar = bulto, posibilidad de perder uno, experiencia fragmentada.

### Solución: QR grabado en el enclosure del tracker

El QR de PawTrack es simplemente una URL: `pawtrack.cr/p/{serialCollar}`. Se puede **grabar con láser directamente en el enclosure de plástico** del tracker — sin pantalla, sin energía, permanente.

```
┌─────────────────────────────────┐
│  TRACKER GPS PAWTRACK           │
│  ┌───────┐  ┌───────────────┐   │
│  │ [QR] │  │  GPS + LTE    │   │
│  │código│  │  batería      │   │
│  └───────┘  └───────────────┘   │
│  grabado en TPU/ABS             │
└─────────────────────────────────┘
```

### Cómo funciona el binding QR ↔ mascota

Cada unidad de hardware sale de fábrica con un **serial único** (ej: `PT-001234`). El QR codifica `pawtrack.cr/p/PT-001234`. Al activar el collar en la app PawTrack:

```
1. Usuario escanea el QR del collar con la cámara
2. App detecta el serial y lo vincula al petId del usuario
3. Backend mapea: serial → petId (tabla CollarQrBinding)
4. Cualquier escaneo del QR en adelante muestra el perfil correcto
```

Esto permite fabricar collares en lote sin saber a qué mascota se asignará cada uno — el binding ocurre en la app, no en fábrica.

### Opciones de manufactura del QR en el enclosure

| Método                            | Costo/unidad | Duración   | Calidad    | Apto para agua    |
| --------------------------------- | ------------ | ---------- | ---------- | ----------------- |
| **Grabado láser en ABS/PC**       | $0.50–1.00   | Permanente | Alta       | ✅                |
| Sticker UV laminado (encapsulado) | $0.20–0.40   | 2–3 años   | Media      | ✅ (con laminado) |
| Serigrafía en enclosure           | $0.30–0.60   | Permanente | Media-alta | ✅                |
| Placa metálica encastrada         | $1.50–3.00   | Permanente | Muy alta   | ✅                |

**Recomendación para MVP:** pedir al proveedor OEM (Concox/Jimi) que incluya **grabado láser** del QR en el enclosure. Esto se solicita en la orden de personalización y agrega ~$0.50–1.00/unidad. JLCPCB y los mismos fabricantes ofrecen este servicio.

### Impacto en el backend

Requiere una tabla adicional mínima:

```sql
CREATE TABLE CollarQrBindings (
    Serial      NVARCHAR(20) PRIMARY KEY,   -- PT-001234
    CollarId    UNIQUEIDENTIFIER NULL,       -- NULL si aún no vinculado
    BoundAt     DATETIMEOFFSET NULL
);
```

Y un endpoint nuevo:

```
POST /api/collars/bind-serial
Body: { serial: "PT-001234", petId: "..." }
```

El perfil público `/p/{serial}` resuelve:

1. Si está vinculado → muestra perfil de la mascota (igual que `/p/{petId}`)
2. Si no está vinculado → muestra página de activación con CTA "Activar este collar"

### Ventajas del collar integrado

- **UX premium**: un solo objeto en el collar del perro
- **Diferenciador de producto**: "El primer collar de CR con QR + GPS integrados"
- **Menor fricción**: el usuario no necesita comprar ni imprimir nada extra
- **Revenue**: vendemos el hardware completo (₡20,000–30,000) + suscripción Plus

### Timeline de implementación

| Paso                                                    | Tiempo           | Quién    |
| ------------------------------------------------------- | ---------------- | -------- |
| Agregar tabla `CollarQrBindings` + endpoint bind-serial | 1 día            | Backend  |
| Adaptar `/p/{id}` para resolver serial O petId          | 0.5 día          | Backend  |
| Pantalla de activación de collar en app                 | 1 día            | Frontend |
| Coordinar grabado láser con proveedor OEM               | En próxima orden | —        |
| **Total de desarrollo**                                 | **~2.5 días**    | —        |

---

## 13. Requisitos para hardware propio — lista completa

### Resumen ejecutivo

| Categoría                              | Costo estimado           | Tiempo        | Estado       |
| -------------------------------------- | ------------------------ | ------------- | ------------ |
| Electrónica + PCB (prototipos)         | $700–$1,500 USD          | Semanas 1–8   | ⛔ Pendiente |
| EE engineer freelance (firmware + PCB) | $3,000–$7,000            | Meses 1–3     | ⛔ Pendiente |
| Herramientas de desarrollo             | $800–$1,100              | Única vez     | ⛔ Pendiente |
| Certificación SUTEL (CR) + FCC         | $1,500–$4,500            | **2–6 meses** | ⛔ Pendiente |
| Backend (IoT Hub + ingest endpoint)    | ~5–6 días dev            | Mes 2         | ⛔ Pendiente |
| Primer lote producción (50 u.)         | $4,000–$8,000            | Mes 4–6       | ⛔ Futuro    |
| **TOTAL hasta producción**             | **~$10,000–$21,000 USD** | **4–6 meses** |              |

> ⚠️ **La certificación SUTEL es el cuello de botella.** Sin ella el collar no puede operar legalmente en CR con LTE-M. Iniciar en paralelo desde el día 1.

---

### 13.1 BOM completo (Bill of Materials)

| Componente                       | Modelo                        | Propósito                                | Fuente            | USD/u        |
| -------------------------------- | ----------------------------- | ---------------------------------------- | ----------------- | ------------ |
| MCU                              | ESP32-S3-WROOM-1              | CPU + BLE + WiFi provisioning            | DigiKey / Mouser  | $4.00        |
| Celular + GPS                    | SIMCOM SIM7080G               | LTE-M + NB-IoT + GNSS integrado          | SIMCOM directo    | $12.00       |
| Acelerómetro                     | ADXL345                       | Wake-on-motion interrupt                 | AliExpress        | $0.80        |
| **BMS chip**                     | **TP4056 o BQ25100**          | **Carga segura LiPo — obligatorio**      | LCSC              | $0.40        |
| Regulador 3.3V                   | AMS1117-3.3                   | Alimentar ESP32/ADXL                     | LCSC              | $0.15        |
| Batería                          | LiPo 3.7V 1000mAh (50×34×5mm) | Energía principal                        | AliExpress        | $3.50        |
| Conector carga                   | USB-C 2.0 (solo carga)        | Carga de batería                         | LCSC              | $0.30        |
| Antena LTE                       | Flex PCB 700–2100 MHz         | Señal celular (el 30% del trabajo de RF) | SIMCOM / Molex    | $1.20        |
| LED indicador                    | WS2812B RGB                   | Estado: conectado/sin señal/cargando     | AliExpress        | $0.20        |
| Protección ESD                   | TVS diodes                    | Proteger USB-C y antena                  | LCSC              | $0.30        |
| Pasivos varios                   | Condensadores, resistencias   | Circuito soporte                         | JLCPCB BOM        | ~$1.00       |
| PCB prototipo (5u.)              | 4 capas, 50×35mm              | Placa base                               | JLCPCB            | $30–80       |
| Enclosure                        | TPU flexible (3D print)       | Case resistente al agua IP65             | Local / Shapeways | $8–15        |
| **Total por unidad (prototipo)** |                               |                                          |                   | **~$24–$37** |

**Notas críticas de diseño:**

- El BMS (TP4056) es **obligatorio** — sin él la carga del LiPo puede incendiar el collar
- La antena LTE-M es el componente de diseño más crítico: requiere clearance, ground plane y match de impedancia
- IP65 mínimo: conformal coating en PCB + gasket de silicona en USB-C + enclosure sellado
- Incluir test pads en la PCB para facilitar QA en manufactura

---

### 13.2 Herramientas de desarrollo

| Herramienta           | Modelo                       | Para qué                      | USD              |
| --------------------- | ---------------------------- | ----------------------------- | ---------------- |
| **Osciloscopio**      | Rigol DS1054Z (100MHz)       | Debug señales, consumo, RF    | ~$350            |
| **Analizador lógico** | Saleae Logic 8 o clone       | Debug UART/SPI/I2C            | $30–150          |
| **USB Power Meter**   | Atorch o similar             | Medir consumo real por estado | $15–30           |
| Estación soldadura    | Hakko FX-888D                | Prototipo manual              | $120             |
| Impresora 3D          | Bambu Lab A1 Mini            | Prototipar enclosures TPU     | $300–400         |
| Programador JTAG      | ESP-Prog (oficial Espressif) | Flashear + debug firmware     | $15              |
| SIM de desarrollo     | Emnify / Hologram (1 SIM)    | Probar LTE-M en CR            | $5/mes           |
| **Total**             |                              |                               | **~$850–$1,100** |

---

### 13.3 Firmware — checklist completo

```
CORE
  ☐ Boot sequence (init periféricos, leer config desde NVS)
  ☐ Driver SIM7080G via AT commands (UART2, 115200 baud)
  ☐ Registro LTE-M + config APN (Emnify: em / Hologram: hologram)
  ☐ MQTT over TLS 1.2 → Azure IoT Hub (MQTT broker)
  ☐ GNSS parsing: lat, lng, altitude, HDOP, num satélites
  ☐ A-GPS via LTE antes de cold fix (AT+CGNSSINFO, reduce cold fix a 3-8s)

POWER MANAGEMENT — el trabajo más difícil y el más importante
  ☐ Driver ADXL345 vía I2C + configurar motion/inactivity interrupt
  ☐ State machine: Active → Light Sleep → Deep Sleep (ver §2.3)
  ☐ ESP32 light_sleep_enable() con wake sources: ADXL345 INT + timer
  ☐ SIM7080G PSM (Power Saving Mode): AT+CPSMS=1,...
  ☐ GPS power gate (transistor en VCC del módulo)
  ☐ Calibrar umbrales de movimiento (THRESH_ACT/INACT) con mascotas reales

SEGURIDAD Y PROVISIONING
  ☐ Secure boot con ESP32 eFuse (previene firmware no autorizado)
  ☐ Certificado TLS por dispositivo para Azure IoT Hub DPS
  ☐ Provisioning flow: BLE → app PawTrack → credenciales MQTT guardadas en NVS
  ☐ Serial único en eFuse o NVS cifrado con clave maestra

OTA — CRÍTICO para actualizar collares en campo
  ☐ Partition table con 2 slots (OTA_0 / OTA_1)
  ☐ esp_https_ota() con verificación de firma digital
  ☐ Rollback automático si el nuevo firmware no bootea correctamente
  ☐ Versión de firmware reportada en telemetría MQTT

ROBUSTEZ
  ☐ Watchdog timer (WDT) con recovery automático
  ☐ Cola local en NVS para MQTT publish si sin conexión
  ☐ Reconexión LTE-M automática (backoff exponencial)
  ☐ Alerta batería baja (<20%) publicada vía MQTT
  ☐ LED indicador: verde=OK, amarillo=sin señal, rojo=batería baja, azul=cargando
```

---

### 13.4 Backend — lo que PawTrack debe construir

| Componente                      | Estado       | Días dev      | Descripción                                                  |
| ------------------------------- | ------------ | ------------- | ------------------------------------------------------------ |
| Azure IoT Hub (infra)           | ⛔ Pendiente | 0.5           | Agregar `Microsoft.Devices/IotHubs` al Bicep                 |
| Azure Function IoT → API        | ⛔ Pendiente | 1             | Trigger IoT Hub → transforma telemetría → POST backend       |
| `POST /api/collars/ingest`      | ⛔ Pendiente | 1.5           | Recibe posición y batería del collar propio                  |
| DPS (Device Provisioning)       | ⛔ Pendiente | 1             | Enrolar collares en producción sin credenciales hardcodeadas |
| OTA bucket Blob Storage         | ⛔ Pendiente | 0.5           | Container `collar-firmware` + endpoint de descarga           |
| `CollarQrBindings` tabla        | ⛔ Pendiente | 0.5           | Serial → petId (ver §12)                                     |
| `POST /api/collars/bind-serial` | ⛔ Pendiente | 0.5           | Vincular serial a mascota en la app                          |
| **Total**                       |              | **~5–6 días** |                                                              |

---

### 13.5 Certificaciones — SUTEL es la más crítica

| Certificación          | Organismo              |  CR obligatorio  | Costo                            | Tiempo        |
| ---------------------- | ---------------------- | :--------------: | -------------------------------- | ------------- |
| **SUTEL homologación** | SUTEL CR (sutel.go.cr) |      ✅ Sí       | ₡200,000–₡500,000 (~$400–$1,000) | **2–6 meses** |
| FCC (USA)              | FCC.gov                |   Recomendado    | $1,000–$3,000                    | 4–8 semanas   |
| CE (EU)                | Notified Body          |     Opcional     | $1,500–$4,000                    | 8–16 semanas  |
| ROHS                   | Fabricante             | Buenas prácticas | $0 (declaración)                 | —             |

**Proceso SUTEL:**

1. Reunir: FCC cert del módulo SIM7080G + datasheet + manual técnico + esquemático
2. Ingresar expediente en sutel.go.cr → Servicios → Homologación
3. Pagar timbre fiscal + tarifa de análisis técnico
4. Esperar revisión técnica: 60–180 días hábiles
5. Recibir número de homologación → grabarlo en el enclosure del collar
6. Renovar cada 5 años

**Cómo acelerar:** el SIM7080G ya tiene FCC/CE — se puede argumentar que el dispositivo usa un "certified radio module" y reducir el alcance de la certificación. Contratar un consultor local de SUTEL reduce el tiempo en 30–50%.

---

### 13.6 Fases de manufactura

**Fase 1 — Prototipos ($1,500–$3,000 · semanas 1–8)**

- PCB en JLCPCB con SMT assembly
- Enclosure TPU impreso en 3D
- Pruebas: GPS fix outdoor, sleep actual con USB Power Meter, inmersión 30 min
- Sin QR permanente — sticker temporal para pruebas

**Fase 2 — Pilotos ($3,000–$8,000 · meses 2–4)**

- PCB revisada con mejoras de prototipo
- Tooling para enclosure de plástico: $2,000–$5,000 en Shenzhen (molde inyección)
- QR grabado láser en enclosure
- SUTEL iniciado en paralelo
- 10–30 unidades para beta testers reales

**Fase 3 — Producción ($8,000–$15,000 · meses 4–6)**

- SUTEL obtenida
- Lote 50–100 unidades con partner de manufactura
- QA automatizado (5–10% sampling por lote)
- Caja de empaque con marca PawTrack CR

---

### 13.7 Referencias técnicas

| Recurso                        | URL                                               |
| ------------------------------ | ------------------------------------------------- |
| SIM7080G Hardware Design Guide | simcom.com/product/SIM7080G.html                  |
| SIM7080G AT Command Manual     | simcom.com/support                                |
| ESP32-S3 Technical Reference   | docs.espressif.com/esp-idf/esp32s3                |
| ESP32 Power Management         | espressif.com → AN-ESP32-POWERSAVE                |
| ADXL345 Datasheet              | analog.com/en/products/adxl345.html               |
| Azure IoT Hub + DPS            | learn.microsoft.com/azure/iot-hub                 |
| JLCPCB (PCB + SMT)             | jlcpcb.com                                        |
| SUTEL homologación             | sutel.go.cr → Servicios → Homologación de equipos |
| KiCad (EDA open source)        | kicad.org                                         |

---

### 13.8 Riesgos principales

| Riesgo                       | Probabilidad | Mitigación                                                           |
| ---------------------------- | :----------: | -------------------------------------------------------------------- |
| SUTEL demora >6 meses        |   **Alta**   | Iniciar día 1; usar módulo certificado; consultor local              |
| Consumo real > estimado      |    Media     | Medir con USB Power Meter desde el primer prototipo                  |
| SIM7080G desabasto           |     Baja     | Pedir 20+ unidades de dev inmediatamente; tiene alternativa SIM7070G |
| Waterproofing falla en campo |    Media     | IP65 mínimo; test de inmersión en cada lote                          |
| LTE-M señal baja en CR rural |    Media     | SIM7080G soporta fallback a 2G/EDGE                                  |

---

_PawTrack CR · Documento técnico collares GPS · Agosto 2026_
