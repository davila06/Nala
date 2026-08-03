# Manual de Usuario — Rol: Administrador

**Versión:** 2.0 | **Actualización:** Agosto 2026

---

## ¿Qué puede hacer un administrador?

El administrador es el equipo interno de PawTrack CR. Tiene acceso total a la plataforma: moderar contenido, verificar aliados, activar clínicas, gestionar suscripciones de usuarios, asignar tiers municipales, revisar reportes de fraude y acceder a todas las estadísticas.

> **Ruta principal:** `/admin`

---

## Acceso

Solo las cuentas con rol `Admin` (asignado directamente en base de datos o por otro Admin) tienen acceso al panel.

1. Inicia sesión normalmente.
2. Ve a `/admin`.

El panel admin está protegido por `[Authorize(Roles = "Admin")]`. Cualquier intento de acceso desde otro rol devuelve 403.

---

## Panel de administración

El panel incluye las siguientes secciones:

### 1. Usuarios

- Listado de todos los usuarios registrados
- Búsqueda por correo, nombre o rol
- Ver detalle de usuario: rol, fecha de registro, estado de verificación de email, plan activo
- **Cambiar rol** de un usuario (Owner / Ally / Clinic / Municipality / Admin)
- **Bloquear / desbloquear** cuenta manualmente

### 2. Suscripciones

- Ver suscripciones activas, pendientes y vencidas
- **Activar suscripción** pendiente de pago (una vez verificado el SINPE): `PUT /api/subscriptions/{id}/activate`
- **Cancelar suscripción** de forma manual
- Asignar suscripción a un usuario directamente

### 3. Clínicas

- Listado de clínicas registradas con su estado (Pendiente / Aprobada / Rechazada)
- **Aprobar** una clínica: cambia el estado a Aprobada y habilita el acceso al portal
- **Rechazar** una clínica con motivo
- Ver métricas de escaneo por clínica

### 4. Aliados

- Listado de perfiles de aliado enviados a revisión
- **Aprobar / rechazar** cada perfil
- Ver área de cobertura en el mapa

### 5. Municipalidades

- Crear o actualizar el perfil municipal de un usuario (asignar cantón, tier y fecha de vencimiento):

```http
POST /api/municipalities/admin/profiles
Authorization: Bearer {admin_jwt}
Content-Type: application/json

{
  "userId": "{guid-del-usuario}",
  "canton": "Alajuela",
  "orgName": "Municipalidad de Alajuela",
  "tier": "Full",
  "expiresAt": "2027-08-01T00:00:00Z",
  "additionalCantons": []
}
```

Tiers válidos: `Basica`, `Full`, `RedRegional`.

Para Red Regional con múltiples cantones:
```json
{
  "additionalCantons": ["Poás", "Grecia", "San Carlos"]
}
```

### 6. Reportes de fraude

- Listado de reportes de fraude enviados desde el chat
- Ver el hilo del chat denunciado
- Marcar como revisado / tomar acción (bloquear usuario, cerrar sesión activa)

### 7. Estadísticas globales

Accede a `/estadisticas` para ver:
- Tasas de recuperación por especie, raza y cantón
- Tendencias mensuales de reportes y reunificaciones

---

## Activar suscripciones de pago (SINPE)

Cuando un usuario reporta haber realizado el pago SINPE:

1. El sistema registra un `PaymentReference` en la suscripción.
2. El panel admin muestra la suscripción en estado **Pago pendiente de verificación**.
3. Verifica manualmente el pago en el extracto SINPE.
4. Si es correcto, activa la suscripción desde el panel admin o via:

```http
PUT /api/subscriptions/{subscriptionId}/activate
Authorization: Bearer {admin_jwt}
```

5. El plan se activa inmediatamente y el usuario recibe una notificación.

---

## Gestión de municipalidades — flujo completo

### Onboarding de una municipalidad nueva

1. **Crear usuario** con rol `Municipality` (panel admin → Usuarios → Nuevo usuario o desde BD).
2. **Asignar perfil municipal** via `POST /api/municipalities/admin/profiles` con el `userId` del usuario, cantón, nombre de organización, tier y fecha de expiración.
3. **Comunicar credenciales** al funcionario municipal.
4. El funcionario inicia sesión y ya puede usar el portal en `/municipalidad`.

### Renovar o cambiar tier

Vuelve a llamar `POST /api/municipalities/admin/profiles` con el mismo `userId` — si el perfil ya existe, se actualiza (upsert).

### Agregar cantones a Red Regional

```json
{
  "userId": "...",
  "canton": "Cantón principal sin cambio",
  "orgName": "...",
  "tier": "RedRegional",
  "expiresAt": "...",
  "additionalCantons": ["Nuevo Cantón 1", "Nuevo Cantón 2"]
}
```

---

## Herramientas de diagnóstico

### Healthcheck

`GET /health` — retorna el estado de todos los servicios registrados (DB, Blob Storage, etc.).

### Logs de aplicación

Los logs estructurados (Serilog) están disponibles en Application Insights en Azure, o en la consola del servidor en desarrollo.

### Seed de datos de prueba

Para entornos de desarrollo, ejecuta el seed de prueba:

```powershell
# Desde la raíz del proyecto
sqllocaldb start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev -i "backend/scripts/seed-test-users.sql"
```

Los usuarios de prueba disponibles están documentados en `usuarios-prueba.md` en la raíz del proyecto.

---

## Probar los features como Administrador

| Feature | Cómo |
|---|---|
| **Panel admin** | `/admin` con cuenta Admin |
| **Activar suscripción** | Crea una suscripción pendiente con un usuario Owner → actívala desde el panel |
| **Aprobar clínica** | Registra una clínica en `/clinica/registro` → apruébala desde el panel |
| **Crear perfil municipal Básica** | `POST /api/municipalities/admin/profiles` con tier `Basica` |
| **Crear perfil municipal Full** | Ídem con tier `Full` |
| **Crear perfil municipal RedRegional** | Ídem con tier `RedRegional` + `additionalCantons` |
| **Ver reportes de fraude** | Usa el chat desde dos usuarios distintos → reporta fraude → verifica en panel |
| **Ver estadísticas** | `/estadisticas` |

### Tabla de usuarios de prueba recomendados

Para probar la totalidad de los features de la plataforma, utiliza los siguientes usuarios (ver `usuarios-prueba.md`):

| Usuario | Rol | Plan | Para probar |
|---|---|---|---|
| admin@pawtrack.cr | Admin | — | Panel admin, activar suscripciones, crear perfiles |
| owner_free@test.cr | Owner | Explorador (gratis) | Funciones básicas, limitaciones de plan |
| owner_plus@test.cr | Owner | UserPlus activo | GPS, historial completo de escaneos, radio 10km |
| owner_familia@test.cr | Owner | UserFamilia activo | Historial médico, cuenta familiar, PDF |
| ally@test.cr | Ally | — | Panel aliado, alertas de zona, coordinación |
| clinica_basica@test.cr | Clinic | ClinicBasic | Escanear QR, estadísticas |
| clinica_partner@test.cr | Clinic | ClinicPartner | Alertas cercanas, API keys, mapa público |
| municipal_basica@test.cr | Municipality | Básica | Solo cantón propio, sin fotos/stats |
| municipal_full@test.cr | Municipality | Full | Fotos, bulk, estadísticas, multi-cantón |
| municipal_regional@test.cr | Municipality | RedRegional | Dashboard regional, transferencias |

> Crea los perfiles municipales con `POST /api/municipalities/admin/profiles` como Admin antes de probar.

---

_PawTrack CR — Manual del Administrador_
