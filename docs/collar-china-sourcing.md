# PawTrack Collar GPS — Ficha de Sourcing y Contacto a Fabricantes

> Documento de trabajo para conseguir un fabricante en China que produzca el collar PawTrack.
> Basado en el estado real del código a 2026-08-24 (no en el roadmap aspiracional).
> Complementa a [`collar.md`](./collar.md) — este documento es específico para el proceso de sourcing/RFQ.

---

## 1. Resumen ejecutivo

Con lo que **ya está implementado** en el backend, la ruta de menor esfuerzo para lanzar un collar GPS fabricado en China es integrarlo como **`CollarProvider.Generic`**, reutilizando el mismo patrón de `TractivePollingJob` (polling REST cada 5 min). Esta ficha existe para:

1. Dejar claro qué necesita el proveedor soportar técnicamente para que la integración sea rápida (días, no meses).
2. Detectar de una vez los gaps de seguridad/backend que hay que cerrar antes de aceptar tráfico real de dispositivos.
3. Definir las **variantes de producto** que queremos cotizar en el mismo RFQ (collar solo con GPS, GPS + cámara, GPS + pantalla e-ink, y combinaciones), ya que el mismo proveedor puede ofrecer varios niveles de producto sobre una plataforma común.
4. Traer una plantilla de correo lista para enviar a los 4 fabricantes candidatos (Concox, Jimi IoT, Queclink, ThinkRace) pidiendo exactamente la información necesaria para decidir.

---

## 2. Estado actual verificado en el código (no en el roadmap)

| Pieza                                                             | Archivo                                                                                                               | Estado                                                                                                                |
| ----------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `CollarProvider` enum (`Own`, `Tractive`, `Kippy`, `Generic`)     | [`CollarProvider.cs`](../backend/src/PawTrack.Domain/Collars/CollarProvider.cs)                                       | ✅ Existe                                                                                                             |
| Integración GPS por OAuth2 + polling (Tractive)                   | [`TractivePollingJob.cs`](../backend/src/PawTrack.Infrastructure/Collars/TractivePollingJob.cs), `TractiveService.cs` | ✅ Completa — es el patrón a clonar para un OEM chino                                                                 |
| Endpoint de push directo `POST /api/collars/pet/{petId}/location` | [`CollarsController.cs`](../backend/src/PawTrack.API/Controllers/CollarsController.cs)                                | ✅ Existe, pero **hereda `[Authorize]` de la clase** — solo acepta JWT de usuario, no hay auth de dispositivo todavía |
| Histórico de posición + purga automática                          | `CollarLocation.cs`, `CollarLocationPurgeJob.cs`                                                                      | ✅ Completo (>30 días se purgan)                                                                                      |
| `CollarQrBindings` (serial de fábrica ↔ mascota)                  | —                                                                                                                     | ❌ Solo documentado en `collar.md`, **no implementado**                                                               |
| Auth de dispositivo (API key por collar)                          | —                                                                                                                     | ❌ No existe — hay un patrón reutilizable en `ClinicApiKeys` que se puede copiar                                      |
| Ingesta de foto/video desde el collar (cámara)                    | —                                                                                                                     | ❌ No existe ningún endpoint ni almacenamiento pensado para esto todavía                                              |
| Collar propio con GPS + pantalla en un solo enclosure             | —                                                                                                                     | ❌ No existe ni en dominio ni en hardware. Roadmap futuro, no bloqueante para el primer lote                          |

**Conclusión:** todo lo que se agregue sobre el collar base (cámara, pantalla) es hardware y backend nuevo — el único componente 100% reutilizable hoy es el flujo GPS (polling REST → `CollarLocation`).

---

## 3. Los dos caminos de integración que el backend soporta

### Camino A — Polling REST (recomendado para el primer lote)

Igual que `TractivePollingJob`: cada 5 min, el backend llama a la API del proveedor y guarda la posición.

