# Manual de Usuario — Rol: Dueño de Mascotas

**Versión:** 2.0 | **Actualización:** Agosto 2026

---

## ¿Qué puede hacer un dueño?

El rol **Dueño** es el rol por defecto al registrarse. Es el núcleo de la plataforma: registras tus mascotas, recibes alertas, coordinas la búsqueda si alguna se pierde, y gestionas el historial médico.

---

## Cuenta y acceso

### Crear cuenta

1. Abre PawTrack CR en tu navegador → **Registrarse**.
2. Ingresa: nombre completo, correo electrónico, contraseña (mínimo 8 caracteres).
3. Verifica tu correo haciendo clic en el enlace que llegará (vigencia 24 h).
4. Inicia sesión con tu correo y contraseña.

### Bloqueo por intentos fallidos

Después de 5 contraseñas incorrectas, la cuenta se bloquea por 15 minutos. No hay forma de saltarse este bloqueo.

### Cerrar sesión

Perfil → **Cerrar sesión**. La sesión se invalida en todos tus dispositivos.

---

## Registrar mascotas

1. Dashboard → **+ Registrar mascota**.
2. Completa: nombre, especie, raza (opcional), fecha de nacimiento (opcional).
3. Sube una foto clara (muy recomendado).
4. Guarda.

**Límites por plan:**
| Plan | Mascotas |
|---|---|
| Explorador | 1 |
| Plus | Hasta 3 |
| Familia | Ilimitadas |

---

## El código QR

Cada mascota tiene un QR único. Desde la pestaña **QR** del perfil de tu mascota:

- **Descargar** la imagen del QR para imprimir y colocar en el collar.
- Ver el **historial de escaneos**: fecha, hora y ubicación aproximada de cada vez que alguien escaneó el QR.
- **Exportar el historial como PDF** (botón en la pestaña QR).
- **Generar avatar para WhatsApp** (disponible solo cuando la mascota está marcada como perdida): imagen con foto + QR superpuesto, ideal para compartir por WhatsApp.

> El QR funciona con cualquier cámara de celular. No requiere que la otra persona tenga cuenta ni la app instalada.

---

## Reportar una mascota perdida

Activa el reporte lo antes posible. Cada hora importa.

1. Perfil de la mascota → **Reportar como perdida**.
2. Completa el formulario:
   - **Último lugar visto**: toca el mapa para marcar el punto.
   - **Fecha y hora** aproximada.
   - **Foto reciente** (los últimos 30 días son ideales).
   - **Descripción**: señas especiales, collar, comportamiento habitual.
   - **Mensaje público**: lo verá cualquier persona que escanee el QR ("Llámame, mi nombre es...").
   - **Nombre de contacto**: puede ser apodo, aparece en el perfil público.
   - **Teléfono de contacto**: solo visible para rescatistas autenticados, nunca público.
   - **Recompensa** (opcional): monto en ₡ + descripción.
3. **Activar reporte**.

El sistema cambia el estado a **Perdida** y notifica automáticamente a:

- Aliados verificados en el radio de alerta (según tu plan: 3 km gratis, 10 km Plus, sin límite Familia).
- Usuarios que tienen activadas las alertas de zona.
- Miembros de tu cuenta familiar (plan Familia).

---

## La sala de caso

Accede desde el Dashboard o desde `/lost/{id}/case`. Desde aquí:

| Acción                       | Descripción                                              |
| ---------------------------- | -------------------------------------------------------- |
| **Ver avistamientos**        | Todos los reportes de avistamientos en mapa y lista      |
| **Chat**                     | Conversación enmascarada con cada rescatista             |
| **Difundir**                 | Enviar el reporte a WhatsApp, Telegram, Facebook, correo |
| **Coordinación de búsqueda** | Cuadrícula 7×7 en tiempo real para búsqueda organizada   |
| **Cambiar estado**           | Activo / Suspendido / Reunificado / Cerrado              |

### Difusión multicanal

Toca **Difundir** para enviar el reporte por todos los canales configurados. Límite: 3 difusiones cada 10 minutos.

### Coordinación de búsqueda

