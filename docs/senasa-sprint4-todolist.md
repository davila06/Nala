# Sprint 4 — Casos de Bienestar Animal Enterprise

> Alcance: crear el modulo `AnimalWelfare` para formalizar reportes, triage,
> evidencia, asignacion, derivacion y cierre de casos de bienestar animal en
> PawTrack/NALA. El objetivo es SENASA-ready, no integracion oficial con SENASA.
> Fecha: 2026-09-06.

---

## 1. Objetivo del sprint

Convertir eventos dispersos de perdida, avistamientos, capturas municipales,
adopciones y reportes ciudadanos en **casos formales de bienestar animal** con
seguimiento, privacidad, evidencia, auditoria y derivacion institucional.

Resultado esperado:

- cualquier ciudadano puede reportar un posible caso de bienestar animal con datos minimos;
- el admin/NALA puede hacer triage y asignar el caso;
- municipalidades, aliados, refugios y clinicas pueden recibir casos segun rol;
- evidencia sensible se guarda en Blob privado;
- el sistema mantiene bitacora y auditoria append-only;
- los casos pueden vincularse con mascotas, perdidas, capturas municipales o adopciones;
- la informacion publica queda minimizada y no expone reportantes ni ubicaciones exactas innecesarias;
- el flujo queda listo para reportes institucionales futuros sin enviar nada oficialmente a SENASA.

---

## 2. Fuera de alcance

- [ ] Integracion oficial con SENASA.
- [ ] Envio automatico de denuncias a autoridades.
- [ ] Gestion legal/policial del caso fuera de PawTrack.
- [ ] Firma digital juridica de evidencias.
- [ ] Cadena de custodia legal completa equivalente a sistema judicial.
- [ ] Modulo de reportes institucionales masivos del Sprint 5.
- [ ] Dashboard nacional NALA completo del Sprint 5.
- [ ] Validacion biometrica/forense de evidencia.

---

## 3. Estado base actual

PawTrack ya tiene piezas que alimentan bienestar animal:

- `LostPetEvent`: reportes de perdida y reunificacion.
- `Sighting`: avistamientos anonimos y fotos.
- `CapturedAnimal`: capturas municipales.
- `AdoptablePet` / `AdoptionApplication`: adopciones/refugios.
- `AllyProfile`: aliados verificados por cobertura.
- `Clinic`: clinicas activas y verificadas para pasaportes.
- `Pet`: identidad sanitaria extendida y microchip verificado/conflicto.
- `Notification`: inbox y notificaciones in-app.
- Blob Storage privado ya se usa para documentos sensibles.

Estado verificado: el modulo `AnimalWelfare` y su primer flujo enterprise ya
existen en Domain/Application/Infrastructure. Las brechas restantes son de
producto, formulario publico dedicado, observabilidad avanzada, retencion
especifica y cobertura de pruebas/documentacion.

---

## 4. Checklist ejecutivo

- [x] Crear modulo `AnimalWelfare` en Domain/Application/Infrastructure.
- [x] Crear entidad `AnimalWelfareCase`.
- [x] Crear tipos `WelfareCaseType`, `WelfareCaseStatus`, `WelfareSeverity`.
- [x] Crear estado maquina de caso.
- [x] Crear entidad de evidencia privada.
- [x] Crear bitacora interna de caso.
- [x] Crear auditoria append-only.
- [x] Crear derivaciones/asignaciones a organizaciones.
- [x] Crear endpoint publico minimizado de reporte.
- [x] Crear cola admin/NALA de triage.
- [x] Crear endpoints para aliados/municipalidades/refugios/clinicas.
- [x] Integrar capturas municipales con casos.
- [ ] Integrar adopciones/refugios con casos.
- [ ] Integrar identidad sanitaria/microchip con casos.
- [x] Agregar retencion/purga base para evidencia y exports relacionados.
- [x] Completar UI admin y roles operativos base; formulario público dedicado queda pendiente.
- [x] Completar pruebas unitarias focalizadas e integración de autorización.
- [ ] Actualizar manuales, privacidad, terminos y runbook.

