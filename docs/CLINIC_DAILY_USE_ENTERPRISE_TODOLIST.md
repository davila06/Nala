# NALA - TODO Enterprise para uso diario de clínicas veterinarias

> Estado: fuente de control para convertir NALA en app diaria de clínicas.  
> Corte: 2026-09-25.  
> Control enterprise de seguridad: [CLINIC_ENTERPRISE_SECURITY_CONTROL_MATRIX.md](CLINIC_ENTERPRISE_SECURITY_CONTROL_MATRIX.md).  
> Principio rector: NALA debe pasar de ser una capa de identidad, recuperación,
> expediente autorizado y certificación a ser el sistema operativo diario de una
> clínica veterinaria sin sacrificar privacidad, consentimiento ni trazabilidad.

## 1. Definición de éxito

NALA se considera app diaria de clínicas cuando una clínica puede abrirla al
inicio del día y operar de punta a punta:

1. revisar agenda y sala de espera;
2. identificar mascota por QR, microchip o búsqueda autorizada;
3. abrir expediente y consentimiento;
4. ejecutar consulta estructurada;
5. aplicar vacunas/medicamentos con lote e inventario;
6. emitir receta, certificado o pasaporte cuando aplique;
7. cobrar, facturar o registrar pago;
8. enviar instrucciones y recordatorios al dueño;
9. cerrar caja y revisar métricas;
10. auditar quién hizo qué, cuándo y con qué permiso.

## 2. Escala de estados

- `[ ]` Pendiente.
- `[~]` En progreso.
- `[x]` Cerrado con implementación, pruebas, documentación y evidencia.
- `[E]` Bloqueado por decisión legal, proveedor, contrato o regulación.

Nada pasa a `[x]` por existir solo en UI o solo en backend. El cierre exige:
backend, frontend, pruebas, seguridad/autorización, documentación y evidencia.

## 3. Checkpoints de control

### CP0 - Diseño y baseline

- [ ] Confirmar alcance: complemento clínico vs sistema clínico principal.
- [ ] Definir roles internos: propietario, admin, veterinario, recepción,
      asistente, cajero, solo lectura.
- [ ] Definir permisos por módulo y acciones sensibles.
- [ ] Definir modelo de datos para agenda, consulta, inventario, caja y
      comunicación.
- [ ] Revisar impactos de privacidad: salud, contacto, menores/familia,
      documentos, pagos y auditoría.
- [ ] Revisar qué se mantiene como SENASA-ready y qué requiere aprobación externa.
- [ ] Definir métricas de adopción diaria: citas/día, consultas cerradas,
      expedientes actualizados, recordatorios enviados y clínicas activas.

**Gate de salida:** diseño aprobado y backlog priorizado en este documento.

### CP1 - Agenda diaria usable

- [x] Agenda por día, semana y veterinario.
- [x] Estados operativos: programada, confirmada, checked-in, en consulta,
      completada, no-show y cancelada.
- [x] Transiciones válidas con auditoría.
- [x] Endpoint para listar agenda por rango.
- [x] Endpoint para cambiar estado.
- [x] Validación de solapamiento por veterinario.
- [x] Bloqueos de agenda por veterinario.
- [x] Reprogramación con validación de conflicto.
- [~] Recordatorio y confirmación de cita. Implementado: recordatorio interno `VetReminder`
  y confirmación operativa por estado `Confirmed`. Pendiente: WhatsApp/email externo
  con plantilla aprobada, opt-in, proveedor validado y evidencia de entrega.
- [x] Vista frontend de agenda diaria.
- [x] Auditoría visible/exportable de agenda.
- [x] Pruebas unitarias, integración y UI crítica.

**Gate de salida:** recepción puede operar el día clínico completo desde NALA.

### CP2 - Consulta clínica estructurada

