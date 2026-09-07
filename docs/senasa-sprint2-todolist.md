# Sprint 2 — Verificacion Enterprise de Clinicas y Veterinarios

> Alcance: llevar la verificacion de clinicas y veterinarios desde el minimo funcional
> de Sprint 1 hasta un flujo enterprise SENASA-ready: documentado, auditable,
> revalidable, seguro, operable por admin y usable por clinicas Partner.
> Fecha: 2026-09-06.

---

## 1. Objetivo del sprint

Construir un sistema completo de verificacion documental para clinicas veterinarias
y veterinarios emisores, de modo que la emision de pasaportes veterinarios
SENASA-ready no dependa de confianza manual invisible ni de datos libres sin respaldo.

Resultado esperado:

- una clinica puede enviar documentos de verificacion desde su portal;
- un admin puede aprobar, rechazar, vencer o reabrir verificaciones;
- una clinica verificada puede administrar veterinarios autorizados;
- cada veterinario tiene licencia, estado, vigencia, evidencia y auditoria;
- los pasaportes solo se emiten si clinica y veterinario cumplen los requisitos;
- el sistema notifica vencimientos, rechazos y cambios criticos;
- la operacion queda cubierta por tests, manuales y runbook.

---

## 2. Fuera de alcance

- [ ] Validacion automatica contra una base oficial SENASA.
- [ ] Integracion API/email/carga directa hacia SENASA.
- [ ] Firma digital juridica avanzada o certificado criptografico de veterinario.
- [ ] Workflow de inspeccion fisica de clinicas.
- [ ] Modulo general de bienestar animal.
- [ ] Identidad sanitaria extendida de mascota, salvo ajustes necesarios para mostrar dependencias.
- [ ] Reportes institucionales masivos.

---

## 3. Estado base desde Sprint 1

Sprint 1 ya agrego la base minima:

- `ClinicVerification` con estados `Pending`, `Verified`, `Rejected`, `Expired`.
- `ClinicVeterinarian` con licencia, permiso de emision y revocacion.
- `IssueVaccinePassportCommand` bloquea emision si no hay clinica verificada o veterinario activo.
- `GET /api/clinics/me/certificate-issuers` expone verificacion y veterinarios activos.
- `POST /api/clinics/me/veterinarians` permite crear veterinario autorizado.
- `PUT /api/clinics/admin/{clinicId}/certificate-verification` permite verificacion admin minima.
- Auditoria de certificados existe en `CertificateAuditLogs`.

Sprint 2 debe convertir esa base en proceso enterprise completo.

### 3.1 Estado de implementacion actual

Implementado en este corte:

- dominio enterprise de `ClinicVerification` con documento privado, revalidacion, supersede, revision y expiracion;
- dominio enterprise de `ClinicVeterinarian` con estados `PendingReview`, `Authorized`, `Rejected`, `Suspended`, `Revoked`, `Expired`;
- auditoria dedicada `VerificationAuditLogs` para cambios y descargas documentales;
- uploads privados de documentos de clinica, documentos de veterinario y firma/sello;
- endpoints clinic owner para solicitud, carga, listado, revocacion y descarga documental;
- endpoints admin para listar, aprobar, rechazar, suspender y descargar documentos;
- bloqueo de emision de pasaportes si clinica/veterinario no estan vigentes y documentados;
- jobs diarios con `IDistributedJobLock` para expiracion y recordatorios de renovacion;
- notificaciones in-app `SystemMessage` para vencimientos y proximos vencimientos;
- panel de clinica para verificacion y veterinarios;
- pestana admin de verificacion;
- actualizacion de politica, terminos, manuales y runbook.

Pendiente post-sprint recomendado:

- eventos custom de Application Insights por cada transicion;
- antivirus/malware scanning externo sobre documentos privados;
- pruebas frontend exhaustivas para cada estado visual admin/clinica;
- descarga documental desde UI admin con boton dedicado si operaciones lo requiere.

---

## 4. Checklist ejecutivo

- [x] Definir estados y reglas finales de verificacion documental.
- [x] Agregar carga privada de documentos de clinica.
- [x] Agregar carga privada de documentos/firma de veterinario.
- [x] Separar alta de veterinario de autorizacion para emitir.
- [x] Agregar aprobacion/rechazo/revocacion admin de veterinarios.
- [x] Agregar expiracion y revalidacion de clinicas.
- [x] Agregar expiracion y revalidacion de veterinarios.
- [x] Auditar todo cambio de estado de clinica/veterinario.
- [x] Notificar aprobaciones, rechazos y proximos vencimientos.
- [x] Endurecer endpoints con roles, ownership, rate limits y request limits.
- [x] Completar UI admin y UI clinica.
- [x] Completar pruebas unitarias, integracion y frontend smoke.
- [x] Actualizar manuales, runbook y checklist de lanzamiento.

