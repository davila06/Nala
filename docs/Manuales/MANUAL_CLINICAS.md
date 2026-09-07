# Manual de Clínicas Veterinarias — PawTrack CR

**Versión:** 2.0  
**Audiencia:** Clínicas veterinarias afiliadas a la red PawTrack CR  
**Última actualización:** 2026-09-06

---

## Tabla de contenidos

1. [¿Qué es una clínica afiliada PawTrack?](#1-qué-es-una-clínica-afiliada-pawtrack)
2. [Registro de la clínica](#2-registro-de-la-clínica)
3. [Estado de cuenta — etapas](#3-estado-de-cuenta--etapas)
4. [Portal de escaneo](#4-portal-de-escaneo)
5. [Cómo escanear una mascota](#5-cómo-escanear-una-mascota)
6. [Resultado de escaneo](#6-resultado-de-escaneo)
7. [Pasaporte veterinario SENASA-ready](#7-pasaporte-veterinario-senasa-ready)
8. [Preguntas frecuentes](#8-preguntas-frecuentes)

---

## 1. ¿Qué es una clínica afiliada PawTrack?

Las clínicas veterinarias afiliadas a PawTrack CR forman parte de la red de identificación de mascotas. Cuando una mascota llega a tu consultorio y no conoces quién es su dueño, puedes escanear su código QR del collar o leer su microchip RFID para obtener de forma inmediata el nombre de la mascota y los datos de contacto de su dueño.

Al identificarla, el sistema **notifica automáticamente al dueño** que su mascota fue vista en tu clínica.

---

## 2. Registro de la clínica

### 2.1 Acceso al formulario de registro

El registro es público y no requiere cuenta previa. Accede a `/clinica/registro` o haz clic en el enlace **Registrar mi clínica** en la página principal.

### 2.2 Datos requeridos

Completa el formulario con los siguientes datos:

| Campo                              | Descripción                                                                | Ejemplo                                 |
| ---------------------------------- | -------------------------------------------------------------------------- | --------------------------------------- |
| **Nombre de la clínica**           | Nombre oficial del establecimiento                                         | Clínica Veterinaria Los Yoses           |
| **Número de licencia SENASA**      | Número de licencia veterinaria emitida por el SENASA                       | VET-2024-0123                           |
| **Dirección**                      | Dirección descriptiva del establecimiento                                  | 300m norte del parque central, San José |
| **Latitud / Longitud**             | Coordenadas geográficas de la clínica (pre-rellenadas con San José centro) | 9.9281 / -84.0908                       |
| **Correo electrónico de contacto** | Email con el que se comunicará el equipo PawTrack                          | clinica@ejemplo.cr                      |
| **Contraseña**                     | Contraseña para acceder al portal (mínimo 8 caracteres)                    | —                                       |
| **Confirmar contraseña**           | Repetición de la contraseña para verificar                                 | —                                       |

> **Nota sobre coordenadas:** Los valores de latitud y longitud están pre-rellenados con coordenadas aproximadas de San José. Ajusta los valores numéricos si tu clínica está en otra provincia o cantón.

### 2.3 Enviar el registro

Haz clic en **Solicitar registro**. Si los datos son válidos y las contraseñas coinciden, serás redirigido automáticamente a la página de espera (`/clinica/pendiente`).

---

## 3. Estado de cuenta — etapas

Tu cuenta de clínica puede estar en tres estados:

| Estado         | Descripción                                                         | Acceso al portal     |
| -------------- | ------------------------------------------------------------------- | -------------------- |
| **Pendiente**  | Tu solicitud fue recibida y está en revisión por el equipo PawTrack | ❌ Bloqueado         |
| **Activa**     | Tu clínica fue aprobada                                             | ✅ Portal disponible |
| **Suspendida** | Tu cuenta fue suspendida por el equipo PawTrack                     | ❌ Bloqueado         |

### 3.1 Tiempo de activación

El equipo de PawTrack revisa las solicitudes en **1–2 días hábiles**. No es necesario que hagas nada durante este período.

### 3.2 Mientras la cuenta está pendiente

Al iniciar sesión verás la pantalla de espera con el mensaje:  
_"Tu clínica está en revisión. PawTrack activará tu cuenta en 1-2 días hábiles."_

### 3.3 Si la cuenta fue suspendida

Al iniciar sesión verás el mensaje:  
_"Tu cuenta ha sido suspendida. Contacta al equipo de PawTrack para más información."_

Escribe a **soporte@pawtrack.cr** indicando el nombre de tu clínica y número de licencia SENASA para gestionar la reactivación.

## 3.4 Planes de clínica

El registro y el perfil de directorio son la entrada gratuita. Los planes comerciales activos son:

| Tier interno    |      Precio | Capacidades principales                                                                                                |
| --------------- | ----------: | ---------------------------------------------------------------------------------------------------------------------- |
| `ClinicPlus`    | ₡15,000/mes | Destacado en mapa, badge verificado, estadísticas de escaneos, métricas de visibilidad y certificados PDF verificables |
| `ClinicPartner` | ₡35,000/mes | Todo ClinicPlus, API keys, widget embebible y endpoints especializados                                                 |

Los gates se validan en el backend con una suscripción activa y no solo desde la interfaz. `ClinicPartner` es necesario para API keys, widget, integraciones y emisión de pasaportes veterinarios digitales SENASA-ready; `ClinicPlus` habilita las métricas y la visibilidad premium.

---

## 4. Portal de escaneo

### 4.1 Acceso al portal

1. Inicia sesión en `https://pawtrack.cr` con el correo y contraseña que registraste.
2. El sistema te lleva directamente al portal de escaneo en `/clinica/portal`.

### 4.2 Encabezado del portal

En la parte superior verás:

- Emoji 🏥 + nombre de tu clínica
- Número de licencia SENASA
- Badge verde **Activa** confirmando que tu cuenta está operativa

---

## 5. Cómo escanear una mascota

El portal ofrece dos métodos de identificación:

### 5.1 Método 1 — Cámara QR (recomendado para collares)

> Disponible solo en dispositivos y navegadores que soporten la API `BarcodeDetector` (Chrome en Android/desktop, Edge).

1. En el portal, haz clic en el botón de activar cámara (sección "Escanear mascota").
2. Apunta la cámara al código QR del collar de la mascota.
3. El sistema detecta automáticamente el código y procesa el escaneo sin necesidad de presionar ningún botón adicional.
4. La cámara se detiene sola una vez que se detecta el código.

### 5.2 Método 2 — Entrada manual (QR o microchip RFID)

1. En el campo de texto del portal, ingresa:
   - La URL completa del QR (si la lees con un lector externo), ej: `https://pawtrack.cr/p/abc123`
   - O el número de identificación del microchip RFID (solo el número, sin prefijos)
2. Haz clic en **Buscar** o presiona Enter.

> **Cómo distingue el sistema el tipo de entrada:** Si el valor ingresado comienza con `http`, se trata como código QR. Cualquier otro texto se interpreta como número de microchip RFID.

### 5.3 Durante el procesamiento

Mientras el sistema busca la coincidencia, el botón queda deshabilitado y muestra indicador de carga. Si el escaneo demora más de unos segundos, puede haber un problema de conectividad.

---

## 6. Resultado de escaneo

### 6.1 Mascota encontrada

Si PawTrack tiene registrada la mascota, aparece una tarjeta verde con:

- **Foto de la mascota** (si el dueño la subió)
- **Nombre de la mascota**
- **Especie**
- **Nombre del dueño**
- **Correo electrónico del dueño** (enlace `mailto:` para abrir tu cliente de correo directamente)
- Nota de confirmación: _"Se ha notificado al dueño que su mascota fue vista aquí."_

El dueño recibe una notificación automática en la plataforma en ese mismo momento.

### 6.2 Mascota no encontrada

Si el QR o microchip no coincide con ninguna mascota registrada en PawTrack, aparece el mensaje:  
_"No hay ninguna mascota registrada con ese QR o microchip en PawTrack."_

En este caso puedes:

- Intentar con el otro método de escaneo (QR ↔ RFID).
- Contactar al dueño por otros medios si la mascota tiene placa con teléfono.

### 6.3 Escanear otra mascota

Después de ver el resultado (encontrada o no), haz clic en **Escanear otra mascota** o **Intentar de nuevo** para volver al estado inicial del portal y procesar la siguiente mascota.

---

## 7. Pasaporte veterinario SENASA-ready

El portal de clínica permite emitir un pasaporte veterinario digital verificable cuando se cumplen todas estas condiciones:

- la clínica está activa;
- la clínica tiene plan `ClinicPartner` activo;
- administración verificó la licencia de la clínica para emisión de certificados;
- la clínica registró al menos un veterinario autorizado;
- la mascota tiene un grant activo de acceso al expediente médico para la clínica;
- el formulario incluye las vacunas requeridas, incluida rabia para perros.

El documento generado incluye código y QR de verificación pública. Esa verificación muestra datos mínimos: estado, tipo, mascota, especie, clínica emisora, fecha y vigencia. El PDF completo se descarga solo desde una sesión autorizada.

Este flujo es **SENASA-ready**: está preparado para trazabilidad sanitaria y revisión documental, pero no sustituye trámites oficiales ni implica integración o aprobación oficial de SENASA.

### 7.1 Emisión

1. Abre `/clinica/portal`.
2. Confirma que el plan activo sea `ClinicPartner`.
3. Solicita verificación de la clínica y sube el documento de respaldo si aún no está cargado.
4. Registra el veterinario, sube su documento y espera aprobación administrativa.
5. Selecciona un veterinario autorizado.
6. Ingresa el ID PawTrack de la mascota con acceso médico activo.
7. Completa color/señas visibles, vacuna, marca, lote, fecha de aplicación y vigencia.
8. Agrega control antiparasitario si aplica.
9. Presiona **Emitir pasaporte SENASA-ready**.

### 7.2 Verificación documental

El panel **Verificación SENASA-ready** permite:

- solicitar revisión de la clínica;
- subir documentos privados de respaldo;
- ver si la verificación está pendiente, aprobada, rechazada o vencida;
- registrar veterinarios para revisión;
- subir documento de veterinario;
- subir firma o sello opcional;
- revocar un veterinario que ya no debe emitir.

Los documentos no se publican ni se exponen en el verificador público.

### 7.3 Revocación

Si un pasaporte fue emitido con datos incorrectos, solicita o ejecuta la revocación con motivo. Un documento revocado seguirá siendo verificable públicamente, pero aparecerá como **Revocado**.

---

## 8. Preguntas frecuentes

**¿Necesito instalar alguna aplicación para usar el portal?**  
No. El portal es una aplicación web progresiva (PWA). Accedes desde el navegador de cualquier computadora, tableta o teléfono. Para usar la cámara QR, Chrome (Android o desktop) ofrece la mejor compatibilidad.

**¿Qué pasa si no tengo cámara disponible o no funciona el escaneo?**  
Usa siempre el campo de texto manual. Con un lector de códigos de barras USB o un lector RFID conectado al teclado puedes ingresar los datos directamente en el campo y el sistema los procesa igual.

**¿Mis datos de acceso (email y contraseña) son los mismos que los de una cuenta individual PawTrack?**  
No. Las clínicas tienen cuentas separadas. El email y contraseña que registraste en `/clinica/registro` son exclusivos del portal de clínicas.

**¿El dueño puede ver que fue mi clínica la que escaneó a su mascota?**  
La notificación al dueño confirma que la mascota fue vista, pero no expone el nombre de tu clínica en la versión actual del sistema.

**¿Puedo cambiar la contraseña de la cuenta de la clínica?**  
La funcionalidad de cambio de contraseña para cuentas de clínicas no está disponible en el panel actual. Si necesitas restablecerla, contacta al equipo PawTrack.

**¿Qué hago si la mascota llega en mal estado de salud?**  
El portal solo sirve para identificación. Para emergencias veterinarias, actúa según tus protocolos clínicos habituales y usa los datos del dueño que aparecen en el resultado para coordinar.

**¿Qué pasa si ingreso un número de microchip erróneo?**  
El sistema simplemente devuelve "mascota no encontrada". No hay penalización por intentos fallidos. Verifica el número leyendo el microchip nuevamente y reintenta.
