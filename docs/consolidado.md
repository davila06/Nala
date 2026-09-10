# PawTrack CR / NALA - Documento Consolidado de Producto

> **Estado:** fuente consolidada de producto y capacidades verificables
> **Corte:** 2026-09-10
> **Alcance:** visión NALA, roles, superficies, features por rol, planes, ciclos,
> autorización y límites comerciales.
>
> Este documento distingue tres conceptos que no deben mezclarse:
>
> - **Implementado:** existe en backend/frontend y tiene ruta, endpoint o gate verificable.
> - **Técnico:** existe como capacidad o membresía de gating, pero no implica precio o comercialización aprobada.
> - **Pendiente/propuesta:** aparece en roadmap, documentos históricos o decisiones aún no aprobadas.

---

## 1. Qué es NALA y PawTrack CR

**PawTrack CR** es una PWA para identidad digital, protección y recuperación de
mascotas en Costa Rica. Su ciclo principal es:

```text
Registrar mascota -> QR permanente -> Reportar pérdida -> Avistamientos
        -> Alertas y coordinación -> Matching visual -> Handover seguro
        -> Reunificación -> Estadísticas e incentivos
```

**NALA** es el nombre interno de la capa institucional y de inteligencia
operativa que evolucionó junto con PawTrack CR. NALA presenta indicadores
agregados, tendencias, resúmenes territoriales y reportes institucionales; no
reemplaza los portales operativos de dueños, aliados, clínicas, refugios,
tiendas, proveedores o municipalidades.

### 1.1 Superficies NALA

| Superficie                   | Acceso                                                          | Propósito                                                             |
| ---------------------------- | --------------------------------------------------------------- | --------------------------------------------------------------------- |
| `/nala`                      | `Admin`; `Nala` solo como compatibilidad histórica del atributo | Overview, tendencias, instituciones y capas geográficas generalizadas |
| `/reportes-institucionales`  | `Admin`, `Municipality`, `Clinic`, `Ally` según scope           | Catálogo, previews, solicitudes y descargas agregadas                 |
| `GET /api/nala/overview`     | `Admin,Nala` en controller                                      | Indicadores del periodo                                               |
| `GET /api/nala/trends`       | `Admin,Nala` en controller                                      | Tendencias agregadas                                                  |
| `GET /api/nala/cantons`      | `Admin,Nala` en controller                                      | Resumen por cantón                                                    |
| `GET /api/nala/map-layers`   | `Admin,Nala` en controller                                      | Capas geográficas generalizadas                                       |
| `GET /api/nala/institutions` | `Admin,Nala` en controller                                      | Rendimiento institucional agregado                                    |

El enum `UserRole` no define un rol `Nala`; define `Owner`, `Ally`, `Admin`,
`Clinic`, `Municipality`, `Store`, `ServiceProvider` y `Support`. En la
operación actual, el acceso efectivo a NALA corresponde a `Admin`. No debe
crearse un usuario `Nala` como si fuera un rol normal sin una decisión explícita
de RBAC.

### 1.2 Privacidad NALA

Los indicadores y reportes deben:

- usar agregación y suppression de grupos pequeños;
- respetar organización y cantones autorizados;
- omitir domicilios, GPS exacto, reportantes, evidencia sensible y PII;
- evitar reconstruir individuos mediante consultas sucesivas de áreas pequeñas;
- etiquetar los exports como `SENASA-ready`, no como integración o aprobación oficial de SENASA.

---

## 2. Roles reales y superficies

La autenticación no concede acceso por sí sola. Cada módulo combina rol con
ownership, tenant, suscripción, verificación, grant o scope de API.

