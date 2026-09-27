# NALA - Guia enterprise+ del Calendario Integral de Salud

> Corte de evidencia: 2026-09-27. Estado: propuesta de implementacion, **no** funcionalidad terminada.
> Producto: seguimiento preventivo de mascotas para tutores y equipos clinicos autorizados.
> Fuente de verdad: codigo vigente; los documentos historicos pueden estar desactualizados.
> Este plan no autoriza recomendaciones diagnosticas, prescripciones automaticas ni envio de datos a terceros.

## 1. Resultado esperado y limites

Una persona autorizada puede ver, por mascota y en una vista familiar, los hechos
pasados y las acciones futuras de vacunas, desparasitacion, control antipulgas,
medicamentos, citas y esterilizacion. Cada evento muestra su origen, responsable,
estado, fecha local, instrucciones aprobadas y una accion pertinente. Los avisos
son oportunos, configurables, deduplicados y auditables. El calendario no modifica
el expediente clinico para presentar un evento: lee fuentes de verdad existentes.

**Fuera de alcance inicial:** diagnostico por IA, calculo autonomo de dosis,
calendarios vacunales prescriptivos sin validacion veterinaria, renovacion de
recetas, citas reservadas por el tutor, grabacion de consultas, comercializacion
de medicamentos y garantias de entrega por WhatsApp. La consulta remota es un
proyecto distinto; una cita virtual futura deberia aparecer como cita sin dar al
calendario acceso a video.

**Definicion de terminado:** no basta una cuadricula visual. Una capacidad pasa
a completa solo con fuente de datos, permisos, UI, accesibilidad, pruebas,
observabilidad, retencion, despliegue reversible y evidencia de aceptacion.

### Baseline comprobado en codigo

- [x] `MedicalRecord` tiene Vaccine, Deworming, Checkup, Surgery, Medication,
      fechas `Date`/`NextDueDate` y datos basicos de medicacion; el enum se guarda
      como entero: **agregar al final, nunca reordenar**
      ([MedicalRecord.cs](../backend/src/PawTrack.Domain/Medical/MedicalRecord.cs)).
- [x] `VetReminder` tiene vencimiento, completado y `ReminderSentAt`; la API
      permite crear, completar y borrar recordatorios con MFA para mutaciones
      ([VetReminder.cs](../backend/src/PawTrack.Domain/Medical/VetReminder.cs),
      [MedicalController.cs](../backend/src/PawTrack.API/Controllers/MedicalController.cs)).
- [x] Existe vista calendario por mascota, solo para `VetReminder`, en la pestana
      Salud ([ReminderCalendar.tsx](../frontend/src/features/medical/components/ReminderCalendar.tsx),
      [MedicalHistoryTab.tsx](../frontend/src/features/medical/components/MedicalHistoryTab.tsx)).
- [x] Existe consulta multimascota `GET /api/me/medical/reminders` y componente
      `ReminderDashboard`; **el componente no esta montado en ninguna pagina del
      frontend revisado** ([MedicalCommands.cs](../backend/src/PawTrack.Application/Medical/MedicalCommands.cs),
      [ReminderDashboard.tsx](../frontend/src/features/medical/components/ReminderDashboard.tsx)).
- [x] Hay protocolos fijos por especie para vacuna, desparasitacion y checkup;
      el motor solo computa alertas para tipos con al menos un registro previo
      ([HealthProtocol.cs](../backend/src/PawTrack.Domain/Medical/HealthProtocol.cs),
      [MedicalCommands.cs](../backend/src/PawTrack.Application/Medical/MedicalCommands.cs)).
- [x] Existen dos jobs diarios: uno envia push para recordatorios futuros a tres
      dias; otro analiza protocolos vencidos/proximos, crea recordatorio solo
      para Familia y envia push para todos los planes
      ([VetReminderHostedService.cs](../backend/src/PawTrack.Infrastructure/Medical/VetReminderHostedService.cs),
      [HealthAlertHostedService.cs](../backend/src/PawTrack.Infrastructure/Medical/HealthAlertHostedService.cs)).
