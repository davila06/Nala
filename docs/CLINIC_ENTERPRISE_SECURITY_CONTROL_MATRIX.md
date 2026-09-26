# PawTrack CR - Control de seguridad enterprise para clínicas

> Estado clínico revisado al 2026-09-26. Este documento acompaña a `CLINIC_DAILY_USE_ENTERPRISE_TODOLIST.md` y registra evidencia verificable; no sustituye la revisión previa a producción.

## Resumen de control

| Control                          | Estado                                      | Evidencia / límite                                                                              |
| -------------------------------- | ------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Catálogo de endpoints clínicos   | Verificado estructuralmente                 | 137 acciones: 110 de `ClinicsController` + 27 de Medical, Certificates y PetClinicAccess.       |
| Acceso anónimo                   | Verificado                                  | Allowlist de 5 acciones; el resto declara autorización.                                         |
| MFA de mutaciones clínicas       | Verificado por metadata y HTTP              | Las 137 acciones inventariadas tienen policy MFA o excepción justificada.                       |
| BOLA/IDOR                        | Parcial                                     | Hay pruebas HTTP por familias críticas, no una petición por cada ruta/recurso.                  |
| Sedes físicas                    | Fase 1 implementada; autorización pendiente | `ClinicOrganizationSite` enlaza cada `Clinic.Id`; selección/permisos de sede siguen pendientes. |
| Migración operativa CP6          | Aplicada en `PawTrackDev`                   | Backfill local: 6 clínicas, 6 organizaciones, 6 sedes y 0 clínicas huérfanas.                   |
| Base compartida/Azure            | No verificable                              | No hay target SQL compartido visible en este entorno.                                           |
| Sesiones/dispositivos confiables | Implementado localmente                     | SessionId, listado/revocación, proof rotado, MFA y revocación de JWT por sesión.                |
| UI de autenticación/seguridad    | Implementado                                | Login MFA y Perfil con setup, recovery codes, step-up y gestión de sesiones/dispositivos.       |

La re-invitación organizacional se validó con una migración aditiva de índice
filtrado y una base LocalDB temporal; staging y producción siguen pendientes.

## Matriz ejecutable de endpoints

La fuente de verdad de la matriz es [ClinicEndpointSecurityMatrixTests.cs](../backend/tests/PawTrack.IntegrationTests/Clinics/ClinicEndpointSecurityMatrixTests.cs). El test contiene los catálogos explícitos de 110 acciones de `ClinicsController` y 27 acciones médicas/certificados/grants, y comprueba:

1. Que el inventario runtime de `ClinicsController` coincide exactamente con el catálogo revisado.
2. Que cada acción inventariada tenga verbo HTTP y template de ruta, incluidos aliases versionados.
3. Que ninguna acción quede sin `[Authorize]` o `[AllowAnonymous]`.
4. Que `AllowAnonymous` coincida con las cuatro rutas públicas de clínica y la verificación pública de certificado.
5. Que toda mutación de los cuatro controladores clínicos use una policy MFA; las diez excepciones justificadas aplican solo al catálogo de `ClinicsController`.
6. Que iniciar o aceptar un grant clínico deniegue una sesión sin MFA y acepte una sesión elevada; escrituras representativas de medical/grant/certificate también retornan 403 sin step-up.

Las rutas de `ClinicsController` se publican bajo `/api/clinics` y `/api/v1/clinics`; el catálogo compara acciones únicas, mientras ASP.NET conserva ambas versiones de ruta.

### Reglas MFA

Policies disponibles:

| Policy                | Límite                                                                                                                                      |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `ClinicOperationsMfa` | Escrituras del titular de clínica que alteran agenda, consulta, expediente, inventario, credenciales de integración o verificación clínica. |
| `ClinicStaffMfa`      | Escrituras operativas por personal con membresía activa de clínica.                                                                         |
| `ClinicFinanceMfa`    | Reversiones, cierres, fiscal y administración de membresías financieras.                                                                    |
| `ClinicAdminMfa`      | Decisiones administrativas de aprobación/verificación clínica; exige rol `Admin` además del claim MFA.                                      |
| `MfaStepUp`           | Medical records/reminders del tutor, consentimiento/grants, revocar sesiones/dispositivos, confiar un dispositivo y desactivar MFA.         |

