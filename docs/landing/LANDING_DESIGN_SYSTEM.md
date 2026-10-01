# Design system del landing

## Tokens

- Ink: `#173c34`; coral: `#d9664b`; sun: `#f2c85c`; paper: `#f7f8f3`; mint: `#dbe9d9`.
- Radios: 2–8px; evitar tarjetas anidadas.
- Contenedor: 1240px; gutters 20px móvil, 48–96px desktop.
- Estados: ready verde, partial amarillo, blocked coral; nunca depender solo de color.
- Focus: outline 3px con offset 4px.
- Motion: transform/opacity, 180–240ms; reduced motion desactiva transiciones.
- Touch targets: mínimo 44px.

## Componentes

Header, CTA, intent card, capability status, empty catalog, FAQ details, breadcrumb, editorial card y footer. Todos deben conservar HTML semántico y estados keyboard/focus.
