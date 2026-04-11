# Manual de Usuario — PawTrack CR

**Versión:** 1.0  
**Plataforma:** Aplicación Web Progresiva (PWA) — accesible desde cualquier navegador  
**Idioma:** Español  
**Última actualización:** Abril 2026

---

## Tabla de contenidos

1. [¿Qué es PawTrack CR?](#1-qué-es-pawtrack-cr)
2. [Tipos de usuario](#2-tipos-de-usuario)
3. [Crear una cuenta](#3-crear-una-cuenta)
4. [Iniciar sesión y recuperar acceso](#4-iniciar-sesión-y-recuperar-acceso)
5. [Registrar una mascota](#5-registrar-una-mascota)
6. [El código QR de tu mascota](#6-el-código-qr-de-tu-mascota)
7. [Perfil público de la mascota](#7-perfil-público-de-la-mascota)
8. [Reportar mascota perdida](#8-reportar-mascota-perdida)
9. [La sala de caso (Case Room)](#9-la-sala-de-caso-case-room)
10. [Difusión del reporte](#10-difusión-del-reporte)
11. [Coordinación de búsqueda en campo](#11-coordinación-de-búsqueda-en-campo)
12. [Reportar un avistamiento](#12-reportar-un-avistamiento)
13. [Encontré una mascota sin collar o QR](#13-encontré-una-mascota-sin-collar-o-qr)
14. [Chat seguro](#14-chat-seguro)
15. [Código de entrega segura (Handover)](#15-código-de-entrega-segura-handover)
16. [Resolver el caso: mascota reunificada](#16-resolver-el-caso-mascota-reunificada)
17. [Notificaciones](#17-notificaciones)
18. [Red de aliados verificados](#18-red-de-aliados-verificados)
19. [Voluntarios custodia (Fosters)](#19-voluntarios-custodia-fosters)
20. [Clínicas veterinarias afiliadas](#20-clínicas-veterinarias-afiliadas)
21. [Bot de WhatsApp](#21-bot-de-whatsapp)
22. [Incentivos y leaderboard](#22-incentivos-y-leaderboard)
23. [Estadísticas de recuperación](#23-estadísticas-de-recuperación)
24. [Mapa público](#24-mapa-público)
25. [Configuración de perfil y preferencias](#25-configuración-de-perfil-y-preferencias)
26. [Preguntas frecuentes](#26-preguntas-frecuentes)

---

## 1. ¿Qué es PawTrack CR?

PawTrack CR es una plataforma digital para Costa Rica que ayuda a reunir mascotas perdidas con sus familias.

El proceso es simple:

1. Registras a tu mascota y le generas un código QR.
2. Si se pierde, activas un reporte desde la app.
3. La comunidad reporta avistamientos, el sistema los muestra en el mapa y te notifica.
4. Coordinas la búsqueda en campo, chateas de forma segura con quien la encontró, y confirmas la entrega con un código de 4 dígitos.
5. Cierras el caso, tu mascota está en casa.

La plataforma funciona como una **aplicación web progresiva (PWA)**: puedes usarla desde el navegador de tu celular o computadora, o instalarla en la pantalla de inicio de tu teléfono como si fuera una app nativa — sin pasar por Play Store ni App Store.

---

## 2. Tipos de usuario

| Rol | Descripción |
|-----|-------------|
| **Dueño** | Registra mascotas, activa reportes, coordina la búsqueda. Rol por defecto al registrarse. |
| **Aliado** | Organización verificada (rescatista, refugio, protectora). Recibe alertas enriquecidas y tiene acceso a herramientas de operación. |
| **Clínica** | Veterinaria afiliada. Puede escanear microchips y registrar visitas de mascotas. |
| **Administrador** | Equipo PawTrack CR. Verifica aliados, activa clínicas, modera la plataforma. |
| **Comunidad** | Cualquier persona que escanea un QR o reporta un avistamiento **sin necesidad de cuenta**. |

---

## 3. Crear una cuenta

1. Abre PawTrack CR en tu navegador o escanea el código QR de presentación.
2. Toca **Registrarse**.
3. Completa:
   - Nombre completo
   - Correo electrónico
   - Contraseña (mínimo 8 caracteres, mezcla de letras y números recomendada)
4. Toca **Crear cuenta**.
5. Revisa tu correo. Llegará un mensaje de PawTrack CR con un enlace de verificación.
6. Toca el enlace en el correo (tiene vigencia de 24 horas).
7. Tu cuenta queda activa. Inicia sesión.

> **Tip:** Si no recibes el correo en 5 minutos, revisa la carpeta de spam.

---

## 4. Iniciar sesión y recuperar acceso

### Inicio de sesión

1. Ve a `/login` o toca **Iniciar sesión** en la página principal.
2. Ingresa tu correo y contraseña.
3. Toca **Entrar**.

La app intenta renovar tu sesión automáticamente mientras la estás usando. Si pasa tiempo sin actividad o vuelves más tarde, es posible que debas iniciar sesión de nuevo.

### Bloqueo de cuenta

Si ingresas la contraseña incorrecta **5 veces seguidas**, la cuenta se bloqueará por **15 minutos** por razones de seguridad. Después del tiempo de espera, podrás intentar de nuevo.

### Cerrar sesión

Ve a tu perfil (ícono superior derecho) y toca **Cerrar sesión**. Tu sesión queda invalidada de inmediato en todos los dispositivos.

---

## 5. Registrar una mascota

1. Desde el **Dashboard**, toca el botón **Registrar mascota**.
2. Completa la información:
   - **Nombre** de la mascota
   - **Especie** (Perro, Gato, u otro)
   - **Raza** (opcional)
   - **Fecha de nacimiento** (opcional)
3. Toca **Guardar**.
4. Una vez creada la mascota, entra a su perfil para revisar sus datos, mostrar su código QR y, si aplica, reportarla como perdida.

Puedes registrar **tantas mascotas como quieras** en la misma cuenta.

### Editar la información

En el perfil de la mascota, toca el ícono de editar (lápiz) para actualizar nombre, especie, raza o fecha de nacimiento.

---

## 6. El código QR de tu mascota

Cada mascota tiene un código QR único vinculado a su perfil público.

### Generar y descargar el QR

1. Ve al perfil de tu mascota.
2. Toca **Ver QR** o **Descargar QR**.
3. Guarda la imagen o imprímela para colocarla en el collar, placa, o cédula de tu mascota.

### ¿Qué pasa cuando alguien escanea el QR?

La persona que escanea el QR es redirigida automáticamente al **perfil público de tu mascota** (ver sección 7). No necesitan tener la app instalada — funciona con cualquier cámara de celular.

El sistema registra cada escaneo con fecha, hora y ubicación aproximada. Puedes ver el **historial de escaneos** en el perfil de tu mascota para saber cuándo y dónde fue escaneado.

### Avatar de WhatsApp

Para facilitar la difusión, puedes generar una imagen especial con la foto de tu mascota y su código QR superpuesto, optimizada para compartir en WhatsApp.

---

## 7. Perfil público de la mascota

El perfil público es la página que ve cualquier persona que escanea el QR. Incluye:

- Nombre, especie y raza
- Foto
- Estado actual (Normal / **Perdido**)

Cuando la mascota está **perdida**, el perfil también muestra:

- Mensaje del dueño (si configuraste uno)
- Nombre de contacto del dueño
- Recompensa ofrecida (si aplica)
- Botón para **reportar un avistamiento**
- Botón para **contactar al dueño** (abre el chat seguro)

Lo que **nunca** aparece en el perfil público:
- Número de teléfono del dueño
- Dirección del dueño
- Correo electrónico

---

## 8. Reportar mascota perdida

Cuando tu mascota no aparece, activa un reporte lo antes posible. Cada minuto cuenta.

1. Ve al perfil de tu mascota.
2. Toca **Reportar como perdida**.
3. Completa el formulario:
   - **Último lugar visto**: toca el mapa y ubica el punto exacto o arrastra el marcador.
   - **Fecha y hora** del último avistamiento.
   - **Foto reciente** (recomendado: una foto clara de los últimos 30 días).
   - **Descripción** (opcional): señas particulares, collar, comportamiento.
   - **Mensaje público**: texto que verá quien encuentre a tu mascota ("Por favor llámame, es mi mejor amigo").
   - **Nombre de contacto**: el nombre que aparecerá en el perfil público (puede ser apodo).
   - **Teléfono de contacto**: solo accesible para rescatistas autenticados — nunca público.
   - **Recompensa** (opcional): monto en colones y nota adicional.
4. Toca **Activar reporte**.

El sistema cambia el estado de tu mascota a **Perdida**, notifica a aliados y usuarios en el área, y activa la sala de caso.

---

## 9. La sala de caso (Case Room)

La sala de caso es el centro de operaciones mientras tu mascota está perdida. Accede desde tu Dashboard tocando el reporte activo, o desde `/lost/:id/case`.

Desde la sala de caso puedes:

- Ver todos los **avistamientos** recibidos en el mapa y en lista
- Acceder al **chat** con cada rescatista que contactó
- Activar la **coordinación de búsqueda en campo**
- Ver el estado de la **difusión** (canales usados y resultados)
- Cambiar el **estado del caso**:
  - **Activo**: búsqueda en curso (estado inicial)
  - **Suspendido**: pausar temporalmente sin cerrar el caso
  - **Reunificado**: tu mascota fue encontrada ✓
  - **Cerrado sin reunificación**: el caso se cierra sin encontrar a la mascota

---

## 10. Difusión del reporte

Desde la sala de caso, toca **Difundir** para enviar tu reporte por múltiples canales simultáneamente:

- **Correo electrónico** a aliados verificados de tu zona
- **WhatsApp** (si tienes el número configurado)
- **Telegram**
- **Facebook**

La difusión incluye la foto, descripción y enlace al perfil público de tu mascota.

> **Límite:** Se permiten hasta 3 difusiones por cada 10 minutos para evitar saturación.

---

## 11. Coordinación de búsqueda en campo

Para búsquedas organizadas con varias personas, activa el modo de coordinación:

1. En la sala de caso, toca **Activar coordinación de búsqueda**.
2. El sistema genera automáticamente una cuadrícula de **49 zonas (7×7) de 300 metros** centrada en el último lugar visto de tu mascota.
3. Comparte el enlace de búsqueda (`/lost/:id/busqueda`) con los voluntarios.

### Cómo usar las zonas

Cada voluntario en el mapa puede:
- **Reclamar** una zona: "Yo estoy buscando en esta área"
- **Limpiar** una zona: "Revisé esta zona y no está aquí"
- **Liberar** una zona: "No puedo seguir, libero la zona para otro"

Los cambios son **en tiempo real**: todos los participantes conectados ven inmediatamente qué zonas están siendo revisadas, cuáles ya fueron despejadas y cuáles están libres.

---

## 12. Reportar un avistamiento

Si ves una mascota que parece perdida:

### Opción A: Escaneaste el QR de la mascota

1. Escanea el código QR del collar.
2. En el perfil público, toca **Reportar avistamiento**.
3. Indica la ubicación (el mapa ya detecta tu posición aproximada).
4. Sube una foto si puedes.
5. Agrega una nota si quieres (ej: "Está cerca de la panadería en Avenida 10").
6. Toca **Enviar avistamiento**.

Tu reporte llega al dueño en segundos. **No necesitas crear una cuenta** para hacer esto.

### Opción B: Viste una mascota perdida pero no sabes de quién es

Usa el flujo "Encontré una mascota" (ver sección 13).

### ¿Qué pasa con mis datos?

Los avistamientos son **completamente anónimos**: la plataforma **no almacena** tu nombre, número de teléfono ni correo. Solo registra la ubicación GPS, foto (si la subiste), nota (si agregaste una) y la hora del reporte.

---

## 13. Encontré una mascota sin collar o QR

Si encontraste una mascota pero no tiene identificación:

1. Ve a `/encontre-mascota` (también accesible desde el mapa público).
2. Sube una **foto clara** de la mascota.
3. Indica la **ubicación** donde la encontraste.
4. Toca **Buscar coincidencias**.

El sistema usa **inteligencia artificial** para comparar la foto con todos los perfiles activos de mascotas perdidas y retorna los candidatos más similares, ordenados por similitud visual y cercanía geográfica.

5. Revisa las coincidencias sugeridas. Si la coincidencia es alta, PawTrack puede notificar automáticamente al dueño.
6. Si no aparece una coincidencia clara, igual puedes enviar el reporte. El sistema lo guarda para cruzarlo con reportes activos y futuros.

---

## 14. Chat seguro

Cuando alguien quiere contactarte sobre tu mascota perdida, el sistema abre un **chat enmascarado**: ninguna de las dos partes ve el número de teléfono, correo ni datos personales del otro.

### Iniciar un chat

- **Como encontrador:** Desde el perfil público de la mascota perdida, toca **Contactar al dueño**.
- **Como dueño:** Desde la sala de caso, toca el nombre de un avistamiento para abrir el chat con esa persona.

### Privacidad del chat

- Tu número de teléfono jamás aparece en el chat.
- El chat queda asociado al reporte activo. Cuando el caso se cierra, el chat también se cierra.
- Si sospechas de comportamiento fraudulento de alguien en el chat, usa el botón **Reportar fraude** (ver sección siguiente).

### Reporte de fraude

Si alguien te pide dinero antes de devolvertela mascota, exige algo sospechoso o actúa de forma irregular, toca **Reportar fraude** en el chat. El equipo de PawTrack CR revisará el reporte.

---

## 15. Código de entrega segura (Handover)

Para proteger la entrega física de tu mascota:

### El dueño genera el código

1. En la sala de caso, toca **Generar código de entrega**.
2. El sistema muestra un código de **4 dígitos** válido por 24 horas.
3. **No compartas este código por el chat** — compártelo personalmente al momento de la entrega.

### El encontrador verifica el código

1. Cuando llegues al punto de encuentro, el dueño te pedirá que ingreses el código en la app.
2. El sistema valida el código y confirma la identidad.
3. Ambas partes tienen certeza de que la entrega es legítima.

Esto previene estafas donde alguien reclama una mascota sin ser el dueño real.

---

## 16. Resolver el caso: mascota reunificada

Cuando tu mascota llegue a casa:

1. Ve al perfil de tu mascota o a las acciones del caso activo.
2. Toca **Marcar como reunido**.
3. Confirma la acción.

El estado de tu mascota vuelve a **Activo** (sana y salva). El reporte se cierra. Tu puntaje de contribución aumenta.

---

## 17. Notificaciones

### Tipos de notificaciones

| Tipo | Cuándo llega |
|------|-------------|
| **Avistamiento nuevo** | Alguien reportó ver a tu mascota |
| **Chat nuevo** | Alguien escribió en el chat de un caso activo |
| **Mascota perdida cerca** | Una mascota fue reportada perdida en tu área (si tienes alertas activas) |
| **Zona de búsqueda** | Cambios en zonas de búsqueda en las que participas |
| **Sistema** | Actualizaciones de la plataforma |

### Centro de notificaciones

Accede desde el ícono de campana en la barra superior → `/notifications`. Puedes marcar notificaciones individuales como leídas o marcar todas de una vez.

### Push notifications

Para recibir notificaciones incluso cuando la app está cerrada:

1. Ve a **Notificaciones** desde el ícono de campana.
2. Activa **Notificaciones push**.
3. El navegador solicitará permiso — acéptalo.

### Preferencias disponibles

En el centro de notificaciones puedes:

- Marcar todas como leídas
- Activar notificaciones push
- Activar o desactivar **alertas preventivas de riesgo**

### Alertas de zona

Para recibir notificaciones cuando se reporten mascotas perdidas cerca de ti:

1. Ve al **Dashboard**.
2. Busca la sección **Configuración de alertas**.
3. Activa **Alertas de mascotas perdidas cerca de mí**.
4. Permite que la app acceda a tu ubicación.

Tu ubicación se usa solo para enviarte alertas relevantes. No se comparte públicamente.

---

## 18. Red de aliados verificados

Los **aliados** son organizaciones de bienestar animal verificadas por el equipo de PawTrack CR: rescatistas independientes, refugios, protectoras, grupos de rescate.

### ¿Cómo convertirse en aliado?

1. Actualmente el panel de aliados está disponible para cuentas habilitadas por el equipo de PawTrack CR.
2. Si tu cuenta ya tiene acceso a `/allies/panel`, completa el formulario:
   - Nombre de la organización
   - Tipo de organización
   - Área de cobertura (en el mapa)
   - Radio de cobertura en metros
3. El equipo de PawTrack CR revisa tu solicitud.
4. Al ser aprobada, tu organización accede a la bandeja operativa de alertas por zona.

### Beneficios de ser aliado

- Alertas instantáneas cuando hay mascotas perdidas en tu zona
- Acceso al panel de aliado con herramientas adicionales
- Visibilidad en el mapa de aliados de la plataforma
- Estadísticas avanzadas de recuperación

---

## 19. Voluntarios custodia (Fosters)

Los **custodios** son voluntarios que pueden alojar temporalmente a una mascota encontrada mientras se localiza a su dueño.

### Registrarte como custodio

1. Ve a **Perfil**.
2. Completa tu perfil:
   - Ubicación de tu hogar (en el mapa)
   - Especies que aceptas
   - Preferencia de tamaño (Pequeño / Mediano / Grande)
   - Máximo de días disponibles
   - Si estás disponible ahora mismo
3. Guarda tu perfil.

### ¿Cómo funciona la asignación?

Cuando alguien reporta haber encontrado una mascota, el sistema sugiere automáticamente los 3 custodios más cercanos geográficamente como opción para cuidar a la mascota temporalmente. Si te asignan una mascota, recibirás una notificación.

---

## 20. Clínicas veterinarias afiliadas

Las **clínicas afiliadas** son veterinarias verificadas que pueden usar PawTrack CR para registrar visitas y escanear microchips.

### Registrar tu clínica

1. Ve a `/clinica/registro` (accesible sin iniciar sesión).
2. Completa:
   - Nombre de la clínica
   - Número de licencia veterinaria
   - Dirección
   - Ubicación en el mapa
   - Correo electrónico y contraseña
3. El equipo de PawTrack CR revisa y aprueba la clínica.

### Portal de clínica

Una vez aprobada, accede a `/clinica/portal` donde puedes:
- Buscar mascotas por número de microchip
- Escanear el código QR del collar o ingresar el número de microchip RFID
- Ver si la mascota coincide con un perfil registrado en PawTrack

---

## 21. Bot de WhatsApp

Si no tienes acceso a la app web, puedes reportar una mascota perdida directamente por **WhatsApp**.

### Cómo usarlo

1. Envia un mensaje al número de WhatsApp de PawTrack CR.
2. El bot te guiará paso a paso:
   - **Nombre** de tu mascota
   - **Cuándo y dónde** la viste por última vez
   - **Foto** de la mascota
3. Al completar el flujo, el bot crea el reporte automáticamente y te envía el enlace al perfil público.
4. Puedes seguir gestionando el caso desde la web usando ese enlace.

### Privacidad en WhatsApp

El bot **nunca almacena tu número de teléfono**. Solo guarda un hash criptográfico irreversible para mantener el hilo de la conversación. Nadie en PawTrack CR puede ver tu número.

---

## 22. Incentivos y leaderboard

PawTrack CR reconoce a los usuarios que más han contribuido a reunificar mascotas con sus familias.

### Sistema de puntos

- Cada reunificación de tu mascota suma **100 puntos** a tu marcador.
- Los puntos se acumulan a lo largo del tiempo.

### Insignias

| Insignia | Reunificaciones |
|----------|----------------|
| Sin insignia | 0 |
| Bronce | 1–4 |
| Plata | 5–9 |
| Oro | 10+ |

### Leaderboard

El leaderboard muestra a los usuarios con más reunificaciones en la plataforma. Actualmente se visualiza como widget dentro del **Dashboard**.

### Ver tu puntaje

Tu posición y la de otros usuarios se reflejan en el leaderboard del Dashboard.

---

## 23. Estadísticas de recuperación

PawTrack CR muestra estadísticas agregadas de recuperación (sin datos personales) para apoyar la operación y el análisis de casos.

### Acceder a las estadísticas

Ve a `/estadisticas`. Actualmente esta vista requiere rol **Aliado** o **Admin**.

### Qué incluye

- **Tasa de recuperación**: porcentaje de mascotas perdidas que son reunificadas
- **Filtros disponibles**: por especie (perro/gato), raza y cantón
- **Resumen general**: total de reportes, reunificaciones, tiempo promedio de recuperación

---

## 24. Mapa público

El mapa público (`/map`) muestra en tiempo real:

- Mascotas reportadas como perdidas (marcadores de alerta)
- Avistamientos recientes
- Aliados verificados en el área

### Cómo usarlo

- Usa los filtros de la barra lateral para ver solo mascotas perdidas, solo aliados, o ambos.
- Toca cualquier marcador para ver el detalle del reporte.
- Desde un marcador de mascota perdida, puedes acceder directamente a su perfil público.

### Predicción de movimiento

En reportes activos con varios avistamientos, el sistema puede mostrar una **predicción de movimiento**: una estimación del radio probable de la mascota basada en sus avistamientos registrados.

---

## 25. Configuración de perfil y preferencias

### Tu perfil

Accede a `/perfil` para:
- Actualizar tu nombre
- Ver tu correo electrónico
- Configurar tu disponibilidad como voluntario de custodia

### Preferencias de alertas

En la app, las preferencias están repartidas en dos espacios:

- En el **Dashboard** puedes activar o desactivar las alertas de mascotas perdidas cerca de ti.
- En **Notificaciones** puedes activar notificaciones push y las alertas preventivas de riesgo.

Tu ubicación se usa **solo para enviarte alertas relevantes** de mascotas perdidas cerca de ti. No es visible para otros usuarios.

---

## 26. Preguntas frecuentes

**¿Necesito crear una cuenta para reportar un avistamiento?**  
No. Cualquier persona puede reportar un avistamiento escaneando el QR de la mascota o desde el mapa público. No se necesita cuenta ni datos personales.

**¿Mi dirección o teléfono son visibles para extraños?**  
No. Tu teléfono de contacto solo es visible para rescatistas autenticados en el sistema. Tu dirección jamás se expone. El chat es completamente enmascarado.

**¿Qué pasa si olvido mi contraseña?**  
Puedes usar **¿Olvidaste tu contraseña?** en la pantalla de inicio de sesión. Recibirás un enlace o token para crear una nueva contraseña.

**¿Puedo usar la app en iPhone?**  
Sí. PawTrack CR funciona en el navegador Safari de iPhone. Además, puedes instalarla en la pantalla de inicio: en Safari, toca el ícono de compartir → "Agregar a pantalla de inicio".

**¿El código QR funciona aunque no haya internet?**  
El código QR es solo una imagen — funciona como cualquier QR aunque el celular de quien lo escanea necesita conectarse a internet para ver el perfil.

**¿Cuánto cuesta PawTrack CR?**  
La plataforma es gratuita para dueños de mascotas y la comunidad en el MVP actual.

**¿Cómo sé si alguien escaneó el QR de mi mascota?**  
Cada escaneo queda registrado. Puedes ver el historial de escaneos en el perfil de tu mascota: fecha, hora y ubicación aproximada.

**¿Qué hago si encuentro una mascota y el dueño no responde?**  
Puedes registrarte como custodio temporal y usar el sistema de fosters para alojar a la mascota mientras el dueño es localizado.

**¿La app funciona sin conexión a internet?**  
La app puede instalarse como PWA y funciona parcialmente offline gracias al service worker. Sin embargo, las funciones que dependen del servidor (avistamientos, chat, mapa) requieren conexión.

**¿Cómo reporto un problema de seguridad o abuso en la plataforma?**  
Si ves comportamiento sospechoso en el chat, usa el botón "Reportar fraude". Para problemas técnicos o de seguridad, contacta directamente al equipo de PawTrack CR.

---

*PawTrack CR — Cada mascota merece volver a casa.*
