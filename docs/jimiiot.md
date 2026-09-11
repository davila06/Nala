# PawTrack CR × Jimi IoT — Documento Técnico y Comercial

> Documento preparado para compartir con **Jimi IoT** como parte de la evaluación de un
> collar GPS para mascotas (OEM/marca blanca) para **PawTrack CR**.
> Última actualización: 2026-09-11
>
> **Estado actual:** Jimi IoT aceptó un piloto de 50 unidades del AL600, confirmó
> TrackSolid Pro, `appKey`, licencia de USD 3.50 por dispositivo el primer año y
> conectividad de la versión Latinoamérica con Claro. La cotización oficial con
> envío queda pendiente de los datos del consignatario. Ver resumen y plan en §1.

---

## 1. Resumen ejecutivo al 11 de septiembre de 2026

### Decisión recomendada para el piloto

Avanzar con **50 unidades AL600 versión Latinoamérica**, siempre que la cotización
oficial confirme el precio unitario, flete, Incoterm, cobertura/datos y acceso a
TrackSolid Pro. La integración del piloto debe usar **TrackSolid Pro**, no la app
Jimi Life y no el protocolo binario directo del dispositivo.

El AL600 estándar puede configurarse con un servidor propio, pero no habla JSON
por HTTPS ni MQTT de forma nativa. Por tanto, no es compatible directamente con
el endpoint HTTP actual de PawTrack. TrackSolid Pro es la capa que permite a
PawTrack consultar o recibir los datos usando su API documentada.

### Confirmado por Jimi IoT

| Tema           | Confirmación                                                                                                                 |
| -------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| Piloto         | Aceptan 50 unidades, por debajo del MOQ estándar de 100, con precio de escala inferior a 100                                 |
| Modelo         | AL600, versión Latinoamérica                                                                                                 |
| Plataforma     | TrackSolid Pro, con subcuentas y entrega de `appKey` para integración                                                        |
| Licencia/API   | USD 3.50 por dispositivo durante el primer año; USD 6.00 por dispositivo/año desde el segundo; la API sigue el mismo esquema |
| Conectividad   | SIM embebida compatible con Claro                                                                                            |
| Bandas LTE FDD | B2/B3/B4/B5/B7/B8/B28/B66                                                                                                    |
| Bandas LTE TDD | B38/B40/B41                                                                                                                  |
| Firmware       | Actualización remota por Jimi IoT o local por cable y paquete de firmware                                                    |
| Marca blanca   | Disponible; firmware a medida y ODM requieren evaluación y NDA                                                               |
| Cotización     | La emitirán con envío cuando reciban datos del consignatario                                                                 |

### Pendientes de Jimi IoT antes de emitir la orden

1. Cotización oficial de 50 unidades: precio unitario, Incoterm, flete, seguro,
   plazo, forma de pago, validez y costos de muestra si aplica.
2. Acceso de prueba o cuenta de TrackSolid Pro: nodo regional, creación de
   subcuentas, `appKey`, secretos, documentación y límites de API.
3. Pasos de aprovisionamiento: alta de IMEI/serial, asignación al tenant de
   PawTrack, activación y desactivación de equipos.
4. Esquema de datos/SIM: confirmar que los 30 MB/mes y el año incluido aplican
   al piloto, cobertura efectiva sobre Claro, política de uso justo, sobreconsumo,
   suspensión, reactivación, reemplazo de SIM y soporte.
5. Documentación de seguridad: autenticación de API, rotación de credenciales,
   origen/firma de webhooks si se usaran y lista de IPs de salida.
6. Confirmación de certificaciones aplicables, garantía, RMA, resistencia IP67,
   peso/dimensiones finales y material de empaque.
7. Borrador de NDA para explorar firmware personalizado u ODM, sin bloquear el
   piloto estándar.

### Próximos pasos de PawTrack

1. Esperar la respuesta de Cristina al correo enviado el 2026-09-11, con la
   cotización para 2 muestras y la cotización referencial del lote.
2. Revisar que la respuesta incluya costos totales, documentación aduanera,
   TrackSolid Pro, SIM/datos y condiciones de garantía.
3. Validar el costo total puesto en Costa Rica con un agente aduanal antes de
   pagar: flete, seguro, partida arancelaria, DAI, IVA y trámite.
4. Solicitar cuenta sandbox o de prueba de TrackSolid Pro y completar una prueba
   con datos reales usando las muestras antes de confirmar la orden de 50 unidades.
