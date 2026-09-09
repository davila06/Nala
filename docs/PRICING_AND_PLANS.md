# PawTrack CR - Planes y capacidades comerciales

> Estado: activo como consolidacion tecnica; precios sujetos a aprobacion
> comercial y legal. Corte: 2026-09-09.
>
> La autoridad tecnica son `SubscriptionTier`, `SubscriptionPricing` y los
> gates del backend. Este documento no convierte una capacidad tecnica en una
> promesa comercial automaticamente.

## B2C

| Plan        | Estado tecnico | Capacidad principal                                |
| ----------- | -------------- | -------------------------------------------------- |
| Free        | Activo         | 1 mascota, limites basicos de QR/IA                |
| UserPlus    | Activo         | Hasta 3 mascotas, funciones ampliadas, GPS         |
| UserFamilia | Activo         | Mascotas ilimitadas, familia y expediente completo |

Los precios B2C deben mantenerse sincronizados con `SubscriptionPricing` y
aprobarse antes de publicarse.

## Clinicas

| Plan          | Estado tecnico | Capacidades                                |
| ------------- | -------------- | ------------------------------------------ |
| ClinicPlus    | Activo         | Visibilidad, badge, estadisticas y alertas |
| ClinicPartner | Activo         | Integraciones, API, widget y certificados  |

La verificacion visible no equivale a licencia estatal ni aval regulatorio.

## Tiendas

| Plan         | Estado tecnico | Capacidades actuales                                                                                     |
| ------------ | -------------- | -------------------------------------------------------------------------------------------------------- |
| StorePlus    | Activo         | Catalogo, directorio y solicitudes de pedido                                                             |
| StorePartner | Parcial        | Capacidades avanzadas tecnicas heredadas; multi-sede queda fuera del alcance comercial actual de tiendas |

Decisiones de tiendas vigentes:

- PawTrack comunica solicitudes; no vende ni intermedia productos.
- No cobra comision transaccional.
- El pago, si existe, se gestiona manualmente entre cliente y tienda.
- Una cuenta administra una tienda y una sede en la fase actual.
- La tienda controla disponibilidad, entrega, aceptacion y estados.
- PawTrack no maneja inventario ni garantiza existencia.

Ver [pendientesTiendas.md](pendientesTiendas.md) y [legal.md](legal.md).

## Refugios y adopciones

| Plan        | Estado tecnico | Capacidades                                   |
| ----------- | -------------- | --------------------------------------------- |
| ShelterPlus | Activo         | Publicacion y funciones avanzadas de adopcion |

Los limites gratuitos de entrada deben documentarse como acceso base, no como
planes pagados si no existe flujo de billing activo.

## Municipalidades

| Plan            | Periodicidad tecnica | Estado                                      |
| --------------- | -------------------- | ------------------------------------------- |
| MuniBasica      | Anual                | Tier modelado; compra/renovacion incompleta |
| MuniFull        | Anual                | Tier modelado; compra/renovacion incompleta |
| MuniRedRegional | Anual                | Tier modelado; compra/renovacion incompleta |

No publicar precios ni SLA definitivos hasta cerrar contratacion, facturacion,
roles y soporte institucional.

## Proveedores de servicios

- Directorio, catalogo, disponibilidad, reservas y verificacion existen.
- No existe pricing recurrente aprobado.
- No publicar comisiones, payout o reembolsos como activos.
- La membresia tecnica `Free`/`Verified`/`Featured` es una capacidad de gating,
  no un catalogo comercial aprobado.

## Reglas de mantenimiento

- Cada precio debe tener moneda, periodicidad, impuestos incluidos/excluidos,
  fecha de vigencia y responsable de aprobacion.
- Cada capacidad debe tener endpoint, gate, prueba y evidencia de operacion.
- No duplicar tablas de planes en `planes.md`, `precios.md` o `pricing.md`.
- Las propuestas historicas deben permanecer marcadas como draft/historical.
- Los claims de recuperacion, SENASA, verificacion o seguridad necesitan
  evidencia y revision legal antes de marketing.
