# Plan enterprise: clínicas unisede y multisede

> Estado: dirección técnica inicial aprobada; Fase 1 parcialmente implementada en local.  
> Fecha: 2026-09-26.  
> Alcance: modelar una organización clínica con una o varias sedes, mantener aislamiento clínico/financiero por sede y permitir que un solo login administre sedes autorizadas.  
> Deploy: ninguno. Todas las pruebas y migraciones aplicadas hasta ahora son locales. No aplicar a staging/producción/DB compartida desde este plan.

## Decisión de arquitectura

Separar el tenant organizacional del establecimiento físico:

- `ClinicOrganization`: grupo legal/operativo que administra sus sedes, membresías, suscripción y políticas compartidas.
- `Clinic` existente: representa una sede física durante la transición; ya contiene licencia SENASA, dirección, coordenadas, contacto, horario y emergencia. No crear una segunda entidad `ClinicSite` paralela en la primera fase.
- `ClinicOrganizationMembership`: usuario, organización, rol organizacional, estado y auditoría de concesión/revocación.
- `ClinicOrganizationSite`: vínculo único entre `ClinicOrganization` y `Clinic.Id`; guarda sede primaria y fecha de incorporación sin reescribir los IDs existentes.
- La autorización operativa siempre resuelve un `ClinicId` de sede y valida que el usuario tenga membresía organizacional y alcance a esa sede. Tener membresía org no debe implicar acceso clínico indiscriminado a toda sede.

Unisede y multisede usan exactamente el mismo modelo:

- Unisede: una organización con una `Clinic` principal.
- Multisede: una organización con N `Clinic`.

### Compatibilidad de identidad

`Clinic.Id` se conserva como ID estable de la sede. Esto evita reescribir IDs referenciados por agenda, consultas, grants, certificados, inventario, ventas, caja, CRM, auditoría y exportaciones. `Clinic.UserId` permanece durante la expansión como titular/creador heredado; deja de ser la fuente final de autorización cuando las membresías organizacionales y de sede estén desplegadas.

La licencia SENASA permanece inicialmente en `Clinic` porque el código la valida como única por establecimiento. Confirmar con Legal/SENASA si la licencia pertenece a cada local físico o a una razón social antes de moverla. Billing/tax identity debe quedar en la organización salvo requisito fiscal que obligue al emisor por sede.

## Invariantes no negociables

1. Cada sede tiene exactamente una organización padre.
2. Cada organización activa tiene al menos una sede principal.
3. Toda cita, expediente, grant, inventario, venta/cobro, cierre, tarea y export conserva un `ClinicId` de sede; no se agrega un fallback que interprete `null` como todas las sedes.
4. Cada request selecciona una sede explícita o un contexto activo resuelto desde el servidor; jamás confiar en un `OrganizationId` enviado por el cliente sin validar la membresía.
5. Acceso cross-site requiere permiso explícito; reportes agregados consultan sólo el conjunto de sedes autorizado.
6. Revocar membresía/scope elimina acceso con el mismo access token o al expirar su corto TTL; MFA step-up se mantiene para mutaciones sensibles.
7. Los IDs existentes de `Clinic` no cambian durante backfill.
8. Todo query de lista se filtra por organización/sede en SQL, proyecta sólo columnas necesarias y tiene límite/paginación.

## Fases y entregables

### Fase 0 - Decisiones de producto/legal

- Confirmar titular legal y regla de una licencia SENASA por sede o por organización.
- Definir qué configuración se hereda de organización (nombre comercial, billing, branding, políticas) y cuál es propia de sede (licencia, dirección, coordenadas, horarios, teléfonos, stock, agenda, cierres).
- Definir roles org (Owner, Admin, FinanceManager, ReadOnly) y permisos site (Veterinarian, Receptionist, Assistant, Cashier, SiteManager).
- Definir invitación, incorporación, suspensión y baja de sede; definir qué pasa con citas, expedientes, stock y reportes al cerrar una sede.
- Confirmar si colaboradores son miembros de organización, de sede o ambos y si una persona puede trabajar en varias organizaciones.

