# Decisión 3D del landing

**Fecha:** 2026-10-01  
**Decisión:** Implementar profundidad 3D CSS en la página principal; diferir WebGL/Three.js.

## Razón

El núcleo verificable de PawTrack es identidad y recuperación. La profundidad CSS mejora jerarquía, cards, hero e iconos sin sugerir hardware. Un tag 3D WebGL sería decorativo mientras no exista un asset de producto aprobado, y podría hacer parecer disponible un hardware o bundle no verificado.

## Condición para WebGL

Reabrir la decisión WebGL únicamente cuando exista:

- asset GLB aprobado y con derechos;
- objetivo UX medible, no solo impacto visual;
- poster estático y alternativa HTML;
- presupuesto móvil validado;
- prueba reduced-motion, teclado y lector de pantalla;
- decisión de producto sobre si representa QR, NFC o hardware futuro.

## Implementación actual

La capa CSS aplica perspectiva, `translateZ`, sombras y elevación a hero, botones, cards, catálogo, paneles e iconos. Se desactiva en móvil y `prefers-reduced-motion`.

## Fallback

Mantener el hero fotográfico y la explicación HTML del recorrido identidad → recuperación → ayuda. Esto preserva comprensión, rendimiento y honestidad comercial.
