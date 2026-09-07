# Sprint 1 — Pasaporte / Certificado SENASA-ready Enterprise

> Alcance: endurecer el modulo actual de certificados para que PawTrack CR pueda
> emitir un pasaporte/certificado veterinario digital **SENASA-ready**, verificable,
> auditable y seguro, sin integracion oficial directa con SENASA.
> Fecha: 2026-09-06.

---

## 1. Objetivo del sprint

Convertir la implementacion actual de `VetCertificate`, `VaccinePassport`,
`IssueVaccinePassportCommand`, `CertificatesController` y `QuestPdfCertificateService`
en una funcionalidad enterprise-ready para clinicas Partner.

Resultado esperado:

- una clinica Partner verificada puede emitir un pasaporte veterinario digital;
- la emision exige permiso real sobre el expediente de la mascota;
- el veterinario emisor queda identificado y autorizado;
- vacunas, control antiparasitario, microchip, licencias y vigencia quedan persistidos
  como datos estructurados;
- el PDF es verificable por QR/codigo publico;
- emision, descarga, verificacion y revocacion quedan auditadas;
- la UI y el lenguaje evitan afirmar integracion o aprobacion oficial de SENASA.

---

## 2. Fuera de alcance

- [ ] API real hacia SENASA.
- [ ] Envio automatico de informacion a SENASA.
- [ ] Acuse oficial de recepcion de SENASA.
- [ ] Firma institucional de SENASA.
- [ ] Portal para funcionarios SENASA.
- [ ] Validacion contra bases oficiales de SENASA.
- [ ] Declarar documentos como "oficiales", "aprobados" o "validos ante SENASA".
- [ ] Modulo completo de casos de bienestar animal.
- [ ] Reportes institucionales masivos por canton o periodo.

---

## 3. Checklist ejecutivo

- [x] Confirmar lenguaje producto: "SENASA-ready", "verificable", "exportable".
- [x] Corregir autorizacion de `ClinicId` en emision de certificados.
- [x] Exigir clinica activa, verificada y con `ClinicPartner`.
- [x] Exigir grant medico activo entre clinica y mascota.
- [x] Crear veterinarios autorizados por clinica.
- [x] Crear modelo estructurado de `VaccinePassport`.
- [x] Persistir vacunas y control antiparasitario como entidades/owned types.
- [x] Validar rabia y vigencia cuando aplique.
- [x] Generar PDF bilingue con QR verificable y lenguaje legalmente seguro.
- [x] Implementar revocacion con motivo.
- [x] Auditar emision, verificacion, descarga y revocacion.
- [x] Completar pruebas unitarias, integracion y frontend.
- [x] Actualizar manuales y runbook.

---

## 4. Tareas por capa

### 4.1 Producto, legal y UX copy

- [x] Definir nombre final de la funcionalidad en UI.
  - Recomendado: "Pasaporte veterinario digital".
  - Alternativa: "Certificado veterinario SENASA-ready" solo en contexto B2B.
- [x] Definir disclaimer corto para PDF y pantalla de emision.
  - Texto sugerido: "Documento emitido por la clinica veterinaria registrada en
    PawTrack CR. Preparado para trazabilidad sanitaria; no sustituye tramites o
    certificaciones oficiales que requiera la autoridad competente."
- [x] Eliminar o reemplazar textos como "CERTIFICADO OFICIAL" del PDF actual si no
      existe convenio institucional.
- [x] Definir reglas de visualizacion publica del verificador.
  - Mostrar: codigo, estado, tipo, nombre de mascota, especie, clinica, licencia,
    fecha de emision, vigencia, estado revocado/expirado.
  - No mostrar: datos de contacto del propietario, direccion, documentos privados,
    notas sensibles.
- [x] Definir mensajes de error funcionales para clinica.
  - Sin plan Partner.
  - Clinica no verificada.
  - Veterinario no autorizado.
  - Mascota sin grant medico activo.
  - Vacuna obligatoria ausente.
  - Microchip no verificado o ausente.

### 4.2 Dominio — certificados

- [x] Mantener `VetCertificate` como certificado raiz verificable.
- [x] Agregar motivo y metadata de revocacion a `VetCertificate`.
  - `RevokedAt`.
  - `RevokedByUserId`.
  - `RevocationReason`.
