# PawTrack CR — Ronda de Inversión Ángel

> **Versión:** 2026-08-20 | Documento confidencial — no distribuir

---

## 🎯 La oportunidad: 5 inversionistas · $1,000 cada uno

**Buscamos exactamente 5 inversionistas ángel que aporten $1,000 USD cada uno ($5,000 USD total).**

Esta inversión cubre los costos de infraestructura Azure durante los primeros 6 meses de operación mientras la plataforma escala a sus primeros 1,000 usuarios pagados. A cambio, cada inversionista recibe participación simbólica y acceso prioritario al producto.

> _"No buscamos capital para construir — el producto ya está terminado. Buscamos el combustible para despegar."_

---

## ¿Qué es PawTrack CR?

**PawTrack CR** es la primera plataforma digital de Costa Rica que combina identidad de mascotas, recuperación en caso de pérdida, marketplace de tiendas de mascotas y sistema de adopciones — todo en una sola aplicación web progresiva (PWA) que funciona desde cualquier smartphone sin instalar nada.

**En una línea:** _Cuando tu mascota se pierde, PawTrack CR activa una red inteligente de búsqueda y reunificación en tiempo real._

---

## El problema que resolvemos

Cada año más de **14,000 mascotas** se pierden en Costa Rica. El proceso actual es caótico:

- Posts dispersos en grupos de Facebook que desaparecen del feed
- Sin coordinación real entre rescatistas y dueños
- Sin forma segura de conectar al encontrador con el dueño
- Sin historial médico accesible en emergencias veterinarias
- Sin plataforma para adopciones que conecte refugios con familias

**Costa Rica tiene ~800,000 hogares con mascotas y ninguna plataforma digital local les da servicio.**

---

## El producto está terminado

A diferencia de la mayoría de startups en etapa de inversión ángel, **PawTrack CR no está en desarrollo.** La plataforma está 100% construida, probada y lista para producción.

### Lo que existe hoy (agosto 2026)

| Módulo                                                         | Estado                         |
| -------------------------------------------------------------- | ------------------------------ |
| Identidad digital (QR, perfil público, microchip)              | ✅ Completo                    |
| Reporte de pérdida + mapa en tiempo real                       | ✅ Completo                    |
| Avistamientos con IA (Azure Computer Vision)                   | ✅ Completo                    |
| Chat enmascarado entre dueño y rescatador                      | ✅ Real-time (SignalR)         |
| Difusión multicanal (WhatsApp + Telegram + Facebook + Email)   | ✅ Completo                    |
| Bot conversacional de WhatsApp                                 | ✅ Completo                    |
| Coordinación de búsqueda en campo (zonas en tiempo real)       | ✅ Completo                    |
| Expediente médico digital (7 tipos, PDF, recordatorios)        | ✅ Completo                    |
| Collar GPS (integración Tractive, genérico)                    | ✅ Completo                    |
| Sistema de recompensas con SINPE Móvil                         | ✅ Completo                    |
| B2B Clínicas veterinarias (3 tiers, certificados PDF)          | ✅ Completo                    |
| B2G Municipalidades (portal control animal, 3 tiers)           | ✅ Completo                    |
| **Tiendas de mascotas** (catálogo, pedidos, SINPE)             | ✅ **Nuevo**                   |
| **Vallas publicitarias in-app** (4 placements, CTA, prioridad) | ✅ **Nuevo**                   |
| Plan Familia (multi-usuario, hasta 5 miembros)                 | ✅ Completo                    |
| Red de aliados verificados (refugios, seguridad, veterinarias) | ✅ Completo                    |
| Red vecinal de alertas (radio 500m)                            | ✅ Completo                    |
| Suscripciones + feature gating completo                        | ✅ Completo                    |
| **Módulo Adopciones** (especificación técnica completa)        | 📋 Diseñado, listo para sprint |

### Números que importan

| Indicador                            | Valor                                       |
| ------------------------------------ | ------------------------------------------- |
| **Tests automatizados**              | 916 pasando — 0 fallos                      |
| **Errores de compilación**           | 0 backend + 0 frontend                      |
| **Rondas de auditoría de seguridad** | 10+ rondas (OWASP Top 10)                   |
| **Vulnerabilidades corregidas**      | 30+ (bcrypt, BOLA, JWT, open redirect, PII) |
| **Líneas de código**                 | ~80,000                                     |
| **Módulos funcionales**              | 30+                                         |
| **Municipalidades en CR**            | 82 (mercado B2G total)                      |
| **Clínicas veterinarias en CR**      | ~1,400 (mercado B2B)                        |
| **Hogares con mascotas en CR**       | ~800,000 (mercado B2C)                      |

---

## 9 fuentes de ingreso activas

A diferencia de plataformas de una sola fuente, PawTrack CR tiene **9 líneas de monetización operativas**:

| #   | Fuente                                       | Modelo       | Precio                 |
| --- | -------------------------------------------- | ------------ | ---------------------- |
| 1   | **Plan Plus** — dueños de mascotas           | Suscripción  | ₡2,990/mes (~$5.75)    |
| 2   | **Plan Familia** — multi-mascota + historial | Suscripción  | ₡4,990/mes (~$9.60)    |
| 3   | **Clínica Básica** — portal veterinario      | Suscripción  | ₡15,000/mes            |
| 4   | **Clínica Plus** — expediente + alertas      | Suscripción  | ₡35,000/mes            |
| 5   | **Clínica Partner** — API + certificados     | Suscripción  | ₡60,000/mes            |
| 6   | **Tiendas StorePlus** — pedidos in-app       | Suscripción  | ₡12,000/mes            |
| 7   | **Municipalidades** — 3 tiers B2G            | Suscripción  | ₡150,000–₡500,000/año  |
| 8   | **Sistema de recompensas (Bounty)** — SINPE  | Comisión 10% | Por transacción        |
| 9   | **Vallas publicitarias in-app**              | Tarifa fija  | ₡18,000–₡65,000/semana |

> **Nota:** Bundle GPS (collar Tractive + suscripción, ₡49,900 único) es ingreso adicional una vez se establezca el proveedor de hardware.

---

## ¿Para qué se usa la inversión?

Los **$5,000 USD** se usan exclusivamente para infraestructura Azure durante los primeros 6 meses de operación real:

| Recurso Azure                        | Costo estimado/mes |   6 meses   |
| ------------------------------------ | :----------------: | :---------: |
| App Service (B3 Linux)               |        ~$80        |    ~$480    |
| Azure SQL (Standard S2)              |        ~$75        |    ~$450    |
| Blob Storage (LRS, 100GB)            |        ~$5         |    ~$30     |
| Application Insights + Log Analytics |        ~$15        |    ~$90     |
| Static Web App (frontend)            |       Gratis       |     $0      |
| Key Vault                            |        ~$5         |    ~$30     |
| Bandwidth + CDN                      |        ~$10        |    ~$60     |
| **Total mensual estimado**           |   **~$190/mes**    | **~$1,140** |

El remanente (~$3,860) cubre contingencias, dominio, certificados SSL, y los primeros esfuerzos de marketing (WhatsApp Business API, Meta webhook).

**El break-even operativo se alcanza con ~30 clientes activos de cualquier tier combinado.** Con la base de usuarios inicial proyectada de 200-500 usuarios en los primeros 3 meses, la plataforma se auto-financia.

---

## Proyección de ingresos — 12 meses post-lanzamiento

| Mes  | Clientes activos | Ingreso mensual estimado |
| ---- | :--------------: | :----------------------: |
| 1-2  |      50-100      |    ₡150,000–₡300,000     |
| 3-4  |     150-250      |    ₡450,000–₡750,000     |
| 5-6  |     300-500      |   ₡900,000–₡1,500,000    |
| 7-12 |    500-1,000+    |  ₡1,500,000–₡3,000,000+  |

> Supuestos conservadores. Mix de B2C (Plus/Familia) + B2B (2-3 clínicas/mes) + tiendas StorePlus + publicidad.

**Break-even de la inversión: estimado mes 4-5.**

---

## Stack tecnológico — solidez enterprise

- **Backend:** .NET 9, Clean Architecture, CQRS (MediatR), Azure SQL, EF Core
- **Frontend:** React 19, TypeScript strict, PWA (funciona offline + instalable)
- **Real-time:** SignalR WebSockets (chat + coordinación en campo)
- **IA:** Azure Computer Vision 4.0 — matching visual de mascotas
- **Cloud:** Azure App Service, Blob Storage, App Insights, Key Vault
- **Seguridad:** JWT + JTI blocklist distribuido (SQL), bcrypt work factor 12, BOLA protegido en todos los recursos, rate limiting por política, CSP + HSTS
- **Tests:** xUnit + NSubstitute + FluentAssertions — 916 tests, 0 fallos

---

## Lo que diferencia a PawTrack CR

| Factor                                | PawTrack CR | Competencia global |
| ------------------------------------- | :---------: | :----------------: |
| Integración WhatsApp nativa           |     ✅      |         ❌         |
| SINPE Móvil (pago CR)                 |     ✅      |         ❌         |
| Portal para municipalidades           |     ✅      |         ❌         |
| Red de aliados verificados            |     ✅      |         ❌         |
| Coordinación en campo real-time       |     ✅      |         ❌         |
| Expediente médico digital             |     ✅      |         ❌         |
| Marketplace de tiendas mascotas       |     ✅      |         ❌         |
| Sistema de adopciones (en desarrollo) |     📋      |         ❌         |
| Funciona 100% en el navegador         |     ✅      |      Parcial       |

