# Manual de Usuario — Rol: Clínica Veterinaria

**Versión:** 2.0 | **Actualización:** Agosto 2026

---

## ¿Qué puede hacer una clínica?

Las clínicas veterinarias afiliadas a PawTrack CR tienen acceso a un **portal especializado** para:

- Identificar mascotas por microchip o código QR
- Registrar visitas y escaneos
- Ver estadísticas de escaneos propios
- Configurar claves API para integraciones externas
- Recibir alertas de mascotas perdidas cercanas (planes Plus y Partner)

---

## Registrar tu clínica

1. Ve a `/clinica/registro` (no requiere iniciar sesión).
2. Completa:
   - **Nombre** de la clínica
   - **Número de licencia veterinaria** (requerido)
   - **Dirección** y **ubicación en el mapa**
   - **Correo electrónico** y **contraseña**
3. Toca **Registrar clínica**.
4. El equipo de PawTrack CR revisa y aprueba la clínica.
5. Recibirás un correo de confirmación cuando tu clínica sea aprobada.

---

## Acceder al portal

Una vez aprobada, inicia sesión con las credenciales registradas. El sistema te llevará automáticamente al portal de clínica (`/clinica/portal`).

Si la aprobación está pendiente, verás la pantalla `/clinica/pendiente` con el estado de tu solicitud.

---

## Portal de clínica — Pestañas

### 🔍 Escanear

La función principal del portal. Úsala cuando un paciente entra a consulta.

**Flujo de escaneo:**

1. Toca **Escanear QR** para abrir el escáner de cámara (escanea el QR del collar).
   — O bien, ingresa manualmente el número de **microchip RFID**.
2. El sistema busca la mascota en PawTrack CR.
3. Si hay coincidencia, se muestra:
   - Nombre, especie, raza, foto
   - Estado actual (Normal / **Perdido**)
   - Si la mascota está reportada como perdida: datos de contacto del dueño y enlace a la sala de caso
4. El escaneo queda registrado en el historial de la mascota.

> Si la mascota **está perdida**, el sistema notifica automáticamente al dueño que fue vista en tu clínica.

### 📊 Estadísticas

Visualiza tus métricas de escaneo:

- Total de escaneos por período
- Distribución por especie
- Top mascotas más frecuentes
- Escaneos del día/semana/mes

### 🔑 Claves API (plan Clínica Plus y Partner)

Para integrar PawTrack CR con tu software de gestión veterinaria (PMS):

1. Pestaña **API** → **Crear nueva clave**.
2. Asigna un nombre descriptivo (ej. "PMS Principal").
3. Copia la clave generada (solo se muestra una vez).
4. Usa la clave en el encabezado `X-Api-Key` de tus solicitudes HTTP.
5. Puedes revocar claves en cualquier momento tocando **Revocar**.

**Endpoints disponibles con API Key:**

- `GET /api/clinics/scan/{microchipOrQr}` — buscar mascota por chip o QR
- `POST /api/clinics/scan` — registrar escaneo programático

### 🚨 Alertas cercanas (plan Plus y Partner)

Muestra las mascotas perdidas reportadas en un radio configurable alrededor de tu clínica. Útil para identificar mascotas que llegan sin dueño o para consultas de emergencia.

- Las alertas se actualizan automáticamente.
- Cada alerta incluye: foto, descripción, último lugar visto, distancia estimada a tu clínica.
- Toca una alerta para ver el perfil público completo.

---

## Planes para clínicas

| Plan                | Precio      | Destacado                                                                   |
| ------------------- | ----------- | --------------------------------------------------------------------------- |
| **Clínica Básica**  | ₡9,900/mes  | Portal base, escaneos, estadísticas                                         |
| **Clínica Plus**    | ₡19,900/mes | Todo Básica + Alertas cercanas, API Key                                     |
| **Clínica Partner** | ₡29,900/mes | Todo Plus + logo en mapa público, notificaciones a dueños, soporte dedicado |

### Beneficios exclusivos del plan Partner

- Tu clínica aparece **en el mapa público** con un marcador destacado (borde naranja) y tu logo.
- El popup del mapa muestra tu teléfono, sitio web y badge **"VERIFICADA"**.
- Los dueños de mascotas perdidas que buscan atención veterinaria te encuentran fácilmente.

---

## Aparecer en el mapa público

