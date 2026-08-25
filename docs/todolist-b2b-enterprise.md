# PawTrack CR — TODO B2B Enterprise

> Checklist maestro para completar y endurecer todas las funciones B2B/B2G.
> Fecha: 2026-08-25
> Alcance: tiendas de mascotas, clínicas veterinarias, aliados/refugios, municipalidades, adopciones y publicidad.
> Objetivo: no declarar B2B terminado hasta cumplir funcionalidad, seguridad, billing, UX, pruebas y operación enterprise.

## Cómo usar este documento

- `[ ]` Pendiente
- `[~]` En progreso o parcialmente implementado
- `[x]` Implementado y validado con pruebas
- Cada tarea debe terminar con evidencia: código, prueba automatizada, captura/flujo validado o procedimiento operativo.

---

## 0. Decisiones bloqueantes de producto y facturación

Estas tareas deben completarse antes de seguir agregando features, porque existen contradicciones entre documentos y tiers.

- [ ] Elegir la fuente única de precios y tiers: `docs/planes.md`, `docs/precios.md` o una tabla de producto aprobada.
- [ ] Eliminar duplicaciones de B2C en `docs/planes.md` y conservar una sola definición por plan.
- [ ] Confirmar tiers finales de clínicas:
  - [ ] Clínica afiliada/básica gratis, Plus ₡15,000 y Partner ₡35,000; o
  - [ ] ClinicBasic ₡15,000, ClinicPlus ₡35,000 y ClinicPartner ₡60,000.
- [ ] Confirmar si municipalidades se facturan mensual o anual y fijar un solo modelo.
- [ ] Confirmar nombres canónicos de enums, tiers y productos entre dominio, frontend, documentación y billing.
- [ ] Crear catálogo centralizado de productos/precios/taxes/moneda/versiones; no dejar precios solo en comentarios o handlers.
- [ ] Definir política de cambios de precio, grandfathering, upgrades, downgrades, cancelaciones, prorrateo y reembolsos.
- [ ] Definir si B2B soporta únicamente CRC o también USD y facturación internacional.
- [ ] Definir quién puede comprar cada tier, requisitos SENASA, verificación de identidad y documentos requeridos.
- [ ] Aprobar métricas enterprise: disponibilidad, tiempo de respuesta API, soporte, retención de datos y SLA por tier.

---

## 1. Fundación multi-tenant y permisos

- [ ] Documentar el modelo de tenancy: usuario propietario, organización B2B, miembros, sedes y roles.
- [ ] Separar explícitamente identidad de usuario, organización, clínica, tienda y municipalidad.
- [ ] Definir roles mínimos: Owner, Admin, Manager, Staff, Veterinarian, Cashier, Analyst, ReadOnly, APIClient.
- [ ] Implementar autorización por organización/recurso; comprobar que ningún usuario puede leer o mutar datos de otro tenant.
- [ ] Implementar políticas RBAC/ABAC centralizadas para backend y frontend.
- [ ] Añadir invitaciones de miembros con expiración, revocación, reenvío y auditoría.
- [ ] Añadir MFA para administradores y usuarios con API keys.
- [ ] Añadir sesiones, refresh tokens, revocación y logout global para cuentas B2B.
- [ ] Aplicar rate limits por tenant, usuario, API key y endpoint.
- [ ] Crear auditoría inmutable para accesos, cambios de permisos, exportaciones, certificados, pedidos y acciones administrativas.
- [ ] Definir retención y borrado de datos por tenant, incluyendo solicitudes de exportación y eliminación.
- [ ] Ejecutar pruebas BOLA/IDOR, privilege escalation, tenant isolation y abuso de endpoints.

---

## 2. Billing y enforcement de planes

- [ ] Crear catálogo de planes B2B versionado en base de datos o configuración fuertemente tipada.
- [ ] Implementar precios en CRC y USD según la decisión comercial aprobada.
- [ ] Implementar suscripción, estado, periodo, fecha de cobro, grace period, cancelación y reactivación.
- [ ] Implementar upgrades/downgrades con prorrateo y vigencia claramente definida.
- [ ] Implementar invoice/receipt, referencia de pago, estado de pago y conciliación SINPE/tarjeta.
- [ ] Implementar webhook idempotente del proveedor de pagos, si aplica.
- [ ] Implementar feature gates en backend como autoridad final; la UI nunca debe ser el control de acceso.
- [ ] Implementar gates consistentes para StorePlus/Partner, ClinicPlus/Partner y tiers municipales.
- [ ] Implementar jobs para expiración, grace period, suspensión y restauración de features.
- [ ] Implementar notificaciones de renovación, fallo de pago, vencimiento y cambio de plan.
- [ ] Crear pantalla B2B de plan actual, límites, facturación, historial y acciones disponibles.
- [ ] Agregar pruebas de todos los gates, transiciones y bypasses.
- [ ] Verificar que ningún precio permanezca hardcodeado en comandos, enums o comentarios sin una fuente única.

