# Sprint 5 — Reportes Institucionales y NALA Core Enterprise

> Alcance: construir la capa de reportes institucionales, exports regulatorios y
> NALA Core sobre PawTrack CR. El objetivo es **SENASA-ready**, no integración
> oficial con SENASA. Los exports son reportes compatibles y auditables; no son
> certificados oficiales ni comunicaciones oficiales hasta existir convenio,
> canal aprobado y revisión legal.
> Fecha: 2026-09-07.

---

## 1. Objetivo del sprint

Convertir los datos operativos de PawTrack/NALA en información institucional
confiable, agregada, trazable y exportable para municipalidades, aliados,
refugios, clínicas, administración y futuras conversaciones con SENASA.

Resultado esperado:

- reportes por cantón, especie, periodo, estado y organización;
- indicadores de pérdidas, reunificaciones, capturas, adopciones, certificados,
  casos de bienestar y tiempos de respuesta;
- exports CSV, JSON y PDF con esquema versionado, hash y Blob privado;
- NALA Core con dashboard nacional/regional y mapas operativos por capas;
- APIs institucionales separadas de las APIs públicas;
- suppression y agregación para evitar reidentificación;
- auditoría de generación, descarga, regeneración y envío de reportes;
- base preparada para un futuro `IRegulatorySubmissionGateway` sin activar
  ningún envío oficial;
- controles de permisos, rate limiting, límites de filas, timeouts y protección
  contra exports costosos;
- pruebas unitarias, integración, frontend, rendimiento y seguridad.

## 1.1 Estado verificado de implementación

Implementado en código:

- módulo `Regulatory` con `RegulatoryExport`, `ReportDefinition` y
  `RegulatorySubmission` persistidos;
- autorización central `IReportAuthorizationService` con protección básica de
  cantón, scope y ownership/BOLA;
- preview con filtros tipados de cantón, especie, estado y organización;
- reportes agregados de recuperación, capturas, adopciones, bienestar,
  identidad sanitaria, cobertura y NALA;
- exports CSV/JSON/PDF con hash SHA-256 y Blob privado;
- jobs con lock distribuido, expiración y purga;
- submissions con payload privado, `NoOp`, cancelación y reintento limitado;
- cache distribuido de overview/mapas y ETag para overview NALA;
- dashboard NALA, tendencias y portal institucional inicial;
- tests unitarios Regulatory, pruebas de autorización e integración de endpoints.

Pendiente para cierre operativo: aprobación legal del catálogo, alertas y
dashboards de Azure, benchmarks de carga, E2E institucional, pruebas completas
de seguridad, staging/rollback y piloto.

---

## 2. Fuera de alcance

- [ ] Integración oficial con SENASA.
- [ ] Envío automático de reportes a autoridades.
- [ ] Firma digital jurídica o sello oficial.
- [ ] Sustituir los dashboards operativos existentes de municipalidades o clínicas.
- [ ] Publicar mapas con ubicaciones exactas de propietarios, reportantes,
      animales vulnerables o evidencia sensible.
- [ ] Ranking público de organizaciones con datos no normalizados o sin contexto.
- [ ] Modelos predictivos de riesgo o decisiones automatizadas sobre personas.
- [ ] Reescritura de los módulos `LostPets`, `Municipalities`, `Adoptions`,
      `Certificates` o `AnimalWelfare`.

---

## 3. Dependencias y estado base

### 3.1 Módulos que alimentan los reportes

- `LostPetEvent`: pérdidas, estados y reunificaciones.
- `Sighting` / `FoundPetReport`: avistamientos y animales encontrados.
- `CapturedAnimal`: capturas municipales y estados operativos.
- `AdoptablePet` / `AdoptionApplication`: adopciones y solicitudes.
- `AnimalWelfareCase`: casos, severidad, estado, derivaciones y cierres.
- `Pet`: especie, identidad sanitaria, microchip y cantón aproximado.
- `VetCertificate` / `VaccinePassport`: certificados emitidos, vigencia y revocación.
- `Clinic`, `ClinicVerification`, `ClinicVeterinarian`: red profesional verificada.
- `MunicipalityProfile`: cantón, nivel y organización municipal.
- `AuditLog` y auditorías específicas de módulos.
- Outbox para eventos de dominio cuando el cambio se origine en una operación
  transaccional.

### 3.2 Brechas que Sprint 5 debe cerrar

- [x] Existe modelo común de consultas regulatorias agregadas.
- [x] Existe contrato versionado de export institucional.
- [x] Existe registro de export con hash, Blob, actor y estado.
- [x] Existe separación formal entre export público, institucional y admin.
- [x] Existe suppression por umbral mínimo de observaciones.
- [x] Existe dashboard NALA multicapa inicial.
- [x] Existe API `/api/nala/*` y `/api/institutional/*`.
- [x] Existe política base de costo, límites de rango y cancelación/reintento de submissions.
- [x] Existe retención específica para paquetes de exportación.
- [x] Existe gateway `NoOp` preparado para futuras entregas externas.