| Rol                   | Superficie principal                                                            | Features principales                                                         | Condiciones                                                |
| --------------------- | ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- | ---------------------------------------------------------- |
| `Owner`               | `/dashboard`, `/perfil`, `/pets/*`, `/lost/*`, `/chat/*`, `/mis-adopciones`     | Mascotas, QR, pérdidas, avistamientos, chat, salud, familia, GPS, adopción   | Ownership de recursos y tier cuando aplica                 |
| `Ally`                | `/allies/panel`                                                                 | Alertas operativas, cobertura, confirmación de acciones                      | Perfil verificado                                          |
| `Ally` tipo `Shelter` | `/shelter/dashboard`, `/shelter/publicar`, `/shelter/animales/:id/aplicaciones` | Publicar animales, solicitudes y ferias                                      | Refugio verificado; límites Shelter                        |
| `Clinic`              | `/clinica/portal`, `/clinicas`, `/clinica/registro`                             | Escaneo, pacientes, expediente concedido, certificados                       | Clínica activa; Partner para integraciones y certificados  |
| `Store`               | `/tienda/portal/*`, `/tiendas`                                                  | Perfil, catálogo, pedidos, analytics y sedes técnicas                        | Tienda activa y tier cuando aplica                         |
| `ServiceProvider`     | `/servicio/portal/*`, `/servicios`                                              | Perfil, catálogo, disponibilidad, reservas, verificación                     | Proveedor activo; membresía técnica para catálogo/reservas |
| `Municipality`        | `/municipalidad/portal`                                                         | Capturas, estados, fotos, estadísticas y transferencias                      | Tenant municipal, cantones y tier                          |
| `Admin`               | `/admin`, `/estadisticas`, `/nala`, reportes                                    | Aprobaciones, planes, usuarios, moderación, auditoría, inventario y métricas | Rol privilegiado, MFA/políticas según entorno              |
| `Support`             | Colas autorizadas de bienestar e incidentes                                     | Triage, incidentes y resoluciones operativas                                 | Asignación manual por Admin; sin portal independiente      |

### 2.1 Rutas públicas importantes

- `/map`: mapa de pérdidas, avistamientos, clínicas, tiendas, adopciones y servicios.
- `/map/match`: búsqueda visual por foto.
- `/p/:id`: perfil público QR.
- `/p/:id/report-sighting`: reportar avistamiento.
- `/encontre-mascota` y `/encontre`: reportar mascota encontrada.
- `/clinicas`, `/tiendas`, `/servicios`: directorios públicos.
- `/adopciones`, `/adopciones/:id`, `/adopciones/ferias`: adopciones públicas.
- `/verificar/:code` y `/verificar/pasaporte/:code`: verificaciones públicas.

---

## 3. Features transversales del producto

| Área            | Estado verificable                                                                                         |
| --------------- | ---------------------------------------------------------------------------------------------------------- |
| Identidad QR    | QR único por mascota, perfil público mínimo y escaneos auditables                                          |
| Recuperación    | Reporte de pérdida, avistamientos, contacto relay, Case Room y reunificación                               |
| Coordinación    | Cuadrícula de búsqueda, zonas reclamables y SignalR en tiempo real                                         |
| Matching visual | Búsqueda por foto con integración de embeddings cuando el servicio está configurado                        |
| Comunicación    | Chat enmascarado, notificaciones, difusión por canales configurados y código de handover                   |
| Privacidad      | PII scrubber, minimización de DTOs, hashes de tokens/teléfonos y grants médicos                            |
| GPS             | Tags genéricos con serial/device key, telemetría, offline, batería, modo perdido, zonas seguras y handover |
| Salud           | Historial médico, vacunas, medicación, peso, recordatorios, grants y export según tier/scope               |
| Adopciones      | Directorio, animales, solicitudes, ferias y paneles de refugio                                             |
| Marketplace     | Tiendas con catálogo/pedidos y proveedores con catálogo/disponibilidad/reservas                            |
| Publicidad      | Campañas por placement, revisión, creativos, rotación, impresiones, clics y métricas                       |
| Institucional   | NALA, reportes agregados, municipalidades y superficies regulatorias                                       |
| Operación       | Auditoría, rate limits, jobs distribuidos, Application Insights y health checks                            |

---

## 4. Matriz completa B2C: dueños

| Feature                       | `Free`       | `UserPlus`                       | `UserFamilia`               |
| ----------------------------- | ------------ | -------------------------------- | --------------------------- |
| Mascotas                      | 1            | Hasta 3                          | Ilimitadas                  |
| Historial QR                  | Últimos 5    | Ilimitado                        | Ilimitado                   |
| Búsqueda visual IA            | 3/mes        | Ilimitada                        | Ilimitada                   |
| Alertas geográficas           | Radio base   | Multiplicador 3.33, aprox. 10 km | Sin límite efectivo         |
| Difusión multicanal           | No premium   | Sí                               | Sí                          |
| Case Room/coordinación        | Base         | Completa                         | Completa                    |
| GPS collar                    | No           | Sí                               | Sí                          |
| Recompensas/bounty            | No premium   | Sí                               | Sí                          |
| Miembros familiares           | Solo dueño   | Solo dueño                       | Hasta 5 miembros familiares |
| Historial médico              | Vista previa | Vista previa limitada            | Completo                    |
| Peso/medicación/recordatorios | No           | No                               | Sí                          |
| Export médico PDF             | No           | No                               | Sí                          |

