# Guia de QA y E2E

**Estado:** activo  
**Audiencia:** QA, frontend y backend  
**Corte:** 2026-09-09

## Capas de prueba

- Unitarias: dominio, handlers y servicios.
- Integracion: API, persistencia, autorizacion y migraciones.
- Frontend: Vitest y Testing Library.
- E2E: Playwright contra backend real, SQL/Azurite y frontend preview.

## Preparacion local

1. `dotnet restore` y `dotnet build` desde `backend`.
2. Levantar SQL LocalDB/SQL Server y Azurite.
3. Ejecutar seeds documentados en [pruebas.md](pruebas.md).
4. Confirmar que el backend usa el entorno y base correctos.
5. `npm install` en `frontend`.
6. Ejecutar `npm test`, `npm run typecheck`, `npm run lint` y `npm run build`.
7. Ejecutar `npm run test:e2e` con el backend ya disponible.

## Flujos criticos

- registro, verificacion y login;
- crear mascota y QR publico;
- reportar perdida y avistamiento;
- chat enmascarado y handover;
- collar: activacion, GPS, zona segura y modo perdido;
- clinica: scan, grant medico y certificado;
- tienda: catalogo y pedido;
- proveedor: servicio, agenda y reserva;
- municipalidad: captura, estados y reportes;
- Admin/Support: aprobaciones, bienestar e incidentes.

## Regresiones obligatorias

Cada cambio de autorizacion requiere prueba BOLA/IDOR. Cada cambio de tier
requiere prueba con suscripcion activa, vencida y ausente. Cada cambio de
migracion requiere base vacia o upgrade incremental. Cada endpoint de archivo
requiere limite de tamano y content type.

## Datos de prueba

Usar usuarios y referencias de [pruebas.md](pruebas.md). No usar credenciales
reales ni bases productivas. Las suscripciones deben tener `ExpiresAt` futuro y
los seriales unicos por prueba.