---

## 5. Tareas por capa

### 5.1 Producto, legal y operaciones

- [ ] Definir tipos de caso soportados en Sprint 4:
  - abandono;
  - posible maltrato;
  - negligencia o falta de atencion veterinaria;
  - animal herido;
  - animal en via publica en riesgo;
  - captura municipal con indicios de abandono;
  - acumulacion/tenencia inadecuada;
  - adopcion irregular o seguimiento post-adopcion;
  - solicitud de inspeccion o apoyo institucional.
- [ ] Definir severidades:
  - baja;
  - media;
  - alta;
  - critica.
- [ ] Definir SLA operativo por severidad.
- [ ] Definir criterios de derivacion.
- [ ] Definir informacion publica vs privada.
- [ ] Definir si reportante puede ser anonimo o autenticado.
- [ ] Definir proceso para reportes falsos o abusivos.
- [ ] Definir reglas de moderacion de imagenes sensibles.
- [ ] Definir retencion de evidencia y casos cerrados.
- [ ] Revisar lenguaje legal: PawTrack apoya coordinacion, no reemplaza autoridades.

### 5.2 Dominio — `AnimalWelfareCase`

- [ ] Crear entidad `AnimalWelfareCase`.
- [ ] Campos minimos:
  - `Id`;
  - `Type`;
  - `Status`;
  - `Severity`;
  - `PetId?`;
  - `LostPetEventId?`;
  - `SightingId?`;
  - `CapturedAnimalId?`;
  - `AdoptablePetId?`;
  - `Canton`;
  - `ApproxLat?`;
  - `ApproxLng?`;
  - `DescriptionSanitized`;
  - `ReporterUserId?`;
  - `ReporterIsAnonymous`;
  - `AssignedOrganizationUserId?`;
  - `AssignedRole?`;
  - `CreatedAt`;
  - `UpdatedAt`;
  - `ClosedAt?`.
- [ ] Crear enum `WelfareCaseType`.
- [ ] Crear enum `WelfareCaseStatus`:
  - `Received`;
  - `Triage`;
  - `Assigned`;
  - `InProgress`;
  - `Referred`;
  - `Resolved`;
  - `Dismissed`;
  - `ClosedNoAction`.
- [ ] Crear enum `WelfareSeverity`.
- [ ] Metodos de dominio:
  - `Create(...)`;
  - `StartTriage(...)`;
  - `SetSeverity(...)`;
  - `AssignTo(...)`;
  - `MarkInProgress(...)`;
  - `ReferTo(...)`;
  - `Resolve(...)`;
  - `Dismiss(...)`;
  - `CloseNoAction(...)`.
- [ ] Reglas de dominio:
  - no cerrar sin motivo/resolucion;
  - no asignar caso cerrado;
  - no derivar caso cerrado;
  - severidad critica requiere triage/admin antes de cierre;
  - descripcion se persiste sanitizada;
  - ubicacion exacta no se expone en DTO publico.
- [ ] Tests de estado maquina.

### 5.3 Dominio — evidencia

- [ ] Crear `AnimalWelfareEvidence`.
- [ ] Campos minimos:
  - `Id`;
  - `CaseId`;
  - `BlobUrl`;
  - `ContentType`;
  - `FileSizeBytes`;
  - `UploadedByUserId?`;
  - `UploadedAt`;
  - `EvidenceKind`;
  - `IsSensitive`;
  - `HashSha256` opcional.
- [ ] Crear enum `WelfareEvidenceKind`:
  - `Photo`;
  - `Video`;
  - `Document`;
  - `VeterinaryNote`;
  - `MunicipalReport`.
- [ ] Reglas:
  - evidencias sensibles siempre en Blob privado;
  - maximo de archivos por caso configurable;
  - no permitir SVG/ejecutables;
  - registrar tamano y content type.
