# Sprint 3 — Identidad Sanitaria Extendida de Mascota

> Alcance: evolucionar la entidad `Pet` desde un perfil de recuperación hacia una
> identidad sanitaria SENASA-ready, con datos progresivos, verificación clínica de
> microchip, auditoría de cambios críticos, UI para dueño/clínica/admin y controles
> de privacidad. No incluye integración oficial con SENASA.
> Fecha: 2026-09-06.

---

## 1. Objetivo del sprint

Crear una capa enterprise de identidad sanitaria para mascotas que permita respaldar
pasaportes, certificados, reportes institucionales y futuros flujos de bienestar
animal sin romper el registro simple actual.

Resultado esperado:

- la mascota conserva su flujo simple de alta y edición;
- el dueño puede completar datos sanitarios progresivamente;
- una clínica autorizada puede verificar microchip y datos sanitarios clave;
- los datos críticos quedan versionados/auditados;
- el pasaporte SENASA-ready puede usar snapshots sanitarios más completos;
- la UI muestra estado de identidad: incompleta, declarada, verificada, requiere revisión;
- la exposición pública sigue minimizada y no muestra domicilio ni datos sensibles.

---

## 2. Fuera de alcance

- [ ] Registro oficial ante SENASA.
- [ ] Validación contra bases oficiales externas.
- [ ] Integración API/carga directa a SENASA.
- [ ] Casos de bienestar animal.
- [ ] Reportes institucionales masivos.
- [ ] Geocodificación oficial de direcciones exactas.
- [ ] Verificación biométrica o genética del animal.
- [ ] Transferencia legal completa de titularidad de mascota entre dueños.

---

## 3. Estado base actual

El modelo actual `Pet` incluye:

- `OwnerId`;
- `Name`;
- `Species`;
- `Breed`;
- `BirthDate`;
- `PhotoUrl`;
- `Status`;
- `MicrochipId` declarado;
- `CreatedAt` / `UpdatedAt`.

Limitaciones actuales para SENASA-ready:

- no existe sexo del animal;
- no existe color ni señas particulares estructuradas;
- no existe estado de esterilización;
- no existe cantón de residencia aproximado;
- no se distingue microchip declarado por dueño vs verificado por clínica;
- no existe auditoría dedicada de cambios de identidad sanitaria;
- no existe historial de responsables/titularidad;
- el pasaporte usa datos limitados del perfil y del formulario.

### 3.1 Estado de implementacion actual

Implementado en este corte:

- enums `PetSex`, `SterilizedStatus` y `MicrochipVerificationStatus`;
- campos sanitarios opcionales en `Pet` con defaults seguros para datos existentes;
- declaración, verificación, conflicto, revocación y resolución de microchip;
- auditoría `PetSanitaryIdentityAuditLog` para cambios críticos;
- endpoints owner para ver/editar identidad sanitaria y consultar auditoría;
- endpoint clínica para ver identidad sanitaria con grant/scan y verificar microchip;
- endpoints admin para listar conflictos, resolverlos o revocar verificación;
- snapshots extendidos en pasaporte SENASA-ready y bloqueo si hay conflicto de microchip;
- panel frontend para dueño en expediente médico;
- acción frontend clínica para verificar microchip;
- pestaña admin de conflictos de microchip;
- migración `AddPetSanitaryIdentity`.

Pendiente post-sprint recomendado:

- pruebas frontend exhaustivas por cada estado visual;
- observabilidad custom en Application Insights;
- catálogo formal de cantones;
- enmascaramiento parcial de microchip en notificaciones/logs si producto lo exige.

---

## 4. Checklist ejecutivo

- [x] Definir modelo de identidad sanitaria extendida.
- [x] Agregar campos opcionales y progresivos a `Pet`.
- [x] Agregar estado de completitud/verificación sanitaria.
- [x] Agregar verificación de microchip por clínica autorizada.
- [x] Agregar auditoría append-only de cambios sanitarios críticos.
- [x] Agregar endpoints owner para editar datos sanitarios.
- [x] Agregar endpoints clinic para verificar microchip/datos.
- [x] Agregar endpoints admin para revisar auditoría y conflictos.
- [x] Actualizar pasaporte SENASA-ready para consumir snapshots extendidos.
- [x] Actualizar frontend dueño con panel de identidad sanitaria.
- [x] Actualizar frontend clínica con acción de verificación.
- [x] Actualizar frontend admin con visor de auditoría/conflictos.
- [x] Completar pruebas unitarias y build frontend/backend.
- [x] Actualizar manuales, privacidad, términos y runbook.

