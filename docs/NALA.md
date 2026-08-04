# NALA — Documento de Visión y Propósito

> **NALA** es el nombre interno del proyecto que evolucionó en **PawTrack CR**: una plataforma de identidad y recuperación de mascotas perdidas diseñada para Costa Rica.

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

### 10. Incentivos y estadísticas

El sistema mantiene un **leaderboard** de los usuarios con más reunificaciones exitosas, con insignias progresivas. Las estadísticas de recuperación (tasa por especie, raza y cantón) son públicas, accesibles para aliados y administradores.

---

## Público meta

PawTrack CR está diseñado para **tres audiencias concentricas**, todas presentes en Costa Rica:

### Audiencia primaria: Dueños de mascotas

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
- **Veterinarias** que necesitan identificar mascotas y registrar visitas

**Lo que quieren:** una forma rápida y anónima de ayudar sin comprometer su privacidad ni hacer un proceso largo.

### Audiencia terciaria: Administradores de la plataforma

El equipo operativo de PawTrack CR que verifica aliados, activa clínicas, modera contenido y monitorea el sistema.

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

| Característica | Facebook/Grupos | PawTrack CR |
|---|---|---|
| Identidad permanente | ❌ El post desaparece | ✅ El perfil QR es permanente |
| Geofencing y alertas | ❌ Manual | ✅ Automático por radio y cantón |
| Coordinación de búsqueda | ❌ Por mensajes | ✅ Cuadrícula en tiempo real |
| Matching visual por IA | ❌ | ✅ Azure Computer Vision |
| Privacidad del reportante | ❌ Nombre público | ✅ Anonimato por diseño |
| Chat seguro | ❌ WhatsApp personal | ✅ Chat enmascarado in-app |
| Entrega segura | ❌ Sin protocolo | ✅ Código de 4 dígitos |
| Estadísticas de recuperación | ❌ | ✅ Por especie, raza y cantón |
| Custodios temporales | ❌ | ✅ Red de fosters sugerida |
| Bot de WhatsApp | ❌ | ✅ Para usuarios sin app |

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

- ✅ Autenticación completa (JWT + refresh, verificación de email, bloqueo de cuenta)
- ✅ Gestión de mascotas, QR, perfil público
- ✅ Reporte de pérdida y Case Room
- ✅ Avistamientos con matching visual por IA
- ✅ Coordinación de búsqueda en campo (SignalR)
- ✅ Difusión multi-canal
- ✅ Chat enmascarado y handover seguro
- ✅ Bot de WhatsApp (Meta Cloud API)
- ✅ Red de aliados verificados
- ✅ Voluntarios custodia (fosters)
- ✅ Clínicas afiliadas
- ✅ Sistema de incentivos y leaderboard
- ✅ Estadísticas públicas de recuperación
- ✅ Mapa público interactivo con predicción de movimiento
- ✅ PWA instalable (Android/iOS)
- ✅ Infraestructura Azure declarada en Bicep

**Siguiente fase:** integración institucional con perreras y municipalidades.

---

*Proyecto desarrollado por Denis Avila Umaña · Costa Rica · 2026*