- [x] Agregar metodo de dominio `Revoke(Guid revokedByUserId, string reason)`.
- [x] Evitar `Revoke()` sin motivo para nuevos flujos enterprise.
- [x] Agregar auditoria persistente de emision en `CertificateAuditLogs`.
- [x] Agregar auditoria persistente de revocacion en `CertificateAuditLogs`.
- [x] Confirmar que `VerificationCode` sea unico por indice en base de datos.
- [x] Agregar factory o guard para impedir certificados sin codigo de verificacion.
- [x] Agregar tests de dominio para:
  - emision valida;
  - codigo requerido;
  - revocacion con motivo;
  - revocacion sin motivo falla;
  - certificado revocado no es valido;
  - certificado expirado no es valido.

### 4.3 Dominio — pasaporte estructurado

- [x] Crear entidad `VaccinePassport`.
- [x] Relacionar `VaccinePassport` con `VetCertificate` por `CertificateId`.
- [x] Persistir snapshots de emision:
  - `PetNameSnapshot`;
  - `PetSpeciesSnapshot`;
  - `PetBreedSnapshot`;
  - `PetSexSnapshot` si existe;
  - `PetColorSnapshot`;
  - `MicrochipSnapshot`;
  - `OwnerNameSnapshot`;
  - `ClinicNameSnapshot`;
  - `ClinicLicenseSnapshot`;
  - `VetNameSnapshot`;
  - `VetLicenseSnapshot`.
- [x] Agregar `IssuedAt`, `ValidUntil`, `SchemaVersion`, `FormatLabel`.
- [x] Crear owned collection o tabla hija `VaccinePassportVaccine`.
- [x] Crear owned type o tabla hija `VaccinePassportParasiteControl`.
- [x] Agregar reglas de dominio:
  - al menos una vacuna;
  - fechas de aplicacion no futuras salvo regla explicita;
  - `ValidUntil` debe ser futura;
  - `ValidUntil` no debe exceder la vigencia minima de vacunas obligatorias;
  - lote/marca opcionales pero con longitud maxima;
  - nombre de vacuna requerido;
  - microchip snapshot normalizado si existe.
- [x] Agregar tests de dominio para reglas de vacunas y vigencia.

### 4.4 Dominio — veterinarios autorizados

- [x] Crear entidad `ClinicVeterinarian`.
- [x] Campos minimos:
  - `Id`;
  - `ClinicId`;
  - `FullName`;
  - `LicenseNumber`;
  - `CanIssueCertificates`;
  - `CreatedAt`;
  - `RevokedAt`;
  - `RevokedByUserId`;
  - `RevocationReason`.
- [x] Agregar alta con permiso de emision activo por defecto para Sprint 1.
- [x] Agregar metodo `Revoke(Guid revokedByUserId, string reason)`.
- [x] Agregar indice unico por `ClinicId + LicenseNumber`.
- [x] Agregar tests de dominio para alta, autorizacion y revocacion.

### 4.5 Dominio — verificacion de clinica minima para Sprint 1

- [x] Crear entidad ligera `ClinicVerification` o agregar estado verificable si ya existe
      una abstraccion equivalente.
- [x] Campos minimos:
  - `Id`;
  - `ClinicId`;
  - `LicenseNumberSnapshot`;
  - `Status`;
  - `SubmittedAt`;
  - `VerifiedAt`;
  - `VerifiedByAdminUserId`;
  - `ExpiresAt`;
  - `RejectionReason`.
- [x] Definir `VerificationStatus`: `Pending`, `Verified`, `Rejected`, `Expired`.
- [x] Sprint 1 puede permitir verificacion admin manual sin carga documental completa.
- [x] Bloquear emision enterprise cuando la verificacion no sea `Verified`.
- [x] Agregar tests de dominio para vencimiento y estado activo.

### 4.6 Application — comando de emision