---

## 5. Tareas por capa

### 5.1 Producto, legal y operaciones

- [ ] Definir datos sanitarios mínimos recomendados:
  - sexo;
  - color principal;
  - señas particulares;
  - estado de esterilización;
  - fecha de esterilización;
  - cantón de residencia aproximado;
  - microchip;
  - estado de verificación de microchip.
- [ ] Definir qué datos son públicos, privados o institucionales.
- [ ] Definir si `ResidenceCanton` es libre, catálogo o geocodificado.
- [ ] Definir si `Sex` permite `Unknown`, `Male`, `Female` y `NotApplicable`.
- [ ] Definir si `SterilizedStatus` permite `Unknown`, `Yes`, `No`, `NotApplicable`.
- [ ] Definir política de corrección de microchip erróneo.
- [ ] Definir quién puede marcar microchip como verificado.
- [ ] Definir cómo se resuelven conflictos cuando una clínica detecta chip distinto.
- [ ] Definir lenguaje UI: "declarado", "verificado por clínica", "requiere revisión".
- [ ] Confirmar que no se mostrará domicilio exacto en perfiles públicos.

### 5.2 Dominio — `Pet`

- [ ] Crear enum `PetSex`:
  - `Unknown`;
  - `Male`;
  - `Female`;
  - `NotApplicable`.
- [ ] Crear enum `SterilizedStatus`:
  - `Unknown`;
  - `Yes`;
  - `No`;
  - `NotApplicable`.
- [ ] Crear enum `MicrochipVerificationStatus`:
  - `NotProvided`;
  - `Declared`;
  - `Verified`;
  - `Conflict`;
  - `Revoked`.
- [ ] Agregar campos a `Pet`:
  - `Sex`;
  - `Color`;
  - `DistinctiveMarks`;
  - `SterilizedStatus`;
  - `SterilizedAt`;
  - `ResidenceCanton`;
  - `MicrochipVerificationStatus`;
  - `MicrochipVerifiedAt`;
  - `MicrochipVerifiedByClinicId`;
  - `MicrochipVerificationNotes`;
  - `ResponsibleOwnerId` si se separa de `OwnerId`.
- [ ] Mantener compatibilidad: mascotas existentes deben mapear a `Unknown` / `NotProvided`.
- [ ] Agregar método `UpdateSanitaryIdentity(...)`.
- [ ] Agregar método `SetMicrochipDeclared(string? chipId)`.
- [ ] Agregar método `VerifyMicrochip(Guid clinicId, string chipId, string? notes)`.
- [ ] Agregar método `FlagMicrochipConflict(Guid clinicId, string observedChipId, string reason)`.
- [ ] Agregar método `ClearMicrochipVerification(Guid actorId, string reason)` si aplica.
- [ ] Reglas de dominio:
  - microchip ISO 11784 máximo 15 caracteres numéricos;
  - microchip verificado no puede cambiarse por dueño sin abrir conflicto;
  - solo clínica puede verificar;
  - esterilización `Yes` permite `SterilizedAt`;
  - esterilización `No/Unknown` limpia o ignora `SterilizedAt`;
  - `ResidenceCanton` no debe contener dirección exacta.
- [ ] Tests de dominio para cada transición.

### 5.3 Dominio — auditoría sanitaria

- [ ] Crear `PetSanitaryIdentityAuditLog`.
- [ ] Campos mínimos:
  - `Id`;
  - `PetId`;
  - `ActorUserId`;
  - `ActorClinicId` opcional;
  - `Action`;
  - `FieldName`;
  - `PreviousValue`;
  - `NewValue`;
  - `Reason`;
  - `CreatedAt`.
- [ ] Acciones sugeridas:
  - `SanitaryIdentityUpdated`;
  - `MicrochipDeclared`;
  - `MicrochipVerified`;
  - `MicrochipConflictFlagged`;
  - `MicrochipVerificationRevoked`;
  - `SterilizationUpdated`;
  - `ResidenceCantonUpdated`.
