# PawTrack CR - Decisiones de Producto y Negocio Pendientes

> **Fecha:** 2026-09-22  
> **Objetivo:** convertir los pendientes enterprise en decisiones aprobables.  
> **Cómo usarlo:** para cada decisión, elegir una opción, registrar responsable y fecha. Una opción marcada como recomendada es un punto de partida técnico, no una decisión ya tomada.

## 1. Reglas de aprobación

- Una decisión comercial se implementa sólo después de tener una opción seleccionada.
- Los límites aprobados se guardan como entitlements persistidos; no como constantes en frontend o backend.
- El backend mantiene la autoridad para cuota, tenant, ownership, rol y estado operativo.
- Un downgrade conserva los datos; sólo modifica publicación, escritura o disponibilidad.
- Las reglas de seguridad, privacidad y recuperación de mascotas perdidas no pueden convertirse en beneficios premium.

| Estado       | Significado                                          |
| ------------ | ---------------------------------------------------- |
| Pendiente    | Debe elegirse una opción.                            |
| Aprobada     | Producto autorizó la opción y fecha efectiva.        |
| Implementada | Código, pruebas, UI y documentación están completos. |

## 2. Registro de decisiones

| ID         | Tema                                  | Responsable            | Estado    | Opción aprobada | Fecha efectiva |
| ---------- | ------------------------------------- | ---------------------- | --------- | --------------- | -------------- |
| DEC-IMP-01 | Recursos importables                  | Producto               | Pendiente |                 |                |
| DEC-IMP-02 | Formato y codificación                | Producto + Operaciones | Pendiente |                 |                |
| DEC-IMP-03 | Duplicados y actualización            | Producto               | Pendiente |                 |                |
| DEC-IMP-04 | Límites, retención y reintentos       | Producto + Operaciones | Pendiente |                 |                |
| DEC-DWN-01 | Política común de downgrade           | Producto               | Pendiente |                 |                |
| DEC-DWN-02 | Clínica y capacidades Partner         | Producto + Clínica     | Pendiente |                 |                |
| DEC-DWN-03 | Municipalidad y datos institucionales | Producto + Legal       | Pendiente |                 |                |
| DEC-ANL-01 | Métricas de proveedores               | Producto + Datos       | Pendiente |                 |                |
| DEC-ANL-02 | Exportación y retención analítica     | Producto + Legal       | Pendiente |                 |                |
| DEC-PRO-01 | Propiedad de promociones              | Producto + Comercial   | Pendiente |                 |                |
| DEC-PRO-02 | Cuotas y apilamiento de promociones   | Producto + Finanzas    | Pendiente |                 |                |
| DEC-BIL-01 | IVA y detalle de add-ons              | Finanzas + Producto    | Pendiente |                 |                |
| DEC-BIL-02 | Crédito o reembolso                   | Finanzas + Soporte     | Pendiente |                 |                |
| DEC-UI-01  | UX de cuota agotada y upgrade         | Producto + Diseño      | Pendiente |                 |                |
| DEC-AUD-01 | Retención de auditoría                | Legal + Seguridad      | Pendiente |                 |                |
| DEC-OPS-01 | Estrategia de validación y CI         | Ingeniería + DevOps    | Pendiente |                 |                |

## 2.1 Recomendaciones propuestas para aprobar

Estas son las opciones recomendadas, consolidadas por decisión. Producto puede aprobarlas tal cual o reemplazarlas por otra opción documentada en la sección correspondiente.

