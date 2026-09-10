# PawTrack CR × Jimi IoT — Documento Técnico y Comercial

> Documento preparado para compartir con **Jimi IoT** como parte de la evaluación de un
> collar GPS para mascotas (OEM/marca blanca) para **PawTrack CR**.
> Última actualización: 2026-09-04
>
> **📌 Estado de la conversación (2026-09-03):** Jimi IoT respondió positivamente al
> RFQ inicial — interesados en una relación de socio tecnológico/fabricante a largo
> plazo, no solo venta de un dispositivo existente. Están consolidando información
> técnica y comercial internamente (producto, ingeniería, ventas) antes de dar
> respuesta estructurada punto por punto. Ver bitácora completa en §8.

---

## 1. Quiénes somos

**PawTrack CR** es una plataforma digital (PWA) de identidad de mascotas y recuperación
de mascotas perdidas, operando en Costa Rica. Los dueños registran a su mascota,
generan un código QR permanente vinculado a un perfil público, y — si la mascota se
pierde — activan un reporte que coordina avistamientos, difusión multicanal, matching
visual por IA y búsqueda en campo en tiempo real.

Nuestro plan **Plus** (de pago) ya integra collares GPS de terceros:

- **Tractive** — vía OAuth2 + polling cada 5 minutos.
- **Collares genéricos/OEM** — vía activación por serial físico + credencial de
  dispositivo (`X-Collar-Key`), con push HTTP directo al servidor.

Estamos evaluando manufactureras para lanzar nuestro propio collar de marca PawTrack
(OEM/marca blanca) como producto físico vendido en Costa Rica, en un modelo de
"bundle" (collar + suscripción). **Jimi IoT** es uno de los fabricantes candidatos
que estamos evaluando para esta línea de producto.

---

## 2. Lo que ya tenemos construido (del lado de PawTrack)

Esto es importante para Jimi IoT: **el backend que recibirá los datos del collar ya
existe y está en producción** — no estamos partiendo de cero. Lo que necesitamos de
Jimi IoT es hardware + firmware compatible con nuestro protocolo de ingesta (o, si su
plataforma lo soporta, adaptarnos a la de ustedes).

| Componente                                              | Estado                                                                                     |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| Modelo de datos del collar (`Collar`, `CollarLocation`) | ✅ En producción                                                                           |
| Activación por serial físico (`CollarTag`)              | ✅ En producción — formato `PT-[4 hex]-[7 dígitos]`, grabado láser en la carcasa           |
| Autenticación de dispositivo (`X-Collar-Key`)           | ✅ En producción — credencial hasheada (SHA-256), nunca en texto plano en la base de datos |
| Endpoint de ingesta HTTP                                | ✅ En producción — ver §3                                                                  |
| Dashboard de inventario/admin                           | ✅ En producción — activar, revocar, métricas de collares                                  |
| Alertas de conectividad y batería baja                  | ✅ En producción                                                                           |
| Modo perdido, zonas seguras (geofencing)                | ✅ En producción                                                                           |
| Transferencia segura entre dueños (handover)            | ✅ En producción                                                                           |

---

## 3. Nuestro protocolo de ingesta (lo que el collar debe hablar)

Preferimos la opción más simple para el firmware: **HTTP POST periódico** (polling
saliente desde el dispositivo), no requerimos que Jimi IoT implemente nada del lado
del servidor — el collar (o su gateway/SIM) llama directamente a nuestro endpoint.

### 3.1 Activación (una sola vez, en fábrica o en la app del usuario)

Cada collar se fabrica con un **serial único** grabado en la carcasa (o impreso en una
etiqueta/QR dentro de la caja). El usuario final activa el collar desde la app
PawTrack escaneando o ingresando ese serial. Nuestro servidor genera entonces una
**API key de dispositivo** (`collarApiKey`) que el collar debe usar en cada request
subsecuente.

**Cómo la key llega al dispositivo** (dos opciones, a validar con Jimi IoT cuál es
factible con su hardware/firmware):

- **Opción A — BLE (preferida):** al encender por primera vez, el collar entra en modo
  pairing. La app PawTrack envía `{ collarApiKey, serverUrl }` vía GATT Write. El
  firmware guarda la key en almacenamiento no volátil (NVS/flash).
- **Opción B — Aprovisionamiento en fábrica:** si el collar no tiene BLE, la key se
  puede generar y quemar en fábrica junto con el serial (requeriría una llamada a
  nuestra API de aprovisionamiento antes del envío, o un lote pre-generado que
  compartimos con ustedes).