**Gate:** Product + Legal + Seguridad firman el modelo de ownership y permisos. La implementación local inicia con supuestos reversibles: licencia y datos clínicos permanecen por sede.

### Fase 1 - Fundamento de dominio y registro unisede (en curso)

- Agregar `ClinicOrganization`, `ClinicOrganizationMembership` y `ClinicOrganizationSite`.
- Usar `ClinicOrganizationSite` como puente de migración: una fila por `Clinic.Id`, con FK, sin modificar la tabla ni PK `Clinics`.
- El alta actual de clínica crea una organización, su sede principal y membership Owner atómicamente.
- Validar que el agregado admite más de una sede y protege al único Owner activo.
- Mantener por ahora el índice único `Clinic.UserId`; es una restricción legacy de identidad, no la autorización final. No habilitar una misma cuenta en varias sedes hasta la fase 2.
- Migración expand/backfill: crear una organización por clínica existente, vincular el `Clinic.Id` como sede primaria y crear membership Owner para el `UserId` heredado.
- No activar todavía el alta de sedes secundarias por UI/API hasta que el contexto activo y las comprobaciones de autorización estén listas.

**Estado/evidencia actual:** dominio de organización, roles/membresías, enlaces a sitios, alta unisede que crea Owner+sede primaria, prueba EF de índices y backfill `AddClinicOrganizations` están implementados localmente. `Clinic.Id` se preserva; `Clinic.UserId` continúa único. `PawTrackDev` quedó migrada: 6 clínicas, 6 organizaciones, 6 sedes, 6 memberships Owner y 0 clínicas sin vínculo. No hay endpoint/UI para crear o cambiar a una segunda sede.

**Gate pendiente:** test HTTP de registro unisede y tests de dominio/metadata pasan localmente; falta probar el backfill sobre snapshot/staging, verificar atomicidad/duplicidad en el ambiente destino y aprobar despliegue. Ninguna migración se aplicó a base compartida.

### Fase 2 - Membresías, selección de sede y autorización backend

- Introducir queries `GetMyClinicOrganizations` y `GetOrganizationSites`; respuestas mínimas y acotadas.
- Crear `ClinicSiteMembership` o scope explícito en membership org para enumerar `AllowedClinicIds`; decidir con el gate de fase 0, no usar un boolean ambiguo `AllSites` sin auditoría.
- Cambiar el contexto `me` a una sede seleccionada validada por backend y resolver el caso de usuarios con cero/una/muchas sedes.
- Reemplazar usos de `GetByUserIdAsync` que asumen una sola clínica. Eliminar el índice único de `Clinic.UserId` sólo después de que todos los callers estén migrados.
- Auditar todos los endpoints clínicos versionados y rutas fuera de `ClinicsController` (Medical, Certificates, PetClinicAccess, API Partner, exports, jobs y webhooks).
- Crear política central `CanAccessClinicSite(userId, clinicId, permission)` con cache versionado/invalidation al revocar; usar MediatR/ports, no lectura directa de otra área.
- Añadir pruebas BOLA por endpoint × rol × recurso propio/ajeno × sede asignada/no asignada.

**Gate:** la matriz HTTP negativa/positiva está verde por cada route family, y no existe query clínica sin filtro de sede.

### Fase 3 - Datos, índices y concurrencia

- Añadir `ClinicId` site explícito donde hoy sólo existe `OrganizationId` o texto libre.
- Migrar `ClinicInventoryLot.LocationName` a `ClinicSiteId` si identifica ubicación física; separar ubicaciones internas de almacén de sedes.
- Preservar `RowVersion`/control de concurrencia en movimientos de stock y cierres de caja.
- Índices compuestos comienzan con scope de sede, por ejemplo `(ClinicId, BusinessDate, Status)` y `(ClinicId, AssignedRole, Status, DueDate)`.
- Incluir `OrganizationId` denormalizado sólo si hay justificación de rendimiento, con constraint/proceso de consistencia; no sustituir la FK site.
- Exports, auditoría y background jobs reciben scope de organización/sedes autorizadas y límites/paginación.

**Gate:** scripts de backfill con conteos before/after, checks de FK, índices medidos y no regresión de stock/ventas.

### Fase 4 - UI multisede

