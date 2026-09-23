# NALA - Azure Marketplace Go-to-Market y arquitectura de oferta

> Estado: `draft` hasta aprobación comercial, legal y de Azure. Corte:
> 2026-09-23.

## 1. Decisión estratégica

Azure Marketplace debe ser un canal B2B/B2G para vender **NALA Recovery & Care
Cloud**, no el canal principal del plan consumidor. La oferta inicial debe
resolver un problema comprable por una organización:

- identidad y recuperación de mascotas para municipalidades, refugios, clínicas,
  aseguradoras y redes de bienestar;
- API, portal operativo y automatización IA con datos segregados por tenant;
- despliegue Azure-native, soporte empresarial, auditoría y métricas de outcome.

La oferta no debe anunciar integración Marketplace hasta que exista una oferta
publicada, un flujo de activación probado y evidencia de transacciones en un
tenant de prueba.

## 2. Producto recomendado para la primera oferta

**Nombre de trabajo:** NALA Recovery & Care Cloud.

**Tipo:** SaaS offer en Microsoft Marketplace, con planes privados para cuentas
ancla y facturación transaccionada por suscripción. Los planes B2C siguen fuera
del Marketplace en la primera fase.

**Cliente ideal inicial:** municipalidad o red de refugios/clínicas que necesita
un sistema de recuperación, identidad y coordinación sin construir su propia
plataforma.

**Unidades comerciales posibles:**

- tenant/organización por mes;
- número de mascotas activas protegidas;
- paquete de automatizaciones IA o matching, con límites transparentes;
- módulos opcionales de API, analytics, soporte y conectores.

No activar metering por evento hasta que la definición de consumo, idempotencia,
reconciliación y soporte financiero estén probados. Un precio simple por tenant
reduce el riesgo del primer lanzamiento.

## 3. Arquitectura de integración

```text
Partner Center / Azure Marketplace
          |
          | purchase + subscription lifecycle
          v
NALA Marketplace Fulfillment API
          |
          +--> tenant provisioning / Entra mapping
          +--> plan and entitlement activation
          +--> suspend / reinstate / cancel
          +--> audit + correlation IDs
          |
NALA API + Azure SQL + Blob + Key Vault + Container Apps
          |
          +--> Application Insights / Log Analytics
          +--> AI services with tenant-safe data boundaries
```

### Requisitos técnicos

- Landing page HTTPS estable y capaz de recibir el token de compra.
- Resolución idempotente del plan y `subscriptionId` de Marketplace a un tenant
  NALA.
- Fulfillment API para activar, actualizar, suspender, reactivar y cancelar.
- Autenticación server-to-server con credenciales almacenadas en Key Vault.
- Correlation ID por operación y auditoría sin secretos ni PII innecesaria.
- Mapeo explícito: Marketplace subscription -> NALA tenant -> plan -> entitlements.
- Separación entre tenant comprador, usuarios finales y organizaciones aliadas.
- Reconciliación periódica contra el estado de Marketplace.
- Estados de degradación documentados si Marketplace o la API de fulfillment no
  están disponibles.
- Feature flags para no habilitar módulos no incluidos en el plan comprado.
- Medición y soporte de moneda, impuestos, cancelación, renovación y prorrateo
  según el contrato aprobado.

## 4. Checklist de preparación

### Cuenta y publicación

- [ ] Enrolar publisher en Microsoft Marketplace.
- [ ] Aceptar Microsoft Publisher Agreement.
- [ ] Completar perfil fiscal y de payout.
- [ ] Definir publisher, marca, categorías, regiones y mercados.
- [ ] Preparar logo, screenshots, video, descripción, privacidad, términos,
      soporte y arquitectura.
- [ ] Definir audience pública, privada y planes de prueba.
- [ ] Nombrar responsable de certificación y responsable operativo.

### Oferta SaaS

- [ ] Elegir plan inicial por tenant y límites incluidos.
- [ ] Definir landing page y onboarding de tenant.
- [ ] Implementar fulfillment lifecycle con reintentos e idempotencia.
- [ ] Implementar cancelación, suspensión, reactivación y downgrade.
- [ ] Implementar reconciliación y alertas de discrepancias.
- [ ] Implementar metering solo si el modelo comercial lo requiere.
- [ ] Probar sandbox/test customer de punta a punta.
- [ ] Verificar que el portal NALA no active acceso antes de validar la compra.
- [ ] Publicar documentación de soporte y respuesta a incidentes.