---

## 3. Clínicas veterinarias

### 3.1 Base y directorio

- [x] Registro, revisión administrativa, perfil, directorio y mapa básico.
- [x] Escaneo QR y microchip RFID manual.
- [x] Resultado de mascota y notificación al propietario.
- [ ] Resolver definitivamente los campos públicos: teléfono, website, horario, dirección, logo y consentimiento.
- [ ] Crear perfil público de clínica con mapa, contacto, horario, servicios y estado de verificación.
- [ ] Añadir filtros y paginación del directorio por ubicación, servicios, horario y disponibilidad.
- [ ] Añadir flujo de corrección/actualización del perfil y revisión administrativa de cambios.
- [ ] Validar licencia SENASA, fecha de vencimiento y re-verificación periódica.

### 3.2 ClinicPlus

- [x] Posición destacada y ordenamiento en mapa/directorio.
- [x] Badge de clínica verificada.
- [x] Estadísticas de escaneos mensuales.
- [x] Banner/sponsorship de Case Room.
- [~] Logo en alertas cercanas: verificar delivery real en WhatsApp, push, email y plantillas.
- [~] Métricas de visibilidad: backend existe; completar y validar la pestaña frontend.
- [ ] Definir métricas y nomenclatura: profile views, map clicks, search appearances, scans y matched scans.
- [ ] Aplicar deduplicación, anonimización/hash de IP y retención documentada.
- [ ] Implementar soporte prioritario con SLA, cola y trazabilidad operacional.

### 3.3 ClinicPartner

- [x] Certificados PDF y código de verificación público.
- [x] API keys y endpoints de lookup.
- [x] Widget embebible.
- [~] QR dentro del PDF: agregarlo y probar que apunta al código correcto.
- [~] Firma digital: reemplazar firma visual por firma criptográfica verificable si es requisito del tier.
- [ ] Definir CA, certificado por clínica, rotación, revocación y custodia en Azure Key Vault/HSM.
- [ ] Validar firma PDF en Adobe/validadores independientes y documentar la cadena de confianza.
- [ ] Completar permisos por API key: scopes, expiración, rotación, revocación, last-used y rate limit.
- [ ] Añadir versionado de API, OpenAPI publicada, ejemplos, errores RFC 7807 y changelog.
- [ ] Añadir sandbox para integradores HIS.
- [ ] Completar integración con lectores RFID USB/BLE solo si queda dentro del alcance contractual; si no, retirarla del plan comercial.
- [ ] Añadir multi-veterinario: perfiles, agenda/atribución, permisos y auditoría.
- [ ] Añadir exportaciones auditadas y límites por periodo.

### 3.4 Expediente y consentimiento

- [x] Grants de acceso y expediente compartido base.
- [ ] Validar consentimiento explícito, alcance, expiración y revocación por mascota.
- [ ] Implementar permisos separados para leer, agregar, editar, eliminar y exportar.
- [ ] Registrar cada acceso clínico con actor, clínica, mascota, motivo, timestamp y resultado.
- [ ] Añadir bloqueo de edición posterior o historial de versiones para registros médicos.
- [ ] Definir retención, exportación y eliminación conforme a la política de privacidad.
- [ ] Probar que el plan del dueño no permite bypass del consentimiento ni acceso de clínica no autorizada.

---

## 4. Tiendas de mascotas

### 4.1 StoreBasic

- [x] Registro, aprobación administrativa, perfil público, catálogo y mapa/directorio.
- [ ] Completar perfil comercial: teléfono, WhatsApp, horario, dirección, métodos de entrega y políticas.
- [ ] Añadir gestión de inventario, disponibilidad y productos archivados.
- [ ] Añadir validación y moderación de imágenes, descripciones, precios y enlaces.
- [ ] Añadir estados de tienda: pendiente, activa, suspendida, cerrada y rechazada.
- [ ] Añadir privacidad para datos de clientes y no exponer información sensible en el catálogo.

### 4.2 StorePlus