- [x] Crear entidad de consulta clínica.
- [x] Asociar consulta a cita, mascota, clínica, veterinario y dueño.
- [x] SOAP: subjetivo, objetivo, evaluación y plan.
- [x] Signos vitales: peso, temperatura, frecuencia cardíaca, frecuencia
      respiratoria, condición corporal, dolor e hidratación.
- [x] Motivo de consulta y diagnóstico.
- [x] Tratamientos aplicados.
- [x] Receta/indicaciones para dueño. Incluye resumen para dueño, registro médico
      derivado e indicaciones imprimibles.
- [x] Adjuntos de consulta.
- [x] Cierre/firma de consulta.
- [x] Bloqueo o versionado tras cierre.
- [x] Resumen para dueño.
- [x] Plantillas por tipo de consulta.
- [x] Pruebas de ownership, grants, permisos y auditoría.

**Gate de salida:** un veterinario puede documentar una consulta real sin salir
del sistema.

### CP3 - Vacunas, medicamentos e inventario

> Los lotes y movimientos son por `ClinicId`. La mención histórica de "sede"
> en este checkpoint no significa inventario separado ni autorizado por
> `ClinicOrganizationSite`; `LocationName` es texto libre. Ver la matriz de
> seguridad para el gate multi-sede.

- [x] Catálogo de productos clínicos.
- [x] Tipos: vacuna, medicamento, antiparasitario, insumo, alimento y servicio.
- [x] Lotes, proveedor, fecha de vencimiento y costo.
- [x] Stock por clínica/sede.
- [x] Stock mínimo y alertas.
- [x] Kardex de movimientos.
- [x] Aplicación de vacuna descuenta inventario.
- [x] Medicamento aplicado o vendido descuenta inventario.
- [x] Asociación lote -> mascota -> consulta -> certificado.
- [x] Ajustes manuales con motivo y auditoría.
- [x] Reporte de inventario valorizado.
- [x] Pruebas de concurrencia para stock. Incluye token `rowversion`, regresión
      anti-stock negativo y validación de consumo.

**Gate de salida:** la clínica puede operar vacunas e insumos con trazabilidad.

### CP4 - Caja, cobros y facturación

- [x] Orden de venta desde cita o consulta.
- [x] Servicios y productos cobrables.
- [x] Métodos de pago: efectivo, tarjeta, SINPE, transferencia y crédito interno.
- [x] Pagos parciales.
- [x] Cuentas por cobrar.
- [x] Descuentos sólo con permiso administrativo.
- [x] Recibos.
- [x] Cierre de caja diario con total neto por método y fecha de Costa Rica;
      movimientos nuevos se rechazan tras el cierre.
- [x] Anulación con motivo y devoluciones manuales trazadas por pago, monto y
      comprobante; anulación de venta cobrada exige devolución íntegra previa.
- [~] Presentación a proveedor fiscal vía HTTPS con clave idempotente por venta,
  emisor verificado y estado `SubmittedToProvider`. No equivale a factura
  aceptada por Hacienda: faltan proveedor homologado, XML fiscal, firma,
  acuse tributario y nota de crédito para devoluciones fiscalizadas.
- [~] Reporte por método de pago, veterinario y servicio. Totales de cobros
  netos de devoluciones; ventas anuladas excluidas de servicios. Pendiente
  atribución de cobros sin cita y conciliación bancaria externa.
- [x] Auditoría financiera sin exponer datos sensibles.
- [x] Membresías de cajero/administrador separadas del rol global: concesión y
      revocación por titular con MFA; devolución, anulación, cierre y fiscal requieren
      además MFA del actor y claim de sesión emitido tras desafío. El refresh no
      conserva esa elevación: para operaciones sensibles se requiere nuevo login MFA.
      Ruta de caja de personal con permisos backend por clínica.

**UI CP4:** caja dedicada en `/clinica/caja` para titular y colaboradores;
el cajero no ve acciones administrativas. La devolución registra una operación
externa ya efectuada, no mueve dinero desde NALA. El cierre no es conciliación
bancaria certificada. Ninguna venta con presentación fiscal pendiente o enviada
puede anularse ni devolverse localmente sin flujo de nota de crédito.

