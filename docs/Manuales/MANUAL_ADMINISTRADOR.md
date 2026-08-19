# Manual de Administrador — PawTrack CR

**Versión:** 2.0  
**Audiencia:** Administradores del sistema  
**Última actualización:** 2026-08-19

---

## Tabla de contenidos

1. [Acceso al panel de administración](#1-acceso-al-panel-de-administración)
2. [Tabs disponibles en el panel](#2-tabs-disponibles-en-el-panel)
3. [Gestión de Aliados](#3-gestión-de-aliados)
4. [Gestión de Clínicas Veterinarias](#4-gestión-de-clínicas-veterinarias)
5. [Gestión de Tiendas de Mascotas](#5-gestión-de-tiendas-de-mascotas)
6. [Vallas Publicitarias](#6-vallas-publicitarias)
7. [Suscripciones y Planes](#7-suscripciones-y-planes)
8. [Promociones y Descuentos](#8-promociones-y-descuentos)
9. [Bundles GPS](#9-bundles-gps)
10. [Preguntas frecuentes](#10-preguntas-frecuentes)

---

## 1. Acceso al panel de administración

### 1.1 Requisitos de rol

Exclusivo para cuentas con rol **Admin**. Si intentas acceder sin ese rol, el sistema te redirige al Dashboard.

### 1.2 Cómo acceder

1. Inicia sesión en `https://pawtrack.cr`.
2. Navega a `/admin` o usa el enlace **Panel de administración** en la barra de navegación.

---

## 2. Tabs disponibles en el panel

| Tab | Función |
|-----|---------|
| **Aliados** | Revisar/aprobar solicitudes de aliados |
| **Clínicas** | Gestionar clínicas veterinarias |
| **Suscripciones** | Ver y gestionar planes de todos los usuarios |
| **Bundles** | Bundles GPS on-demand |
| **Promociones** | Códigos de descuento y promociones |
| **Tiendas** | Aprobar/rechazar tiendas pendientes de registro |
| **Vallas** 🆕 | Crear, editar, activar/pausar vallas publicitarias |

---

## 3. Gestión de Aliados

### 3.1 Revisar solicitudes pendientes

En el tab **Aliados**, verás la lista de perfiles pendientes de verificación con:
- Nombre del aliado
- Tipo de organización
- Zona de cobertura declarada
- Fecha de solicitud

### 3.2 Acciones disponibles

- **Aprobar** → El aliado recibe acceso completo al Panel de Aliado y sus alertas quedan activas.
- **Rechazar** → La solicitud se descarta. El usuario permanece con rol Owner.

---

## 4. Gestión de Clínicas Veterinarias

### 4.1 Solicitudes pendientes

En el tab **Clínicas**, verás clínicas en estado `Pending`:
- Nombre de la clínica
- Dirección y teléfono
- Email de contacto
- Fecha de solicitud

### 4.2 Activar suscripción de clínica

1. Selecciona la clínica → **Activar**.
2. Elige el tier: `ClinicBasic`, `ClinicPlus` o `ClinicPartner`.
3. La clínica recibe acceso inmediato al portal veterinario.

### 4.3 Gestionar API keys

Las clínicas con plan ClinicPartner pueden tener API keys para integración externa. El administrador puede:
- Ver keys activas
- Revocar una key comprometida
- Generar una nueva key

---

## 5. Gestión de Tiendas de Mascotas

### 5.1 Flujo de aprobación

Cuando un dueño de tienda completa el registro en `/tienda/registro`, la tienda queda en estado `Pending`.

En el tab **Tiendas**:

1. Ver lista de tiendas pendientes (nombre, dirección, coordenadas, email de contacto).
2. **Aprobar** → La tienda aparece en el mapa y directorio públicos en estado `Active`.
3. **Rechazar** → La tienda queda en estado `Suspended`.

### 5.2 Tiers de tienda

| Tier | Capacidades |
|------|-------------|
| `StoreBasic` | Catálogo visible, sin pedidos in-app |
| `StorePlus` | Catálogo + pedidos in-app + SINPE |
| `StorePartner` | Todo StorePlus + analytics + posición prioritaria |

Para cambiar el tier de una tienda, usa la suscripción (tab **Suscripciones**) y asigna el tier correspondiente al userId del dueño de la tienda.

---

## 6. Vallas Publicitarias 🆕

El sistema de vallas permite mostrar anuncios en 4 ubicaciones de la app.

### 6.1 Crear una valla

1. Tab **Vallas** → **+ Nueva valla**.
2. Completa:
   - **Título** (máx. 120 chars) — texto principal del anuncio
   - **Descripción** (máx. 300 chars) — texto secundario opcional
   - **Ubicación (Placement):**
     - `Map` — Overlay en el mapa público
     - `Dashboard` — Entre tarjetas de mascotas
     - `Directory` — Top del directorio de tiendas/clínicas
     - `Feed` — Sobre la lista de mascotas perdidas
   - **Inicio y Fin** — ventana de actividad del anuncio
   - **CTA Texto** — etiqueta del botón (ej: "Ver más →")
   - **CTA URL** — URL destino (solo HTTPS o mismo dominio)
   - **Prioridad** (0-100) — si hay varias vallas activas, la de mayor prioridad se muestra primero

3. Click **Crear valla** → la valla queda en estado `Draft`.

### 6.2 Subir imagen

Una vez creada la valla:
1. Click **📷 Imagen** en la fila de la valla.
2. Selecciona la imagen (JPEG, PNG o WebP, máx. 5MB).
3. La imagen se redimensiona automáticamente a 1200px y se guarda en Blob Storage.

### 6.3 Activar / Pausar / Expirar

| Acción | Estado resultante | Efecto |
|--------|-------------------|--------|
| **Activar** | `Active` | Aparece en la app dentro de la ventana de fechas |
| **Pausar** | `Paused` | Desaparece temporalmente; puede reactivarse |
| **Expirar** | `Expired` | Permanente; no se puede reactivar |

### 6.4 Comportamiento en la app

- Solo aparece **1 valla por placement** en cada momento (la de mayor prioridad).
- El usuario puede **cerrar** la valla; no vuelve a aparecer en esa sesión (sessionStorage).
- Si hay múltiples vallas activas para el mismo placement, al cerrar una aparece la siguiente en prioridad.
- Máximo 5 vallas activas por placement para no saturar la UI.

---

## 7. Suscripciones y Planes

### 7.1 Ver suscripciones activas

Tab **Suscripciones** → lista de todos los usuarios con suscripción activa:
- Email del usuario
- Plan actual
- Fecha de activación
- Referencia SINPE del pago

### 7.2 Activar suscripción manualmente

Para activar un plan después de verificar el pago SINPE:

1. Localiza la suscripción en estado `PendingPayment` o `PaymentReported`.
2. Click **Activar**.
3. El usuario recibe acceso inmediato al plan.

### 7.3 Tiers disponibles

| Tier interno | Plan visible |
|-------------|-------------|
| `Free` | Explorador |
| `UserPlus` | Plus (₡2,990/mes) |
| `UserFamilia` | Familia (₡4,990/mes) |
| `ClinicBasic` | Clínica Básica (₡15,000/mes) |
| `ClinicPlus` | Clínica Plus (₡35,000/mes) |
| `ClinicPartner` | Clínica Partner (₡60,000/mes) |
| `StoreBasic` | Tienda Básica (gratis) |
| `StorePlus` | Tienda Plus (₡12,000/mes) |
| `StorePartner` | Tienda Partner (₡25,000/mes) |

---

## 8. Promociones y Descuentos

Tab **Promociones** → gestión de códigos de descuento:

- **Crear código**: define el porcentaje de descuento, fechas de vigencia y usos máximos.
- **Desactivar**: desactiva un código antes de su expiración.
- **Ver redemptions**: qué usuarios usaron cada código.

---

## 9. Bundles GPS

Tab **Bundles** → pedidos de Bundle GPS:

- Lista de pedidos de bundle (hardware collar GPS + suscripción).
- Estado: `PendingPayment` → `Active`.
- Activar manualmente después de verificar el pago.

---

## 10. Preguntas frecuentes

**¿Cómo desbloqueo una cuenta con demasiados intentos fallidos?**  
Ver `RUNBOOK_OPERACIONES.md §12`. Requiere acceso directo a la base de datos.

**¿Puedo cambiar el rol de un usuario directamente?**  
No desde la UI; se requiere acceso a la DB. Los roles se asignan al crear el perfil de aliado, clínica o tienda.

**¿Qué pasa si apruebo una tienda por error?**  
Cambia su estado a `Suspended` via la API admin: `PUT /api/admin/stores/{id}/review` con `{ approve: false }`.

**¿Las vallas aparecen para usuarios no autenticados?**  
Sí. El endpoint `GET /api/billboards?placement=X` es público. Esto es intencional para máxima exposición.

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

| Pestaña      | Contenido                                                                    |
| ------------ | ---------------------------------------------------------------------------- |
| **Aliados**  | Solicitudes de verificación de organizaciones aliadas pendientes de revisión |
| **Clínicas** | Solicitudes de registro de clínicas veterinarias pendientes de activación    |

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

| Botón        | Color | Efecto                                                                                                        |
| ------------ | ----- | ------------------------------------------------------------------------------------------------------------- |
| **Aprobar**  | Verde | Activa la cuenta como aliado verificado. La organización puede acceder a su bandeja operativa inmediatamente. |
| **Rechazar** | Rojo  | Descarta la solicitud. La organización puede volver a aplicar.                                                |

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

| Botón         | Color | Efecto                                                                                              |
| ------------- | ----- | --------------------------------------------------------------------------------------------------- |
| **Activar**   | Verde | Cambia el estado de la clínica a `Activa`. El portal de escaneo queda disponible de inmediato.      |
| **Suspender** | Rojo  | Bloquea el acceso de la clínica al portal. La clínica ve el mensaje "Tu cuenta ha sido suspendida". |

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
