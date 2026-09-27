# Límites del marketplace

- El [gateway manual](../../backend/src/PawTrack.Application/ServiceProviders/Payments/ManualProviderPaymentGateway.cs) no demuestra captura, comisión ni liquidación automática; no anunciar checkout de proveedor como activo.
- [StoreOrdersController](../../backend/src/PawTrack.API/Controllers/StoreOrdersController.cs) admite `LocationId` opcional y confirmación del pedido, pero no acredita stock, factura o cobro de la tienda. La [guía de operación de tiendas](../ROADMAP_TIENDAS_USO_DIARIO.md) separa trabajo pendiente.
- Los [endpoints de proveedores](../../backend/src/PawTrack.API/Controllers/ServiceProvidersController.cs) prueban superficie de catálogo, no convenios comerciales ni verificación operativa de cada anunciante. Los [tests de concurrencia](../../backend/tests/PawTrack.IntegrationTests/ServiceProviders/ProviderBookingSqlConcurrencyTests.cs) requieren SQL; no se ejecutaron aquí.
