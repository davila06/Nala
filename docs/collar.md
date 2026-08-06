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
Collar (ESP32-S3 + SIM7080G) → MQTT/TLS → Azure IoT Hub → Azure Function → POST /api/collars/ingest
```

**Variable `CollarProvider.Own = 0`** ya reservada en el dominio.

#### Componentes recomendados

| Componente | Modelo | Dónde comprar | Costo aprox |
| ---------- | ------ | ------------- | ----------- |
| MCU | ESP32-S3 (dual-core, BLE) | DigiKey / Mouser | $4 |
| Módulo celular + GPS | SIM7080G (LTE-M + GNSS integrado) | SIMCOM directo / DigiKey | $12 |
| Acelerómetro | ADXL345 (detección de movimiento) | AliExpress | $0.80 |
| Batería | LiPo 3.7V 1000mAh (plana, 50×34×5mm) | AliExpress | $3.50 |
| PCB | JLCPCB (5 prototipos ~$2 + SMT assembly) | jlcpcb.com | $2–15 |
| Case | Impresión 3D TPU flexible (resistente a agua) | Local o Shapeways | $5–15 |

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

| Escenario | % tiempo activo | Consumo promedio | Duración |
| --------- | --------------- | ---------------- | -------- |
| Sin sleep (malo) | 100% | 280 mA | ~3.5 horas |
| Con Light Sleep solo | 10% activo | ~30 mA | ~33 horas |
| Con Deep Sleep (mascota en casa) | 2% activo | ~5 mA | **~8 días** |
| Mascota activa (caminata 2h/día) | 15% activo | ~43 mA | **~23 horas** |

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

| Proveedor | Modelo | MOQ | Precio FCA Shenzhen | API | Cert. |
| --------- | ------ | --- | ------------------- | --- | ----- |
| **Concox** | AT4 (GPS+WiFi+LTE) | 50 u. | ~$18 | REST propietaria | FCC, CE, ROHS |
| **Jimi IoT** | JM-VL01 / LL01 | 50 u. | ~$15–22 | REST + MQTT | FCC, CE |
| **Queclink** | GL300 (miniatura) | 50 u. | ~$18–20 | REST + protocolo binario | FCC, CE, ROHS |
| **ThinkRace** | TK115 (pet-specific) | 100 u. | ~$12–16 | REST + WebSocket | CE |

> Contactar siempre a `sales@[proveedor].com` pidiendo **API docs + 2 muestras** antes de confirmar orden. Las muestras cuestan $50–100 y llegan en 5–7 días.

**Precios detallados (Concox AT4 como referencia):**

| Concepto | Costo USD |
| -------- | --------- |
| Unidad Concox AT4 (FCA Shenzhen) | $18.00 |
| Flete DHL Express Shenzhen → CR (50 u.) | ~$200 / 50 = $4.00/u |
| Impuestos importación CR (~15%) | ~$2.70/u |
| SIM IoT mensual (Emnify/Hologram) | $2.00/mes/u |
| **Costo total landed CR (hardware)** | **~$24.70/u** |
| **Precio venta sugerido** | **₡20,000–₡25,000 (~$38–$48)** |
| **Margen bruto hardware** | **~$13–$23/u** |

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

| Proveedor | Cobertura CR | Precio/SIM/mes | Dashboard | API gestión |
| --------- | ------------ | -------------- | --------- | ----------- |
| **Emnify** | Movistar + Kölbi | $1.50–$2.50 (5 MB) | ✅ Web | ✅ REST |
| **Hologram** | Claro + Kölbi | $1.00–$2.00 (1 MB) | ✅ Web | ✅ REST |

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

## 11. Collar PawTrack integrado — QR + GPS en un solo dispositivo

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

| Método | Costo/unidad | Duración | Calidad | Apto para agua |
| ------ | ------------ | -------- | ------- | -------------- |
| **Grabado láser en ABS/PC** | $0.50–1.00 | Permanente | Alta | ✅ |
| Sticker UV laminado (encapsulado) | $0.20–0.40 | 2–3 años | Media | ✅ (con laminado) |
| Serigrafía en enclosure | $0.30–0.60 | Permanente | Media-alta | ✅ |
| Placa metálica encastrada | $1.50–3.00 | Permanente | Muy alta | ✅ |

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

| Paso | Tiempo | Quién |
| ---- | ------ | ----- |
| Agregar tabla `CollarQrBindings` + endpoint bind-serial | 1 día | Backend |
| Adaptar `/p/{id}` para resolver serial O petId | 0.5 día | Backend |
| Pantalla de activación de collar en app | 1 día | Frontend |
| Coordinar grabado láser con proveedor OEM | En próxima orden | — |
| **Total de desarrollo** | **~2.5 días** | — |

---

_PawTrack CR · Documento técnico collares GPS · Agosto 2026_