- [ ] Tests de creacion y limites.

### 5.4 Dominio — bitacora y auditoria

- [ ] Crear `AnimalWelfareCaseNote`.
- [ ] Crear `AnimalWelfareReferral`.
- [ ] Crear `AnimalWelfareCaseAuditLog`.
- [ ] Acciones auditadas:
  - `CaseReported`;
  - `EvidenceUploaded`;
  - `TriageStarted`;
  - `SeverityChanged`;
  - `Assigned`;
  - `Referred`;
  - `StatusChanged`;
  - `Resolved`;
  - `Dismissed`;
  - `ClosedNoAction`;
  - `DocumentDownloaded`.
- [ ] Notas internas visibles segun rol.
- [ ] Auditoria append-only.
- [ ] No guardar PII innecesaria en `Details`.
- [ ] Tests de auditoria.

### 5.5 Application — DTOs y contratos

- [ ] Crear `AnimalWelfareCaseDto`.
- [ ] Crear `PublicAnimalWelfareCaseDto` minimizado.
- [ ] Crear `AnimalWelfareCaseDetailDto` para admin/roles asignados.
- [ ] Crear `AnimalWelfareEvidenceDto` sin URL publica directa.
- [ ] Crear `AnimalWelfareNoteDto`.
- [ ] Crear `AnimalWelfareReferralDto`.
- [ ] Crear `AnimalWelfareAuditDto`.
- [ ] Crear paginacion para listados.
- [ ] Definir campos redacted/publicos.

### 5.6 Application — comandos publicos

- [ ] `ReportAnimalWelfareCaseCommand`.
  - Permite anonimo o autenticado.
  - Sanitiza notas con PII scrubber.
  - Valida tipo, severidad inicial, canton, ubicacion aproximada.
  - Crea caso en `Received`.
  - Audita `CaseReported`.
  - Notifica admin/NALA.
- [ ] `UploadAnimalWelfareEvidenceCommand`.
  - Publico con token temporal o autenticado segun decision.
  - Blob privado.
  - Max 5 MB imagen/documento en MVP.
  - Rechaza SVG/ejecutables.
  - Audita `EvidenceUploaded`.
- [ ] Tests unitarios de sanitizacion y evidencia.

### 5.7 Application — comandos admin/NALA

- [ ] `StartWelfareCaseTriageCommand`.
- [ ] `SetWelfareCaseSeverityCommand`.
- [ ] `AssignWelfareCaseCommand`.
- [ ] `ReferWelfareCaseCommand`.
- [ ] `ResolveWelfareCaseCommand`.
- [ ] `DismissWelfareCaseCommand`.
- [ ] `CloseWelfareCaseNoActionCommand`.
- [ ] `AddWelfareCaseNoteCommand`.
- [ ] `DownloadWelfareEvidenceQuery`.
- [ ] Todos auditan cambios.
- [ ] Todos validan rol/permisos.
- [ ] Tests unitarios de estado maquina y permisos.

### 5.8 Application — queries

- [ ] `GetWelfareCaseQueueQuery` para admin/NALA.
- [ ] `GetAssignedWelfareCasesQuery` para aliados/municipalidades/refugios/clinicas.
- [ ] `GetWelfareCaseDetailQuery`.
- [ ] `GetPublicWelfareCaseStatusQuery` minimizado.
- [ ] `GetWelfareCaseAuditLogQuery`.
- [ ] `GetWelfareCasesByLinkedPetQuery`.
- [ ] `GetWelfareCasesByCapturedAnimalQuery`.
- [ ] Paginacion obligatoria.
- [ ] Filtros por estado, severidad, canton, tipo, fecha, asignado.
- [ ] `AsNoTracking()` en lecturas.

### 5.9 Application — integraciones internas