- [x] `Pet` conserva `SterilizedStatus` y `SterilizedAt`; la agenda clinica tiene
      citas con estados y veterinario, pero hoy la cita la crea la clinica
      ([Pet.cs](../backend/src/PawTrack.Domain/Pets/Pet.cs),
      [VeterinarianAppointment.cs](../backend/src/PawTrack.Domain/Certificates/VeterinarianAppointment.cs)).
- [x] El expediente completo y su escritura por tutor estan sujetos a plan
      Familia, permiso de mascota y consentimiento de datos de salud; Plus tiene
      preview enmascarado y Explorador no recibe registros en el endpoint de
      historial ([MedicalCommands.cs](../backend/src/PawTrack.Application/Medical/MedicalCommands.cs)).

**Brechas verificadas que condicionan el diseno:** no existe tipo separado para
antipulgas; `Frequency` es texto, no una pauta de tomas; esterilizacion realizada
no equivale a una intervencion programada; el calendario no incorpora citas ni
hechos historicos; el dashboard multimascota no esta expuesto en una ruta. El job
de protocolos puede reenviar push cada dia cuando no existe recordatorio pendiente
(p. ej. en planes sin Familia); el job de recordatorios excluye vencidos. Ambos
calculan `today` en UTC aunque su disparo se anuncie en hora de Costa Rica. Estas
son observaciones del codigo, no incidentes de produccion demostrados.

La consulta actual de recordatorios usa `GetUpcomingRemindersAsync`, que excluye
los completados: aunque la UI tenga estilo para ellos, el calendario existente
no puede mostrar recordatorios finalizados con ese contrato.

## 2. Decisiones de producto y clinicas (gate D0)

Registrar decision, responsable, fecha y evidencia **antes** de fijar esquema o
prometer plazos. Las opciones no resueltas permanecen pendientes.

- [ ] Definir publico: tutor, miembros Familia, personal clinico con grant,
      veterinario asignado; establecer matriz de visibilidad y edicion por
      origen de dato, mascota, clinica y sede.
- [ ] Definir acceso por plan a calendario agregado, detalles medicos y push.
      No deducir del endpoint de recordatorios que todos los planes pueden ver
      dosis/diagnosticos; mantener el contrato de enmascaramiento existente hasta
      decision comercial y revision de privacidad.
- [ ] Validar con veterinarios de perros, gatos y otras especies el catalogo de
      tipos, la frecuencia de control antipulgas, etapas de vida, peso, riesgo,
      producto, edad, contraindicaciones y pautas variables; no universalizar
      intervalos de los seeds actuales.
- [ ] Acordar si una dosis registrada por tutor es `declarada` y una de clinica
      `verificada`; el calendario debe distinguir ambas, sin inventar una
      verificacion clinica.
- [ ] Definir cual es evento y cual es tarea: aplicacion realizada, cita agendada,
      dosis futura, recordatorio manual, ciclo terminado, cancelacion y no-show.
- [ ] Definir quien puede confirmar aplicacion, omitir dosis, suspender pauta,
      posponer, completar o reabrir; exigir motivo y trazabilidad para cambios
      clinicos. No convertir un clic en evidencia de administracion.
- [ ] Definir regla de precedencia y deduplicacion al coincidir un `NextDueDate`,
      un `VetReminder`, un protocolo y una cita para el mismo cuidado. Mantener
      fuentes originales identificables incluso al fusionar visualmente.
- [ ] Establecer umbrales de avisos por tipo, quiet hours, huso horario del tutor,
      escalamiento, resumen diario y opt-out; no enviar repetidamente alertas
      criticas sin accion util.
- [ ] Revisar textos y finalidad con asesoramiento juridico local: datos del
      tutor, acceso compartido, consentimiento para comunicaciones, proveedores,
      retencion y derecho de eliminacion. La aplicabilidad concreta de Ley 8968
      a cada dato/canal requiere validacion legal; no inferir que todo dato de
      salud animal sea automaticamente dato sensible de una persona.
- [ ] Aprobar accion en casos de urgencia: derivar a atencion veterinaria,
      sin consejo diagnostico generado ni falsa garantia de respuesta inmediata.

**Gate D0:** decision record firmado por Producto, Clinica, Seguridad/Privacidad,
Legal y Operaciones; escenarios de negocio y politica por plan aprobados.