También requieren step-up el escaneo QR/RFID, verificación de microchip, alta de expediente médico, generación/aceptación de grants, inicio de exportación médica, envío de documentos de verificación, altas/revocaciones/rotación de API keys y las escrituras clínicas existentes de agenda/consulta/inventario.

Excepciones explícitas verificadas por test: registro público de clínica, actualización básica del perfil del titular, envío de cambios de perfil para revisión, telemetría anónima del directorio, carga de logo público, crear venta/cobro rutinario y opt-in de comunicación iniciado por el tutor autenticado. Las acciones de devolución/anulación/cierre/fiscal siguen protegidas con MFA.

## Matriz BOLA/IDOR con evidencia HTTP

| Superficie            | Caso negativo probado                                                              | Resultado esperado                                                           | Test                                                                                                                              |
| --------------------- | ---------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Agenda staff          | Membresía intenta leer/modificar agenda usando otro `clinicId`.                    | Acceso denegado, sin filtrar ni mutar cita ajena.                            | [ClinicAgendaEndpointsTests.cs](../backend/tests/PawTrack.IntegrationTests/Clinics/ClinicAgendaEndpointsTests.cs)                 |
| Consulta clínica      | Titular cierra una consulta persistida bajo otra clínica.                          | `422`/no encontrado de dominio; la consulta extranjera permanece sin cambio. | [ClinicalConsultationEndpointsTests.cs](../backend/tests/PawTrack.IntegrationTests/Clinics/ClinicalConsultationEndpointsTests.cs) |
| Inventario            | Clínica ajusta un lote cuyo `ClinicId` pertenece a otra.                           | `422`; stock del lote ajeno no cambia.                                       | [ClinicInventoryEndpointsTests.cs](../backend/tests/PawTrack.IntegrationTests/Clinics/ClinicInventoryEndpointsTests.cs)           |
| Caja/ventas           | Cajero crea venta en clínica ajena o lee ledger con `saleId` ajeno.                | `422` / `404`; sin acceso al ledger ni creación.                             | [ClinicFinanceEndpointsTests.cs](../backend/tests/PawTrack.IntegrationTests/Clinics/ClinicFinanceEndpointsTests.cs)               |
| CRM                   | Gerencia usa `clinicId` ajeno para dashboard, candidatos, crear o completar tarea. | `403`; no hay lectura ni mutación.                                           | [ClinicCrmEndpointsTests.cs](../backend/tests/PawTrack.IntegrationTests/Clinics/ClinicCrmEndpointsTests.cs)                       |
| Grants del expediente | Otro usuario intenta listar/revocar el grant de una mascota ajena.                 | La lectura devuelve `403`; revocación devuelve `422`, sin cambio.            | [MedicalEndpointsTests.cs](../backend/tests/PawTrack.IntegrationTests/Medical/MedicalEndpointsTests.cs)                           |
| Grants/certificados   | Sesión sin MFA intenta aceptar grant o escribir certificado.                       | `403` antes de leer/modificar el recurso.                                    | [ClinicEndpointSecurityMatrixTests.cs](../backend/tests/PawTrack.IntegrationTests/Clinics/ClinicEndpointSecurityMatrixTests.cs)   |
| Sesiones              | Otro usuario lista o revoca un `SessionId` que no le pertenece.                    | No aparece en su lista; revocación devuelve `404`.                           | [TrustedSessionEndpointsTests.cs](../backend/tests/PawTrack.IntegrationTests/Auth/TrustedSessionEndpointsTests.cs)                |
| Dispositivos          | Otro usuario intenta revocar un trusted device ajeno.                              | `404`; el device legítimo sigue activo.                                      | [TrustedSessionEndpointsTests.cs](../backend/tests/PawTrack.IntegrationTests/Auth/TrustedSessionEndpointsTests.cs)                |

La matriz prueba aislamiento real en familias con mayor impacto, pero **no es una ejecución dinámica de IDOR contra cada parámetro de cada endpoint**. Las rutas restantes se cubren estructuralmente y con pruebas existentes por módulo; la brecha de pruebas individuales permanece abierta y queda rastreada abajo.

## Sesiones y dispositivos confiables

