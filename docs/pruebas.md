# PawTrack CR — Guía de Pruebas Locales

> **Solo para entorno local / dev.** Nunca usar estas credenciales en staging o producción.  
> Última actualización: 2026-08-19

---

## Entorno de desarrollo

| Servicio           | URL                                            | Estado esperado                   |
| ------------------ | ---------------------------------------------- | --------------------------------- |
| **Frontend**       | http://localhost:5173                          | Vite PWA (registerType: "prompt") |
| **Backend API**    | http://localhost:5000                          | .NET 9                            |
| **Azurite** (Blob) | http://localhost:10000                         | Emulador Azure Storage            |
| **Base de datos**  | `CPC-davil-ECEKS\SQLEXPRESS` / `PawTrackLocal` | SQL Express                       |

### Iniciar todos los servicios

```powershell
# Desde la raíz del proyecto
pwsh -ExecutionPolicy Bypass -File .\start-dev.ps1 -RestartAzurite
```

### Variables de entorno frontend (`frontend/.env.local`)

```env
VITE_API_URL=http://localhost:5000
VITE_VAPID_PUBLIC_KEY=<opcional-para-push>
```

---

---

## Credenciales de todos los usuarios de prueba

> **Contraseña universal para usuarios `@test.cr` / `@pawtrack.cr`:** `Test123!`

| Email                        | Contraseña   | Rol          | Plan / Tier             | Para probar                                         |
| ---------------------------- | ------------ | ------------ | ----------------------- | --------------------------------------------------- |
| `admin@pawtrack.cr`          | `Test123!`   | Admin        | —                       | Panel admin, activar suscripciones, gestionar roles |
| `admin@pawtrack.test`        | `Admin123!`  | Admin        | —                       | Alternativa admin (seed original)                   |
| `owner_free@test.cr`         | `Test123!`   | Owner        | Explorador (gratis)     | Límite 1 mascota, 5 escaneos, sin GPS/IA avanzada   |
| `owner_plus@test.cr`         | `Test123!`   | Owner        | UserPlus ✅ activo      | GPS tab, radio 10km, IA ilimitada, Case Room        |
| `owner_familia@test.cr`      | `Test123!`   | Owner        | UserFamilia ✅ activo   | Historial médico, múltiples mascotas, PDF export    |
| `owner@pawtrack.test`        | `Test123!`   | Owner        | Explorador (gratis)     | Alternativa owner (seed original)                   |
| `ally@test.cr`               | `Test123!`   | Ally         | — (verificado)          | Panel aliado, alertas de zona, KPIs                 |
| `ally@pawtrack.test`         | `Ally123!`   | Ally         | —                       | Alternativa ally (sin verificar)                    |
| `clinica_basica@test.cr`     | `Test123!`   | Clinic       | ClinicBasic             | Escanear QR/RFID, directorio básico                 |
| `clinica_partner@test.cr`    | `Test123!`   | Clinic       | ClinicPartner ✅ activo | PDF certs, API keys, posición destacada             |
| `clinic@pawtrack.test`       | `Clinic123!` | Clinic       | — (sin plan)            | Alternativa clínica (seed original)                 |
| `municipal_basica@test.cr`   | `Test123!`   | Municipality | Básica                  | Portal capturas básico, un cantón                   |
| `municipal_full@test.cr`     | `Test123!`   | Municipality | Full                    | Fotos, estadísticas, multi-cantón                   |
| `municipal_regional@test.cr` | `Test123!`   | Municipality | RedRegional             | Red regional, múltiples cantones                    |

---

## Aplicar el seed de usuarios extendidos

Si los usuarios `@test.cr` no existen en la base de datos local:

```powershell
sqlcmd -S "CPC-davil-ECEKS\SQLEXPRESS" -d PawTrackLocal -E `
  -i "backend\scripts\seed-extended-test-users.sql"
```

Para los usuarios originales (`@pawtrack.test`):

```powershell
sqlcmd -S "CPC-davil-ECEKS\SQLEXPRESS" -d PawTrackLocal -E `
  -i "backend\scripts\seed-test-users.sql"
```

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