---

## 5. Tareas por capa

### 5.1 Producto, legal y operaciones

- [ ] Definir requisitos documentales minimos para clinica:
  - licencia o registro SENASA declarado;
  - razon social o nombre comercial;
  - contacto responsable;
  - fecha de vencimiento o revalidacion;
  - documento de respaldo privado.
- [ ] Definir requisitos documentales minimos para veterinario:
  - nombre completo;
  - numero de licencia/colegiado segun regla interna;
  - clinica a la que pertenece;
  - documento de respaldo privado;
  - firma o sello opcional;
  - permiso explicito para emitir certificados.
- [ ] Definir vocabulario permitido:
  - usar "verificado por PawTrack CR";
  - usar "SENASA-ready";
  - evitar "verificado por SENASA" si no hay convenio;
  - evitar "aprobado por SENASA".
- [ ] Definir SLA operativo:
  - revision de clinica: 1-2 dias habiles;
  - revision de veterinario: 1 dia habil;
  - revalidacion previa a vencimiento: 30 dias antes.
- [ ] Definir motivos estandar de rechazo:
  - documento ilegible;
  - licencia no coincide;
  - datos incompletos;
  - duplicado;
  - evidencia vencida;
  - otro con detalle obligatorio.
- [ ] Definir politica de retencion de documentos de verificacion.
- [ ] Definir quien puede ver documentos privados: admin y clinic owner, no publico.

### 5.2 Dominio — `ClinicVerification`

- [ ] Agregar `DocumentUrl` privado.
- [ ] Agregar `SubmittedByUserId`.
- [ ] Agregar `ReviewedByAdminUserId` reemplazando o complementando `VerifiedByAdminUserId`.
- [ ] Agregar `ReviewedAt`.
- [ ] Agregar `ReviewNotes`.
- [ ] Agregar `ExpiresAt` obligatorio al verificar, salvo excepcion explicita.
- [ ] Agregar `RevalidationRequestedAt`.
- [ ] Agregar `SupersededAt` para cerrar verificaciones anteriores.
- [ ] Agregar metodos de dominio:
  - `Submit(...)`;
  - `AttachDocument(...)`;
  - `Verify(...)`;
  - `Reject(...)`;
  - `MarkExpired(...)`;
  - `RequestRevalidation(...)`;
  - `Supersede(...)`.
- [ ] Regla: solo una verificacion activa por clinica.
- [ ] Regla: cambiar licencia de clinica invalida o reabre verificacion.
- [ ] Regla: una verificacion vencida no permite emitir pasaportes.
- [ ] Tests de dominio para todos los estados y transiciones.

### 5.3 Dominio — `ClinicVeterinarian`

- [ ] Separar estados de veterinario:
  - `PendingReview`;
  - `Authorized`;
  - `Rejected`;
  - `Suspended`;
  - `Revoked`;
  - `Expired`.
- [ ] Agregar `DocumentUrl` privado.
- [ ] Agregar `SignatureImageUrl` privado o protegido.
- [ ] Agregar `SubmittedByUserId`.
- [ ] Agregar `ReviewedByAdminUserId`.
- [ ] Agregar `ReviewedAt`.
- [ ] Agregar `ExpiresAt`.
- [ ] Agregar `ReviewNotes`.
- [ ] Agregar `RejectionReason`.
- [ ] Agregar `SuspensionReason`.
- [ ] Agregar metodos de dominio:
  - `Submit(...)`;
  - `AttachDocument(...)`;
  - `AttachSignature(...)`;
  - `Authorize(...)`;
  - `Reject(...)`;
  - `Suspend(...)`;
  - `Reinstate(...)`;
  - `Revoke(...)`;
  - `MarkExpired(...)`.
- [ ] Regla: veterinario pendiente no puede emitir.
- [ ] Regla: veterinario vencido no puede emitir.
- [ ] Regla: veterinario suspendido/revocado no puede emitir.
- [ ] Regla: licencia unica por clinica.
- [ ] Tests de dominio para transiciones validas e invalidas.

### 5.4 Dominio — auditoria de verificacion