- [ ] Desde captura municipal: accion "convertir en caso de bienestar".
- [ ] Desde mascota perdida: crear caso si hay indicios de abandono/negligencia.
- [ ] Desde avistamiento: vincular evidencia si reporta animal herido/riesgo.
- [ ] Desde adopcion: crear caso de seguimiento post-adopcion si aplica.
- [ ] Desde microchip conflict: permitir crear caso si hay indicios de fraude/abandono.
- [ ] Usar MediatR/domain events o comandos explicitos sin romper boundaries.
- [ ] Tests de integracion entre modulos.

### 5.10 Infrastructure — EF Core

- [ ] Agregar `DbSet<AnimalWelfareCase>`.
- [ ] Agregar `DbSet<AnimalWelfareEvidence>`.
- [ ] Agregar `DbSet<AnimalWelfareCaseNote>`.
- [ ] Agregar `DbSet<AnimalWelfareReferral>`.
- [ ] Agregar `DbSet<AnimalWelfareCaseAuditLog>`.
- [ ] Configuraciones EF separadas.
- [ ] Indices:
  - `Status + CreatedAt`;
  - `Severity + Status`;
  - `Canton + Status`;
  - `AssignedOrganizationUserId + Status`;
  - `PetId`;
  - `CapturedAnimalId`;
  - `CaseId + CreatedAt` para notas/auditoria/evidencia.
- [ ] Configurar longitudes maximas.
- [ ] Evitar cascadas destructivas en evidencia/auditoria.
- [ ] Generar migracion `AddAnimalWelfareCases`.
- [ ] Revisar migracion y snapshot.

### 5.11 Infrastructure — repositorios

- [ ] Crear `IAnimalWelfareCaseRepository`.
- [ ] Crear `IAnimalWelfareEvidenceRepository`.
- [ ] Crear `IAnimalWelfareAuditRepository`.
- [ ] Implementar repositorios EF.
- [ ] Lecturas con `AsNoTracking()`.
- [ ] Listados paginados.
- [ ] No cargar evidencias binarias desde DB.
- [ ] No hacer joins en memoria para colas grandes.
- [ ] Tests de repositorio si hay fixture adecuado.

### 5.12 Infrastructure — Blob Storage

- [ ] Crear contenedor privado `welfare-evidence`.
- [ ] Estructura de blobs:
  - `cases/{caseId}/evidence/{evidenceId}.{ext}`.
- [ ] Content types permitidos:
  - JPEG;
  - PNG;
  - WebP;
  - PDF.
- [ ] Maximo 5 MB por archivo en MVP.
- [ ] Descargar evidencia solo por endpoint autenticado.
- [ ] Auditar descargas.
- [ ] Preparar hook futuro para malware scanning.

### 5.13 API — endpoints publicos

- [ ] `POST /api/public/welfare-cases`.
- [ ] `POST /api/public/welfare-cases/{id}/evidence`.
- [ ] `GET /api/public/welfare-cases/{publicCode}` minimizado.
- [ ] Rate limit especifico `welfare-report`.
- [ ] Request size limits explicitos.
- [ ] ProblemDetails para errores.
- [ ] No retornar datos internos ni asignaciones privadas.

### 5.14 API — endpoints admin/NALA

- [ ] `GET /api/admin/welfare-cases` paginado.
- [ ] `GET /api/admin/welfare-cases/{id}`.
- [ ] `POST /api/admin/welfare-cases/{id}/triage`.
- [ ] `PUT /api/admin/welfare-cases/{id}/severity`.
- [ ] `POST /api/admin/welfare-cases/{id}/assign`.
- [ ] `POST /api/admin/welfare-cases/{id}/refer`.
- [ ] `POST /api/admin/welfare-cases/{id}/resolve`.
- [ ] `POST /api/admin/welfare-cases/{id}/dismiss`.
- [ ] `POST /api/admin/welfare-cases/{id}/close-no-action`.
- [ ] `POST /api/admin/welfare-cases/{id}/notes`.
- [ ] `GET /api/admin/welfare-cases/{id}/audit`.
- [ ] `GET /api/admin/welfare-cases/{id}/evidence/{evidenceId}`.
- [ ] Todos requieren rol `Admin` o rol institucional definido.