- Selector accesible de organización/sede, persistido como preferencia no confiable y validado en cada request.
- En unisede ocultar selector; conservar el mismo contrato y permisos que multisede.
- Workspace staff, agenda, métricas, inventario, caja, CRM y membresías filtran por sede seleccionada.
- Dashboard org-level agrega sólo sedes asignadas; distinguir totales agregados de detalle clínico.
- Mostrar claramente nombre/licencia/sede activa en acciones sensibles; exigir confirmación al cambiar sede antes de cobrar, emitir certificado o abrir expediente.

**Gate:** pruebas de UI de cambio de sede y tests contractuales de selección persistida inválida/revocada.

### Fase 5 - Rollout y operación

- Desplegar migración primero en ambiente efímero y staging; verificar backup restaurable y tiempos/bloqueos.
- Ejecutar expansión/backfill, validar organizaciones/sedes/membresías y `ClinicId` de cada hijo; comparar conteos de antes/después.
- Desplegar dual-read/dual-write sólo si el rollout lo requiere; nunca dejar datos de producción sin tenant/sede resoluble.
- Habilitar multisede por feature flag/org piloto; monitorear denegaciones, query latency, deadlocks, errores de FK y telemetría de selector.
- Plan de rollback documenta que la migración inversa puede perder relaciones/organizaciones creadas; preferir forward-fix si ya hay sedes secundarias.

**Gate:** aprobación DBA/Seguridad/Producto, smoke y restauración aprobados, runbook de incidente y propietario operativo.

## Primera implementación local

Fase 1 está parcialmente implementada: existen `ClinicOrganization`, `ClinicOrganizationMembership` y `ClinicOrganizationSite`; el registro HTTP crea organización, sede primaria que conserva `Clinic.Id` y Owner en la misma unidad de trabajo. `ClinicOrganizationModelTests` (3) y `ClinicOrganizationRegistrationEndpointsTests` (1) pasan en artefactos aislados. `20260926153155_AddClinicOrganizations` fue aplicada únicamente a `PawTrackDev` con conexión explícita y backfill verificado (`ClinicsWithoutSiteLink = 0`); no se aplicó a `PawTrackLocal`, Azure ni ninguna base compartida. El alta de sedes secundarias y el uso de organización desde `GetMyClinicQuery` siguen deshabilitados hasta Fase 2, porque `Clinic.UserId` todavía es único y `/me` asume un sitio.

## Estado y riesgos

- La clínica actual tiene `Clinic.UserId` único y muchos recursos referencian `Clinic.Id`; cambiar el significado sin mantener ese ID rompería ownership existente.
- `GetMyClinicQuery` y `IClinicRepository.GetByUserIdAsync` asumen exactamente una clínica por usuario. El índice no se debe relajar antes de implementar selección de sede.
- Los memberships staff/finance actuales están scoped a `ClinicId`; la transición debe decidir si se preservan como site access y cómo se mapean a roles org.
- La matriz existente de 137 acciones verifica catálogo, auth y MFA; BOLA dinámico aún es por familias críticas, no por cada ID de cada endpoint.
- No ha habido deploy. Migraciones se generan y prueban localmente; no ejecutar `database update` en producción/DB compartida.

## Verificación local prevista

```powershell
# Backend domain/handlers e integración de registro
 dotnet test .\backend\tests\PawTrack.UnitTests\PawTrack.UnitTests.csproj --filter FullyQualifiedName~ClinicOrganization --verbosity minimal
 dotnet test .\backend\tests\PawTrack.IntegrationTests\PawTrack.IntegrationTests.csproj --filter FullyQualifiedName~RegisterClinic --verbosity minimal

# Modelo EF/migraciones, sin aplicar
 dotnet ef migrations has-pending-model-changes --project .\backend\src\PawTrack.Infrastructure --startup-project .\backend\src\PawTrack.API
 dotnet ef migrations list --project .\backend\src\PawTrack.Infrastructure --startup-project .\backend\src\PawTrack.API
```

El conjunto final incluirá además la matriz clínica/security, los tests por rol/sede, typecheck/UI y smoke local. La salida compartida permanece manual y gated hasta que exista un target confirmado.