- [x] Recepción de pedidos in-app y máquina de estados.
- [x] Referencia de pago/SINPE y panel operativo básico.
- [x] Notificaciones de nuevos pedidos y cambios de estado.
- [ ] Implementar flujo completo de confirmación de pago, rechazo, expiración y reembolso.
- [ ] Añadir idempotencia para crear pedidos, reportar pago y cambiar estados.
- [ ] Añadir control de concurrencia para evitar doble aceptación o doble descuento de inventario.
- [ ] Añadir carrito, impuestos, costo de envío/retiro y total reproducible.
- [ ] Añadir comprobante de pedido para cliente y tienda.
- [ ] Definir si PawTrack intermedia fondos o solo comunica la referencia SINPE.
- [ ] Implementar estadísticas básicas de ventas con periodo, zona horaria y filtros documentados.
- [ ] Definir soporte y SLA StorePlus.

### 4.3 StorePartner

- [ ] Implementar `GetStoreAnalyticsQuery` y endpoints de analytics avanzados.
- [ ] Definir métricas: ventas, órdenes, ticket promedio, productos, conversión, cancelaciones, tiempos y clientes recurrentes.
- [ ] Añadir filtros por periodo, sede, producto y estado.
- [ ] Añadir exportación CSV/PDF con permisos y auditoría.
- [ ] Implementar modelo `StoreLocation`/sedes con tenant común y permisos por sede.
- [ ] Migrar pedidos, inventario, catálogo y analytics para soportar `StoreId` + `LocationId`.
- [ ] Añadir consolidado multi-sucursal y vista local por sede.
- [ ] Implementar badge Partner verificado visible en directorio, mapa y perfil.
- [ ] Implementar posicionamiento prioritario con reglas transparentes y límites contra abuso.
- [ ] Añadir onboarding y soporte de cuenta enterprise.

---

## 5. Aliados, refugios y adopciones

- [x] Registro/verificación de aliados y perfil público base.
- [x] Bandeja de alertas, coordinación y KPI base.
- [x] ShelterBasic/ShelterPlus y flujo de adopciones base.
- [ ] Definir formalmente tipos de aliado, permisos, cobertura geográfica y responsable legal.
- [ ] Añadir roles y miembros para refugios y organizaciones grandes.
- [ ] Añadir workflow de revisión documental, expiración y revalidación.
- [ ] Añadir auditoría de acciones de campo y evidencias adjuntas.
- [ ] Añadir métricas de adopción: publicados, solicitudes, visitas, colocaciones y tasa de éxito.
- [ ] Añadir moderación antifraude para publicaciones, fotos, contactos y solicitudes.
- [ ] Implementar consentimiento y protección de datos de adoptantes.
- [ ] Añadir exportaciones y reportes para organizaciones.
- [ ] Definir SLA de alertas y canales de escalamiento.

---

## 6. Municipalidades B2G

- [x] Perfil, capturas, estados, búsqueda, fotos gateadas, estadísticas y dashboard regional base.
- [x] Transferencia de capturas y multi-cantón base.
- [ ] Resolver definitivamente precios y periodicidad de facturación municipal.
- [ ] Implementar catálogo/billing de MuniBasic, MuniFull y MuniRedRegional.
- [ ] Implementar roles por municipalidad, cantón y dependencia.
- [ ] Completar aislamiento de datos entre cantones y municipalidades.
- [ ] Completar dashboard visual con estadísticas, mapas, tendencias y exportaciones por tier.
- [ ] Añadir reportes oficiales configurables y trazabilidad de generación.
- [ ] Añadir integración/exportación para SENASA/PANI donde esté aprobada.
- [ ] Añadir SLA, disponibilidad y soporte contractual B2G.
- [ ] Añadir flujo de transferencia con aceptación, historial y rollback administrativo.
- [ ] Añadir carga masiva validada y reporte de errores por fila.
- [ ] Definir retención de fotos, datos de captura y documentos públicos.
- [ ] Crear pruebas de autorización cross-canton y cross-municipality.

---

## 7. Vallas publicitarias y monetización B2B

- [x] Placements Map, Dashboard, Directory y Feed.
- [x] Estados, aprobación, imágenes, CTA, dismissal y paginación.
- [ ] Definir catálogo comercial de campañas, CPM/flat fee, duración y segmentación.
- [ ] Implementar contrato/cotización, estado de pago y facturación de anunciantes.
- [ ] Implementar límites por placement, tenant, frecuencia y prioridad.
- [ ] Añadir métricas de impresiones, clics, CTR, dismissal y conversión.
- [ ] Añadir deduplicación y protección contra tráfico automatizado.
- [ ] Añadir consentimiento/privacidad para tracking y documentar retención.
- [ ] Añadir moderación de contenido, revisión legal y lista de categorías prohibidas.
- [ ] Añadir preview responsive antes de aprobación.
- [ ] Crear reporte para anunciante y auditoría de cambios.