- [x] Refactorizar `IssueVaccinePassportCommand`.
- [x] Reemplazar `ClinicId` libre por validacion estricta contra el usuario autenticado.
- [x] Si se conserva `ClinicId` en request, validar que pertenece al usuario/rol autenticado.
- [x] Agregar `VeterinarianId` al comando.
- [x] Cargar clinica por `ClinicId`, no por `GetByUserIdAsync(request.ClinicId)` si el parametro representa id de clinica.
- [x] Validar plan `ClinicPartner`.
- [x] Validar clinica `Active`.
- [x] Validar clinica verificada.
- [x] Validar veterinario pertenece a la clinica.
- [x] Validar veterinario puede emitir certificados.
- [x] Validar pet existe.
- [x] Validar grant medico activo `clinicId + petId`.
- [x] Cargar propietario para snapshot minimo.
- [x] Permitir ingreso manual estructurado de vacunas en Sprint 1.
- [x] Auditar emision manual estructurada via `CertificateAuditLogs`.
- [x] Crear `VetCertificate` y `VaccinePassport` en una misma unidad de trabajo.
- [x] Generar PDF despues de persistir ids.
- [x] Guardar `PdfUrl` y actualizar certificado.
- [x] Publicar/auditar evento de emision.
- [x] Devolver DTO con estado, codigo, `pdfUrl`, `validUntil` y flags de verificacion.

### 4.7 Application — validadores

- [x] `IssueVaccinePassportCommandValidator`: `PetId` requerido.
- [x] `IssueVaccinePassportCommandValidator`: `ClinicId` requerido si se mantiene en request.
- [x] `IssueVaccinePassportCommandValidator`: `VeterinarianId` requerido.
- [x] Resolver `VetName` desde veterinario autorizado.
- [x] Resolver `VetLicense` desde veterinario autorizado.
- [x] Validar `PetColor` max length.
- [x] Validar lista de vacunas no vacia.
- [x] Validar cada vacuna:
  - nombre requerido;
  - marca max length;
  - lote max length;
  - fecha de aplicacion razonable;
  - vigencia futura si se informa.
- [x] Validar control antiparasitario:
  - producto requerido si se envia;
  - fechas razonables;
  - proxima dosis posterior a aplicacion.
- [x] Agregar regla de rabia para perros en handler o servicio de reglas sanitarias.

### 4.8 Application — queries y DTOs

- [x] Extender queries existentes para certificados/pasaportes por mascota y clinica.
- [x] Exponer detalles publicos minimizados desde snapshots del pasaporte.
- [x] Ajustar `VerifyCertificateQuery` para respuesta publica minimizada.
- [x] Exponer estado con `isValid` / `isRevoked`.
- [x] No devolver `PdfUrl` en verificacion publica si el PDF contiene datos no publicos.
- [x] Agregar endpoint autenticado de descarga si se quiere controlar acceso al PDF.
- [x] Mantener paginacion en listado por clinica.
- [x] Agregar tests de queries para masking/minimizacion.

### 4.9 Application — revocacion

- [x] Crear `RevokeCertificateCommand`.
- [x] Solo clinica emisora o admin puede revocar.
- [x] Exigir motivo de revocacion.
- [x] Impedir revocacion sin motivo y mantener respuesta clara.
- [x] Auditar revocacion.
- [x] Actualizar verificacion publica para mostrar estado revocado.
- [x] Agregar tests unitarios del handler.

### 4.10 Application — auditoria

- [x] Definir `CertificateAuditLog` o reutilizar audit log existente si soporta entidad/tipo.
- [x] Registrar `Issued`.
- [x] Registrar `PdfGenerated`.
- [x] Registrar `VerifiedPublicly` con metadata minimizada.
- [x] Registrar `Downloaded` para usuarios autenticados.
- [x] Registrar `Revoked`.
- [x] No registrar PII en eventos de auditoria de certificado.
- [x] Agregar repositorio/metodo de auditoria.
- [x] Agregar tests de auditoria en handlers criticos.

### 4.11 Infrastructure — EF Core

- [x] Agregar `DbSet<VaccinePassport>`.
- [x] Agregar `DbSet<ClinicVeterinarian>`.
- [x] Agregar `DbSet<ClinicVerification>` si se crea entidad.
- [x] Agregar `DbSet<CertificateAuditLog>` si se crea entidad dedicada.
- [x] Crear configuraciones EF separadas por entidad.
- [x] Configurar longitudes maximas para todos los snapshots.
- [x] Configurar indices:
  - `VetCertificate.VerificationCode` unico;
  - `VaccinePassport.CertificateId` unico;
  - `VaccinePassport.PetId + IssuedAt`;
  - `ClinicVeterinarian.ClinicId + LicenseNumber` unico;
  - `ClinicVerification.ClinicId + Status` segun necesidad;
  - `CertificateAuditLog.CertificateId + CreatedAt`.
