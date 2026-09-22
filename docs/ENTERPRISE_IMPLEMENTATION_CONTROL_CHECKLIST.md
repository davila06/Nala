# PawTrack CR - Enterprise Implementation Control Checklist

> **Fecha de corte:** 2026-09-21
> **Fuente funcional:** [FEATURES.md](FEATURES.md)
> **Estado técnico:** [ENTITLEMENTS_IMPLEMENTATION.md](ENTITLEMENTS_IMPLEMENTATION.md)
> **Propósito:** checklist único para controlar el avance de los pendientes enterprise, sus dependencias, pruebas y evidencia de cierre.

## 1. Reglas de control

- [ ] Toda capacidad comercial tiene una clave de entitlement persistida.
- [ ] El backend es la autoridad final para plan, cuota, ownership, tenant, rol y estado operativo.
- [ ] Todo consumo por ciclo es idempotente y tiene índice único.
- [ ] Todo downgrade conserva datos y solo cambia publicación, escritura o disponibilidad comercial.
- [ ] Toda operación mutable tiene prueba unitaria y prueba de integración cuando cruza HTTP, tenant o gateway.
- [ ] Toda migración se aplica primero en entorno de prueba y se verifica antes de producción.
- [ ] Todo cambio comercial actualiza código, catálogo, frontend, documentación y soporte en el mismo cambio.

## 2. Tablero ejecutivo

| ID     | Área                              | Prioridad | Estado      | Dependencias               | Evidencia de cierre                  |
| ------ | --------------------------------- | --------: | ----------- | -------------------------- | ------------------------------------ |
| ENT-01 | Contrato de importación masiva    |        P0 | En progreso | Producto, formato CSV/JSON | API, validadores, migración, pruebas |
| ENT-02 | Modo lectura por agregado         |        P0 | Parcial     | Política por módulo        | Job, estados, pruebas de downgrade   |
| ENT-03 | Analítica avanzada proveedores    |        P1 | Pendiente   | Métricas y retención       | Endpoints, entitlement, pruebas      |
| ENT-04 | Promociones con cuotas            |        P1 | Pendiente   | Decisión comercial         | Entitlements, redenciones, auditoría |
| ENT-05 | Cobro consolidado HTTP            |        P0 | Parcial     | Gateway de prueba          | Prueba HTTP, transacción, gateway    |
| ENT-06 | Migración de límites hardcodeados |        P0 | En progreso | Catálogo persistido        | Auditoría sin constantes comerciales |
| ENT-07 | Add-ons comerciales completos     |        P1 | Parcial     | Precio y facturación       | Prorrateo, renovación, transacción   |
| ENT-08 | UI de entitlements                |        P1 | Parcial     | Snapshot API               | Medidores por módulo y estados       |
| ENT-09 | Auditoría comercial               |        P1 | Parcial     | AuditLog                   | Activar, renovar, downgrade, addon   |
| ENT-10 | Suite completa                    |        P0 | Pendiente   | Bloqueo externo eVista2024 | Backend/frontend verdes              |
| ENT-11 | Estabilidad Vitest/JSDOM/MSW      |        P1 | Parcial     | Setup de red               | Sin timeout ni ruido no controlado   |

## 3. ENT-01 - Contrato de importación masiva

### Decisiones de producto obligatorias

- [ ] Definir recursos importables: productos de tienda, servicios de proveedor, capturas municipales u otros.
- [ ] Definir formatos aceptados: CSV, JSON o ambos.
- [ ] Definir codificación requerida: UTF-8.
- [ ] Definir tamaño máximo de archivo por plan.
- [ ] Definir máximo de filas por operación y por ciclo.
- [ ] Definir comportamiento ante duplicados: rechazar, actualizar, omitir o upsert.
- [ ] Definir clave de idempotencia de la operación.
- [ ] Definir si la importación es síncrona o asíncrona.
- [ ] Definir retención de archivo original y reporte de errores.

### Backend