### 3.2 Reporte de ubicación (periódico, cada N minutos)

```http
POST https://pawtrack.cr/api/collars/ingest
Content-Type: application/json
X-Collar-Key: <collarApiKey>

{
  "serial": "PT-3F2A-0001234",
  "lat": 9.928200,
  "lng": -84.090700,
  "batteryPercent": 78,
  "timestamp": "2026-09-03T14:32:10Z",
  "accuracyMeters": 8
}
```

**Respuestas:**

| Código | Significado                                                                                                                                         |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `204`  | Ubicación aceptada y registrada.                                                                                                                    |
| `401`  | `X-Collar-Key` ausente o inválida.                                                                                                                  |
| `422`  | El `serial` en el body no coincide con la credencial usada (posible clonación de key) — el firmware debería reintentar re-lectura del serial local. |

**Notas para el firmware:**

- `timestamp` en ISO 8601 UTC.
- `batteryPercent` entero 0–100.
- `accuracyMeters` opcional, pero muy útil para nuestro filtro de precisión en el mapa.
- Frecuencia recomendada: cada 5 minutos en movimiento, con back-off a 15–30 min en
  reposo (detectado por acelerómetro) para ahorrar batería — ver §5.

### 3.3 Alternativa: webhook/push desde la nube de Jimi IoT

Si el hardware de Jimi IoT solo reporta a su propia plataforma (no directo a
terceros), podemos en su lugar **hacer polling contra la API de Jimi IoT** cada
5 minutos desde nuestro backend (ya tenemos este patrón implementado para Tractive).
En ese caso necesitaríamos de Jimi IoT:

- Documentación completa de su API REST (o especificación MQTT si aplica).
- Si es OAuth2: `client_id`/`client_secret` y flujo de autorización.
- Si es API key estática: cómo se aprovisiona por dispositivo/cuenta.
- Confirmación de rate limits (necesitamos poder consultar 50+ dispositivos cada
  5 minutos sin fricción, con posibilidad de un endpoint "bulk"/batch).

---

## 4. Variantes de producto a cotizar

Nos interesa comparar el costo incremental real de distintas variantes construidas
sobre la misma plataforma base:

| Variante                         | Qué incluye                     | Prioridad para el piloto inicial |
| -------------------------------- | ------------------------------- | -------------------------------- |
| **V1 — GPS base**                | GPS + LTE-M/NB-IoT              | ✅ Alta — piloto de 50 unidades  |
| **V2 — GPS + cámara**            | GPS + cámara de baja resolución | Media — evaluación año 1         |
| **V3 — GPS + pantalla e-ink**    | GPS + display e-ink pequeño     | Media — evaluación año 1         |
| **V4 — GPS + cámara + pantalla** | Combinación completa            | Baja — roadmap futuro            |

---

## 5. Preguntas para Jimi IoT (RFQ)

### 5.1 Producto y API

1. ¿Su API es REST (HTTP/JSON) o protocolo propietario (¿MQTT, GT06, JT808?)?
   Favor compartir documentación técnica completa.
2. ¿Soportan push/webhook hacia un endpoint HTTPS propio (nuestro `POST
/api/collars/ingest`), o el único camino es hacer polling contra su plataforma
   en la nube?
3. ¿Ofrecen firmware white-label/OEM configurable para reportar a un servidor
   propio (el nuestro), en vez de únicamente a la nube de Jimi IoT?
4. Modelo de referencia: **JM-VL01 / LL01** — ¿siguen siendo los modelos vigentes
   recomendados para un collar de mascota? ¿Hay un modelo más nuevo que recomienden?

### 5.2 Cámara (solo variantes V2 y V4)

5. Resolución de imagen, formato (¿JPEG?), tamaño típico de archivo por foto.
6. ¿Cómo se entrega la imagen — push a nuestro servidor, pull vía su API, o solo
   disponible a través de su plataforma/app?
7. Frecuencia máxima de captura sostenible sin agotar la batería en menos de 24h.

### 5.3 Pantalla e-ink (solo variantes V3 y V4)

8. Tamaños de pantalla disponibles, tiempo de refresco.
9. Consumo en reposo vs. durante un ciclo de refresco.
10. ¿El contenido puede fijarse en fábrica (QR estático), o requiere actualización
    vía BLE/firmware cada vez que cambia?

