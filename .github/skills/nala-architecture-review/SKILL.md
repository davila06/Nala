---
name: nala-architecture-review
description: Analiza y documenta la arquitectura real de NALA. Usar al revisar componentes, capas, dependencias, Clean Architecture, CQRS, persistencia, Azure, integraciones, procesos asíncronos, observabilidad o decisiones arquitectónicas.
---

# Revisión de arquitectura

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Antes de un cambio extenso, inventaría soluciones y proyectos, dependencias y planes existentes; prepara un plan con `nala-product-auditor`. Lee registros, composición de servicios y rutas ejecutables, no solo nombres de carpetas.
2. Reconstruye capas, módulos o bounded contexts, referencias, flujos de datos y APIs; contrasta uso real de Clean Architecture, CQRS y MediatR con sus configuraciones y llamadas. Traza contextos de persistencia, caché, mensajería, procesos asíncronos, integraciones, autenticación y autorización.
3. Contrasta configuración con infraestructura, despliegue, CI/CD y observabilidad. Distingue componente activo, configurado sin uso, despliegue no comprobado y propuesta; no presupongas Redis, Azure ni proveedores por dependencias o carpetas.
4. Documenta riesgos arquitectónicos, deuda y decisiones propuestas aparte del estado actual, con evidencia relativa en cada afirmación. Clasifica capacidades cuando corresponda según `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`; sin ejecución de pruebas pertinentes no declares verificación.
5. Actualiza `docs/ARCHITECTURE.md`, `docs/architecture/COMPONENTS.md`, `docs/architecture/DEPENDENCIES.md` y `docs/architecture/DEPLOYMENT.md`; usa Mermaid para contexto, componentes, secuencia y despliegue solo si representa relaciones demostradas. Comprueba enlaces, coherencia y diff.

## Coordinación

- Consume inventario de `nala-product-auditor` y `nala-feature-inventory`; alimenta `nala-security-audit`, `nala-gps-platform-review`, `nala-telemedicine-review` y `nala-ai-readiness`. Recomienda `nala-api-documenter` para detalles de rutas.
- Puede modificar solo los cuatro documentos de arquitectura anteriores y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica documentos de seguridad, dominio, infraestructura ni código.
- Conserva secciones de otros responsables; eleva contradicciones al orquestador para el reporte de brechas antes de cambiar afirmaciones compartidas y registra cambios propios en el changelog.
