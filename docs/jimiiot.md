# PawTrack CR × Jimi IoT — Documento Técnico y Comercial

> Documento preparado para compartir con **Jimi IoT** como parte de la evaluación de un
> collar GPS para mascotas (OEM/marca blanca) para **PawTrack CR**.
> Última actualización: 2026-09-03

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

| Componente                                    | Estado                                                                |
| ---------------------------------------------- | ---------------------------------------------------------------------- |
| Modelo de datos del collar (`Collar`, `CollarLocation`) | ✅ En producción                                                |
| Activación por serial físico (`CollarTag`)    | ✅ En producción — formato `PT-[4 hex]-[7 dígitos]`, grabado láser en la carcasa |
| Autenticación de dispositivo (`X-Collar-Key`) | ✅ En producción — credencial hasheada (SHA-256), nunca en texto plano en la base de datos |
| Endpoint de ingesta HTTP                      | ✅ En producción — ver §3                                             |
| Dashboard de inventario/admin                 | ✅ En producción — activar, revocar, métricas de collares             |
| Alertas de conectividad y batería baja        | ✅ En producción                                                      |
| Modo perdido, zonas seguras (geofencing)      | ✅ En producción                                                      |
| Transferencia segura entre dueños (handover)  | ✅ En producción                                                      |

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

| Código | Significado                                                                 |
| ------ | ----------------------------------------------------------------------------- |
| `204`  | Ubicación aceptada y registrada.                                              |
| `401`  | `X-Collar-Key` ausente o inválida.                                            |
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

| Variante                          | Qué incluye              | Prioridad para el piloto inicial |
| ---------------------------------- | --------------------------- | ----------------------------------- |
| **V1 — GPS base**                 | GPS + LTE-M/NB-IoT           | ✅ Alta — piloto de 50 unidades      |
| **V2 — GPS + cámara**             | GPS + cámara de baja resolución | Media — evaluación año 1        |
| **V3 — GPS + pantalla e-ink**     | GPS + display e-ink pequeño  | Media — evaluación año 1            |
| **V4 — GPS + cámara + pantalla**  | Combinación completa         | Baja — roadmap futuro               |

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