- [x] Crear modelo persistente `ImportJob`.
- [x] Crear estados de job y errores por fila.
- [x] Añadir hash de archivo y clave de idempotencia al modelo.
- [x] Añadir índices por tenant, hash e idempotencia.
- [x] Generar migración `AddImportJobs`.
- [x] Crear repositorio EF aislado por tenant.
- [x] Crear endpoints HTTP de carga, estado y errores.
- [x] Aplicar límite técnico inicial de 5 MB y 1.000 filas.
- [x] Separar límite técnico de 10.000 filas de cuota comercial `BulkImportLimit`.
- [x] Requerir `Idempotency-Key` y devolver el job existente en reintentos.
- [x] Conectar procesamiento CSV/JSON de productos de tienda.
- [x] Conectar procesamiento CSV/JSON de servicios de proveedores.
- [x] Cubrir procesamiento parcial de productos y servicios con pruebas unitarias.
- [x] Crear política central y pruebas de modo lectura para servicios, sedes y cantones excedentes.
- [ ] Procesar en streaming; no cargar archivos grandes completos en memoria.
- [ ] Aplicar transacciones por lote pequeño, no una transacción monolítica.
- [ ] Implementar idempotencia por `TenantId + ImportHash + IdempotencyKey`.
- [ ] Impedir acceso cruzado a jobs o archivos de otro tenant.
- [ ] Generar reporte descargable de errores sin PII innecesaria.
- [ ] Emitir métricas: filas recibidas, aceptadas, rechazadas, duplicadas y duración.
- [ ] Añadir rate limiting específico para importaciones.

### Frontend y API

- [ ] Crear endpoint `POST /api/.../imports`.
- [ ] Crear endpoint de estado `GET /api/.../imports/{id}`.
- [ ] Crear endpoint de reporte `GET /api/.../imports/{id}/errors`.
- [ ] Mostrar progreso, estado, filas válidas, errores y opción de descarga.
- [ ] Diferenciar `no incluido`, `cuota agotada`, `archivo inválido` y `error de procesamiento`.

### Pruebas

- [ ] CSV válido.
- [ ] JSON válido.
- [ ] Encabezados faltantes.
- [ ] Columnas desconocidas.
- [ ] Codificación no UTF-8.
- [ ] Fila duplicada.
- [ ] Archivo excede tamaño.
- [ ] Operación excede filas.
- [ ] Reintento idempotente.
- [ ] Tenant incorrecto.
- [ ] Cancelación y reanudación.
- [ ] Reporte de errores no filtra secretos ni PII.

## 4. ENT-02 - Modo lectura automático tras downgrade

### Política común

- [ ] No eliminar datos históricos.
- [ ] Conservar acceso de lectura autorizado.
- [ ] Bloquear creación, edición, publicación o expansión sobre recursos excedentes.
- [ ] Registrar qué recursos pasaron a modo lectura.
- [ ] Permitir reactivación automática al hacer upgrade.
- [ ] Mantener activo el ciclo esencial de recuperación de mascotas perdidas.

### Clínicas

- [ ] ClinicPartner -> ClinicPlus: revocar API keys Partner al vencimiento.
- [ ] Desactivar widgets y dominios no incluidos.
- [ ] Conservar certificados y pasaportes verificables en lectura.
- [ ] Conservar auditoría y revocación documental.
- [ ] Bloquear nuevas emisiones cuando no exista entitlement.
- [ ] Probar upgrade que reactive capacidades permitidas.

### Proveedores

    - [x] Implementar modo lectura/inactivo para recursos excedentes tras downgrade.
    - [ ] Reactivar recursos elegibles al upgrade.
    - [ ] Agregar pruebas de upgrade/reactivación.

### Municipalidades

- [ ] RedRegional -> Full: desactivar dashboard regional y transferencias.
- [ ] Full -> Basica: conservar capturas e historial en lectura.
- [ ] Desactivar cantones adicionales.
- [ ] Bloquear cargas masivas, reportes y API institucional fuera del entitlement.
- [ ] Aplicar agregación y supresión de datos sensibles en reportes.

### Tiendas