| ID | Recomendación concreta | Decisión que desbloquea |
| --- | --- | --- |
| DEC-IMP-01 | **A. Catálogos B2B:** importar solamente productos de tienda y servicios de proveedor en lanzamiento. | Alcance de importación. |
| DEC-IMP-02 | **B. CSV UTF-8 + JSON:** CSV para operación manual, JSON para integraciones; columnas desconocidas generan error por fila. | Contrato de archivos y validación. |
| DEC-IMP-03 | **C. Actualizar campos permitidos:** usar SKU/código interno como clave natural, con auditoría por fila. | Tratamiento de duplicados. |
| DEC-IMP-04 | **Híbrida:** hasta 5 MB, 1.000 filas StorePartner por archivo, procesamiento síncrono pequeño/asíncrono grande, retención cifrada de 30 días y errores CSV por 90 días. | Límites, operación y almacenamiento. |
| DEC-DWN-01 | **A para catálogos; C para historial:** desactivar productos, servicios y sedes excedentes; dejar historial y evidencias en lectura. Reactivar sólo recursos `PlanRestricted`. | Regla transversal de downgrade. |
| DEC-DWN-02 | **Credenciales sensibles requieren acción:** revocar API keys, conservar sólo dominios incluidos, mantener certificados verificables y exigir regeneración de credenciales tras upgrade. | ClinicPartner -> ClinicPlus. |
| DEC-DWN-03 | **Histórico agregado en lectura:** bloquear nuevas operaciones fuera de entitlement, conservar un cantón permitido y aplicar supresión estadística con umbral de 10 registros. | Downgrade municipal y privacidad. |
| DEC-ANL-01 | **Analítica por sesión-día:** vistas únicas por sesión-día; clics de contacto/reserva; conversión reserva creada/vista; ingreso total de reservas completadas; ocupación por tiempo reservado. | Modelo de eventos y fórmulas. |
| DEC-ANL-02 | **Verified 90 días / Featured 24 meses:** CSV sólo para Featured, máximo 20 exportaciones por ciclo, sin PII y granularidad diaria hasta 90 días. | Retención, exportación y privacidad. |
| DEC-PRO-01 | **A. Promociones centrales en MVP:** sólo Administración PawTrack crea campañas. No aplicar `MaxActivePromotions` por plan hasta vender campañas autogestionadas. | Modelo de propiedad y tenant. |
| DEC-PRO-02 | **Sin apilamiento gratuito:** un uso por usuario/código, fecha absoluta obligatoria y un único descuento porcentual aplicable por compra. | Redención, vigencia y stacking. |
| DEC-BIL-01 | **Precio B2C con IVA incluido:** desglosar IVA y una línea por add-on en factura; renovación por opt-in; tres reintentos con período de gracia de 7 días. | Facturación y cobro recurrente. |
| DEC-BIL-02 | **A. Crédito para siguiente cobro:** no reembolso inmediato; saldo visible, auditado y con fecha de vencimiento. Cambios mensual/anual sólo al vencimiento. | Menor precio y cambios de ciclo. |
| DEC-UI-01 | **Experiencia preventiva y reactiva:** aviso al 80% y al agotarse; mensaje específico por entitlement; B2C abre modal de planes y B2B dirige a planes/contacto comercial; mostrar fechas en hora de Costa Rica. | UI de cuotas y upgrade. |
| DEC-AUD-01 | **B. Retención de 5 años:** acceso para Admin/SuperAdmin, redacción de PII en detalles y exportación sólo con auditoría adicional. | Conservación y acceso de auditoría. |
| DEC-OPS-01 | **C. Validación híbrida:** build y pruebas focalizadas locales; solución completa, E2E y seguridad obligatorias en CI limpio; salida aislada autorizada para DLL bloqueadas. | Política de calidad y CI. |

---

## 3. Importación masiva

### DEC-IMP-01 - Recursos importables

**Pregunta:** ¿qué recursos se pueden crear o actualizar en una importación?

| Opción                   | Alcance                                       | Ventajas                                    | Coste/riesgo                                                    |
| ------------------------ | --------------------------------------------- | ------------------------------------------- | --------------------------------------------------------------- |
| A. Catálogos B2B         | Productos de tienda y servicios de proveedor. | Ya existe base técnica; bajo riesgo de PII. | No cubre municipios.                                            |
| B. Catálogos + municipal | A más: capturas o casos municipales.          | Reduce carga operativa institucional.       | Requiere reglas de PII, deduplicación y permisos más estrictos. |
| C. Genérica por módulo   | Cada módulo incorpora su propio adaptador.    | Escalable a futuro.                         | Requiere contratos y soporte por módulo.                        |

