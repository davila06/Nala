# Auditoría de Cumplimiento — Protección de Datos Personales

> Análisis de PawTrack CR contra la **Ley N.° 8968** (Ley de Protección de la Persona
> frente al Tratamiento de sus Datos Personales, Costa Rica), su Reglamento, la
> supervisión de **PRODHAB**, y mejores prácticas internacionales (alineadas con los
> principios de GDPR como referencia de estándar, sin que GDPR aplique directamente).
> Fecha del análisis: 2026-09-03.

---

## 1. Resumen ejecutivo

PawTrack CR ya tiene una base sólida de cumplimiento: política de privacidad y
términos de uso vigentes que referencian explícitamente la Ley 8968, principios de
"privacidad por diseño" aplicados en el código (anonimización de reportantes, chat
enmascarado, hash de teléfonos), y controles de seguridad técnica extensos (más de
100 rondas de auditoría de seguridad, BOLA fixes, rate limiting, JWT+refresh
seguros).

Durante este análisis se encontró y **corrigió un bug crítico**: el flujo de
eliminación de cuenta (`DeleteAccountCommandHandler`) comparaba un hash de BCrypt
recién generado contra el hash almacenado usando `==` en vez de `Verify()`. Como
BCrypt genera una sal aleatoria distinta en cada llamada, esta comparación **nunca
podía ser verdadera** — ningún usuario podía eliminar su cuenta exitosamente por la
vía de autoservicio, sin importar que ingresara la contraseña correcta. Esto rompía
directamente el **derecho de cancelación/supresión** (ARCO) prometido en la Política
de Privacidad §11. Ya está corregido y cubierto por test de regresión.

Quedan **gaps genuinos pendientes** (documentados en la §4) que requieren decisión de
producto/legal antes de implementarse — no son bugs de código, sino funcionalidad
que aún no existe: exportación de datos autoservicio, verificación de edad al
registro, y consentimiento específico y diferenciado para datos de salud/GPS.

---

## 2. Marco legal aplicable

### 2.1 Ley 8968 (Costa Rica) — obligaciones clave

| Principio / obligación                      | Fuente                    | Resumen                                                                                  |
| -------------------------------------------- | ------------------------- | ------------------------------------------------------------------------------------------ |
| Consentimiento informado y expreso           | Art. 5, 7                 | El tratamiento requiere consentimiento salvo excepciones legales (ejecución contractual, interés vital, etc.) |
| Datos sensibles                              | Art. 9                    | Salud, biométricos y otros datos sensibles requieren consentimiento **expreso y diferenciado** del consentimiento general |
| Calidad y finalidad de los datos             | Art. 6, 11                | Solo recolectar lo necesario para la finalidad declarada; no usar para fines incompatibles |
| Derechos ARCO                                | Art. 4, 8, 27–33          | Acceso, Rectificación, Cancelación (supresión), Oposición — deben poder ejercerse de forma efectiva |
| Seguridad de la información                  | Art. 10                   | Medidas técnicas y organizativas para evitar alteración, pérdida, tratamiento no autorizado |
| Registro de bases de datos ante PRODHAB      | Art. 22 (Reglamento)      | Bases de datos con fines comerciales que contienen datos personales deben registrarse ante PRODHAB |
| Transferencia internacional de datos         | Art. 14                   | Requiere consentimiento o garantías equivalentes de protección en el país destino          |
| Conservación proporcional                    | Art. 6, 11                | No conservar datos más tiempo del necesario para la finalidad                              |

### 2.2 Buenas prácticas internacionales usadas como referencia (no aplican directamente)

- **GDPR (UE):** principios de *privacy by design/default*, *data minimization*,
  *right to erasure* (Art. 17), *data portability* (Art. 20), *edad de consentimiento
  digital* (Art. 8, típicamente 13–16 años según el país).
- **OWASP ASVS / Top 10:** controles técnicos de autenticación, autorización (BOLA),
  cifrado y gestión de secretos — ya cubiertos extensamente en este proyecto (ver
  `pendientesTotales.md` §3).

---

## 3. Hallazgos — Cumple / bien implementado

Verificado directamente contra el código (no solo contra la política escrita):