- [x] Downgrade de productos y sedes excedentes a inactivo.
- [ ] Agregar pruebas de upgrade/reactivación.
- [ ] Validar pedidos históricos tras downgrade.

## 5. ENT-03 - Analítica avanzada de proveedores

- [ ] Definir métricas comerciales: vistas, clics, reservas, conversión, cancelaciones, ingresos y ocupación.
- [ ] Definir retención por membresía.
- [ ] Crear `ProviderAnalyticsEvent` o reutilizar evento analítico existente con tenant/provider.
- [ ] Crear consultas SQL agregadas; evitar N+1 y joins en memoria.
- [ ] Añadir entitlement `ProviderAnalyticsRetentionDays`.
- [ ] Añadir entitlement `AdvancedProviderAnalyticsEnabled`.
- [ ] Aplicar autorización por ownership y tenant.
- [ ] Implementar endpoint resumido y endpoint detallado.
- [ ] Implementar exportación CSV para Featured si aplica.
- [ ] Añadir métricas de uso y auditoría de exportaciones.
- [ ] Cubrir periodo vacío, zona horaria, paginación y límites.

## 6. ENT-04 - Promociones con cuotas comerciales

- [ ] Definir límites por plan para cantidad de promociones activas.
- [ ] Definir redenciones por ciclo y por código.
- [ ] Definir si las promociones pueden apilarse.
- [ ] Crear entitlements `MaxActivePromotions` y `PromotionRedemptionsPerCycle`.
- [ ] Validar cuota antes de crear lote promocional.
- [ ] Validar cuota antes de activar una promoción.
- [ ] Hacer redención idempotente.
- [ ] Aplicar tenant, ownership y fecha de vigencia.
- [ ] Auditar creación, activación, desactivación y redención.
- [ ] Añadir estados frontend de cuota y reinicio.

## 7. ENT-05 - Pruebas HTTP del cobro consolidado

- [ ] Preparar gateway fake inyectable para `IPaymentGatewayService`.
- [ ] Preparar perfil de tarjeta predeterminado de prueba.
- [ ] Crear suscripción con add-on priced.
- [ ] Ejecutar ciclo recurrente controlado.
- [ ] Verificar una sola llamada al gateway.
- [ ] Verificar monto consolidado suscripción + add-ons.
- [ ] Verificar `GrossAmountCrc`, `ProrationCreditCrc` y `AmountCrc`.
- [ ] Verificar transacción `Succeeded`.
- [ ] Verificar renovación de suscripción y add-on.
- [ ] Simular gateway rechazado y verificar transacción `Failed`.
- [ ] Verificar idempotencia/reintento del ciclo.
- [ ] Verificar factura y recibo cuando aplique.

## 8. ENT-06 - Migración de límites hardcodeados

- [ ] Auditar `const`, `Take`, límites mensuales/anuales y comparaciones de tier.
- [ ] Clasificar cada constante como técnica, seguridad o comercial.
- [ ] Migrar toda constante comercial a `PlanEntitlement`.
- [ ] Eliminar fallback legacy tras aplicar migraciones en todos los entornos.
- [ ] Validar que frontend y backend usen la misma clave.
- [ ] Añadir prueba que detecte nuevas claves comerciales no registradas.
- [ ] Actualizar `FEATURES.md` y catálogo público.

## 9. ENT-07 - Add-ons comerciales

- [x] Persistir `PriceCrc` por add-on.
- [x] Persistir bruto, crédito y neto en `PaymentTransaction`.
- [x] Renovar vigencia de add-ons en cobro recurrente.
- [x] Sustituir add-on con cálculo de crédito prorrateado.
- [x] Cobrar saldo de sustitución mediante gateway.
- [ ] Aplicar IVA comercial de add-on donde corresponda.
- [ ] Persistir detalle de add-ons incluidos en la transacción.
- [ ] Implementar reembolso/crédito cuando el nuevo precio sea menor.
- [ ] Añadir prorrateo para cambios de ciclo mensual/anual.
- [ ] Añadir renovación automática configurable por add-on.
- [ ] Añadir pruebas HTTP y de gateway.

