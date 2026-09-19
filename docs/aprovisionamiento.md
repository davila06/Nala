# PawTrack CR - Opciones de aprovisionamiento en Azure

**Fecha:** 2026-09-19  
**Alcance:** produccion inicial con aproximadamente 100 usuarios activos mensuales  
**Fuente de verdad:** `infra/main.bicep`, `infra/parameters.prod.bicepparam` y el codigo actual de la solucion.

## Resumen ejecutivo

PawTrack usa una API .NET 9 con procesos programados, Azure SQL, Blob Storage,
Key Vault, Application Insights, Log Analytics, Azure Static Web Apps y Azure
Container Apps. Las opciones siguientes mantienen esa familia tecnologica y
priorizan no introducir una migracion de motor de base de datos.

Los precios son aproximados en USD por mes para la region East US, sin impuestos,
sin compromisos de reserva y sin incluir dominios, soporte de Azure, SendGrid,
WhatsApp/Meta, Telegram, Facebook, TrackSolid, Azure Maps, Computer Vision,
Redis ni Azure SignalR salvo cuando se indique expresamente. El consumo de
imagenes, logs y egreso puede cambiar el total.

> El costo real depende especialmente de si Azure SQL Serverless permanece
> pausado. Los procesos programados de la API pueden mantener la base activa.

## Componentes comunes

Todos los perfiles incluyen:

- Frontend en Azure Static Web Apps.
- Backend en Azure Container Apps; no App Service.
- Azure Container Registry Basic para la imagen Docker.
- Azure Storage Standard LRS para fotos, documentos y archivos.
- Azure Key Vault Standard con identidad administrada.
- Azure Monitor, Application Insights y Log Analytics con retencion controlada.
- Azure SQL Server y una base `pawtrack` compatible con EF Core y SQL Server.
- HTTPS, health checks, migraciones EF Core y alertas basicas.

No se debe habilitar scale-out de la API sin Redis y Azure SignalR. El codigo usa
estado distribuido para rate limiting y coordinacion, y SignalR necesita un
backplane al trabajar con varias replicas.

## Version 1 - Minima / costo mas bajo

### Objetivo de la version minima

Validar el producto con trafico bajo, manteniendo la aplicacion disponible sin
pagar componentes de escala que todavia no son necesarios.

### Configuracion minima

| Componente      | Configuracion                                                                                |
| --------------- | -------------------------------------------------------------------------------------------- |
| Container Apps  | 0.5 vCPU, 1 GiB, 1 replica minima, 1 replica maxima                                          |
| Azure SQL       | Basic o S0; preferir S0 si se usan jobs y consultas geograficas con frecuencia               |
| Static Web Apps | Free si el dominio, staging y limites son suficientes                                        |
| Storage         | Standard LRS, lifecycle para fotos temporales                                                |
| Key Vault       | Standard                                                                                     |
| ACR             | Basic                                                                                        |
| Logs            | Log Analytics con 7-14 dias de retencion y muestreo de telemetria                            |
| Redis           | No habilitado mientras exista una sola replica                                               |
| Azure SignalR   | No habilitado mientras exista una sola replica                                               |
| Front Door/WAF  | No incluido; usar dominio administrado por Static Web Apps y restricciones de Container Apps |

### Costo mensual de la version minima

**USD 45-90/mes**, aproximadamente **CRC 23.000-47.000/mes** usando USD 1 =
CRC 520.

El extremo bajo supone una base Basic/S0 con poca actividad, pocos logs y bajo
almacenamiento. Si SQL S0 permanece activo todo el mes, el total puede acercarse
a **USD 80-120/mes**.

### Ventajas y riesgos

- Es el perfil mas barato para una prueba de mercado.
- Conserva compatibilidad con el codigo actual.
- No ofrece tolerancia a fallos de replica ni despliegues sin interrupcion.
- No es adecuado para una campana con mucho trafico o para SignalR distribuido.
- El limite de SQL debe vigilarse con alertas de CPU, DTU y errores de conexion.

