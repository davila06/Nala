# FEATURES.md — Matriz definitiva de planes, características y límites

**Producto:** NALA / PawTrack CR  
**Estado:** Contrato funcional propuesto para implementación  
**Versión:** 1.0  
**Fecha:** 2026-09-21  
**Audiencia:** Producto, Backend, Frontend, QA, DevOps, Soporte, Ventas y Operaciones

---

## 1. Propósito

Este documento define la fuente de verdad funcional para los planes, características, límites, reglas de acceso y comportamiento ante cambios de suscripción de NALA / PawTrack CR.

Su objetivo es evitar diferencias entre:

- El catálogo comercial.
- La interfaz web.
- El backend y sus feature gates.
- Los manuales operativos.
- Los casos de prueba.
- Los mensajes de ventas y soporte.

> **Regla principal:** el backend es la autoridad final. Ocultar una función en la interfaz no sustituye la validación de permisos, ownership, tenant, suscripción, cuota o estado operativo en el servidor.

---

## 2. Alcance y estado de esta definición

Esta matriz establece la definición objetivo del producto. Incluye capacidades ya documentadas y límites concretos propuestos para cerrar ambigüedades existentes.

Antes de publicar una característica o límite comercial, el equipo debe verificar que exista:

- Validación en backend.
- Representación en el catálogo de entitlements.
- Feature gate en frontend.
- Telemetría de consumo cuando aplique.
- Pruebas unitarias y de integración.
- Mensaje de error y ruta de upgrade.
- Documentación de soporte.

Las cuotas definidas en este documento se consideran contractuales una vez implementadas y publicadas.

---

## 3. Principios de producto

1. La recuperación esencial de una mascota no se bloquea por falta de pago.
2. Los planes pagados monetizan automatización, alcance, localización, colaboración y administración avanzada.
3. Las funciones de seguridad, privacidad, aislamiento de datos y autorización no son beneficios premium.
4. Toda cuota se valida en backend mediante un servicio central de entitlements.
5. Las cuotas mensuales se reinician al inicio de cada ciclo de facturación.
6. Los límites por caso, mascota, collar, sucursal, cantón o evento no se reinician mensualmente, salvo que se indique lo contrario.
7. Un downgrade no elimina datos históricos.
8. Los recursos que excedan el nuevo límite quedan en modo lectura o inactivos.
9. Un downgrade nunca interrumpe un caso de mascota perdida que ya esté activo.
10. NALA no procesa ni intermedia pagos de productos, adopciones o reservas mientras esa capacidad no se apruebe formalmente.
11. StoreBasic, ShelterBasic y el directorio gratuito de clínicas son niveles base, no planes comerciales pagados.
12. Los términos “ilimitado” y “uso razonable” deben contar con controles técnicos y revisión contra abuso.

---

## 4. Convenciones

| Símbolo o término | Significado |
|---|---|
| Incluido | Disponible sin cuota adicional dentro del plan. |
| No incluido | No disponible para ese plan. |
| Uso razonable | Sin cobro unitario, sujeto a un máximo técnico y revisión de uso anómalo. |
| Por cuenta | Límite compartido por todos los usuarios de la cuenta. |
| Por mascota | Límite independiente para cada mascota. |
| Por collar | Límite independiente para cada dispositivo. |
| Por caso | Límite independiente para cada reporte de pérdida. |
| Por ciclo | Se reinicia al iniciar el siguiente período de facturación. |
| Modo lectura | Los datos se conservan y pueden consultarse, pero no modificarse ni ampliarse. |
| Gate | Regla técnica que habilita o bloquea una capacidad. |

---

# PARTE I — PLANES B2C

## 5. Planes para propietarios

### 5.1 Matriz definitiva