### 5.15 API — endpoints por rol asignado

- [ ] `GET /api/municipalities/welfare-cases`.
- [ ] `GET /api/allies/welfare-cases`.
- [ ] `GET /api/clinics/welfare-cases`.
- [ ] `GET /api/shelters/welfare-cases` si aplica.
- [ ] `GET /api/*/welfare-cases/{id}` detalle permitido.
- [ ] `POST /api/*/welfare-cases/{id}/notes`.
- [ ] `POST /api/*/welfare-cases/{id}/status` limitado por rol.
- [ ] Validar asignacion antes de permitir acceso.

### 5.16 API — integracion municipal

- [ ] En `MunicipalController`, agregar accion convertir captura a caso.
- [ ] `POST /api/municipalities/captures/{id}/welfare-case`.
- [ ] Vincular `CapturedAnimalId`.
- [ ] Copiar datos sanitizados de captura.
- [ ] Preservar foto como evidencia si existe.
- [ ] Auditar origen municipal.
- [ ] Tests de permisos y vinculacion.

### 5.17 Frontend — reporte publico

- [ ] Crear pagina/formulario `ReportWelfareCasePage`.
- [ ] Campos:
  - tipo;
  - severidad percibida;
  - canton;
  - ubicacion aproximada;
  - descripcion;
  - foto/documento opcional;
  - anonimo/autenticado.
- [ ] Copy legal: no sustituye denuncia ante autoridad competente.
- [ ] Validaciones cliente.
- [ ] Estado de exito con codigo publico minimizado.
- [ ] Tests frontend de render/envio/error.

### 5.18 Frontend — admin/NALA

- [ ] Crear tab `Bienestar` en `AdminPage` o dashboard NALA.
- [ ] Cola paginada con filtros.
- [ ] Tarjetas por severidad y SLA.
- [ ] Detalle de caso.
- [ ] Galeria de evidencia privada.
- [ ] Bitacora interna.
- [ ] Acciones triage/asignar/derivar/resolver/descartar/cerrar.
- [ ] Modal de motivo obligatorio.
- [ ] Estados loading/empty/error.
- [ ] Tests frontend criticos.

### 5.19 Frontend — municipalidades/aliados/refugios/clinicas

- [ ] Panel de casos asignados.
- [ ] Detalle limitado por rol.
- [ ] Agregar nota operativa.
- [ ] Cambiar estado permitido.
- [ ] Descargar evidencia si rol autorizado.
- [ ] Desde captura municipal, boton "Crear caso de bienestar".
- [ ] Tests frontend por rol principal.

### 5.20 Seguridad y privacidad

- [ ] Anonimato por defecto para reportante publico.
- [ ] PII scrubber en descripcion publica.
- [ ] Evidencia siempre en Blob privado.
- [ ] No exponer ubicacion exacta en vista publica.
- [ ] BOLA admin/rol asignado.
- [ ] Rate limiting de reportes publicos.
- [ ] Request size limits en evidencia.
- [ ] Auditoria de descargas de evidencia.
- [ ] Logs sin descripcion completa ni URLs privadas.
- [ ] Retencion de evidencia sensible.
- [ ] Moderacion de contenido sensible.

### 5.21 Observabilidad

- [ ] Custom event `WelfareCaseReported`.
- [ ] Custom event `WelfareCaseAssigned`.
- [ ] Custom event `WelfareCaseReferred`.
- [ ] Custom event `WelfareCaseResolved`.
- [ ] Custom metric: casos abiertos por severidad.
- [ ] Custom metric: tiempo promedio en triage.
- [ ] Custom metric: casos vencidos por SLA.
- [ ] Alerta si casos criticos sin triage > umbral.
- [ ] Alerta si falla upload de evidencia.