**Gate de salida:** la clínica puede cobrar y cerrar el día desde NALA o con
integración aprobada.

### CP5 - Comunicación y CRM clínico

- [~] Perfil de cliente clínico. Lectura por mascota/tutor en dashboard acotado;
  falta vista detallada, búsqueda y paginación.
- [~] Historial de comunicaciones por dueño/mascota. Registro manual y últimas
  50 actividades visibles; correo con `X-Message-Id` de SendGrid se marca
  `Sent` (aceptado por proveedor), nunca `Delivered` sin webhook firmado.
- [~] Catálogo de plantillas clínicas fijo para email; nombres candidatos de
  WhatsApp sin homologación Meta. El envío de WhatsApp permanece bloqueado.
- [ ] Confirmación de cita.
- [ ] Recordatorio de cita.
- [ ] Recordatorio de vacuna y desparasitación.
- [ ] Seguimiento postconsulta.
- [ ] Envío de receta o indicaciones.
- [x] Tareas internas: llamar, confirmar resultado, enviar documento; crear,
      consultar y completar con auditoría.
- [~] Segmentos: seguimientos propios próximos, inactivos, geriátricos y tareas
  abiertas. Falta segmentación de vacunas con autorización médica y paginación.
- [~] Opt-in/opt-out por canal y propósito persistido y auditado: opt-in sólo
  mediante API del tutor autenticado y control en ficha de mascota; opt-out desde
  clínica o tutor. El job distribuido purga actividad a 365 días y tareas
  completadas a 730 días; conserva preferencias/opt-outs. Pendiente evidencia
  legal de consentimiento y validación de entrega por proveedor.

**Estado CP5:** correo clínico a pedido con opt-in, correo del tutor verificado,
plantilla fija, clave idempotente y aceptación SendGrid. No hay automatización
programada de recordatorios ni entrega confirmada; WhatsApp requiere destino
verificado y plantillas aprobadas. El gate de salida permanece abierto.

**Contrato proveedor pendiente de homologación:** `ClinicFiscal:ProviderUrl`
debe ser HTTPS; `ClinicFiscal:ApiToken` se inyecta como secreto. El emisor se
configura por `ClinicFiscal:Issuers:{clinicId:N}:TaxId` y `Verified=true` tras
revisión documental. La respuesta HTTP 202 con `X-Fiscal-Reference` sólo
confirma recepción del integrador; no es clave ni aprobación de Hacienda.
`SendGrid:ApiKey` y `SendGrid:FromEmail` deben estar configurados; sólo HTTP 202
con `X-Message-Id` confirma aceptación de correo. No configurar credenciales
reales en archivos versionados. Antes de producción se requieren pruebas con
el proveedor fiscal seleccionado, mensajes XML firmados, estado Hacienda,
reintentos ambiguos, notas de crédito y webhooks firmados de entrega.
La migración `DemoteUnverifiedElectronicInvoices` corrige únicamente documentos
históricos cuyo supuesto XML de respuesta era su propio XML firmado (o cadena
vacía). Hacer respaldo y auditar las filas afectadas antes de aplicarla; no
convierte documentos locales en facturas aceptadas por Hacienda.

**Tareas de homologación externa pendientes (bloqueadas por terceros):**

- [ ] Fiscal: firma de contrato y alcance con el integrador; validar URL HTTPS,
      token, idempotency key y certificado raíz del proveedor.
- [ ] Fiscal: pruebas con XML firmados reales, acuses de Hacienda, nota de crédito,
      reintento idempotente y manejo de errores de timeout / 429 / 5xx.
- [ ] Fiscal: confirmación documental del emisor y SKU/estado verificado por clínica.
- [ ] Meta/WhatsApp: aprobación de plantillas, número verificado, consentimiento y
      temas de privacidad para el destino del cliente.
