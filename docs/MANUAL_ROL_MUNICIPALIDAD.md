# Manual de Usuario — Rol: Municipalidad

**Versión:** 2.0 | **Actualización:** Agosto 2026

---

## ¿Qué puede hacer una municipalidad?

El rol **Municipalidad** permite a entidades municipales costarricenses gestionar el control animal de su cantón directamente en la plataforma. Las funciones disponibles dependen del **plan (tier)** contratado.

---

## Tiers municipales

| Tier | Precio aprox. | Acceso |
|---|---|---|
| **Básica** | ₡150,000/año | Portal base: registrar capturas, actualizar estado, buscar en cantón propio |
| **Full** | ₡300,000/año | Todo Básica + fotos en capturas, estadísticas de cantón, búsqueda multi-cantón, API |
| **Red Regional** | ₡500,000/año | Todo Full + múltiples cantones, transferencias inter-municipales, dashboard regional |

El tier es asignado por el equipo de PawTrack CR. El administrador puede crearlo o modificarlo via `POST /api/municipalities/admin/profiles`.

---

## Acceder al portal

1. El equipo de PawTrack CR crea tu cuenta con rol `Municipality` y asigna el tier.
2. Inicia sesión en PawTrack CR con tu correo y contraseña.
3. Ve a `/municipalidad` (o accede desde el menú lateral si tu cuenta es Municipal).

---

## Ver tu perfil y tier activo

**Endpoint:** `GET /api/municipalities/profile`

Desde el portal de municipalidad puedes ver:

- Nombre de tu organización
- Cantón primario
- Tier activo (`Basica` / `Full` / `RedRegional`)
- Cantons adicionales autorizados (RedRegional)
- Fecha de vencimiento del plan

---

## Plan Básica — Funciones disponibles

### Registrar un animal capturado

Cuando un animal es capturado por la red de control animal municipal:

1. Portal → **+ Nuevo registro**.
2. Completa:
   - **Cantón** (pre-cargado con tu cantón autorizado)
   - **Especie** (Perro, Gato, etc.)
   - **Color** (requerido)
   - **Raza** (opcional)
   - **Edad estimada** (opcional, ej. "1-2 años")
   - **Notas** (observaciones de campo)
   - **Número de collar o chip** (si tiene)
   - **Fecha y hora de captura** (por defecto: ahora)
3. **Guardar**.

El animal queda registrado con estado **Recibido**.

**Endpoint:** `POST /api/municipalities/captures`

### Buscar registros

Filtra por estado y fecha desde el portal.

| Filtro | Descripción |
|---|---|
| Estado | Recibido / Dueño localizado / Transferido / Liberado / Adoptado |
| Fecha | Rango de fechas de captura |

> **Restricción Básica:** Solo puedes ver animales de tu cantón propio, sin importar qué filtro de cantón apliques.

**Endpoint:** `GET /api/municipalities/captures?canton=&status=&page=`

### Actualizar estado

Al resolver el caso de un animal, actualiza su estado:

1. En la lista de capturas, toca el animal.
2. Cambia el estado:
   - **Recibido** → estado inicial al registrar
   - **Dueño localizado** → el dueño fue contactado vía PawTrack o búsqueda manual
   - **Transferido** → el animal fue enviado a otro refugio (en Básica y Full sin destino)
   - **Liberado** → animal salvaje, no mascota
   - **Adoptado** → adoptado por un tercero
3. Opcionalmente, vincula el registro a un perfil de mascota en PawTrack (si se identificó al dueño).
4. Guarda.

**Endpoint:** `PUT /api/municipalities/captures/{id}/status`

---

## Plan Full — Funciones adicionales

Incluye todo lo del plan Básica más:

### Subir foto al registro (Full+)

1. En el detalle de un registro de captura, toca **Subir foto**.
2. Selecciona una imagen (JPEG o PNG, máx. 5 MB).
3. La foto se guarda y se asocia al registro.

Útil para identificar visualmente al animal o para cruzar con búsquedas de mascotas perdidas.

**Endpoint:** `POST /api/municipalities/captures/{id}/photo` (multipart/form-data)

### Actualización masiva de estado (Full+)

Para procesar múltiples animales a la vez:

1. En la lista de capturas, selecciona los registros con el checkbox.
2. Toca **Actualizar estado masivo**.
3. Elige el nuevo estado.
4. Confirma.

**Endpoint:** `PUT /api/municipalities/captures/bulk-status`

> Máximo 50 registros por operación.

### Estadísticas del cantón (Full+)

Ve a **Estadísticas** en el menú del portal para ver:

| Métrica | Descripción |
|---|---|
| Total capturado | Total de animales registrados en el período |
| Recibido | Actualmente en custodia |
| Dueño localizado | Casos resueltos positivamente |
| Transferido | Enviados a otra institución |
| Liberado | Animales silvestres liberados |
| Adoptado | Animales adoptados |
| **Tasa de recuperación** | % de dueños localizados sobre el total |
| Actividad últimos 30 días | Gráfico de capturas diarias |

**Endpoint:** `GET /api/municipalities/stats?canton=`

### Búsqueda multi-cantón (Full+)

Con el plan Full, si tu perfil tiene múltiples cantones autorizados (configurados por el administrador), puedes filtrar por cualquiera de ellos desde el portal.

