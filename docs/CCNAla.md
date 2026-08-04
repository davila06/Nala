# PawTrack CR — Resumen Ejecutivo para Sitio Web

> **Para:** Diseñador/Desarrollador del sitio web corporativo  
> **Versión:** Abril 2026  
> **Confidencialidad:** Uso interno — no publicar como está

---

## ¿Qué es PawTrack CR?

**PawTrack CR** es una plataforma digital costarricense— disponible como aplicación web progresiva (PWA), funciona en cualquier smartphone sin necesidad de instalar nada— cuyo propósito es **reducir el tiempo que una mascota pasa perdida y aumentar las probabilidades de que vuelva a casa**.

La propuesta de valor en una línea:

> *"Cuando tu mascota se pierde, PawTrack CR activa una red inteligente de búsqueda en tiempo real para traerla de vuelta."*

---

## El problema que resuelve

Cada año miles de mascotas se pierden en Costa Rica. El proceso actual es caótico:
- Posts dispersos en grupos de Facebook que se pierden en el feed
- Información desactualizada
- Coordinación entre vecinos por WhatsApp sin estructura
- Sin forma segura de conectar al dueño con quien encontró la mascota

PawTrack CR reemplaza ese caos con una **infraestructura operativa estructurada, privada y en tiempo real**.

---

## ¿Cómo funciona? — El ciclo completo

### 1. Registra tu mascota y obtén su QR
El dueño crea el perfil digital de su mascota: nombre, especie, raza, foto, microchip (si tiene). El sistema genera un **código QR único** que se puede imprimir en un collar, placa o tag.

### 2. Cualquiera puede escanear el QR — sin instalar nada
Si alguien encuentra a la mascota, escanea el QR con su cámara y ve el perfil público: foto, nombre y — si hay un reporte activo — un botón de contacto seguro con el dueño. No se revela la dirección ni el teléfono del dueño.

### 3. Activa el reporte de pérdida
Con un par de toques el dueño activa el reporte, indicando la última ubicación en el mapa, foto reciente y mensaje para quién la encuentre. El sistema notifica **automáticamente** a los aliados registrados y vecinos con alertas activas en la zona.

### 4. Difusión multicanal con un clic
El dueño puede enviar el reporte por **correo electrónico, WhatsApp, Telegram y Facebook** a toda su red. PawTrack genera el mensaje y el enlace compartible.

### 5. Avistamientos con matching visual por IA
Cualquier persona — con o sin cuenta — puede reportar que vio a la mascota: toma una foto, indica la ubicación en el mapa y listo. **No requiere crear cuenta.**

Si alguien encuentra una mascota sin QR visible, puede usar el flujo **"Encontré una mascota"**: la plataforma analiza la foto con inteligencia artificial (Azure Computer Vision) y la compara contra todos los perfiles activos para mostrar los candidatos más similares.

### 6. Coordinación de búsqueda en tiempo real
Para búsquedas organizadas, el sistema genera una **cuadrícula interactiva** centrada en el último lugar visto. Los voluntarios en campo pueden reclamar una zona, marcarla como revisada o liberarla. Los cambios se ven al instante para todos los participantes.

### 7. Comunicación y entrega segura
- El contacto entre el dueño y el rescatador ocurre por un **chat dentro de la app**, sin revelar teléfonos ni datos personales.
- Para la entrega física, la app genera un **código de verificación de 4 dígitos** que el rescatador debe mostrar al dueño. Sin código no hay entrega — protección contra fraude incorporada.

### 8. Red colaborativa
- **Aliados verificados**: organizaciones de rescate, refugios y protectoras que reciben alertas enriquecidas y tienen herramientas avanzadas en los casos.
- **Custodios temporales (Fosters)**: voluntarios que pueden alojar a una mascota encontrada mientras se ubica a su dueño. La plataforma sugiere custodios cercanos automáticamente.
- **Clínicas afiliadas**: veterinarias que pueden escanear microchips y registrar visitas al perfil de la mascota.

### 9. Bot de WhatsApp
Para quienes no usan la app web, hay un **bot conversacional de WhatsApp** que guía al usuario para reportar una mascota perdida directamente desde el chat, step by step. La identidad del reportante es protegida.

### 10. Recompensas e incentivos
El dueño puede ofrecer una recompensa económica que queda en custodia en la plataforma. Cuando la mascota es recuperada y se confirma con el código de entrega, la recompensa se libera automáticamente al aliado que ayudó. PawTrack cobra una comisión de servicio.

El sistema también tiene un **leaderboard público** con los usuarios y aliados que más reunificaciones han logrado, con insignias progresivas.

---

## Estadísticas públicas

La plataforma publica estadísticas de recuperación en tiempo real:
- Tasa de reunificación por especie, raza y cantón
- Tiempo promedio de recuperación
- Distancia promedio entre dónde se perdió y dónde fue encontrada la mascota

Estos datos son públicos y refuerzan la credibilidad de la plataforma.

---

## ¿Por qué no es lo mismo que un grupo de Facebook?