- [ ] Meta/WhatsApp: webhook firmado de entrega/lectura y mapeo de estados reales;
      nada se marcará como entregado sin ese webhook.
- [ ] CRM: campañas y recordatorios sólo con consentimiento persistido y con auditoría
      de canal, propósito, template y result code del proveedor.

**Gate de salida:** NALA ayuda a retener clientes y reducir no-shows.

### CP6 - Panel diario y operación interna

- [x] Dashboard de hoy y operaciones clínicas principales; la pantalla de operación ya
      expone agenda por día/semana, estados, bloqueos, inventario, caja, CRM y
      comunicación desde la UI operativa.
- [x] Sala de espera y agenda por veterinario con filtros por día/semana.
- [x] Consultas en progreso y cambio de estado de cita por coordinación.
- [~] Tareas pendientes internas: faltan prioridades por rol, automatización de tareas
  de recordatorio y flujo de cierre por persona coordinadora.
- [~] Certificados por emitir y seguimientos postconsulta.
- [~] Pagos pendientes y cierre diario por método; requiere conciliar movimientos
  no documentados y banco externo.
- [x] Alertas de inventario y lotes por stock mínimo.
- [ ] Mascotas perdidas cercanas y alertas geográficas operativas.
- [x] Métricas del día con reportes de ventas, COBR y trazas de acción.
- [~] Notificaciones internas por rol; falta aprobación de destinatarios, frecuencia y
  deduplicación por sesión.

**Gate de salida:** NALA es la primera pantalla operativa de la clínica, con agenda,
operación, inventario, pagos y CRM visibles en un mismo panel.

### CP6 - Tareas de ejecución para la siguiente iteración

#### Sprint CP6-A: dashboard operativo del día

- [~] 1. Consolidar el resumen diario en un bloque visible de la primera pantalla:
  - citas del día
  - consultas en espera / en progreso
  - pagos pendientes
  - inventario crítico
  - tareas internas abiertas
  - alertas del día. Parcial: el panel muestra citas, consultas en progreso,
    inventario bajo, tareas, cobros y saldos abiertos, con alertas de stock bajo,
    vencimiento próximo, ventas pendientes y tareas vencidas. `/clinica/equipo`
    ahora integra agenda y métricas diarias para el colaborador. Falta una métrica
    explícita de sala de espera y una bandeja con navegación por alerta.
- [~] 2. Añadir tarjetas de resumen con métricas por clínica y sede:
  - total de citas programadas
  - total de consultas completadas
  - total de pagos del día
  - total de alertas de inventario. Parcial: las métricas actuales ya muestran
    ventas y saldos pendientes por clínica; el espacio del colaborador también
    cuenta citas, consultas en progreso/completadas y tareas. Faltan agregados
    comparables por sede y un reporte consolidado multi-sede.
- [x] 3. La agenda y las consultas diarias usan fechas de clínica en
     `America/Costa_Rica`, incluidas medianoche CR -> UTC, entrada `datetime-local`,
     formato de vencimiento y fecha por defecto del endpoint.
- [~] 4. La cola de trabajo ordena por prioridad persistida (urgente/alta/normal/baja)
  y vencimiento; agenda y métricas ya conviven en el espacio del colaborador, pero
  aún no hay una única cola priorizada que combine citas, caja, alertas y próximos
  7 días.
- [~] 5. El titular conserva el dashboard completo y existe una API de tareas CRM
  acotada por membresía; `/clinica/equipo` combina membresías clínicas/financieras,
  agenda del día y métricas de trabajo, y muestra colas de recepción, veterinario,
  asistencia, caja y gerencia. La consulta queda limitada a la clínica seleccionada;
  no es todavía un resumen agregado entre sedes.

#### Sprint CP6-B: tareas internas por rol

