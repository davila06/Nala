# Dominio: NFC

**Estado del repositorio al 2026-10-01:** `PARCIALMENTE_IMPLEMENTADO` para bundle y guía de configuración manual; integración nativa NFC `DECLARADO_NO_IMPLEMENTADO`; hardware, venta y fulfillment `NO_VERIFICADO`.

La PWA incluye [NfcSetupGuide](../../frontend/src/features/bundles/components/NfcSetupGuide.tsx), que instruye a la persona a escribir y leer la URL del perfil mediante NFC Tools y una etiqueta compatible. La guía indica que PawTrack no escribe el chip por la persona. El dominio incluye el tipo de bundle [NfcQrCombo](../../backend/src/PawTrack.Domain/Bundles/BundleProductType.cs) y pruebas de su precio técnico en [BundleProductTypeTests](../../backend/tests/PawTrack.UnitTests/Bundles/BundleProductTypeTests.cs). Estos elementos acreditan un flujo de configuración asistida y un SKU modelado, no un lector, pairing, escritura/lectura nativa, compra disponible, dispositivo entregado, soporte por fabricante o compatibilidad universal.

La [estrategia](../../docs/PRODUCT_STRATEGY_TOP1.md) conserva NFC como apuesta de identidad portable; la operación del hardware debe verificarse aparte. No afirmar NFC como GPS, localización en vivo, función automática de PawTrack ni producto disponible para compra sin evidencia fechada de dispositivo, fulfillment, compatibilidad y entorno.