### 4.1 Compra de planes B2C

Solo `UserPlus` y `UserFamilia` aceptan selección de plazo:

| Plazo    | Cálculo             | Descuento |
| -------- | ------------------- | --------- |
| 1 mes    | mensual x 1         | 0%        |
| 3 meses  | mensual x 3         | 0%        |
| 6 meses  | mensual x 6         | 0%        |
| 12 meses | mensual x 12 x 0.80 | 20%       |

El backend valida el plazo, calcula el importe, persiste `BillingMonths` y usa el
plazo persistido al activar. Esta modalidad no aplica a clínicas, tiendas,
refugios ni municipalidades.

Precios base actuales:

- `UserPlus`: ₡2,990/mes.
- `UserFamilia`: ₡4,990/mes.

Estos son precios técnicos de referencia del catálogo backend. La contratación
real se procesa actualmente mediante solicitud y verificación manual de SINPE.

---

## 5. Matriz B2B/B2G de planes

### 5.1 Clínicas veterinarias

| Feature                                               | `ClinicPlus` | `ClinicPartner`             |
| ----------------------------------------------------- | ------------ | --------------------------- |
| Registro/directorio/mapa                              | Sí           | Sí                          |
| Badge y destaque                                      | Sí           | Sí                          |
| Escaneo QR/microchip                                  | Sí           | Sí                          |
| Estadísticas y visibilidad                            | Sí           | Sí                          |
| Certificados PDF verificables                         | Sí           | Sí                          |
| API keys                                              | No           | Sí                          |
| Scopes API (`scan`, medical, certificates, analytics) | No           | Sí                          |
| Widget/API M2M                                        | No           | Sí                          |
| Grants y auditoría médica avanzada                    | Parcial      | Sí                          |
| Veterinarios y citas                                  | Parcial      | Sí                          |
| Export médico                                         | No           | Sí, con scope/grants/cuotas |

Precios base: `ClinicPlus` ₡15,000/mes; `ClinicPartner` ₡35,000/mes.
`ClinicBasic` es entrada/directorio, no plan comercial principal.

### 5.2 Tiendas

| Feature                    | `StoreBasic` entrada | `StorePlus`         | `StorePartner` |
| -------------------------- | -------------------- | ------------------- | -------------- |
| Directorio/mapa/perfil     | Sí                   | Sí                  | Sí             |
| Catálogo                   | Base limitada        | Sí                  | Sí             |
| Pedidos in-app             | No                   | Sí                  | Sí             |
| Gestión de pedidos         | No                   | Sí                  | Sí             |
| Analytics avanzados/export | No                   | No                  | Sí             |
| Sedes                      | No                   | Gate técnico        | Gate técnico   |
| Posicionamiento destacado  | No                   | Sí según activación | Sí             |

Precios base: `StorePlus` ₡12,000/mes; `StorePartner` ₡25,000/mes.
PawTrack comunica solicitudes: no intermedia fondos ni ofrece checkout SINPE
activo como parte del flujo de órdenes documentado.

### 5.3 Refugios y adopciones

| Feature                  | `ShelterBasic` entrada | `ShelterPlus` |
| ------------------------ | ---------------------- | ------------- |
| Publicar animales        | Hasta 5 activos        | Ilimitados    |
| Solicitudes de adopción  | Sí                     | Sí            |
| Panel de refugio         | Sí                     | Sí            |
| Ferias                   | No                     | Sí            |
| Pin destacado            | No                     | Sí            |
| Estadísticas/visibilidad | Base                   | Avanzada      |

Precio base `ShelterPlus`: ₡8,000/mes. `ShelterBasic` representa el acceso
gratuito de entrada, no un checkout separado.

### 5.4 Municipalidades

| Feature                         | `MuniBasica` | `MuniFull`        | `MuniRedRegional` |
| ------------------------------- | ------------ | ----------------- | ----------------- |
| Portal y capturas               | Sí           | Sí                | Sí                |
| Fotos                           | No           | Sí                | Sí                |
| Estadísticas/reportes           | No           | Sí                | Sí                |
| Multi-cantón                    | No           | Consulta ampliada | Sí                |
| Dashboard regional              | No           | No                | Sí                |
| Transferencias intermunicipales | No           | No                | Sí                |