- [~] 6. Crear panel de tareas internas con filtros por rol y estado:
  - recepción: confirmar citas, reprogramar y registrar no-shows
  - veterinario: consultas pendientes, firmar y cerrar expediente
  - cajero: cobro de saldos pendientes; gerencia financiera: devoluciones y cierre
    de caja; asistencia: preparación clínica y revisión de inventario. El espacio
    del colaborador integra agenda/métricas con esas colas y permite a recepción
    cambiar estado de cita; caja y operación mantienen acciones específicas, aún
    sin una cola única de trabajo priorizada.
- [x] 7. Las tareas persisten prioridad, vencimiento, rol responsable y responsable
     individual opcional. El creador se asigna cuando pertenece al rol de la tarea;
     el backend valida asignados activos de la clínica.
- [x] 8. Clave idempotente única por clínica: reintentos iguales devuelven el mismo ID,
     payload distinto devuelve conflicto y una carrera concurrente recupera la fila
     ganadora sin duplicar.
- [~] 9. Creación y cierre registran auditoría; faltan cancelación, reasignación y
  exportación completa del historial de cambios de tarea.

#### Sprint CP6-C: seguimientos clínicos y recordatorios

- [ ] 10. Cerrar flujo de recordatorio de cita y seguimiento postconsulta.
- [ ] 11. Activar recordatorio de vacuna y desparasitación con validación de consentimiento
      y canal autorizado por el tutor.
- [ ] 12. Enviar mensajes de receta/indicaciones solo al destinatario verificado y con
      plantillas o contenido aprobados.
- [ ] 13. Diferenciar `Sent` del proveedor de `Delivered` confirmado por webhook firmado.

#### Sprint CP6-D: hardening y seguridad del panel

- [~] 14. Escrituras de tareas staff, cambios CRM del titular y acciones sensibles de
  caja/membresía exigen claim MFA del access token. También exigen step-up agenda,
  consulta, expediente/reminders del tutor, grants, certificados, inventario,
  escaneo/identidad sanitaria, API keys, verificación y cambios administrativos clínicos.
  El refresh no conserva
  elevación; sesiones/dispositivos confiables tienen listado, revocación, prueba rotada,
  bloqueo de access tokens hermanos y MFA. Trusted devices no sustituyen TOTP para
  Admin/Support/SuperAdmin. Pendiente: aprobación de excepciones de bajo riesgo y
  revisión de nuevas rutas fuera de los controladores inventariados.
- [~] 15. Rutas staff validan membresía activa por clínica y la revocación corta acceso
  con el mismo token. Hay pruebas con recursos ajenos reales para agenda, consulta,
  grants de mascota, lote de inventario, venta, CRM, sesiones y dispositivos; existe
  catálogo estructural de 110 acciones de `ClinicsController` y 27 acciones relacionadas
  de Medical/Certificates/PetClinicAccess, con verbo HTTP y template de ruta comprobados.
  Falta ejecutar BOLA dinámico contra cada ID/ruta y cada sede física; el modelo no tiene
  entidad de sucursal (`LocationName` de inventario no es un límite de autorización).
- [x] 16. Colas filtradas por rol, tipo y responsable en SQL; caja, gerencia y asistencia
      usan membresías apropiadas. Las respuestas staff excluyen preferencias, historial de
      comunicación y segmentos de clientes.
- [~] 17. Pruebas HTTP cubren MFA, BOLA entre clínicas para agenda/estado, cierre de
  consulta, grants de mascota, ajuste de lote, creación/ledger de venta y tareas CRM,
  más aislamiento usuario-sesión/dispositivo, refresh sin MFA y step-up para grants/
  certificados. La matriz estructural cubre 137 acciones, pero no ejecuta IDOR contra cada recurso
  ni cada sede; seguir SEC-01/SEC-02 en el documento de control enterprise.