- [ ] No auditar datos innecesarios ni direcciones exactas.
- [ ] Indices por `PetId + CreatedAt`, `ActorUserId`, `ActorClinicId`, `Action`.
- [ ] Tests de creación y minimización de auditoría.

### 5.4 Application — DTOs y contratos

- [ ] Extender `PetDto` con datos sanitarios.
- [ ] Crear `PetSanitaryIdentityDto`.
- [ ] Crear `UpdatePetSanitaryIdentityCommand`.
- [ ] Crear `VerifyPetMicrochipCommand`.
- [ ] Crear `FlagPetMicrochipConflictCommand`.
- [ ] Crear `GetPetSanitaryIdentityQuery`.
- [ ] Crear `GetPetSanitaryAuditLogQuery`.
- [ ] Crear `GetPetsWithMicrochipConflictsQuery` para admin.
- [ ] Crear `PetSanitaryCompletenessDto`.
- [ ] Mantener respuestas públicas minimizadas: perfil público no debe exponer datos sanitarios privados por defecto.

### 5.5 Application — validadores

- [ ] Validar `Color` máximo 80 caracteres.
- [ ] Validar `DistinctiveMarks` máximo 300 caracteres.
- [ ] Validar `ResidenceCanton` contra catálogo o longitud máxima 80.
- [ ] Validar microchip con regex conservador `^[0-9]{1,15}$`.
- [ ] Validar `SterilizedAt <= today`.
- [ ] Validar `SterilizedAt` solo cuando `SterilizedStatus == Yes`.
- [ ] Validar notas de verificación/conflicto máximo 300 caracteres.
- [ ] Validar que clínica que verifica esté activa, verificada y autorizada.
- [ ] Validar ownership/family para edición de dueño.
- [ ] Validar rol `Clinic` para verificación clínica.

### 5.6 Application — owner workflows

- [ ] `UpdatePetSanitaryIdentityCommandHandler`.
  - Permite al dueño o familia activa actualizar datos no verificados.
  - Audita cambios campo por campo.
  - No permite sobrescribir microchip verificado sin conflicto/revisión.
- [ ] `DeclarePetMicrochipCommandHandler`.
  - Dueño declara microchip.
  - Estado pasa a `Declared`.
  - Audita declaración.
- [ ] `GetPetSanitaryIdentityQueryHandler`.
  - Dueño/familia ve identidad completa.
  - Clínica con grant activo ve identidad necesaria para atención.
- [ ] Tests unitarios de permisos y cambios.

### 5.7 Application — clinic workflows

- [ ] `VerifyPetMicrochipCommandHandler`.
  - Requiere rol clínica.
  - Resuelve `ClinicId` desde usuario autenticado.
  - Requiere clínica activa y verificada.
  - Requiere scan reciente o grant médico activo.
  - Si chip coincide, marca `Verified`.
  - Si chip no coincide, marca `Conflict` con razón.
  - Audita acción.
- [ ] `UpdateClinicalSanitaryNotesCommandHandler` si se permite nota clínica no pública.
- [ ] `GetClinicPetSanitaryIdentityQueryHandler`.
  - Clínica ve datos necesarios, sin datos privados del dueño.
- [ ] Tests unitarios de BOLA y conflicto.

### 5.8 Application — admin workflows

- [ ] `GetMicrochipConflictsForAdminQuery` paginado.
- [ ] `ResolveMicrochipConflictCommand`.
  - Admin confirma chip nuevo, conserva chip anterior en auditoría.
  - Admin descarta conflicto con motivo.
- [ ] `RevokeMicrochipVerificationCommand`.
  - Requiere motivo.
  - Audita revocación.
- [ ] `GetPetSanitaryAuditLogForAdminQuery`.
- [ ] Tests unitarios de resolución de conflicto y revocación.

### 5.9 Infrastructure — EF Core

- [ ] Actualizar `PetConfiguration`.
- [ ] Agregar columnas con defaults seguros:
  - `Sex` default `Unknown`;
  - `SterilizedStatus` default `Unknown`;
  - `MicrochipVerificationStatus` default `NotProvided`.