5. Validar con Jimi el código HS, reglas de origen y certificado para aplicar,
   si corresponde, la preferencia del TLC Costa Rica-China.
6. Elegir el nodo regional tras una prueba básica de latencia y disponibilidad;
   no decidirlo solo por cercanía geográfica.
7. Definir soporte al cliente, política de garantía, reposición y renovación de
   conectividad antes de vender el primer collar.

### ¿Debemos hablar con Claro?

**No como primer bloqueo.** La SIM embebida del AL600 ya fue declarada compatible
con la red Claro. Primero Jimi IoT debe confirmar por escrito que la SIM y sus
datos cubren Costa Rica, quién es el responsable operativo y qué ocurre cuando
vence o excede el paquete.

Hablar con Claro es recomendable como validación adicional o alternativa futura,
no como condición previa a la muestra, para confirmar cobertura LTE Cat.1 en los
cantones piloto y evaluar una SIM IoT local si la SIM de Jimi no ofrece soporte,
precio o control operativo adecuados. Kölbi y Movistar también deben verificarse
antes de prometer cobertura nacional.

### Cambios requeridos en PawTrack para AL600

| Cambio                     | Alcance                                                                                                             | Prioridad |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------- | --------- |
| Adaptador TrackSolid Pro   | Servicio de infraestructura para autenticación, consulta de dispositivos y normalización de posiciones              | Alta      |
| Proveedor `JimiTrackSolid` | Nuevo valor/proveedor de collar o identificador de integración, sin reutilizar `Generic` para datos de terceros     | Alta      |
| Vinculación IMEI/serial    | Guardar el identificador externo de TrackSolid y asociarlo a CollarTag/Pet con ownership validado                   | Alta      |
| Trabajo de sincronización  | Consulta periódica por lotes, idempotencia, backoff y lock distribuido; reutilizar el patrón de ubicación existente | Alta      |
| Normalizador de telemetría | Convertir respuesta TrackSolid a latitud, longitud, precisión, batería y timestamp UTC antes de persistir           | Alta      |
| Estado de conectividad     | Mapear última conexión, batería, alarmas y eventos de geocerca disponibles desde TrackSolid                         | Media     |
| Webhooks TrackSolid        | Evaluar solo tras confirmar firma o allowlist; no exponer el endpoint actual al protocolo binario del AL600         | Media     |
| Gestión de credenciales    | `appKey` y secreto solo en Key Vault; nunca en frontend, BD sin cifrar o documentación                              | Alta      |
| Pruebas                    | Integración simulada, ownership/BOLA, duplicados, errores API, datos fuera de rango y pérdida de conexión           | Alta      |

**No se requiere** firmware personalizado, Azure IoT Hub, MQTT ni una SIM propia
para validar el primer piloto si TrackSolid Pro y la SIM embebida cumplen lo
confirmado. Esas opciones pertenecen a una fase posterior de firmware propio u ODM.

---

## 2. Quiénes somos

**PawTrack CR** es una plataforma digital (PWA) de identidad de mascotas y recuperación
de mascotas perdidas, operando en Costa Rica. Los dueños registran a su mascota,
generan un código QR permanente vinculado a un perfil público, y — si la mascota se
pierde — activan un reporte que coordina avistamientos, difusión multicanal, matching
visual por IA y búsqueda en campo en tiempo real.

El plan **Plus** habilita collares GPS. El flujo operativo activo usa collares
genéricos/OEM por serial físico y credencial de dispositivo. Las integraciones
externas directas, incluida Tractive, permanecen deshabilitadas hasta una
decisión comercial e integración aprobada.

Estamos evaluando manufactureras para lanzar nuestro propio collar de marca PawTrack
(OEM/marca blanca) como producto físico vendido en Costa Rica, en un modelo de
"bundle" (collar + suscripción). **Jimi IoT** es uno de los fabricantes candidatos
que estamos evaluando para esta línea de producto.

---

## 3. Lo que ya tenemos construido (del lado de PawTrack)

Esto es importante para Jimi IoT: **el backend y la interfaz para administrar
collares ya existen** — no estamos partiendo de cero. Para el AL600, PawTrack
se adaptará a TrackSolid Pro; no se presupone que el firmware estándar hable el
protocolo HTTP nativo de PawTrack.

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

## 4. Integración de telemetría para el piloto AL600

El AL600 estándar no habla JSON por HTTPS ni MQTT de forma nativa. Para el piloto,
PawTrack debe consumir TrackSolid Pro mediante su API y normalizar la telemetría
antes de persistirla. El endpoint HTTP actual queda disponible para prototipos u
OEM que cumplan su contrato, pero no es el canal directo del AL600 estándar.

