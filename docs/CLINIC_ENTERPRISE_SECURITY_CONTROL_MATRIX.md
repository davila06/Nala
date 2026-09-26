# PawTrack CR - Control de seguridad enterprise para clínicas

> Estado al 2026-09-25. Este documento acompaña a `CLINIC_DAILY_USE_ENTERPRISE_TODOLIST.md` y registra evidencia verificable; no sustituye la revisión previa a producción.

## Resumen de control

| Control                          | Estado                                 | Evidencia / límite                                                                                                                                                 |
| -------------------------------- | -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Catálogo de endpoints clínicos   | Verificado estructuralmente            | 137 acciones únicas: 110 de `ClinicsController` + 27 de `MedicalController`, `CertificatesController` y `PetClinicAccessController`; test exacto de inventario.    |
| Acceso anónimo                   | Verificado                             | Allowlist de 5 acciones: 4 de `ClinicsController` y `CertificatesController.Verify`; el resto declara autorización.                                                |
| MFA de mutaciones clínicas       | Verificado por metadata y HTTP         | Todas las escrituras de las 137 acciones clínicas inventariadas usan policy MFA, salvo 10 excepciones de bajo riesgo del controlador clínico con razón registrada. |
| BOLA/IDOR                        | Parcial, probado por familias críticas | Agenda, consulta, inventario, caja/ventas, CRM, grants seleccionados, sesiones y dispositivos tienen pruebas HTTP. No hay una petición IDOR por cada ruta/recurso. |
| Sedes físicas                    | Bloqueado por modelo                   | No existe `ClinicSite`/`ClinicLocation`; `Clinic` es el límite de tenant actual. `ClinicInventoryLot.LocationName` es texto, no una sede autorizable.              |
| Migración operativa CP6          | Pendiente en `PawTrackDev`             | La consulta EF de solo lectura muestra `AddClinicOperationalTaskMetadata (Pending)`.                                                                               |
| Base compartida/Azure            | No verificable desde este entorno      | No hay conexiones MSSQL guardadas y Resource Graph no devuelve servidores ni bases SQL en la suscripción Azure seleccionada. No se afirma su estado.               |
| Sesiones/dispositivos confiables | Implementado; migración pendiente      | Refresh sessions con `SessionId`, listado/revocación, proof de dispositivo rotado y almacenado como hash, MFA y revocación inmediata de JWT por sesión.            |
| UI de autenticación/seguridad    | Implementado                           | Login pide TOTP/recovery si backend responde `MFA_REQUIRED`; Perfil permite setup, recovery codes, step-up, confianza y revocación.                                |

## Matriz ejecutable de endpoints

La fuente de verdad de la matriz es [ClinicEndpointSecurityMatrixTests.cs](../backend/tests/PawTrack.IntegrationTests/Clinics/ClinicEndpointSecurityMatrixTests.cs). El test contiene los catálogos explícitos de 110 acciones de `ClinicsController` y 27 acciones médicas/certificados/grants, y comprueba:

1. Que el inventario runtime de `ClinicsController` coincide exactamente con el catálogo revisado.
2. Que ninguna acción queda sin `[Authorize]` o `[AllowAnonymous]`.
3. Que `AllowAnonymous` coincide con las cuatro rutas públicas de clínica y la verificación pública de certificado.
4. Que toda mutación de los cuatro controladores clínicos usa una policy MFA; las diez excepciones justificadas aplican solo al catálogo de `ClinicsController`.
5. Que iniciar o aceptar un grant clínico deniega una sesión sin MFA y acepta una sesión elevada; escrituras representativas de medical/grant/certificate también retornan 403 sin step-up.

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

El esquema actual no modela sucursales físicas separadas. Una `Clinic` mantiene dirección/coordenadas y funciona como unidad de autorización. El inventario guarda `LocationName` como texto libre, sin FK, membresía ni reglas de acceso por sede. Por tanto:

- Los casos entre clínicas distintas sí son comprobables y están cubiertos en las familias anteriores.
- No se puede afirmar aislamiento por sucursal dentro de una clínica ni crear una matriz real “por sede” con el esquema actual.
- Antes de vender multi-sede se requiere diseñar `ClinicSite` (identidad, estado, membresías/roles, sede por defecto), asociar agenda, consultas, inventario, ventas/cierres, CRM y auditoría con FK, y decidir si los grants médicos son a la clínica o a una sede concreta.
- Criterio de cierre: cada endpoint que lee/escribe recurso con `SiteId` debe recibir sede explícita o resolverla desde sesión/membresía; probar recurso de sede A contra actor limitado a sede B en cada módulo y negar por defecto.