- Cada login asigna un `SessionId`; la rotación del refresh token conserva ese identificador y el claim `sid` del access token.
- El login presenta desafío TOTP/recovery cuando la cuenta lo requiere; Perfil permite setup/enable MFA y muestra recovery codes una sola vez.
- `POST /api/auth/mfa/step-up` valida TOTP o consume un recovery code, emite access token MFA para la misma `sid` y permite usar funciones protegidas después de un refresh.
- El endpoint step-up limita intentos a 5 cada 5 minutos por IP, separado de la cuota de cambio de contraseña.
- `GET /api/auth/me/sessions` devuelve las sesiones activas del usuario autenticado, con `IsCurrent`; la consulta trae un máximo de 100 tokens y agrupa en memoria hasta 50 sesiones.
- `DELETE /api/auth/me/sessions/{sessionId}` exige MFA, revoca todos los refresh tokens de esa sesión y bloquea su `sid` en el blocklist distribuido para invalidar access tokens hermanos inmediatamente.
- Un trusted device persiste solo el hash SHA-256 de una prueba opaca; la cookie es `HttpOnly`, `SameSite=Strict`, `Secure` fuera de Development, limitada a `/api/auth`, expira en 30 días y rota después de un login confiable.
- Enrolar/revocar un dispositivo requiere claim MFA y que la cuenta tenga MFA configurado. La revocación de sesión invalida el device usado por última vez en esa sesión.
- Desactivar MFA exige step-up, revoca todos los dispositivos y refresh sessions y bloquea los access tokens asociados; el cliente vuelve a login.
- Trusted devices **no** reemplazan TOTP para `Admin`, `Support` o `SuperAdmin`. Refresh nunca emite claim `mfa=true`.
- `GET /api/auth/me/trusted-devices` no devuelve el token ni su hash; las pruebas comprueban enrolamiento, rotación, revocación, login posterior rechazado, aislamiento entre usuarios y refresh sin elevación.
- El nombre del dispositivo es auto-declarado y la confianza es una cookie bearer opaca revocable; no hay attestation/hardware binding. No presentarlo como garantía de dispositivo físico administrado.

## Sedes y alcance de aislamiento

**Corte 2026-09-26:** `ClinicOrganizationMembership` (`Owner`,
`Administrator`, `FinanceManager`, `Member`) es identidad organizacional,
no otorga acceso a la API clínica ni a las sedes. `ClinicStaffMembership`
autoriza agenda/consulta/inventario/CRM por `ClinicId`;
`ClinicFinanceMembership` autoriza caja por `ClinicId` con MFA para acciones
sensibles. El inventario es **clinic-scoped**; `LocationName` es texto y no
un identificador autorizable de sede. La organización de una clínica nueva
tiene una sede principal y owner, pero no hay API/UI de selección de sede ni
permisos por `ClinicOrganizationSite`.

La migración aditiva `FilterActiveClinicOrganizationMemberships` cambia el
índice de membresías a único entre filas activas (`IsRevoked = 0`), conservando
las revocadas y permitiendo re-invitación. Validado con test de modelo y
persistencia en base LocalDB temporal; **no** se aplicó al entorno de staging
ni a la base `PawTrackDev`. El backfill de organizaciones proviene de la
migración `AddClinicOrganizations` ya aplicada localmente; verificar conteos,
owner/sede única y ausencia de huérfanos al migrar cada entorno compartido.

El puente organizacional ya modela la relación organización-sede mediante `ClinicOrganizationSite`, conservando `Clinic` como identidad física y `Clinic.Id` como PK de los recursos existentes. Todavía no hay selección ni autorización por sede en los endpoints. El inventario guarda `LocationName` como texto libre, sin FK a una sede. Por tanto:

- Los casos entre clínicas distintas sí son comprobables y están cubiertos en las familias anteriores.
- No se puede afirmar aislamiento por sucursal dentro de una organización mientras no exista `AllowedClinicIds`/scope de sede y contexto activo validado.
- Antes de vender multi-sede se requiere completar membresías/scope, asociar autorización de agenda, consultas, inventario, ventas/cierres, CRM y auditoría al `Clinic.Id` permitido, y decidir si los grants médicos son a la organización o a una sede concreta.
- Criterio de cierre: cada endpoint que lee/escribe recurso con `SiteId` debe recibir sede explícita o resolverla desde sesión/membresía; probar recurso de sede A contra actor limitado a sede B en cada módulo y negar por defecto.

