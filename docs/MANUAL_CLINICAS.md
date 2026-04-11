# Manual de Clínicas Veterinarias — PawTrack CR

**Versión:** 1.0  
**Audiencia:** Clínicas veterinarias afiliadas a la red PawTrack CR  
**Última actualización:** Junio 2026

---

## Tabla de contenidos

1. [¿Qué es una clínica afiliada PawTrack?](#1-qué-es-una-clínica-afiliada-pawtrack)
2. [Registro de la clínica](#2-registro-de-la-clínica)
3. [Estado de cuenta — etapas](#3-estado-de-cuenta--etapas)
4. [Portal de escaneo](#4-portal-de-escaneo)
5. [Cómo escanear una mascota](#5-cómo-escanear-una-mascota)
6. [Resultado de escaneo](#6-resultado-de-escaneo)
7. [Preguntas frecuentes](#7-preguntas-frecuentes)

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

| Campo | Descripción | Ejemplo |
|-------|-------------|---------|
| **Nombre de la clínica** | Nombre oficial del establecimiento | Clínica Veterinaria Los Yoses |
| **Número de licencia SENASA** | Número de licencia veterinaria emitida por el SENASA | VET-2024-0123 |
| **Dirección** | Dirección descriptiva del establecimiento | 300m norte del parque central, San José |
| **Latitud / Longitud** | Coordenadas geográficas de la clínica (pre-rellenadas con San José centro) | 9.9281 / -84.0908 |
| **Correo electrónico de contacto** | Email con el que se comunicará el equipo PawTrack | clinica@ejemplo.cr |
| **Contraseña** | Contraseña para acceder al portal (mínimo 8 caracteres) | — |
| **Confirmar contraseña** | Repetición de la contraseña para verificar | — |

> **Nota sobre coordenadas:** Los valores de latitud y longitud están pre-rellenados con coordenadas aproximadas de San José. Ajusta los valores numéricos si tu clínica está en otra provincia o cantón.

### 2.3 Enviar el registro

Haz clic en **Solicitar registro**. Si los datos son válidos y las contraseñas coinciden, serás redirigido automáticamente a la página de espera (`/clinica/pendiente`).

---

## 3. Estado de cuenta — etapas

Tu cuenta de clínica puede estar en tres estados:

| Estado | Descripción | Acceso al portal |
|--------|-------------|-----------------|
| **Pendiente** | Tu solicitud fue recibida y está en revisión por el equipo PawTrack | ❌ Bloqueado |
| **Activa** | Tu clínica fue aprobada | ✅ Portal disponible |
| **Suspendida** | Tu cuenta fue suspendida por el equipo PawTrack | ❌ Bloqueado |

### 3.1 Tiempo de activación

El equipo de PawTrack revisa las solicitudes en **1–2 días hábiles**. No es necesario que hagas nada durante este período.

### 3.2 Mientras la cuenta está pendiente

Al iniciar sesión verás la pantalla de espera con el mensaje:  
*"Tu clínica está en revisión. PawTrack activará tu cuenta en 1-2 días hábiles."*

### 3.3 Si la cuenta fue suspendida

Al iniciar sesión verás el mensaje:  
*"Tu cuenta ha sido suspendida. Contacta al equipo de PawTrack para más información."*

Escribe a **soporte@pawtrack.cr** indicando el nombre de tu clínica y número de licencia SENASA para gestionar la reactivación.

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
- Nota de confirmación: *"Se ha notificado al dueño que su mascota fue vista aquí."*

El dueño recibe una notificación automática en la plataforma en ese mismo momento.

### 6.2 Mascota no encontrada

Si el QR o microchip no coincide con ninguna mascota registrada en PawTrack, aparece el mensaje:  
*"No hay ninguna mascota registrada con ese QR o microchip en PawTrack."*

En este caso puedes:
- Intentar con el otro método de escaneo (QR ↔ RFID).
- Contactar al dueño por otros medios si la mascota tiene placa con teléfono.

### 6.3 Escanear otra mascota

Después de ver el resultado (encontrada o no), haz clic en **Escanear otra mascota** o **Intentar de nuevo** para volver al estado inicial del portal y procesar la siguiente mascota.

---

## 7. Preguntas frecuentes

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