---

## Plan Red Regional — Funciones adicionales

Incluye todo lo del plan Full más:

### Dashboard regional (Red Regional)

Vista consolidada de todos los cantones bajo tu contrato:

| Columna | Descripción |
|---|---|
| Cantón | Nombre del cantón |
| Total | Animales registrados |
| Activo | En custodia actualmente |
| Dueño localizado | Casos resueltos |
| Tasa | % recuperación |

Al final del dashboard: **Total regional** y **tasa de recuperación regional**.

**Endpoint:** `GET /api/municipalities/regional`

### Transferencia entre municipalidades (Red Regional)

Cuando un animal debe ser transferido a otro cantón dentro de tu red:

1. En el detalle del registro de captura, toca **Transferir**.
2. Selecciona el **cantón de destino** (solo cantons de tu red).
3. Agrega una nota de transferencia (motivo, condición del animal, contacto en destino).
4. Confirma.

El estado del animal cambia a **Transferido** y la nota queda registrada.

**Endpoint:** `POST /api/municipalities/captures/{id}/transfer`

---

## Integración vía API (Full+)

Con una clave API, puedes consultar e ingresar registros desde tu sistema de gestión municipal (SIG, ERP, etc.).

> **Nota:** Las claves API para municipalidades son generadas por el administrador de PawTrack CR, no desde el portal de municipalidad directamente en esta versión.

**Autenticación:** `Authorization: Bearer {jwt_token}` o `X-Api-Key: {api_key}` (configuración futura).

---

## Endpoints de referencia

| Método | Endpoint | Tier mínimo | Descripción |
|---|---|---|---|
| GET | `/api/municipalities/profile` | Básica | Ver perfil y tier actual |
| GET | `/api/municipalities/captures` | Básica | Buscar capturas (cantón propio) |
| POST | `/api/municipalities/captures` | Básica | Registrar animal capturado |
| PUT | `/api/municipalities/captures/{id}/status` | Básica | Actualizar estado individual |
| POST | `/api/municipalities/captures/{id}/photo` | Full | Subir foto al registro |
| PUT | `/api/municipalities/captures/bulk-status` | Full | Actualización masiva |
| GET | `/api/municipalities/stats` | Full | Estadísticas del cantón |
| GET | `/api/municipalities/regional` | Red Regional | Dashboard regional |
| POST | `/api/municipalities/captures/{id}/transfer` | Red Regional | Transferir entre cantones |
| POST | `/api/municipalities/admin/profiles` | Admin | Crear/actualizar perfil municipal |

---

## Cómo configurar un usuario municipal (para el Administrador)

1. Crea la cuenta del usuario con rol `Municipality` (desde el panel admin o directamente en BD).
2. Haz una solicitud autenticada como Admin:

```http
POST /api/municipalities/admin/profiles
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "userId": "guid-del-usuario-municipal",
  "canton": "San José",
  "orgName": "Municipalidad de San José",
  "tier": "Full",
  "expiresAt": "2027-08-01T00:00:00Z",
  "additionalCantons": []
}
```

Para Red Regional con múltiples cantones:
```json
{
  "userId": "...",
  "canton": "San José",
  "orgName": "AMSJ — Área Metropolitana",
  "tier": "RedRegional",
  "expiresAt": "2027-08-01T00:00:00Z",
  "additionalCantons": ["Escazú", "Desamparados", "Goicoechea"]
}
```

---

## Probar los features como Municipalidad

| Feature | Cómo probar |
|---|---|
| **Ver perfil/tier** | `GET /api/municipalities/profile` o portal → sección Perfil |
| **Registrar captura** | Portal → "+ Nuevo registro" con datos de prueba |
| **Buscar capturas** | Portal → lista de capturas, aplica filtros |
| **Actualizar estado** | Toca un registro → cambia estado |
| **Subir foto** (Full+) | Toca un registro → "Subir foto" (requiere tier Full) |
| **Bulk update** (Full+) | Selecciona múltiples registros → "Actualizar estado masivo" |
| **Estadísticas** (Full+) | Portal → menú Estadísticas |
| **Dashboard regional** (Red Regional) | Portal → menú Regional |
| **Transferir** (Red Regional) | Detalle de captura → "Transferir" → selecciona cantón destino |

> **Usuarios de prueba:**
> - `Municipality` con tier `Basica` → para probar restricciones de cantón único y sin fotos.
> - `Municipality` con tier `Full` → para probar fotos, estadísticas y bulk update.
> - `Municipality` con tier `RedRegional` + múltiples cantones → para dashboard regional y transferencias.
> - Usar el endpoint `POST /api/municipalities/admin/profiles` con el JWT del Admin para crear estos perfiles.

### Verificar que las restricciones de tier funcionan

1. Con usuario Básica, intenta `POST /api/municipalities/captures/{id}/photo` → deberías recibir **402**.
2. Con usuario Básica, intenta `GET /api/municipalities/stats` → **402**.
3. Con usuario Full, intenta `GET /api/municipalities/regional` → **402**.
4. Con usuario Full, `GET /api/municipalities/stats` → **200** con estadísticas reales.
5. Con usuario Red Regional, `GET /api/municipalities/regional` → **200** con dashboard multi-cantón.

---

_PawTrack CR — Manual de la Municipalidad_