### 5.22 Retencion y jobs

- [ ] Definir `AnimalWelfareRetentionSettings`.
- [ ] Job diario con `IDistributedJobLock`.
- [ ] Purga o anonimiza casos cerrados segun politica.
- [ ] No purgar evidencia de casos abiertos.
- [ ] No purgar casos con hold legal/manual.
- [ ] Auditar purgas anonimizadas.
- [ ] Tests unitarios del job.

### 5.23 Documentacion y operacion

- [ ] Actualizar `docs/senasa.md` con Sprint 4 y estado.
- [ ] Actualizar manual de usuario con reporte de bienestar.
- [ ] Actualizar manual admin con triage y asignacion.
- [ ] Actualizar manual municipal con conversion captura -> caso.
- [ ] Actualizar manual aliados/refugios si reciben casos.
- [ ] Actualizar politica de privacidad con evidencia sensible.
- [ ] Actualizar terminos por reportes falsos/evidencia sensible.
- [ ] Actualizar runbook:
  - caso critico;
  - evidencia sensible;
  - reporte falso;
  - derivacion institucional;
  - descarga de evidencia;
  - retencion/purga.

---

## 6. Orden recomendado de ejecucion

### Dia 1 — TDD y dominio

- [ ] Tests rojos de estado maquina `AnimalWelfareCase`.
- [ ] Tests rojos de evidencia privada.
- [ ] Tests rojos de auditoria.
- [ ] Implementar dominio minimo.

### Dias 2-3 — Persistencia y application

- [ ] Configuraciones EF y migracion.
- [ ] Repositorios paginados.
- [ ] Comandos publicos.
- [ ] Comandos admin.
- [ ] Queries de cola/detalle/auditoria.

### Dias 4-5 — API y seguridad

- [ ] Endpoints publicos.
- [ ] Endpoints admin/NALA.
- [ ] Endpoints rol asignado.
- [ ] Rate limits y request limits.
- [ ] Integration tests de BOLA.

### Dias 6-8 — Frontend publico y admin

- [ ] Formulario publico.
- [ ] Cola admin/NALA.
- [ ] Detalle, evidencia y bitacora.
- [ ] Acciones de triage/asignacion/cierre.
- [ ] Tests frontend.

### Dias 9-10 — Integraciones internas

- [ ] Conversion captura municipal -> caso.
- [ ] Vinculos perdida/avistamiento/adopcion.
- [ ] Panel de casos asignados.
- [ ] Regression tests.

### Dias 11-12 — Hardening

- [ ] Retencion/job.
- [ ] Observabilidad.
- [ ] Docs/manuales/runbook.
- [ ] Build/test final.
- [ ] Revision privacidad/seguridad.

---

## 7. Checklist de verificacion de avance

### 7.1 Avance funcional

- [ ] Ciudadano puede reportar caso de bienestar.
- [ ] Reporte publico puede ser anonimo.
- [ ] Evidencia se sube a Blob privado.
- [ ] Admin ve cola de triage.
- [ ] Admin cambia severidad.
- [ ] Admin asigna caso a organizacion.
- [ ] Admin deriva caso con motivo.
- [ ] Organizacion asignada ve el caso.
- [ ] Organizacion asignada agrega nota.
- [ ] Admin resuelve/descarta/cierra con motivo.
- [ ] Captura municipal puede convertirse en caso.
- [ ] Verificacion publica minimizada no expone PII.

### 7.2 Avance tecnico backend

- [ ] Dominio compila con estado maquina.
- [ ] EF tiene entidades e indices.
- [ ] Migracion revisada.
- [ ] Repositorios usan `AsNoTracking` en lecturas.
- [ ] Listados son paginados.
- [ ] Evidencia no se almacena en DB.
- [ ] Auditoria append-only se persiste.
- [ ] Retencion/job implementado.
- [ ] Unit tests pasan.
- [ ] Integration tests pasan.

