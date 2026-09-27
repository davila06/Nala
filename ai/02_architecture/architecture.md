# Arquitectura observada

**Corte:** 2026-09-27. Esta es una síntesis; ver [arquitectura canónica](../../docs/ARCHITECTURE.md), [componentes](../../docs/architecture/COMPONENTS.md) y [dependencias](../../docs/architecture/DEPENDENCIES.md).

## Topología

`PawTrack.sln` contiene seis proyectos: `PawTrack.API`, `PawTrack.Application`, `PawTrack.Domain`, `PawTrack.Infrastructure`, `PawTrack.UnitTests` y `PawTrack.IntegrationTests`. `backend/HashGen` queda fuera de la solución. Las capas API/Application/Domain/Infrastructure representan un monolito modular; el [contexto EF](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs) agrupa entidades de varios dominios en un `PawTrackDbContext`.

El backend registra MediatR, validación/pipeline, ASP.NET Core, SQL Server/EF Core y adaptadores en sus extensiones de composición: [Application DI](../../backend/src/PawTrack.Application/ApplicationServiceCollectionExtensions.cs), [Infrastructure DI](../../backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs) y [API](../../backend/src/PawTrack.API/Program.cs). No inferir límites de datos autónomos a partir de carpetas.

El cliente es una PWA React/TypeScript con rutas en [routes.tsx](../../frontend/src/app/routes.tsx). La infraestructura declarada reside en [infra](../../infra/); CI está definido bajo [.github/workflows](../../.github/workflows/). Ninguno demuestra por sí solo despliegue exitoso.

## Deuda arquitectónica observable

- Contextos de dominio amplios comparten persistencia: riesgo de acoplamiento y coordinación de cambios/migraciones.
- La distribución del monolito no está separada en microservicios; extracción requiere contratos y ownership de datos explícitos.
- Múltiples documentos cumplen roles similares de estrategia/capacidades, aunque hay un índice y trazabilidad recientes. Mantener el hub como índice, no crear otra fuente maestra.
- Tests/documentación de corte anterior no validan los cambios concurrentes del árbol actual.

Decisiones históricas no confirmadas formalmente: se registran en `04_decisions/ADR-INDEX.md`. Riesgos no equivalen a defectos confirmados.