### 4.1 Activación y asociación de dispositivo

Cada collar se fabrica con un **serial único** grabado en la carcasa (o impreso en una
etiqueta/QR dentro de la caja). El usuario final activa el collar desde la app
PawTrack escaneando o ingresando ese serial. Nuestro servidor genera entonces una
**API key de dispositivo** (`collarApiKey`) que el collar debe usar en cada request
subsecuente.

Para AL600, la activación debe ocurrir en TrackSolid Pro usando IMEI/serial y
asociando el equipo a la cuenta o subcuenta de PawTrack. La clave de dispositivo
`X-Collar-Key` solo aplica al canal HTTP nativo de PawTrack; no debe intentarse
aprovisionarla en firmware estándar del AL600 sin confirmar un desarrollo a medida.

- **Opción A — BLE (preferida):** al encender por primera vez, el collar entra en modo
  pairing. La app PawTrack envía `{ collarApiKey, serverUrl }` vía GATT Write. El
  firmware guarda la key en almacenamiento no volátil (NVS/flash).
- **Opción B — Aprovisionamiento en fábrica:** si el collar no tiene BLE, la key se
  puede generar y quemar en fábrica junto con el serial (requeriría una llamada a
  nuestra API de aprovisionamiento antes del envío, o un lote pre-generado que
  compartimos con ustedes).

### 4.2 Canal HTTP nativo de PawTrack

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

### 4.3 TrackSolid Pro: consulta o webhook de API

Para AL600, TrackSolid Pro es la ruta de corto plazo. PawTrack puede consultar
ubicaciones por lotes desde el backend y, posteriormente, evaluar un webhook de
la API cuando Jimi documente autenticación, firma o rangos de IP de salida.
Necesitamos de Jimi IoT:

- Documentación completa de su API REST (o especificación MQTT si aplica).
- Si es OAuth2: `client_id`/`client_secret` y flujo de autorización.
- Si es API key estática: cómo se aprovisiona por dispositivo/cuenta.
- Confirmación de rate limits (necesitamos poder consultar 50+ dispositivos cada
  5 minutos sin fricción, con posibilidad de un endpoint "bulk"/batch).

---

## 5. Variantes de producto a cotizar

Nos interesa comparar el costo incremental real de distintas variantes construidas
sobre la misma plataforma base:

| Variante                         | Qué incluye                     | Prioridad para el piloto inicial |
| -------------------------------- | ------------------------------- | -------------------------------- |
| **V1 — GPS base**                | GPS + LTE Cat.1                 | ✅ Alta — piloto de 50 unidades  |
| **V2 — GPS + cámara**            | GPS + cámara de baja resolución | Media — evaluación año 1         |
| **V3 — GPS + pantalla e-ink**    | GPS + display e-ink pequeño     | Media — evaluación año 1         |
| **V4 — GPS + cámara + pantalla** | Combinación completa            | Baja — roadmap futuro            |

---

## 6. Preguntas para Jimi IoT (RFQ)

### 6.1 Producto y API

1. ¿Su API es REST (HTTP/JSON) o protocolo propietario (¿MQTT, GT06, JT808?)?
   Favor compartir documentación técnica completa.
2. ¿Soportan push/webhook hacia un endpoint HTTPS propio (nuestro `POST
/api/collars/ingest`), o el único camino es hacer polling contra su plataforma
   en la nube?
3. ¿Ofrecen firmware white-label/OEM configurable para reportar a un servidor
   propio (el nuestro), en vez de únicamente a la nube de Jimi IoT?
4. Modelo de referencia: **JM-VL01 / LL01** — ¿siguen siendo los modelos vigentes
   recomendados para un collar de mascota? ¿Hay un modelo más nuevo que recomienden?

### 6.2 Cámara (solo variantes V2 y V4)

- **5.** Resolución de imagen, formato (¿JPEG?), tamaño típico de archivo por foto.
- **6.** ¿Cómo se entrega la imagen — push a nuestro servidor, pull vía su API, o solo
  disponible a través de su plataforma/app?
- **7.** Frecuencia máxima de captura sostenible sin agotar la batería en menos de 24h.

### 6.3 Pantalla e-ink (solo variantes V3 y V4)

- **8.** Tamaños de pantalla disponibles, tiempo de refresco.
- **9.** Consumo en reposo vs. durante un ciclo de refresco.
- **10.** ¿El contenido puede fijarse en fábrica (QR estático), o requiere actualización
  vía BLE/firmware cada vez que cambia?