**Recomendación:** A para lanzamiento; habilitar B sólo después de la definición de datos municipales.

### DEC-IMP-02 - Formato, codificación y esquema

**Pregunta:** ¿qué recibe el endpoint y cómo se valida?

| Opción                   | Regla                                                                          |
| ------------------------ | ------------------------------------------------------------------------------ |
| A. CSV UTF-8 obligatorio | Encabezados definidos por recurso; más fácil de operar desde hojas de cálculo. |
| B. CSV UTF-8 y JSON      | CSV para carga manual, JSON para integraciones.                                |
| C. JSON exclusivo        | Contrato estricto para integraciones; menos accesible para operación manual.   |

Definir además, por recurso:

- Encabezados obligatorios, opcionales y prohibidos.
- Tipos, longitudes, moneda, zona horaria y catálogos permitidos.
- Si columnas desconocidas son error total, error por fila o se ignoran.
- Si una fila parcialmente válida se crea o se rechaza completa.

**Recomendación:** B con CSV UTF-8 como flujo operativo y JSON para API; columnas desconocidas deben ser error por fila.

### DEC-IMP-03 - Duplicados

**Pregunta:** ¿qué ocurre cuando una fila coincide con un recurso existente?

| Opción                          | Comportamiento                           | Consecuencia                                                     |
| ------------------------------- | ---------------------------------------- | ---------------------------------------------------------------- |
| A. Rechazar                     | Toda fila duplicada queda en error.      | Máxima previsibilidad; más trabajo manual.                       |
| B. Omitir                       | Se marca como omitida y no cambia datos. | Idónea para reintentos; puede ocultar datos desactualizados.     |
| C. Actualizar campos permitidos | Se actualiza por clave natural.          | Útil operativamente; exige auditoría y lista de campos mutables. |
| D. Upsert                       | Crea si no existe, actualiza si existe.  | Mejor experiencia; mayor riesgo de sobrescritura.                |

Definir la clave natural por recurso, por ejemplo SKU de tienda y código interno de servicio. Nunca usar nombre como clave única.

**Recomendación:** C para catálogos, con campos permitidos explícitos y auditoría por fila; no implementar D sin control de concurrencia/versionado.

### DEC-IMP-04 - Cuotas, ejecución y retención

Decidir cada fila de la tabla:

| Aspecto            | Opciones                                                       | Recomendación inicial                                                                |
| ------------------ | -------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Tamaño por archivo | 5 MB, 10 MB, 25 MB, por plan                                   | Mantener 5 MB hasta usar streaming/blob.                                             |
| Filas por archivo  | 1.000, 10.000, 50.000, por plan                                | 1.000 StorePartner es contractual en FEATURES; 10.000 es sólo tope técnico temporal. |
| Filas por ciclo    | Sin límite, límite mensual, límite anual                       | Límite mensual por entitlement si tiene coste operativo.                             |
| Ejecución          | Síncrona, asíncrona siempre, híbrida por tamaño                | Híbrida: pequeña síncrona, grande asíncrona.                                         |
| Reintento          | No permitir, reintentar sólo fallidos, reintentar job completo | Reintentar sólo filas fallidas con nueva clave idempotente.                          |
| Cancelación        | Inmediata, al terminar lote, no disponible                     | Al terminar lote para no dejar transacciones ambiguas.                               |
| Retención archivo  | No guardar, 7, 30, 90 días                                     | 30 días cifrado; reporte de errores 90 días.                                         |
| Reporte de errores | JSON, CSV, ambos                                               | CSV descargable sin PII innecesaria.                                                 |

---

## 4. Downgrade y modo lectura

### DEC-DWN-01 - Política común

**Pregunta:** al superar el límite del nuevo plan, ¿qué pasa con el recurso excedente?

