# PawTrack CR — Guía de Pruebas Locales

> **Solo para entorno local / dev.** Nunca usar estas credenciales en staging o producción.
> Última actualización: 2026-09-08 (auditado contra la BD que el backend usa REALMENTE al correr `start-dev.ps1`)

---

## ⚠️ Base de datos real usada por `start-dev.ps1` — NO es `PawTrackLocal`

`start-dev.ps1` fija `ASPNETCORE_ENVIRONMENT=Development`, que carga `appsettings.Development.json` → conecta a:

```
Server=(localdb)\MSSQLLocalDB;Database=PawTrackDev
```

**`PawTrackLocal` (en `CPC-davil-ECEKS\SQLEXPRESS`) es una base distinta, separada, que NO usa el backend cuando lo levantas con `start-dev.ps1`.** Una auditoría previa de este documento se hizo contra `PawTrackLocal` por error — esa base tenía los usuarios `@test.cr` pero el backend real corriendo nunca los ve. Todos los comandos `sqlcmd`/seeds de esta guía apuntan ahora a `PawTrackDev` (la base real). Si en tu máquina `start-dev.ps1` usa otro `ASPNETCORE_ENVIRONMENT`, ajusta el `-S`/`-d` de los comandos de abajo según corresponda.

```powershell
# Conectarse a la BD real que usa el backend en dev:
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev -Q "SELECT Email, Role FROM dbo.Users;"
```

---

## Entorno de desarrollo

| Servicio           | URL                                      | Estado esperado                                              |
| ------------------ | ---------------------------------------- | ------------------------------------------------------------ |
| **Frontend**       | http://localhost:5173                    | Vite PWA (registerType: "prompt")                            |
| **Backend API**    | http://localhost:5199                    | .NET 9 (ver `launchSettings.json`; NO es 5000)               |
| **Azurite** (Blob) | http://localhost:10000                   | Emulador Azure Storage                                       |
| **Base de datos**  | `(localdb)\MSSQLLocalDB` / `PawTrackDev` | LocalDB (usada por `start-dev.ps1`, environment=Development) |

> ✅ **Nota (2026-09-08): seed extendido ejecutado en `PawTrackDev`.** `owner_plus@test.cr`, `owner_familia@test.cr` y `clinica_partner@test.cr` tienen suscripciones activas con vencimiento renovado por un mes. Si vuelven a vencer, reejecuta `backend/scripts/seed-extended-test-users.sql`.

```powershell
# Desde la raíz del proyecto
pwsh -ExecutionPolicy Bypass -File .\start-dev.ps1 -RestartAzurite
```

### Variables de entorno frontend (`frontend/.env.local`)

```env
VITE_API_URL=http://localhost:5199
VITE_VAPID_PUBLIC_KEY=<opcional-para-push>
```

---

---

## Credenciales de todos los usuarios de prueba

> **Contraseña universal para usuarios `@test.cr` / `@pawtrack.cr`:** `Test123!`
> (el hash reutiliza el de `Test123!` del seed original — ver comentario en `seed-extended-test-users.sql`)

