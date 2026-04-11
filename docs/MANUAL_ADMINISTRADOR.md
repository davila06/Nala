# Manual de Administrador — PawTrack CR

**Versión:** 1.0  
**Audiencia:** Administradores del sistema  
**Última actualización:** Junio 2026

---

## Tabla de contenidos

1. [Acceso al panel de administración](#1-acceso-al-panel-de-administración)
2. [Gestión de solicitudes de aliados](#2-gestión-de-solicitudes-de-aliados)
3. [Gestión de clínicas veterinarias](#3-gestión-de-clínicas-veterinarias)
4. [Flujo de revisión](#4-flujo-de-revisión)
5. [Preguntas frecuentes](#5-preguntas-frecuentes)

---

## 1. Acceso al panel de administración

### 1.1 Requisitos de rol

El panel de administración es exclusivo para cuentas con rol **Admin**. Si intentas acceder sin ese rol, el sistema te redirige automáticamente al Dashboard.

### 1.2 Cómo acceder

1. Inicia sesión con tu cuenta de administrador en `https://pawtrack.cr`.
2. Navega directamente a `/admin` o usa el enlace **Panel de administración** que aparece en la barra de navegación cuando tu cuenta tiene el rol Admin.

### 1.3 Vista general

El panel muestra dos pestañas:

| Pestaña | Contenido |
|---------|-----------|
| **Aliados** | Solicitudes de verificación de organizaciones aliadas pendientes de revisión |
| **Clínicas** | Solicitudes de registro de clínicas veterinarias pendientes de activación |

Cada pestaña indica cuántos ítems hay pendientes. Si no hay pendientes, se muestra un mensaje vacío.

---

## 2. Gestión de solicitudes de aliados

### 2.1 Qué es un aliado

Un aliado es una organización (veterinaria, refugio, comercio pet-friendly, seguridad privada o municipalidad) que se postula para unirse a la red de apoyo de PawTrack CR. Una vez verificada, la organización recibe alertas operativas sobre mascotas perdidas dentro de su zona de cobertura declarada.

### 2.2 Información visible en cada solicitud

Cada tarjeta de solicitud muestra:

- **Nombre de la organización**
- **Tipo de aliado** — puede ser: `Veterinaria`, `Refugio`, `Comercio pet-friendly`, `Seguridad privada` o `Municipalidad`
- **Zona de cobertura** — nombre descriptivo de la zona que declaró el aplicante
- **Fecha de aplicación** — formato `dd/mm/aaaa`

### 2.3 Acciones disponibles

| Botón | Color | Efecto |
|-------|-------|--------|
| **Aprobar** | Verde | Activa la cuenta como aliado verificado. La organización puede acceder a su bandeja operativa inmediatamente. |
| **Rechazar** | Rojo | Descarta la solicitud. La organización puede volver a aplicar. |

### 2.4 Criterios de aprobación sugeridos

Antes de aprobar una solicitud verifica:

1. El nombre de la organización corresponde a una entidad real y reconocible.
2. El tipo de aliado es coherente con el nombre declarado.
3. La zona de cobertura es razonable para el tipo de organización.
4. No existe un aliado duplicado con el mismo nombre y zona.

Si hay dudas, rechaza la solicitud; el aplicante puede re-enviar con información corregida.

---

## 3. Gestión de clínicas veterinarias

### 3.1 Proceso de registro de clínicas

Las clínicas se registran de forma autónoma en `/clinica/registro` sin necesidad de autenticación previa. Al registrarse, su estado inicial es **Pendiente** y el portal de escaneo permanece bloqueado hasta que un administrador la active.

### 3.2 Información visible en cada solicitud de clínica

Cada tarjeta muestra:

- **Nombre de la clínica**
- **Número de licencia SENASA** — ej. `VET-2024-0123`
- **Dirección** — texto descriptivo ingresado durante el registro
- **Correo electrónico de contacto**
- **Fecha de registro** — formato `dd/mm/aaaa`

### 3.3 Acciones disponibles

| Botón | Color | Efecto |
|-------|-------|--------|
| **Activar** | Verde | Cambia el estado de la clínica a `Activa`. El portal de escaneo queda disponible de inmediato. |
| **Suspender** | Rojo | Bloquea el acceso de la clínica al portal. La clínica ve el mensaje "Tu cuenta ha sido suspendida". |

> **Nota:** Suspender una clínica ya activa es una acción reversible — puedes volver a activarla en cualquier momento.

### 3.4 Criterios de revisión sugeridos

Antes de activar una clínica verifica:

1. El número de licencia SENASA tiene el formato correcto y es plausible.
2. El nombre y la dirección son coherentes con una clínica veterinaria real.
3. El correo electrónico de contacto no pertenece a proveedores de email temporal o desechable.
4. No existe otra clínica ya activa con el mismo número de licencia SENASA.

---

## 4. Flujo de revisión

### 4.1 Flujo completo para aliados

```
Organización aplica en /allies/panel
           ↓
  [Panel Admin → pestaña Aliados]
           ↓
   Admin revisa la solicitud
           ↓
    ┌──────┴──────┐
    ▼             ▼
 Aprobar       Rechazar
    ↓             ↓
Estado:        Solicitud
Verified       descartada
    ↓
Organización accede
a bandeja operativa
```

### 4.2 Flujo completo para clínicas

```
Clínica se registra en /clinica/registro
            ↓
Redirigida a /clinica/pendiente
(estado: Pending — portal bloqueado)
            ↓
  [Panel Admin → pestaña Clínicas]
            ↓
    Admin revisa la solicitud
            ↓
    ┌────────┴────────┐
    ▼                 ▼
  Activar          Suspender
    ↓                 ↓
Estado: Active    Estado: Suspended
Portal disponible  Portal bloqueado
```

### 4.3 Tiempo de respuesta recomendado

Para mantener la confianza de las organizaciones que aplican, se recomienda revisar las solicitudes pendientes **dentro de 1–2 días hábiles**.

---

## 5. Preguntas frecuentes

**¿Puedo ver la cobertura geográfica exacta de un aliado antes de aprobar?**  
Actualmente el panel muestra el nombre de la zona declarada y el radio en metros, pero no renderiza un mapa. Para ver el mapa exacto tendrías que consultar la base de datos directamente o pedir al aplicante que lo describa.

**¿Se notifica al aliado cuando apruebo o rechazo su solicitud?**  
Sí. El sistema envía automáticamente una notificación en la plataforma al usuario cuya solicitud fue procesada.

**¿Puedo re-activar una clínica que fue suspendida?**  
Sí. Una clínica suspendida seguirá apareciendo en la pestaña de clínicas del panel. Usa el botón **Activar** para restaurar su acceso.

**¿Qué pasa si apruebo una solicitud por error?**  
Para aliados no hay un botón de "desaprobar" en el panel actual. Ajusta el rol manualmente desde la base de datos o contacta al equipo técnico. Para clínicas puedes usar **Suspender** para bloquear el acceso de inmediato.

**¿El panel tiene paginación?**  
No en la versión actual. Si hay muchas solicitudes simultáneas pueden aparecer todas en una lista desplegable larga. Esta funcionalidad está pendiente para una iteración futura.