| Opción          | Publicación          | Edición          | Lectura | Reversión en upgrade                    |
| --------------- | -------------------- | ---------------- | ------- | --------------------------------------- |
| A. Inactivo     | No público           | No               | Sí      | Automática si fue restringido por plan. |
| B. Borrador     | No público           | Sí, sin publicar | Sí      | Automática/manual al publicar.          |
| C. Sólo lectura | Puede seguir público | No               | Sí      | Automática.                             |

**Recomendación:** A para productos, servicios y sedes excedentes; C para historial, certificados y datos regulatorios. Nunca reactivar recursos desactivados manualmente.

Confirmar también:

- Orden de prioridad: principal, más reciente, más antiguo, mayor venta o selección manual.
- Si se avisa antes del downgrade y con cuántos días de anticipación.
- Si el administrador puede elegir qué recursos mantener activos antes del vencimiento.

### DEC-DWN-02 - Clínicas

| Decisión                              | Opción A                    | Opción B                                     | Recomendación                                                                 |
| ------------------------------------- | --------------------------- | -------------------------------------------- | ----------------------------------------------------------------------------- |
| ClinicPartner -> ClinicPlus, API keys | Revocar definitivamente     | Desactivar y permitir regenerar tras upgrade | Desactivar/revocar por seguridad; generar una nueva clave tras upgrade.       |
| Widgets/dominios                      | Desactivar todos            | Conservar un dominio incluido                | Conservar sólo los incluidos por el plan destino.                             |
| Certificados/pasaportes emitidos      | Lectura pública verificable | Ocultarlos                                   | Mantener verificables; bloquear nuevas emisiones no incluidas.                |
| Función tras upgrade                  | Reactivar automáticamente   | Requiere acción de clínica                   | Requiere acción para credenciales; automática sólo para estados no sensibles. |

### DEC-DWN-03 - Municipalidades

| Capacidad                       | RedRegional -> Full           | Full -> Basica     | Recomendación                                       |
| ------------------------------- | ----------------------------- | ------------------ | --------------------------------------------------- |
| Dashboard regional              | Desactivar vistas regionales  | No aplica          | Sólo lectura de históricos agregados.               |
| Cantones                        | Conservar selección permitida | Mantener un cantón | Mantener primer cantón o permitir selección previa. |
| Transferencias intermunicipales | Bloquear creación             | Bloquear creación  | Conservar historial y trazabilidad.                 |
| Cargas masivas/API/reportes     | Según entitlement             | Según entitlement  | Bloquear nuevas operaciones, no lectura autorizada. |
| Datos sensibles                 | Agregados                     | Agregados          | Aplicar supresión por umbral acordado.              |

Definir umbral de supresión de reportes agregados: 5, 10 o 20 registros mínimos por segmento.

---

## 5. Analítica avanzada de proveedores

### DEC-ANL-01 - Métricas y definiciones

La primera versión actual ya calcula reservas, completadas, canceladas, ingreso bruto, ticket promedio y servicios publicados. Falta decidir las métricas avanzadas:

| Métrica     | Definición a aprobar           | Opciones                                                                  |
| ----------- | ------------------------------ | ------------------------------------------------------------------------- |
| Vista       | Visita de perfil o de servicio | Todas las cargas / única por usuario-día / única por sesión-día.          |
| Clic        | Acción medible                 | Contacto / WhatsApp / reservar / sitio externo / todas.                   |
| Conversión  | Numerador y denominador        | Reserva creada ÷ vistas / reserva confirmada ÷ clics.                     |
| Cancelación | Estados incluidos              | Cliente / proveedor / ambas / excluir no-show.                            |
| Ingreso     | Base monetaria                 | Reserva completada, subtotal, total con impuesto, neto de reembolsos.     |
| Ocupación   | Fórmula                        | Tiempo reservado ÷ horario disponible / cupos vendidos ÷ cupos ofertados. |

**Recomendación:** vista única por sesión-día, clic de contacto/reserva, conversión de reserva creada sobre vista, ingreso total de reservas completadas y ocupación por tiempo reservado sobre disponibilidad publicada.

### DEC-ANL-02 - Retención, exportación y privacidad