---

## 4. Checklist ejecutivo

- [x] Definir catálogo técnico inicial de métricas y fórmulas; aprobación institucional queda pendiente.
- [x] Definir calendario, zona horaria y límites de periodos consultables.
- [x] Crear módulo `Regulatory` en Domain/Application/Infrastructure (primer corte).
- [x] Crear `ReportDefinition` y versiones de esquema.
- [x] Crear `RegulatoryExport` con hash y estado.
- [x] Crear consultas SQL agregadas, acotadas y `AsNoTracking` para los reportes soportados.
- [x] Crear exportadores CSV, JSON y PDF.
- [x] Crear Blob privado `regulatory-exports`.
- [x] Crear endpoints institucionales y NALA (catálogo, solicitud, estado y overview).
- [x] Crear endpoints públicos con datos agregados y suppression (impact stats).
- [x] Crear dashboard NALA inicial con overview, filtros de periodo y capas agregadas.
- [x] Crear tendencias, resumen por cantón y desempeño institucional agregado.
- [x] Añadir feature flags de despliegue para Regulatory y NALA.
- [x] Crear mapa multicapa inicial con celdas generalizadas, bounds y suppression.
- [x] Crear filtros por periodo, cantón, especie, estado y organización en contratos y preview; falta ampliar cobertura de cada reporte.
- [x] Implementar idempotencia y trazabilidad de estado de exports.
- [x] Implementar auditoría append-only de solicitud, generación, fallo y descarga.
- [x] Implementar retención y purga de exports expirados.
- [x] Preparar `IRegulatorySubmissionGateway` con implementación `NoOp` sin envío externo.
- [x] Añadir feature flags de despliegue para Regulatory y NALA.
- [x] Agregar rate limits, permisos básicos, row limits y protección contra abuso en endpoints principales.
- [x] Completar tests unitarios focalizados, integración de autorización, build frontend y E2E institucional inicial; seguridad y rendimiento de carga quedan pendientes.
- [x] Actualizar runbook, privacidad, términos y manuales institucionales base; aprobación legal y manuales de roles adicionales quedan pendientes.

---

## 5. Producto, gobierno y definiciones

### 5.1 Decisiones institucionales

- [ ] Confirmar que todo el sprint se comercializa como `SENASA-ready`.
- [ ] Aprobar glosario de lenguaje permitido:
  - `reporte institucional`;
  - `export compatible`;
  - `indicador agregado`;
  - `expediente verificable`;
  - `preparado para futura interoperabilidad`.
- [ ] Prohibir en UI, PDF, nombres de archivo y metadata:
  - `certificado oficial SENASA`;
  - `aprobado por SENASA`;
  - `enviado a SENASA`;
  - `válido ante SENASA`;
  - cualquier sello o logo institucional no autorizado.
- [ ] Definir responsables de aprobación para cada tipo de reporte.
- [ ] Definir periodicidad: diario, semanal, mensual y bajo demanda.
- [ ] Definir SLA interno de generación por tamaño de reporte.
- [ ] Definir qué organizaciones pueden consultar datos propios, regionales o
      nacionales.
- [ ] Definir proceso de corrección cuando un dato fuente sea rectificado.
- [ ] Definir política para reportes cancelados, incompletos o regenerados.
- [ ] Definir contacto operativo y responsable de incidentes de reportes.

### 5.2 Catálogo mínimo de reportes

- [ ] Recuperación de mascotas:
  - pérdidas reportadas;
  - pérdidas activas;
  - reunificaciones;
  - tiempo mediano hasta reunificación;
  - distancia de recuperación cuando exista;
  - tasa de recuperación por cantón y periodo.
- [ ] Capturas municipales:
  - capturas por especie y cantón;
  - estados de captura;
  - dueño localizado;
  - transferencias;
  - liberaciones y adopciones;
  - tiempo en custodia cuando esté disponible.
- [ ] Adopciones:
  - animales publicados;
  - solicitudes recibidas;
  - solicitudes aprobadas;
  - adopciones completadas;
  - tiempo mediano hasta adopción;
  - seguimiento post-adopción si existe.
- [ ] Bienestar animal:
  - casos recibidos;
  - severidad;
  - estados;
  - derivaciones;
  - tiempo hasta triage;
  - tiempo hasta asignación;
  - tiempo hasta cierre;
  - resultados agregados.
- [ ] Salud e identidad sanitaria:
  - mascotas con microchip declarado/verificado;
  - certificados emitidos, vigentes y revocados;
  - pasaportes por especie;
  - esterilización agregada;
  - nunca exponer datos clínicos o del propietario en reportes públicos.