- **Esfuerzo:** ~1–2 días (clonar `ITractiveService` → `IConcoxService`/`IJimiService`, registrar el job).
- **Requisito del proveedor:** API REST documentada para _consultar_ posición de un dispositivo por ID.
- **Ventaja:** cero trabajo de firmware, cero problema de auth de dispositivo (las credenciales quedan del lado del backend, no en el hardware).
- **Candidatos:** Concox, Jimi IoT, Queclink (ver tabla en sección 5).

**Paso a paso técnico (idéntico al usado con Tractive):**

1. **Autenticación con el proveedor.** La mayoría de estas plataformas OEM usan un usuario/contraseña de cuenta de "reseller" o una API key estática (no OAuth2 como Tractive) — hay que confirmar en el RFQ cuál de los dos modelos aplica, porque cambia si hace falta un flujo de intercambio de token o basta con una key fija guardada en Key Vault.
2. **Registro del dispositivo en la plataforma del proveedor.** Cada collar viene con un `deviceId`/IMEI de fábrica que hay que asociar a la cuenta de PawTrack en el dashboard del proveedor antes de que la API devuelva datos — este paso es manual o vía su API de aprovisionamiento, según el fabricante.
3. **Mapeo `deviceId` → `Collar` en nuestra DB.** Al momento de `RegisterCollarCommand`, el `ExternalDeviceId` guarda el IMEI/deviceId del proveedor — mismo campo que ya usa Tractive, no requiere cambio de esquema.
4. **Polling job dedicado.** Se agrega un servicio hermano a `TractivePollingJob` (o se extiende el mismo `PeriodicTimer` de 5 min) que filtra `Collar.Provider == CollarProvider.Generic` y llama al endpoint de posición del proveedor por cada `deviceId` activo.
5. **Normalización de la respuesta.** Cada proveedor devuelve el payload en su propio formato (nombres de campo, unidades, timestamp) — hace falta un mapeo a `CollarPosition(Lat, Lng, BatteryPercent)` igual que hace `TractiveService.GetLatestPositionAsync`, para no filtrar el formato del proveedor al resto del dominio.
6. **Manejo de errores y rate limits.** Igual que con Tractive (`try/catch` + `LogWarning` por collar, sin abortar el batch completo), pero además revisar si el proveedor impone un límite de requests/minuto por cuenta — con 50+ collares consultados cada 5 min puede ser necesario agrupar la consulta en un solo endpoint "bulk" si el proveedor lo ofrece, en vez de una llamada por dispositivo.
7. **Persistencia de histórico.** Reutiliza `CollarLocation.Record(...)` y el job de purga existente (`CollarLocationPurgeJob`, >30 días) sin cambios.
8. **Validación end-to-end en staging.** Antes de producción: registrar 1 collar físico de prueba, confirmar que el polling detecta movimiento real y que la latencia entre movimiento físico y actualización en la tab GPS del frontend es aceptable (Tractive tarda hasta 5 min por el intervalo del job — mismo comportamiento esperado aquí).

**Qué puede cambiar el esfuerzo estimado de 1–2 días:** si el proveedor no ofrece SDK/librería HTTP client y hay que armar las firmas de autenticación a mano (HMAC, por ejemplo), o si su API de consulta de posición no es 1:1 por dispositivo sino que requiere una suscripción previa a un webhook de su lado para "habilitar" el polling — ambos casos son preguntas explícitas del checklist de la sección 6.

### Camino B — Push directo HTTP (ya existe el endpoint, pero falta cerrar un gap)

El dispositivo (o el gateway del proveedor) hace `POST /api/collars/pet/{petId}/location` directamente.

- **Esfuerzo:** depende de si el proveedor puede reconfigurar su firmware para apuntar a un servidor propio (poco común en trackers de catálogo cerrados; más viable en módulos crudos o pedidos con firmware personalizado).
- **Gap a cerrar antes de producción:** el endpoint hoy exige JWT de usuario. Embeber un JWT de usuario en la flash de miles de collares es mala práctica (no rota, expira, se filtra una sesión real si se extrae el firmware). Hace falta:
  1. Tabla `CollarDeviceCredentials` (`CollarId`, `ApiKeyHash`, `CreatedAt`, `RevokedAt`).
  2. Nuevo `[AllowAnonymous]` + validación de API key por header (`X-Collar-Key`) en vez de heredar `[Authorize]`.
  3. Reutilizar el patrón ya probado en `ClinicApiKeys` (mismo concepto: secreto de larga vida por entidad, hash en DB, nunca en texto plano).
