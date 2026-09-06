# NALA — Documento de Visión y Propósito

> **NALA** es el nombre interno del proyecto que evolucionó en **PawTrack CR**.  
> Última actualización: 2026-09-03

---

## ¿Qué es PawTrack CR?

PawTrack CR es una plataforma digital — disponible como aplicación web progresiva (PWA) — cuyo propósito central es **reducir el tiempo que una mascota pasa perdida y aumentar la tasa de reunificación con su familia**.

El problema que resuelve es cotidiano y doloroso: miles de mascotas se pierden cada año en Costa Rica. Sus dueños no saben qué hacer, la información se dispersa en grupos de Facebook, los avisos son estáticos, y la coordinación entre vecinos es caótica. PawTrack CR reemplaza ese caos con una infraestructura digital estructurada, colaborativa y segura.

---

## El ciclo de vida de una mascota en PawTrack CR

```
Registrar mascota
      │
      ▼
  Generar QR  ──────────────────────────────────────────────────────┐
      │                                                              │
      ▼                                                Cualquiera escanea el QR
  Mascota perdida                                      y ve el perfil público
      │                                                              │
      ▼                                                              ▼
Activar reporte ◄─────────── Notificación a aliados ◄─── Avistamiento reportado
      │                      y usuarios cercanos
      ▼
  Case Room ──► Difusión multicanal ──► Coordinación en campo (mapa)
      │
      ▼
  Avistamientos + Matching visual por IA
      │
      ▼
  Handover seguro (código de entrega)
      │
      ▼
  Reunificación ──► Score e incentivos
```

Cada etapa tiene soporte técnico activo: no es solo un directorio estático, sino un sistema operativo de recuperación en tiempo real.

---

## ¿Cómo funciona?

### 1. Identidad digital de la mascota

Cada mascota registrada recibe un **código QR único** vinculado a su perfil público. El perfil muestra nombre, especie, raza, foto y — cuando está activo un reporte de pérdida — un mensaje del dueño, información de recompensa y el estado del caso.

Cualquier persona que encuentre a la mascota puede escanear el QR **sin necesidad de instalar nada** y ver la información de contacto controlada por el dueño. Si la mascota tiene microchip ISO 11784, ese dato también queda registrado en el sistema.

### 2. Reporte de pérdida

El dueño activa un reporte de pérdida desde la app. El sistema captura:

- Última ubicación vista (mapa interactivo)
- Foto reciente
- Mensaje público para el encontrador
- Información de contacto controlada (nunca pública por defecto)
- Recompensa opcional en colones (CRC)

Al activarse el reporte, el sistema notifica automáticamente a:

- **Aliados verificados** en el radio de cobertura
- **Usuarios con alertas geográficas activas** en la zona

### 3. Difusión multi-canal

Con un solo clic, el dueño puede difundir el reporte por **correo electrónico, WhatsApp, Telegram y Facebook** hacia una base de contactos y aliados. El sistema lleva registro de cada intento y sus resultados.

### 4. Avistamientos y matching visual por IA

Cualquier persona — sin necesidad de cuenta — puede reportar un avistamiento indicando:

- Ubicación en el mapa
- Foto de la mascota vista
- Nota libre (sanitizada automáticamente para remover datos personales)

Si la persona no sabe a qué mascota pertenece, puede usar el flujo **"Encontré una mascota"**: el sistema vectoriza la foto usando **Azure Computer Vision** (embeddings de 1024 dimensiones) y la compara contra todos los perfiles activos en la base de datos, retornando los candidatos más similares ordenados por similitud coseno ponderada con proximidad geográfica.

### 5. Case Room (Sala de caso)

Cada reporte activo tiene una sala de operaciones que centraliza:

- Estado actual del caso
- Todos los avistamientos recibidos
- Actividad de búsqueda
- Historial de difusión

El dueño puede cambiar el estado del caso (activo → suspendido → reunificado) y resolver el reporte cuando su mascota es encontrada. Al registrar la reunificación, el sistema captura dónde fue hallada y calcula la distancia y el tiempo total de recuperación, datos que alimentan las **estadísticas públicas**.

### 6. Coordinación de búsqueda en campo

Para búsquedas organizadas, el sistema genera automáticamente una **cuadrícula de 7×7 zonas de 300 m** centrada en el último lugar visto. Los voluntarios en campo pueden:

- **Reclamar** una zona (la estoy buscando)
- **Limpiar** una zona (la revisé, no está)
- **Liberar** una zona (no puedo continuar)

Los cambios se propagan en tiempo real a todos los participantes via **SignalR**.

### 7. Comunicación segura

La plataforma provee un **chat enmascarado**: el contacto entre el dueño y el rescatador ocurre dentro de la app, sin revelar números de teléfono ni datos personales. Para proteger la entrega física, el sistema genera un **código de 4 dígitos** que el rescatador debe presentar al dueño para confirmar la identidad antes de entregar la mascota.

### 8. Red colaborativa

**Aliados verificados** son organizaciones (rescatistas, refugios, protectoras) que aplican, son verificadas por el admin, y reciben alertas enriquecidas con la capacidad de interactuar en los casos con más herramientas.

**Custodios temporales (Fosters)** son voluntarios que pueden alojar una mascota encontrada mientras se localiza a su dueño. El sistema sugiere custodios geográficamente cercanos al reporte de mascota encontrada.

**Clínicas afiliadas** son veterinarias verificadas que pueden escanear microchips y vincular visitas a perfiles de mascotas en la plataforma.

### 9. Bot de WhatsApp

Para usuarios que no tienen acceso a la app web, el sistema ofrece un **bot conversacional de WhatsApp** (Meta Cloud API). El bot guía al usuario paso a paso para reportar una mascota perdida directamente desde WhatsApp, crea un reporte y envía el enlace al perfil público. La identidad del reportante es protegida mediante hash SHA-256 de su número de teléfono.

### 10. Suscripciones y monetización

PawTrack CR opera bajo un modelo freemium con tres planes para dueños de mascotas, según la implementación actual del backend:

| Plan           | Precio     | Mascotas   | Diferenciador                             |
| -------------- | ---------- | ---------- | ----------------------------------------- |
| **Explorador** | Gratis     | 1          | QR, reportes, avistamientos               |
| **Plus**       | ₡2,990/mes | 3          | GPS, WhatsApp, IA ilimitada, coordinación |
| **Familia**    | ₡4,990/mes | Ilimitadas | Multi-usuario (5), expediente médico, PDF |

Adicionalmente existen tiers B2B activos en producción:

- Tiendas: `StorePlus` (₡12,000/mes) y `StorePartner` (₡25,000/mes)
- Refugios: `ShelterPlus` (₡8,000/mes)
- Clínicas: `ClinicPlus` (₡15,000/mes) y `ClinicPartner` (₡35,000/mes)
- Municipalidades: `MuniBasica` (₡150,000/año), `MuniFull` (₡300,000/año), `MuniRedRegional` (₡500,000/año)

El sistema de feature gating está implementado tanto en el backend (enforcement por plan) como en el frontend (UI gates).

### 11. Collar GPS y expediente médico

El **plan Plus** habilita la integración con collares GPS de terceros (Tractive) o genéricos (activación por serial/tag + device key). El dueño conecta su cuenta Tractive via OAuth2 desde la tab GPS del perfil de mascota. El sistema actualiza la posición y muestra el historial de trayectoria por rango de fechas en un mapa interactivo. Además incluye alertas de conectividad (offline) y batería baja, modo perdido (búsqueda intensiva coordinada con la red), zonas seguras (geofencing con alerta de salida), transferencia segura del collar entre dueños (handover code) y auditoría de eventos. Un dashboard de administración permite ver inventario y métricas de collares.

El **plan Familia** desbloquea el expediente médico digital: registro de vacunas, desparasitaciones, visitas veterinarias y recordatorios automáticos. El historial puede ser compartido con clínicas afiliadas y exportado en PDF. Las clínicas Partner pueden emitir certificados veterinarios PDF con QR de verificación pública.

### 12. Incentivos y estadísticas

El sistema mantiene un **leaderboard** de los usuarios con más reunificaciones exitosas, con insignias progresivas. Las estadísticas de recuperación (tasa por especie, raza y cantón) son públicas, accesibles para aliados y administradores.

---

## Público meta

PawTrack CR está diseñado para **cuatro audiencias**, todas presentes en Costa Rica:

### Audiencia primaria: Dueños de mascotas (B2C)

