# NALA - TODO Enterprise para uso diario de clínicas veterinarias

> Estado: fuente de control para convertir NALA en app diaria de clínicas.  
> Corte: 2026-09-25.  
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
- [x] Descuentos con permiso.
- [x] Recibos.
- [x] Cierre de caja diario.
- [x] Anulación con motivo.
- [~] Integración o export para factura electrónica. Base de recibo y reporte
  implementada; integración tributaria externa queda como proveedor/gate fiscal.
- [~] Reporte por método de pago, veterinario y servicio. Implementado por método
  y servicio; desglose por veterinario queda para dashboard analítico ligado a
  agenda/consulta.
- [x] Auditoría financiera sin exponer datos sensibles.

**Gate de salida:** la clínica puede cobrar y cerrar el día desde NALA o con
integración aprobada.

### CP5 - Comunicación y CRM clínico

- [ ] Perfil de cliente clínico.
- [ ] Historial de comunicaciones por dueño/mascota.
- [ ] Plantillas de WhatsApp/email.
- [ ] Confirmación de cita.
- [ ] Recordatorio de cita.
- [ ] Recordatorio de vacuna y desparasitación.
- [ ] Seguimiento postconsulta.
- [ ] Envío de receta o indicaciones.
- [ ] Tareas internas: llamar, confirmar resultado, enviar documento.
- [ ] Segmentos: vacunas vencidas, controles pendientes, inactivos, geriátricos.
- [ ] Opt-in/opt-out y consentimiento de comunicaciones.

**Gate de salida:** NALA ayuda a retener clientes y reducir no-shows.

### CP6 - Panel diario y operación interna

- [ ] Dashboard de hoy.
- [ ] Sala de espera.
- [ ] Consultas en progreso.
- [ ] Tareas pendientes.
- [ ] Certificados por emitir.
- [ ] Pagos pendientes.
- [ ] Alertas de inventario.
- [ ] Mascotas perdidas cercanas.
- [ ] Métricas del día.
- [ ] Notificaciones internas por rol.

**Gate de salida:** NALA es la primera pantalla operativa de la clínica.

### CP7 - Multiusuario, seguridad y auditoría enterprise

- [ ] Usuarios internos por clínica.
- [ ] Invitación y revocación de usuario.
- [ ] Roles y permisos granulares.
- [ ] MFA obligatorio para admin, certificados, caja e integraciones.
- [ ] Auditoría de acceso a expediente.
- [ ] Auditoría de cambios clínicos.
- [ ] Auditoría de caja e inventario.
- [ ] Sesiones y dispositivos confiables.
- [ ] Export de auditoría para propietario de clínica.
- [ ] Pruebas BOLA/IDOR por clínica, veterinario, usuario interno y mascota.

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
