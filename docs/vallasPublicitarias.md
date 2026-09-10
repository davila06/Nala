# PawTrack CR - Vallas Publicitarias: Pendientes Enterprise

> Estado: 2026-09-10
> Base local validada: `PawTrackDev` en `(localdb)\MSSQLLocalDB`; las migraciones de vallas y dependencias se aplicaron correctamente.
> Alcance: campañas in-app, inventario, entrega, métricas, operación administrativa y monetización.

## Estado implementado

La plataforma ya cuenta con:

- 15 placements: Map, Dashboard, Directory, Feed, perfil QR, historial de escaneos, Case Room, directorios y perfiles de clínicas/proveedores, adopciones, ferias, registro y CollarTag.
- CRUD administrativo: crear, editar, subir imagen, enviar a revisión, aprobar/rechazar, activar, pausar y eliminar.
- Campañas con anunciante, categoría, cantón objetivo, contrato, presupuesto, frecuencia diaria, exclusividad y servicio VIP.
- Revisión por segundo administrador, con motivo de rechazo y auditoría.
- Restricción de Feed y Case Room a categorías de recuperación.
- Rotación de campañas activas por placement, prioridad VIP mínima de 90 y límite de cinco campañas candidatas.
- Eventos de impresión, clic y conversión con deduplicación, rate limit por IP+campaña y contador diario por visitante pseudonimizado.
- Métricas de impresiones, clics, CTR, conversiones, cantón y placement.
- CTA de WhatsApp en los popups del mapa cuando clínica, tienda o proveedor configura un número móvil CR y da consentimiento explícito.
- Renderizado de banners en los 15 placements; el dashboard y el directorio de servicios tienen campañas demo activas en el seed local.
- En `/map`, las vallas flotantes se muestran en una pila separada del panel de leyenda para evitar solapamientos; los cierres solo aplican a la vista montada actual.
- Las imágenes de campaña se sirven desde orígenes compatibles con la CSP; las URLs externas no permitidas se omiten en el cliente para evitar errores de consola.

## Pendientes priorizados

### P0 - Garantías comerciales de inventario

#### Enforce de presupuesto

**Estado:** configurado, no aplicado.

El presupuesto se guarda y se muestra, pero una campaña no se pausa al alcanzar un importe contratado. Definir modelo de compra: tarifa fija, CPM, CPC o CPA. Solo CPM/CPC/CPA requieren consumo automático.

**Implementación necesaria:**

- Agregar tipo de cobro, precio unitario, unidades contratadas y unidades consumidas a la campaña.
- Incrementar consumo de manera transaccional e idempotente al registrar evento facturable.
- Excluir la campaña de entrega al alcanzar el límite y cambiarla a `PausedBudgetExhausted`.
- Notificar al administrador y anunciante cuando llegue a 80%, 95% y 100%.
- Mantener un ledger inmutable de cargos y ajustes manuales.

**Criterios de aceptación:** ninguna impresión/clic facturable puede exceder unidades contratadas, aun con solicitudes concurrentes.

#### Exclusividad absoluta de categoría

**Estado:** el checkbox existe; no bloquea solapamientos.

**Implementación necesaria:**

- Al crear, editar, aprobar o activar, comprobar conflicto por `placement + categoría + cantón + intervalo`.
- Tratar `cantón nulo` como cobertura nacional, que se solapa con todos los cantones.
- Resolver concurrencia con índice/rango protegido o lock distribuido por clave comercial.
- Registrar decisión, conflicto y operador en auditoría.

**Criterios de aceptación:** no es posible activar dos campañas exclusivas que se solapen comercialmente.

#### Frequency cap efectivo en selección

**Estado:** el backend rechaza la impresión adicional; el cliente puede seguir intentando mostrar la campaña.

**Implementación necesaria:**

- Entregar solo campañas elegibles para el visitante mediante token anónimo firmado o endpoint de decisión de entrega.
- Aplicar el contador antes de seleccionar la creatividad, no después de pintar.
- Rotar únicamente entre campañas elegibles.

**Criterios de aceptación:** una persona no ve una campaña más veces que su límite diario y no genera requests fallidos repetitivos.

### P1 - Renovación y reporte comercial

#### Reporte detallado y exportable

**Estado:** API y panel muestran resumen, sin exportación.

**Implementación necesaria:**

- Selector de fechas en Admin y en futuro portal del anunciante.
- Tablas y gráficas de impresiones, clics, CTR, conversiones, cantones y placements.
- Exportación CSV; PDF solo cuando se apruebe plantilla fiscal/comercial.
- Snapshot inmutable mensual para que reportes históricos no cambien por correcciones posteriores.
- Auditoría de cada descarga y URLs temporales de Blob para los archivos.

**Criterios de aceptación:** un reporte mensual puede descargarse, reproducirse y conciliarse con el ledger de eventos.

#### Alertas de renovación

**Estado:** aviso visual de vencimiento en Admin.

**Implementación necesaria:**