| Email                        | Contraseña   | Rol          | Plan / Tier                              | Para probar                                                       |
| ---------------------------- | ------------ | ------------ | ---------------------------------------- | ----------------------------------------------------------------- |
| `admin@pawtrack.cr`          | `Test123!`   | Admin        | —                                        | Panel admin, activar suscripciones, gestionar roles               |
| `admin@pawtrack.test`        | `Admin123!`  | Admin        | —                                        | Alternativa admin (seed original)                                 |
| `owner_free@test.cr`         | `Test123!`   | Owner        | Explorador (gratis)                      | Límite 1 mascota, 5 escaneos, sin GPS/IA avanzada                 |
| `owner_plus@test.cr`         | `Test123!`   | Owner        | UserPlus ⚠️ ver nota                     | GPS tab, radio 10km, IA ilimitada, Case Room                      |
| `owner_familia@test.cr`      | `Test123!`   | Owner        | UserFamilia ⚠️ ver nota                  | Historial médico, múltiples mascotas, PDF export                  |
| `owner@pawtrack.test`        | `Test123!`   | Owner        | UserFamilia ✅ activo (hasta 2026-10-02) | Alternativa owner (seed original) — **ya NO es gratis**, ver nota |
| `ally@test.cr`               | `Test123!`   | Ally         | — (Verified)                             | Panel aliado, alertas de zona, KPIs                               |
| `ally@pawtrack.test`         | `Ally123!`   | Ally         | —                                        | Alternativa ally (sin verificar)                                  |
| `clinica_basica@test.cr`     | `Test123!`   | Clinic       | ClinicBasic                              | Escanear QR/RFID, directorio básico                               |
| `clinica_partner@test.cr`    | `Test123!`   | Clinic       | ClinicPartner ⚠️ ver nota                | PDF certs, API keys, posición destacada                           |
| `clinic@pawtrack.test`       | `Clinic123!` | Clinic       | — (sin plan)                             | Alternativa clínica (seed original)                               |
| `municipal_basica@test.cr`   | `Test123!`   | Municipality | Básica                                   | Portal capturas básico, un cantón                                 |
| `municipal_full@test.cr`     | `Test123!`   | Municipality | Full                                     | Fotos, estadísticas, multi-cantón                                 |
| `municipal_regional@test.cr` | `Test123!`   | Municipality | RedRegional                              | Red regional, múltiples cantones                                  |

> ✅ **Nota (2026-09-08): seed extendido ejecutado en `PawTrackDev`.** `owner_plus@test.cr`, `owner_familia@test.cr` y `clinica_partner@test.cr` tienen suscripciones activas con vencimiento renovado por un mes. Si vuelven a vencer, reejecuta `backend/scripts/seed-extended-test-users.sql`.
>
> ```powershell
> sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev -i "backend\scripts\seed-extended-test-users.sql"
> ```

### Usuarios adicionales para probar el panel Admin (aprobaciones pendientes)

Estos vienen de `backend/scripts/seed-admin-data.sql` (ejecutar después de `seed-test-users.sql`) y no requieren aprobación previa para hacer login — sirven para poblar las pestañas "Aliados pendientes" / "Clínicas pendientes" del panel admin:

| Email                             | Contraseña | Rol    | Estado                | Para probar                         |
| --------------------------------- | ---------- | ------ | --------------------- | ----------------------------------- |
| `maria.garcia@pawtrack.test`      | `Test123!` | Ally   | AllyProfile `Pending` | Aprobar/rechazar aliado individual  |
| `patitas.felices@pawtrack.test`   | `Test123!` | Ally   | AllyProfile `Pending` | Aprobar/rechazar ONG                |
| `refugio.animal.cr@pawtrack.test` | `Test123!` | Ally   | AllyProfile `Pending` | Aprobar/rechazar refugio            |
| `animal.house@pawtrack.test`      | `Test123!` | Clinic | Clinic `Pending`      | Aprobar/rechazar clínica (San José) |
| `vet.angeles@pawtrack.test`       | `Test123!` | Clinic | Clinic `Pending`      | Aprobar/rechazar clínica (Alajuela) |

> **Rol `ServiceProvider` sin usuario sembrado actualmente**: `seed-test-users.sql` define `provider@pawtrack.test` / `Test123!` — confirmado presente en `PawTrackDev`. `Store` y `Support` ahora tienen cuenta de prueba (ver tabla abajo).

### Nueva cuenta de prueba: rol Store

| Email                   | Contraseña | Rol   | Estado         | Para probar                                 |
| ----------------------- | ---------- | ----- | -------------- | ------------------------------------------- |
| `tienda_activa@test.cr` | `Test123!` | Store | Store `Active` | Directorio de pet stores, catálogo, pedidos |

### Nueva cuenta de prueba: rol Support

| Email                       | Contraseña | Rol     | Estado | Para probar                                                                       |
| --------------------------- | ---------- | ------- | ------ | --------------------------------------------------------------------------------- |
| `soporte_bienestar@test.cr` | `Test123!` | Support | N/A    | `GET/POST /api/admin/welfare-cases/*` (triage de casos de maltrato sin ser Admin) |