### 5.4 Energía y conectividad

11. Autonomía de batería estimada por variante, bajo un escenario concreto: reporte
    de GPS cada 5–10 minutos (no una cifra de marketing genérica).
12. ¿Bandas LTE-M / NB-IoT compatibles con operadores de Costa Rica (Kölbi,
    Movistar, Claro)?
13. ¿El collar viene con eSIM pre-activado, o nosotros proveemos nuestra propia SIM
    IoT (ej. Emnify, Hologram)?
14. ¿Tiene acelerómetro/sensor de movimiento para reducir frecuencia de reporte GPS
    en reposo (ahorro de batería)? ¿Soporta "wake on motion"?

### 5.5 Certificaciones y calidad

15. Certificaciones vigentes (FCC, CE, ROHS) y clasificación IP (resistencia a agua/polvo).
16. ¿El serial/IMEI puede grabarse láser en la carcasa en fábrica, con un formato
    que nosotros definamos (`PT-XXXX-NNNNNNN`)?

### 5.6 Términos comerciales (por variante)

17. MOQ y precio unitario a 50 / 100 / 500 unidades (FCA Shenzhen), por variante.
18. Costo y tiempo de entrega de 2–3 muestras por variante.
19. Tiempo de producción estándar tras confirmar orden.
20. ¿Ofrecen marca blanca (logo, empaque personalizado)?

---

## 6. Plan de piloto

Estamos planificando un piloto inicial de **~50 unidades (variante V1)**, con
posible escalamiento a 500+ unidades y evaluación de variantes con cámara/pantalla
dentro del primer año, sujeto a los resultados del piloto.

**Cronograma tentativo:**

```
Semana 1     → Solicitar 2-3 muestras, validar GPS/batería/resistencia al agua
Semana 2–3   → Integrar como proveedor "Generic" en nuestro backend (ya soportado),
               confirmar que el reporte de ubicación llega correctamente
Semana 4     → Confirmar orden de 50 unidades
Semana 6–8   → Producción + envío + aduana + activación de SIMs + QA
```

---

## 7. Contacto

**PawTrack CR**
Denis Avila Umaña
[correo] · [WhatsApp/teléfono]
https://pawtrack.cr

---

## 8. Bitácora de conversación

### 2026-09-03 — Respuesta inicial de Jimi IoT al RFQ

**Resumen de su respuesta:**

- **Interés confirmado en partnership a largo plazo** — leyeron el RFQ como una
  propuesta de socio tecnológico/fabricante, no solo compra de un tracker existente.
  Buena señal para negociar términos OEM/ODM más adelante.
- **Integración con servidor:** confirman soporte de **RESTful API y MQTT**, con
  experiencia previa integrando con plataformas de terceros. Están dispuestos a
  evaluar **comunicación directa dispositivo → nuestro propio backend** (es decir,
  el Camino B / push directo a `POST /api/collars/ingest` que ya tenemos
  implementado — ver `collarFinal.md` §4), dejando a PawTrack el control completo
  de la app, backend y datos. También evaluarán requisitos específicos si
  necesitamos Azure IoT Hub o infraestructura MQTT administrada por nosotros.
- **OEM/ODM:** confirman branding de producto, empaque, manuales personalizados,
  nombre del dispositivo y personalización de firmware como parte de su oferta
  estándar. Personalización de **hardware más profunda** requiere que su equipo de
  ingeniería evalúe requisitos, volúmenes y alcance antes de confirmar viabilidad,
  MOQ y costos de ingeniería — es decir, no es gratis ni inmediato, hay que llegar
  con specs concretas.
- **Sobre las variantes V1–V4:** algunas se cubren con su portafolio actual; otras
  — mencionan explícitamente **NFC, pantalla E-Ink, cámara y un dispositivo
  totalmente custom** — probablemente requieren desarrollo adicional. Van a evaluar
  cada variante individualmente y recomendarnos la solución más cercana.
  _(Nota: nosotros no propusimos NFC en las variantes V1–V4 de este documento —
  puede que lo hayan inferido del roadmap de collar propio en `collarFinal.md`, o
  que lo mencionen como capacidad general de su portafolio. Aclarar en la próxima
  respuesta si preguntan por esto.)_
- **No darán estimados sin validar internamente** — conectividad, autonomía de
  batería, specs de cámara, certificaciones, MOQ, precios y plazos de entrega
  quedan pendientes de una revisión interna con sus equipos de producto,
  ingeniería y ventas.
