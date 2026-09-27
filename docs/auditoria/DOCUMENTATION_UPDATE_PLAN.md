# Plan de actualización documental de NALA

**Corte:** 2026-09-27. **Estado:** corte documental completado con comprobaciones pendientes; no equivale a certificación exhaustiva de endpoints ni de producción. Este plan no acredita funcionalidades ni constituye una aprobación comercial o de despliegue.

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

Los seis proyectos de la solución y HashGen se inventariaron; API y frontend se inspeccionaron por módulos, pero no se verificó cada método de los 68 controllers con pruebas de integración. Cobertura y excepciones actualizadas en [TESTING](../TESTING.md), [catálogo API](../api/ENDPOINT_CATALOG.md) y [brechas](DOCUMENTATION_GAP_REPORT.md).

Durante la revisión surgieron ediciones concurrentes en código médico y pruebas ([MedicalController](../../backend/src/PawTrack.API/Controllers/MedicalController.cs), [MedicalEndpointsTests](../../backend/tests/PawTrack.IntegrationTests/Medical/MedicalEndpointsTests.cs)); se conservaron. La verificación ejecutada es de un estado anterior a esas ediciones y no las acredita ([TESTING](../TESTING.md)).

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

**Resultado del corte:** build solución correcto, 1621 unitarias backend aprobadas, typecheck frontend correcto, Vitest 142/143 con un fallo en caja clínica, NuGet sin vulnerabilidades reportadas; [TESTING](../TESTING.md). Integración SQL/Blob, proveedor, E2E, despliegue, todos los contratos API y verificaciones remotas CI quedan abiertos. No se actualizó código productivo.

## Iniciativa de conocimiento y gobierno AI (2026-09-27)

Esta iniciativa amplía el índice de conocimiento sin reemplazar los documentos canónicos existentes. `/ai` funciona como hub para agentes y debe enlazar a `docs/ai/`, `docs/business/`, `docs/architecture/`, `docs/domains/`, `docs/security/` y `docs/auditoria/` cuando esas fuentes ya existan. No se reescriben documentos concurrentes ni se modifica código productivo.

| Entregable | Evidencia base y límite |
| ---------- | ---------------------- |
| Contexto, producto, arquitectura, dominios e integraciones | `PawTrack.sln`, proyectos backend/frontend, `infra/` y documentación activa. El inventario no certifica despliegue ni operación externa. |
| Memoria del producto | Git inicial fechado 2026-04-10, changelog y documentos fechados; incluir solo hitos que tengan evidencia versionada y señalar fechas históricas como aproximadas cuando la fuente no sea un registro de release. |
| ADRs | Siete decisiones narradas en `docs/Manuales/MANUAL_TECNICO.md`; trasladarlas como `DRAFT` rastreables, no como decisiones aprobadas, porque no hay registro formal que acredite aprobación. |
| Agentes y skills | Inventario local de `.github/skills/` y `.agents/skills/`; un skill describe un flujo/restricción, no demuestra que su auditoría haya sido ejecutada. |
| Ontología, capacidades, estrategia y compliance | Resumir fuentes existentes; separar implementación, contrato objetivo, hipótesis, propuesta y pendiente de validación legal/comercial. |
| Métricas AI | Score cualitativo 0-100 con criterios, rutas y limitaciones explícitas; no presentar como métrica automática ni como benchmark. |

Validación: conservar el árbol sucio previo; revisar solo los archivos creados para rutas relativas y marcadores de estado; no ejecutar tests de producto ni despliegues para una entrega exclusivamente documental. Reportar cobertura observada, áreas no verificadas y el nivel de preparación como evaluación documental del corte, no certificación.