| Característica | Free | UserPlus | UserFamilia |
|---|---:|---:|---:|
| Mascotas activas | 1 | 3 | Uso razonable, máximo técnico de 25 |
| Usuarios de la cuenta | 1 | 1 | 5 en total |
| Perfil público | Incluido | Incluido | Incluido |
| QR permanente | 1 | 1 por mascota activa | 1 por mascota activa |
| Descarga de QR | Incluida | Incluida | Incluida |
| Historial de escaneos | 30 días | 12 meses | Completo |
| Reportes de pérdida | Ilimitados | Ilimitados | Ilimitados |
| Casos activos simultáneos | 1 | 3 | 10 |
| Case Room | Incluido | Incluido | Incluido |
| Avistamientos anónimos | Ilimitados | Ilimitados | Ilimitados |
| Mascota encontrada sin QR | Incluido | Incluido | Incluido |
| Chat enmascarado | Incluido | Incluido | Incluido |
| Código de entrega segura | Incluido | Incluido | Incluido |
| Reporte de fraude | Incluido | Incluido | Incluido |
| Notificaciones in-app | Incluidas | Incluidas | Incluidas |
| Notificaciones push | Incluidas | Incluidas | Incluidas |
| Alertas geográficas | 1 ubicación, radio fijo de 3 km | 1 ubicación, hasta 20 km | 5 ubicaciones, hasta 30 km cada una |
| Matching visual por IA | 1 búsqueda por caso activo | 10 búsquedas por ciclo | 30 búsquedas por ciclo y cuenta |
| Resultados por matching | 5 candidatos | 15 candidatos | 35 candidatos |
| Enlaces manuales para compartir | Incluidos | Incluidos | Incluidos |
| Difusión automatizada | 1 por caso | 5 por caso cada 24 horas | 10 por caso cada 24 horas |
| Repetición programada de difusión | No incluida | No incluida | Incluida |
| Participación en cuadrícula | Incluida | Incluida | Incluida |
| Activación de cuadrícula | No incluida | Incluida | Incluida |
| Administración de cuadrícula | No incluida | Incluida | Incluida |
| Historial de coordinación | No incluido | Resumen del caso | Completo |
| Collares GPS activos | 0 | 1 | 5 |
| Historial GPS | No aplica | 30 días | 12 meses |
| Zonas seguras | No aplica | 3 por collar | 10 por collar |
| Alertas de batería y desconexión | No aplica | Incluidas | Incluidas |
| Modo perdido GPS | No aplica | Incluido | Incluido |
| Transferencia segura del collar | No aplica | Incluida | Incluida |
| Auditoría del collar | No aplica | Incluida | Incluida |
| Cuenta familiar | No incluida | No incluida | Incluida |
| Miembros adicionales | 0 | 0 | 4 adicionales |
| Expediente médico | Contador y teaser | Contador y teaser | Completo |
| Registros médicos | No permite crear | No permite crear | Uso razonable |
| Archivo por registro médico | No aplica | No aplica | 5 MB |
| Recordatorios veterinarios activos | 0 | 0 | 50 por cuenta |
| Exportación médica PDF | No incluida | No incluida | Incluida |
| Grants clínicos administrables | No incluidos | Acceso temporal por flujo válido | 10 por mascota |
| Historial de accesos clínicos | No incluido | No incluido | Completo |
| Directorios públicos | Incluidos | Incluidos | Incluidos |
| Solicitudes de adopción | Incluidas | Incluidas | Incluidas |
| Reservas de servicios | Incluidas | Incluidas | Incluidas |
| Exportación de datos personales | Incluida | Incluida | Incluida |
| Eliminación de cuenta | Incluida | Incluida | Incluida |
| Nivel de soporte | Centro de ayuda | Prioridad por correo | Prioridad alta |

### 5.2 Ciclo esencial de recuperación

Las siguientes capacidades no pueden bloquearse por plan:

- Consultar el perfil público.
- Crear un reporte de pérdida para una mascota activa.
- Recibir avistamientos.
- Consultar la Case Room.
- Utilizar chat enmascarado.
- Generar y verificar el código de entrega.
- Marcar el caso como reunificado.
- Reportar fraude.

### 5.3 Uso razonable de UserFamilia

- La cuenta no paga por mascota adicional.
- El máximo técnico es de 25 mascotas activas.
- Al intentar superar 25 mascotas, el sistema debe bloquear la creación y orientar al usuario hacia una cuenta institucional apropiada.
- Las mascotas existentes nunca se eliminan automáticamente.
- Las cuentas que representen refugios, operadores comerciales o uso institucional deben migrarse al tipo correspondiente.

### 5.4 Matching visual por IA

Una búsqueda se contabiliza cuando el backend acepta la imagen y ejecuta el proceso de matching. No debe cobrarse o descontarse cuota cuando:

- La solicitud falla antes de procesar la imagen.
- El archivo es inválido.
- El servicio externo no inicia el procesamiento.
- La operación es un reintento idempotente de una solicitud ya contabilizada.

Al agotarse la cuota:

- El reporte de avistamiento continúa disponible.
- La carga de evidencia continúa disponible.
- Solo se bloquea una nueva ejecución de matching.
- El frontend debe mostrar la fecha de reinicio de la cuota y la opción de upgrade.

### 5.5 Difusión automatizada

- Free permite una difusión automatizada inicial por caso.
- UserPlus permite hasta cinco ejecuciones por caso dentro de una ventana móvil de 24 horas.
- UserFamilia permite hasta diez ejecuciones por caso dentro de una ventana móvil de 24 horas.
- Los enlaces manuales para compartir no consumen cuota.
- El rate limiting de seguridad se aplica además del límite comercial.
- Un canal fallido no debe volver a cobrarse si se reintenta con la misma clave idempotente.

### 5.6 GPS

- El hardware se compra por separado o mediante un bundle.
- La suscripción habilita las funciones digitales, no incluye automáticamente el costo del dispositivo ni conectividad celular.
- Cada collar solo puede estar asociado a una mascota activa a la vez.
- El historial vencido puede archivarse según la política de retención, pero no debe seguir visible en el plan.
- Un downgrade desactiva nuevas lecturas premium y conservación ampliada, sin eliminar la auditoría obligatoria.

---

# PARTE II — PLANES B2B Y SOCIALES

## 6. Planes para tiendas

### 6.1 Matriz definitiva

| Característica | StoreBasic | StorePlus | StorePartner |
|---|---:|---:|---:|
| Perfil público | Incluido | Incluido | Incluido |
| Posición en directorio | Estándar | Estándar | Prioritaria |
| Sucursales | 1 | 1 | 5 |
| Productos activos | 10 | 100 | 1.000 |
| Imágenes por producto | 1 | 5 | 10 |
| Tamaño máximo por imagen | 5 MB | 5 MB | 5 MB |
| Pedidos in-app | No incluidos | Incluidos | Incluidos |
| Pedidos por ciclo | 0 | 250 | 2.500 |
| Gestión de estados del pedido | No incluida | Incluida | Incluida |
| Retiro o entrega | No incluido | Incluido | Incluido |
| Procesamiento de pagos por NALA | No | No | No |
| Comisión por venta | 0% | 0% | 0% |
| Analítica | No incluida | Resumen de 30 días | 24 meses |
| Exportación CSV | No incluida | No incluida | Incluida |
| Usuarios operadores | 1 | 3 | 15 |
| Importación masiva | No incluida | No incluida | 1.000 productos por archivo |
| Soporte | Estándar | Correo | Prioritario |

### 6.2 Reglas operativas de tiendas

- NALA comunica solicitudes, pero no garantiza inventario.
- La tienda debe confirmar disponibilidad antes de aceptar.
- El pago se coordina directamente entre cliente y tienda.
- NALA no cobra comisión ni liquida fondos.
- No debe publicarse “SINPE integrado” como beneficio del plan.
- El límite de pedidos se consume al crear una solicitud válida, no cuando la tienda la acepta.
- Los pedidos cancelados se conservan para auditoría y no liberan cuota retroactivamente.

---

## 7. Planes para refugios

### 7.1 Matriz definitiva

| Característica | ShelterBasic | ShelterPlus |
|---|---:|---:|
| Requisito | Ally Shelter verificado | Ally Shelter verificado |
| Perfil de refugio | Incluido | Incluido |
| Animales activos | 5 | Uso razonable, máximo técnico de 500 |
| Fotografías por animal | 5 | 10 |
| Solicitudes de adopción | Incluidas | Incluidas |
| Gestión de solicitudes | Incluida | Incluida |
| Usuarios operadores | 1 | 10 |
| Ferias activas simultáneas | 0 | 5 |
| Ferias por año | 0 | 24 |
| Animales por feria | No aplica | 100 |
| Radio de alertas de feria | No aplica | 10 km |
| Pin destacado | No incluido | Incluido |
| Exportación CSV | No incluida | Incluida |
| Analítica | Resumen básico | 24 meses |
| Alertas de mascotas perdidas | Incluidas | Incluidas |
| Confirmación de búsqueda | Incluida | Incluida |
| Soporte | Estándar | Prioritario social |