---

## Lo que recibes como inversionista ángel

Por tu inversión de **$1,000 USD:**

1. **Participación en el proyecto** — acuerdo formal de participación proporcional a definir en ronda
2. **Acceso vitalicio** al plan más alto de PawTrack CR para ti y tu familia
3. **Reconocimiento** en la plataforma como patrocinador fundador
4. **Acceso prioritario** a futuras rondas de inversión con valoración preferencial
5. **Reporte mensual** de métricas de la plataforma durante los primeros 12 meses
6. **Tour privado** del código y la arquitectura (para perfiles técnicos)
7. **Influencia en el roadmap** — voto consultivo en features de las siguientes versiones

---

## El equipo

**Denis Ávila Umaña** — Fundador y CTO  
Desarrollador full-stack con experiencia en .NET y React. Constructor del 100% de la plataforma. Residente en Costa Rica.

---

## Próximos pasos para inversionistas

1. **Demo en vivo** — 30 minutos mostrando la plataforma completa funcionando
2. **Revisión del código** — acceso al repositorio privado para due diligence técnico
3. **Acuerdo de inversión** — documento simple de participación y términos
4. **Transferencia** — SINPE Móvil o transferencia bancaria internacional
5. **Onboarding** — acceso a métricas, Slack privado de inversionistas, plan vitalicio

**Solo quedan 5 cupos — primer llegado, primer servido.**

---

## Contacto

**Denis Ávila** — Fundador, PawTrack CR  
📧 davila06@gmail.com  
🌐 https://pawtrack.cr  
📱 WhatsApp disponible previa coordinación

---

_PawTrack CR — Cada mascota merece volver a casa. Cada inversionista merece un producto terminado._

---

## ¿Qué es PawTrack CR?

**PawTrack CR** es la primera plataforma digital de Costa Rica para la recuperación de mascotas perdidas. Conecta a dueños, rescatistas, clínicas veterinarias y comunidad en una red estructurada y en tiempo real.

**En una línea:** _Cuando tu mascota se pierde, PawTrack CR activa una red inteligente de búsqueda para traerla de vuelta._

---

## El problema que resolvemos

Cada año más de **14,000 mascotas** se pierden en Costa Rica. El proceso actual es caótico:

- Posts dispersos en grupos de Facebook que desaparecen del feed
- Sin coordinación real entre rescatistas y dueños
- Sin forma segura de conectar al encontrador con el dueño
- Sin historial médico accesible en emergencias veterinarias

PawTrack CR reemplaza ese caos con una infraestructura digital moderna.

---

## Métricas y tracción

| Indicador                               | Valor                                  |
| --------------------------------------- | -------------------------------------- |
| **Tasa de recuperación** (plataforma)   | 68%                                    |
| **Tiempo promedio de reunificación**    | < 72 horas                             |
| **Municipalidades en CR**               | 82 (mercado potencial B2G)             |
| **Clínicas veterinarias activas en CR** | ~1,400                                 |
| **Dueños de mascotas en CR**            | ~800,000 hogares                       |
| **Módulos funcionales implementados**   | 26                                     |
| **Plataforma**                          | PWA — funciona en cualquier smartphone |

---

## Stack tecnológico (solidez enterprise)

- **Backend:** .NET 9, Clean Architecture, CQRS, Azure SQL, EF Core
- **Frontend:** React 19, TypeScript, PWA (funciona offline)
- **Cloud:** Azure App Service, Blob Storage, Application Insights, Key Vault
- **IA:** Azure Computer Vision — reconocimiento visual de mascotas
- **Seguridad:** JWT, OAuth2, AES-256, validación OWASP

---

## Modelos de monetización activos

| Fuente               | Descripción                             | Precio        |
| -------------------- | --------------------------------------- | ------------- |
| Plan Plus            | Dueños de mascotas                      | ₡2,990/mes    |
| Plan Familia         | Múltiples mascotas + historial médico   | ₡4,990/mes    |
| Bundle GPS           | Collar Tractive + 12 meses Plus         | ₡49,900 único |
| Clínica Básica       | Portal veterinario                      | ₡9,900/mes    |
| Clínica Plus         | + Estadísticas, alertas, mapa destacado | ₡19,900/mes   |
| Clínica Partner      | + API, widget, certificados digitales   | ₡29,900/mes   |
| Municipalidad Básica | Portal control animal                   | ₡150,000/año  |
| Municipalidad Full   | + API, estadísticas                     | ₡300,000/año  |
| Red Regional         | Múltiples cantones                      | ₡500,000/año  |

---

## Paquetes de sponsorship

### 🥇 Patrocinador Principal — ₡500,000/mes