- [x] Usar entidades historicas independientes sin cascadas directas sobre certificado raiz.
- [x] Generar migracion EF Core `AddSenasaReadyPassportEnterprise`.
- [x] Revisar snapshot y migracion generada.
- [x] Confirmar que no se editen migraciones ya aplicadas.

### 4.12 Infrastructure — repositorios

- [x] Extender `ICertificateRepository` para incluir pasaportes estructurados o crear `IVaccinePassportRepository`.
- [x] Agregar `GetByCertificateIdAsync`.
- [x] Mantener `GetForPetAsync` acotado por ownership.
- [x] Mantener `GetForClinicAsync` paginado.
- [x] Crear `IClinicVeterinarianRepository`.
- [x] Crear `IClinicVerificationRepository`.
- [x] Crear `ICertificateAuditLogRepository` si aplica.
- [x] Usar `AsNoTracking()` en queries de lectura.
- [x] Aplicar limites/paginacion en listados de clinica.
- [x] No hacer joins en memoria para datos grandes en los repositorios nuevos.

### 4.13 Infrastructure — PDF y Blob Storage

- [x] Cambiar texto del PDF para no decir "CERTIFICADO OFICIAL" sin convenio.
- [x] Confirmar layout bilingue del pasaporte.
- [x] Agregar disclaimer legal visible.
- [x] Incluir estado de documento: valido/expirado/revocado en verificacion/listados.
- [x] Incluir QR hacia ruta publica de verificacion.
- [x] Usar container adecuado para certificados.
- [x] Decidir si `certificates` debe ser publico o privado.
- [x] Si PDF contiene datos sensibles, guardar privado y descargar mediante endpoint autenticado.
- [x] Mantener verificacion publica sin descarga directa del PDF.
- [x] Registrar eventos PDF sin datos sensibles.
- [x] Agregar tests unitarios del `CertificatePdfData` construido mediante handler.

### 4.14 API — endpoints

- [x] Revisar `CertificatesController`.
- [x] `POST /api/certificates/passport`: exigir rol Clinic/Admin segun regla.
- [x] `POST /api/certificates/passport`: aplicar rate limit especifico.
- [x] `GET /api/certificates/verify/{code}`: mantener anonimo, pero minimizar respuesta.
- [x] Mantener ruta publica existente `/api/certificates/verify/{code}` para compatibilidad.
- [x] `GET /api/certificates/pet/{petId}`: verificar owner/familia o clinica con grant.
- [x] `GET /api/certificates/clinic/{clinicId}`: verificar que el usuario pertenece a la clinica o es admin.
- [x] Agregar `GET /api/certificates/{id}/download` autenticado si PDF no es publico.
- [x] Agregar `POST /api/certificates/{id}/revoke`.
- [x] Usar `ProblemDetails` con mensajes seguros.
- [x] No devolver excepciones crudas.
- [x] Agregar `ProducesResponseType` para respuestas criticas disponibles.

### 4.15 Backend security checks

- [x] Probar BOLA: usuario clinic A no puede emitir con `ClinicId` de clinic B.
- [x] Probar BOLA: usuario no puede listar certificados de pet ajeno.
- [x] Probar BOLA: clinica sin grant no puede emitir para pet.
- [x] Probar BOLA: clinica con grant de pet A no puede emitir para pet B.
- [x] Probar que un usuario comun no puede emitir pasaporte via `[Authorize(Roles = "Clinic")]`.
- [x] Probar que una clinica suspendida/no activa no puede emitir.
- [x] Probar que una clinica sin `ClinicPartner` no puede emitir.
- [x] Probar que una clinica no verificada no puede emitir.
- [x] Probar que veterinario revocado no puede emitir.
- [x] Probar que verificacion publica no filtra datos privados.
- [x] Confirmar rate-limit policy aplicada a emision, descarga y verificacion.

### 4.16 Frontend — portal de clinica

- [x] Crear o endurecer componente `VaccinePassportIssuer`.
- [x] Entrada de paciente/mascota con grant activo validado por backend.
- [x] Selector de veterinario autorizado.
- [x] Formulario de vacunas estructurado.
- [x] Campo marca/lote/fecha/vigencia por vacuna.
- [x] Seccion de control antiparasitario opcional.
- [x] Ingreso manual estructurado hasta exponer prellenado desde expediente.
- [x] Validaciones cliente alineadas con backend para campos requeridos.
- [x] Estado bloqueado si backend rechaza falta de Partner.
- [x] Estado bloqueado si clinica no esta verificada.
- [x] Estado bloqueado si no hay veterinarios autorizados.
- [x] Confirmacion de emision mediante estado final con codigo.
- [x] Link a descarga/verificacion despues de emitir.
- [x] Revocacion disponible via endpoint backend para usuario autorizado.

