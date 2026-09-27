# NALA AI Knowledge Hub

> Estado: infraestructura documental. Corte de evidencia: 2026-09-27. Este hub no implementa agentes ni funcionalidades de producto.

## Política oficial: NO OPINION WITHOUT EVIDENCE

Un agente no debe presentar una opinión, inferencia o recomendación como hecho. Toda afirmación factual sobre NALA debe incluir una ruta de repositorio, el fragmento o artefacto que la respalda, su fecha/corte si importa, y una prueba asociada cuando exista. Si no hay evidencia suficiente, usar `NO_VERIFICADO`, indicar qué dato falta y no completar el vacío con una suposición.

**Evidencia aceptable:** código ejecutable y sus llamadas; configuración activa; migraciones/esquema; pruebas ejecutadas con comando y resultado; infraestructura declarada (solo acredita intención/configuración); registro Git o documento versionado fechado; contrato de proveedor o evidencia operativa redactada, con acceso y fecha. Debe citarse la ruta relativa y distinguir evidencia de implementación de evidencia de producción.

**No es evidencia por sí sola:** una interfaz, mock, interfaz/DTO, endpoint aislado, `DbSet`, paquete instalado, TODO, roadmap, contrato comercial propuesto, documento sin fecha, configuración sin despliegue, skill no invocado, salida generada sin fuente, afirmación de marketing, recuerdo de una conversación o resultado de prueba no reproducible. Un documento puede ser evidencia de que una propuesta fue escrita, no de que el producto la implemente.

## Reglas de uso

1. Usar el código, las pruebas y la configuración más recientes antes que material histórico. Leer el estado Git y preservar cambios existentes.
2. Citar rutas relativas a la raíz del repositorio. Para capacidades, trazar entrada, lógica, persistencia, autorización, UI/integración y pruebas pertinentes; no generalizar más allá del alcance observado.
3. Separar `HECHO`, `HIPÓTESIS`, `PROPUESTA`, `ROADMAP` y `NO_VERIFICADO`. Los estados técnicos normalizados son `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` y `NO_VERIFICADO`.
4. No afirmar disponibilidad de producción, contrato, SLA, cumplimiento legal, precisión, costo o impacto sin evidencia operativa fechada y aprobada. IaC y registro DI no prueban despliegue.
5. Proteger datos personales, secretos, coordenadas exactas e información clínica. La IA puede ayudar a resumir o buscar; no diagnostica ni sustituye a profesionales veterinarios. Toda acción sensible requiere autorización y supervisión humana explícitas.
6. Antes de cambiar producto, seguridad, API o arquitectura, revisar las fuentes propietarias actuales y registrar impacto y evidencia. La existencia de un agente o skill no concede permisos para ejecutar acciones.

## Precedencia y fuentes

El hub enruta a las fuentes vigentes; no las reemplaza ni replica tablas completas. El código ejecutable y pruebas aprobadas definen comportamiento técnico; la fuente de estado/documentación y sus reportes delimitan el corte y las incertidumbres; estrategia, contratos comerciales y roadmaps describen propuestas.

La estructura detallada de este hub se organiza en `00_context.md`, `01_product/`, `02_architecture/`, `03_domains/`, `04_decisions/`, `05_integrations/`, `06_security/`, `07_releases/`, `08_prompts/`, `09_metrics/` y `10_agents/`.

Fuentes canónicas actuales: [alcance del producto](../docs/PRODUCT_SCOPE.md), [trazabilidad de capacidades](../docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md), [brechas documentales](../docs/auditoria/DOCUMENTATION_GAP_REPORT.md), [arquitectura](../docs/ARCHITECTURE.md), [API](../docs/API.md), [seguridad](../docs/SECURITY.md), [integraciones](../docs/INTEGRATIONS.md), [preparación IA](../docs/ai/AI_READINESS.md), [gobernanza IA](../docs/ai/AI_GOVERNANCE.md), [capacidades de negocio](../docs/business/BUSINESS_CAPABILITIES.md) y [plan de auditoría](../docs/auditoria/DOCUMENTATION_UPDATE_PLAN.md).

## Navegación del hub

- Contexto: [inventario técnico](02_architecture/repository-inventory.md) y [contexto breve](00_context.md).
- Producto: [visión](01_product/vision.md), [misión propuesta](01_product/mission.md), [alcance](01_product/scope.md), [roadmap](01_product/roadmap.md), [historia](01_product/history.md).
- Arquitectura: [arquitectura](02_architecture/architecture.md), [dominios](02_architecture/domains.md), [integraciones](02_architecture/integrations.md), [seguridad](02_architecture/security.md), [despliegue](02_architecture/deployment.md).
- Dominios: [mascotas](03_domains/pets.md), [identidad](03_domains/identity.md), [QR](03_domains/qr.md), [NFC](03_domains/nfc.md), [GPS](03_domains/gps.md), [salud](03_domains/health.md), [telemedicina](03_domains/telemedicine.md), [marketplace](03_domains/marketplace.md), [comunidad](03_domains/community.md), [municipal](03_domains/municipal.md), [proveedores](03_domains/providers.md), [suscripciones](03_domains/subscriptions.md).
- Decisiones: [registro ADR](04_decisions/README.md) y [índice ADR](../docs/adrs/ADR-INDEX.md).
- Integraciones/seguridad/releases: [catálogo de integraciones](05_integrations/INTEGRATION_CATALOG.md), [baseline de seguridad AI](06_security/SECURITY_BASELINE.md), [evidencia de releases](07_releases/RELEASE_EVIDENCE.md).
- Prompts: [arquitectura](08_prompts/architecture-review.prompt.md), [feature](08_prompts/feature-review.prompt.md), [seguridad](08_prompts/security-review.prompt.md), [GPS](08_prompts/gps-review.prompt.md), [telemedicina](08_prompts/telemedicine-review.prompt.md), [municipal](08_prompts/municipal-review.prompt.md), [release](08_prompts/release-review.prompt.md).
- Métricas: [AI readiness](09_metrics/AI_READINESS.md).
- Agentes: [catálogo](10_agents/AGENT_CATALOG.md), [matriz de interacción](10_agents/AGENT_INTERACTION_MATRIX.md) y [Documentation Guardian](10_agents/documentation-guardian.md).

Otros documentos derivados de este hub: [ontología](../docs/business/ONTOLOGY.md), [capacidades](../docs/business/CAPABILITIES.md), [estrategia](../docs/strategy/VISION.md), [posicionamiento](../docs/strategy/POSITIONING.md), [ventajas hipotéticas](../docs/strategy/COMPETITIVE_ADVANTAGES.md), [supuestos de mercado](../docs/strategy/MARKET_ASSUMPTIONS.md), [modelo de ingresos](../docs/strategy/REVENUE_MODEL.md), [privacidad](../docs/compliance/PRIVACY.md), [gobierno de datos](../docs/compliance/DATA_GOVERNANCE.md) y [registro de riesgos](../docs/compliance/RISK_REGISTER.md).

## Mantenimiento

Los documentos de este hub son vistas de navegación y síntesis con fecha/corte y enlaces a evidencia. Ante contradicción, registrar la discrepancia en el reporte de brechas y mantener ambas afirmaciones atribuibles hasta resolverla. Registrar los cambios documentales en [changelog](../docs/auditoria/DOCUMENTATION_CHANGELOG.md). No copiar secretos, PII ni expedientes a prompts, memoria o ejemplos.