## Version 2 - Recomendada / mejor costo-precio

### Objetivo

Operar produccion con 100 usuarios activos, QR publico, reportes de perdida,
subida de fotos, jobs diarios y margen razonable para picos pequenos.

### Configuracion recomendada

| Componente      | Configuracion                                                                        |
| --------------- | ------------------------------------------------------------------------------------ |
| Container Apps  | 0.5 vCPU, 1 GiB, 1 replica minima, maximo 2-3 replicas                               |
| Azure SQL       | S0/S1 provisionado segun metricas; S0 para comenzar y S1 si suben las consultas      |
| Static Web Apps | Standard para dominio, staging y limites de produccion                               |
| Storage         | Standard LRS, lifecycle de 7/90 dias segun contenedor                                |
| Key Vault       | Standard, RBAC e identidad administrada                                              |
| ACR             | Basic, tags inmutables y limpieza de imagenes antiguas                               |
| Logs            | 30 dias, muestreo de Application Insights y alertas 5xx, latencia y disponibilidad   |
| Redis           | Basic C0 o equivalente, obligatorio antes de usar mas de una replica                 |
| Azure SignalR   | Free mientras los limites sean suficientes; Standard S1 cuando haya uso real de hubs |
| Backups         | Retencion operativa de Azure SQL y prueba periodica de restauracion                  |
| WAF             | Posponer hasta que haya trafico publico relevante; activarlo antes de campanas o B2B |

### Costo mensual de la version recomendada

**USD 90-170/mes**, aproximadamente **CRC 47.000-88.000/mes**.

Una configuracion inicial razonable es:

- Container Apps: USD 30-45.
- SQL S0/S1: USD 15-55.
- Static Web Apps Standard: USD 9.
- Redis pequeno: USD 15-25.
- Storage, ACR, Key Vault y monitorizacion: USD 15-35.

Computer Vision, Maps, correo y canales de mensajeria se deben presupuestar
aparte porque dependen del numero de imagenes, consultas y mensajes, no del
numero nominal de usuarios.

### Recomendacion

Este es el perfil recomendado para el primer lanzamiento comercial. Mantiene un
servicio administrado, evita una migracion de motor y permite crecer de una a
tres replicas sin rediseñar la API. El presupuesto de Azure debe configurarse
entre **USD 150 y USD 200** para generar alertas antes de superar el objetivo.

## Version 3 - Alta disponibilidad / crecimiento

### Objetivo de alta disponibilidad

Preparar la plataforma para campanas, crecimiento B2B/B2G, mayor volumen de
fotos y disponibilidad superior a la de una sola replica.

### Configuracion de alta disponibilidad

| Componente      | Configuracion                                                                            |
| --------------- | ---------------------------------------------------------------------------------------- |
| Container Apps  | 1 vCPU, 2 GiB, 2 replicas minimas, autoscale hasta 5-10                                  |
| Azure SQL       | General Purpose provisionado de 2 vCores o superior, con backups y restauracion probados |
| Static Web Apps | Standard                                                                                 |
| Storage         | Standard LRS inicialmente; ZRS si el requisito de continuidad lo justifica               |
| Key Vault       | Standard con RBAC, purge protection y rotacion documentada                               |
| ACR             | Basic o Standard si se requieren mas operaciones y retencion de imagenes                 |
| Redis           | Standard C1 o superior, con disponibilidad y persistencia segun necesidad                |
| Azure SignalR   | Standard S1 o superior                                                                   |
| Front Door      | Azure Front Door Premium con WAF administrado y dominio personalizado                    |
| Monitorizacion  | Application Insights, Log Analytics, alertas, dashboards y SLOs de negocio               |
| Recuperacion    | Pruebas de restauracion SQL, runbook de rollback y despliegues por revision              |