- [ ] Red operativa:
  - clínicas verificadas activas;
  - veterinarios autorizados;
  - municipalidades activas;
  - refugios y aliados activos;
  - cobertura por cantón;
  - tiempos de atención por organización.

### 5.3 Definiciones y calidad de datos

- [ ] Documentar fórmula, fuente, filtros y unidad de cada métrica.
- [ ] Diferenciar conteo de eventos, animales, mascotas, casos y usuarios.
- [ ] Definir si los periodos usan `CreatedAt`, `ReportedAt`, `ResolvedAt` u otra
      fecha de negocio.
- [ ] Usar zona horaria Costa Rica para agrupaciones de negocio.
- [ ] Normalizar cantones, provincias, especies y estados antes de agregar.
- [ ] Definir cómo tratar registros sin cantón, especie o fecha válida.
- [ ] Definir deduplicación de casos vinculados a la misma captura o mascota.
- [ ] Definir snapshot de datos al generar un export para reproducibilidad.
- [ ] Documentar diferencias entre tiempo calendario y tiempo hábil.
- [ ] Añadir versión de fórmula al resultado y al archivo exportado.

---

## 6. Domain — Regulatory

### 6.1 `RegulatoryExport`

- [x] Crear entidad `RegulatoryExport` con:
  - `Id` Guid v7;
  - `ExportCode` público no sensible;
  - `ReportType`;
  - `SchemaVersion`;
  - `ScopeType` (`Public`, `Institutional`, `Admin`, `Nala`);
  - `RequestedByUserId`;
  - `OrganizationId?`;
  - `Canton?`;
  - `PeriodStart`;
  - `PeriodEnd`;
  - `TimeZone`;
  - `Format` (`Csv`, `Json`, `Pdf`);
  - `Status` (`Requested`, `Running`, `Completed`, `Failed`, `Expired`);
  - `RowCount?`;
  - `SuppressedRowCount?`;
  - `PayloadSha256?`;
  - `BlobUrl?` privado;
  - `ErrorCode?` sin secretos;
  - `RequestedAt`;
  - `CompletedAt?`;
  - `ExpiresAt?`;
  - `DownloadedAt?`.
- [ ] Crear métodos de dominio:
  - `Request(...)`;
  - `Start(...)`;
  - `Complete(...)`;
  - `Fail(...)`;
  - `Expire(...)`;
  - `RecordDownload(...)`.
- [ ] Impedir completar dos veces el mismo export.
- [ ] Impedir descargar exports fallidos, expirados o sin Blob.
- [ ] Guardar solamente metadata y no payload completo en SQL.
- [ ] Aplicar idempotency key por actor, definición, rango y filtros.
- [ ] Crear índices por actor, organización, estado, tipo y periodo.

### 6.2 Definiciones, filtros y suppression

- [ ] Crear `ReportDefinition` o catálogo equivalente con:
  - código estable;
  - nombre interno;
  - descripción;
  - versión;
  - scope permitido;
  - roles permitidos;
  - columnas autorizadas;
  - retención;
  - threshold de suppression;
  - fórmula o referencia documental.
- [ ] Crear `ReportFilter` fuertemente tipado, no JSON libre sin validación.
- [ ] Definir threshold mínimo configurable, por ejemplo `k >= 5`, para
      métricas públicas.
- [ ] Suprimir grupos bajo threshold con `Suppressed` y no con cero.
- [ ] Evitar que combinaciones de filtros permitan reconstruir grupos suprimidos.
- [ ] Redondear o agrupar coordenadas en mapas públicos.
- [ ] Quitar IDs, emails, teléfonos, direcciones y URLs privadas de exports públicos.
- [ ] Prohibir datos de salud, evidencia y reportantes en exports públicos.
- [ ] Definir reglas especiales para cantones con pocos registros.
- [ ] Tests de suppression, redacción y ataques de diferencia entre consultas.

### 6.3 Registro de envíos futuros

- [x] Crear `RegulatorySubmission` solo como registro de preparación/envío:
  - `Id`;
  - `ExportId`;
  - `Destination`;
  - `SubmissionType`;
  - `IdempotencyKey`;
  - `PayloadSha256`;
  - `Status`;
  - `SubmittedAt?`;
  - `AcknowledgedAt?`;
  - `ExternalReference?`;
  - `ErrorCode?`;
  - `RetryCount`.
- [ ] No activar destinos oficiales en Sprint 5.
- [ ] Crear estados `Prepared`, `Queued`, `Submitted`, `Acknowledged`,
      `Rejected`, `Failed`, `Cancelled`.
- [ ] Crear garantía de no duplicar un envío con la misma idempotency key.
- [ ] No almacenar tokens, credenciales o respuestas sensibles en texto plano.

---

## 7. Application — consultas y servicios