- **Ventaja a largo plazo:** cero dependencia de la nube del proveedor, cero costo recurrente de licencia de plataforma.

**Recomendación:** arrancar con Camino A para el primer lote (menor riesgo técnico, reutiliza código probado), y evaluar Camino B solo si se pasa a hardware 100% propio (`CollarProvider.Own`).

---

## 4. Variantes de producto a cotizar en el mismo RFQ

La idea es pedirle al mismo proveedor una **familia de productos** sobre una plataforma común (mismo enclosure/base si es posible), no un solo SKU. Esto da flexibilidad de pricing por segmento sin negociar con fabricantes distintos.

| Variante                               | Qué incluye                                                            | Para qué sirve                                                                                         | Impacto en backend                                                                                                        | Impacto en batería                                                                                                         |
| -------------------------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **V1 — Collar + GPS (base)**           | Módulo GPS/LTE-M + GSM, sin extras                                     | Producto de entrada, cubre el caso de uso principal (ubicación en tiempo real)                         | Ninguno adicional — es exactamente lo que ya soporta `CollarProvider.Generic` (Camino A)                                  | Referencia base — la que ya está estimada en `collar.md` (~3–5 días con sleep)                                             |
| **V2 — Collar + GPS + cámara**         | Módulo GPS + cámara de baja resolución (foto periódica o bajo demanda) | Diferenciador premium: "última foto vista" del entorno de la mascota                                   | Requiere endpoint nuevo de ingesta de imagen + almacenamiento en Blob Storage (no existe hoy) — mayor esfuerzo de backend | Alto impacto — la cámara + transmisión de imagen consume mucho más que solo GPS; hay que pedir el consumo real por captura |
| **V3 — Collar + GPS + pantalla e-ink** | Módulo GPS + pantalla e-ink pequeña                                    | Mostrar batería/estado o un QR estático grabado en fábrica sin gastar energía                          | Ninguno adicional al Camino A si la pantalla es solo informativa y no requiere control remoto                             | Bajo impacto si es e-ink (~0 mA en reposo) — rechazar OLED por consumo                                                     |
| **V4 — GPS + cámara + pantalla**       | Combinación completa                                                   | Producto "flagship" a evaluar solo si V2 y V3 individualmente muestran buen resultado en batería/costo | Suma de los requisitos de V2 + V3                                                                                         | El más alto — requiere validación real de muestra antes de comprometer inventario                                          |

**Por qué pedir todas las variantes en el mismo correo:** permite comparar el costo incremental real de cada componente (cámara, pantalla) sobre la misma base de GPS, y decidir con datos de precio/consumo en vez de estimaciones. No implica comprar las 4 — el pedido piloto puede ser solo V1, dejando V2–V4 como opciones de escalamiento una vez validado el mercado.

**Lo que hay que pedir específicamente por variante en el RFQ (ver también la plantilla de correo en la sección 8):**

- **V1 (GPS base):** ver checklist estándar de la sección 6 — es el camino ya validado.
- **V2 (+ cámara):** resolución de imagen, formato de salida (JPEG comprimido, base64, o URL propia si el proveedor tiene su propia nube de imágenes), frecuencia máxima de captura sin drenar la batería en <1 día, tamaño típico de archivo por foto, y si el proveedor ofrece SDK para descargar la imagen o si exige usar su plataforma/app.
- **V3 (+ pantalla e-ink):** tamaño de pantalla disponible, tiempo de refresco, consumo en reposo vs. refresco, si el contenido se puede fijar en fábrica (estático) o si requiere actualizarse por BLE/firmware desde la app.
- **V4 (combo):** todo lo anterior + estimación de batería combinada — no aceptar solo "duración estimada" de marketing, pedir el dato bajo un escenario de uso concreto (ej. "reporte GPS cada 10 min + 1 foto/día + pantalla estática").

