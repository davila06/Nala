# Limitaciones de telemedicina

- [Citas y consultas](../../backend/src/PawTrack.API/Controllers/ClinicsController.cs) no garantizan presencia del profesional ni conexión en tiempo real. No anuncies videoconsulta como activa.
- Las [entidades clínicas](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs) no demuestran firma de receta, grabación, token de sala o consentimiento audiovisual.
- No se ejecutó una prueba end-to-end de proveedor ni una llamada remota; disponibilidad territorial, pagos, cancelaciones y manejo de emergencias quedan `NO_VERIFICADO`. Ver [documentación de salud](HEALTH_SAFETY_BOUNDARIES.md).
