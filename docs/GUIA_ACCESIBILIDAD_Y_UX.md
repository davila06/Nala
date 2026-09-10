# Guia de Accesibilidad y UX

**Estado:** criterio activo de calidad  
**Audiencia:** frontend, QA y producto  
**Corte:** 2026-09-09

## Criterios minimos

- Toda accion puede completarse con teclado.
- Inputs tienen label asociado y errores legibles.
- Focus visible, orden logico y no depender solo del color.
- Contraste suficiente en texto, badges, mapas y estados.
- Dialogos tienen nombre, cierre por teclado y foco controlado.
- Imagenes informativas tienen `alt`; decorativas usan `aria-hidden`.
- Loading, error, vacio y offline tienen estados visibles.
- Texto y botones caben en viewport movil sin solaparse.
- Mapas tienen alternativa de lista cuando la ubicacion sea operativa.
- No exponer PII en titles, toasts, URLs o capturas.

## Flujos a revisar

Login, registro, reportar perdida, reportar avistamiento, chat, salud, clinica,
municipalidad, tienda, proveedor, Admin y Support. Probar al menos viewport
movil, desktop, zoom 200% y navegacion sin mouse.

## Criterios de aceptacion

Un cambio frontend no se cierra si rompe typecheck, lint, tests, responsive,
lectores de pantalla o mensajes de error. Registrar excepciones conocidas en
[ERRORES_PENDIENTES.md](ERRORES_PENDIENTES.md).