**Migración de despliegue:** en `PawTrackDev` local ya se aplicó la cadena pendiente, incluida
`AddClinicOperationalTaskMetadata`. La migración permite tareas sin mascota, rellena
rol derivado, prioridad normal y clave única para tareas históricas e indexa las colas.
Para staging/producción hacer respaldo antes de aplicar. El downgrade elimina tareas sin mascota/tutor porque
el esquema anterior no puede representarlas; exportarlas antes de revertir.
`AddTrustedSessionLifecycle` y `AddClinicOrganizations` también están aplicadas en
`PawTrackDev`; el backfill local produjo 6 organizaciones, 6 sedes y 0 clínicas huérfanas.
No se aplicó ninguna migración a una base compartida. El estado de Azure compartido
no es verificable sin un target/conexión identificados. Ver matriz enterprise antes
de planificar una ventana y backup.

#### Sprint CP6-E: cierre de homologación externa

- [ ] 18. Documentar evidencia de aprobación del proveedor fiscal (HTTPS, token, emisor,
      idempotency, acuse, nota de crédito, error handling).
- [ ] 19. Documentar evidencia de aprobación de Meta/WhatsApp (plantillas, número
      verificado, consentimiento, webhook firmado, entregas reales).
- [ ] 20. Requiere cierre legal/comercial con terceros antes de marcar CP4 y CP5 como
      finales para producción.

**Estado de la fase:** la base funcional de CP6 está en UI y backend, pero la salida
final exige aprobación de proveedores externos y cierre de la operación diaria con
notas de crédito, entregas confirmadas y plantillas autorizadas.

**Plan P1 de salida controlada (aún abierto):**

1. Mensajería: aprobar plantillas y opt-in por propósito/canal; enviar con clave
  idempotente, registrar `Sent` solo tras aceptación del proveedor y `Delivered`
  solo tras webhook firmado. Probar timeout, duplicado, 429/5xx, revocación de
  consentimiento y destino incorrecto sin filtrar datos de salud.
2. Fiscal: homologar emisor y proveedor con XML/firma/acuses reales en sandbox;
  conciliar `SubmittedToProvider`, aceptado/rechazado por Hacienda y notas de
  crédito. Probar reintentos ambiguos y devoluciones sin declarar una factura
  aceptada antes del acuse oficial.
3. Seguridad: completar tabla endpoint × actor (owner/staff/finanzas/ajeno) ×
  recurso propio/ajeno × resultado, con IDs reales. Un `ClinicOrganizationSite`
  no autoriza por sí solo: añadir pruebas inter-sede únicamente cuando exista
  alcance efectivo de sitio en todos los módulos.
4. Release: identificar staging y versión exacta de `__EFMigrationsHistory`,
  aprobar backup/restauración y generar/revisar SQL de migraciones pendientes;
  aplicar primero allí y verificar backfill 1:1 de clínica a organización,
  owner principal, índice filtrado, conteos y smoke funcional. No aplicar a
  producción sin aprobación de operaciones/DBA.
5. Piloto: operar al menos una clínica en paralelo con su proceso actual;
  conciliar citas, inventario, cobros, facturas y comunicaciones a diario.
  Documentar discrepancias, recuperación ante fallos, SLA real y autorización
  legal/comercial antes de llamar a NALA sistema principal.

### CP7 - Multiusuario, seguridad y auditoría enterprise

**Estado de modelos:** ya existen membresías operativas de staff y finanzas
por clínica y una nueva membresía de organización creada en el registro. La
última no hereda permisos clínicos ni permite seleccionar sedes; consolidar
roles/invitaciones/revocaciones sin ampliar acceso implícitamente. El índice
filtrado de membresía activa tiene migración aditiva y prueba SQL temporal;
pendiente de rollout en staging.

- [ ] Usuarios internos por clínica.
- [ ] Invitación y revocación de usuario.
- [ ] Roles y permisos granulares.
- [x] MFA obligatorio para mutaciones clínicas/integraciones y step-up para acciones
      administrativas, certificados, caja y accesos médicos; mantener allowlist revisada.