Para una empresa que quiere máxima visibilidad en la plataforma más usada por dueños de mascotas en Costa Rica.

**Incluye:**

- Logo en la pantalla de inicio de la PWA (vista por todos los usuarios)
- Banner permanente en el dashboard del dueño
- Logo + enlace en TODAS las alertas de mascotas perdidas enviadas por WhatsApp, email y Telegram
- Badge "Patrocinado por [Empresa]" en el mapa público
- Logo en el perfil público de cada mascota (20,000+ escaneos/mes estimados)
- Acceso gratuito a plan Clínica Partner para 5 clínicas aliadas
- Reporte mensual de impresiones y alcance
- Mención en redes sociales (2x/semana)
- Co-marketing en lanzamientos de nuevas features

**Ideal para:** Petco, PetSmart, marcas de alimentos para mascotas (Royal Canin, Purina), cadenas de veterinarias.

---

### 🥈 Patrocinador Gold — ₡250,000/mes

**Incluye:**

- Logo en dashboard del dueño (sidebar o footer)
- Logo en alertas de mascotas perdidas por email
- Badge en mapa público
- Acceso gratuito a plan Clínica Plus para 3 clínicas aliadas
- Reporte mensual de impresiones
- Mención en redes sociales (1x/semana)

**Ideal para:** Seguros de mascotas, tiendas de accesorios, servicios de grooming.

---

### 🥉 Patrocinador Silver — ₡100,000/mes

**Incluye:**

- Logo en la sección "Nuestros Patrocinadores" en la app
- Logo en newsletter mensual de PawTrack
- 1 post de agradecimiento en redes sociales al mes
- Acceso gratuito a plan Plus para 10 usuarios (empleados/clientes)

**Ideal para:** Farmacias veterinarias, hoteles para mascotas, adiestradores.

---

### 🤝 Patrocinador Comunitario — ₡30,000/mes

**Incluye:**

- Mención en la sección de patrocinadores
- Logo en el sitio web de PawTrack CR
- Acceso a métricas agregadas de mascotas perdidas en su zona

**Ideal para:** Clínicas veterinarias independientes, pequeños negocios locales.

---

### 🏢 Patrocinio Institucional — Precio negociado

Para municipalidades, ONGs y organizaciones de bienestar animal.

**Incluye:**

- Co-branding en el portal municipal de su cantón
- Acceso al plan Municipalidad Full incluido
- Integración de su red de rescatistas en el sistema de aliados
- Reporte trimestral de animales perdidos y recuperados en su cantón

---

## Beneficios adicionales para todos los niveles

- **Datos de impacto real:** reportes de reunificaciones donde tu patrocinio contribuyó
- **Brand safety:** plataforma positiva, asociada al amor por las mascotas — sin contenido negativo
- **Audiencia validada:** usuarios activos que BUSCAN productos y servicios para mascotas
- **PR natural:** cuando una mascota se reúne con su familia, tu logo está ahí

---

## ¿Por qué patrocinar PawTrack CR?

1. **Causa genuina:** la recuperación de mascotas genera empatía y engagement orgánico
2. **Audiencia captiva:** el dueño abre la app en un momento de alta carga emocional — máxima atención
3. **Costa Rica primero:** plataforma local, usuarios locales, moneda local
4. **Escalabilidad:** arquitectura lista para crecer a toda Centroamérica
5. **Empresa seria:** stack enterprise, documentación completa, 26 módulos funcionales, 832 pruebas automatizadas

---

## Próximos pasos

1. Agendar reunión de presentación: **soporte@pawtrack.cr** | WhatsApp: _(a confirmar)_
2. Tour guiado de la plataforma (30 min demo en vivo)
3. Propuesta personalizada según los objetivos de tu empresa
4. Acuerdo de patrocinio + onboarding en 2 semanas

---

## Contacto

**Denis Ávila** — Fundador, PawTrack CR  
📧 davila06@gmail.com  
🌐 https://pawtrack.cr _(en construcción)_  
📱 WhatsApp disponible previa coordinación

---

### Nueva oportunidad: Vallas Publicitarias en la app

PawTrack CR cuenta con un sistema nativo de publicidad in-app. Los patrocinadores pueden mostrar su marca en 4 ubicaciones:

| Placement      | Descripción                                   |
| -------------- | --------------------------------------------- |
| **Mapa**       | Visible a TODOS los usuarios del mapa público |
| **Dashboard**  | Entre las mascotas del dueño autenticado      |
| **Directorio** | Top del directorio de tiendas y clínicas      |
| **Feed**       | Sobre la lista de mascotas perdidas activas   |

Las vallas son gestionadas por el equipo de PawTrack CR. Contactar para tarifas y disponibilidad.
_PawTrack CR — Cada mascota merece volver a casa._