| Área                                  | Evidencia en código                                                                                                   |
| --------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| Anonimato del reportante de avistamientos | Los avistamientos no almacenan identidad del reportante — diseño confirmado en `NALA.md` y el módulo `Sightings`      |
| Chat enmascarado                       | Contacto dueño↔rescatador sin exponer teléfono real; validado en `ChatContactGuardTests`                              |
| Hash de teléfonos (bot WhatsApp)       | HMAC-SHA256 con clave secreta, no SHA-256 plano (ya corregido en rondas de seguridad previas)                         |
| Contraseñas                            | BCrypt work factor 12, `Verify()` usado correctamente en login/`ChangePassword` (y ahora también en `DeleteAccount`)  |
| JWT + refresh                          | Access token en memoria (nunca `localStorage`), refresh en cookie `httpOnly`/`Secure`/`SameSite`, JTI blocklist en SQL |
| BOLA (acceso a datos de terceros)      | Ownership checks explícitos en collares, expediente médico, pedidos — auditado extensamente esta sesión               |
| Minimización — collar GPS              | `CollarLocationPurgeJob` purga ubicaciones >30 días; historial expuesto solo por rango, máx. 10,000 puntos            |
| Minimización — QR scans                | `QrScanRetentionJob` corre diario, purga eventos antiguos                                                             |
| Minimización — vistas de perfil clínica | `ClinicProfileViewPurgeHostedService` purga vistas antiguas                                                           |
| Cookies                                | Banner con opciones **igualmente prominentes** "Aceptar todo" / "Solo esenciales" — buena práctica (no solo "aceptar") |
| Rate limiting anti fuerza bruta        | Partición por IP en todos los endpoints sensibles — protege contra exfiltración masiva de datos personales            |
| Cifrado de tokens externos             | Token OAuth de Tractive cifrado con AES-256 antes de persistir                                                        |
| No venta de datos                      | Declarado explícitamente en Política de Privacidad §7                                                                 |
| Derecho de cancelación (ahora funcional) | `DeleteAccountCommandHandler` — **corregido en este análisis** (ver §1)                                              |

---

## 4. Hallazgos — Gaps pendientes (requieren decisión de producto/legal)

Estos NO son bugs — son funcionalidad que la política de privacidad ya redacta de
forma general ("cuando sea aplicable", "según corresponda") pero que aún no tiene
una implementación técnica dedicada. Priorizados por relevancia legal.

### 🔴 Alto — datos sensibles de salud sin consentimiento diferenciado

**Problema:** el expediente médico (vacunas, diagnósticos, tratamientos) es un
**dato sensible** bajo el Art. 9 de la Ley 8968. Actualmente su único "gate" es
comercial (plan Familia) — no existe un paso de consentimiento **específico y
diferenciado** del consentimiento general de los Términos de Uso antes de que el
usuario suba el primer registro médico.

