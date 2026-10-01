# Decisión 3D del landing

**Fecha:** 2026-10-01  
**Decisión:** Implementar profundidad 3D CSS y una escena Three.js ligera en el hero; diferir modelos GLB de hardware.

## Razón

El núcleo verificable de PawTrack es identidad y recuperación. La escena representa un tag abstracto de identidad y nodos de coordinación, no hardware vendible ni una integración operativa. La profundidad CSS mejora cards, hero e iconos sin sugerir disponibilidad comercial.

## Condición para modelos de hardware 3D

Reabrir esta parte únicamente cuando exista:

- asset GLB aprobado y con derechos;
- objetivo UX medible, no solo impacto visual;
- poster estático y alternativa HTML;
- presupuesto móvil validado;
- prueba reduced-motion, teclado y lector de pantalla;
- decisión de producto sobre si representa QR, NFC o hardware futuro.

## Implementación actual

La capa CSS aplica perspectiva, `translateZ`, sombras y elevación a hero, botones, cards, catálogo, paneles e iconos. La escena Three.js crea un tag abstracto, un anillo y nodos, con importación dinámica, DPR limitado, pausa fuera de viewport, cleanup y reduced motion.

## Fallback

Mantener el hero fotográfico y la explicación HTML del recorrido identidad → recuperación → ayuda. Esto preserva comprensión, rendimiento y honestidad comercial.