### 7.1 Interfaces

- [ ] `IRegulatoryReportRepository`.
- [ ] `IRegulatoryExportRepository`.
- [ ] `IRegulatorySubmissionRepository`.
- [ ] `IRegulatoryReportQueryService`.
- [ ] `IRegulatoryExportRenderer`.
- [ ] `IRegulatoryExportStorage` o reutilizar `IBlobStorageService` con política
      específica de contenedor.
- [ ] `IRegulatorySubmissionGateway`.
- [x] `IReportAuthorizationService`.
- [ ] `IReportSuppressionService`.
- [ ] `IReportClock` si se requiere determinismo para periodos y tests.

### 7.2 Consultas agregadas

- [ ] Implementar una consulta por reporte, no una consulta por cada fila.
- [ ] Ejecutar agregaciones en SQL; prohibido traer tablas completas a memoria.
- [ ] Aplicar `AsNoTracking()` en todas las lecturas.
- [ ] Aplicar filtros de periodo y scope antes de agrupar.
- [ ] Usar proyecciones DTO, no entidades completas.
- [ ] Usar índices y revisar planes de ejecución para reportes críticos.
- [ ] Limitar cardinalidad y tamaño máximo de respuesta.
- [ ] Evitar N+1 al incorporar nombres de organizaciones o cantones.
- [ ] Validar que reportes de bienestar no unan evidencia ni notas privadas.
- [ ] Crear consultas para:
  - pérdidas y reunificaciones;
  - capturas municipales;
  - adopciones;
  - casos de bienestar;
  - certificados y microchips;
  - cobertura de red;
  - tiempos SLA;
  - métricas combinadas para NALA.

### 7.3 Commands

- [ ] `RequestRegulatoryExportCommand`.
  - valida scope, rol, periodo y formato;
  - valida límites de rango y filas;
  - crea registro idempotente;
  - devuelve código y estado, no el payload completo.
- [ ] `GenerateRegulatoryExportCommand`.
  - se ejecuta como job controlado;
  - toma snapshot lógico de parámetros;
  - genera CSV/JSON/PDF;
  - calcula SHA-256;
  - sube a Blob privado;
  - marca `Completed` o `Failed`.
- [ ] `RecordRegulatoryExportDownloadCommand`.
- [ ] `ExpireRegulatoryExportCommand`.
- [ ] `PrepareRegulatorySubmissionCommand`.
- [ ] `CancelRegulatorySubmissionCommand`.
- [ ] Añadir validadores FluentValidation para todos los commands.
- [ ] Usar `Result<T>` y Problem Details; no retornar excepciones internas.

### 7.4 Queries

- [ ] `GetReportCatalogQuery`.
- [x] `GetReportPreviewQuery` con límites estrictos y datos no sensibles.
- [ ] `GetRegulatoryExportStatusQuery`.
- [ ] `GetRegulatoryExportDownloadQuery` con autorización y auditoría.
- [ ] `GetInstitutionalDashboardQuery`.
- [ ] `GetNalaOverviewQuery`.
- [x] `GetNalaTrendsQuery`.
- [ ] `GetNalaMapLayersQuery`.
- [ ] `GetNalaInstitutionPerformanceQuery`.
- [ ] `GetPublicImpactStatsQuery` con suppression obligatorio.
- [ ] `GetRegionalCantonSummaryQuery`.
- [ ] Todas las listas con paginación y máximo de página.

---

## 8. Infraestructura y persistencia

### 8.1 EF Core

- [ ] Agregar DbSets para `RegulatoryExport`, `ReportDefinition` y
      `RegulatorySubmission` si se persisten como entidades.
- [ ] Crear configuraciones EF separadas.
- [ ] Configurar enums como strings versionables o enteros documentados.
- [ ] Configurar longitudes, nullability y conversiones explícitas.
- [ ] Crear índices por:
  - `Status, RequestedAt`;
  - `RequestedByUserId, RequestedAt`;
  - `OrganizationId, PeriodStart, PeriodEnd`;
  - `ReportType, SchemaVersion`;
  - `PayloadSha256`;
  - `IdempotencyKey` único cuando aplique.
- [ ] Agregar filtros para no incluir exports expirados en consultas operativas.
- [ ] Generar migración `AddRegulatoryExportsAndNala`.
- [ ] Revisar migración contra el snapshot actual.
- [ ] Probar migración en base vacía y base con datos existentes.
- [ ] No editar migraciones ya aplicadas en ambientes compartidos.

### 8.2 Blob Storage

- [ ] Crear contenedor privado `regulatory-exports`.
- [ ] Bloquear acceso anónimo y listing público.
- [ ] Convención de ruta:
  - `{scope}/{reportType}/{yyyy}/{MM}/{exportId}/{filename}`.