> `Support` no tiene entidad de perfil propia — solo es un valor de `Users.Role`, normalmente asignado vía `AssignSupportRoleCommand` (acción de Admin). Este usuario se sembró directamente con `Role='Support'` para facilitar pruebas sin ese paso extra.

---

## Cómo probar el Pasaporte de Vacunas (PDF) de una mascota

**Cuenta a usar: `clinica_partner@test.cr` / `Test123!`** — es la única cuenta con todo el flujo habilitado para emitir y descargar el pasaporte (`IssueVaccinePassportCommand` solo permite `SubscriptionTier.ClinicPartner`, clínica `Active`, verificación `Verified`, veterinario `Authorized`, y un `ClinicMedicalAccessGrant` activo sobre la mascota).

Esta sesión dejó preparada toda la cadena de prerrequisitos en `PawTrackDev` para que el flujo funcione de inmediato:

1. Suscripción `ClinicPartner` activa (`Status=Active`, `ExpiresAt` a 1 mes).
2. `ClinicVerification` con `Status=Verified` para la clínica de `clinica_partner@test.cr`.
3. `ClinicVeterinarian` con `Status=Authorized` ("Dra. Vet Partner Test").
4. `ClinicMedicalAccessGrant` activo entre esa clínica y la mascota **Max** (perro, propiedad de `owner@pawtrack.test`).

Pasos para probar en la UI:

1. Login como `clinica_partner@test.cr` / `Test123!`.
2. Ir al portal de la clínica → expediente de **Max** (mascota de `owner@pawtrack.test`, ya tiene acceso otorgado).
3. "Emitir Pasaporte de Vacunas" → completa al menos una vacuna (Max es perro, **debe incluir la vacuna de rabia** o el backend rechaza la emisión).
4. Descarga el PDF y verifica el código público en `http://localhost:5173/verificar/{code}` (no requiere login).

> Si el prerrequisito se pierde (p. ej. porque vuelves a correr `seed-extended-test-users.sql`, lo cual **regenera el `Id` de la fila `Clinics`** y desvincula `ClinicVerifications`/`ClinicVeterinarians`/`ClinicMedicalAccessGrants` existentes), vuelve a ejecutar el bloque de re-vinculación con el nuevo `ClinicId` — ver notas de la sesión 2026-09-08 en la memoria del repo, o pide que se regenere.

---

## Aplicar el seed de usuarios extendidos

Si los usuarios `@test.cr` no existen en la base de datos real usada por `start-dev.ps1` (`PawTrackDev`):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev `
  -i "backend\scripts\seed-extended-test-users.sql"
```

Para los usuarios originales (`@pawtrack.test`):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev `
  -i "backend\scripts\seed-test-users.sql"
```

Para los usuarios de aprobación pendiente del panel Admin (ejecutar después de los dos anteriores):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev `
  -i "backend\scripts\seed-admin-data.sql"
```

Para poblar escenarios enterprise de collares, GPS, zonas seguras y vallas (ejecutar después del seed extendido):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev -f 65001 `
  -i "backend\scripts\seed-enterprise-demo-data.sql"
```

> Si tu entorno local usa `PawTrackLocal` en `CPC-davil-ECEKS\SQLEXPRESS` en vez de LocalDB (por ejemplo, si cambiaste `ASPNETCORE_ENVIRONMENT` a `Local`), sustituye `-S "(localdb)\MSSQLLocalDB" -d PawTrackDev` por `-S "CPC-davil-ECEKS\SQLEXPRESS" -d PawTrackLocal -E` en los tres comandos.

---

## Guía de pruebas por rol

### Como Admin (`admin@pawtrack.cr`)

| Feature                        | Ruta                                      |
| ------------------------------ | ----------------------------------------- |
| Panel de administración        | `/admin`                                  |
| Revisar aliados pendientes     | `/admin` → pestaña Aliados                |
| Aprobar clínicas               | `/admin` → pestaña Clínicas               |
| Ver / activar suscripciones    | `/admin` → pestaña Suscripciones          |
| Estadísticas globales          | `/estadisticas`                           |
| Crear perfil municipal vía API | `POST /api/municipalities/admin/profiles` |

---