- **Validan nuestro roadmap de fases** (50 u. → 500+ u. → hardware NALA
  personalizado) como un enfoque práctico: proponen identificar primero la
  solución existente más cercana para la Fase 1 (piloto), validar en mercado, y
  luego migrar gradualmente a desarrollo OEM/ODM más profundo.
- **Interés geográfico alineado:** mencionan que ya tienen presencia activa en
  Centroamérica/LatAm, lo cual coincide con nuestro plan de expansión post-Costa
  Rica.
- **Próximo paso de su lado:** van a consolidar la información técnica y
  comercial y enviarnos una respuesta estructurada punto por punto (referenciando
  las preguntas de §5 de este documento).

**Próximos pasos de nuestro lado (pendiente):**

- [x] Esperar la respuesta estructurada de Jimi IoT a las preguntas de §5. **Recibida 2026-09-10, ver entrada de esa fecha más abajo.**
- [x] Cuando llegue, decidir si el **Camino B (push directo)** — que ellos ya
      confirmaron poder soportar — se vuelve la ruta preferida para el piloto en
      vez del Camino A (polling), ya que evitaría depender de la nube de Jimi IoT
      para leer posiciones. **Actualización 2026-09-04:** confirmado independientemente
      contra la documentación pública de su plataforma TrackSolid Pro
      (`tracksolidprodocs.jimicloud.com`) — su Open API ya expone tanto Camino A
      (`jimi.device.location.get`, polling) como Camino B (`/location/push`, webhook
      directo a nuestra URL) de forma nativa, sin depender de que confirmen nada
      adicional por firmware custom. Ver análisis completo en `collarFinal.md` §3.3.1.
- [ ] Si preguntan por NFC específicamente, aclarar que no es un requisito de la
      Fase 1/2 (V1–V4) — es una idea de roadmap futuro mencionada en
      `collarFinal.md`, no una especificación formal enviada a fabricantes.
- [ ] Preparar specs concretas (volumen esperado año 1, escenario de uso real de
      batería) para cuando pidan detalle antes de cotizar personalización de
      hardware.

### 2026-09-04 — Viabilidad de TrackSolid Pro / Jimi Life (análisis independiente)

Mientras esperamos la respuesta estructurada de Jimi IoT, se analizó directamente
la documentación pública de sus dos productos de plataforma para no depender
solo de lo que ellos confirmen por correo:

- **TrackSolid Pro** (`tracksolidprodocs.jimicloud.com`) es su Open API B2B real
  y madura — modela dispositivos `mcTypeUseScope: "pet"` explícitamente, soporta
  Camino A y Camino B nativamente (ver detalle en `collarFinal.md` §3.3.1). Es una
  ruta de integración viable **hoy**, en paralelo a la negociación OEM/ODM de
  hardware propio.