| Aspecto            | Opciones                       | Recomendación inicial                               |
| ------------------ | ------------------------------ | --------------------------------------------------- |
| Retención Verified | 30, 90, 180 días               | 90 días.                                            |
| Retención Featured | 12, 24, 36 meses               | 24 meses (730 días ya implementados).               |
| Exportación CSV    | No, Featured, todos            | Sólo Featured.                                      |
| Límite exportación | 1, 5, 20 por ciclo             | 20 por ciclo, por cuenta.                           |
| Granularidad       | Día, semana, mes               | Día para 90 días; semana/mes para periodos mayores. |
| PII                | Incluir, excluir, seudonimizar | Excluir PII; sólo métricas agregadas.               |

---

## 6. Promociones y cuotas

### DEC-PRO-01 - Propiedad de promociones

El código actual es administrado globalmente. `MaxActivePromotions` no puede aplicarse correctamente hasta decidir quién es dueño de una promoción.

| Opción                  | Dueño                       | Uso                              | Impacto                                                             |
| ----------------------- | --------------------------- | -------------------------------- | ------------------------------------------------------------------- |
| A. Central              | Administración PawTrack     | Adquisición y campañas globales. | No necesita cuota por plan de tienda/proveedor.                     |
| B. Comercial por tenant | Tienda, proveedor o clínica | Promociones propias.             | Requiere `TenantId`, ownership y catálogo de descuentos aplicables. |
| C. Híbrida              | Admin y tenant              | Ambos modelos.                   | Mayor valor; mayor complejidad y reglas de precedencia.             |

**Recomendación:** A en MVP. Elegir B/C sólo si el producto vende promociones autogestionadas.

### DEC-PRO-02 - Límites, redención y apilamiento

| Decisión      | Opciones                                                  | Recomendación inicial                                                      |
| ------------- | --------------------------------------------------------- | -------------------------------------------------------------------------- |
| Máximo activo | 0, 1, 5, 20, ilimitado controlado                         | No aplicar a modelo central; para tenant: 1 Basic, 5 Plus, 20 Partner.     |
| Redenciones   | Por código, por cuenta, por ciclo o combinación           | Por código + una por usuario/código + cuota mensual de usuario si aplica.  |
| Apilamiento   | Nunca, sólo descuento, sólo créditos, reglas de prioridad | Nunca para beneficios gratuitos; un único descuento porcentual por compra. |
| Vigencia      | Fecha absoluta, ventana desde emisión, ambas              | Fecha absoluta obligatoria para campañas.                                  |
| Reembolso     | Revocar beneficio, mantener beneficio, crédito futuro     | Revocar sólo beneficios no consumidos; conservar auditoría.                |

Definir si las promociones pueden aplicarse a: suscripciones, add-ons, productos de tienda, reservas de proveedor o más de una categoría.

---

## 7. Add-ons, facturación y crédito

### DEC-BIL-01 - IVA, factura y renovación

| Decisión         | Opciones                                   | Recomendación inicial                                   |
| ---------------- | ------------------------------------------ | ------------------------------------------------------- |
| Precio mostrado  | Sin IVA, IVA incluido, ambos               | IVA incluido para B2C; desglose en factura.             |
| Tasa de IVA      | Configurable, fija por país, por categoría | Configurable por jurisdicción/categoría; no hardcodear. |
| Línea de factura | Agrupada, una por add-on, una por ciclo    | Una línea por add-on con periodo y cantidad.            |
| Renovación       | Siempre automática, opt-in, opt-out        | Opt-in explícito al adquirir.                           |
| Fallo de cobro   | Reintentos, gracia, desactivar inmediato   | 3 reintentos y gracia definida antes de desactivar.     |

Definir número de días de gracia: 0, 3, 7 o 14.

### DEC-BIL-02 - Cambio a add-on más barato

| Opción                          | Efecto                       | Ventaja           | Riesgo                                      |
| ------------------------------- | ---------------------------- | ----------------- | ------------------------------------------- |
| A. Crédito para siguiente cobro | No hay devolución inmediata. | Operación simple. | Usuario puede percibir poca transparencia.  |
| B. Reembolso al medio original  | Devuelve diferencia.         | Transparente.     | Coste operativo, comisiones y conciliación. |
| C. Cambio al siguiente ciclo    | No calcula crédito.          | Muy simple.       | Menos flexible.                             |