### Como Owner Explorador (`owner_free@test.cr`)

| Feature              | Qué esperar                                                        |
| -------------------- | ------------------------------------------------------------------ |
| Registrar mascota    | Permite 1 mascota; al intentar la 2.ª debe mostrar bloqueo de plan |
| Reportar pérdida     | Funciona; radio de alertas 3 km                                    |
| Ver tab GPS          | Tab visible pero muestra "Activa Plus para conectar"               |
| Búsqueda IA por foto | Permitido hasta 3/mes                                              |
| Panel "Ver planes"   | Botón en Dashboard → abre FreemiumModal                            |

---

### Como Owner Plus (`owner_plus@test.cr`)

| Feature              | Qué esperar                                  |
| -------------------- | -------------------------------------------- |
| Registrar mascotas   | Hasta 3 mascotas                             |
| Tab GPS              | Formulario de conexión Tractive/Kippy activo |
| Búsqueda IA por foto | Ilimitada                                    |
| Case Room            | Sala de coordinación completa activa         |
| Radio de alertas     | 10 km                                        |

---

### Como Owner Familia (`owner_familia@test.cr`)

| Feature                | Qué esperar                           |
| ---------------------- | ------------------------------------- |
| Mascotas               | Sin límite                            |
| Historial médico       | Tab médico disponible en cada mascota |
| Exportar PDF historial | Botón activo                          |
| Multi-usuario          | Invitar hasta 5 miembros de familia   |

Datos sembrados para esta prueba:

| Mascota      | Pet ID                                 | Historial disponible                             |
| ------------ | -------------------------------------- | ------------------------------------------------ |
| Nala Familia | `AA100001-0000-0000-0000-000000000001` | 4 registros médicos, actividades y recordatorios |
| Milo Familia | `AA100001-0000-0000-0000-000000000002` | 3 registros médicos, actividad y recordatorios   |

El seed deja 7 registros médicos, 3 actividades y 3 recordatorios en total
para el usuario Familia. Incluye vacunas, desparasitación, chequeos, una
medicación estructurada, peso y una alergia registrada.

---

### Como Ally (`ally@test.cr`)

| Feature          | Qué esperar                                              |
| ---------------- | -------------------------------------------------------- |
| Panel aliado     | `/allies/panel` → bandeja de alertas activa              |
| KPIs             | Alertas recibidas, respondidas, tasa de respuesta, radio |
| Confirmar acción | Botón "Ya buscamos en nuestra área" en cada alerta       |

---

### Como Clínica Básica (`clinica_basica@test.cr`)

| Feature            | Qué esperar                                                        |
| ------------------ | ------------------------------------------------------------------ |
| Portal clínica     | `/clinica/portal`                                                  |
| Escanear QR        | Input de código QR → retorna datos de mascota                      |
| Escanear microchip | Input RFID → búsqueda ISO 11784                                    |
| Banner de upgrade  | Muestra "Ver planes" → ClinicTiersModal                            |
| Emitir certificado | Botón visible pero al intentar → error 422 "Requiere plan Partner" |

---

### Como Clínica Partner (`clinica_partner@test.cr`)

| Feature                        | Qué esperar                                        |
| ------------------------------ | -------------------------------------------------- |
| Todo lo de Básica              | ✅                                                 |
| Emitir certificado PDF         | Formulario completo habilitado                     |
| Verificar certificado          | `GET /api/certificates/verify/{code}` devuelve 200 |
| Página pública verificación    | `http://localhost:5173/verificar/{code}`           |
| Lista de certificados emitidos | Visible en el portal debajo del botón              |

---

### Como Municipalidad (`municipal_basica@test.cr`)

| Feature               | Qué esperar                                                                 |
| --------------------- | --------------------------------------------------------------------------- |
| Portal municipal      | `/municipalidad` → panel operativo visible al autenticarse                  |
| Registrar captura     | Formulario activo, crea registro                                            |
| Filtrar por cantón    | Solo "Desamparados" disponible (tier Básica)                                |
| Vincular con PawTrack | Campo "N° chip/collar" → si coincide con mascota registrada, aparece enlace |

---