1. Sala de caso → **Activar coordinación de búsqueda**.
2. Comparte el enlace `/lost/{id}/busqueda` con tus voluntarios.
3. Cada voluntario puede reclamar, limpiar o liberar zonas en tiempo real.

---

## Chat seguro

- **Como dueño**, ves todos los hilos de chat iniciados por rescatistas en la sala de caso.
- Tu número de teléfono nunca es visible para el rescatista.
- Si detectas comportamiento sospechoso (piden dinero antes de devolverte la mascota), usa el botón **Reportar fraude**.

---

## Código de entrega segura (Handover)

Cuando vas a reunirte con quien tiene a tu mascota:

1. Sala de caso → **Generar código de entrega**.
2. Un código de 4 dígitos válido por 24 horas.
3. **Comparte el código en persona al momento de la entrega**, nunca por el chat.
4. El rescatista ingresa el código en la app para confirmar la entrega.
5. El sistema confirma la identidad y marca automáticamente cualquier recompensa activa como reclamada.

---

## Marcar como reunificado

1. Perfil de la mascota → **Marcar como reunido**, o desde la sala de caso.
2. Confirma.
3. El estado vuelve a **Activo**. El reporte se cierra.

---

## Cuenta familiar (plan Familia)

Permite que hasta 4 personas más compartan acceso al historial médico y reciban alertas.

### Crear la cuenta familiar

Perfil → sección **Cuenta familiar** → escribe el nombre → **Crear**.

### Invitar miembros

1. Toca **Invitar** en la sección Cuenta familiar.
2. Ingresa el correo.
3. Se genera un enlace único (`/familia/invitacion/{token}`).
4. Compártelo. El invitado debe tener cuenta en PawTrack CR (o crearla al abrir el enlace).
5. Al aceptar, queda vinculado.

> Los miembros pueden ver y agregar registros médicos de tus mascotas, y reciben notificaciones cuando reportas una mascota como perdida.

### Quitar un miembro

En la sección Cuenta familiar, toca **Quitar** junto al nombre del miembro.

---

## Historial médico (plan Familia)

Pestaña **Salud 🏥** en el perfil de cada mascota.

### Tipos de registro disponibles

| Ícono | Tipo            | Ejemplos de uso                      |
| ----- | --------------- | ------------------------------------ |
| 💉    | Vacuna          | Rabia anual, parvovirus, etc.        |
| 🪱    | Desparasitación | Drontal, Milbemax                    |
| 🩺    | Consulta        | Revisión general, diagnóstico        |
| 🔪    | Cirugía         | Esterilización, extracción dental    |
| 💊    | Medicamento     | Antibiótico, antiparasitario crónico |
| 🌿    | Alergia         | Alergia a proteína de pollo, etc.    |
| 📋    | Otro            | Observaciones generales              |

### Agregar un registro

1. Salud → **+ Agregar**.
2. Selecciona el tipo.
3. Fecha (requerida), descripción (requerida), veterinario y clínica (opcionales).
4. **Próxima cita**: si la ingresas, el sistema creará un recordatorio automático.
5. Adjunta un documento (PDF, JPEG o PNG, máx. 5 MB) si quieres conservar el comprobante.
6. **Guardar**.

### Recordatorios de citas

- Se crean automáticamente al agregar una próxima cita.
- **3 días antes** de la fecha, recibes una notificación push (también los miembros de la familia).
- Pendientes aparecen en la parte superior de la pestaña Salud.
- Los vencidos aparecen en rojo.
- Toca **✓ Marcar como hecho** para completarlos.

### Exportar PDF

Toca **Exportar PDF** para descargar el historial completo. Útil para llevar al veterinario o para registros de viaje internacional.

---

## Voluntario de custodia (Foster)

Cualquier dueño puede también ser voluntario de custodia.

1. Perfil → sección de custodio.
2. Configura: ubicación, especies aceptadas, tamaño, días disponibles, disponibilidad actual.
3. Cuando alguien encuentra una mascota perdida, el sistema te puede sugerir como custodio temporal.

---

## Notificaciones

### Activar notificaciones push

Notificaciones → **Activar notificaciones push** → acepta el permiso del navegador.