Solo disponible con el plan **Clínica Partner**. El equipo de PawTrack CR activa esta función al aprobar el plan.

Para maximizar visibilidad:

- Sube tu **logo** desde el portal (pestaña Configuración).
- Asegúrate de que tu dirección en el mapa sea precisa.

---

## Cargar / actualizar logo

1. Portal de clínica → pestaña **Configuración** (o el ícono de ajustes).
2. Toca **Subir logo**.
3. Selecciona una imagen (PNG o JPEG, recomendado 512×512 px).
4. El logo se guarda y se muestra en el mapa público (si eres Partner) y en el portal.

---

## Notificaciones push

1. Portal → sección de notificaciones → **Activar notificaciones push**.
2. Acepta el permiso del navegador.

Recibirás notificaciones cuando:

- Una mascota que escaneas en tu clínica esté perdida.
- Se reporte una mascota perdida en tu área (planes Plus y Partner).

---

## Expediente médico digital del paciente

### Acceso al expediente

Para ver y agregar registros al expediente médico de un paciente, tu clínica debe tener acceso activo. Hay tres formas:

| Opción                           | Cómo funciona                                                                                                                        |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| **A — Código del dueño**         | El dueño genera un código de 8 caracteres desde su app y te lo comparte. Lo ingresas en tu portal para activar el acceso permanente. |
| **B — Código de la clínica**     | Tú generates un código desde el portal y se lo compartes al dueño para que lo active en su app.                                      |
| **C — Escaneo durante consulta** | Cuando escaneas el QR o chip de la mascota durante una visita, tienes acceso temporal hasta 90 días.                                 |

### ¿Qué puedes hacer con el expediente?

- **Ver** historial completo de la mascota: vacunas, consultas, cirugías, medicamentos, alergias, peso por visita.
- **Agregar** registros médicos con firma de tu clínica (aparecen marcados como "🏥 Clínica" para el dueño).
- **Registrar** la fecha de próxima cita — el sistema crea un recordatorio automático para el dueño.

### Plan gating importante

El dueño **no necesita** Plan Familia para que su clínica acceda y escriba en el expediente. La clínica puede agregar registros en cualquier momento. El dueño necesita Plan Familia para **leer** los registros desde su app. Si no tiene el plan, ve un contador: _"Tu veterinaria ha agregado N registros. Actualiza para verlos."_

### Auditoría de acceso

Cada vez que tu clínica consulta el expediente de un paciente, **se registra automáticamente** el acceso con fecha y hora. El dueño puede ver este historial en la sección 🔐 de su pestaña Salud. Es una medida de transparencia y confianza — no afecta tu operación, pero es importante saberlo.

### Acceso en el portal

1. Portal → Escanear QR/chip del paciente.
2. Una vez identificada la mascota, toca **📋 Ver expediente**.
3. En la pestaña Expediente puedes ver el historial y agregar registros.

---

## Verificación de certificados

Los certificados médicos emitidos por clínicas afiliadas pueden verificarse en `/clinica/verificar-certificado`.

Los dueños de mascotas pueden compartir el código de verificación de un certificado para que terceros (aerolíneas, hoteles pet-friendly, etc.) confirmen su autenticidad.

---

## Probar los features como Clínica

| Feature                  | Cómo                                                                                    |
| ------------------------ | --------------------------------------------------------------------------------------- |
| **Escanear QR**          | Portal → Escanear → apunta la cámara al QR de una mascota de prueba                     |
| **Buscar por microchip** | Portal → Escanear → ingresa manualmente el número de chip                               |
| **Ver expediente**       | Escanear mascota → "📋 Ver expediente"                                                  |
| **Agregar registro**     | Pestaña Expediente → rellenar formulario → Guardar                                      |
| **Ver estadísticas**     | Portal → pestaña Estadísticas                                                           |
| **API Keys**             | Portal → pestaña API (requiere plan Plus o Partner)                                     |
| **Alertas cercanas**     | Portal → pestaña 🚨 Alertas (requiere plan Plus o Partner)                              |
| **Logo en mapa**         | Sube el logo y verifica en `/map` con filtro de clínicas activo (requiere plan Partner) |

> **Usuario de prueba sugerido:** Cuenta con rol `Clinic`, clínica aprobada, y plan `ClinicPlus` o `ClinicPartner` activo para probar las funciones premium.

---

_PawTrack CR — Manual de la Clínica Veterinaria_