### Costo mensual de alta disponibilidad

**USD 300-550+/mes**, aproximadamente **CRC 156.000-286.000+/mes**.

El rango depende principalmente de SQL, Front Door/WAF, Redis, SignalR, logs y
cantidad de replicas. No es necesario para 100 usuarios activos si el trafico es
normal; se justifica por disponibilidad, contratos institucionales o picos
predecibles.

### Ventajas y riesgos de alta disponibilidad

- Mejor continuidad durante despliegues y fallos de instancia.
- Permite separar el trafico publico de la API mediante Front Door/WAF.
- Requiere Redis y SignalR correctamente configurados.
- Aumenta el costo fijo y la complejidad operativa.
- Debe adoptarse junto con pruebas de carga y metricas, no solo por anticipacion.

## Comparacion rapida

| Perfil              |     Usuarios objetivo | Costo estimado/mes | Disponibilidad                    | Recomendacion                   |
| ------------------- | --------------------: | -----------------: | --------------------------------- | ------------------------------- |
| Minima              |                 1-100 |          USD 45-90 | Basica, una replica               | Demo, beta o validacion inicial |
| Recomendada         |               100-500 |         USD 90-170 | Buena, con crecimiento controlado | Primer lanzamiento comercial    |
| Alta disponibilidad | 500+ o picos/campanas |       USD 300-550+ | Alta, multi-replica y WAF         | B2B/B2G, SLA o trafico elevado  |

## Decisiones de costo que no degradan calidad

1. Mantener una sola imagen Docker y limpiar tags antiguos de ACR.
2. Mantener Container Apps en 0.5 vCPU/1 GiB mientras CPU y memoria esten por debajo de los umbrales observados.
3. Limitar `maxReplicas` a 3 para el MVP y ampliar solo con evidencia de saturacion.
4. Usar lifecycle management en Blob Storage para fotos temporales y avatares.
5. Aplicar muestreo de Application Insights y retencion de Log Analytics de 14-30 dias.
6. Mantener Static Web Apps para el frontend en lugar de servirlo desde la API.
7. Comenzar con SQL S0 en produccion pequena y subir a S1 por metricas, no por suposicion.
8. Activar Redis y Azure SignalR solamente cuando se use mas de una replica o haya trafico real de tiempo real.
9. No provisionar Front Door Premium antes de tener un requisito de WAF, dominio centralizado o proteccion de campanas.
10. Configurar alertas de presupuesto en 80% y 100% del limite mensual.

## Cambiar entre perfiles

- **Minima a recomendada:** subir SQL a S0/S1, Static Web Apps a Standard,
  agregar Redis y habilitar maximo de 2-3 replicas despues de validar SignalR.
- **Recomendada a alta disponibilidad:** subir CPU/memoria, SQL provisionado,
  Redis Standard, SignalR Standard, Front Door/WAF, backups y pruebas de carga.
- **No cambiar de SQL Server a PostgreSQL solo por precio:** PawTrack depende de
  `geography`, `STDistance`, `sp_getapplock`, migraciones SQL Server y EF Core.
  Esa migracion tendria un costo y riesgo superior al ahorro inicial.

## Parametros que deben medirse

Antes de cambiar de perfil, revisar durante al menos 7 dias:

- CPU y memoria de cada revision de Container Apps.
- Numero de replicas y tiempo de respuesta P95/P99.
- DTU/vCore, conexiones, bloqueos y errores de Azure SQL.
- Duracion y frecuencia de los jobs programados.
- GB ingeridos por Log Analytics y Application Insights.
- GB almacenados y operaciones de Blob Storage.
- Conexiones y mensajes de SignalR si se habilita.
- Operaciones y memoria de Redis si se habilita.

La configuracion actual de IaC debe seguir siendo la fuente de verdad. Los
cambios manuales en Portal o CLI deben reflejarse despues en
`infra/main.bicep` para evitar deriva entre ambientes.