## 3. Modelo objetivo y contratos (gate D1)

### 3.1 Fuentes de verdad

| Categoria       | Fuente primaria                                                       | Proyeccion futura                      | Regla imprescindible                                                                       |
| --------------- | --------------------------------------------------------------------- | -------------------------------------- | ------------------------------------------------------------------------------------------ |
| Vacunas         | `MedicalRecord(Vaccine)` y, cuando corresponda, pasaporte/certificado | Vencimiento explicito o pauta aprobada | No inferir vacuna especifica del enum generico; lote y evidencia siguen en origen          |
| Desparasitacion | `MedicalRecord(Deworming)`                                            | Proxima fecha o plan aprobado          | Distinguir interna/externa solo si se registra; no reinterpretar datos antiguos            |
| Antipulgas      | Nuevo subtipo/categoria clinica aprobada                              | Aplicacion y proxima aplicacion        | No migrar todos los `Deworming` historicos a antipulgas                                    |
| Medicamentos    | `MedicalRecord(Medication)` + pauta nueva versionable                 | Inicio, tomas, fin y pausas            | Horarios/dosis solo de receta o indicacion explicita                                       |
| Citas           | `VeterinarianAppointment`                                             | Inicio, reprogramacion, cancelacion    | Mostrar solo mascota/tutor/clinica autorizados; cancelar oculta accion, conserva historial |
| Esterilizacion  | `Pet.SterilizedStatus`/`SterilizedAt` y evento agendado si existe     | Cirugia planificada o realizada        | Un estado `No` no significa que se deba programar cirugia                                  |
| Manual          | `VetReminder`                                                         | Fecha elegida                          | Un recordatorio completado no certifica acto clinico                                       |

- [ ] Crear especificacion de un `HealthCalendarItemDto`/read model sin tabla de
      copia por defecto: `id` estable, `petId`, categoria/subtipo, `sourceType`,
      `sourceId`, `sourceVersion`, `occurrenceKey`, estado, `startsAt`/`endsAt`
      o `localDate`, zona IANA, titulo breve, origen/autor, `verified`, acciones
      permitidas y proxima fecha calculada. Evitar incluir PII o texto clinico
      sensible en vistas resumen y notificaciones.
- [ ] Decidir si la proyeccion sera on-demand desde repositorios filtrados en SQL
      o tabla materializada. Empezar on-demand con indices y limites; materializar
      solo con medicion de volumen/latencia. No unir colecciones completas en
      memoria ni hacer una consulta por mascota para el feed familiar.
- [ ] Definir ventana de consulta obligatoria (p. ej. 31/90 dias), paginacion
      estable por fecha+id, orden determinista y limites maximos; no retornar
      todo el expediente en cada cambio de mes.
- [ ] Modelar `care plan`/pauta como agregado separado solo si hace falta
      recurrencia, horario, versionado, adherencia, pausa y atribucion clinica.
      Vincular a `MedicalRecord` de origen sin alterar una consulta cerrada.
- [ ] Para antipulgas, preferir un concepto estructurado de control externo o
      subtipo de tratamiento aprobado; si se extiende `MedicalRecordType` entero,
      anexar valor al final y actualizar EF, API, DTO, UI, filtros, seeds y exportes.
- [ ] Persistir fechas unicas como `DateOnly` y ocurrencias horarias como
      `DateTimeOffset` + zona IANA del destinatario; definir horario por defecto
      para legado sin hora, DST, viajes y cambio de zona. No depender de UTC
      para el dia local de Costa Rica.
- [ ] Decidir recurrencia finita, excepciones y pausa/fin. Cada ocurrencia
      debera tener identidad estable (`planId`, version, fecha/hora) y limite
      de expansion para impedir series ilimitadas.
- [ ] Definir ciclo de vida `planned`, `due`, `done`, `skipped`, `cancelled`,
      `superseded`; mapear estados de cita existentes sin cambiarlos por
      comodidad de la UI. Conservar hecho historico incluso tras corregir fuente.
- [ ] Diseñar sincronizacion transaccional al editar/sustituir `MedicalRecord`
      o reprogramar/cancelar cita: invalidar derivados y no dejar avisos huerfanos.
      Versionar contrato/API antes de cambiar respuestas existentes.