- [ ] Aplicar nombres sanitizados y extensión permitida.
- [ ] Configurar content type y metadata:
  - `report-type`;
  - `schema-version`;
  - `payload-sha256`;
  - `period-start`;
  - `period-end`;
  - `scope`.
- [ ] Usar descarga autenticada o URL SAS corta y auditada si el patrón existente
      la requiere.
- [ ] No persistir SAS en SQL ni logs.
- [ ] Verificar integridad descargando y comparando SHA-256.
- [ ] Crear job de borrado de blobs expirados.

### 8.3 Jobs y escala

- [ ] Crear `RegulatoryExportJob` separado de la request HTTP.
- [ ] Crear `RegulatoryExportHostedService` con `IDistributedJobLock`.
- [ ] Limitar concurrencia global de generación.
- [ ] Configurar timeout y cancelación por export.
- [ ] Reintentar solo errores transitorios.
- [ ] No reintentar errores de validación ni autorización.
- [ ] Registrar backoff, número de intento y causa resumida.
- [ ] Evitar duplicados con idempotencia y lock distribuido.
- [ ] Medir cola, edad del export más antiguo y duración por formato.
- [ ] Crear job de retención de exports y submissions expirados.

### 8.4 Renderers

- [ ] CSV:
  - encoding UTF-8 con BOM si usuarios institucionales lo requieren;
  - escape correcto de comas, saltos y fórmulas de hoja de cálculo;
  - columnas versionadas;
  - no permitir CSV injection con valores que comiencen por `=`, `+`, `-` o `@`.
- [ ] JSON:
  - schema version;
  - metadata del reporte;
  - filtros;
  - periodo y timezone;
  - métricas y suppression metadata;
  - hash calculado sobre payload canónico.
- [ ] PDF:
  - encabezado SENASA-ready sin afirmar aprobación oficial;
  - periodo, alcance, filtros y versión;
  - totales y notas metodológicas;
  - leyenda de datos suprimidos;
  - fecha de generación y código de export;
  - no incluir PII ni evidencia sensible.
- [ ] Tests golden/snapshot para CSV, JSON y PDF.
- [ ] Verificar que todos los formatos representen los mismos valores.

---

## 9. API institucional y NALA

### 9.1 Separación de rutas

- [ ] `/api/institutional/reports/catalog`.
- [ ] `/api/institutional/reports/preview`.
- [ ] `/api/institutional/exports`.
- [ ] `/api/institutional/exports/{id}`.
- [ ] `/api/institutional/exports/{id}/download`.
- [ ] `/api/institutional/exports/{id}/regulatory-submission` solo para preparación
      autorizada y sin destino oficial activo.
- [ ] `/api/nala/overview`.
- [ ] `/api/nala/trends`.
- [ ] `/api/nala/map-layers`.
- [ ] `/api/nala/institutions`.
- [ ] `/api/nala/cantons`.
- [ ] `/api/public/impact-stats`.

### 9.2 Autorización

- [ ] `Admin`: acceso nacional y configuración.
- [ ] `Nala`: acceso nacional y regional según permisos explícitos.
- [ ] `Municipality`: solo su cantón y exports autorizados.
- [ ] `Ally` / `Shelter` / `Clinic`: solo datos propios y casos asignados.
- [ ] `Institutional` futuro: permiso explícito y scope aprobado.
- [ ] Validar organización, cantón y scope en servidor; nunca confiar en querystring.
- [ ] Registrar denegaciones de acceso sin incluir datos sensibles.
- [ ] Evitar BOLA entre IDs de exports de distintos actores.
- [ ] Agregar policy handlers o servicio central de autorización.

### 9.3 Protecciones HTTP

- [ ] Rate limit `regulatory-preview`.
- [ ] Rate limit `regulatory-export`.
- [ ] Rate limit `regulatory-download`.
- [ ] Rate limit `nala-dashboard`.
- [ ] Límite de rango máximo por tipo de reporte.
- [ ] Límite de filas, bytes y duración de generación.
- [ ] Request size limit en filtros JSON.
- [ ] Rechazar rangos invertidos y fechas futuras no permitidas.
- [ ] No exponer stack traces, SQL, Blob URL ni errores del proveedor.
- [ ] Agregar correlation ID y export ID a respuestas y logs seguros.

---

## 10. NALA Core — dashboard y mapas

### 10.1 Overview nacional/regional

- [x] Crear tarjetas de indicadores iniciales para pérdidas, reunificaciones,
      capturas, adopciones, bienestar, microchips y certificados.
- [ ] Crear tarjetas adicionales de indicadores:
  - pérdidas activas;
  - tasa de reunificación;
  - capturas;
  - adopciones;
  - casos de bienestar abiertos;
  - tiempo mediano de triage;
  - tiempo mediano de cierre;
  - certificados vigentes;
  - cobertura de clínicas y municipalidades.