## 10. ENT-08 - UI de entitlements

- [x] Exponer snapshot autenticado.
- [x] Hook frontend de entitlements.
- [x] Medidor con loading/error/no incluido/disponible/agotado.
- [ ] Integrar medidores en dashboard de mascotas.
- [ ] Integrar medidores en clínica.
- [ ] Integrar medidores en tienda.
- [ ] Integrar medidores en proveedor.
- [ ] Integrar medidores en municipalidad.
- [ ] Mostrar `resetsAt` con zona horaria correcta.
- [ ] Mostrar CTA de upgrade contextual.
- [ ] Manejar Problem Details `PLAN_LIMIT_REACHED` de forma uniforme.

## 11. ENT-09 - Auditoría comercial

- [x] Auditar autorización/revocación de dominios widget.
- [x] Auditar creación/desactivación de add-ons.
- [x] Auditar downgrades.
- [x] Auditar activación administrativa.
- [x] Auditar activación por referencia de pago.
- [ ] Auditar renovación recurrente exitosa.
- [ ] Auditar renovación recurrente fallida.
- [ ] Auditar reemplazo de add-on y crédito.
- [ ] Auditar reembolsos y créditos.
- [ ] Añadir consulta administrativa por entidad, actor y periodo.
- [ ] Añadir retención y protección de datos de auditoría.

## 12. ENT-10 - Suite completa y bloqueo eVista2024

- [ ] Identificar proceso externo que bloquea DLLs.
- [ ] Detenerlo de forma segura o ejecutar build en entorno aislado.
- [ ] Ejecutar `dotnet build PawTrack.sln` serial.
- [ ] Ejecutar `dotnet test PawTrack.sln` serial.
- [x] Ejecutar `npm run typecheck`.
- [x] Ejecutar `npm test -- --run`.
- [x] Ejecutar suite unitaria backend: 1.519/1.519.
- [x] Ejecutar suite frontend: 104/104 tests en 33/33 archivos.
- [ ] Ejecutar integración HTTP de pagos.
- [ ] Registrar conteos y fallos reproducibles.
- [ ] No marcar verde por una ejecución parcial.

## 13. Definition of Done enterprise

Una tarea solo se marca `[x]` cuando cumple todo lo siguiente:

- [ ] Regla comercial definida y aprobada.
- [ ] Entitlement persistido y sembrado.
- [ ] Backend valida antes de mutar.
- [ ] Consumo idempotente si aplica.
- [ ] Ownership, tenant, rol y estado operativo validados.
- [ ] Respuesta Problem Details consistente.
- [ ] Telemetría sin PII innecesaria.
- [ ] Auditoría de acción comercial.
- [ ] Prueba unitaria.
- [ ] Prueba de integración o HTTP cuando corresponda.
- [ ] Frontend muestra estado correcto.
- [ ] Documentación comercial y operativa actualizada.
- [ ] Migración aplicada y verificada.
- [ ] Suite afectada en verde.

## 14. Registro de ejecución

| Fecha      | ID       | Cambio                                                                 | Pruebas ejecutadas        | Resultado                 | Responsable  |
| ---------- | -------- | ---------------------------------------------------------------------- | ------------------------- | ------------------------- | ------------ |
| 2026-09-21 | Baseline | Sistema de entitlements, cuotas, add-ons, gates y downgrades parciales | Builds y suites enfocadas | Parcialmente verde        | Coding Agent |
| 2026-09-21 | ENT-05   | Cobro consolidado suscripción + add-ons                                | Unitarias financieras     | Verde parcial; falta HTTP | Coding Agent |

## 15. Dependencias y bloqueos

- La suite completa puede bloquearse si el proceso externo `eVista2024` mantiene DLLs en uso.
- Importación masiva requiere decisión de Producto sobre formato, columnas, duplicados y límites.
- Promociones requieren límites comerciales aprobados antes de codificar cuotas.
- Modo lectura requiere confirmar por módulo qué recursos deben quedar visibles, pausados o inactivos.
- Analítica avanzada requiere definición de métricas, retención y roles autorizados.