### Enterprise y seguridad

- [ ] Tenant isolation y pruebas cross-tenant con IDs válidos.
- [ ] Entra ID/SSO para compradores institucionales.
- [ ] RBAC por organización, sede, clínica, refugio y operador municipal.
- [ ] MFA para administradores y soporte privilegiado.
- [ ] Key Vault, managed identity, rotación y mínimo privilegio.
- [ ] Defender/SAST/SCA/secret scanning y SBOM en CI.
- [ ] Logs sin tokens, coordenadas exactas ni PII innecesaria.
- [ ] Retención, borrado, exportación y subprocesadores documentados.
- [ ] SLA, RTO/RPO, backups, soporte y proceso de incidentes.
- [ ] Responsible AI para matching, triage y copiloto: fuentes, aprobación,
      evaluación, sesgo, drift, rollback y apelación.

### Venta empresarial

- [ ] Private offer para las primeras cuentas ancla.
- [ ] Azure consumption commitment y procurement path documentados.
- [ ] Partner/CSP/reseller motion definido para Latinoamérica.
- [ ] Co-sell readiness y ficha de solución preparada.
- [ ] Caso de negocio con reducción de tiempo de recuperación y carga operativa.
- [ ] Referencias, resultados agregados y denominadores verificables.
- [ ] Soporte en español, horario, escalamiento y documentación regional.

## 5. Fases y gates

| Fase                 | Resultado                                             | Gate                                                       |
| -------------------- | ----------------------------------------------------- | ---------------------------------------------------------- |
| M0 - Diseño          | oferta, buyer, pricing, legal y arquitectura          | aprobación de producto, finanzas, legal y seguridad        |
| M1 - Fulfillment     | compra de prueba provisiona tenant y entitlements     | pruebas lifecycle 100% idempotentes                        |
| M2 - Enterprise      | SSO/RBAC, observabilidad, backup, DR y abuse controls | threat model y evidencia de CI/staging                     |
| M3 - Certificación   | listing, metadata, test plan y assets                 | certificación de Marketplace aprobada                      |
| M4 - Private preview | 2-3 organizaciones ancla en Costa Rica                | outcomes, soporte y reconciliación sin incidentes críticos |
| M5 - GA Costa Rica   | venta repetible por Marketplace y partners            | 90 días de SLO y referencias verificables                  |
| M6 - LATAM           | español regional, moneda, contratos y partners        | repetibilidad en al menos 2 países adicionales             |

## 6. Lo que no está implementado hoy

La revisión del repositorio del 2026-09-23 no encontró:

- controlador de SaaS fulfillment de Azure Marketplace;
- modelo persistente de Marketplace subscription;
- flujo landing page + token + tenant provisioning;
- metering/reconciliation de Marketplace;
- private offers o co-sell assets como proceso operativo;
- integración Entra B2B/SSO específica para compradores Marketplace;
- pruebas de certificación Marketplace.

Por eso el estado actual es **Marketplace-ready architecture planned**, no
“integrado con Azure Marketplace”.

## 7. Riesgos de posicionamiento

- Marketplace no aporta product-market fit por sí mismo; solo acelera procurement.
- Publicar una oferta antes de estabilizar soporte y fulfillment crea churn y
  mala reputación difícil de revertir.
- Metering por matching IA puede crear disputas si no existe una definición
  precisa de consumo y reintento.
- La presencia en Azure no equivale a certificación, co-sell ni recomendación de
  Microsoft.
- Un claim de liderazgo mundial requiere métricas externas; la documentación
  interna no es prueba de ranking.

## 8. Fuentes de implementación

- Microsoft Marketplace partner documentation:
  https://learn.microsoft.com/en-us/partner-center/marketplace-offers/
- Microsoft Marketplace APIs, SaaS fulfillment y metering: consultar las
  referencias vigentes desde Partner Center antes de implementar, porque cambian
  los endpoints y requisitos.
- Fuentes internas: `STATUS.md`, `MASTER_TODO.md`,
  `EXTERNAL_PROVIDER_VALIDATION.md`, `API_AUTHORIZATION_MATRIX.md`,
  `RUNBOOK_DEPLOYMENT.md`, `CUMPLIMIENTO_PROTECCION_DATOS.md`.