- [ ] Comparar periodo actual contra periodo anterior sin datos personales.
- [ ] Mostrar fecha de actualización y frescura de datos.
- [ ] Mostrar `suppressed` cuando no haya base estadística suficiente.
- [ ] Evitar rankings sin normalización por población o volumen operativo.
- [ ] Permitir filtros por provincia, cantón, especie y rango temporal.
- [ ] Mantener filtros en URL solo si no contienen datos sensibles.

### 10.2 Mapa operacional multicapa

- [ ] Capa de pérdidas activas con ubicación generalizada.
- [ ] Capa de capturas municipales agregadas.
- [ ] Capa de casos de bienestar por cuadrícula o cantón.
- [ ] Capa de clínicas verificadas.
- [ ] Capa de refugios/aliados activos.
- [ ] No dibujar domicilio exacto, GPS de collar, reportante ni evidencia.
- [ ] Aplicar clustering, geohash truncado o grid cells.
- [ ] Aplicar threshold de puntos antes de mostrar una celda.
- [ ] Limitar bounds, zoom y tamaño de respuesta.
- [ ] Evitar inferencia por consultas consecutivas de bounds pequeños.
- [ ] Tests de redacción geográfica y autorización de capas.

### 10.3 Rendimiento y caching

- [ ] Crear cache distribuido para métricas agregadas de lectura frecuente.
- [ ] Versionar keys por fórmula, periodo y filtros.
- [ ] Invalidar cache al cambiar datos relevantes o aceptar TTL explícito.
- [ ] No cachear respuestas institucionales con PII.
- [ ] Configurar `ETag`/`Last-Modified` para dashboards públicos cuando aplique.
- [ ] Medir p50/p95 de consultas y endpoints de mapa.
- [ ] Añadir límites para evitar consultas de un año completo en cada request.

---

## 11. Frontend

### 11.1 Administración de exports

- [ ] Crear feature `regulatory` o `institutional` co-localizada.
- [ ] Crear catálogo de reportes filtrado por rol.
- [x] Crear formulario de periodo, cantón y formato.
- [ ] Mostrar preview agregado antes de solicitar export.
- [ ] Mostrar advertencia de suppression y alcance de datos.
- [ ] Mostrar estado de job: solicitado, generando, listo, fallido, expirado.
- [x] Permitir descarga solo cuando el export esté completo.
- [ ] Mostrar hash y versión de esquema al usuario autorizado.
- [ ] Mostrar errores accionables sin detalles internos.
- [ ] Mostrar historial de exports propios con paginación.

### 11.2 Dashboard NALA

- [x] Crear ruta protegida `/nala`.
- [x] Añadir guard de roles y estado de inicialización auth.
- [x] Diseñar overview con filtros de periodo y fecha de actualización.
- [ ] Crear gráficas de tendencia accesibles con tabla alternativa.
- [x] Crear vista de capas agregadas con estados de carga/error y suppression.
- [ ] Crear vista de cantones y detalle regional.
- [ ] Crear vista de instituciones y SLA sin exponer PII.
- [ ] Crear estado vacío para datos suprimidos o insuficientes.
- [ ] No usar color como único indicador.
- [ ] Verificar responsive desktop/tablet y navegación por teclado.

### 11.3 Portal municipal/institucional

- [ ] Añadir acceso a reportes autorizados desde el portal existente.
- [ ] Mostrar solo el cantón/organización permitido.
- [ ] Permitir generar reporte mensual de capturas y bienestar propio.
- [ ] Permitir descargar export completado.
- [ ] Mostrar auditoría básica de solicitudes propias.
- [ ] Evitar duplicar estadísticas ya existentes; reutilizar contratos comunes.

### 11.4 Frontend testing

- [ ] Tests de permisos de catálogo.
- [ ] Tests de filtros y validación de rango.
- [ ] Tests de suppression visible.
- [ ] Tests de estados del job.
- [ ] Tests de descarga y errores 403/404/410.
- [x] Tests de mapa sin puntos sensibles.
- [ ] Tests responsive y accesibilidad de controles principales.
- [ ] E2E institucional con datos de prueba agregados y roles separados.

---

## 12. Seguridad, privacidad y cumplimiento

- [ ] Threat model de exports, dashboard y mapas.
- [ ] Revisar BOLA para export IDs, report IDs y capas NALA.
- [ ] Aplicar mínimo privilegio por scope y organización.
- [ ] No incluir PII en logs, métricas, nombres de Blob o nombres de archivo.
- [ ] No incluir coordenadas exactas en métricas públicas.
- [ ] Revisar suppression contra ataques de diferencia:
  - filtros por fecha contigua;
  - cantones vecinos;
  - especies raras;
  - estado antes/después;
  - organización individual.
