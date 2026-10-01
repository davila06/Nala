# Reporte de rendimiento

## Estado medido

La build estática genera 49/49 páginas. No existe baseline Lighthouse/CWV ejecutada en este corte; por tanto no se inventan LCP, CLS, INP ni TTFB.

## Controles implementados

- Export estático.
- `next/image` con dimensiones, `priority` y `sizes` en hero.
- Sin bundle 3D.
- Sin analytics ni SDK externo.
- Catálogo API solo cuando hay endpoint configurado.

## Próximo benchmark

Lighthouse móvil/desktop, throttling 4G, cold cache, 320/390/768/1440px y browser real. Objetivo: LCP <2.5s, CLS <0.1, INP <200ms; son objetivos, no resultados actuales.
