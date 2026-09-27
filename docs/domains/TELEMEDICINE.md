# Telemedicina: límite del producto

Estado `DECLARADO_NO_IMPLEMENTADO` para **consulta remota con audio/video de extremo a extremo**: el [controller de clínicas](../../backend/src/PawTrack.API/Controllers/ClinicsController.cs) y el [router](../../frontend/src/app/routes.tsx) exponen agendas/consultas y páginas clínicas, pero no prueban una sesión remota. La búsqueda de SDK de comunicación, sala, video o telemedicina en `backend/src/` y `frontend/src/` no encontró un flujo de llamadas en este corte. No presentar registro de consulta como telemedicina activa.

La consulta clínica documentada es una capacidad distinta; ver [arquitectura](TELEMEDICINE_ARCHITECTURE.md), [seguridad](TELEMEDICINE_SECURITY.md) y [limitaciones](TELEMEDICINE_LIMITATIONS.md).