- [ ] Mantener limites de modulo: calendario consulta `Medical`/`Certificates`
      por interfaces de lectura o eventos publicados, no por acceso directo a
      servicios internos de otro modulo. Mutaciones permanecen en su modulo
      propietario, segun arquitectura CQRS/MediatR del repositorio.

### 3.2 Reglas por tipo

- [ ] Vacunas: nombre exacto, fecha aplicada, lote/clinica si consta, siguiente
      fecha indicada y evidencia; mostrar `sin fecha recomendada` si falta.
- [ ] Desparasitacion: producto/pauta y fecha si constan; no asumir periodicidad
      clinica a partir de especie solamente.
- [ ] Antipulgas: producto, via, fecha aplicada, intervalo indicado, pausa y
      responsable. Evitar dosis por peso calculadas en frontend.
- [ ] Medicamentos: distinguir plan prescrito y log de administracion; frecuencia
      estructurada, zona, horas, fecha de inicio/fin, omisiones y cambio de pauta;
      requerir confirmacion veterinaria para recomendaciones clinicas.
- [ ] Citas: incluir creador, estado, fecha local y acceso del tutor; usar la
      misma cita persistida que la agenda clinica, no duplicar reserva.
- [ ] Esterilizacion: fecha realizada del perfil y, solo si existe agendamiento
      real, cirugia futura; conflictos entre perfil, certificado y registro
      clinico requieren regla aprobada y señal de discrepancia.
- [ ] Manuales: conservar titulo y notas, pero no etiquetar como vacunacion
      verificada solo por escoger tipo de recordatorio.

**Gate D1:** ERD, API schema, matriz de estados, politica de versionado,
prototipos y ejemplos de zona horaria revisados; migraciones aditivas aprobadas.

## 4. Entrega por incrementos

### E1 - Datos confiables y APIs

- [ ] Inventariar registros historicos reales: tipos genericos, recordatorios
      duplicados, `NextDueDate` pasado, citas huérfanas y fechas de esterilizacion
      inconsistentes. Crear informe de calidad sin modificar datos compartidos.
- [ ] Generar migraciones EF aditivas para pautas/aplicaciones/consentimientos
      nuevos, indices por `PetId`+fecha+estado y claves de idempotencia. Evitar
      editar migraciones aplicadas; definir backfill verificable y rollback.
- [ ] Implementar query CQRS por mascota y query consolidada por tutor/familia,
      con filtros por rango, autorizacion por recurso y proyecciones SQL acotadas.
      No exponer notas de otros tutores ni detalles de plan restringidos.
- [ ] Exponer historial de recordatorios completados dentro del rango autorizado;
      no reutilizar `GetUpcomingRemindersAsync` para la linea de tiempo completa.
      Incluir fecha de completado y fuente, con limites para historicos largos.
- [ ] Integrar citas por `PetId` y chequear relacion tutor-mascota; el endpoint
      clinico actual agenda por `ClinicId`, no es una API de calendario del tutor.
- [ ] Al crear o modificar datos derivados, asegurar transaccion y outbox
      idempotente para publicaciones; concurrencia/versiones para evitar perdida
      de cambios, especialmente en completar y reprogramar.
- [ ] Publicar contrato OpenAPI versionado con paginacion, codigos 400/401/403/
      404/409/422/429 y Problem Details sin detalles sensibles. Definir ETag
      solo si hay consumidores que lo necesiten.

**Gate E1:** query propia/ajena, ventana, orden, limites, versiones y migracion
probados contra SQL Server; tiempos medidos con volumen representativo.

### E2 - Experiencia tutor y clinica

- [ ] Convertir la pestana Salud en calendario/lista accesible de hechos y tareas,
      con hoy/proximos/vencidos/historico, filtros por tipo, detalle y fuente;
      mantener el historial clinico existente disponible sin duplicar formularios.
- [ ] Montar vista familiar multimascota en una ruta real con selector de
      mascota y rango, no solo dejar `ReminderDashboard` sin uso.
