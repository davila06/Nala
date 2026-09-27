# Mapa de dominios

Los nombres de abajo resumen carpetas de backend y superficies, no bounded contexts formalmente aprobados ni microservicios separados. Fuentes: [modelo de datos](../../docs/DATA_MODEL.md), [trazabilidad](../../docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md), [modelo de capacidades](../../docs/business/CAPABILITY_MAP.md).

| Grupo                  | Módulos/capacidades observados                                                                | Límite y detalle                                                                                             |
| ---------------------- | --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| Identidad y perfil     | Auth, Pets, Family, QR/microchip                                                              | Fichas de mascotas, identidad y QR en `03_domains/`; NFC aún no verificado.                                  |
| Recuperación           | LostPets, Sightings, Safety, Chat, SearchCoordination, Broadcast, Locations                   | Flujos y APIs no equivalen a ciclo E2E probado; ficha de comunidad en `03_domains/`.                         |
| Salud y clínica        | Medical, Clinics, Certificates, CastrationCampaigns, AnimalWelfare                            | Acceso a información veterinaria está sujeto a consentimiento/permisos; video remoto no implementado.        |
| Dispositivos           | Collars, Locations                                                                            | TrackSolid/configuración de proveedor y hardware requieren validación operativa; ficha GPS en `03_domains/`. |
| Ecosistema B2B/B2G     | Allies, Fosters, Adoptions, Municipalities, Stores, ServiceProviders                          | Flujos, pagos y acuerdos institucionales poseen límites específicos.                                         |
| Monetización/operación | Subscriptions, Payments, Bundles, Promotions, Bounties, Imports, Notifications, Audit, Outbox | La existencia de un módulo no prueba transacción externa o oferta comercial aprobada.                        |

La solución agrupa estos módulos bajo proyectos comunes y un contexto de datos compartido; no hay evidencia de límites de despliegue independientes.