- [ ] Proteger CSV contra formula injection.
- [ ] Validar content types y tamaño de exports generados.
- [ ] Escanear archivos generados si el pipeline lo requiere.
- [ ] Auditar cada preview, solicitud, generación, descarga y expiración.
- [ ] Auditar preparación de submission aunque el gateway sea `NoOp`.
- [ ] Configurar retención diferenciada para exports públicos, institucionales y
      admin.
- [ ] Documentar base legal, finalidad y destinatarios de cada export.
- [ ] Actualizar `docs/CUMPLIMIENTO_PROTECCION_DATOS.md`.
- [ ] Actualizar `docs/POLITICA_DE_PRIVACIDAD.md`.
- [ ] Actualizar `docs/TERMINOS_DE_USO.md`.

---

## 13. Observabilidad y operaciones

### 13.1 Métricas Application Insights

- [x] `RegulatoryExport.Requested`.
- [x] `RegulatoryExport.Started`.
- [x] `RegulatoryExport.Completed`.
- [x] `RegulatoryExport.Failed`.
- [x] `RegulatoryExport.Downloaded`.
- [ ] `RegulatoryExport.Expired`.
- [ ] `RegulatorySubmission.Prepared`.
- [ ] `NalaDashboard.Query`.
- [ ] `NalaDashboard.CacheHit` / `CacheMiss`.
- [ ] `ReportSuppression.Applied`.
- [x] Row count, byte count y formato como métricas no sensibles.
- [x] Nunca enviar PII, IDs de propietario, emails, teléfonos ni contenido libre.

### 13.2 Alertas

- [ ] Tasa de exports fallidos sobre umbral.
- [ ] Cola de exports atascada o envejecida.
- [ ] Duración p95 por encima del SLA.
- [ ] Errores de Blob Storage.
- [ ] Hash mismatch durante descarga/verificación.
- [ ] Aumento anómalo de previews o solicitudes por actor/IP.
- [ ] Cache de NALA con errores o datos obsoletos.
- [ ] Fallos de migración o job de retención.

### 13.3 Runbook

- [x] Crear `docs/RUNBOOK_REPORTES_INSTITUCIONALES.md`.
- [x] Documentar cómo inspeccionar un export por `ExportCode`.
- [x] Documentar expiración y purga.
- [x] Documentar el comportamiento `NoOp` de integración externa.
- [ ] Documentar regeneración segura, cancelación de jobs largos, Blob faltante,
      hash mismatch y escalamiento detallado.

---

## 14. Pruebas y calidad

### 14.1 Dominio y Application

- [ ] Crea export con parámetros válidos.
- [ ] Rechaza periodo invertido, futuro o demasiado amplio.
- [x] Idempotencia devuelve el export existente.
- [ ] No permite completar dos veces.
- [ ] No permite descargar export expirado.
- [ ] Hash es estable para el mismo payload canónico.
- [ ] Suppression aplica threshold y metadata correcta.
- [ ] Scope municipal no puede leer otro cantón.
- [ ] Clinic/ally/shelter solo ve datos propios o asignados.
- [ ] No se incluyen PII, notas, evidencia ni coordenadas exactas.
- [ ] CSV injection queda neutralizada.

### 14.2 Infraestructura

- [ ] Repositorios agregan en SQL y no en memoria.
- [ ] Queries usan `AsNoTracking`.
- [ ] No existen N+1 en reportes combinados.
- [ ] Índices se usan en periodos y scopes principales.
- [ ] Job distribuido no corre dos veces en escala horizontal.
- [ ] Reintentos no duplican export ni submission.
- [ ] Purga elimina solamente exports expirados.
- [ ] Migración funciona en base nueva y existente.

### 14.3 API/integración

- [ ] 401 para requests sin autenticación cuando corresponde.
- [ ] 403 para rol o scope insuficiente.
- [ ] 404/410 sin filtrar existencia sensible cuando corresponda.
- [ ] Rate limiting retorna respuesta consistente.
- [ ] Problem Details no contiene stack trace ni SQL.
- [ ] Preview no permite by-pass de suppression.
- [ ] Download registra auditoría.
- [ ] Contratos JSON tienen schema version.
- [ ] Se verifica content disposition y content type.

### 14.4 Performance y carga

- [ ] Dataset sintético con al menos un año de eventos.
- [ ] Benchmark de reportes pequeños, medianos y grandes.
- [ ] Pruebas p50/p95 de dashboard y mapas.
- [ ] Prueba de concurrencia de exports.
- [ ] Prueba de rate limit y backpressure.
- [ ] Prueba de cancelación de export largo.
- [ ] Prueba de recuperación después de caída de Blob.
- [ ] Revisar consumo de memoria del renderer PDF.

### 14.5 Frontend/E2E