- [ ] Ofrecer acciones correctas por evento: ver fuente, registrar aplicacion,
      confirmar toma, posponer aviso, ver cita, marcar tarea, solicitar atencion.
      Deshabilitar acciones sin permiso y validar siempre en servidor.
- [ ] Estados completos: carga, vacio genuino, sin historial, sin permiso,
      preview por plan, error recuperable, offline PWA y sincronizacion pendiente.
- [ ] Accesibilidad: teclado, foco, nombres de controles, contraste, lectura de
      estados sin depender de color, tamanos tactiles, screen reader, mes sin
      saltos de layout; validar movil y escritorio.
- [ ] Separar visualmente `recomendado`, `registrado por tutor`, `verificado por
    clinica` y `vencido`; copiar claramente que una pauta generica no sustituye
      indicacion veterinaria.
- [ ] Añadir vista clinica solo donde hay permiso/grant activo; revocacion de
      acceso invalida datos en cache y enlaces del calendario inmediatamente.

**Gate E2:** flujos tutor/clinica funcionales en dispositivos reales y pruebas
de accesibilidad automatizadas + revision manual de teclado y lector.

### E3 - Motor de alertas y comunicaciones

- [ ] Consolidar el calculo de proximidad de `HealthAlertJob` y
      `VetReminderNotificationJob` para que una ocurrencia tenga una sola
      politica de envio; evitar duplicado por `VetReminder` y protocolo.
- [ ] Corregir condicion de repeticion diaria para planes sin Familia: persistir
      estado de notificacion por ocurrencia+canal, tambien si no se crea
      `VetReminder`; no usar `ReminderSentAt` como prueba de entrega.
- [ ] Definir transiciones `scheduled` -> `queued` -> `accepted_by_provider`
      -> `delivered`/`failed`/`suppressed`; no marcar como entregado un push o
      correo solo por retorno de la llamada al proveedor.
- [ ] Job con lock distribuido, paginacion por cursor, reintentos acotados,
      idempotencia por evento+destinatario+canal+ventana, dead letter y replay
      auditado. No ejecutar notificaciones dentro de una transaccion larga.
- [ ] Resolver fecha del destinatario en zona configurada, quiet hours, festivos
      si negocio lo exige, preferencias por canal/proposito, token de push
      invalido, revocacion y opt-out antes de cada envio.
- [ ] Evitar PII/diagnosticos/dosis en lock screen, asunto de email, URL y logs;
      el enlace autenticado lleva a detalle permitido y expira al revocar acceso.
- [ ] Recordatorios de medicamento por toma: cap de mensajes diarios, resumen
      configurable y distincion entre administracion no confirmada y omision
      comprobada. Escalamiento solo con consentimiento y politica definida.
- [ ] WhatsApp/email solo tras aprobacion de templates, opt-in verificable,
      proveedor/contrato y webhook firmado; seguir pendientes clinicos
      ([CLINIC_DAILY_USE_ENTERPRISE_TODOLIST.md](CLINIC_DAILY_USE_ENTERPRISE_TODOLIST.md)).

**Gate E3:** pruebas de cero envios duplicados en reintentos y escala horizontal,
opt-out efectivo, horario correcto y trazabilidad de resultado por canal.

### E4 - Seguridad, privacidad y cumplimiento

- [ ] Threat model: IDOR por `petId`/`itemId`/`appointmentId`, acceso de miembro
      revocado, medico de otra clinica/sede, links compartidos, suplantacion de
      remitente, alteracion de dosis, replay y fuga por cache/push.
- [ ] Matriz por actor/plan/consentimiento/permiso/origen y pruebas HTTP con
      IDs ajenos persistidos; comparar siempre `petId` de ruta y fuente, no solo
      claims. Respetar MFA/step-up existentes en mutaciones medicas.
- [ ] Revisar scope de sede activa y membresia clinic-scoped antes de habilitar
      vista compartida; el control multi-sede no se da por cerrado
      ([CLINIC_ENTERPRISE_SECURITY_CONTROL_MATRIX.md](CLINIC_ENTERPRISE_SECURITY_CONTROL_MATRIX.md)).
- [ ] Revisar consentimiento de tratamiento, comunicaciones y acceso clinico
      por separado; registrar finalidad, version, fecha, actor y revocacion.
