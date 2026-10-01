# Sistema de movimiento

- Entrada: hero copy y visual con opacity/transform, 180–240ms.
- Hover: elevación leve de tarjetas, nunca `transition: all`.
- Focus: persistente y visible.
- Catálogo: skeleton/status con `aria-live`.
- No scrolljacking, autoplay ni parallax obligatorio.
- `prefers-reduced-motion`: scroll automático y transiciones desactivadas.
- 3D futuro: pausa fuera de viewport y fallback poster.