### 7.3 Avance tecnico frontend

- [ ] Formulario publico renderiza y valida.
- [ ] Upload evidencia muestra progreso/error.
- [ ] Cola admin filtra por estado/severidad.
- [ ] Detalle muestra evidencia/bitacora.
- [ ] Acciones exigen motivo.
- [ ] Panel rol asignado muestra solo casos permitidos.
- [ ] UI no muestra ubicacion exacta en publico.
- [ ] Build frontend pasa.
- [ ] Tests frontend criticos pasan.

### 7.4 Seguridad y privacidad

- [ ] PII scrubber aplicado.
- [ ] Reportante anonimo no queda expuesto.
- [ ] Evidencia privada.
- [ ] Descargas auditadas.
- [ ] BOLA admin/rol asignado probado.
- [ ] Rate limits probados.
- [ ] Request size limits probados.
- [ ] Logs sin evidencia/PII.
- [ ] Retencion definida.

### 7.5 Documentacion y operacion

- [ ] Manual usuario actualizado.
- [ ] Manual admin actualizado.
- [ ] Manual municipal actualizado.
- [ ] Manual aliados/refugios actualizado si aplica.
- [ ] Runbook actualizado.
- [ ] Politica privacidad revisada.
- [ ] Terminos revisados.

---

## 8. Definicion de terminado

Sprint 4 esta completo cuando:

- [ ] existe modulo `AnimalWelfare` con dominio, application, infrastructure y API;
- [ ] casos tienen estado maquina completo y probado;
- [ ] evidencia se almacena privada y se descarga solo con autorizacion;
- [ ] admin/NALA puede hacer triage, asignar, derivar, resolver y cerrar;
- [ ] organizaciones asignadas pueden ver y operar casos permitidos;
- [ ] capturas municipales pueden convertirse en casos;
- [ ] auditoria y bitacora quedan persistidas;
- [ ] vistas publicas minimizan datos y ubicacion;
- [ ] retencion/purga esta definida o implementada;
- [ ] unit, integration y frontend tests criticos pasan;
- [ ] build backend/frontend pasa;
- [ ] manuales, privacidad, terminos y runbook reflejan el flujo real.

---

## 9. Estimacion del Sprint 4

| Bloque                               | Esfuerzo |
| ------------------------------------ | -------- |
| Dominio + auditoria + EF             | 3-4 dias |
| Application handlers + queries       | 4-5 dias |
| API + seguridad + evidencia privada  | 3-4 dias |
| Frontend publico                     | 2-3 dias |
| Frontend admin/NALA                  | 4-5 dias |
| Frontend roles asignados + municipal | 3-4 dias |
| Retencion + observabilidad           | 2-3 dias |
| Tests + docs + hardening             | 4-5 dias |

Total estimado: **25-33 dias-persona**.

Calendario probable:

- 1 dev full-stack senior: **5-7 semanas**.
- 2 devs senior: **3-4.5 semanas**.
- 2 devs + QA parcial: **2.5-4 semanas**.

---

## 10. Riesgos especificos del sprint

| Riesgo                           | Mitigacion                                                            |
| -------------------------------- | --------------------------------------------------------------------- |
| Reportes falsos o maliciosos     | Rate limiting, auditoria, triage y capacidad de descartar con motivo. |
| Exposicion de evidencia sensible | Blob privado, descarga autenticada, redaccion publica y tests BOLA.   |
| Sobrecarga operativa admin       | Cola paginada, filtros, severidad y SLA.                              |
| Derivacion prematura a autoridad | Triage interno y lenguaje SENASA-ready sin envio oficial.             |
| Ubicaciones exactas expuestas    | Aproximacion publica y detalle solo por rol autorizado.               |
| Caso cerrado sin trazabilidad    | Motivo obligatorio y audit log append-only.                           |
| N+1 en colas grandes             | Queries paginadas/proyectadas y `AsNoTracking`.                       |