Precios técnicos: ₡150,000, ₡300,000 y ₡500,000 anuales respectivamente.
La compra, renovación y contratación institucional siguen sujetas al alcance
comercial aprobado; no presentar los tiers técnicos como autoservicio activo.

---

## 6. Proveedores de servicios

Los proveedores no tienen un tier de suscripción comercial aprobado. Usan
membresías técnicas:

| Membresía  | Capacidades                                                  |
| ---------- | ------------------------------------------------------------ |
| `Free`     | Perfil/directorio y contacto básico                          |
| `Verified` | Prueba única de 30 días; catálogo, disponibilidad y reservas |
| `Featured` | Prioridad/destaque técnico; asignación manual posible        |

Capacidades implementadas: registro, aprobación, perfil, categorías, servicios,
modalidades, precios CRC, capacidad, disponibilidad, bloqueos, reservas,
verificación documental e incidentes. No están aprobados como producto:
comisiones, payout, checkout recurrente, KYC empresarial, multi-sede comercial,
SLA o ranking patrocinado.

El precio mostrado en cada servicio lo define el proveedor en colones; no es una
tarifa de PawTrack ni representa una comisión aprobada. Las reservas conservan
un snapshot comercial del servicio, pero la monetización de la plataforma sigue
pendiente de aprobación.

Categorías: `Trainer`, `Groomer`, `Hotel`, `Daycare`, `Walker`, `Photographer` y
`Other`.

### 6.1 Precios de proveedores

No existe un precio recurrente aprobado para proveedores. Las cifras que aparecen
en documentos de estrategia son propuestas para validar mercado, no cobros
activos:

| Membresía propuesta | Precio sugerido | Estado                                                                |
| ------------------- | --------------: | --------------------------------------------------------------------- |
| Perfil base         |          Gratis | Compatible con el acceso técnico `Free`                               |
| Verificado          |      ₡3,000/mes | Propuesta no aprobada; actualmente se usa una prueba única de 30 días |
| Destacado           |      ₡5,000/mes | Propuesta no aprobada; puede existir como asignación técnica/manual   |

No hay comisiones, payout, checkout recurrente ni factura activa para este segmento.

---

## 7. Publicidad y vallas

Hay 15 placements técnicos: `Map`, `Dashboard`, `Directory`, `Feed`, perfiles
QR, historial, Case Room, clínicas, proveedores, adopciones, ferias, registro y
CollarTag.

Capacidades activas: CRUD Admin, carga de creativo Blob, revisión por segundo
administrador, aprobación, activación/pausa/expiración, prioridad, rotación,
eventos de impresión/clic/conversión, deduplicación, frecuencia diaria y
métricas. En `/map`, las vallas se separan de la leyenda y se apilan para evitar
solapamientos. Cerrar una valla solo afecta la vista actual.

No deben venderse todavía como garantías activas: presupuesto automático,
ledger de cargos, exclusividad absoluta, segmentación geográfica real, ranking
patrocinado, portal de anunciante, facturación, antifraude avanzado o reportes
comerciales exportables.

### 7.1 Tarifas propuestas de vallas

`docs/publicidad.md` contiene una tarifa de referencia asignada para construir
el inventario comercial. Estas tarifas no están conectadas al backend, no
generan factura ni representan ingresos reconocidos hasta aprobar e implementar
el modelo de cobro. Para un inversionista, esto significa que existe una unidad
de monetización definida, pero todavía no una línea de revenue validada.

| Placement   | 1 semana |   1 mes |  3 meses |  6 meses | 12 meses |
| ----------- | -------: | ------: | -------: | -------: | -------: |
| `Map`       |  ₡18,000 | ₡55,000 | ₡140,000 | ₡240,000 | ₡400,000 |
| `Dashboard` |  ₡10,000 | ₡30,000 |  ₡75,000 | ₡130,000 | ₡220,000 |
| `Directory` |  ₡12,000 | ₡35,000 |  ₡90,000 | ₡155,000 | ₡260,000 |
| `Feed`      |  ₡22,000 | ₡65,000 | ₡165,000 | ₡290,000 | ₡490,000 |

