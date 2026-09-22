# PawTrack CR - Brief de inversión y patrocinio

> Versión: 2026-09-22 | Documento de trabajo para due diligence

## Resumen

PawTrack CR es una PWA para identidad de mascotas y recuperación de animales perdidos en Costa Rica. El flujo central es: registrar mascota, generar QR, reportar pérdida, recibir avistamientos, coordinar búsqueda y confirmar reunificación.

El producto tiene una base funcional amplia y continúa en consolidación enterprise. No se presenta como completamente production-ready hasta completar la validación operativa, observabilidad y gates comerciales descritos en [docs/STATUS.md](STATUS.md).

## Problema

Los reportes de mascotas perdidas suelen estar dispersos entre redes sociales y canales privados. PawTrack estructura identidad, contacto protegido, avistamientos, coordinación de campo y handover seguro en un único flujo.

Las cifras nacionales de mercado y de mascotas perdidas deben citar una fuente pública antes de usarse como claims comerciales.

## Capacidades implementadas

| Área                                              | Estado verificado                                                     |
| ------------------------------------------------- | --------------------------------------------------------------------- |
| Identidad, QR, perfil público y microchip         | Implementado                                                          |
| Reportes de pérdida, avistamientos y mapa público | Implementado                                                          |
| Matching visual y predicción geográfica           | Implementado                                                          |
| Chat enmascarado y handover seguro                | Implementado                                                          |
| Coordinación de búsqueda con SignalR              | Implementado con consentimiento de ubicación                          |
| Difusión por canales externos                     | Implementado según configuración del proveedor                        |
| Expediente médico, consentimiento y exportación   | Implementado                                                          |
| Clínicas, certificados y grants de acceso         | Implementado con gates de autorización                                |
| Municipalidades, tiendas y suscripciones          | Implementado; validar operación comercial                             |
| Collar GPS, historial, alertas y zonas seguras    | Implementado; depende de proveedores/configuración                    |
| Adopciones                                        | Parcial; revisar estado actual antes de venderlo como módulo completo |

## Evidencia técnica

Las cifras deben regenerarse con los comandos actuales antes de cada presentación. La fuente operativa de métricas de recuperación es `GET /api/v1/product-events/performance`, con resultados correlacionados por incidente de pérdida y medianas de tiempo.

| Indicador                    | Fuente o estado                                                                              |
| ---------------------------- | -------------------------------------------------------------------------------------------- |
| Build backend Release        | `dotnet build PawTrack.sln --configuration Release`                                          |
| Typecheck y build frontend   | `npm run build` en `frontend/`                                                               |
| Lint frontend                | `npm run lint` en `frontend/`                                                                |
| Métricas de recuperación     | Endpoint administrativo autorizado                                                           |
| Cumplimiento regulatorio     | `SENASA-ready` solo como trazabilidad/revisión; no implica aprobación ni integración oficial |
| Tracción, usuarios y mercado | No publicar sin fuente y fecha de corte                                                      |

## Arquitectura

- Backend .NET 9, Clean Architecture, CQRS, MediatR y EF Core.
- Frontend React 19, TypeScript estricto, Vite, PWA y TanStack Query.
- SQL Server/Azure SQL, Blob Storage, SignalR, Application Insights y Key Vault.
- Las fotografías y binarios se almacenan en Blob Storage; los secretos no deben incluirse en documentación ni repositorio.

## Modelo comercial

Los siguientes conceptos son líneas de negocio configurables o propuestas y requieren validación de precio, contrato, impuestos, disponibilidad y operación antes de presentarse como ingresos activos:

- Suscripciones para dueños y familias.
- Planes para clínicas veterinarias.
- Portales institucionales para municipalidades.
- Catálogo y pedidos para tiendas.
- Bundles de collar GPS sujetos al proveedor de hardware.
- Recompensas y pagos SINPE según alcance operativo y custodia real de fondos.
- Publicidad y patrocinios dentro de la aplicación.

No hay que presentar checkout recurrente, integración oficial con SENASA, escrow, payout o volumen de usuarios como capacidad comercial confirmada sin evidencia contractual y operativa.

## Patrocinio

El patrocinio puede incluir, sujeto a disponibilidad y aprobación:

- Placement publicitario en mapa, dashboard, directorio o feed.
- Co-branding en campañas de recuperación.
- Reportes agregados de impresiones y alcance.
- Beneficios promocionales para clínicas, refugios o usuarios.

Las impresiones, usuarios alcanzados, escaneos y reunificaciones atribuibles deben medirse primero. No se garantizan volúmenes ni exclusividad sin contrato.

## Riesgos abiertos

1. Completar validación operativa de producción, alertas y ventanas SLO.
2. Mantener documentación comercial sincronizada con código y evidencia.
3. Ejecutar suites completas backend/frontend y CI en cada release.
4. Validar proveedores externos: WhatsApp, Telegram, Facebook, GPS, Azure y pagos.
5. Obtener revisión legal antes de usar lenguaje regulatorio o contratos de inversión.

## Próximos pasos de due diligence

1. Demo en entorno controlado con datos no sensibles.
2. Entrega de resultados de build, lint, tests y migraciones con fecha de corte.
3. Revisión de arquitectura, seguridad, costos Azure y dependencias externas.
4. Definición contractual de patrocinio o inversión; este documento no constituye una oferta de valores.

## Contacto

**Denis Ávila** - Fundador y CTO, PawTrack CR
Correo: davila06@gmail.com
Sitio: https://pawtrack.cr