### 7.2 Reglas sociales

- NALA puede patrocinar o becar ShelterPlus.
- Una clínica, tienda o empresa puede patrocinar un refugio.
- La suspensión de pago no elimina publicaciones ni historial.
- Al vencer ShelterPlus, solo cinco publicaciones seleccionadas permanecen activas.
- Las publicaciones restantes pasan a modo lectura y dejan de aparecer en el directorio activo.

---

## 8. Planes para clínicas veterinarias

### 8.1 Matriz definitiva

| Característica | Directorio gratuito | ClinicPlus | ClinicPartner |
|---|---:|---:|---:|
| Perfil público | Incluido | Incluido | Incluido |
| Sucursales | 1 | 1 | 5 |
| Usuarios operadores | 1 | 5 | 25 |
| Escaneos por ciclo | 25 | 500 | 5.000 |
| QR y microchip | Dentro de cuota | Dentro de cuota | Dentro de cuota |
| Notificación al dueño | Incluida | Incluida | Incluida |
| Badge verificado | No incluido | Incluido | Incluido |
| Destacado en mapa | No incluido | Rotación premium | Prioridad Partner |
| Estadísticas de escaneo | No incluidas | 12 meses | 36 meses |
| Métricas de visibilidad | No incluidas | Incluidas | Incluidas |
| Exportación CSV | No incluida | No incluida | Incluida |
| Lectura de expediente | Con grant o flujo válido | Con grant | Con grant |
| Escritura en expediente | Con autorización aplicable | Con grant de escritura | Con grant de escritura |
| API keys activas | 0 | 0 | 10 |
| Vigencia de API key | No aplica | No aplica | 1 año |
| Permisos separados por API key | No | No | Sí |
| Widget embebible | No incluido | No incluido | 5 dominios autorizados |
| Integración M2M | No incluida | No incluida | Incluida |
| Certificados PDF verificables | 0 | 0 | 500 por ciclo |
| Pasaportes SENASA-ready | 0 | 0 | 250 por ciclo |
| Veterinarios autorizados | 0 | 0 | 25 |
| Auditoría de emisión | No aplica | No aplica | Completa |
| Revocación de documentos | No aplica | No aplica | Incluida |
| Gestión de conflictos de microchip | Reporte básico | Gestión básica | Gestión completa |
| Soporte | Estándar | Prioritario | Integración prioritaria |

### 8.2 Reglas de identificación

- La cuota se consume al ejecutar una búsqueda válida por QR o microchip.
- Un reintento idempotente no consume una segunda unidad.
- Una entrada inválida rechazada antes de buscar no consume cuota.
- La clínica nunca recibe datos privados no autorizados del propietario.
- La notificación al dueño forma parte del flujo de identificación.

### 8.3 Regla definitiva de certificados

- ClinicPlus no emite certificados verificables.
- ClinicPartner puede emitir certificados y pasaportes SENASA-ready dentro de su cuota.
- La emisión exige suscripción activa, clínica habilitada, verificación documental, veterinario autorizado y grant médico válido.
- “SENASA-ready” significa preparado para trazabilidad y revisión documental.
- No implica envío, integración, aprobación o validez oficial automática ante SENASA.
- Todo documento debe tener estado, código de verificación, auditoría y capacidad de revocación.

### 8.4 API keys

Cada API key debe:

- Mostrarse una sola vez al crearla.
- Almacenarse mediante hash seguro.
- Tener fecha de expiración.
- Permitir revocación y rotación.
- Restringirse por permisos.
- Asociarse con una clínica y sus scopes.
- Registrar uso y resultado sin almacenar secretos.

Permisos disponibles:

- `scan`
- `medical:read`
- `medical:write`
- `certificates`
- `analytics`

---

## 9. Planes municipales

### 9.1 Matriz definitiva

