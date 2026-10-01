# Auditoría actual del landing

**Corte:** 2026-10-01. **Alcance:** `landing/frontend` únicamente.

## Estado

Next.js 16.3.8 con App Router y export estático. El landing usa React/TypeScript, CSS global, `next/image`, metadata, sitemap, robots, Vitest y ESLint. No tiene librería 3D, analytics, i18n, icon library ni assets de marca propios. El landing no conecta directamente con SQL ni ejecuta mutaciones; el catálogo opcional usa `GET` contra API pública.

## Fortalezas

- Skip link, landmarks, HTML semántico, alt text y focus visible.
- Copy conservador que distingue implementación, parcialidad y no verificado.
- Flujos de pérdida/hallazgo separados y sin captura de datos del caso.
- Export estático, CSP generada y rutas documentales.
- Estados de catálogo dinámico: loading, vacío, error y planes públicos.

## Deuda

- Imagen hero remota con derechos de uso no verificados.
- No existe captura real del producto ni asset visual local.
- No hay eventos analíticos ni consentimiento asociado.
- Header móvil usa `details`; falta prueba E2E de foco, escape y cierre.
- No hay auditoría WCAG 2.2 AA en navegador real ni pruebas visuales multi-viewport.
- La arquitectura contiene muchas rutas y puede sobrecargar la navegación.
- La página de registro del landing es una superficie legacy; la cuenta real vive en PawTrack.

## Prioridades

1. Intención y CTA contextual.
2. Estados de evidencia visibles.
3. Catálogo público únicamente por API.
4. Assets licenciados/locales y performance.
5. Analytics sin PII, SEO estructurado y pruebas visuales.

## No modificar

No tocar lógica crítica del backend, pricing runtime, gates de aprobación ni base de datos desde el landing.