---

## 5. Fabricantes candidatos — comparativa para RFQ

| Fabricante                  | API                             | MOQ    | Fortaleza                                                                          | Riesgo                                                               | Prioridad de contacto |
| --------------------------- | ------------------------------- | ------ | ---------------------------------------------------------------------------------- | -------------------------------------------------------------------- | --------------------- |
| **Concox (AT4)**            | REST propietaria, documentada   | 50 u.  | Contacto de ventas ya verificado (`sales@concox.com`), certificaciones FCC/CE/ROHS | Bajo                                                                 | **1º**                |
| **Jimi IoT (JM-VL01/LL01)** | REST + MQTT                     | 50 u.  | Soporta push (MQTT) además de polling — más flexible a futuro                      | Bajo                                                                 | **2º**                |
| **Queclink (GL300)**        | REST + protocolo binario propio | 50 u.  | Hardware muy robusto, miniatura                                                    | Medio — protocolo binario propietario agrega esfuerzo de parseo      | 3º                    |
| **ThinkRace (TK115)**       | REST + WebSocket                | 100 u. | Diseño "pet-specific" ya pensado para collar                                       | Medio-alto — MOQ mayor, menor trayectoria de integración documentada | 4º                    |

Contactar los 4 en paralelo con el mismo correo (plantilla abajo) y decidir según: (a) calidad de la documentación de API que devuelvan, (b) qué variantes (V1–V4) pueden fabricar realmente sobre su plataforma actual, (c) costo total landed a Costa Rica, (d) tiempo de respuesta.

---

## 6. Checklist de lo que se necesita confirmar con cada proveedor

- [ ] ¿La API es REST (HTTP/JSON) o requiere parsear un protocolo binario propietario (ej. GT06/JT808)?
- [ ] ¿Ofrecen webhook/push a un servidor propio, o solo hay que hacer polling contra su nube?
- [ ] ¿Cuáles de las variantes V1–V4 (GPS, GPS+cámara, GPS+pantalla, combo) pueden fabricar sobre la misma plataforma/enclosure?
- [ ] Para V2 (cámara): ¿resolución, formato de imagen, tamaño de archivo, frecuencia máxima de captura sostenible con la batería?
- [ ] Para V3 (pantalla): ¿tamaño y tipo de pantalla e-ink disponible, consumo en reposo/refresco, contenido fijo de fábrica vs. actualizable?
- [ ] ¿Cuál es el consumo de batería estimado por variante y con qué frecuencia de reporte GPS (pedir bajo un escenario concreto, no "duración estimada" genérica)?
- [ ] ¿Certificaciones vigentes (FCC, CE, ROHS) y compatibilidad con bandas LTE-M/NB-IoT usadas en Costa Rica (Kölbi, Movistar, Claro)?
- [ ] ¿MOQ real, precio FCA Shenzhen por unidad en 50/100/500 unidades, por cada variante?
- [ ] ¿Costo y tiempo de entrega de 2–3 muestras por variante antes de confirmar orden?
- [ ] ¿Aceptan personalización de firmware (marca propia, endpoint propio) o es firmware cerrado de fábrica?
- [ ] ¿Qué SIM/plan de datos IoT recomiendan o si el dispositivo viene con eSIM ya activada?
- [ ] ¿Tiempo de producción y lead time de envío a Costa Rica (DHL/FedEx)?

---

## 7. Roadmap de implementación backend (cuando lleguen las specs del proveedor elegido)

1. Crear `I{Proveedor}Service` clonando la forma de `ITractiveService` (autenticación + `GetLatestPositionAsync`) — cubre V1 (GPS base) para cualquier variante.
2. Registrar el nuevo servicio en `InfrastructureServiceCollectionExtensions.cs` con `HttpClientFactory` dedicado.
3. Extender `TractivePollingJob` (o crear un job hermano) para incluir collares con `Provider == CollarProvider.Generic`.
4. Si se confirma la variante V2 (cámara): diseñar endpoint de ingesta de imagen (`POST /api/collars/pet/{petId}/photo`), almacenamiento en Blob Storage y política de retención — no existe hoy, requiere diseño nuevo.
5. Si se confirma la variante V3 (pantalla): implementar `CollarQrBindings` (tabla + endpoint `POST /api/collars/bind-serial`) solo si el contenido de pantalla depende de un serial vinculado dinámicamente a la mascota.
6. Solo si se opta por Camino B (push directo): crear `CollarDeviceCredentials`, cambiar el endpoint de ubicación a `[AllowAnonymous]` + validación de API key por header.

