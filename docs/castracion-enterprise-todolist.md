# Modulo Enterprise de Campanas de Castracion - Todo List

> Estado: EN IMPLEMENTACION  
> Inicio: 2026-09-18  
> Alcance: jornadas de esterilizacion/castracion coordinadas por clinicas, municipalidades, aliados y Admin/SuperAdmin, con cupos, citas, consentimiento, trazabilidad clinica, certificados y reportes agregados.

## 0. Decisiones de arquitectura

- [x] Crear un modulo propio `CastrationCampaigns`; no reutilizar `AdoptionFair` porque sus reglas, datos sensibles y ciclo de vida son distintos.
- [x] Mantener Clean Architecture y CQRS mediante MediatR.
- [x] Usar `Guid.CreateVersion7()` para identificadores.
- [x] Mantener datos clinicos y consentimiento fuera del perfil publico.
- [x] Usar eventos de dominio/MediatR para integrar Pets, Certificates, Notifications y Payments sin acceso directo entre modulos.
- [x] Tratar los cupos como recurso transaccional; impedir sobreventa mediante concurrencia optimista en base de datos.
- [ ] Aprobar textos legales, politica de cancelacion y consentimiento informado con asesor veterinario/legal de Costa Rica.

## 1. Modelo de dominio

- [x] Crear `CastrationCampaign` con organizador, clinica ejecutora, canton, recinto, coordenadas, fechas, ventanas de reserva y capacidad.
- [x] Modelar estados `Draft`, `PendingApproval`, `Approved`, `Published`, `Active`, `Completed`, `Cancelled`.
- [ ] Modelar cupos por especie, sexo, rango de peso y franja horaria.
- [x] Crear `CastrationAppointment` con mascota, responsable, horario, estado y snapshot de elegibilidad.
- [x] Modelar estados de cita `Reserved`, `Confirmed`, `CheckedIn`, `Completed`, `NoShow`, `Cancelled`, `Rejected`.
- [x] Impedir reservas duplicadas para la misma mascota y campana a nivel de base de datos.
- [x] Impedir reservas fuera de ventana, en campanas no publicadas o sin capacidad.
- [ ] Registrar cancelacion, no-show y liberacion atomica del cupo.
- [x] Crear version/concurrency token para prevenir sobreasignacion de cupos.
- [ ] Definir requisitos configurables: edad, peso, ayuno, embarazo/celo, condiciones clinicas, vacunas y responsable mayor de edad.
- [ ] Registrar precio base, subsidio, IVA aplicable y total pagable sin almacenar datos de tarjeta.

## 2. Persistencia y rendimiento

- [x] Agregar configuraciones EF Core y `DbSet` propiedad exclusiva del modulo.
- [x] Crear migracion code-first; no modificar migraciones ya aplicadas.
- [x] Agregar indices para estado/fecha/canton, organizador, clinica, mascota y agenda.
- [x] Agregar indice unico de cita por campana + mascota.
- [x] Configurar row version/concurrency token para capacidad.
- [~] Implementar repositorios de escritura y read store con proyecciones `AsNoTracking`. Campanas y citas listas; faltan slots y lista de espera.
- [~] Paginar todas las consultas administrativas y publicas. Listado publico listo; faltan agendas administrativas.
- [ ] Evitar N+1 mediante proyecciones SQL para listados, ocupacion y reportes.

## 3. Autorizacion, privacidad y seguridad

- [x] Definir matriz de autorizacion: Admin/SuperAdmin crea y aprueba; Clinic gestiona ejecucion; Municipality/Ally organiza; Owner reserva; publico solo consulta datos publicados.
- [~] Validar tenant/organizacion en cada comando y consulta para evitar BOLA. Rol persistido y propiedad de mascota listos.
- [~] Aplicar rate limiting a busqueda publica, reserva, cancelacion y check-in. Busqueda y reserva listas.
- [ ] Mantener consentimiento, telefono, identificacion y notas clinicas fuera de respuestas publicas.
- [ ] Auditar cambios de estado, capacidad, elegibilidad, consentimiento y resultado clinico.
- [ ] Proteger comprobantes y consentimientos en Blob privado con URLs temporales autorizadas.
- [ ] Implementar idempotencia en reserva, pago, check-in y cierre clinico.
- [ ] Aplicar antifraude a multiples reservas/no-shows por responsable.

## 4. CQRS y API

- [x] Crear comando para borrador de campana.
- [~] Crear comandos para enviar a aprobacion, aprobar, publicar, iniciar, completar y cancelar. Envio, aprobacion y publicacion listos.
- [ ] Crear comandos para configurar y modificar slots antes de publicar.
- [x] Crear comando de reserva con validacion inicial de elegibilidad, consentimiento, duplicidad y capacidad atomica.
- [~] Crear comandos para confirmar, cancelar, check-in, no-show y completar procedimiento. Confirmar, check-in, no-show y completar listos; falta cancelacion Owner.
- [~] Crear query publica geolocalizada y paginada de campanas disponibles. Paginacion y filtro por canton listos; falta radio geografico.
- [ ] Crear query de detalle publico sin PII ni datos clinicos.
- [~] Crear queries operativas de agenda, ocupacion, lista de espera y resultados. Agenda clinica y citas del Owner listas.
- [~] Exponer endpoints REST con Problem Details y metadata OpenAPI. Creacion, listado publico y reserva listos.
- [~] Crear archivo `.http` con flujos happy path y errores. Happy path inicial listo; faltan cancelacion y operacion clinica.