---

## 8. Frontend enterprise y experiencia B2B

- [ ] Crear navegación B2B consistente por organización, módulo, sede y rol.
- [ ] Mostrar plan, límites, estado de suscripción y permisos en cada dashboard.
- [ ] Eliminar botones que aparentan estar disponibles cuando el backend los rechaza.
- [ ] Añadir estados completos: loading, empty, error, forbidden, suspended, expired y pending approval.
- [ ] Completar Clinic Visibility tab.
- [ ] Completar Store Partner Analytics.
- [ ] Completar vistas multi-sucursal y selector de sede.
- [ ] Mostrar badges y posiciones promocionadas de forma consistente y accesible.
- [ ] Añadir tablas con filtros, paginación, exportación y zona horaria visible.
- [ ] Añadir confirmaciones para acciones irreversibles y cambios de estado.
- [ ] Añadir accesibilidad WCAG 2.2 AA: teclado, foco, contraste, labels y lector de pantalla.
- [ ] Añadir responsive para operación en tablet/móvil y escritorio.
- [ ] Añadir i18n preparado para español/inglés sin romper formatos CRC/fecha.
- [ ] Validar performance de dashboards con datasets grandes.

---

## 9. API, integraciones y plataforma

- [ ] Publicar OpenAPI por módulo y por versión.
- [ ] Estandarizar envelopes de error, códigos HTTP, correlation ID y Problem Details.
- [ ] Versionar API pública B2B y definir política de deprecación.
- [ ] Añadir idempotency keys a mutaciones de pedidos, pagos, transferencias y uploads.
- [ ] Añadir paginación cursor-based donde existan listas grandes.
- [ ] Evitar N+1 queries y aplicar índices, `AsNoTracking` en lecturas y límites de filas.
- [ ] Añadir timeouts, retries con backoff y circuit breakers para integraciones externas.
- [ ] Añadir webhooks salientes firmados, reintentos, replay protection y delivery log.
- [ ] Validar CORS del widget y scopes por dominio registrado.
- [ ] Añadir SDK o ejemplos oficiales para HIS, tiendas y municipalidades.
- [ ] Crear entorno sandbox con datos sintéticos.

---

## 10. Seguridad, privacidad y cumplimiento

- [ ] Completar threat model de cada módulo B2B.
- [ ] Aplicar secretos únicamente desde Azure Key Vault/managed identity.
- [ ] Rotar API keys, secretos de integración y certificados sin downtime.
- [ ] Cifrar datos sensibles en tránsito y reposo.
- [ ] Hash/anonymize IPs, tokens y datos de auditoría donde corresponda.
- [ ] Revisar uploads: MIME real, extensión, tamaño, malware scanning, dimensiones y re-encoding.
- [ ] Proteger URLs de Blob con SAS de corta duración y permisos mínimos.
- [ ] Añadir anti-abuse para escaneos, búsquedas, pedidos, login y API.
- [ ] Revisar logs para no escribir PII, tokens, API keys ni secretos.
- [ ] Ejecutar SAST, dependency scanning, secret scanning, DAST y penetration test.
- [ ] Documentar privacidad, consentimiento médico, retención y derechos del titular.
- [ ] Preparar procedimiento de incidente, revocación masiva de keys y comunicación a clientes.

---

## 11. Pruebas enterprise

- [ ] Unit tests para dominio, pricing, gates, estados, permisos y validadores.
- [ ] Integration tests para cada endpoint B2B y cada transición de estado.
- [ ] Contract tests para API pública, widget, webhooks y pagos.
- [ ] Tests de aislamiento multi-tenant y autorización negativa.
- [ ] Tests de concurrencia para pedidos, pagos, inventario y transferencias.
- [ ] Tests de idempotencia y reintentos.
- [ ] Tests de migraciones sobre base vacía y base con datos existentes.
- [ ] Tests de jobs: expiración, purga, notificaciones, estadísticas y agregaciones.
- [ ] E2E frontend para onboarding, activación, operación, upgrade y suspensión.
- [ ] Accessibility tests y pruebas manuales con teclado/lector.
- [ ] Performance/load tests con objetivos documentados por endpoint.
- [ ] Chaos/failure tests para Blob, email, WhatsApp, pagos y servicios externos.
- [ ] Snapshot tests para PDFs, certificados, respuestas API y documentos críticos.
- [ ] Revisar cobertura y mutation testing en reglas de autorización y billing.

