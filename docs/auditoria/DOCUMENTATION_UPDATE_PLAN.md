# Plan de actualización documental de NALA

**Corte:** 2026-09-27. **Estado:** corte documental completado con comprobaciones pendientes; no equivale a certificación exhaustiva de endpoints ni de producción. Este plan no acredita funcionalidades ni constituye una aprobación comercial o de despliegue.

## Inventario y cobertura pendiente

La solución [PawTrack.sln](../../PawTrack.sln) incluye seis proyectos: [API](../../backend/src/PawTrack.API/PawTrack.API.csproj), [Application](../../backend/src/PawTrack.Application/PawTrack.Application.csproj), [Domain](../../backend/src/PawTrack.Domain/PawTrack.Domain.csproj), [Infrastructure](../../backend/src/PawTrack.Infrastructure/PawTrack.Infrastructure.csproj), [UnitTests](../../backend/tests/PawTrack.UnitTests/PawTrack.UnitTests.csproj) e [IntegrationTests](../../backend/tests/PawTrack.IntegrationTests/PawTrack.IntegrationTests.csproj). También existe [HashGen](../../backend/HashGen/HashGen.csproj), fuera de la solución: se inspeccionará como herramienta auxiliar, sin atribuirle funcionalidad de producto. El frontend está definido en [package.json](../../frontend/package.json); la infraestructura declarada incluye [main.bicep](../../infra/main.bicep) y los workflows bajo [`.github/workflows`](../../.github/workflows/).

| Superficie                   | Verificación pendiente                                                                            |
| ---------------------------- | ------------------------------------------------------------------------------------------------- |
| API                          | Registro de rutas, controllers, contratos, middlewares y autorización efectiva.                   |
| Application                  | Commands, queries, handlers, validación, reglas y servicios registrados.                          |
| Domain                       | Entidades, estados, invariantes y dependencias.                                                   |
| Infrastructure               | EF Core, migraciones, jobs, adaptadores, configuración e integraciones.                           |
| UnitTests / IntegrationTests | Cobertura pertinente, condiciones de ejecución y resultados.                                      |
| HashGen                      | Propósito, límites y relación con la aplicación.                                                  |
| Frontend                     | Rutas, componentes, clientes HTTP, flags y correspondencia con la API.                            |
| Infraestructura y CI         | Recursos declarados frente a servicios realmente utilizados; despliegue real no inferible de IaC. |
| Documentación                | Índice, contratos vigentes, históricos, enlaces y afirmaciones que requieren contraste.           |

Los seis proyectos de la solución y HashGen se inventariaron; API y frontend se inspeccionaron por módulos, pero no se verificó cada método de los 68 controllers con pruebas de integración. Cobertura y excepciones actualizadas en [TESTING](../TESTING.md), [catálogo API](../api/ENDPOINT_CATALOG.md) y [brechas](DOCUMENTATION_GAP_REPORT.md).

Durante la revisión surgieron ediciones concurrentes en código médico y pruebas ([MedicalController](../../backend/src/PawTrack.API/Controllers/MedicalController.cs), [MedicalEndpointsTests](../../backend/tests/PawTrack.IntegrationTests/Medical/MedicalEndpointsTests.cs)); se conservaron. La verificación ejecutada es de un estado anterior a esas ediciones y no las acredita ([TESTING](../TESTING.md)).

## Fuentes y conflictos iniciales

Prioridad: código ejecutable, configuración y migraciones, pruebas, infraestructura y por último documentación. [README.md](../README.md) sitúa [STATUS.md](../STATUS.md) como estado técnico y [FEATURES.md](../FEATURES.md) como matriz de planes; este último se declara expresamente «contrato funcional propuesto». No se convertirán sus tablas en afirmaciones de producto disponible sin comprobar enforcement y pruebas. El índice existente tiene información histórica útil; no se reemplazará silenciosamente. Cualquier contradicción concreta encontrada se registrará en `DOCUMENTATION_GAP_REPORT.md` con rutas antes de corregirla.

## Orden de trabajo