- [ ] Crear `VerificationAuditLog` o extender audit log especifico de certificados.
- [ ] Acciones sugeridas:
  - `ClinicVerificationSubmitted`;
  - `ClinicVerificationDocumentUploaded`;
  - `ClinicVerificationApproved`;
  - `ClinicVerificationRejected`;
  - `ClinicVerificationExpired`;
  - `VeterinarianSubmitted`;
  - `VeterinarianDocumentUploaded`;
  - `VeterinarianSignatureUploaded`;
  - `VeterinarianAuthorized`;
  - `VeterinarianRejected`;
  - `VeterinarianSuspended`;
  - `VeterinarianRevoked`;
  - `VeterinarianExpired`.
- [ ] Registrar `ActorUserId`, entidad, accion, timestamp y detalles no sensibles.
- [ ] No guardar URLs firmadas temporales ni datos personales innecesarios en `Details`.
- [ ] Indices por entidad, actor, accion y fecha.
- [ ] Tests de auditoria para comandos principales.

### 5.5 Application — comandos de clinica

- [ ] `SubmitClinicVerificationCommand`.
  - Clinica autenticada solicita/re-solicita verificacion.
  - Si hay verificacion activa, devuelve estado existente.
  - Si hay rechazo previo, permite reenviar.
- [ ] `UploadClinicVerificationDocumentCommand`.
  - Requiere clinic owner.
  - Solo PDF/JPEG/PNG/WebP.
  - Max 5 MB.
  - Blob privado.
  - Escaneo/guard de archivo si existe helper local.
- [ ] `GetMyClinicVerificationQuery`.
  - Devuelve estado, fechas, vencimiento, motivo de rechazo y acciones disponibles.
  - No devuelve blob privado directo si se decide descarga controlada.
- [ ] `RequestClinicReverificationCommand`.
  - Permite revalidar antes de vencimiento.
  - Cierra o supersede verificaciones anteriores segun regla.
- [ ] Tests unitarios de handlers.

### 5.6 Application — comandos admin de clinica

- [ ] `ReviewClinicVerificationCommand`.
  - Aprueba o rechaza.
  - Requiere admin.
  - Exige motivo al rechazar.
  - Exige `ExpiresAt` al aprobar.
  - Audita decision.
  - Notifica a la clinica.
- [ ] `ExpireClinicVerificationCommand`.
  - Admin o job puede marcar vencida.
  - Bloquea emision futura.
- [ ] `GetClinicVerificationsForAdminQuery`.
  - Paginado.
  - Filtros por status, fecha, vencimiento, busqueda por nombre/licencia.
  - `AsNoTracking`.
- [ ] `DownloadClinicVerificationDocumentQuery`.
  - Solo admin o owner de la clinica.
  - Audita descarga.
- [ ] Tests unitarios de permisos, rechazo, aprobacion y descarga.

### 5.7 Application — comandos de veterinario

- [ ] `SubmitClinicVeterinarianCommand`.
  - Reemplaza alta directa como autorizado.
  - Crea veterinario en `PendingReview`.
  - Requiere clinic owner.
  - Normaliza licencia.
- [ ] `UploadVeterinarianDocumentCommand`.
  - Blob privado.
  - Max 5 MB.
  - Tipos permitidos PDF/JPEG/PNG/WebP.
- [ ] `UploadVeterinarianSignatureCommand`.
  - Blob privado/protegido.
  - Max 2 MB.
  - Tipos permitidos PNG/JPEG/WebP.
  - Resize si se usa en PDF.
- [ ] `GetMyClinicVeterinariansQuery`.
  - Lista veterinarios con estado y acciones.
  - No lista documentos privados como URL publica.
- [ ] `RevokeMyClinicVeterinarianCommand`.
  - Clinic owner puede revocar veterinario propio con motivo.
  - No elimina certificados historicos.
- [ ] Tests unitarios de handlers.

### 5.8 Application — comandos admin de veterinario

- [ ] `ReviewClinicVeterinarianCommand`.
  - Aprueba/rechaza veterinario.
  - Exige motivo al rechazar.
  - Exige vencimiento al aprobar si la politica lo define.
  - Audita decision.
  - Notifica a la clinica.
- [ ] `SuspendClinicVeterinarianCommand`.
  - Admin suspende con motivo.
  - Bloquea nuevas emisiones.
- [ ] `ReinstateClinicVeterinarianCommand`.
  - Admin reactiva si la verificacion sigue vigente.
- [ ] `ExpireClinicVeterinarianCommand`.
  - Admin o job marca vencimiento.
