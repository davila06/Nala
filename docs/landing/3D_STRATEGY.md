# 3D Strategy

## Decisión actual

Implementar 3D CSS de baja complejidad: perspectiva, capas, `translateZ`, sombras y profundidad en hero, cards, botones e iconos. No implementar WebGL en esta fase. El producto verificable es identidad y recuperación; un objeto 3D sin asset de hardware aprobado sería decorativo y podría sugerir disponibilidad de IoT.

## Candidata WebGL futura

**Tag de identidad conectado:** tag conceptual → perfil → ruta de ayuda.

- Emoción: claridad y conexión.
- Stack: Three.js/React Three Fiber solo si ya existe una dependencia aprobada.
- Impacto esperado: comprensión del recorrido, no conversión garantizada.
- Coste: alto en asset, performance, QA y accesibilidad.
- Fallback: poster estático + HTML semántico.
- Requisitos: GLB optimizado, lazy load, DPR limitado, pausa fuera de viewport, reduced motion y presupuesto móvil.

Spline/Babylon.js no se justifican frente al export estático actual. No usar collar futuro como producto disponible.