- [ ] Agregar columnas nullable para campos opcionales.
- [ ] Crear `PetSanitaryIdentityAuditLogConfiguration`.
- [ ] Indices:
  - `Pets.MicrochipId` existente debe mantenerse único filtrado;
  - `Pets.MicrochipVerificationStatus`;
  - `Pets.ResidenceCanton`;
  - `PetSanitaryIdentityAuditLogs.PetId + CreatedAt`;
  - `PetSanitaryIdentityAuditLogs.ActorClinicId`;
  - `PetSanitaryIdentityAuditLogs.Action`.
- [ ] Generar migración `AddPetSanitaryIdentity`.
- [ ] Revisar migración para backfill seguro.

### 5.10 Infrastructure — repositorios

- [ ] Extender `IPetRepository` si se requiere query por estado de microchip.
- [ ] Crear `IPetSanitaryIdentityAuditRepository`.
- [ ] Implementar repositorio con `AsNoTracking()` para lecturas.
- [ ] Agregar query paginada para conflictos de microchip.
- [ ] Evitar joins en memoria para listados admin.
- [ ] Tests de repositorio si existe fixture relacional disponible.

### 5.11 API — endpoints owner

- [ ] `GET /api/pets/{petId}/sanitary-identity`.
- [ ] `PUT /api/pets/{petId}/sanitary-identity`.
- [ ] `PUT /api/pets/{petId}/microchip` o reutilizar update actual con reglas nuevas.
- [ ] `GET /api/pets/{petId}/sanitary-audit`.
- [ ] Requiere auth.
- [ ] Verifica ownership/family.
- [ ] `RequestSizeLimit` explícito.
- [ ] `EnableRateLimiting` explícito.
- [ ] `ProblemDetails` para 400/403/404/422.

### 5.12 API — endpoints clinic

- [ ] `GET /api/clinics/patients/{petId}/sanitary-identity`.
- [ ] `POST /api/clinics/patients/{petId}/microchip/verify`.
- [ ] `POST /api/clinics/patients/{petId}/microchip/conflict`.
- [ ] Requiere rol `Clinic`.
- [ ] Resuelve `clinicId` desde usuario autenticado.
- [ ] Requiere grant o scan reciente.
- [ ] Rate limit específico para verificación de microchip.
- [ ] Tests de autorización.

### 5.13 API — endpoints admin

- [ ] `GET /api/admin/pets/microchip-conflicts` paginado.
- [ ] `POST /api/admin/pets/{petId}/microchip-conflicts/resolve`.
- [ ] `POST /api/admin/pets/{petId}/microchip-verification/revoke`.
- [ ] `GET /api/admin/pets/{petId}/sanitary-audit`.
- [ ] Requiere rol `Admin`.
- [ ] Audita resoluciones.
- [ ] Tests de autorización y paginación.

### 5.14 Integración con pasaporte SENASA-ready

- [ ] Actualizar `IssueVaccinePassportCommandHandler` para snapshots extendidos:
  - sexo;
  - color desde `Pet.Color` si no viene en request;
  - señas particulares;
  - microchip y estado verificado;
  - esterilización si aplica.
- [ ] Bloquear emisión si microchip está en estado `Conflict`.
- [ ] Mostrar advertencia si microchip no está verificado.
- [ ] Incluir microchip verificado en PDF.
- [ ] Tests de regresión de pasaporte.

### 5.15 Frontend — dueño

- [ ] Crear `PetSanitaryIdentityPanel`.
- [ ] Mostrar estado de completitud sanitaria.
- [ ] Formulario progresivo para sexo, color, señas, esterilización, cantón.
- [ ] Campo de microchip con estado visual:
  - no registrado;
  - declarado;
  - verificado;
  - conflicto;
  - revocado.
- [ ] Bloquear edición directa de microchip verificado.
- [ ] Mostrar explicación de privacidad: cantón aproximado, no dirección exacta.
- [ ] Mostrar historial de cambios críticos si el plan/permiso aplica.
- [ ] Tests frontend de render y edición.

### 5.16 Frontend — clínica

- [ ] En `ClinicExpedienteTab`, mostrar identidad sanitaria del paciente.
- [ ] Agregar acción "Verificar microchip".
- [ ] Agregar acción "Reportar conflicto de microchip".
- [ ] Mostrar bloqueos si no hay grant o scan reciente.
- [ ] Mostrar estado de microchip en resultado de scan.
- [ ] Tests frontend de verificación y conflicto.