| | Facebook / Grupos | **PawTrack CR** |
|---|---|---|
| Identidad permanente de la mascota | ❌ El post desaparece | ✅ Perfil QR permanente |
| Alertas automáticas por zona | ❌ Manual | ✅ Geofencing automático |
| Coordinación de búsqueda | ❌ Por mensajes de texto | ✅ Cuadrícula en tiempo real |
| Matching visual por IA | ❌ | ✅ |
| Privacidad del reportante | ❌ Nombre visible | ✅ Anonimato por diseño |
| Chat seguro | ❌ WhatsApp personal | ✅ Chat enmascarado in-app |
| Entrega verificada | ❌ Sin protocolo | ✅ Código de 4 dígitos |
| Red de custodios temporales | ❌ | ✅ |
| Recompensas en custodia | ❌ | ✅ |
| Bot de WhatsApp | ❌ | ✅ |
| Estadísticas de recuperación | ❌ | ✅ Públicas y en tiempo real |

---

## Planes y precios

### Para dueños de mascotas

| | **Free** | **Plus** ₡2,990/mes | **Familia** ₡4,990/mes |
|---|---|---|---|
| Mascotas registradas | 1 | 3 | Ilimitadas |
| Historial de escaneos del QR | Últimos 5 | Completo + mapa de calor | Completo |
| Predicción de movimiento IA | — | ✅ | ✅ |
| Notificaciones prioritarias | — | ✅ | ✅ |
| Radio de alerta (3 km → 10 km) | — | ✅ | ✅ |
| Sala de coordinación | — | ✅ | ✅ |
| Registros médicos en QR | — | — | ✅ |
| Acceso multi-usuario (familia) | — | — | ✅ |

### Para clínicas veterinarias

| | **Afiliada básica** (gratis) | **Clínica Plus** ₡15,000/mes | **Clínica Partner** ₡35,000/mes |
|---|---|---|---|
| Directorio y escaneo QR/microchip | ✅ | ✅ | ✅ |
| Posición destacada en mapa | — | ✅ | ✅ |
| Logo en alertas de pérdida cercanas | — | ✅ | ✅ |
| Integración microchip RFID | — | — | ✅ |
| Widget embebible en su sitio web | — | — | ✅ |

### Accesorios físicos con QR (tienda)

La plataforma vende accesorios con el QR de la mascota impreso o grabado:

| Producto | Precio |
|---|---|
| Placa de aluminio grabada (3×5 cm) | ₡4,500 |
| Tag de silicona con QR impreso UV | ₡5,500 |
| Collar nylon básico + placa incluida | ₡9,500 |
| Tag NFC + QR combo (toca o escanea) | ₡12,000 |
| Pack emergencia (placa + tarjeta bolsillo) | ₡7,000 |

---

## A quién va dirigido

### Dueños de mascotas (audiencia principal)
Personas de 22–55 años, residentes en el GAM y ciudades intermedias de Costa Rica, que tienen mascota y que valoran su seguridad. No necesitan experiencia técnica — la plataforma funciona desde cualquier navegador.

### Comunidad y rescatistas
Ciudadanos que encuentran mascotas, rescatistas independientes, organizaciones de bienestar animal y veterinarias. Para ellos la plataforma es **completamente gratuita** y no requiere registro para reportar un avistamiento.

### Municipalidades e instituciones
Municipalidades y perreras que necesitan digitalizar el control animal. PawTrack ofrece licencias institucionales para integración con el mapa público y gestión de animales capturados.

---

## Por qué Costa Rica primero

- **+60% de hogares** con al menos una mascota
- Grupos de Facebook de mascotas perdidas con **cientos de miles de miembros** — demanda probada
- **+80% de penetración** de smartphones
- Sistema de **cantones** que permite alertas geográficas precisas
- Red informal de rescatistas ya existente, lista para convertirse en el primer bloque de aliados

---

## Principios que definen la plataforma

**Privacidad por diseño** — Nadie que ayude tiene que exponer su identidad. Nadie que busque a su mascota tiene que revelar su dirección a desconocidos.

**Colaboración sin fricción** — Reportar un avistamiento no requiere cuenta. Escanear un QR no requiere instalar nada. El bot funciona desde WhatsApp.

**Operación real, no solo información** — PawTrack no es un directorio de avisos. Es un sistema operativo de recuperación: coordina búsquedas, hace matching por IA, gestiona custodios y verifica entregas.

---

## Tecnología (para contexto técnico del sitio)

- Aplicación web progresiva (PWA) — funciona en cualquier smartphone desde el navegador
- Backend en .NET con arquitectura cloud-native en Microsoft Azure
- Inteligencia artificial: Azure Computer Vision para matching visual de mascotas
- Mapas interactivos con coordenadas GPS en tiempo real
- Notificaciones push web sin necesidad de instalar app
- Comunicación en tiempo real vía WebSockets (SignalR)
- Infraestructura desplegada en Azure (App Service, SQL Database, Blob Storage, Key Vault, Application Insights)

---

*Documento generado para uso del equipo de diseño web. Para información técnica detallada ver `PawTrack_Documento_Maestro_v3.1.md`.*