| Característica | MuniBasica | MuniFull | MuniRedRegional |
|---|---:|---:|---:|
| Cantones incluidos | 1 | 1 | 10 |
| Organizaciones municipales | 1 | 1 | 10 |
| Usuarios operadores | 5 | 20 | 100 |
| Capturas por año | 500 | 5.000 | 30.000 |
| Registro de capturas | Incluido | Incluido | Incluido |
| Actualización individual | Incluida | Incluida | Incluida |
| Filtros y búsqueda | Incluidos | Incluidos | Incluidos |
| Asociación con mascota NALA | Incluida | Incluida | Incluida |
| Conversión a caso de bienestar | Incluida | Incluida | Incluida |
| Fotografía de evidencia | No incluida | 5 MB por archivo | 5 MB por archivo |
| Actualización masiva | No incluida | 500 registros por operación | 2.000 por operación |
| Estadísticas | Resumen operativo | Cantonales | Cantonales y regionales |
| Historial estadístico | 12 meses | 36 meses | 60 meses |
| Exportación CSV | No incluida | Incluida | Incluida |
| Reportes PDF | 1 resumen anual | 12 por año | 60 por año |
| Reportes institucionales | Catálogo básico | Catálogo cantonal | Catálogo regional |
| Dashboard regional | No incluido | No incluido | Incluido |
| Transferencias entre cantones | No incluidas | No incluidas | Incluidas |
| API institucional | No incluida | Lectura opcional por contrato | Incluida por contrato |
| Soporte | Estándar | Prioritario | Gestor institucional |

### 9.2 Reglas municipales

- Toda consulta debe limitarse al tenant y los cantones autorizados.
- Una transferencia regional debe registrar origen, destino, motivo, actor y fecha.
- La evidencia no debe exponerse públicamente.
- Los reportes institucionales deben aplicar agregación y supresión de datos sensibles.
- No deben divulgar GPS exacto, domicilios, reportantes, evidencia privada ni identificadores de propietarios.
- Los reportes SENASA-ready no son envíos oficiales.
- La capacidad adicional se contrata mediante add-ons o acuerdo institucional.

---

## 10. Membresías para proveedores de servicios

### 10.1 Matriz definitiva

| Característica | Provider Free | Provider Verified | Provider Featured |
|---|---:|---:|---:|
| Perfil público | Incluido | Incluido | Incluido |
| Posición en directorio | Estándar | Verificada | Prioritaria |
| Servicios activos | 3 | 25 | 100 |
| Fotografías por servicio | 1 | 5 | 10 |
| Agenda de disponibilidad | No incluida | Incluida | Incluida |
| Bloqueos activos de agenda | 0 | 20 | 100 |
| Reservas por ciclo | 0 | 100 | 1.000 |
| Usuarios operadores | 1 | 3 | 10 |
| Ubicaciones | 1 | 2 | 10 |
| Sello verificado | No incluido | Incluido | Incluido |
| Evidencia documental | Opcional | Requerida | Requerida |
| Analítica | No incluida | 90 días | 24 meses |
| Exportación CSV | No incluida | No incluida | Incluida |
| Promociones activas | 0 | 1 | 10 |
| Comisión de NALA | 0% | 0% | 0% |
| Procesamiento de pagos | No | No | No |
| Soporte | Estándar | Prioritario | Prioridad alta |

### 10.2 Prueba Verified

- Cada proveedor tiene derecho a una única prueba de 30 días.
- La prueba no se reinicia por suspensión, reactivación o cambio de propietario.
- Al vencer, la membresía vuelve a Free.
- Los servicios y reservas históricas se conservan.
- Los servicios que excedan el límite Free dejan de publicarse.
- La agenda y creación de nuevas reservas se deshabilitan.

---

## 11. Aliados verificados

El programa de aliados es gratuito y no constituye una suscripción comercial.

| Característica | Límite |
|---|---:|
| Organizaciones por cuenta | 1 |
| Zona central de cobertura | 1 |
| Radio máximo | 50 km |
| Alertas operativas | Sin límite |
| Confirmaciones | 1 por organización y caso |
| Usuarios operadores | 5 |
| Historial de alertas | 24 meses |
| Cambio de cobertura | Solicitud administrativa |
| Acceso Shelter | Solo Ally de tipo Shelter verificado |
| Costo | Gratuito |

---

# PARTE III — REGLAS DE CAMBIO DE PLAN

## 12. Upgrades

- El upgrade puede habilitar capacidades de inmediato después de confirmar el pago.
- Las cuotas del nuevo plan se inicializan completas, salvo política comercial distinta explícitamente configurada.
- Los recursos previamente inactivos pueden reactivarse hasta el nuevo límite.
- El importe, ciclo y vigencia se persisten antes de activar la suscripción.
- El backend no debe confiar en montos ni plazos enviados por el cliente.