### 5.17 Frontend — admin

- [ ] Agregar tab o sección de conflictos de microchip.
- [ ] Lista paginada de conflictos.
- [ ] Ver detalle de mascota, dueño, clínica reportante y auditoría.
- [ ] Acción resolver confirmando chip.
- [ ] Acción descartar conflicto con motivo.
- [ ] Acción revocar verificación con motivo.
- [ ] Tests frontend admin.

### 5.18 Seguridad y privacidad

- [ ] No exponer datos sanitarios completos en perfil público.
- [ ] No exponer cantón de residencia si se considera sensible en casos específicos.
- [ ] No permitir edición de datos verificados sin auditoría.
- [ ] BOLA owner/family en endpoints de dueño.
- [ ] BOLA clinic grant/scan en endpoints de clínica.
- [ ] Admin-only en resolución de conflictos.
- [ ] Rate limit en verificación de microchip.
- [ ] Logs sin microchip completo si se considera sensible; usar parcial/enmascarado en mensajes.
- [ ] Auditoría no debe incluir dirección exacta ni notas clínicas sensibles.

### 5.19 Observabilidad

- [ ] Custom event `PetSanitaryIdentityUpdated`.
- [ ] Custom event `MicrochipDeclared`.
- [ ] Custom event `MicrochipVerified`.
- [ ] Custom event `MicrochipConflictFlagged`.
- [ ] Custom metric: porcentaje mascotas con identidad sanitaria completa.
- [ ] Custom metric: conflictos de microchip pendientes.
- [ ] Alerta si conflictos pendientes superan umbral.

### 5.20 Documentación y operación

- [ ] Actualizar `docs/senasa.md` con Sprint 3 y estado.
- [ ] Actualizar manual de usuario.
- [ ] Actualizar manual de clínicas.
- [ ] Actualizar manual administrador.
- [ ] Actualizar runbook:
  - conflicto de microchip;
  - revocación de verificación;
  - corrección de datos sanitarios.
- [ ] Revisar política de privacidad si se agregan nuevos datos personales/sanitarios.
- [ ] Revisar términos por veracidad de datos de mascota/microchip.

---

## 6. Orden recomendado de ejecución

### Día 1 — TDD y modelo de dominio

- [ ] Tests rojos para `PetSex`, `SterilizedStatus`, `MicrochipVerificationStatus`.
- [ ] Tests rojos para declaración/verificación/conflicto de microchip.
- [ ] Tests rojos para actualización sanitaria por dueño.
- [ ] Implementar dominio mínimo y auditoría.

### Días 2-3 — Persistencia y application

- [ ] Configurar EF y migración.
- [ ] Crear repositorios de auditoría.
- [ ] Implementar comandos owner.
- [ ] Implementar comandos clinic.
- [ ] Implementar queries admin.

### Días 4-5 — API y seguridad

- [ ] Endpoints owner.
- [ ] Endpoints clinic.
- [ ] Endpoints admin.
- [ ] Rate limits y request limits.
- [ ] Tests de integración de BOLA.

### Días 6-8 — Frontend dueño y clínica

- [ ] Panel sanitario del dueño.
- [ ] Verificación clínica de microchip.
- [ ] Estados visuales y errores.
- [ ] Tests frontend críticos.

### Días 9-10 — Admin, pasaporte y hardening

- [ ] Admin de conflictos.
- [ ] Snapshots extendidos en pasaporte.
- [ ] Docs/manuales/runbook.
- [ ] Build/test final.

---

## 7. Checklist de verificación de avance

### 7.1 Avance funcional

- [ ] Dueño puede completar identidad sanitaria.
- [ ] Dueño puede declarar microchip.
- [ ] Dueño no puede sobrescribir microchip verificado sin flujo de conflicto/revisión.
- [ ] Clínica puede ver identidad sanitaria con grant/scan.
- [ ] Clínica puede verificar microchip coincidente.
- [ ] Clínica puede reportar conflicto de microchip.
- [ ] Admin puede ver conflictos pendientes.
- [ ] Admin puede resolver conflicto.
- [ ] Admin puede revocar verificación con motivo.
- [ ] Pasaporte usa snapshots sanitarios extendidos.