- **Jimi Life** es su app de consumidor final (equivalente a "la app de
  Tractive") — no tiene API propia y no es un punto de integración. Si un
  collar se aprovisiona solo hacia Jimi Life, no tendríamos acceso programático
  a los datos. Descartado como ruta de integración.

**Próximo paso:** si la respuesta estructurada de Jimi IoT tarda o no cubre push
directo con suficiente detalle, evaluar arrancar el piloto directamente contra
TrackSolid Pro (Camino A, clonando `TractivePollingJob`) sin esperar más — ya
confirmamos independientemente que su plataforma lo soporta.

### 2026-09-04 — Correo de seguimiento enviado: alinear el piloto con TrackSolid Pro

Se envió un correo de seguimiento en el mismo hilo del RFQ (asunto: "Re: RFQ —
GPS Pet Tracker Collar (OEM/Custom Branding) — Aligning on TrackSolid Pro for
the pilot"), sin esperar la respuesta estructurada de Jimi IoT a §5, para no
bloquear la validación técnica. Pide, en orden:

1. **Cuenta y acceso**: cómo obtener una cuenta distribuidor/reseller de
   TrackSolid Pro con `appKey`/`appSecret` para nuestro backend (no la app
   Jimi Life), si el acceso a la API tiene costo recurrente separado del
   hardware, y qué nodo regional usar desde Costa Rica (US/EU/HK-SG).
2. **Ruta de integración**: confirmar que el modelo V1 (piloto) se puede
   aprovisionar bajo `mcTypeUseScope = "pet"`, preferencia por Camino B
   (`/location/push`, `/api/v1/tag/data/push`) con pasos exactos de
   configuración del dispositivo, y si existe firma/secreto compartido para
   verificar el origen del webhook (si no, pedir su rango de IPs de salida
   para allowlist). Camino A (`jimi.device.location.get`, polling) queda como
   respaldo si el push no está listo para el modelo V1.
3. **Piloto**: si los ~50 collares se pueden pre-activar en TrackSolid Pro
   antes del envío o se activan por serial/IMEI al recibirlos, y si pueden dar
   una cuenta sandbox/de prueba ya mismo para validar la integración antes de
   confirmar la orden de hardware.

Se aclaró que esto corre en paralelo a la conversación OEM/ODM de largo plazo,
sin reemplazarla.

**Pendiente:** respuesta de Jimi IoT a este correo de seguimiento + su
respuesta estructurada original a §5.

### 2026-09-10 — Respuesta estructurada completa de Jimi IoT (RFQ §5 + seguimiento TrackSolid Pro)

Jimi IoT respondió punto por punto tanto al RFQ original (§5 de este documento)
como al correo de seguimiento sobre TrackSolid Pro. Resumen completo — detalle
técnico y comercial del producto recomendado (AL600) en `collarFinal.md` §3.3.2.

**1. Arquitectura e integración:**

- El AL600 permite configurar un servidor propio (IP/Dominio + Puerto) para que
  el dispositivo envíe datos directo a infraestructura gestionada por NALA —
  **pero** la versión estándar **no soporta MQTT ni HTTPS API**; hablar JSON/HTTPS
  o MQTT con nuestro backend requiere **desarrollo de firmware personalizado**,
  no viene de fábrica en el modelo recomendado para el piloto.
- **Confirman explícitamente que JimiLife (su app) no soporta APIs, pero
  TrackSolid Pro sí** — validación directa de nuestro análisis independiente
  del 2026-09-04 (`collarFinal.md` §3.3.1).
- No hay integración nativa con Azure IoT Hub — se evaluaría solo dentro de un
  desarrollo personalizado.
- Mencionan una plataforma adicional, **TurboHive**, que facilita integración
  con la plataforma del cliente — pendiente de más detalle, no evaluada aún.
- SDK propio disponible (costo adicional) si en el futuro queremos una app
  100% propia en lugar de JimiLife/TrackSolid Pro.

**2. Producto recomendado para el piloto (Fase 1): modelo AL600**

Conectividad LTE Cat.1 (sin LTE-M/NB-IoT), SIM embebida, GPS+BDS+WiFi+LBS+BT+A-GPS,
batería hasta 10 días activo / 60 días standby, IP67, historial 7 días, triple
geocerca. **MOQ estándar: 100 unidades** (más alto que las 50 planeadas). Precio
de muestra $32 USD (incluye 1 año de datos, 30MB/mes, sin licencia de
plataforma). Licencia JimiLife: $3.50 USD/dispositivo primer año, $6.00 USD
renovación anual — pendiente confirmar si aplica igual bajo TrackSolid Pro.
Lead times: 3 días si hay stock, 30–45 días si no; personalización ~10 días
(muestra) + ~45 días (producción) tras aprobar muestra. Tabla completa de MOQs
por nivel de personalización en `collarFinal.md` §3.3.2.

**3. Capacidades OEM confirmadas:** logo, empaque, manuales, nombre de
dispositivo, QR, formato de seriales — todo estándar. Personalización de
firmware requiere evaluación previa de su equipo de ingeniería (no automático).

**4. Roadmap ODM (hardware propio, largo plazo):** modelo ODM integral
(diseño+hardware+firmware+certificaciones+manufactura, 300+ ingenieros I+D,
8M dispositivos/año, clientes Fortune 500). **Ya tienen una línea propia de
"smart pet collar" wearable con IA de cuidados** — precedente directo
relevante. MOQ ODM típico desde 1,000 u. Proceso: requisitos conjuntos →
diseño → prototipo/validación → refinamiento → producción/QC → serie →
soporte. **Ofrecen firmar NDA** para compartir documentación de protocolo más
profunda — paso pendiente si se explora firmware custom u ODM.

**Próximos pasos de nuestro lado:**

- [x] Decidir si negociamos MOQ 100→50, o ajustamos el piloto a 100 unidades. **Enviado en correo de seguimiento 2026-09-10, ver entrada de esa fecha abajo.**
- [x] Confirmar costo de licencia de plataforma bajo TrackSolid Pro (no solo JimiLife). **Ídem.**
- [x] Evaluar firmar el NDA si se quiere profundizar en protocolo/firmware custom u ODM. **Ídem — pedimos su borrador estándar.**
- [x] Validar cobertura/costo real de LTE Cat.1 con Kölbi/Movistar/Claro antes de comprometer el piloto. **Ídem — pedido directamente a Jimi IoT.**
- [ ] Aún pendiente: respuesta específica sobre acceso a cuenta TrackSolid Pro (appKey/appSecret, nodo regional, costo de API) del correo de seguimiento — no vino en esta respuesta, insistir en el próximo intercambio. **Reiterado en el correo de seguimiento 2026-09-10.**

### 2026-09-10 — Segundo correo de seguimiento enviado: MOQ, licencia TrackSolid Pro, cuenta, LTE Cat.1, NDA

En el mismo hilo (asunto: "Re: RFQ — GPS Pet Tracker Collar (OEM/Custom
Branding) — AL600 pilot & TrackSolid Pro follow-up"), en respuesta directa a
la respuesta estructurada del mismo día. Pide, en orden:

1. **MOQ del piloto**: si hay flexibilidad para arrancar en 50 unidades (aunque
   sea a precio unitario algo más alto) en vez del MOQ estándar de 100.
2. **Licencia de plataforma bajo TrackSolid Pro**: si el esquema de $3.50
   (primer año) / $6.00 (renovación) por dispositivo cotizado para JimiLife
   aplica igual integrando vía TrackSolid Pro, o tiene un precio distinto.
3. **Acceso a cuenta TrackSolid Pro** (reiterado, no respondido en el correo
   anterior): cuenta distribuidor/reseller con `appKey`/`appSecret`, nodo
   regional recomendado desde Costa Rica (US/EU/HK-SG), activación de las
   unidades piloto bajo `mcTypeUseScope = "pet"`, pasos exactos para habilitar
   polling (`jimi.device.location.get`) o push (`/location/push`) con el
   AL600, y disponibilidad de una cuenta sandbox/de prueba antes de confirmar
   la orden.
4. **Conectividad**: qué operadores costarricenses (Kölbi, Movistar, Claro)
   tienen validados para LTE Cat.1 del AL600, o qué proveedor de SIM IoT
   recomiendan para la región.
5. **NDA**: confirmamos apertura a firmar y pedimos que compartan su borrador
   estándar para revisión.

Se dejó claro que, una vez resueltos los puntos 1–3, quedaríamos listos para
confirmar la orden del piloto.

También se preparó una versión en español, formato WhatsApp, del mismo
contenido (mismos 5 puntos, tono más directo) para enviar por ese canal si el
contacto de Jimi IoT lo prefiere sobre correo.

**Pendiente:** respuesta de Jimi IoT a este segundo correo de seguimiento.

### 2026-09-10 — Adjuntos analizados: deck de producto + datasheet AL600

El correo estructurado vino con dos PDFs adjuntos, ya revisados a fondo (ver
`collarFinal.md` §3.3.3 para la tabla completa):

- **Deck de producto** (8 páginas, marketing): confirma las features ya
  conocidas (posicionamiento híbrido, buzzer/LED de búsqueda, modo "Live
  Finder" de 5s, geocercas Wi-Fi/virtuales, correa virtual Bluetooth,
  reportes de actividad/calorías) y precisa la batería en **530 mAh**.
- **Datasheet técnico** (1 página "Standard Configuration"): ya trae una
  **marca blanca de ejemplo ("PawBasis")** — confirma que el proceso de
  white-label es rutinario para ellos. Corrige la temperatura operativa a
  **-20 °C a +60 °C** (más amplio que el "0–45 °C" mencionado en el correo) y
  lista las **bandas LTE exactas**: B1/B3/B5/B7/B8/B20/B28/B38/B40/B41.

**Hallazgo a validar:** esa lista de bandas no incluye B2 ni B4 (las bandas
históricas más usadas en Latinoamérica), aunque sí incluye B28 y B7 que
Kölbi/Movistar/Claro también usan. Hay que confirmar compatibilidad exacta
antes de comprometer el piloto — se puede preguntar directamente a los
operadores o pedirle a Jimi IoT que lo confirmen contra estas bandas
específicas en el próximo intercambio.