No se introduce ahora una entidad de sede a medias: los datos clínicos, ventas, inventario y permisos requieren una migración coordinada y un contrato de producto explícito.

## Evidencia de migraciones

Comprobación ejecutada con `ASPNETCORE_ENVIRONMENT=Development`:

| Target                             | Resultado                                                                                                                                                    | Acción tomada                                                                                               |
| ---------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| `PawTrackDev`                      | Última aplicada: `20260923190817_AddProductEventTenantAttribution`; 15 pendientes.                                                                           | Solo lectura con `dotnet ef migrations list`; no se aplicó ninguna.                                         |
| `AddClinicOperationalTaskMetadata` | `Pending`. Su `Down` elimina tareas con `PetId`/`OwnerUserId` nulos.                                                                                         | Requiere backup, export de esas tareas y aprobación de ventana antes de despliegue/reversión.               |
| `AddTrustedSessionLifecycle`       | Registrada y pendiente junto con el resto del lote. Backfill de `SessionId` usa `NEWID()` por refresh token antiguo; crea `TrustedDevices` con FK a `Users`. | No aplicada.                                                                                                |
| Azure SQL compartido               | No localizado: cero recursos SQL en la suscripción Azure seleccionada; MSSQL extension no tiene servidores/conexiones abiertas.                              | Estado de `__EFMigrationsHistory` **desconocido** hasta recibir una conexión/target compartido verificable. |

**No ejecutar `database update` contra producción/compartida desde esta tarea.** Antes de aplicar: confirmar servidor/base/ambiente con el dueño, backup probado, revisar `Down` destructivo de `AddClinicOperationalTaskMetadata`, aplicar en staging primero y validar counts + smoke tests.

## Control de pendientes

| ID     | Pendiente                                                | Responsable sugerido    | Criterio de salida                                                                                                                                 | Estado                                                      |
| ------ | -------------------------------------------------------- | ----------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| SEC-01 | BOLA dinámico de todos los recursos/IDs de 137 acciones. | Backend/Security        | Tabla endpoint × actor × recurso propio/ajeno × status; prueba HTTP negativa y positiva por fila de datos.                                         | En progreso: familias críticas; no afirmar cobertura total. |
| SEC-02 | Modelo y pruebas de sede física.                         | Arquitectura + Clínicas | Modelo aprobado, FK `SiteId`, backfill por sede principal y matriz inter-sede para agenda, expediente, inventario, caja, CRM y exports.            | Bloqueado por falta de entidad/contrato.                    |
| SEC-03 | Aplicar migraciones clínicas en staging/compartida.      | Release/DBA             | Target identificado, backup validado, migraciones ordenadas, `__EFMigrationsHistory` y smoke tests aprobados.                                      | No ejecutado; Azure target no disponible.                   |
| SEC-04 | MFA estructural para acciones nuevas.                    | Backend                 | Mantener verdes las pruebas de catálogo 110+27 y de toda mutación MFA; excepción nueva requiere owner/razón/test.                                  | Implementado en CI local.                                   |
| SEC-05 | Trusted sessions y dispositivos.                         | Auth/Security           | Listar/revocar sesión propia, MFA para trust/revoke, prueba rotada, revocación de JWT hermano, logout, refresh sin `mfa` y no bypass privilegiado. | Implementado; cobertura de integración 6 casos.             |
| SEC-06 | Revisión de excepciones MFA y sus contratos de producto. | Product/Security        | Aprobar cada motivo de `MfaExemptWriteReasons`; excepción nueva requiere PR/test y propietario.                                                    | Revisión continua.                                          |

## Comandos de verificación

Desde `frontend/`:

```powershell
dotnet test ..\backend\tests\PawTrack.IntegrationTests\PawTrack.IntegrationTests.csproj --filter "FullyQualifiedName~ClinicEndpointSecurityMatrixTests|FullyQualifiedName~TrustedSessionEndpointsTests" --verbosity minimal
dotnet test ..\backend\tests\PawTrack.IntegrationTests\PawTrack.IntegrationTests.csproj --filter "FullyQualifiedName~Clinics" --verbosity minimal
npx vitest run tests/features/auth/LoginPage.test.tsx tests/features/auth/ProfilePage.test.tsx
npm run typecheck -- --pretty false
```

La primera orden valida catálogo/MFA y ciclo auth; la segunda es el slice de integración clínica. La matriz no sustituye pruebas BOLA específicas para las filas todavía abiertas en SEC-01/SEC-02.