1. `nala-feature-inventory`: reconstruir capacidades y trazabilidad de extremo a extremo; clasificar con los ocho estados estándar.
2. `nala-architecture-review`: validar composición, referencias, datos, despliegue y observabilidad.
3. `nala-security-audit`: contrastar autenticación, ownership, acceso, privacidad y dependencias.
4. `nala-api-documenter`: inventariar rutas activas, contratos y errores.
5. `nala-subscription-plans`: comprobar planes, cuotas y enforcement, separando contrato propuesto de implementación.
6. Skills especializados solo donde haya implementación o documentación relevante: dominios de recuperación, salud, collares/GPS, clínica/telemedicina, B2B, marketplace, institucional y crecimiento; registrar exclusiones y evidencia.
7. `nala-ai-readiness`: distinguir funciones activas de propuestas y examinar límites de seguridad.
8. `nala-business-analyst`: traducir estados ya establecidos a capacidades y valor sin elevar propuestas a disponibilidad.
9. Consolidación por `nala-product-auditor`, verificación cruzada y changelog documental.

## Documentos previstos y propiedad

Se preservarán [README.md](../README.md), [STATUS.md](../STATUS.md), [FEATURES.md](../FEATURES.md) y documentos existentes, actualizando secciones puntuales cuando la evidencia lo justifique. El orquestador creará o actualizará `docs/PRODUCT_SCOPE.md`, `docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md`, `docs/auditoria/DOCUMENTATION_GAP_REPORT.md` y `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; inventario se encargará de `docs/FEATURES.md` (sin borrar su contrato propuesto), arquitectura de `docs/ARCHITECTURE.md` y `docs/architecture/`, seguridad de `docs/SECURITY.md` y `docs/security/`, API de `docs/API.md` y `docs/api/`, planes de `docs/commercial/`, dominios pertinentes de `docs/domains/`, `docs/editions/` o `docs/growth/`, IA de `docs/ai/` y negocio de `docs/business/`. No se archivará un documento sin demostrar que ha sido reemplazado; de ocurrir se preservará en `docs/archive/`.

## Verificación, riesgos y criterios de cierre

Cuando sea seguro, ejecutar restauración, compilación, pruebas y análisis de dependencias sobre la solución, más pruebas del frontend; evitar pruebas que requieran servicios externos, secretos, migraciones o despliegues. Registrar comando, resultado y alcance exactos. No interpretar ausencia de errores de compilación como verificación funcional. Revisar enlaces relativos, presencia de evidencia para cada afirmación, clasificación y conteos, consistencia estado actual/roadmap, secretos, cambios ajenos y diff de `docs/` completo. Riesgos iniciales: amplitud de dominios, contratos propuestos mezclados con productos vigentes e imposibilidad de inferir disponibilidad en producción de la configuración local; hasta verificarlos usar `NO_VERIFICADO`.

**Resultado del corte:** build solución correcto, 1621 unitarias backend aprobadas, typecheck frontend correcto, Vitest 142/143 con un fallo en caja clínica, NuGet sin vulnerabilidades reportadas; [TESTING](../TESTING.md). Integración SQL/Blob, proveedor, E2E, despliegue, todos los contratos API y verificaciones remotas CI quedan abiertos. No se actualizó código productivo.

## Iniciativa de conocimiento y gobierno AI (2026-09-27)

Esta iniciativa amplía el índice de conocimiento sin reemplazar los documentos canónicos existentes. `/ai` funciona como hub para agentes y debe enlazar a `docs/ai/`, `docs/business/`, `docs/architecture/`, `docs/domains/`, `docs/security/` y `docs/auditoria/` cuando esas fuentes ya existan. No se reescriben documentos concurrentes ni se modifica código productivo.

| Entregable                                                 | Evidencia base y límite                                                                                                                                                                                          |
| ---------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Contexto, producto, arquitectura, dominios e integraciones | `PawTrack.sln`, proyectos backend/frontend, `infra/` y documentación activa. El inventario no certifica despliegue ni operación externa.                                                                         |
| Memoria del producto                                       | Git inicial fechado 2026-04-10, changelog y documentos fechados; incluir solo hitos que tengan evidencia versionada y señalar fechas históricas como aproximadas cuando la fuente no sea un registro de release. |
| ADRs                                                       | Siete decisiones narradas en `docs/Manuales/MANUAL_TECNICO.md`; trasladarlas como `DRAFT` rastreables, no como decisiones aprobadas, porque no hay registro formal que acredite aprobación.                      |
| Agentes y skills                                           | Inventario local de `.github/skills/` y `.agents/skills/`; un skill describe un flujo/restricción, no demuestra que su auditoría haya sido ejecutada.                                                            |
| Ontología, capacidades, estrategia y compliance            | Resumir fuentes existentes; separar implementación, contrato objetivo, hipótesis, propuesta y pendiente de validación legal/comercial.                                                                           |
| Métricas AI                                                | Score cualitativo 0-100 con criterios, rutas y limitaciones explícitas; no presentar como métrica automática ni como benchmark.                                                                                  |

Validación: conservar el árbol sucio previo; revisar solo los archivos creados para rutas relativas y marcadores de estado; no ejecutar tests de producto ni despliegues para una entrega exclusivamente documental. Reportar cobertura observada, áreas no verificadas y el nivel de preparación como evaluación documental del corte, no certificación.

**Resultado de esta iniciativa (2026-09-27):** creado `ai/` con 40 documentos Markdown, siete ADR `DRAFT`, doce fichas de dominio y siete prompts; añadidos dos documentos de negocio, cinco de estrategia y tres de compliance. Actualizados el índice `docs/README.md`, `docs/auditoria/DOCUMENTATION_CHANGELOG.md`, este plan y `.github/copilot-instructions.md`. La navegación local se verificó en el corpus y el scorecard registrado es 63/100 cualitativo. No se ejecutaron pruebas de producto, migraciones ni despliegues; los cambios médicos concurrentes no fueron validados.

## Revalidación focal de FEATURES §24 (2026-09-28)

Esta ampliación corrige el estado de los pendientes de entitlements; no reabre ni certifica la auditoría completa del producto.

| Superficie         | Cobertura comprobada                                                                                                    | Límite                                                                                                                 |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Application/Domain | Servicio, modelos, fallback y consumidores de `AuthorizeAsync`/`ConsumeAsync` inspeccionados.                           | La matriz de límites no está completamente migrada a definiciones persistidas.                                         |
| Infrastructure     | Repositorios, configuraciones y migraciones de `PlanEntitlements`, consumos, add-ons y `BroadcastRunId` inspeccionados. | No se verificó aplicación en una base compartida/producción.                                                           |
| API                | Snapshot autenticado y catálogo público de planes presentes; CRUD administrativo gestiona planes/precios.               | No se encontró CRUD administrativo para editar `PlanEntitlement`; la API de catálogo no acredita aprobación comercial. |
| Frontend           | API/hook para catálogo y snapshot presentes; `EntitlementMeter` existe.                                                 | No se encontró call site de `EntitlementMeter` en `frontend/src`; no se acredita medidor visible ni alerta de umbral.  |
| Tests              | `dotnet test PawTrack.sln` aprobó 1,803 pruebas en este corte.                                                          | Los tests no acreditan todas las claves, combinaciones de plan/add-on, concurrencia relacional ni rollout externo.     |
| Legal/comercial    | [Registro legal](../LEGAL_REVIEW_REGISTER.md) y [precios](../PRICING_AND_PLANS.md) revisados como fuentes.              | Contratos/privacidad y aprobación de oferta permanecen `draft`/pendientes.                                             |

Resolución: actualizar focalmente §24, este plan, el reporte de brechas y el changelog. Mantener como parcial la gestión de definiciones, la sustitución de fallbacks, los gates de cuotas no migrados, los medidores y las pruebas de cambios de plan; no inferir aprobación comercial/legal ni despliegue.

## Auditoría de alcance real solicitada (2026-09-28)

Este corte reemplaza la lectura histórica por evidencia actual del repositorio.
Se inspeccionaron los seis proyectos de `PawTrack.sln`, `backend/HashGen`,
`frontend`, `infra`, workflows y los módulos de Controllers, Application,
Domain, Infrastructure, migraciones, configuración y pruebas. La auditoría no
ejecuta despliegues, migraciones ni llamadas a proveedores externos; por tanto,
un adaptador Azure registrado no acredita operación en Azure.

| Capacidad                    | Estado provisional por evidencia de código                                                                    | Evidencia principal                                                                                               | Límite que debe conservarse                                               |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| Identificación QR            | `IMPLEMENTADO_Y_VERIFICADO`                                                                                   | `PetsController`, `GetPetQrCodeQuery`, `QrCodeService`, `QrCodeDisplay`, pruebas QR                               | No acredita QR físico ni operación de Blob real                           |
| NFC                          | `PROPUESTO`                                                                                                   | `ai/03_domains/nfc.md`, estrategia; sin controlador, servicio o UI NFC                                            | No hay lectura, vinculación ni persistencia NFC                           |
| GPS                          | `IMPLEMENTADO_Y_VERIFICADO` para collar/API; `NO_VERIFICADO` para hardware/proveedor                          | módulo `Collars`, `TrackSolidService`, polling, UI de collar, pruebas unitarias/E2E                               | No acredita hardware, SLA, cobertura ni TrackSolid operativo              |
| Telemedicina                 | `DECLARADO_NO_IMPLEMENTADO` para video/audio; registro clínico presencial parcial                             | `ClinicalConsultation`, `ClinicsController`, limitaciones de telemedicina                                         | No hay sesión audiovisual ni ACS                                          |
| Expediente veterinario       | `IMPLEMENTADO_Y_VERIFICADO`                                                                                   | módulo Medical, endpoints paginados, adjuntos protegidos, PDF/export, pruebas unitarias/integración/UI ejecutadas | No es EHR externo ni firma clínica certificada                            |
| Recordatorios de salud       | `IMPLEMENTADO_Y_VERIFICADO` para consulta/mutaciones; job y proveedor externo limitados                       | `VetReminder`, `HealthAlertHostedService`, protocolos, MedicalController, pruebas                                 | No diagnostica ni acredita resultados veterinarios                        |
| Suscripciones                | `IMPLEMENTADO_Y_VERIFICADO` para catálogo, activación y gates existentes; cuotas aún parciales                | `SubscriptionsController`, `EntitlementService`, persistencia, jobs y pruebas                                     | No acredita oferta comercial ni todos los entitlements                    |
| Marketplace                  | `IMPLEMENTADO_Y_VERIFICADO` para directorios, servicios y reservas; pagos/inventario parcial                  | `StoresController`, `ServiceProvidersController`, `ProviderBookingsController`, frontend y pruebas                | Pedidos/reservas no prueban pago liquidado ni inventario real             |
| IA                           | `IMPLEMENTADO_Y_VERIFICADO` para validación/matching visual condicionado; copiloto/RAG/agente no implementado | `AzureVisionEmbeddingService`, visual match, configuración y pruebas                                              | No acredita precisión, autonomía, diagnóstico ni despliegue Azure         |
| Municipalidades              | `IMPLEMENTADO_Y_VERIFICADO` para perfiles, capturas y reportes internos                                       | `MunicipalController`, Application/Domain Municipalities, pruebas                                                 | No acredita integración oficial con autoridades                           |
| Refugios                     | `IMPLEMENTADO_Y_VERIFICADO` para aliados/refugios, publicaciones y adopción                                   | `AlliesController`, `AdoptionsController`, perfiles/repositorios, pruebas                                         | No acredita operación de una ONG real ni SLA                              |
| Veterinarias                 | `IMPLEMENTADO_Y_VERIFICADO` para superficies clínicas registradas y pruebas ejecutadas                        | `ClinicsController`, módulo Clinics, frontend clinic, matrices HTTP                                               | Telemedicina audiovisual y proveedores fiscales externos siguen limitados |
| Integraciones Azure          | `IMPLEMENTADO_SIN_PRUEBAS` como adaptadores/configuración; operación `NO_VERIFICADO`                          | Blob, SQL, Key Vault, App Insights, Maps, Vision, Container Apps/infra                                            | IaC/SDK/config no demuestra despliegue ni credenciales válidas            |
| Azure Communication Services | `DECLARADO_NO_IMPLEMENTADO`                                                                                   | ausencia de `Azure.Communication.*`, clientes, configuración y rutas                                              | No confundir `CallClient` de CRM con ACS                                  |
| Dynamics 365                 | `DECLARADO_NO_IMPLEMENTADO`                                                                                   | ausencia de SDK, endpoints, configuración y conectores                                                            | CRM propio no es Dynamics 365                                             |
| Power Platform               | `DECLARADO_NO_IMPLEMENTADO`                                                                                   | ausencia de Dataverse, Power Automate, connectors o configuración                                                 | Hosted services propios no son Power Platform                             |

## Entregables y validación de este corte

1. Actualizar `docs/FEATURES.md` para que deje de ser una matriz de producto
   futuro y separe alcance actual, parcial, propuesto y no implementado.
2. Actualizar `docs/PRODUCT_SCOPE.md`, `docs/INTEGRATIONS.md`,
   `docs/KNOWN_LIMITATIONS.md` y `docs/TESTING.md` con las mismas clasificaciones.
3. Reemplazar las filas pertinentes de `docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md`
   y registrar contradicciones en `DOCUMENTATION_GAP_REPORT.md`.
4. Registrar el corte en `DOCUMENTATION_CHANGELOG.md` y revisar `docs/README.md`
   para que enlace los documentos canónicos.

La validación prevista es `dotnet build`, `dotnet test` de la solución,
`npm run typecheck`, `npm test`, revisión de enlaces internos y `git diff
--check`. La ejecución local no certifica staging, producción, proveedores
externos, Azure Communication Services, Dynamics 365 ni Power Platform.

**Resultado del corte:** 7 capacidades `IMPLEMENTADO_Y_VERIFICADO`, 1
`IMPLEMENTADO_SIN_PRUEBAS` (adaptadores Azure), 3 `PARCIALMENTE_IMPLEMENTADO`,
1 `PROPUESTO` y 4 `DECLARADO_NO_IMPLEMENTADO`. La cifra de 7 no convierte
proveedores, hardware o despliegues en operación verificada; esos límites están
descritos por separado en la matriz.

## Ampliación: estrategia de planes, precios y unit economics (2026-09-28)

Esta sección cubre la solicitud de inventariar qué puede vender NALA y qué
debería vender. No reabre ni reemplaza las secciones previas. Los nuevos
documentos solicitados no existían al iniciar esta ampliación. El último commit
contiene informes estratégicos de nombre parecido, pero éstos no sustituyen los
entregables específicos enumerados abajo.

### Inventario y cobertura

| Proyecto/superficie             | Cobertura comprobada                                                                                                                                                             | Límite                                                                                                                                                   |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Solución .NET                   | 6 proyectos en `PawTrack.sln`: API, Application, Domain, Infrastructure, UnitTests, IntegrationTests                                                                             | `backend/HashGen` existe fuera de la solución y no se considera una función de producto                                                                  |
| API/backend                     | 68 archivos controller, 23 hosted services, módulos de Application y Domain; suscripciones, pagos, IA visual, adopciones, salud, GPS, clínicas, proveedores, tiendas y municipal | Inventario funcional amplio por módulos; no se verificará cada handler, permiso y ruta con prueba individual                                             |
| Persistencia                    | DbContext, repositorios, migraciones y seeds inspeccionados en rutas relevantes                                                                                                  | La base local/producción y el catálogo activo de entitlements no se consultaron; una migración/seed no prueba contenido desplegado                       |
| Frontend                        | React 19/TypeScript PWA; rutas/features de mascotas, familia, salud, clínicas, tiendas, refugios, municipal, pagos y administración inspeccionadas por búsqueda                  | La UI no acredita enforcement backend ni uso real; `EntitlementMeter` existe, pero no se encontró call site en búsqueda actual                           |
| Infraestructura e integraciones | `infra/main.bicep`, Docker Compose y workbook de observabilidad; despliegue de SQL, Storage, Key Vault, Container Apps, Front Door y App Insights declarado                      | No se inspeccionaron recursos desplegados ni facturas; no hay proyecto `host.json`/Azure Functions encontrado                                            |
| Pruebas                         | 406 archivos de pruebas C# contabilizados; solución cubre UnitTests e IntegrationTests                                                                                           | Los conteos de pruebas de documentos anteriores son históricos hasta ejecutar tareas actuales                                                            |
| Documentación                   | 204 archivos Markdown en `docs/`; leídos documentos de estado real, capacidades, precios, límites, plan, auditoría y competencia relevantes                                      | Para el inventario documental, el estado se asignará por afirmación/evidencia; no se afirmará revisión semántica línea por línea de los 204 si no ocurre |

### Contradicciones y fuentes de corte

- Los tiers codificados en `SubscriptionTier.cs` son `Free`, `UserPlus`,
  `UserFamilia`, `ClinicBasic/Plus/Partner`, `StoreBasic/Plus/Partner`,
  `ShelterBasic/Plus`, `MuniBasica`, `MuniFull` y `MuniRedRegional`; no existe
  una familia actual `Essential/Premium/Family/Veterinary/Enterprise` completa.
- `SubscriptionPricing.cs` contiene precios CRC de referencia, plazos 1/3/6/12
  meses y descuento anual del 20%; `PRICING_AND_PLANS.md` declara que venta y
  aprobación comercial/legal siguen condicionadas. El código no acredita precio
  cobrado, conversión ni disposición a pagar.
- `EntitlementService.cs` combina entitlements libres, definiciones persistidas
  y fallbacks legacy. Migración `20260921203255_AddEntitlementCatalogAndConsumption`
  y fallbacks coinciden en `UserFamilia.MaxPets=25`; el término “ilimitado” de
  algunos documentos es incorrecto como límite técnico.
- El catálogo/fallback no prueba cobertura de cada gate por plan, ni se observó
  un CRUD completo de definiciones. Cada fila distinguirá plan actual de
  recomendación y señalará `NO_VERIFICADO` donde no haya enforcement trazable.
- Los datos comerciales de conversión, churn, CAC, costo de soporte, GMV,
  consumo Azure y volumen de media/SMS/email no están disponibles en el código o
  documentos de corte. Las proyecciones serán escenarios con supuestos visibles,
  no resultados ni pronósticos observados.
- Azure Retail Pricing devolvió respuesta correcta sin filas para el SKU
  `GP_S_Gen5_1` en `eastus`; no hay región de despliegue confirmada. Costos Azure
  se documentarán como fórmula/escenario a validar, no como factura cotizada.
- La lectura pública a 2026-09-28 recuperó datos de páginas oficiales de
  Tractive, Pawfit, PetHub, Vetster, PetDesk, Digitail, Rover, Wag, Pumpkin y
  Figo. Fi/Whistle redirigen, mientras que Dutch, Airvet, PetPage, Banfield y
  Chewy no dieron contenido útil en esta captura; sus capacidades no se
  inferirán.

### Documentos a crear

1. `docs/NALA_FEATURE_INVENTORY.md`
2. `docs/NALA_PLAN_MAPPING.md`
3. `docs/NALA_PLAN_VALUE_ANALYSIS.md`
4. `docs/NALA_COMPETITIVE_PLAN_ANALYSIS.md`
5. `docs/NALA_CR_PRICING.md`
6. `docs/NALA_LATAM_PRICING.md`
7. `docs/NALA_ROADMAP_MONETIZATION.md`
8. `docs/NALA_UNIT_ECONOMICS.md`
9. `docs/NALA_DOCUMENTATION_AUDIT.md`
10. `docs/NALA_PRICING_EXECUTIVE_SUMMARY.md`
11. `docs/NALA_FEATURE_PLAN_UPSELL_MATRIX.md`

### Orden, riesgos y validación

1. Cerrar el inventario técnico y la matriz de tiers actuales, citando rutas de
   código, migraciones, UI, tests y configuración. No usar la propuesta de
   `FEATURES.md` como implementación.
2. Puntuar el valor como juicio heurístico, comparar competidores solo con
   fuentes públicas recuperadas y marcar inaccesibles como `NO_VERIFICADO`.
3. Formular precios CR/LATAM y upsells como hipótesis para pruebas de precio,
   incluyendo moneda, impuestos, alcance y dependencias locales; no como
   aprobación comercial.
4. Modelar escenarios de ingresos solo a partir de volúmenes asumidos y precios
   explícitos; separar coste fijo, variable, margen, CAC, LTV y break-even que
   no se pueden estimar sin telemetría, facturas y cohortes.
5. Auditar los 204 Markdown por inventario y priorizar los documentos de planes,
   pricing, negocio y capacidades; señalar los que requieren lectura/validación
   de propietario sin etiquetarlos obsoletos por antigüedad solamente.
6. Validar formato, enlaces internos, referencias reales al código, coherencia de
   nombres/tiers, etiquetas de disponibilidad, claims y `git diff --check`.
   Ejecutar pruebas backend/frontend seguras y registrar salida exacta.

Riesgos: amplitud del inventario; entitlements variables por datos de DB;
precios fiscales y disponibilidad comercial pendientes; páginas de competidores
no accesibles; región Azure y volúmenes desconocidos; ausencia de CAC/LTV,
retención y costos reales. Un precio recomendado será una hipótesis de
experimentación, no el “precio óptimo” probado. No se borrarán ni reemplazarán
documentos existentes en esta ampliación.

## Entregables estratégicos completados (2026-09-28)

Se crearon los informes solicitados de estado real, mapa de capacidades,
competencia, Costa Rica, LATAM, visión global, IA, monetización, auditoría
documental y resumen ejecutivo. Todos indican fecha de corte y separan hechos
del repositorio, límites de evidencia y recomendaciones. La comparación de
competidores es una hipótesis estratégica y requiere investigación primaria
vigente por país, plan y fecha antes de convertirse en claim comercial.

La cobertura no certifica cada endpoint con pruebas autenticadas BOLA, ningún
despliegue ni proveedor externo. No se eliminaron archivos históricos y no se
modificó código productivo.

## Entregables solicitados: planes, pricing y upsell (2026-09-28)

Se crearon los 11 entregables específicos pedidos: `NALA_FEATURE_INVENTORY.md`,
`NALA_PLAN_MAPPING.md`, `NALA_PLAN_VALUE_ANALYSIS.md`,
`NALA_COMPETITIVE_PLAN_ANALYSIS.md`, `NALA_CR_PRICING.md`,
`NALA_LATAM_PRICING.md`, `NALA_ROADMAP_MONETIZATION.md`,
`NALA_UNIT_ECONOMICS.md`, `NALA_DOCUMENTATION_AUDIT.md`,
`NALA_PRICING_EXECUTIVE_SUMMARY.md` y
`NALA_FEATURE_PLAN_UPSELL_MATRIX.md`. Se actualizaron `README.md`,
`PRICING_AND_PLANS.md` (corrección de “ilimitadas” a 25 mascotas) y este plan;
se registran cambios en `auditoria/DOCUMENTATION_CHANGELOG.md`.

La matriz maestra es el punto de entrada de pricing/upsell, no autoridad sobre
gates. La auditoría del corpus inventarió 204 Markdown pero clasifica como
`SIN EVIDENCIA` los documentos fuera de la lectura de pricing/producto de esta
ampliación; no declara auditoría semántica línea por línea de los 204. No se
borraron ni archivaron documentos existentes. La comparación de competidores y
las bandas de precios son propuestas apoyadas por páginas públicas recuperadas y
un indicador WB PPA; no son estudio local de disposición a pagar.

La tarea VS Code `test-backend` terminó con código de salida 0; no quedó
recuperable el resumen de conteos, por lo que no se registran cantidades por
proyecto ni cobertura por feature. Azure Retail Pricing para `GP_S_Gen5_1` en
East US respondió sin filas; costos, CAC, retención, LTV, margen y break-even
efectivos continúan `NO_VERIFICADO`.

Revalidación: `Test-GoLiveGovernance.ps1` se ejecuta en el workflow de
despliegue productivo antes de crear/publicar artefactos. No se encontró una
comprobación de esas variables en endpoints de compra, catálogo público o UI;
el estado de las variables productivas no se consultó. El trial Verified se
confirmó en `ServiceProvider.Activate()`: 30 días en la primera aprobación, sin
reinicio al reactivar.

## Auditoría y actualización de contenido del landing (2026-10-01)

### Alcance y fuentes

El corte es una revisión dirigida al contenido público de `landing/frontend`,
no una auditoría semántica de todos los Markdown de `docs/`. Se revisaron las
fuentes canónicas de [alcance](../PRODUCT_SCOPE.md), [trazabilidad](FEATURE_TRACEABILITY_MATRIX.md),
[brechas](DOCUMENTATION_GAP_REPORT.md), [pruebas](../TESTING.md), [estado real](../NALA_ACTUAL_PRODUCT_STATE.md),
[mapa de capacidades](../NALA_CAPABILITY_MAP.md), [planes](../PRICING_AND_PLANS.md),
[registro legal](../LEGAL_REVIEW_REGISTER.md), [claims](../CLAIM_EVIDENCE_MATRIX.md),
[seguridad](../SECURITY.md), [integraciones](../INTEGRATIONS.md), [estrategia](../PRODUCT_STRATEGY_TOP1.md),
[UI/UX](../UIUX_EXECUTIVE_SUMMARY.md), [dependencias externas](../discovery/EXTERNAL_DEPENDENCY_GRAPH.md),
y código de rutas de perfiles, pérdida, hallazgo, contacto y configuración NFC.
También se inspeccionaron las páginas, contenido editorial, metadata y CTA del
landing.

### Discrepancias a resolver en la superficie pública

| Prioridad | Discrepancia                                                                                                                                                                                                         | Acción planeada                                                                                                                          | Límite                                                                                                                |
| --------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| P0        | El copy promete contacto protegido/mediado, pero `GET /api/lost-pets/{id}/contact` devuelve nombre y teléfono del caso activo a cualquier cuenta autenticada; existe rate limit y prueba que codifica esta decisión. | Quitar promesas de teléfono oculto/relay y documentar revisión de consentimiento/abuso antes de publicar claims de protección.           | No cambiar el backend ni afirmar acceso anónimo; elevar decisión al propietario de seguridad/producto.                |
| P1        | NFC aparece como función integrada. El código solo guía escritura manual con NFC Tools; un tipo de bundle existe, pero lectura nativa, disponibilidad física y fulfillment no están verificados.                     | Distinguir NFC como etiqueta configurable externa, no GPS ni pairing nativo; señalar disponibilidad no verificada.                       | No afirmar que el bundle se vende/entrega.                                                                            |
| P1        | El landing enumera Esencial/Plus/Premium/Familiar y productos “activos”. No coincide con tiers técnicos y aprobación comercial/legal pendiente.                                                                      | Retirar paquetes inventados; indicar que no hay precios/planes aprobados para contratar desde la landing.                                | No copiar precios internos ni presentar tiers de código como oferta.                                                  |
| P1        | Telemedicina, tiendas/marketplace, aliados, community y municipalidad se describen como servicios que podrían interpretarse disponibles o verificados.                                                               | Etiquetar cada superficie como propuesta, parcial o no verificada usando `PRODUCT_SCOPE` y la matriz.                                    | No afirmar cobertura, alianzas, soporte, SLA, pago liquidado ni pilotos activos.                                      |
| P1        | “Núcleo de Animal de Localización y Asistencia” indicado por el solicitante difiere de la expansión usada en el login; términos y privacidad dejan la relación jurídica de PawTrack CR/NALA pendiente.               | Usar la expansión solicitada solo en el landing y registrar la discrepancia de marca/legal.                                              | Requiere confirmación del responsable de marca y asesoría legal antes de publicar como razón social/marca registrada. |
| P2        | Blog, privacidad, contacto y accesibilidad contienen avisos obsoletos o acciones que parecen activas.                                                                                                                | Corregir a la existencia real de artículos, controles de exportación/retención, canales no habilitados y ausencia de certificación WCAG. | Implementación de controles no equivale a política aprobada ni cumplimiento legal.                                    |

### Entregables y verificación

1. Crear `docs/landing/LANDING_CONTENT_AUDIT.md` con evidencia por página,
   estado de claims, textos propuestos, prioridades y decisiones de aprobación.
2. Actualizar únicamente copy, enlaces y metadatos del landing que puedan
   respaldarse; no cambiar contratos/API, reglas de negocio, backend,
   configuración legal ni operación externa.
3. Enlazar la auditoría desde `docs/README.md` y registrar el corte en
   `DOCUMENTATION_CHANGELOG.md`.
4. Ejecutar lint, tests y build de `landing/frontend`; revisar páginas
   exportadas, sitemap, enlaces internos, claims residuales y `git diff --check`.

No se ha inspeccionado producción, estado real de tiendas/proveedores, contratos,
aprobaciones de marca, análisis legal, ni ejecución reciente de CI remota. La
fecha de los documentos técnicos citados varía; el estado técnico se toma del
corte vigente 2026-09-28 y la auditoría de landing de 2026-10-01.