Paquetes propuestos: `Visibilidad` ₡75,000/mes o ₡720,000/año;
`Presencia` ₡175,000/mes o ₡1,680,000/año; `Socio Anual` ₡1,400,000/año.
La exclusividad de categoría propuesta agrega ₡8,000 por semana, ₡20,000 por
mes o ₡50,000 por tres meses. Los importes no incluyen IVA.

Los placements contextuales propuestos son: perfil QR ₡40,000/mes o ₡1,500
por clic atribuido; historial de escaneos ₡25,000/mes; Case Room ₡50,000 por
cantón; directorio/perfil de clínica o servicio ₡35,000/mes; adopciones/ferias
₡30,000/mes; confirmación de registro o CollarTag ₡25,000/mes.

Descuentos propuestos: 20% para clientes Store/Clinic activos, 25-30% por
prepago de 6/12 meses, 50% o gratuidad temporal para ONG/aliados y una semana
gratis en Map para el primer anunciante. No son acumulables y requieren aprobación.

**Estado de monetización:** tarifas asignadas para venta consultiva/manual;
checkout, facturación, CPM/CPC/CPA, ledger de cargos y conciliación siguen
pendientes. Por tanto, las vallas deben presentarse como **pipeline comercial
potencial**, no como ingresos actuales.

---

## 8. Admin y Support

| Capacidad                               | `Admin` | `Support`                 |
| --------------------------------------- | ------- | ------------------------- |
| Aprobación de aliados/clínicas/tiendas  | Sí      | No                        |
| Gestión de planes y promociones         | Sí      | No                        |
| Adopciones administrativas              | Sí      | No                        |
| Bienestar: triage/asignación/resolución | Sí      | Sí, según autorización    |
| Incidentes de proveedores               | Sí      | Sí                        |
| Auditoría global                        | Sí      | No                        |
| NALA y reportes institucionales         | Sí      | No, salvo scope explícito |
| Inventario CollarTags                   | Sí      | No                        |
| Vallas publicitarias                    | Sí      | No                        |

---

## 9. Autorización y límites

La matriz completa de ownership, BOLA/IDOR, scopes y pruebas está en
[API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md). Reglas esenciales:

- `Authorize` y el rol nunca sustituyen ownership o tenant isolation.
- Los endpoints públicos devuelven DTO mínimo y aplican rate limit/payload limit.
- Las clínicas requieren grants y scopes para expediente/export.
- Las municipalidades quedan aisladas por organización y cantón.
- Los proveedores solo acceden a sus perfiles, servicios, reservas e incidentes.
- NALA y funnel son superficies privilegiadas y agregadas.
- Tokens, claves, handover codes y documentos no deben aparecer en logs.

---

## 10. Fuente de verdad y documentos relacionados

Orden de autoridad para resolver contradicciones:

1. Código activo de `backend/src` y `frontend/src`.
2. `docs/STATUS.md` y este documento.
3. `docs/FEATURES.md` y `docs/PRICING_AND_PLANS.md`.
4. `docs/API_AUTHORIZATION_MATRIX.md` y `docs/B2B_ESTADO_ACTUAL.md`.
5. Manuales operativos por rol.
6. Propuestas históricas, pricing antiguo y roadmaps.

Fuentes técnicas primarias:

- `backend/src/PawTrack.Domain/Auth/UserRole.cs`
- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs`
- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs`
- `backend/src/PawTrack.Infrastructure/Subscriptions/SubscriptionService.cs`
- `frontend/src/app/routes.tsx`
- `docs/NALA.md`
- `docs/FEATURES.md`
- `docs/API_AUTHORIZATION_MATRIX.md`
- `docs/B2B_ESTADO_ACTUAL.md`
- `docs/Manuales/`

Las capacidades marcadas como técnicas no deben convertirse en promesa
comercial hasta existir aprobación, contrato, facturación, soporte y evidencia
operativa correspondiente.

---

## 11. Estimación financiera, usuarios y margen esperado

> **Naturaleza:** escenario base de planificación, no presupuesto aprobado ni
> resultado contable. Cifras en colones costarricenses (CRC), redondeadas.
>
> **Margen:** EBITDA operativo estimado = ingresos - gastos operativos. No
> incluye impuestos, IVA cobrado, depreciación, deuda, dividendos ni capital.

### 11.1 Supuestos de crecimiento y conversión

- La métrica de usuario es una cuenta Owner registrada; no equivale a usuario
  activo mensual ni a mascota registrada.