### 7.2 Avance técnico backend

- [ ] Dominio compila con nuevos enums y métodos.
- [ ] EF tiene defaults seguros para mascotas existentes.
- [ ] Migración revisada manualmente.
- [ ] Auditoría sanitaria se persiste.
- [ ] Queries admin son paginadas.
- [ ] `IssueVaccinePassportCommand` bloquea microchip en conflicto.
- [ ] Endpoints tienen ownership/role checks.
- [ ] Endpoints tienen rate limits.
- [ ] Unit tests de dominio pasan.
- [ ] Integration tests de API pasan.

### 7.3 Avance técnico frontend

- [ ] Panel dueño renderiza estados completos.
- [ ] Formulario dueño guarda datos sanitarios.
- [ ] Panel clínica verifica microchip.
- [ ] Panel clínica reporta conflicto.
- [ ] Admin lista y resuelve conflictos.
- [ ] UI no expone dirección exacta.
- [ ] Textos explican "declarado" vs "verificado".
- [ ] Build frontend pasa.
- [ ] Tests frontend críticos pasan.

### 7.4 Seguridad y privacidad

- [ ] Perfil público no muestra identidad sanitaria completa.
- [ ] BOLA owner/family probado.
- [ ] BOLA clínica probado.
- [ ] Admin-only conflictos probado.
- [ ] Microchip en logs queda ausente o enmascarado.
- [ ] Auditoría no contiene dirección exacta.
- [ ] Rate limit verificación microchip probado.

### 7.5 Documentación y operación

- [ ] Manual usuario actualizado.
- [ ] Manual clínicas actualizado.
- [ ] Manual administrador actualizado.
- [ ] Runbook actualizado.
- [ ] Política de privacidad revisada.
- [ ] Términos de uso revisados.

---

## 8. Definición de terminado

Sprint 3 está completo cuando:

- [ ] las mascotas tienen identidad sanitaria extendida opcional y compatible con datos existentes;
- [ ] microchip declarado y verificado son estados diferentes;
- [ ] clínicas autorizadas pueden verificar microchip;
- [ ] conflictos de microchip quedan auditados y resolubles por admin;
- [ ] el dueño puede completar datos sanitarios desde UI;
- [ ] la clínica puede ver/verificar identidad sanitaria necesaria desde UI;
- [ ] el admin puede revisar conflictos desde UI;
- [ ] el pasaporte SENASA-ready consume snapshots extendidos;
- [ ] no hay exposición pública indebida de datos sanitarios;
- [ ] unit, integration y frontend tests críticos pasan;
- [ ] build backend/frontend pasa;
- [ ] manuales y runbook reflejan la operación real.

---

## 9. Estimación del Sprint 3

| Bloque                                  | Esfuerzo |
| --------------------------------------- | -------- |
| Dominio + auditoría + EF                | 2-3 días |
| Application handlers + validators       | 3-4 días |
| API + seguridad + integración pasaporte | 2-3 días |
| Frontend dueño                          | 2-3 días |
| Frontend clínica                        | 2-3 días |
| Frontend admin                          | 2-3 días |
| Tests + docs + hardening                | 3-4 días |

Total estimado: **16-23 días-persona**.

Calendario probable:

- 1 dev full-stack senior: **3.5-5 semanas**.
- 2 devs senior: **2-3 semanas**.
- 2 devs + QA parcial: **1.5-2.5 semanas**.

---

## 10. Riesgos específicos del sprint

| Riesgo                                                      | Mitigación                                              |
| ----------------------------------------------------------- | ------------------------------------------------------- |
| Dueño cambia microchip verificado por error                 | Bloquear edición directa y abrir conflicto/revisión.    |
| Datos sanitarios se vuelven obligatorios y dañan conversión | Campos opcionales y UI progresiva.                      |
| Perfil público expone más datos de los necesarios           | DTO público separado y tests de minimización.           |
| Conflictos de microchip quedan sin resolver                 | Cola admin paginada y notificaciones/alertas.           |
| Pasaporte usa datos desactualizados                         | Snapshots al momento de emisión y auditoría de cambios. |
| Cantón se usa como dirección exacta                         | Validación/copy que prohíbe direcciones específicas.    |
