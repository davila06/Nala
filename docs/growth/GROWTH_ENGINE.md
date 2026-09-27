# Crecimiento: alcance comprobable

Existen [validación/canje de promociones](../../backend/src/PawTrack.API/Controllers/PromotionsController.cs) y [eventos/funnel para Admin](../../backend/src/PawTrack.API/Controllers/ProductAnalyticsController.cs). La [persistencia de eventos y canjes](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs) no prueba tracción ni atribución de ventas. Estado de estas superficies por lectura: `IMPLEMENTADO_SIN_PRUEBAS` del flujo completo; alianzas con influencers, afiliados y automatizaciones son `NO_VERIFICADO` en este corte.

Ver [referidos](REFERRALS.md), [campañas](CAMPAIGNS.md), [atribución](ATTRIBUTION.md) y [eventos](GROWTH_EVENTS.md). No atribuir resultados comerciales a instrumentación técnica.