- [ ] `GetClinicVeterinariansForAdminQuery`.
  - Paginado.
  - Filtros por status, clinica, licencia, vencimiento.
- [ ] `DownloadVeterinarianDocumentQuery`.
  - Solo admin o clinic owner.
  - Audita descarga.
- [ ] Tests unitarios de permisos y estado maquina.

### 5.9 Application — jobs y notificaciones

- [ ] Crear `VerificationExpirationJob`.
  - Corre diario.
  - Usa `IDistributedJobLock`.
  - Marca clinicas/veterinarios vencidos.
  - No procesa mas de un lote configurable por ciclo.
- [ ] Crear `VerificationRenewalReminderJob`.
  - Avisa 30, 15 y 7 dias antes de vencer.
  - Throttle para evitar duplicados.
- [ ] Notificar a clinica cuando:
  - solicitud recibida;
  - documento cargado;
  - verificacion aprobada;
  - verificacion rechazada;
  - verificacion por vencer;
  - verificacion vencida;
  - veterinario aprobado/rechazado/suspendido/revocado.
- [ ] Tests unitarios de jobs con repositorios fake o mocks de estado.

### 5.10 Infrastructure — EF Core

- [ ] Actualizar configuracion `ClinicVerificationConfiguration`.
- [ ] Actualizar configuracion `ClinicVeterinarianConfiguration`.
- [ ] Agregar configuracion `VerificationAuditLogConfiguration` si aplica.
- [ ] Indices requeridos:
  - `ClinicVerification.ClinicId + Status`;
  - `ClinicVerification.ExpiresAt`;
  - `ClinicVerification.SubmittedAt`;
  - `ClinicVeterinarian.ClinicId + LicenseNumber` unico;
  - `ClinicVeterinarian.ClinicId + Status`;
  - `ClinicVeterinarian.ExpiresAt`;
  - `VerificationAuditLog.EntityType + EntityId + CreatedAt`.
- [ ] Configurar longitudes maximas para todos los campos de texto.
- [ ] Configurar URLs de documentos con max length 500.
- [ ] Evitar cascadas destructivas sobre verificaciones historicas.
- [ ] Generar migracion `HardenClinicAndVeterinarianVerification`.
- [ ] Revisar migracion y snapshot.

### 5.11 Infrastructure — repositorios

- [ ] Extender `IClinicVerificationRepository`:
  - `GetByIdAsync`;
  - `GetLatestForClinicAsync`;
  - `GetActiveForClinicAsync`;
  - `GetPendingPagedAsync`;
  - `GetExpiringWithinAsync`;
  - `HasActiveVerificationAsync`;
  - `AddAsync`;
  - `Update`.
- [ ] Extender `IClinicVeterinarianRepository`:
  - `GetByIdAsync`;
  - `GetActiveForClinicAsync`;
  - `GetByClinicAsync`;
  - `GetPendingPagedAsync`;
  - `GetExpiringWithinAsync`;
  - `LicenseExistsForClinicAsync`;
  - `AddAsync`;
  - `Update`.
- [ ] Crear `IVerificationAuditLogRepository` si aplica.
- [ ] Usar `AsNoTracking()` en lecturas.
- [ ] Aplicar paginacion y limites en queries admin.
- [ ] No hacer joins en memoria para listados grandes.

### 5.12 Infrastructure — Blob Storage y archivos

- [ ] Crear contenedor privado `verification-documents`.
- [ ] Crear estructura de blobs:
  - `clinics/{clinicId}/verification/{verificationId}/{fileName}`;
  - `clinics/{clinicId}/veterinarians/{veterinarianId}/document/{fileName}`;
  - `clinics/{clinicId}/veterinarians/{veterinarianId}/signature/{fileName}`.
- [ ] Reutilizar validadores de archivo existentes si los hay.
- [ ] Rechazar ejecutables, SVG y contenido ambiguo.
- [ ] Normalizar nombres de archivo.
- [ ] Guardar content type y tamano si se agrega metadata.
- [ ] Descargar documentos por endpoint autenticado, no por URL publica.
- [ ] Auditar descarga de documentos.
- [ ] Tests de content-type y tamano.

### 5.13 API — endpoints de clinica