---

## 8. Plantilla de correo para fabricantes (en inglés — estándar B2B con proveedores chinos)

> Enviar el mismo correo a los 4 proveedores de la sección 5, ajustando solo el nombre de contacto y el modelo referenciado. Pide explícitamente las 4 variantes de producto para comparar costos incrementales sobre la misma plataforma.

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

Could you please share the following for each variant that applies:

1. PRODUCT & API (all variants)
   - Is the position/location API REST (HTTP/JSON), or does it use a proprietary
     binary protocol (e.g. GT06, JT808)? Please share full API documentation.
   - Do you support server-to-server webhooks/push notifications to our own
     HTTPS endpoint, or is polling against your cloud platform the only option?
   - Do you offer white-label / OEM firmware that can be configured to report
     to our own server instead of your default platform?

2. CAMERA MODULE (V2 and V4 only)
   - What image resolution and format does the camera capture (JPEG, etc.)?
   - What is the typical file size per photo, and how is the image delivered
     (pushed to our server, pulled via API, or only accessible through your
     platform/app)?
   - What is the maximum sustainable capture frequency without draining the
     battery in under 24 hours?

3. E-INK / E-PAPER DISPLAY (V3 and V4 only)
   - What display sizes are available, and what is the refresh time?
   - What is the power draw at rest vs. during a refresh cycle?
   - Can the displayed content be fixed at the factory (e.g. a static QR code
     or serial number), or does it require updates from our app via BLE/firmware?

4. POWER & CONNECTIVITY (all variants)
   - What is the estimated battery life under normal use (GPS reporting every
     5–10 minutes) for each variant — please provide a concrete usage scenario
     per variant rather than a generic marketing estimate.
   - Do you support LTE-M / NB-IoT bands compatible with Costa Rican carriers
     (Kölbi, Movistar, Claro)?
   - Do units ship with a pre-activated eSIM, or do we need to source and
     activate our own IoT SIM plan?

5. CERTIFICATIONS & QUALITY (all variants)
   - Please confirm current certifications (FCC, CE, ROHS) and IP rating
     (water/dust resistance) for outdoor pet use.

6. COMMERCIAL TERMS (per variant)
   - Minimum order quantity (MOQ) and unit price at 50 / 100 / 500 units
     (FCA Shenzhen), broken down per variant (V1/V2/V3/V4).
   - Cost and lead time for 2–3 samples per variant before we place a full order.
   - Standard production lead time after order confirmation.
   - Do you support custom branding (logo printing, custom packaging)?

We are planning an initial pilot order of approximately 50 units of the base
GPS variant (V1), with the possibility of scaling to 500+ units and adding the
camera or display variants within the first year if the pilot performs well.
We would appreciate a response with pricing, API documentation, and sample
availability at your earliest convenience.

Thank you very much for your time — we look forward to your reply.

Best regards,
[Your Name]
PawTrack CR
[Contact Email] | [Contact Phone/WhatsApp]
```

---

## 9. Referencias internas

- [`docs/collar.md`](./collar.md) — documento maestro de estrategia de hardware, precios y opciones de collar (Tractive, Kippy, hardware propio, OEM).
- [`backend/src/PawTrack.Infrastructure/Collars/TractivePollingJob.cs`](../backend/src/PawTrack.Infrastructure/Collars/TractivePollingJob.cs) — patrón a clonar para el proveedor OEM elegido.
- [`backend/src/PawTrack.API/Controllers/CollarsController.cs`](../backend/src/PawTrack.API/Controllers/CollarsController.cs) — endpoints existentes (`RecordLocation`, registro, historial).