### 6.4 Energía y conectividad

- **11.** Autonomía de batería estimada por variante, bajo un escenario concreto: reporte
  de GPS cada 5–10 minutos (no una cifra de marketing genérica).
- **12.** ¿Bandas LTE Cat.1 de la versión Latinoamérica y cobertura validada con
  operadores de Costa Rica (Kölbi, Movistar, Claro)?
- **13.** ¿El collar viene con eSIM pre-activado, o nosotros proveemos nuestra propia SIM
  IoT (ej. Emnify, Hologram)?
- **14.** ¿Tiene acelerómetro/sensor de movimiento para reducir frecuencia de reporte GPS
  en reposo (ahorro de batería)? ¿Soporta "wake on motion"?

### 5.5 Certificaciones y calidad

- **15.** Certificaciones vigentes (FCC, CE, ROHS) y clasificación IP (resistencia a agua/polvo).
- **16.** ¿El serial/IMEI puede grabarse láser en la carcasa en fábrica, con un formato
  que nosotros definamos (`PT-XXXX-NNNNNNN`)?

### 5.6 Términos comerciales (por variante)

- **17.** MOQ y precio unitario a 50 / 100 / 500 unidades (FCA Shenzhen), por variante.
- **18.** Costo y tiempo de entrega de 2–3 muestras por variante.
- **19.** Tiempo de producción estándar tras confirmar orden.
- **20.** ¿Ofrecen marca blanca (logo, empaque personalizado)?

---

## 7. Plan de piloto

Estamos planificando un piloto inicial de **~50 unidades (variante V1)**, con
posible escalamiento a 500+ unidades y evaluación de variantes con cámara/pantalla
dentro del primer año, sujeto a los resultados del piloto.

**Cronograma tentativo:**

```text
Semana 1     → Solicitar 2-3 muestras, validar GPS/batería/resistencia al agua
Semana 2–3   → Integrar TrackSolid Pro, asociar IMEI/serial y confirmar consulta
               de ubicación, batería y timestamp con datos reales
Semana 4     → Confirmar orden de 50 unidades
Semana 6–8   → Producción + envío + aduana + activación en TrackSolid Pro + QA
```

---

## 8. Contacto