- [ ] `GET /api/clinics/me/verification`.
- [ ] `POST /api/clinics/me/verification`.
- [ ] `POST /api/clinics/me/verification/document` multipart.
- [ ] `GET /api/clinics/me/veterinarians`.
- [ ] `POST /api/clinics/me/veterinarians` crea `PendingReview`, no autorizado automaticamente.
- [ ] `POST /api/clinics/me/veterinarians/{id}/document` multipart.
- [ ] `POST /api/clinics/me/veterinarians/{id}/signature` multipart.
- [ ] `POST /api/clinics/me/veterinarians/{id}/revoke`.
- [ ] Todos requieren rol `Clinic`.
- [ ] Todos resuelven `clinicId` desde el usuario autenticado, no desde body libre.
- [ ] `RequestSizeLimit` explicito por endpoint.
- [ ] `EnableRateLimiting` explicito por endpoint.
- [ ] `ProblemDetails` para errores 400/403/404/422.

### 5.14 API — endpoints admin

- [ ] `GET /api/clinics/admin/verifications` paginado.
- [ ] `GET /api/clinics/admin/verifications/{id}`.
- [ ] `PUT /api/clinics/admin/verifications/{id}/review`.
- [ ] `POST /api/clinics/admin/verifications/{id}/expire`.
- [ ] `GET /api/clinics/admin/verifications/{id}/document`.
- [ ] `GET /api/clinics/admin/veterinarians` paginado.
- [ ] `GET /api/clinics/admin/veterinarians/{id}`.
- [ ] `PUT /api/clinics/admin/veterinarians/{id}/review`.
- [ ] `POST /api/clinics/admin/veterinarians/{id}/suspend`.
- [ ] `POST /api/clinics/admin/veterinarians/{id}/reinstate`.
- [ ] `POST /api/clinics/admin/veterinarians/{id}/expire`.
- [ ] `GET /api/clinics/admin/veterinarians/{id}/document`.
- [ ] Todos requieren rol `Admin`.
- [ ] Auditoria obligatoria en cambios y descargas.

### 5.15 API — endurecimiento de emision de pasaportes

- [ ] Ajustar `IssueVaccinePassportCommand` para requerir `ClinicVerification.DocumentUrl` si la politica lo exige.
- [ ] Ajustar emision para requerir veterinario `Authorized`, no solo `IsActive` booleano.
- [ ] Validar `ExpiresAt` de clinica y veterinario.
- [ ] Bloquear emision si la licencia de clinica cambio desde la verificacion.
- [ ] Bloquear emision si el veterinario no tiene documento aprobado.
- [ ] Guardar snapshots extendidos en `VaccinePassport` si se agregan nuevos campos.
- [ ] Tests de regresion para cada bloqueo.

### 5.16 Frontend — portal de clinica

- [ ] Crear panel `ClinicVerificationPanel`.
  - estado actual;
  - fecha de envio;
  - fecha de aprobacion/rechazo;
  - vencimiento;
  - motivo de rechazo;
  - CTA para reenviar o subir documento.
- [ ] Crear uploader de documento de clinica.
- [ ] Crear panel `ClinicVeterinariansPanel`.
  - lista de veterinarios;
  - estado;
  - licencia;
  - vencimiento;
  - acciones permitidas.
- [ ] Crear formulario de veterinario pendiente.
- [ ] Crear uploader de documento de veterinario.
- [ ] Crear uploader de firma/sello.
- [ ] Ajustar `CertificateIssueModal`:
  - solo listar veterinarios `Authorized`;
  - mostrar estado bloqueado si no hay clinica verificada;
  - mostrar estado bloqueado si no hay veterinario autorizado;
  - mostrar CTA hacia panel de verificacion.
- [ ] No mostrar URLs privadas de documentos.
- [ ] Toasts claros para aprobacion pendiente/rechazo/vencimiento.

### 5.17 Frontend — admin

- [ ] Agregar tab de verificaciones de clinicas en `AdminPage`.
- [ ] Lista paginada con filtros por estado y busqueda.
- [ ] Vista detalle con datos de clinica y documento descargable autenticado.
- [ ] Acciones aprobar/rechazar con modal de motivo.
- [ ] Campo `ExpiresAt` obligatorio al aprobar.
- [ ] Agregar tab de veterinarios.
- [ ] Lista paginada con filtros por estado, clinica y vencimiento.
- [ ] Vista detalle de veterinario con documento/firma.
- [ ] Acciones aprobar/rechazar/suspender/reactivar/vencer.
- [ ] Confirmaciones explicitas para acciones destructivas.
- [ ] Estados vacio/loading/error.

### 5.18 Frontend — pruebas

- [ ] `ClinicVerificationPanel.test.tsx`:
  - pending;
  - verified;
  - rejected;
  - expired;
  - upload document.
- [ ] `ClinicVeterinariansPanel.test.tsx`:
  - lista estados;
  - crear veterinario pendiente;
  - revocar veterinario;
  - no muestra URLs privadas.