**Recomendación:** A para MVP, con saldo visible, vencimiento y auditoría; B sólo si Finanzas aprueba proceso de reembolso.

Definir además si se permite cambiar mensual <-> anual a mitad de ciclo o sólo al vencimiento.

---

## 8. Experiencia de cuotas y upgrade

### DEC-UI-01 - `PLAN_LIMIT_REACHED`

| Aspecto      | Opciones                                           | Recomendación inicial                                         |
| ------------ | -------------------------------------------------- | ------------------------------------------------------------- |
| Mensaje      | Genérico / específico por entitlement              | Específico: capacidad agotada + reinicio o límite.            |
| CTA          | Abrir modal / ir a planes / contactar ventas       | B2C: modal de planes. B2B: planes o contacto comercial.       |
| Momento      | Sólo al fallar / preventivo al 80% / ambos         | Ambos.                                                        |
| Medidores    | Todos los planes / sólo consumibles / sólo pagados | Cuotas relevantes y consumibles; no mostrar claves técnicas.  |
| Zona horaria | UTC / Costa Rica / perfil del usuario              | Costa Rica para producto local, conservando UTC internamente. |

Definir el mapa `entitlement -> texto, CTA, destino` para cada módulo. Ejemplos: `MaxPets`, `AiMatchesPerCycle`, `MaxActiveProducts`, `MaxOrdersPerCycle`, `BulkImportLimit`.

---

## 9. Auditoría, retención y soporte

### DEC-AUD-01 - Retención de auditoría comercial

| Opción    | Retención                                        | Aplicación                                             |
| --------- | ------------------------------------------------ | ------------------------------------------------------ |
| A. 2 años | Menor coste.                                     | Operación básica.                                      |
| B. 5 años | Equilibrio para soporte, facturación y disputas. | Recomendación inicial.                                 |
| C. 7 años | Mayor cobertura fiscal/contractual.              | Requiere confirmación legal y coste de almacenamiento. |

Definir también:

- Quién puede consultar auditoría: Admin, SuperAdmin, Soporte, Finanzas.
- Qué campos son visibles por rol.
- Si `Details` puede contener datos personales y cómo se minimiza/redacta.
- Si exportar auditoría está permitido y bajo qué registro adicional.

---

## 10. Operación, pruebas y salida a producción

### DEC-OPS-01 - Fuente de verdad de validación

| Opción               | Regla                                                            | Recomendación                           |
| -------------------- | ---------------------------------------------------------------- | --------------------------------------- |
| A. Local obligatorio | Todo debe pasar en cada estación.                                | Poco realista con bloqueos de procesos. |
| B. CI obligatorio    | CI limpio es la aprobación final; local usa suites focalizadas.  | Recomendada.                            |
| C. Híbrido           | Build local + focalizadas obligatorias; solución completa en CI. | Recomendada si se formaliza.            |

Decidir además:

- Si los builds/tests deben usar salida aislada cuando una DLL está bloqueada.
- Tiempo máximo aceptable de una suite completa.
- Suites bloqueantes por pull request: unitarias, integración, frontend, E2E y seguridad.
- Dueño del proceso externo que retiene DLLs y procedimiento para detenerlo.

---

## 11. Paquete mínimo para continuar implementación

Para destrabar la siguiente ola de trabajo, aprobar primero:

1. `DEC-IMP-01` a `DEC-IMP-04`.
2. `DEC-DWN-01` a `DEC-DWN-03`.
3. `DEC-PRO-01` y `DEC-PRO-02`.
4. `DEC-BIL-01` y `DEC-BIL-02`.
5. `DEC-UI-01`.

Después de cada aprobación, Ingeniería debe actualizar el catálogo de entitlements, pruebas, frontend, documentación de soporte y el checklist enterprise antes de considerar la capacidad terminada.