El núcleo del producto. Personas que tienen uno o más animales de compañía (principalmente perros y gatos) y que valoran la seguridad y trazabilidad de sus mascotas. Son el motor de contenido de la plataforma.

**Perfil típico:**

- Hombre o mujer, 22–55 años
- Residente urbano o periurbano en el GAM o ciudades intermedias (Cartago, Heredia, Alajuela, Pérez Zeledón)
- Con smartphone (iOS o Android, aunque la app es PWA y funciona en el navegador)
- Nivel socioeconómico B, C+ y C
- Tiene mascota que considera parte de la familia
- Ha vivido o conoce a alguien que ha vivido la angustia de perder una mascota

**Lo que quieren:** tranquilidad. Saber que si su mascota se pierde, hay un sistema que los ayuda a encontrarla, no solo un post en Facebook.

### Audiencia secundaria: Comunidad y rescatistas

Personas que colaboran en la recuperación sin ser el dueño. Incluyen:

- **Ciudadanos del común** que encuentran una mascota perdida o ven una por el barrio
- **Rescatistas independientes** que operan en redes informales de recuperación
- **Organizaciones de bienestar animal** (rescatistas, refugios, protectoras) que pueden convertirse en **aliados verificados**

**Lo que quieren:** una forma rápida y anónima de ayudar sin comprometer su privacidad ni hacer un proceso largo.

### Audiencia terciaria: Clínicas veterinarias y comercios (B2B)

Clínicas veterinarias registradas ante SENASA que usan la plataforma para:

- Identificar mascotas via QR o microchip RFID
- Emitir certificados veterinarios PDF verificables
- Recibir alertas de mascotas perdidas cercanas
- Compartir expediente médico con dueños

Los planes activos del backend son `ClinicPlus` y `ClinicPartner`, con facturación mensual. El nombre `ClinicBasic` se usa como un estado base o de directorio, no como plan pagado principal del producto actual.

### Audiencia cuaternaria: Municipalidades (B2G)

Gobiernos locales y unidades de control animal que usan la plataforma para gestionar animales capturados, generar reportes para SENASA, y conectar capturas con perfiles de mascotas perdidas registradas. Costa Rica tiene 82 municipalidades como mercado potencial.

### Administradores de la plataforma

El equipo operativo de PawTrack CR que verifica aliados, activa clínicas, aprueba municipalidades, modera contenido y monitorea el sistema.

---

## Por qué Costa Rica

Costa Rica fue elegida como mercado inicial por razones concretas:

1. **Alto índice de tenencia de mascotas**: Más del 60 % de los hogares costarricenses tiene al menos una mascota.
2. **Cultura de redes sociales para mascotas perdidas**: Grupos de Facebook como "Mascotas Perdidas CR" tienen cientos de miles de integrantes, lo que demuestra demanda no satisfecha.
3. **Infraestructura móvil sólida**: Penetración de smartphones superior al 80 % y cobertura 4G en zonas urbanas y semiurbanas.
4. **Organización cantonal conocida**: El sistema de cantones permite geofencing y alertas geográficas con semántica local.
5. **Comunidad de rescatistas activa**: Existe una red informal de rescatistas y refugios que puede convertirse en la primera capa de aliados verificados.

---

## Por qué no es solo un directorio

La diferencia con un aviso de "mascota perdida" en redes sociales o una web de clasificados:

| Característica                   | Facebook/Grupos       | PawTrack CR                                                               |
| -------------------------------- | --------------------- | ------------------------------------------------------------------------- |
| Identidad permanente             | ❌ El post desaparece | ✅ El perfil QR es permanente                                             |
| Geofencing y alertas             | ❌ Manual             | ✅ Automático por radio y cantón                                          |
| Coordinación de búsqueda         | ❌ Por mensajes       | ✅ Cuadrícula en tiempo real                                              |
| Matching visual por IA           | ❌                    | ✅ Azure Computer Vision                                                  |
| Privacidad del reportante        | ❌ Nombre público     | ✅ Anonimato por diseño                                                   |
| Chat seguro                      | ❌ WhatsApp personal  | ✅ Chat enmascarado in-app                                                |
| Entrega segura                   | ❌ Sin protocolo      | ✅ Código de 4 dígitos                                                    |
| Sistema de recompensas           | ❌ Promesa verbal     | ✅ Bounty con escrow + SINPE                                              |
| Estadísticas de recuperación     | ❌                    | ✅ Por especie, raza y cantón                                             |
| Custodios temporales             | ❌                    | ✅ Red de fosters sugerida                                                |
| Bot de WhatsApp                  | ❌                    | ✅ Para usuarios sin app                                                  |
| Collar GPS integrado             | ❌                    | ✅ Tractive/genérico + alertas + modo perdido + zonas seguras (plan Plus) |
| Expediente médico                | ❌                    | ✅ Historial + PDF (plan Familia)                                         |
| Clínicas veterinarias conectadas | ❌                    | ✅ Portal B2B con 3 tiers                                                 |
| Certificados PDF verificables    | ❌                    | ✅ QR de verificación pública                                             |
| Portal municipal                 | ❌                    | ✅ B2G para control animal                                                |