## 13. Downgrades B2C

### 13.1 UserFamilia a UserPlus

- Se mantienen todos los datos.
- El propietario selecciona hasta tres mascotas activas.
- Las restantes quedan en modo lectura.
- La cuenta familiar deja de permitir modificaciones al terminar la vigencia.
- El expediente médico queda como teaser o vista previa.
- No se admiten nuevos registros médicos ni recordatorios.
- Solo un collar permanece activo.
- Los demás collares dejan de recibir funciones premium.
- Los casos de pérdida activos continúan hasta su cierre.

### 13.2 UserPlus a Free

- El propietario selecciona una mascota activa.
- Las demás quedan en modo lectura.
- Se deshabilitan GPS, zonas seguras y difusión ampliada.
- El historial permanece almacenado según las políticas de retención.
- Los casos activos continúan hasta su cierre.

## 14. Downgrades B2B y B2G

- No se eliminan productos, pedidos, animales, adopciones, reservas, certificados, capturas ni reportes.
- Se impide crear recursos nuevos por encima del límite vigente.
- Las API keys se revocan o desactivan al vencer un plan Partner.
- Los widgets e integraciones dejan de responder con contenido premium.
- Los perfiles gratuitos permanecen visibles cuando aplique.
- Los datos históricos quedan disponibles según permisos, retención y obligaciones legales.
- Los documentos ya emitidos conservan su verificación y estado histórico.

---

# PARTE IV — ADD-ONS

## 15. Catálogo de add-ons propuesto

| Add-on | Capacidad |
|---|---|
| Mascota adicional Plus | 1 mascota activa adicional |
| Collar GPS adicional | 1 collar activo adicional |
| Sucursal adicional | 1 sucursal de tienda o clínica |
| Paquete de escaneos | 1.000 escaneos clínicos adicionales |
| Paquete de certificados | 250 certificados adicionales |
| Cantón adicional | 1 cantón adicional |
| Usuarios institucionales | 10 operadores adicionales |
| Paquete de reservas | 500 reservas adicionales |
| Almacenamiento adicional | 25 GB adicionales |
| API Enterprise | Cuota, soporte y SLA contractuales |

Los add-ons deben tener vigencia, precio, renovación y reglas de prorrateo configurables. Una capacidad adicional no debe sobrevivir a su vigencia si no se renueva.

---

# PARTE V — MODELO TÉCNICO

## 16. Entitlements requeridos

La configuración del plan debe exponer, como mínimo, los siguientes entitlements:

```text
MaxPets
MaxFamilyMembers
MaxActiveLostCases
ScanHistoryRetentionDays
AlertLocationCount
AlertRadiusKm
AiMatchesPerCycle
AiCandidatesPerMatch
BroadcastsPerCasePerDay
BroadcastSchedulingEnabled
SearchGridActivationEnabled
SearchGridHistoryEnabled
MaxGpsCollars
GpsHistoryDays
MaxSafeZonesPerCollar
MedicalRecordsEnabled
MaxActiveVetReminders
MedicalPdfExportEnabled
MaxClinicGrantsPerPet
ClinicAccessLogEnabled
MaxBranches
MaxOperators
MaxActiveProducts
MaxProductImages
MaxOrdersPerCycle
AnalyticsRetentionDays
CsvExportEnabled
PriorityListingEnabled
BulkImportLimit
MaxActiveAdoptablePets
MaxAdoptionFairsPerYear
MaxConcurrentAdoptionFairs
MaxAnimalsPerFair
MaxClinicScansPerCycle
MaxApiKeys
MaxWidgetDomains
MaxCertificatesPerCycle
MaxPassportsPerCycle
MaxAuthorizedVeterinarians
MaxCantons
MaxOrganizations
MaxCapturesPerYear
BulkUpdateLimit
MaxReportsPerYear
RegionalDashboardEnabled
InterCantonTransfersEnabled
InstitutionalApiEnabled
MaxActiveServices
MaxScheduleBlocks
MaxBookingsPerCycle
MaxLocations
MaxActivePromotions
```

## 17. Servicio central de autorización comercial

Se recomienda un contrato equivalente a:

```csharp
public interface IEntitlementService
{
    Task<EntitlementSnapshot> GetSnapshotAsync(
        Guid subjectId,
        CancellationToken cancellationToken);

    Task<EntitlementDecision> AuthorizeAsync(
        Guid subjectId,
        string entitlement,
        decimal requestedUnits,
        EntitlementContext context,
        CancellationToken cancellationToken);

    Task<ConsumptionResult> ConsumeAsync(
        Guid subjectId,
        string entitlement,
        decimal units,
        string idempotencyKey,
        EntitlementContext context,
        CancellationToken cancellationToken);
}
```

### Reglas del servicio

- Una decisión debe incluir permitido, límite, consumo, saldo y fecha de reinicio cuando aplique.
- Las operaciones de consumo deben ser idempotentes.
- Los cambios de plan deben invalidar snapshots en caché.
- El frontend puede consultar capacidades, pero nunca sustituir la validación del backend.
- Ownership, tenant, rol y estado operativo se validan además del entitlement.
- Un entitlement no concede acceso a recursos de terceros.

## 18. Persistencia sugerida

### 18.1 PlanDefinition

Define el plan comercial y su vigencia.

### 18.2 PlanEntitlement

Relaciona un plan con una capacidad, valor y unidad.

Campos sugeridos:

```text
Id
PlanId
EntitlementKey
ValueType
NumericValue
BooleanValue
TextValue
Unit
ResetPeriod
IsActive
Version
CreatedAt
UpdatedAt
```

### 18.3 SubscriptionAddon

Relaciona una suscripción con capacidad adicional y vigencia.

### 18.4 EntitlementConsumption

Registra consumo medible e idempotente.

Campos sugeridos:

```text
Id
SubjectId
SubscriptionId
EntitlementKey
Units
IdempotencyKey
CycleStart
CycleEnd
ContextType
ContextId
ConsumedAt
```

Restricción requerida:

```text
UNIQUE (SubjectId, EntitlementKey, IdempotencyKey)
```

## 19. Respuesta estándar al superar límites

Usar Problem Details con HTTP 403 para capacidades no incluidas y HTTP 409 o 422 para una cuota agotada, según la convención acordada por el equipo.

Ejemplo:

```json
{
  "type": "https://pawtrack.cr/problems/entitlement-limit-reached",
  "title": "Límite del plan alcanzado",
  "status": 409,
  "code": "PLAN_LIMIT_REACHED",
  "entitlement": "AiMatchesPerCycle",
  "limit": 10,
  "consumed": 10,
  "remaining": 0,
  "resetsAt": "2026-10-21T00:00:00-06:00",
  "upgradeOptions": ["UserFamilia"]
}
```

No incluir información sensible, secretos o datos de otros tenants en la respuesta.

---

# PARTE VI — FRONTEND Y EXPERIENCIA

## 20. Reglas de UI

Cada feature gate debe contemplar cuatro estados:

1. **Disponible:** la acción se muestra habilitada.
2. **No incluida:** se muestra el beneficio y una ruta clara de upgrade.
3. **Cuota agotada:** se muestra consumo, límite y fecha de reinicio.
4. **Bloqueada por condición operativa:** se explica la condición faltante, por ejemplo verificación clínica o grant médico.

La interfaz debe:

- Mostrar el plan activo.
- Mostrar el ciclo vigente.
- Mostrar consumo de cuotas relevantes.
- Evitar lenguaje que prometa funciones no implementadas.
- Diferenciar restricción comercial de error técnico.
- No revelar información de autorización interna.
- Mantener disponible el ciclo esencial de recuperación.

---

# PARTE VII — QA Y CRITERIOS DE ACEPTACIÓN

## 21. Pruebas mínimas por entitlement

Para cada capacidad, QA debe cubrir:

- Usuario sin suscripción.
- Plan inferior.
- Plan correcto.
- Cuota con saldo.
- Última unidad disponible.
- Cuota agotada.
- Reinicio de ciclo.
- Upgrade.
- Downgrade.
- Suscripción vencida.
- Add-on activo.
- Add-on vencido.
- Reintento idempotente.
- Acceso a recurso propio.
- Intento de acceso a recurso ajeno.
- Tenant incorrecto.
- Rol incorrecto.
- Estado operativo incorrecto.

## 22. Criterios de aceptación generales