- [ ] `CertificateIssueModal.test.tsx`:
  - bloquea sin clinica verificada;
  - bloquea sin veterinario autorizado;
  - emite con veterinario autorizado.
- [ ] `AdminClinicVerification.test.tsx`:
  - aprueba con vencimiento;
  - rechaza con motivo;
  - exige motivo al rechazar.
- [ ] `AdminVeterinarianVerification.test.tsx`:
  - aprueba;
  - rechaza;
  - suspende;
  - reactiva.

### 5.19 Backend — pruebas unitarias

- [ ] `ClinicVerificationDomainTests` completo de estado maquina.
- [ ] `ClinicVeterinarianDomainTests` completo de estado maquina.
- [ ] `SubmitClinicVerificationCommandHandlerTests`.
- [ ] `UploadClinicVerificationDocumentCommandHandlerTests`.
- [ ] `ReviewClinicVerificationCommandHandlerTests`.
- [ ] `DownloadClinicVerificationDocumentQueryHandlerTests`.
- [ ] `SubmitClinicVeterinarianCommandHandlerTests`.
- [ ] `UploadVeterinarianDocumentCommandHandlerTests`.
- [ ] `UploadVeterinarianSignatureCommandHandlerTests`.
- [ ] `ReviewClinicVeterinarianCommandHandlerTests`.
- [ ] `SuspendClinicVeterinarianCommandHandlerTests`.
- [ ] `ReinstateClinicVeterinarianCommandHandlerTests`.
- [ ] `VerificationExpirationJobTests`.
- [ ] `VerificationRenewalReminderJobTests`.
- [ ] Regression tests en `IssueVaccinePassportCommandHandlerTests`.

### 5.20 Backend — pruebas de integracion

- [ ] Clinica puede consultar su verificacion.
- [ ] Clinica puede enviar solicitud de verificacion.
- [ ] Clinica puede subir documento privado.
- [ ] Usuario no-clinica no puede acceder endpoints de clinica.
- [ ] Clinica A no puede ver/subir documentos de clinica B.
- [ ] Admin puede listar verificaciones pendientes.
- [ ] Admin puede aprobar con vencimiento.
- [ ] Admin no puede aprobar sin vencimiento si la politica lo exige.
- [ ] Admin puede rechazar con motivo.
- [ ] Admin no puede rechazar sin motivo.
- [ ] Clinica puede crear veterinario pendiente.
- [ ] Admin puede aprobar veterinario.
- [ ] Veterinario pendiente no puede emitir pasaporte.
- [ ] Veterinario vencido no puede emitir pasaporte.
- [ ] Veterinario suspendido no puede emitir pasaporte.
- [ ] Verificacion de clinica vencida bloquea emision.
- [ ] Descarga de documentos privados exige auth y rol correcto.

### 5.21 Seguridad y privacidad

- [ ] BOLA: clinica no puede operar sobre verificacion de otra clinica.
- [ ] BOLA: clinica no puede operar sobre veterinario de otra clinica.
- [ ] BOLA: clinic owner no puede aprobarse a si mismo como admin.
- [ ] Admin endpoints requieren rol `Admin`.
- [ ] Clinic endpoints requieren rol `Clinic`.
- [ ] Documentos se guardan en container privado.
- [ ] Verificacion publica no expone documentos.
- [ ] Listados admin tienen paginacion y filtros limitados.
- [ ] Rate limits especificos para uploads, review y downloads.
- [ ] Request size limits por endpoint.
- [ ] No logs con documentos, URLs privadas, licencias completas si se considera sensible.
- [ ] Auditoria de descargas de documentos.
- [ ] Antivirus/malware scanning definido si se habilita en plataforma.

### 5.22 Observabilidad

- [ ] Application Insights custom event: `ClinicVerificationSubmitted`.
- [ ] Custom event: `ClinicVerificationReviewed`.
- [ ] Custom event: `VeterinarianVerificationSubmitted`.
- [ ] Custom event: `VeterinarianVerificationReviewed`.
- [ ] Custom metric: verificaciones pendientes por antiguedad.
- [ ] Custom metric: veterinarios por vencer.
- [ ] Alerta: documentos de verificacion fallan en Blob Storage.
- [ ] Alerta: cola de verificaciones pendientes > umbral.
- [ ] Dashboard operativo para admin.

### 5.23 Documentacion y runbook