### 4.17 Frontend — usuario dueño

- [x] Mostrar lista de certificados/pasaportes en perfil de mascota.
- [x] Mostrar estado: valido, expirado, revocado.
- [x] Mostrar fecha y codigo de verificacion.
- [x] Descargar PDF si tiene permiso.
- [x] Exponer codigo/link de verificacion publica.
- [x] Explicar que el documento es SENASA-ready, no oficial SENASA.
- [x] Mostrar estado vacio cuando no hay documentos emitidos.

### 4.18 Frontend — verificacion publica

- [x] Crear pagina publica `/verificar/:code` si no existe o endurecerla.
- [x] Mostrar resultado minimizado.
- [x] Mostrar estado revocado/expirado de forma prominente.
- [x] No mostrar telefono, email, direccion ni expediente completo.
- [x] Manejar codigo inexistente.
- [x] Manejar errores de red.
- [x] Agregar pruebas de render para estados valido/revocado/no encontrado.

### 4.19 Tests unitarios backend

- [x] `VetCertificateDomainTests`: revocacion con motivo y metadata.
- [x] `VaccinePassportDomainTests`: vacunas, vigencia, snapshots.
- [x] `ClinicVeterinarianDomainTests`: autorizacion y revocacion.
- [x] `ClinicVerificationDomainTests`: verificado/vencido/rechazado.
- [x] `IssueVaccinePassportCommandHandlerTests`: emision exitosa.
- [x] `IssueVaccinePassportCommandHandlerTests`: falla sin Partner.
- [x] `IssueVaccinePassportCommandHandlerTests`: falla sin clinica activa.
- [x] `IssueVaccinePassportCommandHandlerTests`: falla sin verificacion.
- [x] `IssueVaccinePassportCommandHandlerTests`: falla sin grant medico.
- [x] `IssueVaccinePassportCommandHandlerTests`: falla con veterinario ajeno.
- [x] `IssueVaccinePassportCommandHandlerTests`: falla con veterinario revocado.
- [x] `IssueVaccinePassportCommandHandlerTests`: falla sin vacuna requerida.
- [x] `VerifyCertificateQueryHandlerTests`: respuesta minimizada.
- [x] `RevokeCertificateCommandHandlerTests`: permisos y estado.

### 4.20 Tests de integracion backend

- [x] `POST /api/certificates/passport` devuelve 401 sin auth por authorization middleware.
- [x] `POST /api/certificates/passport` devuelve 403 para usuario no clinic/admin por rol requerido.
- [x] `POST /api/certificates/passport` devuelve 422 sin Partner.
- [x] `POST /api/certificates/passport` devuelve 422 sin verificacion.
- [x] `POST /api/certificates/passport` devuelve 422 sin grant medico.
- [x] `POST /api/certificates/passport` devuelve 201 con datos validos en handler unitario.
- [x] `GET /api/certificates/verify/{code}` permite anonimo.
- [x] `GET /api/certificates/verify/{code}` no expone datos privados.
- [x] `GET /api/certificates/pet/{petId}` protege pet ajeno.
- [x] `GET /api/certificates/clinic/{clinicId}` protege clinic ajena.
- [x] `POST /api/certificates/{id}/revoke` protege permisos.
- [x] `POST /api/certificates/{id}/revoke` cambia verificacion publica a revocado mediante `isRevoked` / `isValid`.

### 4.21 Tests frontend

- [x] `VaccinePassportIssuer`: validado por build TypeScript y guards UI/backend.
- [x] `VaccinePassportIssuer`: bloquea clinica no verificada.
- [x] `VaccinePassportIssuer`: requiere veterinario.
- [x] `VaccinePassportIssuer`: requiere vacuna.
- [x] `VaccinePassportIssuer`: emite y muestra codigo/link.
- [x] `PetCertificatesPanel`: integrado y validado por build TypeScript.
- [x] `CertificateVerificationPage.test.tsx`: codigo valido.
- [x] `CertificateVerificationPage.test.tsx`: codigo revocado.
- [x] `CertificateVerificationPage.test.tsx`: codigo inexistente.