## Datos enterprise sembrados: collares y vallas

Ejecutar `seed-enterprise-demo-data.sql` deja estos escenarios idempotentes:

| Escenario                 | Cuenta / dato                        | Qué probar                                                                                                                                   |
| ------------------------- | ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| GPS activo                | `owner_plus@test.cr` → `Toby GPS`    | Última ubicación en Heredia, batería 76%, historial de 4 puntos y zona segura `Casa Heredia`.                                                |
| Collar offline            | `owner_plus@test.cr` → `Toby GPS`    | Segundo collar genérico sin reportar durante 5 horas, batería 12% y alertas de conectividad/batería.                                         |
| Tag activado              | `PT-A3F9-0001234`                    | Estado `Activated`, vinculado al collar GPS activo.                                                                                          |
| Tags disponibles          | `PT-B4E1-0001235`, `PT-C7D2-0001236` | Activación de CollarTag desde la UI.                                                                                                         |
| Credencial de dispositivo | Collar de `Toby GPS`                 | Prueba de ingestión: la clave de demostración se almacena solo como hash; crear una nueva clave por API/UI para pruebas de ingestión reales. |
| Valla VIP                 | `PawTrack GPS`                       | Placement Map, VIP, Heredia, aprobada y activa; permite probar prioridad, métricas y límite diario.                                          |
| Valla de recuperación     | `Red de Recuperación CR`             | Placement Feed, San José, aprobada y activa; verifica el filtro de categorías de recuperación.                                               |

> Los seeds no cargan binarios a Azurite. Las vallas usan imágenes remotas de placeholder para que el flujo visual funcione; subir una imagen desde Admin reemplaza el creativo por Blob Storage.

---

## Flujo de prueba de pago SINPE (extremo a extremo)

1. Login como `owner_free@test.cr`
2. Dashboard → clic en "Activa Plus" → elige plan Plus → clic "Continuar con SINPE"
3. El sistema genera referencia de 8 chars (ej. `ABC12345`)
4. En una transferencia real, colocar `ABC12345` exactamente en el asunto o descripción/mensaje de SINPE. En el entorno de prueba, simular el pago activando manualmente vía Admin:
   - Login como `admin@pawtrack.cr` → `/admin` → Suscripciones → buscar la referencia → Activar
5. Volver al usuario → refrescar → el plan Plus debe estar activo

---

## Flujo de prueba de Bounty (recompensa)

1. Login como `owner_plus@test.cr`
2. Registra una mascota → repórtala como perdida
3. Abre el Case Room (`/lost/{eventId}`) → pestaña Acciones
4. Scroll hasta "Recompensa" → ingresa ₡25,000 → clic "Continuar con SINPE"
5. El widget muestra la referencia de depósito
6. Simula el depósito: clic "Ya deposité"
7. El estado cambia a "Activa 🟢" y aparece en el mapa
8. Desde otro usuario (`owner_free@test.cr`): reporta un avistamiento
9. Al confirmar entrega con HandoverCode → el owner ve "Liberar recompensa"

---

## IDs de usuarios (GUIDs)

| Email                        | GUID                                   |
| ---------------------------- | -------------------------------------- |
| `admin@pawtrack.cr`          | `AA000001-0000-0000-0000-000000000001` |
| `owner_free@test.cr`         | `AA000002-0000-0000-0000-000000000002` |
| `owner_plus@test.cr`         | `AA000003-0000-0000-0000-000000000003` |
| `owner_familia@test.cr`      | `AA000004-0000-0000-0000-000000000004` |
| `ally@test.cr`               | `AA000005-0000-0000-0000-000000000005` |
| `clinica_basica@test.cr`     | `AA000006-0000-0000-0000-000000000006` |
| `clinica_partner@test.cr`    | `AA000007-0000-0000-0000-000000000007` |
| `municipal_basica@test.cr`   | `AA000008-0000-0000-0000-000000000008` |
| `municipal_full@test.cr`     | `AA000009-0000-0000-0000-000000000009` |
| `municipal_regional@test.cr` | `AA000010-0000-0000-0000-000000000010` |

---

_PawTrack CR — Guía de Pruebas Locales v1.0_