- [ ] Actualizar `docs/senasa.md` con Sprint 2 y estado de avance.
- [ ] Actualizar `docs/senasa-sprint1-todolist.md` si cambia algun contrato de Sprint 1.
- [ ] Actualizar `docs/Manuales/MANUAL_CLINICAS.md`:
  - solicitar verificacion;
  - subir documento;
  - administrar veterinarios;
  - estados y tiempos.
- [ ] Actualizar `docs/Manuales/MANUAL_ADMINISTRADOR.md`:
  - revisar clinicas;
  - revisar veterinarios;
  - suspender/revocar;
  - criterios de rechazo.
- [ ] Actualizar `docs/RUNBOOK_OPERACIONES.md`:
  - fallo upload documento;
  - documento sospechoso;
  - revalidacion vencida;
  - suspension urgente de veterinario;
  - auditoria de descarga.
- [ ] Actualizar politica de privacidad si se modifica el tratamiento de documentos.
- [ ] Actualizar terminos si se agregan obligaciones de veracidad documental.

---

## 6. Orden recomendado de ejecucion

### Dia 1 — TDD y modelo de estados

- [ ] Tests rojos para estados de `ClinicVerification`.
- [ ] Tests rojos para estados de `ClinicVeterinarian`.
- [ ] Definir enums y transiciones de dominio.
- [ ] Definir reglas de vencimiento y revalidacion.

### Dias 2-3 — Persistencia y documentos privados

- [ ] Extender entidades y configuraciones EF.
- [ ] Crear repositorios paginados y queries de vencimiento.
- [ ] Implementar upload/descarga privada de documentos.
- [ ] Generar migracion.

### Dias 4-5 — Application/admin workflows

- [ ] Implementar submit/review/reject/expire para clinicas.
- [ ] Implementar submit/review/suspend/reinstate/revoke para veterinarios.
- [ ] Implementar auditoria y notificaciones.
- [ ] Endurecer emision de pasaportes con nuevos estados.

### Dias 6-8 — API y frontend clinica

- [ ] Endpoints clinic owner.
- [ ] Panel de verificacion de clinica.
- [ ] Panel de veterinarios.
- [ ] Uploaders y estados bloqueados.
- [ ] Ajustes de emision de pasaporte.

### Dias 9-11 — Frontend admin

- [ ] Lista de verificaciones.
- [ ] Detalle y descarga documento.
- [ ] Aprobar/rechazar clinicas.
- [ ] Aprobar/rechazar/suspender veterinarios.
- [ ] Estados loading/empty/error.

### Dias 12-14 — Hardening

- [ ] Unit tests completos.
- [ ] Integration tests completos.
- [ ] Frontend tests criticos.
- [ ] Build backend/frontend.
- [ ] Docs/manuales/runbook.
- [ ] Revision de seguridad y privacidad.

---

## 7. Checklist de verificacion de avance

### 7.1 Avance funcional

- [ ] Clinica puede solicitar verificacion.
- [ ] Clinica puede subir documento privado.
- [ ] Clinica ve estado `Pending`.
- [ ] Admin ve solicitudes pendientes.
- [ ] Admin puede aprobar con vencimiento.
- [ ] Admin puede rechazar con motivo.
- [ ] Clinica ve motivo de rechazo.
- [ ] Clinica puede reenviar despues de rechazo.
- [ ] Verificacion vencida bloquea emision.
- [ ] Clinica puede registrar veterinario pendiente.
- [ ] Clinica puede subir documento de veterinario.
- [ ] Clinica puede subir firma/sello.
- [ ] Admin puede aprobar veterinario.
- [ ] Admin puede rechazar veterinario con motivo.
- [ ] Admin puede suspender veterinario.
- [ ] Clinica puede revocar veterinario propio.
- [ ] Solo veterinarios autorizados aparecen para emitir pasaportes.

### 7.2 Avance tecnico backend

- [ ] Dominio compila con nuevas transiciones.
- [ ] Configuraciones EF tienen longitudes e indices.
- [ ] Migracion revisada manualmente.
- [ ] Repositorios usan `AsNoTracking` en lecturas.
- [ ] Queries admin son paginadas.
- [ ] Uploads tienen `RequestSizeLimit`.
- [ ] Endpoints tienen rate limiting.
- [ ] `IssueVaccinePassportCommand` usa estados endurecidos.
- [ ] Auditoria se persiste para cambios y descargas.
- [ ] Jobs usan `IDistributedJobLock`.
- [ ] Notificaciones tienen throttle/idempotencia si aplica.

### 7.3 Avance tecnico frontend