- Job diario con lock distribuido.
- Avisos a 14, 7, 3 y 1 día de vencimiento.
- Email/WhatsApp solo con consentimiento del anunciante.
- Flujo de renovación que clone campaña, creativo y términos, pero requiera nueva aprobación si cambia el contenido.

### P1 - Segmentación y ranking patrocinado

#### Segmentación geográfica real

**Estado:** se guarda `TargetCanton`, pero no filtra entrega.

**Implementación necesaria:**

- Resolver cantón desde coordenadas de mapa o perfil, en servidor; no aceptar el cantón del cliente como fuente de verdad.
- Definir fallback nacional y conducta cuando el cantón es desconocido.
- Aplicar segmentación tanto a banners como a resultados destacados.

#### Ranking patrocinado de directorios

**Estado:** clínicas/proveedores tienen `IsFeatured`; no hay compra patrocinada geolocalizada ni disclosure comercial.

**Implementación necesaria:**

- Entidad separada de patrocinio de resultados para no mezclarla con vallas.
- Orden SQL: patrocinado elegible por radio/cantón, luego verificado, luego relevancia orgánica.
- Etiqueta accesible `Patrocinado` y explicación de criterios.
- Límite de resultados patrocinados por página para proteger calidad del directorio.

### P2 - Portal de anunciante y facturación

#### Organización anunciante y autogestión

**Estado:** `OwnerId` de la campaña es el administrador creador, no una organización anunciante.

**Implementación necesaria:**

- Crear bounded context/entidad `AdvertiserOrganization` con propietarios, miembros y roles.
- Asociar campañas y contratos a organización, no a Admin.
- Políticas de autorización: anunciante solo accede a sus campañas, creativos, facturas y reportes.
- Portal separado con creación de borrador, carga de creativo, seguimiento de revisión y reportes.
- El administrador conserva revisión, activación, ajustes de facturación y suspensión.

#### Facturación y conciliación

**Estado:** contrato/referencia se almacenan como texto; no hay factura ni pago.

**Implementación necesaria:**

- Cotización versionada, factura/recibo, impuestos y moneda.
- Estado de pago y conciliación SINPE/tarjeta idempotente.
- No activar campaña pagada sin estado de pago válido, salvo crédito/promoción autorizado.
- Notas de crédito, reembolsos, expiración y prorrateo documentados.

### P2 - Seguridad, privacidad y calidad

#### Antifraude avanzado

**Estado:** rate limit, IP hash, idempotencia y frecuencia diaria implementados.

**Pendiente:**

- Señales de fraude: user-agent, ASN/datacenter, patrones de clic, ráfagas, tasas anómalas y referer.
- Cola de revisión para eventos sospechosos, sin excluir tráfico legítimo automáticamente.
- Separar eventos observados de eventos facturables y conservar evidencia de la decisión.

#### Retención y consentimiento de métricas

**Estado:** eventos de vallas no tienen política de purga dedicada.

**Implementación necesaria:**

- Definir período de retención y anonimización agregada.
- Extender `PersonalDataRetentionJob` para eventos de entrega de vallas.
- Documentar la base legal, cookies/localStorage y opción de privacidad aplicable.

#### Creativos y moderación

**Estado:** revisión humana, validación de MIME/tamaño y redimensionado a JPEG al subir el creativo; no hay workflow completo.

**Pendiente:**

- Validar MIME real, dimensiones, re-encoding y escaneo de malware.
- Motivos normalizados de rechazo, historial de versiones de creativo y SLA de revisión.
- Preview móvil y escritorio contra cada placement.
- Política de contenido vinculada desde el formulario de campaña.

## Datos demo locales

El seed `backend/scripts/seed-enterprise-demo-data.sql` crea cuatro campañas
activas para probar `Map`, `Feed`, `Dashboard` y
`ServiceProviderDirectory`. Las campañas de dashboard y servicios usan CTA
interno y no dependen de imágenes externas.

Para probar el inventario de adopciones se puede ejecutar:

```powershell
sqllocaldb start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev -E -f 65001 -i backend/scripts/seed-adoption-demo-data.sql
```

Ese seed es idempotente y crea cuatro animales disponibles y una feria próxima
para el refugio verificado `ally@test.cr` / `Refugio Central CR`.

## Operación local

Base usada por desarrollo y pruebas locales:

```powershell
sqllocaldb start MSSQLLocalDB
Set-Location backend
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/PawTrack.Infrastructure/PawTrack.Infrastructure.csproj --startup-project src/PawTrack.API/PawTrack.API.csproj
```

La cadena de conexión se define en `backend/src/PawTrack.API/appsettings.Development.json` y apunta a `PawTrackDev`.

## Próxima entrega recomendada

Implementar exclusividad absoluta por categoría/cantón/placement/período. Es el siguiente entregable con mayor valor comercial porque permite vender un inventario escaso con una garantía verificable, sin requerir aún un portal de anunciante ni integración de pagos.