- [ ] Cada plan tiene entitlements persistidos.
- [ ] No existen límites comerciales hardcodeados en controllers.
- [ ] Frontend y backend usan las mismas claves de entitlement.
- [ ] El backend valida todas las operaciones mutables.
- [ ] Las cuotas tienen telemetría y auditoría.
- [ ] Los consumos son idempotentes.
- [ ] Los mensajes de límite son consistentes.
- [ ] Los downgrades no eliminan datos.
- [ ] Los casos activos de pérdida no se interrumpen.
- [ ] Los perfiles B2B respetan ownership y tenant.
- [ ] Los reportes institucionales aplican agregación y supresión.
- [ ] Los documentos veterinarios conservan verificación y revocación.
- [ ] La tabla pública de precios coincide con este documento.
- [ ] Los manuales operativos fueron actualizados.
- [ ] Existen pruebas unitarias e integración por plan.

---

# PARTE VIII — DECISIONES DE PRODUCTO CERRADAS

## 23. Decisiones definitivas

1. Free mantiene el ciclo esencial de recuperación.
2. UserPlus incluye GPS para un collar.
3. UserFamilia incluye salud completa y hasta cinco miembros.
4. ClinicPlus no incluye certificados verificables.
5. ClinicPartner incluye certificados y pasaporte SENASA-ready.
6. StorePlus y StorePartner no procesan pagos.
7. StorePartner habilita hasta cinco sucursales.
8. ShelterPlus incluye ferias y animales ilimitados bajo uso razonable.
9. Provider Verified y Featured se convierten en niveles comerciales definidos.
10. Ally permanece gratuito.
11. Los reportes SENASA-ready no son envíos oficiales.
12. Las funciones de seguridad y privacidad son transversales.
13. Los precios no se definen en este archivo; deben provenir del catálogo administrable.
14. Todo límite debe aplicarse mediante `IEntitlementService` o equivalente.

---

## 24. Pendientes de implementación

- [ ] Ampliar `SubscriptionPlan` para asociar entitlements configurables.
- [ ] Crear migraciones para definiciones, add-ons y consumos.
- [ ] Implementar el servicio central de entitlements.
- [ ] Reemplazar límites hardcodeados existentes.
- [ ] Incorporar idempotencia en consumos de cuota.
- [ ] Crear endpoint para consultar snapshot de capacidades.
- [ ] Incorporar medidores de cuota en frontend.
- [ ] Aplicar gates a matching, difusión, GPS, salud y familia.
- [ ] Aplicar gates a productos, pedidos, sedes y analítica.
- [ ] Aplicar gates a escaneos, API keys, certificados y pasaportes.
- [ ] Aplicar gates a capturas, reportes y red regional.
- [ ] Aplicar gates a servicios, agenda y reservas.
- [ ] Crear alertas de proximidad al límite.
- [ ] Actualizar catálogo público y página de precios.
- [ ] Actualizar manuales de usuario, tiendas, clínicas, refugios, municipalidades y proveedores.
- [ ] Crear pruebas de upgrade y downgrade.
- [ ] Validar términos comerciales, privacidad y retención.

---

## 25. Definition of Done

Una característica de plan se considera terminada cuando:

- [ ] La regla está configurada en datos.
- [ ] El backend la valida.
- [ ] El frontend representa correctamente su estado.
- [ ] La cuota se mide de forma idempotente, si aplica.
- [ ] La operación respeta ownership, rol y tenant.
- [ ] Existen logs y métricas sin PII innecesaria.
- [ ] Existen pruebas unitarias.
- [ ] Existen pruebas de integración.
- [ ] Existe documentación para soporte.
- [ ] La descripción comercial coincide con el comportamiento real.

---

## 26. Gobierno del documento

- Producto es responsable de aprobar cambios de alcance.
- Arquitectura valida que las reglas sean implementables y consistentes.
- Backend implementa la autoridad de acceso y consumo.
- Frontend implementa presentación, medidores y rutas de upgrade.
- QA valida planes, límites, cambios de ciclo y escenarios de abuso.
- Operaciones valida procesos B2B, B2G y sociales.
- Ventas solo puede ofrecer capacidades marcadas como implementadas y publicadas.
- Todo cambio requiere actualizar este archivo, el catálogo, pruebas y manuales dentro del mismo cambio de versión.

---

**Fin de FEATURES.md**