---

## 12. Observabilidad y operación

- [ ] Definir dashboards por módulo: errores, latencia, volumen, conversión y uso de gates.
- [ ] Instrumentar correlation ID y distributed tracing.
- [ ] Crear alertas para errores de pagos, API keys, jobs, colas y notificaciones.
- [ ] Medir SLI/SLO por tier comercial.
- [ ] Crear runbooks para incidentes B2B, pagos, pérdida de datos, abuso y caída de integraciones.
- [ ] Añadir health checks dependenciales y readiness/liveness correctos.
- [ ] Validar backups, restore drills, RPO/RTO y retención.
- [ ] Crear procedimiento de despliegue, rollback y migraciones sin downtime.
- [ ] Crear soporte interno con clasificación P1/P2/P3/P4 y tiempos por plan.
- [ ] Añadir auditoría y exportación de logs para clientes enterprise sin exponer PII de terceros.
- [ ] Crear reporte mensual de uso, SLA y seguridad para clientes Partner/B2G.

---

## 13. Datos, migraciones y calidad

- [ ] Crear migraciones para todas las nuevas entidades y probarlas en SQL Local/CI/staging.
- [ ] Actualizar scripts de esquema local y seed de usuarios/organizaciones B2B.
- [ ] Crear índices para búsquedas por tenant, sede, periodo, estado y ownership.
- [ ] Preparar backfill de datos existentes al nuevo modelo de organización/sede.
- [ ] Validar consistencia entre enums, seeds, DTOs y valores persistidos.
- [ ] Crear datos sintéticos representativos para clínicas, tiendas y municipalidades.
- [ ] Ejecutar análisis de datos huérfanos, duplicados y registros sin tenant.
- [ ] Definir políticas de archival/purge por entidad.

---

## 14. Documentación y preparación comercial

- [ ] Actualizar `docs/planes.md` con la matriz final aprobada, sin duplicados.
- [ ] Actualizar `docs/precios.md`, `docs/featuresB2B.md` y documento maestro para que coincidan.
- [ ] Separar claramente implementado, parcial, roadmap y servicio operacional.
- [ ] Crear manual de clínica, tienda, aliado y municipalidad con flujos actuales.
- [ ] Crear guía de onboarding por tier.
- [ ] Crear guía de API Partner con autenticación, scopes, límites y ejemplos.
- [ ] Crear guía del widget con dominios permitidos y configuración.
- [ ] Crear matriz de responsabilidades PawTrack/cliente/proveedor de pagos.
- [ ] Crear términos comerciales, privacidad, consentimiento y SLA B2B.
- [ ] Preparar demos y cuentas de prueba por cada tier.
- [ ] Crear checklist de due diligence para clientes enterprise.

---

## 15. Gate de salida B2B Enterprise

No marcar el objetivo como terminado hasta cumplir todos los puntos:

- [ ] Precios, tiers y nombres únicos y sincronizados entre documentación, código y billing.
- [ ] Cada feature vendida tiene backend, frontend, autorización, prueba y evidencia operacional.
- [ ] No existen features premium solo descritas o solo visibles en UI.
- [ ] No existen endpoints críticos sin autorización, idempotencia, rate limit y auditoría.
- [ ] No existen datos cross-tenant en pruebas negativas.
- [ ] Billing probado para alta, renovación, fallo, upgrade, downgrade, cancelación y reactivación.
- [ ] Clínicas, tiendas, aliados y municipalidades tienen onboarding y soporte definidos.
- [ ] Analytics, multi-sucursal, firma digital y RFID avanzado están implementados o retirados explícitamente del catálogo comercial.
- [ ] API pública, widget y webhooks tienen contrato versionado y sandbox.
- [ ] Backups, restore, observabilidad, alertas y runbooks fueron probados.
- [ ] Seguridad externa y pruebas de carga completadas sin hallazgos bloqueantes.
- [ ] Product owner y responsable técnico firman la matriz de aceptación final.

---

## Orden recomendado de ejecución

1. Normalizar producto, tiers y precios.
2. Cerrar tenancy, permisos y billing.
3. Completar brechas de clínicas y tiendas que ya se venden como premium.
4. Completar frontend enterprise y API contracts.
5. Endurecer seguridad, privacidad y auditoría.
6. Completar pruebas, performance y observabilidad.
7. Actualizar documentación comercial y ejecutar el gate de salida.