---

## Arquitectura y stack técnico

PawTrack CR es un **monolito modular** (Clean Architecture) preparado para extracción futura de servicios, sin sobre-ingeniería prematura de microservicios.

```
[Frontend React PWA] ←→ [ASP.NET Core API] ←→ [Azure SQL]
                              ↑↓ SignalR         ↑↓ EF Core
                         [Azure Blob Storage]
                         [Azure Key Vault]
                         [Application Insights]
```

**Capas backend** (dependencias apuntan hacia adentro):

| Capa                      | Responsabilidad                                                                                       |
| ------------------------- | ----------------------------------------------------------------------------------------------------- |
| `PawTrack.API`            | Controllers, middleware, hubs SignalR, composición de DI                                              |
| `PawTrack.Application`    | Commands/Queries (CQRS via MediatR), validadores FluentValidation, interfaces                         |
| `PawTrack.Domain`         | Entidades, value objects, lógica de negocio pura — sin dependencias externas                          |
| `PawTrack.Infrastructure` | Implementación de repositorios, EF Core, integraciones externas (Azure, Tractive, SendGrid, WhatsApp) |

**Convenciones que no se negocian:**

- CQRS estricto — comandos mutan y retornan datos mínimos; queries son de solo lectura y retornan DTOs, nunca entidades.
- Validación exclusivamente en pipeline behaviors de MediatR (FluentValidation) — nunca a mano dentro de un handler.
- IDs con `Guid.CreateVersion7()` (sortable por tiempo, mejor para índices clustered) — expuestos como string en las respuestas de API.
- Fotos y binarios siempre en Azure Blob Storage — nunca en la base de datos.
- Secretos exclusivamente en Azure Key Vault — cero secretos hardcodeados en `appsettings.json`.
- Comunicación entre módulos solo vía MediatR (commands/notifications) — nunca llamadas directas entre servicios de distintos módulos.
- Errores de dominio con patrón `Result<T>` o Problem Details (RFC 7807) — nunca excepciones de negocio cruzando límites de módulo.
- Todas las rutas HTTP tienen una política de rate limiting explícita, particionada por IP (nunca un límite global compartido entre todos los clientes).

### Stack backend

| Tecnología                             | Versión | Uso                                              |
| -------------------------------------- | ------- | ------------------------------------------------ |
| .NET / ASP.NET Core                    | 9.0     | Runtime y Web API                                |
| MediatR                                | 12.x    | Pipeline CQRS                                    |
| Entity Framework Core                  | 9.x     | ORM + migraciones code-first, SQL Server         |
| FluentValidation                       | 11.x    | Validación en pipeline behaviors                 |
| SignalR                                | 9.0     | Real-time (`/hubs/search-coordination`, chat)    |
| Serilog                                | —       | Logging estructurado                             |
| Microsoft.Extensions.Http.Resilience   | —       | Retry/circuit-breaker en clientes HTTP externos  |
| QuestPDF                               | 2025.x  | Certificados veterinarios PDF con QR verificable |
| xUnit + NSubstitute + FluentAssertions | —       | Suite de tests unitarios e integración           |
| Stryker.NET                            | —       | Mutation testing                                 |
| Application Insights                   | —       | Telemetría y monitoreo                           |

### Stack frontend

