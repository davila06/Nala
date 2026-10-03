# NALA: estado real del producto

**Corte general:** 2026-09-28
**Revalidación focal de tiendas:** 2026-10-02
**Fuente primaria:** código, configuración, migraciones y pruebas del repositorio.  
**Alcance:** auditoría documental; no certifica producción, contratos ni proveedores externos.

## Resumen ejecutivo

NALA/PawTrack es un monolito modular con API ASP.NET Core, frontend React PWA, persistencia EF Core sobre SQL Server y una capa de infraestructura preparada para Azure. El código ya cubre un producto amplio de identificación, recuperación, salud, clínicas, adopciones, servicios, tiendas, pagos, cumplimiento y reportes institucionales. La amplitud no equivale a madurez operativa uniforme.

La capacidad más sólida es el núcleo de identidad y recuperación: mascotas, QR, perfiles, pérdida, avistamientos, protección de contacto, chat enmascarado y flujos de entrega. Salud y clínicas también tienen un alcance considerable. Las principales limitaciones son la operación real de proveedores externos, la telemedicina audiovisual, la liquidación comercial de marketplace, la cobertura completa de entitlements y las superficies de IA generativa.

## Composición técnica observada

| Superficie       | Realidad verificable                                                                                       | Evidencia                                                                                         |
| ---------------- | ---------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| Solución backend | API, Application, Domain, Infrastructure y dos proyectos de pruebas; `HashGen` existe fuera de la solución | [PawTrack.sln](../PawTrack.sln), proyectos en [backend](../backend)                               |
| API              | Controllers ASP.NET Core, autorización, rate limits, Problem Details/middleware y CQRS/MediatR             | [PawTrack.API](../backend/src/PawTrack.API), [Program.cs](../backend/src/PawTrack.API/Program.cs) |
| Aplicación       | Commands, queries, handlers, validadores y servicios por dominio                                           | [Application](../backend/src/PawTrack.Application)                                                |
| Dominio          | Entidades, invariantes, eventos y estados de negocio                                                       | [Domain](../backend/src/PawTrack.Domain)                                                          |
| Persistencia     | Un `PawTrackDbContext`, repositorios, configuraciones EF y migraciones en dos carpetas históricas          | [PawTrackDbContext.cs](../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs)   |
| Frontend         | React/TypeScript PWA, rutas por feature, React Query/Zustand y service worker                              | [routes.tsx](../frontend/src/app/routes.tsx), [frontend/src](../frontend/src)                     |
| Infraestructura  | Bicep para SQL, Storage, Key Vault, Container Apps, Front Door, red y observabilidad                       | [main.bicep](../infra/main.bicep)                                                                 |
| Pruebas          | Unitarias e integración; la ejecución registrada en este corte alcanzó 1,803 pruebas backend aprobadas     | [TESTING.md](TESTING.md)                                                                          |

## Capacidades actuales

### Identificación y recuperación

- Registro, edición, baja/reactivación y perfil público de mascotas.
- QR generado por servicio y superficies de visualización/escaneo.
- Microchip y datos sanitarios de identidad.
- Reporte de pérdida, estados, eventos, contacto protegido y recuperación.
- Reporte anónimo de avistamientos, fotografías, ubicación y limpieza de PII.
- Matching visual condicionado por configuración de Azure Vision.
- Chat enmascarado y códigos de entrega.
- Collar genérico, ingestión autenticada, historial, zonas, modo perdido y polling TrackSolid.

Evidencia principal: [PetsController.cs](../backend/src/PawTrack.API/Controllers/PetsController.cs), [LostPetsController.cs](../backend/src/PawTrack.API/Controllers/LostPetsController.cs), [SightingsController.cs](../backend/src/PawTrack.API/Controllers/SightingsController.cs), [CollarsController.cs](../backend/src/PawTrack.API/Controllers/CollarsController.cs).

### Salud y clínicas

Existen expediente, timeline, recordatorios, documentos, exportaciones, consultas clínicas administrativas, certificados/pasaportes, grants de acceso, veterinarios, organizaciones multi-sede, inventario, ventas, caja, CRM y reportes clínicos. El código no demuestra diagnóstico autónomo, firma clínica certificada, EHR externo ni video/audio remoto.