- `MAU` representa dueños que realizan al menos una acción mensual relevante.
- La conversión pagada se calcula sobre Owners registrados, no sobre MAU.
- Los precios B2C usados son ₡2,990 para Plus y ₡4,990 para Familia. Se asume
  que 20% de los clientes B2C usa el plazo anual y recibe el descuento del 20%.
- El caso base no reconoce ingresos por proveedores ni vallas hasta que tengan
  billing aprobado. Tampoco reconoce venta de hardware/QR.
- Los clientes B2B se expresan como cuentas promedio pagadas durante el año;
  el cierre de contratos puede producir ingresos diferidos.

### 11.2 Proyección de usuarios y clientes

| Horizonte       | Owners registrados |    MAU | Plus promedio | Familia promedio | Conversión pagada | Clínicas | Tiendas | Refugios | Municipios |
| --------------- | -----------------: | -----: | ------------: | ---------------: | ----------------: | -------: | ------: | -------: | ---------: |
| Mes 3 acumulado |              2,500 |  1,300 |            80 |               20 |              4.0% |        3 |       3 |        1 |          0 |
| Mes 12 / Año 1  |             15,000 |  7,000 |           750 |              300 |              7.0% |       12 |      15 |        5 |          1 |
| Año 2           |             35,000 | 17,000 |         1,800 |              700 |              7.1% |       25 |      30 |       10 |          2 |
| Año 3           |             70,000 | 34,000 |         3,500 |            1,500 |              7.1% |       45 |      50 |       20 |          4 |
| Año 4           |            120,000 | 58,000 |         6,000 |            2,500 |              7.1% |       70 |      80 |       30 |          6 |
| Año 5           |            200,000 | 95,000 |         9,500 |            4,500 |              7.0% |      100 |     120 |       45 |         10 |

La conversión pagada debe sustituirse con cohortes reales después de medir
activación, churn mensual, retención a 90 días y uso por plan.

### 11.3 Ingresos por línea

| Horizonte       |       B2C | Clínicas |  Tiendas | Refugios | Municipalidades | Total ingresos |
| --------------- | --------: | -------: | -------: | -------: | --------------: | -------------: |
| Mes 3 acumulado |   ₡0.52 M |  ₡0.04 M |  ₡0.07 M |  ₡0.01 M |              ₡0 |    **₡0.64 M** |
| Mes 12 / Año 1  |  ₡44.85 M |  ₡3.12 M |  ₡2.94 M |  ₡0.48 M |         ₡0.15 M |   **₡51.54 M** |
| Año 2           | ₡102.48 M |  ₡6.90 M |  ₡5.88 M |  ₡0.96 M |         ₡0.45 M |  **₡116.67 M** |
| Año 3           | ₡188.62 M | ₡11.70 M | ₡10.32 M |  ₡1.92 M |         ₡1.20 M |  **₡213.76 M** |
| Año 4           | ₡341.23 M | ₡19.80 M | ₡17.76 M |  ₡2.88 M |         ₡1.80 M |  **₡383.47 M** |
| Año 5           | ₡563.35 M | ₡27.90 M | ₡26.64 M |  ₡4.32 M |         ₡3.00 M |  **₡625.21 M** |

El cálculo B2C usa clientes promedio por año y precio mensual; el descuento
anual ya está reflejado en el promedio. Los ingresos institucionales se
reconocen solo cuando existe contrato o activación comercial válida.

### 11.4 Gastos detallados por categoría

