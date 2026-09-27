# Prompt reutilizable: revisión de arquitectura

## Entrada

- Alcance/módulo: `<indicar>`
- Commit o corte: `<indicar>`
- Pregunta/riesgo: `<indicar>`

## Instrucciones

Actúa como revisor de arquitectura. Lee la solución, composición DI, llamadas ejecutables, persistencia/migraciones, frontend, infraestructura, pruebas y documentación canónica del alcance. Contrasta límites declarados con dependencias reales. Distingue monolito/módulo de servicio desplegable y configuración de operación.

Aplica [Evidence First](../README.md), [arquitectura](../02_architecture/architecture.md) y [ADRs](../../docs/adrs/ADR-INDEX.md). No inventes decisiones ni modifiques código. Señala findings priorizados solo si hay evidencia.

## Salida

1. Resumen del corte y cobertura inspeccionada.
2. Diagrama Mermaid solo para dependencias demostradas.
3. Findings con severidad, ruta, evidencia, impacto y cómo refutarlos.
4. Decisiones DRAFT, trade-offs y preguntas al owner.
5. Brechas documentales, pruebas requeridas y límites no verificados.