- [ ] Auditoría de acceso a expediente.
- [ ] Auditoría de cambios clínicos.
- [ ] Auditoría de caja e inventario.
- [x] Sesiones y dispositivos confiables con revocación y step-up MFA.
- [ ] Export de auditoría para propietario de clínica.
- [~] Pruebas BOLA/IDOR: catálogo estructural 137 acciones y casos HTTP en familias
  críticas; falta matriz dinámica de cada recurso, usuario interno, veterinario y sede.

**Gate de salida:** operación multiusuario segura y defendible para clínicas
medianas o redes.

### CP8 - Integraciones, importación y API Partner

- [ ] Importar clientes desde CSV.
- [ ] Importar mascotas.
- [ ] Importar vacunas históricas.
- [ ] Importar inventario.
- [ ] Importar citas.
- [ ] API Partner ampliada para agenda, consulta, inventario y pagos.
- [ ] Webhooks: cita creada, cita completada, vacuna aplicada, pago recibido,
      certificado emitido.
- [ ] Widget de solicitud de cita.
- [ ] Dominio autorizado por widget.
- [ ] Rate limits y scopes por integración.
- [ ] Sandbox y documentación versionada.

**Gate de salida:** una clínica puede adoptar NALA sin empezar desde cero.

### CP9 - Analítica y dirección clínica

- [ ] Citas por día/semana/mes.
- [ ] No-show rate.
- [ ] Consultas completadas.
- [ ] Ingresos por servicio y veterinario.
- [ ] Vacunas aplicadas.
- [ ] Clientes nuevos vs recurrentes.
- [ ] Mascotas activas atendidas.
- [ ] Inventario más usado y margen.
- [ ] Certificados emitidos.
- [ ] Conversiones desde directorio/mapa/widget.
- [ ] Mascotas recuperadas vinculadas a la clínica.
- [ ] Export CSV/PDF con minimización de PII.

**Gate de salida:** la clínica ve valor de negocio, no solo operativo.

### CP10 - Mobile, offline y campañas de campo

- [ ] Vista tablet para consulta.
- [ ] Escaneo rápido móvil.
- [ ] Carga de foto/documento desde cámara.
- [ ] Firma táctil.
- [ ] Modo offline para campaña o feria.
- [ ] Sincronización con idempotencia.
- [ ] Resolución de conflictos offline.
- [ ] Pruebas en red lenta y móvil.

**Gate de salida:** NALA funciona en recepción, consultorio y campañas.

## 4. Roadmap de implementación

### Fase 1 - Uso diario mínimo viable

- [ ] CP1 Agenda diaria usable.
- [ ] CP2 Consulta clínica estructurada básica.
- [ ] CP6 Panel diario básico.
- [ ] Roles mínimos: admin, veterinario y recepción.

**Resultado:** la clínica usa NALA cada mañana para agenda y consulta.

### Fase 2 - Sistema operativo clínico

- [ ] CP3 Inventario clínico.
- [ ] CP4 Caja y cobros.
- [ ] CP5 Comunicación y CRM.
- [ ] CP7 Seguridad multiusuario ampliada.

**Resultado:** NALA puede reemplazar herramientas pequeñas de operación clínica.

### Fase 3 - ClinicPartner enterprise

- [ ] CP8 Integraciones y migración.
- [ ] CP9 Analítica directiva.
- [ ] CP10 Mobile/offline.
- [ ] SLA, soporte, runbooks y evidencias de staging.

**Resultado:** NALA puede venderse a clínicas grandes o redes clínicas.

## 5. Primer corte técnico seleccionado

El primer corte de implementación es **CP1 Agenda diaria usable**, empezando por:

- [ ] extender estados de `VeterinarianAppointment`;
- [ ] validar transiciones de estado en dominio;
- [ ] exponer comando para cambio de estado;
- [ ] listar agenda por rango desde API;
- [ ] añadir pruebas unitarias e integración enfocadas;
- [ ] documentar el flujo en el manual clínico.

Motivo: ya existe programación básica de citas, pero sin estados diarios la
clínica no puede operar recepción, sala de espera ni consulta desde NALA.