| Tecnología              | Versión    | Uso                                              |
| ----------------------- | ---------- | ------------------------------------------------ |
| React                   | 19         | UI                                               |
| TypeScript              | 5.x strict | Tipado estricto en todo el codebase              |
| Vite                    | 6          | Build + HMR + PWA plugin (`injectManifest`)      |
| React Router            | 7          | Enrutamiento (`createBrowserRouter`)             |
| TanStack React Query    | 5          | Estado de servidor (cache, invalidación)         |
| Zustand                 | 5          | Estado de UI que persiste entre rutas (ej. auth) |
| Leaflet / React-Leaflet | —          | Mapa interactivo (avistamientos, zonas, GPS)     |
| Playwright              | —          | Suite de tests end-to-end                        |

### Infraestructura Azure

| Servicio              | Uso                                                                                                                        |
| --------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Azure Container Apps  | Hosting del backend, con scale-out multi-instancia                                                                         |
| Azure SQL Database    | Base de datos relacional principal                                                                                         |
| Azure Blob Storage    | Fotos de mascotas, avistamientos, certificados PDF, logos de vallas                                                        |
| Azure Key Vault       | Secretos, connection strings, claves de firma JWT                                                                          |
| Azure Cache for Redis | Cache distribuido — rate limiting de notificaciones, throttle de SignalR, estado de chat, todo compartido entre instancias |
| Application Insights  | Telemetría, logs, métricas                                                                                                 |
| Azure Computer Vision | Embeddings de imágenes para matching visual de mascotas                                                                    |
| GitHub Actions        | CI/CD: build → test → Docker → ACR → Container App update                                                                  |
| Bicep                 | Infraestructura como código                                                                                                |

**Diseñado para múltiples instancias desde el inicio:** todo el estado que antes vivía en memoria de un solo proceso (rate limiting de notificaciones, indicador de "escribiendo" en chat, throttle de ubicación GPS en tiempo real, locks de jobs programados) está respaldado por Redis o por locks distribuidos a nivel de base de datos — ninguna réplica del Container App puede quedar desincronizada con las demás.

---

## Calidad, testing y seguridad

**Cobertura de tests (backend):**

- **1,150+ tests unitarios** (xUnit + NSubstitute + FluentAssertions) — 0 fallos
- **88 tests de integración** end-to-end contra `WebApplicationFactory`
- Suite de tests end-to-end (Playwright) para los flujos críticos: autenticación, GPS de collar, modo perdido, transferencia segura, dashboard admin
- Mutation testing con Stryker.NET para validar que los tests realmente detectan código roto, no solo que "pasan"
- 0 errores de compilación en backend y frontend

**Rondas de seguridad:** la plataforma ha pasado por **más de 100 rondas de auditoría de seguridad** (regresión activa en `backend/tests/PawTrack.UnitTests/Security/`), cubriendo:

- **BOLA (Broken Object Level Authorization):** cada endpoint que expone datos de una mascota, collar, expediente médico o pedido verifica explícitamente que el usuario autenticado es el dueño (o tiene un grant válido) — nunca solo que el JWT es válido.
- **Autenticación:** JWT de acceso en memoria (nunca `localStorage`), refresh token en cookie `httpOnly`/`Secure`/`SameSite`, bloqueo de cuenta tras intentos fallidos, JTI blocklist respaldada en SQL (funciona correctamente en multi-instancia).
- **Rate limiting:** partición por IP (no un contador global compartido) en cada endpoint sensible — login, refresh, reportes, mensajes de chat, ingestión de ubicación GPS, verificación de seriales de collar.
- **Privacidad de avistamientos:** el reportante nunca queda identificado; el chat entre dueño y rescatador es enmascarado; los números de teléfono del bot de WhatsApp se almacenan solo como hash HMAC-SHA256.
- **Idempotencia:** los webhooks de WhatsApp usan un índice único a nivel de base de datos (no un simple check-then-set en memoria) para garantizar que un mensaje reenviado por Meta nunca se procese dos veces.
- **Confiabilidad de notificaciones:** patrón de Outbox transaccional — los eventos de dominio se persisten en la misma transacción que el cambio de estado, y un proceso en segundo plano los entrega con reintentos, evitando pérdida de notificaciones si el proceso se reinicia entre el commit y el envío.

---

## La filosofía del diseño

PawTrack CR está construido sobre tres principios que no se negocian:

**1. Privacidad por diseño**
Los avistamientos no almacenan datos del reportante. El chat es enmascarado. El bot almacena solo el hash del número de teléfono. La entrega física requiere código verificado. Un dueño angustiado no debería tener que exponer su dirección a desconocidos para recuperar a su mascota.