**PawTrack CR**
Denis Avila Umaña
[correo] · [WhatsApp/teléfono]
[pawtrack.cr](https://pawtrack.cr)

---

## 9. Bitácora de conversación

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

Se envió un correo de seguimiento en el mismo hilo del RFQ, sin esperar la
respuesta estructurada de Jimi IoT a §6, para no
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

#### 2. Producto recomendado para el piloto: modelo AL600

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

### 2026-09-10 — Segundo correo de seguimiento enviado: MOQ, licencia TrackSolid Pro, cuenta, LTE Cat.1 y NDA

En el mismo hilo, en respuesta directa a la respuesta estructurada del mismo
día, se solicitó lo siguiente:

1. **MOQ del piloto**: si hay flexibilidad para arrancar en 50 unidades (aunque
   sea a precio unitario algo más alto) en vez del MOQ estándar de 100.
2. **Licencia de plataforma bajo TrackSolid Pro**: si el esquema de $3.50
   (primer año) / $6.00 (renovación) por dispositivo cotizado para JimiLife
   aplica igual integrando vía TrackSolid Pro, o tiene un precio distinto.
3. **Acceso a cuenta TrackSolid Pro** (reiterado, no respondido en el correo
   anterior): cuenta de distribuidor con `appKey`/`appSecret`, nodo
   regional recomendado desde Costa Rica (US/EU/HK-SG), activación de las
   unidades piloto bajo `mcTypeUseScope = "pet"`, pasos exactos para habilitar
   consulta periódica (`jimi.device.location.get`) o notificación (`/location/push`) con el
   AL600, y disponibilidad de una cuenta de prueba antes de confirmar
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

### 2026-09-11 — Respuesta al segundo seguimiento: piloto y conectividad confirmados

Jimi IoT respondió a los puntos que quedaban del segundo seguimiento. Esta
entrada reemplaza cualquier dato anterior contradictorio de la bitácora.

- **Piloto:** aceptan 50 unidades, aunque el MOQ estándar sea 100. El precio
  final será el aplicable a pedidos inferiores a 100 unidades y se incluirá en
  la cotización oficial.
- **TrackSolid Pro:** licencia y API cuestan USD 3.50 por dispositivo durante
  el primer año y USD 6.00 por dispositivo/año desde el segundo. Pueden crear
  subcuentas y entregar `appKey`; la documentación de integración queda por
  recibir.
- **Conectividad:** la SIM embebida del AL600 es compatible con Claro. La
  versión Latinoamérica soporta LTE FDD B2/B3/B4/B5/B7/B8/B28/B66 y LTE TDD
  B38/B40/B41. Este dato reemplaza la lista limitada del datasheet anterior.
- **Firmware y NDA:** aceptan NDA; pueden actualizar firmware remotamente o
  por cable. El firmware personalizado y ODM se evaluarán cuando PawTrack
  entregue requisitos específicos.
- **Cotización:** solicitan nombre de empresa, dirección, persona de contacto,
  teléfono y datos del consignatario para emitir la cotización con envío.

**Pendiente inmediato:** entregar datos del consignatario y solicitar por
escrito precio de 50 unidades, Incoterm, flete, impuestos estimados, acceso de
prueba TrackSolid Pro, documentación, gestión de SIM/datos y aprovisionamiento
por IMEI/serial.

### 2026-09-11 — Correo enviado a Cristina: 2 muestras antes del lote

Se envió a Cristina la respuesta comercial para continuar el seguimiento. La
solicitud establece que PawTrack desea recibir primero **2 dispositivos de
muestra** del AL600 versión Latinoamérica, con la misma configuración de
hardware, firmware, SIM embebida y bandas prevista para el lote de 50.

La respuesta solicita para las muestras:

- Precio unitario y costo total puesto en Costa Rica, incluyendo flete, seguro,
  courier, preparación, configuración, activación, serialización, empaque y
  cualquier cargo por pedido pequeño.
- Separación de arancel potencial, IVA, cargos aduaneros y otros costos de
  destino, sin asumir que el TLC exonera automáticamente todos los impuestos.
- Confirmación de 30 MB/mes, primer año de datos, vigencia y costos de
  TrackSolid Pro/API.
- Activación por IMEI/serial, cuenta de prueba o sandbox, nodo regional,
  documentación, límites de API y procedimiento de aprovisionamiento.
- Garantía, soporte y reemplazo de una muestra defectuosa.

También se solicita una cotización referencial separada para 50 unidades, pero
se deja claro que la orden quedará condicionada a los resultados de las pruebas
de las muestras.

Para el TLC Costa Rica-China se pide a Jimi IoT confirmar código HS, país y
reglas de origen, certificado de origen, documentos comerciales y si la
preferencia puede aplicarse tanto a las 2 muestras como al lote. La aplicación
final queda sujeta a revisión de un agente aduanal en Costa Rica.

La respuesta no incluye credenciales ni secretos; cualquier `appKey` o secreto
debe entregarse por un canal seguro.

**Siguiente hito:** esperar la cotización y la documentación de Jimi IoT. No
confirmar ni pagar las 50 unidades hasta recibir y probar las muestras, validar
TrackSolid Pro, cobertura, batería, GPS y el costo final de importación.

### 2026-09-10 — Adjuntos analizados: presentación de producto y ficha técnica AL600

El correo estructurado vino con dos PDFs adjuntos, ya revisados a fondo (ver
`collarFinal.md` §3.3.3 para la tabla completa):

- **Presentación de producto** (8 páginas, comercial): confirma las funciones
  ya conocidas (posicionamiento híbrido, buzzer/LED de búsqueda, modo de
  localización en vivo de 5 s, geocercas Wi-Fi/virtuales, correa virtual Bluetooth,
  reportes de actividad/calorías) y precisa la batería en **530 mAh**.
- **Ficha técnica** (1 página de configuración estándar): ya trae una
  **marca blanca de ejemplo ("PawBasis")** — confirma que el proceso de
  marca blanca es rutinario para ellos. Corrige la temperatura operativa a
  **-20 °C a +60 °C** (más amplio que el "0–45 °C" mencionado en el correo) y
  lista las **bandas LTE exactas**: B1/B3/B5/B7/B8/B20/B28/B38/B40/B41.

**Actualización:** la respuesta del 2026-09-11 confirma que la versión
Latinoamérica sí incluye B2 y B4, además de B3/B5/B7/B8/B28/B66 y las bandas
TDD indicadas. La compatibilidad declarada con Claro queda confirmada; todavía
debe validarse cobertura en los cantones piloto y compatibilidad con Kölbi y
Movistar antes de ofrecer cobertura nacional.