**Recomendación:** al crear el primer registro médico de una mascota, mostrar un
modal de consentimiento explícito ("Acepto que PawTrack CR trate datos de salud de
mi mascota con fines de cuidado y coordinación con clínicas") con registro de
`ConsentedAt` en la base de datos — no basta con un checkbox genérico ya aceptado
en el registro de la cuenta.

### 🟡 Medio — sin verificación de edad al registro

**Problema:** `RegisterCommand`/`RegisterCommandValidator` no capturan fecha de
nacimiento ni ningún control de edad mínima. Los Términos de Uso §4 dicen "si eres
menor de edad, debes usar la plataforma con autorización... de tu tutor legal", pero
no hay ningún mecanismo técnico que lo verifique o lo haga cumplir.

**Recomendación (mínimo viable):** agregar un campo de confirmación de mayoría de
edad (checkbox "Confirmo que soy mayor de edad o cuento con autorización de mi
tutor legal") en el registro, con el valor persistido. No requiere verificación de
identidad formal (fuera de alcance realista para el MVP), pero cierra la brecha
entre lo que dice el ToS y lo que el sistema efectivamente registra.

### 🟡 Medio — sin exportación de datos autoservicio (portabilidad)

**Problema:** la Política de Privacidad §11 promete "portabilidad de ciertos datos
cuando sea aplicable", pero el único mecanismo es escribir a
`privacidad@pawtrack.cr` para un proceso manual. No hay endpoint de "descargar mis
datos".

**Recomendación:** endpoint `GET /api/auth/me/export` que compile JSON con:
perfil, mascotas, reportes de pérdida propios, mensajes de chat propios,
expediente médico — reutilizando los repositorios ya existentes (no requiere
nuevas queries complejas, es una agregación de lo que ya se puede consultar por
partes). Prioridad media porque el canal manual ya cumple el requisito legal
mínimo, pero el autoservicio es la práctica internacional recomendada y reduce
carga operativa de soporte.

### 🟢 Bajo — sin retención/purga para sightings, chat y notificaciones

**Problema:** a diferencia de ubicaciones de collar, QR scans y vistas de clínica
(que sí purgan automáticamente), los avistamientos, mensajes de chat y
notificaciones **se conservan indefinidamente**. Esto no es ilegal per se (Ley 8968
no fija un plazo numérico), pero contradice el principio de conservación
proporcional (Art. 6/11) si esos datos ya no sirven la finalidad original (ej. un
caso cerrado hace 2 años).

**Recomendación:** definir una política de retención razonable (ej. 2 años tras el
cierre del caso para sightings/chat asociado) y un job de purga/anonimización
análogo a `QrScanRetentionJob`. No urgente — priorizar después de los ítems
🔴/🟡.

### 🟢 Bajo (organizacional, no de código) — registro ante PRODHAB

**Problema:** la Ley 8968 (Art. 22 del Reglamento) exige que las bases de datos con
fines comerciales que contienen datos personales se **registren ante PRODHAB**.
Esto es un trámite administrativo/legal, no algo que se resuelva en el código.

**Recomendación:** confirmar con el equipo legal si el registro ante PRODHAB ya se
tramitó antes de lanzamiento comercial pleno en Costa Rica. Si no, es un
pendiente operacional de la misma categoría que "GitHub Secrets CI/CD" o "dominio
pawtrack.cr" en `pendientesTotales.md` §1 — debería agregarse ahí.

### 🟢 Bajo (organizacional) — transferencia internacional ya divulgada, verificar DPA

**Confirmado en infraestructura:** `infra/parameters.prod.bicepparam` despliega en
`eastus` (Estados Unidos) — los datos de usuarios costarricenses se almacenan fuera
de Costa Rica. La Política de Privacidad §8 ya divulga esto de forma general
("Algunos proveedores tecnológicos pueden almacenar... fuera de Costa Rica").

**Recomendación:** confirmar que existe un **Data Processing Addendum (DPA)** firmado
con Microsoft/Azure (Microsoft ofrece uno estándar, el "Microsoft Products and
Services Data Protection Addendum") que documente las garantías de protección
aplicables a la transferencia — esto es lo que Ley 8968 Art. 14 busca cuando exige
"garantías equivalentes" en el país destino. Es un trámite administrativo, no de
código.

---

## 5. Checklist de cumplimiento

- [x] Política de privacidad y términos de uso vigentes, referencian Ley 8968
- [x] Consentimiento de cookies con opción de rechazo igualmente prominente
- [x] Anonimización de reportantes de avistamientos
- [x] Chat enmascarado (no expone contacto real)
- [x] Hash seguro de datos identificables (teléfonos, contraseñas)
- [x] Cifrado de tokens de terceros en reposo
- [x] Controles BOLA en todos los endpoints de datos personales/sensibles
- [x] Derecho de cancelación (eliminación de cuenta) — **corregido, ahora funcional**
- [x] Purga automática: ubicaciones GPS de collar, QR scans, vistas de clínica
- [ ] Consentimiento diferenciado para datos de salud (expediente médico) — 🔴 pendiente
- [ ] Verificación/confirmación de mayoría de edad al registro — 🟡 pendiente
- [ ] Exportación de datos autoservicio (portabilidad) — 🟡 pendiente
- [ ] Retención/purga para sightings, chat, notificaciones — 🟢 pendiente
- [ ] Confirmar registro de bases de datos ante PRODHAB — 🟢 organizacional
- [ ] Confirmar DPA con Microsoft Azure para transferencia internacional — 🟢 organizacional

---

## 6. Referencias de código auditadas

| Archivo                                                                                  | Relevancia                                                    |
| ------------------------------------------------------------------------------------------ | ---------------------------------------------------------------- |
| `backend/src/PawTrack.Application/Auth/Commands/DeleteAccount/DeleteAccountCommandHandler.cs` | Derecho de cancelación — bug corregido en este análisis         |
| `backend/src/PawTrack.Infrastructure/Collars/CollarLocationPurgeJob.cs`                    | Minimización de datos de ubicación                              |
| `backend/src/PawTrack.Infrastructure/Notifications/Jobs/QrScanRetentionJob.cs`             | Minimización de eventos de escaneo QR                            |
| `backend/src/PawTrack.Infrastructure/Clinics/ClinicProfileViewPurgeHostedService.cs`        | Minimización de vistas de perfil de clínica                     |
| `frontend/src/shared/ui/CookieConsentBanner.tsx`                                           | Consentimiento de cookies con opción de rechazo                 |
| `docs/POLITICA_DE_PRIVACIDAD.md`, `docs/TERMINOS_DE_USO.md`                               | Textos legales vigentes                                          |
| `infra/parameters.prod.bicepparam`                                                         | Confirma región de hosting (`eastus`) — transferencia internacional |