**2. Colaboración sin fricción**
Reportar un avistamiento no requiere cuenta. Escanear un QR no requiere instalar nada. El bot funciona desde WhatsApp, que ya está instalado en prácticamente todos los smartphones de Costa Rica. La plataforma baja la barrera de participación al mínimo posible.

**3. Operación real, no solo información**
El sistema no es un directorio. Tiene coordinación en campo en tiempo real, difusión automatizada, matching visual, custodios sugeridos y códigos de entrega. Está diseñado para acompañar toda la operación de recuperación, no solo publicar un aviso.

---

## Estado actual

PawTrack CR se encuentra en **MVP ampliado**, con todos sus módulos principales funcionando:

**Core — recuperación de mascotas**

- ✅ Autenticación completa (JWT + refresh, verificación de email, bloqueo de cuenta)
- ✅ Gestión de mascotas, QR, perfil público, historial de escaneos
- ✅ Reporte de pérdida y Case Room
- ✅ Avistamientos con matching visual por IA (Azure Computer Vision, embeddings 1024d)
- ✅ Flujo "encontré una mascota" sin QR (IA + geoproximidad)
- ✅ Coordinación de búsqueda en campo (cuadrícula 7×7, SignalR)
- ✅ Difusión multi-canal (Email, WhatsApp, Telegram, Facebook)
- ✅ Chat enmascarado y handover seguro (código 4 dígitos)
- ✅ Mapa público interactivo con predicción de movimiento IA
- ✅ Bot de WhatsApp (Meta Cloud API, hash SHA-256 del número)
- ✅ Sistema de recompensas económicas (Bounties con escrow, SINPE Móvil)

**Red colaborativa**

- ✅ Red de aliados verificados con alertas enriquecidas
- ✅ Voluntarios custodia (fosters) con sugerencias geográficas
- ✅ Clínicas afiliadas — escaneo QR/microchip, vinculación a perfil de mascota
- ✅ Sistema de incentivos y leaderboard con insignias

**Monetización — B2C**

- ✅ Sistema de suscripciones con 3 planes: Explorador (gratis), Plus (₡2,990/mes), Familia (₡4,990/mes)
- ✅ Feature gating completo por plan (UI gates + backend enforcement)
- ✅ Cuentas familiares multi-usuario (hasta 5 miembros, Plan Familia)
- ✅ Expediente médico digital: vacunas, visitas, recordatorios, exportación PDF (Plan Familia)
- ✅ Integración collar GPS Tractive/genérico (OAuth2, polling, activación por tag/serial, historial por rango)
- ✅ Alertas de conectividad/batería, modo perdido, zonas seguras (geofencing), transferencia segura y auditoría de eventos del collar
- ✅ Bundle GPS on-demand

**Monetización — B2B Clínicas veterinarias**

- ✅ Portal de clínicas con 3 tiers: Básica (gratis), Plus (₡15,000/mes), Partner (₡35,000/mes)
- ✅ Expediente digital compartido clínica ↔ dueño (Opciones A, B y C)
- ✅ Certificados veterinarios PDF verificables con QR único (QuestPDF, Plan Partner)
- ✅ Widget embebible y API de consulta para clínicas Partner
- ✅ Integración microchip RFID avanzada

**Monetización — B2G Municipalidades**

- ✅ Portal de control animal municipal con 3 planes: Básica, Full, Red Regional
- ✅ Registro digital de animales capturados, estados, reportes SENASA
- ✅ API de consulta pública y estadísticas por cantón (plan Full+)

**Infraestructura**

- ✅ Estadísticas públicas de recuperación (por especie, raza, cantón)
- ✅ PWA instalable (Android/iOS) con soporte offline
- ✅ Infraestructura Azure declarada en Bicep (Container Apps, SQL, Blob, Key Vault, App Insights)
- ✅ CI/CD con GitHub Actions (build → test → Docker → ACR → Container App)

**Pendientes operacionales (no código):** GitHub Secrets CI/CD, dominio pawtrack.cr, WhatsApp webhook, VAPID keys, migraciones EF en Azure SQL.

**Siguiente paso:** despliegue a producción en Azure.

---

_Proyecto desarrollado por Denis Avila Umaña · Costa Rica · 2026_