- [ ] Actualizar exportacion, correccion, supresion, retencion, copias de
      seguridad y auditoria para modelos nuevos; definir tratamiento de
      historial necesario cuando el tutor elimina cuenta o cambia clinica.
- [ ] Limitar datos enviados a telemetria y terceros; secretos solo en gestor
      de secretos, cifrado en transito y reposo, acceso minimo e inventario de
      transferencias internacionales segun revision legal.
- [ ] Revisar politica de cache PWA/React Query: no persistir expediente de otro
      usuario tras logout, cambio de cuenta, plan, grant o sede activa.

**Gate E4:** aprobacion de Seguridad/Privacidad/Legal, matriz BOLA ejecutada,
retencion y consentimiento evidenciados; ningun incidente critico abierto.

### E5 - Calidad, rendimiento y lanzamiento

- [ ] Unit tests de recurrencias (meses, DST, leap day), precedencia, fechas,
      idempotencia, correccion de fuente y cancelacion/suspension.
- [ ] Integration tests SQL Server de indices, paginacion, concurrencia,
      autorizacion familiar/clinica, plan, consentimiento y ausencia de N+1.
- [ ] Contract tests API/DTO y pruebas de regresion para enum entero historico,
      exportes y consumidores de expediente/citas.
- [ ] Frontend tests de filtros, zonas horarias, eventos duplicados, MFA,
      estados de error/preview y accesibilidad; E2E de vacuna, antipulgas,
      medicamento, cita reprogramada, esterilizacion y opt-out.
- [ ] Ensayo de carga con hogares multimascota y meses densos; revisar plan de
      ejecucion SQL, memoria, p95 y coste por 1.000 mascotas activas.
- [ ] Desplegar bajo feature flag por cohorte, solo lectura primero; migracion
      aditiva separada, backfill con conteos/conciliacion y sin activar envio
      masivo retroactivo. Probar rollback del flag y consumidores antiguos.
- [ ] Piloto con clinicas y tutores voluntarios; soporte con FAQ, escalamiento,
      reporte de datos erróneos y procedimiento de desactivar envios sin perder
      expediente. Revisar textos con veterinarios.
- [ ] Hacer revision post-lanzamiento a 24 h, 7 dias y 30 dias; no retirar
      feature flag hasta pasar gate y aceptar riesgos residuales documentados.

**Gate E5:** criterios de la seccion 5 cumplidos; acta de go/no-go y runbook
aprobados por Producto, Clinica, Seguridad, Operaciones y QA.

## 5. Criterios de aceptacion medibles

Estos son **objetivos propuestos**, no mediciones actuales. Ajustarlos con
baseline y SLO aprobados antes del piloto.

| Dimension      | Criterio de salida                                                                                                                    |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Integridad     | 100% de items tienen fuente e ID verificable; ninguna cita cancelada se ofrece como proxima; un cambio de fuente invalida derivados   |
| Autorizacion   | 0 lecturas/mutaciones exitosas con mascota, clinica o sede ajena en matriz negativa; revocacion efectiva sin nuevo login              |
| Notificaciones | 0 duplicados por ocurrencia+canal+ventana en pruebas de reintento y concurrencia; 100% de opt-outs suprimen envios posteriores        |
| Privacidad     | Ningun mensaje en pantalla bloqueada revela tratamiento, dosis o diagnostico; exportacion/supresion cubre nuevos modelos              |
| Rendimiento    | p95 API de mes por mascota < 500 ms y vista familiar < 800 ms bajo carga acordada; sin consultas proporcionales al numero de mascotas |
| Disponibilidad | Objetivo inicial 99,9% de lectura del calendario; alertas con atraso medible y recuperacion mediante replay                           |
| Accesibilidad  | Flujos principales operables por teclado y lector, sin errores criticos automatizados en movil/escritorio                             |
| Operacion      | Dashboard, alertas, runbook, prueba de rollback y responsable de guardia definidos antes de abrir cohortes                            |

## 6. Monitoreo de avance

### 6.1 Registro de checkpoints

Actualizar esta tabla en cada revision semanal. `[ ]` pendiente, `[~]` en
curso, `[x]` cerrado con evidencia, `[E]` bloqueado externamente. Enlaces a
PR/test/captura/reporte y responsable real se agregan al iniciar cada etapa.