### Alertas de mascotas perdidas cerca

Dashboard → **Alertas de mascotas perdidas cerca de mí** → activa y permite acceso a tu ubicación.

### Tipos de notificaciones que recibirás

| Tipo                  | Cuándo                           |
| --------------------- | -------------------------------- |
| Avistamiento          | Alguien vio a tu mascota         |
| Chat nuevo            | Rescatista escribió en el chat   |
| Mascota perdida cerca | Reporte nuevo en tu área         |
| Recordatorio vet      | 3 días antes de una cita médica  |
| Sistema               | Actualizaciones de la plataforma |

---

## Plan Explorador vs Plus vs Familia

| Función                                | Explorador   | Plus                | Familia        |
| -------------------------------------- | ------------ | ------------------- | -------------- |
| Mascotas                               | 1            | 3                   | ∞              |
| Búsqueda IA por foto                   | 3/mes        | Ilimitada           | Ilimitada      |
| Historial de escaneos                  | Últimos 5    | Completo            | Completo       |
| Radio de alerta                        | 3 km         | 10 km               | Sin límite     |
| Panel GPS collar                       | —            | ✓                   | ✓              |
| Predicción de movimiento               | —            | ✓                   | ✓              |
| Expediente médico                      | Count teaser | Preview (últimos 3) | ✓ Completo     |
| Editar/eliminar registros médicos      | —            | —                   | ✓              |
| Peso por visita / medicación detallada | —            | —                   | ✓              |
| Vista calendario recordatorios         | —            | —                   | ✓              |
| Dashboard multi-mascota recordatorios  | —            | —                   | ✓              |
| Recordatorios vet (crear/eliminar)     | —            | —                   | ✓              |
| Audit log de acceso veterinario        | —            | —                   | ✓              |
| Cuenta familiar                        | —            | —                   | ✓ (5 miembros) |
| Exportar PDF historial                 | —            | —                   | ✓              |

### Activar Plus o Familia

Perfil → Mi plan → **Mejorar a Plus** (o **Ver Familia**) → SINPE Móvil con el código de referencia generado → **Ya realicé el pago** → el equipo activa el plan en 24 h hábiles.

---

## Probar los features nuevos

Para probar los features implementados en esta sesión:

| Feature                        | Pasos                                                                                     |
| ------------------------------ | ----------------------------------------------------------------------------------------- |
| **Cuenta familiar**            | Perfil → "Cuenta familiar" — necesitas plan Familia activo                                |
| **Invitar miembro**            | Cuenta familiar → Invitar → copia el link y ábrelo en otro navegador/perfil               |
| **Aceptar invitación**         | Abre `/familia/invitacion/{token}` (si no estás autenticado, te redirige a login primero) |
| **Historial médico (Familia)** | Detalle mascota → tab "Salud 🏥" — historial completo, editar, eliminar, exportar PDF     |
| **Preview médico (Plus)**      | Tab "Salud 🏥" → muestra últimos 3 registros + banner de upgrade                          |
| **Agregar registro médico**    | Salud → "+ Registro" → tipo, fecha, descripción, veterinario, peso (opcional), medicación |
| **Editar / eliminar registro** | Tap ✏️ o 🗑️ en cualquier registro que creaste                                             |
| **Recordatorio independiente** | Salud → "⏰ Recordatorio" → tipo, fecha, título, notas                                    |
| **Vista calendario**           | Salud → "📅 Calendario" — muestra dots por día con recordatorios                          |
| **Buscar en historial**        | Barra de búsqueda 🔍 en sección Salud — filtra por descripción/vet/clínica                |
| **Audit log clínica**          | Salud → "🔐 Historial de acceso veterinario" (colapsable)                                 |
| **Exportar PDF médico**        | Salud → "📄 Exportar PDF"                                                                 |
| **Bounty claim**               | Genera código de entrega (Handover) → ingrésalo → la recompensa se marca automáticamente  |

> **Usuario de prueba sugerido:** Usa el usuario con rol `Owner` y plan `UserFamilia` activo (activarlo desde el admin panel o directamente en la BD en desarrollo).

---

_PawTrack CR — Manual del Dueño de Mascotas_