- [ ] Panel de verificacion de clinica renderiza todos los estados.
- [ ] Upload de documento muestra progreso/errores.
- [ ] Panel de veterinarios renderiza todos los estados.
- [ ] Modal de emision bloquea estados no autorizados.
- [ ] Admin puede filtrar verificaciones.
- [ ] Admin puede revisar desde modal accesible.
- [ ] Admin puede descargar documentos autenticados.
- [ ] UI no muestra URLs privadas.
- [ ] Textos usan "SENASA-ready" y no prometen integracion oficial.
- [ ] Mobile layout revisado.

### 7.4 Seguridad y privacidad

- [ ] BOLA clinic verification.
- [ ] BOLA veterinarian ownership.
- [ ] Admin-only review.
- [ ] Clinic-only submission.
- [ ] Documentos privados no son publicos.
- [ ] Logs sin PII/documentos.
- [ ] Descargas auditadas.
- [ ] Rate limits probados.
- [ ] Payloads multipart limitados.
- [ ] Verificacion publica no cambia ni expone documentos.

### 7.5 Calidad y pruebas

- [ ] Unit tests dominio clinica pasan.
- [ ] Unit tests dominio veterinario pasan.
- [ ] Unit tests application pasan.
- [ ] Integration tests endpoints clinic pasan.
- [ ] Integration tests endpoints admin pasan.
- [ ] Regression tests pasaporte pasan.
- [ ] Frontend tests panel clinica pasan.
- [ ] Frontend tests panel admin pasan.
- [ ] `dotnet build` pasa.
- [ ] `dotnet test` backend pasa o fallos preexistentes quedan documentados.
- [ ] `npm run build` pasa.
- [ ] `npm test` frontend pasa o fallos preexistentes quedan documentados.

### 7.6 Documentacion y operacion

- [ ] Manual clinicas actualizado.
- [ ] Manual administrador actualizado.
- [ ] Runbook actualizado.
- [ ] Politica de privacidad revisada.
- [ ] Terminos de uso revisados.
- [ ] Checklist lanzamiento actualizado.
- [ ] Procedimiento de soporte para rechazos documentado.
- [ ] Procedimiento de revocacion/suspension documentado.

---

## 8. Definicion de terminado

Sprint 2 esta completo cuando:

- [ ] ninguna clinica puede emitir pasaportes sin verificacion documental vigente;
- [ ] ningun veterinario puede emitir sin autorizacion vigente;
- [ ] documentos de verificacion son privados y descargables solo por roles autorizados;
- [ ] admin puede aprobar/rechazar/suspender/revocar desde UI;
- [ ] clinica puede solicitar verificacion y administrar veterinarios desde UI;
- [ ] cambios de estado y descargas quedan auditados;
- [ ] vencimientos bloquean emision y generan recordatorios;
- [ ] pruebas unitarias, integracion y frontend cubren flujos criticos;
- [ ] build backend y frontend pasan;
- [ ] manuales y runbook describen la operacion real.

---

## 9. Estimacion del Sprint 2

| Bloque                          | Esfuerzo |
| ------------------------------- | -------- |
| Dominio + EF + migracion        | 2-3 dias |
| Uploads privados + repositorios | 2-3 dias |
| Application handlers + jobs     | 3-4 dias |
| API admin/clinica + seguridad   | 2-3 dias |
| Frontend clinica                | 3-4 dias |
| Frontend admin                  | 3-5 dias |
| Tests + docs + hardening        | 3-4 dias |

Total estimado: **18-26 dias-persona**.

Calendario probable:

- 1 dev full-stack senior: **4-6 semanas**.
- 2 devs senior: **2-3.5 semanas**.
- 2 devs + QA parcial: **2-3 semanas**.

---

## 10. Riesgos especificos del sprint

| Riesgo                                                          | Mitigacion                                         |
| --------------------------------------------------------------- | -------------------------------------------------- |
| Documentos privados quedan accesibles por URL publica           | Blob privado + descarga autenticada + tests BOLA.  |
| Admin aprueba sin vencimiento ni evidencia                      | Validator + UI required + integration tests.       |
| Veterinario creado por clinica queda autorizado automaticamente | Estado `PendingReview` por defecto + review admin. |
| Verificacion vencida no bloquea emision                         | Regression tests en `IssueVaccinePassportCommand`. |
| Listados admin crecen sin limite                                | Paginacion obligatoria + `AsNoTracking`.           |
| Logs contienen PII o rutas privadas                             | Auditoria estructurada y detalles minimizados.     |