- [ ] Catálogo cambia según rol.
- [ ] Filtros validan fechas y scopes.
- [ ] Job muestra progreso y estados finales.
- [ ] Descarga funciona únicamente con export listo.
- [ ] Dashboard maneja loading, error, vacío y suppression.
- [ ] Mapa nunca muestra datos exactos en scope público.
- [ ] Navegación por teclado y labels accesibles.
- [ ] E2E con admin, municipalidad y usuario sin permisos.

---

## 15. Documentación y operación institucional

- [ ] Actualizar `docs/senasa.md` con enlace a este sprint.
- [x] Crear manual de reportes institucionales.
- [x] Crear manual de dashboard NALA.
- [x] Crear catálogo de métricas versionado inicial.
- [x] Crear guía de scopes y suppression.
- [x] Crear guía municipal de export mensual.
- [x] Crear guía de auditoría y hash de exports en el runbook.
- [x] Crear guía de privacidad para datos agregados en política y manuales.
- [ ] Actualizar manual admin con permisos y retención.
- [ ] Actualizar manual municipal con scope por cantón.
- [ ] Actualizar términos y política de privacidad.
- [x] Documentar que ningún export implica integración oficial con SENASA.

---

## 16. Rollout enterprise

- [ ] Activar primero en ambiente local con datos sintéticos.
- [ ] Ejecutar migración en staging y validar rollback documentado.
- [x] Crear feature flags para Regulatory y NALA por entorno; granularidad por tipo/rol queda para Azure App Configuration.
- [x] Activar preview mediante feature flag antes de ampliar exports.
- [ ] Activar exports para Admin/NALA interno.
- [ ] Activar exports municipales piloto con uno o dos cantones.
- [ ] Revisar métricas, costos y tiempos de respuesta.
- [ ] Activar dashboard público solo con datos agregados y suppression validada.
- [ ] Ejecutar revisión de privacidad antes de ampliar scopes.
- [ ] Ejecutar revisión de seguridad antes de producción.
- [ ] Crear plan de rollback de código, migración, jobs y feature flags.
- [ ] Confirmar backup y restauración de metadata y Blobs.
- [ ] Realizar postmortem de piloto y ajustar catálogo de métricas.

---

## 17. Criterios de aceptación enterprise

Sprint 5 se considera completo cuando:

- [ ] existe catálogo versionado de reportes y métricas;
- [ ] los reportes principales se calculan en SQL con filtros y paginación;
- [ ] los exports CSV/JSON/PDF son reproducibles, tienen hash y Blob privado;
- [ ] la descarga requiere autorización, registra auditoría y respeta expiración;
- [ ] los scopes institucionales impiden acceso entre organizaciones/cantones;
- [ ] los datos públicos aplican suppression y redacción verificadas;
- [ ] NALA tiene overview, tendencias, cantones, instituciones y mapa agregado;
- [ ] la API institucional está separada de la API pública;
- [ ] existe preparación de submissions con `NoOp` o gateway manual, sin afirmar
      integración oficial;
- [ ] los jobs tienen lock distribuido, reintentos seguros y métricas;
- [ ] existe retención y purga de exports y submissions;
- [ ] tests unitarios, integración, frontend y performance están documentados;
- [ ] migración EF fue probada en base nueva y existente;
- [ ] privacidad, términos, manuales y runbook están actualizados;
- [ ] el piloto fue aprobado por producto, seguridad y operación.

---

## 18. Orden recomendado de ejecución

1. [ ] Cerrar catálogo de métricas, scopes, suppression y lenguaje legal.
2. [ ] Crear dominio, contratos y migración de exports.
3. [x] Implementar consultas agregadas de pérdidas, capturas, adopciones, bienestar, identidad sanitaria y cobertura de red.
4. [x] Implementar export CSV/JSON y almacenamiento privado con hash.
5. [x] Implementar export PDF y pruebas de formato.
6. [x] Implementar jobs, locks, reintentos, retención; observabilidad avanzada queda pendiente.
7. [x] Exponer API institucional y permisos por scope básico.
8. [x] Crear NALA overview y capas agregadas; tendencias y detalle por cantón quedan pendientes.
9. [x] Crear mapa multicapa con agregación y suppression.
10. [x] Crear portal frontend de reportes y dashboard NALA inicial.
11. [x] Preparar `RegulatorySubmission` y gateway `NoOp` sin envío externo.
12. [ ] Ejecutar pruebas de seguridad, performance y E2E.
13. [ ] Completar documentación institucional, piloto y checklist de go-live.

---

## 19. Próximo sprint

Sprint 6 queda reservado para el adaptador de interoperabilidad externa cuando
exista convenio, canal técnico aprobado, requisitos de autenticación, formato
aceptado y autorización legal. Hasta entonces, Sprint 5 debe producir exports
compatibles, auditables y privados sin enviar datos oficialmente a SENASA.