El vínculo `ClinicOrganizationSite` no cambia el alcance de permisos existente:
datos clínicos, ventas, inventario y staff permanecen por `ClinicId`. Operar
varias sedes bajo una organización requiere un contrato de producto, migración
coordinada y pruebas de autorización adicionales.

## Evidencia de migraciones

Comprobación ejecutada con `ASPNETCORE_ENVIRONMENT=Development`:

| Target                             | Resultado                                                            | Acción tomada                                           |
| ---------------------------------- | -------------------------------------------------------------------- | ------------------------------------------------------- |
| `PawTrackDev`                      | Cadena local aplicada hasta `20260926153155_AddClinicOrganizations`. | `database update` explícito; no es un deploy.           |
| `AddClinicOperationalTaskMetadata` | Aplicada localmente; su `Down` elimina tareas sin mascota/tutor.     | Backup/export obligatorios antes de staging/producción. |
| `AddTrustedSessionLifecycle`       | Aplicada localmente; backfill de SessionId y TrustedDevices.         | No aplicada fuera de `PawTrackDev`.                     |
| `AddClinicOrganizations`           | Aplicada localmente; backfill 6/6/6 y 0 huérfanas.                   | No aplicada fuera de `PawTrackDev`.                     |
| Azure SQL compartido               | No localizado en la suscripción seleccionada.                        | Historial externo desconocido.                          |

**No se ejecutó ningún deploy.** Las migraciones sólo se aplicaron a `PawTrackDev` local. Antes de aplicar en staging/producción: confirmar servidor/base/ambiente con el dueño, backup probado, revisar los `Down` destructivos, ejecutar primero en staging y validar counts + smoke tests.

## Control de pendientes

| ID     | Pendiente                                      | Responsable             | Criterio de salida                                                                    | Estado                                                 |
| ------ | ---------------------------------------------- | ----------------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------ |
| SEC-01 | BOLA dinámico de recursos/IDs de 137 acciones. | Backend/Security        | Tabla endpoint × actor × recurso propio/ajeno × status.                               | En progreso; familias críticas cubiertas.              |
| SEC-02 | Selección y autorización de sede física.       | Arquitectura + Clínicas | `AllowedClinicIds`, contexto activo y matriz inter-sede.                              | En progreso; bridge implementado, permisos pendientes. |
| SEC-03 | Aplicar migraciones en staging/compartida.     | Release/DBA             | Target, backup, `__EFMigrationsHistory` y smoke tests.                                | Local aplicado; externo no ejecutado.                  |
| SEC-04 | MFA estructural para acciones nuevas.          | Backend                 | Mantener catálogo 110+27 y mutaciones MFA verdes.                                     | Implementado localmente.                               |
| SEC-05 | Trusted sessions y dispositivos.               | Auth/Security           | Listado, revocación, rotación, JWT hermano, refresh sin MFA y no bypass privilegiado. | Implementado; 6 casos de integración.                  |
| SEC-06 | Revisión de excepciones MFA.                   | Product/Security        | Cada excepción requiere owner, razón y test.                                          | Revisión continua.                                     |

## Comandos de verificación

Desde `frontend/`:

```powershell
dotnet test ..\backend\tests\PawTrack.IntegrationTests\PawTrack.IntegrationTests.csproj --filter "FullyQualifiedName~ClinicEndpointSecurityMatrixTests|FullyQualifiedName~TrustedSessionEndpointsTests" --verbosity minimal
dotnet test ..\backend\tests\PawTrack.IntegrationTests\PawTrack.IntegrationTests.csproj --filter "FullyQualifiedName~Clinics" --verbosity minimal
npx vitest run tests/features/auth/LoginPage.test.tsx tests/features/auth/ProfilePage.test.tsx
npm run typecheck -- --pretty false
```

La primera orden valida catálogo/MFA y ciclo auth; la segunda es el slice de integración clínica. La matriz no sustituye pruebas BOLA específicas para las filas todavía abiertas en SEC-01/SEC-02.