Evidencia: [MedicalController.cs](../backend/src/PawTrack.API/Controllers/MedicalController.cs), [ClinicsController.cs](../backend/src/PawTrack.API/Controllers/ClinicsController.cs), [CertificatesController.cs](../backend/src/PawTrack.API/Controllers/CertificatesController.cs).

### Comunidad, servicios y comercio

Adopciones, ferias, aliados, refugios, foster/custodia, proveedores, disponibilidad, reservas, tiendas, productos, pedidos, bundles y campañas existen como superficies de código. En tiendas hay stock escalar, reservas temporales y registro manual de reporte/verificacion de pago externo; no equivalen a kardex, POS, conciliacion bancaria ni marketplace liquidado. Comisiones/payouts no están aprobadas y no se demuestra checkout recurrente universal.

Evidencia: [AdoptionsController.cs](../backend/src/PawTrack.API/Controllers/AdoptionsController.cs), [ServiceProvidersController.cs](../backend/src/PawTrack.API/Controllers/ServiceProvidersController.cs), [StoreOrdersController.cs](../backend/src/PawTrack.API/Controllers/StoreOrdersController.cs), [ManualProviderPaymentGateway.cs](../backend/src/PawTrack.Application/ServiceProviders/Payments/ManualProviderPaymentGateway.cs).

### Monetización y seguridad

Hay planes, suscripciones, add-ons, entitlements, activación administrativa, cambios programados, pagos con tarjeta/SINPE, perfiles de pago, facturación, promociones y recompensas. La gestión de definiciones de entitlements, algunos gates y el medidor visible de consumo siguen incompletos. MFA, WebAuthn, trusted devices, rate limiting, auditoría, retención y exportación de datos tienen implementación, con límites de cobertura y operación externa documentados.

Evidencia: [SubscriptionsController.cs](../backend/src/PawTrack.API/Controllers/SubscriptionsController.cs), [EntitlementService.cs](../backend/src/PawTrack.Infrastructure/Subscriptions/EntitlementService.cs), [PaymentsController.cs](../backend/src/PawTrack.API/Controllers/PaymentsController.cs), [AuthController.cs](../backend/src/PawTrack.API/Controllers/AuthController.cs).

### IA e integraciones

La IA actualmente verificable es visual: embeddings/matching condicionados por Azure Vision y jobs de refresco. WhatsApp tiene sesiones de bot y canales de mensajería, pero esto no equivale a un copiloto clínico o agente autónomo. No se encontró RAG, chat generativo, evaluación de modelos, diagnóstico, predicción clínica ni integración ACS.

Las integraciones Azure, pagos, correo, mensajería y TrackSolid están preparadas en código/IaC, pero [INTEGRATIONS.md](INTEGRATIONS.md) las clasifica `NO_VERIFICADO` para operación real.

## No se puede afirmar desde el repositorio

- Despliegue productivo, disponibilidad, SLA, cobertura geográfica o usuarios activos.
- Contratos con municipalidades, SENASA, clínicas, refugios o proveedores.
- Aprobación legal, comercial, fiscal o de protección de datos.
- Precisión de matching, resultados veterinarios o recuperación atribuible a NALA.
- Operación de hardware GPS, pagos reales, entregas de mensajes o disponibilidad de Azure.

## Prioridades derivadas del estado real

1. Cerrar evidencia de proveedores y despliegue antes de vender capacidades dependientes de ellos.
2. Completar entitlements y reconciliar `FEATURES.md`, `PRODUCT_SCOPE.md` y pricing.
3. Convertir el marketplace de catálogo/reserva a operación con pagos, cancelaciones, payout y soporte.
4. Tratar telemedicina e IA generativa como productos nuevos con seguridad, consentimiento y evaluación, no como extensiones implícitas.
5. Usar Costa Rica como mercado de prueba para recuperación, salud documentada, clínicas y flujos institucionales antes de expansión regional.