| Categoría               | Qué cubre                                                    | Mes 3 acumulado |     Año 1 |     Año 2 |      Año 3 |      Año 4 |      Año 5 |
| ----------------------- | ------------------------------------------------------------ | --------------: | --------: | --------: | ---------: | ---------: | ---------: |
| Personal y operación    | Desarrollo, producto, administración y dirección             |          ₡2.1 M |     ₡18 M |     ₡40 M |      ₡75 M |     ₡135 M |     ₡220 M |
| Infraestructura         | SQL, hosting, Blob, Redis, App Insights, backups y dominios  |          ₡0.3 M |      ₡3 M |      ₡7 M |      ₡14 M |      ₡25 M |      ₡42 M |
| Servicios variables     | Email, WhatsApp, mapas, IA, push y procesamiento de imágenes |          ₡0.2 M |      ₡2 M |      ₡4 M |       ₡7 M |      ₡10 M |      ₡15 M |
| Marketing y adquisición | Campañas, contenido, activación local y CAC                  |          ₡1.2 M |      ₡9 M |     ₡18 M |      ₡30 M |      ₡48 M |      ₡70 M |
| Ventas y onboarding     | Materiales, viajes, demos y alta B2B                         |          ₡0.3 M |      ₡4 M |      ₡7 M |      ₡12 M |      ₡20 M |      ₡30 M |
| Soporte y moderación    | Atención, casos, adopciones, incidentes y trust & safety     |          ₡0.3 M |    ₡2.5 M |      ₡5 M |      ₡10 M |      ₡18 M |      ₡25 M |
| Legal y cumplimiento    | Contratos, privacidad, impuestos, seguridad y auditorías     |          ₡0.2 M |    ₡1.5 M |      ₡3 M |       ₡6 M |      ₡10 M |      ₡15 M |
| Contingencia            | Fraude, incidentes, disponibilidad y variaciones             |          ₡0.2 M |      ₡2 M |      ₡4 M |       ₡6 M |       ₡9 M |      ₡13 M |
| **Total gastos**        |                                                              |      **₡4.8 M** | **₡42 M** | **₡88 M** | **₡160 M** | **₡275 M** | **₡430 M** |

Los mayores multiplicadores de gasto son personal, adquisición y soporte. Azure
por sí solo no explica el costo total del negocio; reducir infraestructura sin
controlar CAC o atención operativa no mejora necesariamente el margen.

### 11.5 EBITDA y margen esperado

| Horizonte       |  Ingresos |  Gastos |    EBITDA | Margen EBITDA |
| --------------- | --------: | ------: | --------: | ------------: |
| Mes 3 acumulado |   ₡0.64 M | ₡4.80 M |  -₡4.16 M |       -650.0% |
| Mes 12 / Año 1  |  ₡51.54 M |   ₡42 M |   ₡9.54 M |         18.5% |
| Año 2           | ₡116.67 M |   ₡88 M |  ₡28.67 M |         24.6% |
| Año 3           | ₡213.76 M |  ₡160 M |  ₡53.76 M |         25.1% |
| Año 4           | ₡383.47 M |  ₡275 M | ₡108.47 M |         28.3% |
| Año 5           | ₡625.21 M |  ₡430 M | ₡195.21 M |         31.2% |

El margen negativo del mes 3 es normal en una etapa de adquisición y validación;
no debe compararse directamente con el margen anual. El punto de equilibrio
operativo del caso base se alcanza durante el año 1 si los ingresos acumulados
superan aproximadamente ₡42 M sin exceder el presupuesto de adquisición.

### 11.6 Ingresos no incluidos y potencial adicional

| Línea futura           | Estado                                             | Cómo estimarla cuando exista billing                       |
| ---------------------- | -------------------------------------------------- | ---------------------------------------------------------- |
| Vallas                 | Tarifas de referencia asignadas; no billing activo | Placement, duración, descuentos y exclusividad por aprobar |
| Proveedores            | Membresías propuestas, no activas                  | Proveedores pagados x tarifa x retención                   |
| Hardware/QR            | No consolidado                                     | Unidades vendidas x margen unitario                        |
| Comisiones de reservas | No aprobadas                                       | GMV x comisión después de legal, payout y reembolsos       |
| Bounty                 | Requiere modelo de pago/escrow                     | Fee de plataforma por recompensa liquidada                 |

### 11.7 Sensibilidad y métricas de control

| Variación                               | Impacto esperado                                                                               |
| --------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Owners pagados 30% debajo del caso base | Año 1 puede terminar con EBITDA negativo y equilibrio retrasado                                |
| CAC 25% mayor                           | Reduce aproximadamente 5-10 puntos de margen en los primeros 24 meses                          |
| Churn mensual 2 puntos mayor            | Reduce MRR, LTV y capacidad de financiar adquisición                                           |
| Dos municipalidades adicionales         | Aumentan ingresos, onboarding, soporte y cumplimiento                                          |
| Billing de vallas aprobado              | Convierte tarifas asignadas en revenue medible, pero requiere ventas, facturación y moderación |

Actualizar mensualmente: MRR, ARR, ARPU, CAC, LTV, churn, conversión Free a
pagado, MAU, costo por caso, costo de soporte, ingreso por B2B, ocupación de
proveedores y margen por segmento. Estas cifras son planificación, no utilidad
neta ni garantía para inversionistas.