| Hito                                      | Estado | Responsable | Evidencia requerida             | Dependencia / riesgo |
| ----------------------------------------- | ------ | ----------- | ------------------------------- | -------------------- |
| D0 decisiones clinicas, legales y de plan | [ ]    | Por asignar | Decision record aprobado        | Pautas y permisos    |
| D1 modelo, contratos y UX                 | [ ]    | Por asignar | ERD + OpenAPI + prototipos      | D0                   |
| E1 datos/migraciones/queries              | [ ]    | Por asignar | PR + integracion SQL + medicion | D1                   |
| E2 experiencia tutor/clinica              | [ ]    | Por asignar | E2E + auditoria a11y            | E1                   |
| E3 motor y canales de alertas             | [ ]    | Por asignar | Reintentos, opt-out, trazas     | D0, E1               |
| E4 seguridad y privacidad                 | [ ]    | Por asignar | Threat model, BOLA, legal       | E1-E3                |
| E5 piloto y lanzamiento                   | [ ]    | Por asignar | Runbook, SLO, acta go/no-go     | E2-E4                |

### 6.2 Checklist semanal de control

- [ ] Actualizar responsables, fechas objetivo/reales y estado por hito.
- [ ] Adjuntar evidencia de backend, UI, test, seguridad y docs para cada `[x]`.
- [ ] Revisar dependencias externas y desbloqueos con fecha/decisor.
- [ ] Comparar volumen total de eventos por fuente vs proyeccion y contar
      huerfanos, duplicados, eventos fuera de rango y desfases de zona.
- [ ] Medir p50/p95/p99 por query de calendario, carga DB/indices, filas leidas,
      consumo de memoria y colas pendientes; separar mascota y hogar.
- [ ] Medir avisos elegibles, encolados, aceptados, entregados (si existe acuse),
      fallidos, reintentados, duplicados suprimidos, opt-outs y tiempo de atraso.
- [ ] Vigilar denegaciones 401/403/404/429 por ruta, sin registrar IDs/diagnosticos
      en claro; alertar ante patrones de enumeracion e incidentes de privacidad.
- [ ] Revisar discrepancias de calendario reportadas por usuarios y cambios
      clinicos invalidados; clasificar por origen, no corregir solo la pantalla.
- [ ] Revisar capacidad y presupuesto de proveedor de push/email, DB, colas y
      almacenamiento con volumen observado; escalar por demanda, no por supuesto.
- [ ] Registrar decisiones, riesgos residuales, cambios de alcance y proxima
      revision de go/no-go con firma de responsables.

### 6.3 Alertas operativas propuestas

- Avisar a guardia si el job no completa en su ventana, crece la cola, falla el
  lock repetidamente o una ocurrencia tarda mas que el SLO aprobado.
- Suspender canal/cohorte ante duplicados, fuga de detalle medico en push,
  incremento de accesos ajenos, backfill incorrecto o entrega no consentida;
  conservar posibilidad de lectura del expediente.
- Correlacionar con IDs opacos de ocurrencia, envio y trace; nunca con texto de
  receta ni datos de contacto en logs/metricas. Hacer replay desde outbox con
  operador autorizado y clave idempotente.

## 7. Orden sugerido y estimacion

Estimacion de planificacion, **no compromiso contractual**: con dos ingenieros,
QA compartido, decisiones clinicas disponibles y sin agregar checkout ni
telemedicina, D0-D1: 1-2 semanas; E1-E2: 3-5 semanas; E3-E5: 2-4 semanas
paralelizables parcialmente. Total orientativo: **6-10 semanas** para una version
enterprise+ controlada. Se amplía si se exige prescripcion validada por multiples
clinicas, WhatsApp homologado, sincronizacion externa o soporte multijurisdiccion.

**Ruta critica:** reglas clinicas/consentimiento -> fuentes y contrato ->
autorizacion -> proyeccion -> motor idempotente -> pruebas reales -> piloto.
Priorizar primero calendario de solo lectura y reconciliacion de datos; habilitar
alertas y mutaciones gradualmente, nunca todas las mascotas historicas de golpe.