## 5. Flujo clinico y consentimiento

- [ ] Crear formulario versionado de consentimiento informado por campana.
- [ ] Capturar aceptacion, version, fecha, IP hash y responsable legal.
- [ ] Implementar checklist preoperatorio y decision `Eligible`, `NeedsReview`, `Rejected`.
- [ ] Registrar check-in, peso real, evaluacion veterinaria y observaciones privadas.
- [ ] Registrar resultado, complicaciones, instrucciones postoperatorias y control recomendado.
- [ ] Actualizar estado de esterilizacion de la mascota mediante evento de dominio al completar.
- [ ] Emitir `CertificateType.Neutering` mediante integracion MediatR con Certificates.
- [ ] Crear trazabilidad de veterinario ejecutor y clinica responsable.

## 6. Pagos, subsidios y facturacion

- [ ] Permitir campanas gratuitas, subsidiadas y de pago.
- [ ] Modelar patrocinador/subsidio sin mezclarlo con el precio base.
- [ ] Integrar SINPE y CyberSource usando `PaymentTransaction` e idempotencia.
- [ ] Aplicar politica vigente: precio base neto; agregar 13% IVA si se solicita Factura Electronica.
- [ ] Asignar CABYS aprobado para servicio veterinario de esterilizacion.
- [ ] Emitir Factura/Tiquete DGT solo tras pago confirmado.
- [ ] Implementar reembolso/cancelacion segun politica aprobada.
- [ ] Conciliar ingresos, subsidios, exenciones y comprobantes en reportes administrativos.

## 7. Notificaciones y lista de espera

- [ ] Confirmar reserva por email, push y WhatsApp segun preferencias.
- [ ] Enviar recordatorios configurables 72 h y 24 h antes con instrucciones de ayuno.
- [ ] Notificar cambios de recinto, horario o cancelacion.
- [ ] Implementar lista de espera ordenada y promocion atomica al liberarse cupo.
- [ ] Expirar ofertas de lista de espera y pasar al siguiente responsable.
- [ ] Enviar instrucciones postoperatorias y recordatorio de control.
- [ ] Usar Outbox para evitar perdida de notificaciones.

## 8. Frontend PWA

- [~] Crear directorio publico con mapa, filtros por canton, fecha, especie, costo y disponibilidad. Ruta `/campanas-castracion`, filtro por canton, fechas, costo y disponibilidad listos; falta mapa y filtros avanzados.
- [ ] Crear detalle de campana accesible con requisitos, cupos y organizadores verificados.
- [~] Implementar reserva paso a paso: mascota, slot, elegibilidad, consentimiento y pago. Mascota, peso, ayuno, consentimiento e IVA listos; falta pasarela de pago y slots especializados.
- [ ] Mostrar desglose costo base, subsidio, IVA y total.
- [ ] Crear panel de citas del responsable con cancelacion y documentos.
- [x] Crear consola operativa de clinica para agenda, confirmacion, check-in, no-show y cierre clinico.
- [ ] Crear consola de organizador/municipalidad con capacidad, resultados y exportes.
- [x] Crear flujo Admin de creacion, envio, aprobacion y publicacion desde el panel Adopciones.
- [ ] Verificar accesibilidad WCAG, responsive, estados vacios, errores y carga.

## 9. Reportes, observabilidad y cumplimiento

- [ ] Crear indicadores agregados: citas, ocupacion, no-show, completadas, especie, sexo y canton.
- [ ] Aplicar suppression a grupos pequenos en NALA/reportes institucionales.
- [ ] Exportar reporte agregado SENASA-ready sin afirmar integracion oficial.
- [ ] Registrar metricas y trazas en Application Insights con correlation ID.
- [ ] Crear alertas por sobreventa, fallo de pagos, fallo de certificados y backlog de notificaciones.
- [ ] Definir retencion de consentimientos, registros clinicos y auditoria.
- [ ] Actualizar politica de privacidad, terminos, matriz de retencion y manuales.

## 10. Calidad y despliegue

- [~] Pruebas unitarias de invariantes y transiciones de dominio. Creacion, aprobacion previa, capacidad, snapshots y reserva cubiertos.
- [ ] Pruebas de concurrencia que demuestren que el ultimo cupo no se asigna dos veces.
- [~] Pruebas unitarias de handlers, validadores y autorizacion. Creacion y reserva cubiertas.
- [ ] Pruebas de integracion de API, tenancy, privacidad, idempotencia y pagos.
- [ ] Pruebas frontend de reserva y operacion clinica.
- [ ] Pruebas E2E del flujo publicar -> reservar -> pagar -> check-in -> completar -> certificar.
- [ ] Pruebas de carga sobre consulta publica y reserva concurrente.
- [ ] Migracion validada con `dotnet ef migrations script --idempotent`.
- [ ] Build, tests, typecheck y lint en cero errores.
- [ ] Feature flag y rollout gradual con campana piloto.

## Primera iteracion en curso

- [x] RED: especificar invariantes de creacion, publicacion, capacidad y reserva.
- [x] GREEN: implementar agregados de dominio minimos.
- [x] Integrar persistencia, migracion, CQRS de creacion, consulta publica y reserva inicial.