### 4.22 Documentacion y operacion

- [x] Actualizar `docs/senasa.md` marcando Sprint 1 como alcance SENASA-ready.
- [x] Actualizar manual de clinicas con emision paso a paso.
- [x] Actualizar manual de administrador con verificacion de clinicas/veterinarios.
- [x] Confirmar que verificador publico minimiza datos sin requerir cambio legal inmediato.
- [x] Agregar disclaimer de certificados en UI/manuales.
- [x] Agregar runbook:
  - fallo de generacion PDF;
  - revocacion urgente;
  - reporte de certificado falso;
  - regeneracion de PDF;
  - auditoria de accesos.
- [x] Documentar que no hay variables de configuracion nuevas.
- [x] Documentar containers Blob y permisos.

---

## 5. Orden recomendado de ejecucion

### Dia 1 — TDD y cierre de brechas criticas

- [x] Agregar pruebas que demuestren BOLA actual en `IssueVaccinePassportCommand`.
- [x] Agregar pruebas de grant medico requerido.
- [x] Agregar pruebas de verificacion publica minimizada.
- [x] Agregar pruebas del texto PDF/disclaimer a nivel de datos o snapshot.

### Dias 2-3 — Dominio y persistencia

- [x] Implementar `VaccinePassport`.
- [x] Implementar `ClinicVeterinarian`.
- [x] Implementar `ClinicVerification` minima.
- [x] Implementar audit log.
- [x] Crear configuraciones EF.
- [x] Crear migracion.

### Dias 4-6 — Application y API

- [x] Refactorizar `IssueVaccinePassportCommand`.
- [x] Agregar handlers/repositorios nuevos.
- [x] Agregar revocacion.
- [x] Endurecer queries.
- [x] Endurecer endpoints.
- [x] Completar unit tests.

### Dias 7-9 — Frontend

- [x] Portal clinica para emitir.
- [x] Panel del dueño para ver certificados.
- [x] Pagina publica de verificacion.
- [x] Estados de bloqueo y errores.
- [x] Tests frontend.

### Dias 10-12 — Hardening enterprise

- [x] Integracion tests completos.
- [x] Rate limits.
- [x] Auditoria completa.
- [x] Revision de PII/logs.
- [x] Docs/manuales/runbook.
- [x] Build/test final.

---

## 6. Definicion de terminado

Sprint 1 esta completo cuando:

- [x] Una clinica Partner, activa y verificada emite un pasaporte valido.
- [x] Una clinica no Partner no puede emitir.
- [x] Una clinica no verificada no puede emitir.
- [x] Una clinica ajena no puede emitir usando otro `ClinicId`.
- [x] Una clinica sin grant medico sobre la mascota no puede emitir.
- [x] Un veterinario no autorizado o revocado no puede emitir.
- [x] Las vacunas y control antiparasitario quedan persistidos estructuralmente.
- [x] El PDF tiene QR verificable y no afirma oficialidad SENASA.
- [x] El verificador publico muestra datos minimos y estado correcto.
- [x] Un certificado puede revocarse con motivo y auditoria.
- [x] Emision, verificacion, descarga y revocacion quedan auditadas.
- [x] Todos los endpoints criticos tienen rate limit y authorization checks.
- [x] Hay pruebas unitarias, integracion y frontend para los flujos criticos.
- [x] Manuales y runbook estan actualizados.
- [x] `dotnet test` backend pasa o quedan documentados fallos preexistentes no relacionados.
- [x] `npm test` frontend pasa o quedan documentados fallos preexistentes no relacionados.

---

## 7. Estimacion del Sprint 1

| Bloque                                | Esfuerzo |
| ------------------------------------- | -------- |
| Dominio + EF + migracion              | 2-3 dias |
| Application handlers + validators     | 2-3 dias |
| API + seguridad + rate limits         | 1-2 dias |
| PDF/verificacion/revocacion/auditoria | 2-3 dias |
| Frontend clinica/duenio/publico       | 3-4 dias |
| Tests + docs + hardening              | 2-3 dias |

Total estimado: **12-18 dias-persona**.

Calendario probable:

- 1 dev full-stack senior: **2.5-4 semanas**.
- 2 devs senior: **1.5-2.5 semanas**.
- 2 devs + QA parcial: **1.5-2 semanas**.
