# Plan de actualización documental de NALA

**Corte:** 2026-09-27. **Estado:** en ejecución. Este plan no acredita funcionalidades ni constituye una aprobación comercial o de despliegue.

## Inventario y cobertura pendiente

La solución [PawTrack.sln](../../PawTrack.sln) incluye seis proyectos: [API](../../backend/src/PawTrack.API/PawTrack.API.csproj), [Application](../../backend/src/PawTrack.Application/PawTrack.Application.csproj), [Domain](../../backend/src/PawTrack.Domain/PawTrack.Domain.csproj), [Infrastructure](../../backend/src/PawTrack.Infrastructure/PawTrack.Infrastructure.csproj), [UnitTests](../../backend/tests/PawTrack.UnitTests/PawTrack.UnitTests.csproj) e [IntegrationTests](../../backend/tests/PawTrack.IntegrationTests/PawTrack.IntegrationTests.csproj). También existe [HashGen](../../backend/HashGen/HashGen.csproj), fuera de la solución: se inspeccionará como herramienta auxiliar, sin atribuirle funcionalidad de producto. El frontend está definido en [package.json](../../frontend/package.json); la infraestructura declarada incluye [main.bicep](../../infra/main.bicep) y los workflows bajo [`.github/workflows`](../../.github/workflows/).

| Superficie                   | Verificación pendiente                                                                            |
| ---------------------------- | ------------------------------------------------------------------------------------------------- |
| API                          | Registro de rutas, controllers, contratos, middlewares y autorización efectiva.                   |
| Application                  | Commands, queries, handlers, validación, reglas y servicios registrados.                          |
| Domain                       | Entidades, estados, invariantes y dependencias.                                                   |
| Infrastructure               | EF Core, migraciones, jobs, adaptadores, configuración e integraciones.                           |
| UnitTests / IntegrationTests | Cobertura pertinente, condiciones de ejecución y resultados.                                      |
| HashGen                      | Propósito, límites y relación con la aplicación.                                                  |
| Frontend                     | Rutas, componentes, clientes HTTP, flags y correspondencia con la API.                            |
| Infraestructura y CI         | Recursos declarados frente a servicios realmente utilizados; despliegue real no inferible de IaC. |
| Documentación                | Índice, contratos vigentes, históricos, enlaces y afirmaciones que requieren contraste.           |

## Fuentes y conflictos iniciales

Prioridad: código ejecutable, configuración y migraciones, pruebas, infraestructura y por último documentación. [README.md](../README.md) sitúa [STATUS.md](../STATUS.md) como estado técnico y [FEATURES.md](../FEATURES.md) como matriz de planes; este último se declara expresamente «contrato funcional propuesto». No se convertirán sus tablas en afirmaciones de producto disponible sin comprobar enforcement y pruebas. El índice existente tiene información histórica útil; no se reemplazará silenciosamente. Cualquier contradicción concreta encontrada se registrará en `DOCUMENTATION_GAP_REPORT.md` con rutas antes de corregirla.

## Orden de trabajo

1. `nala-feature-inventory`: reconstruir capacidades y trazabilidad de extremo a extremo; clasificar con los ocho estados estándar.
2. `nala-architecture-review`: validar composición, referencias, datos, despliegue y observabilidad.
3. `nala-security-audit`: contrastar autenticación, ownership, acceso, privacidad y dependencias.
4. `nala-api-documenter`: inventariar rutas activas, contratos y errores.
5. `nala-subscription-plans`: comprobar planes, cuotas y enforcement, separando contrato propuesto de implementación.
6. Skills especializados solo donde haya implementación o documentación relevante: dominios de recuperación, salud, collares/GPS, clínica/telemedicina, B2B, marketplace, institucional y crecimiento; registrar exclusiones y evidencia.
7. `nala-ai-readiness`: distinguir funciones activas de propuestas y examinar límites de seguridad.
8. `nala-business-analyst`: traducir estados ya establecidos a capacidades y valor sin elevar propuestas a disponibilidad.
9. Consolidación por `nala-product-auditor`, verificación cruzada y changelog documental.

## Documentos previstos y propiedad

Se preservarán [README.md](../README.md), [STATUS.md](../STATUS.md), [FEATURES.md](../FEATURES.md) y documentos existentes, actualizando secciones puntuales cuando la evidencia lo justifique. El orquestador creará o actualizará `docs/PRODUCT_SCOPE.md`, `docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md`, `docs/auditoria/DOCUMENTATION_GAP_REPORT.md` y `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; inventario se encargará de `docs/FEATURES.md` (sin borrar su contrato propuesto), arquitectura de `docs/ARCHITECTURE.md` y `docs/architecture/`, seguridad de `docs/SECURITY.md` y `docs/security/`, API de `docs/API.md` y `docs/api/`, planes de `docs/commercial/`, dominios pertinentes de `docs/domains/`, `docs/editions/` o `docs/growth/`, IA de `docs/ai/` y negocio de `docs/business/`. No se archivará un documento sin demostrar que ha sido reemplazado; de ocurrir se preservará en `docs/archive/`.

## Verificación, riesgos y criterios de cierre

Cuando sea seguro, ejecutar restauración, compilación, pruebas y análisis de dependencias sobre la solución, más pruebas del frontend; evitar pruebas que requieran servicios externos, secretos, migraciones o despliegues. Registrar comando, resultado y alcance exactos. No interpretar ausencia de errores de compilación como verificación funcional. Revisar enlaces relativos, presencia de evidencia para cada afirmación, clasificación y conteos, consistencia estado actual/roadmap, secretos, cambios ajenos y diff de `docs/` completo. Riesgos iniciales: amplitud de dominios, contratos propuestos mezclados con productos vigentes e imposibilidad de inferir disponibilidad en producción de la configuración local; hasta verificarlos usar `NO_VERIFICADO`.
