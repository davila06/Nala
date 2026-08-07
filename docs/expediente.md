# PawTrack CR — Expediente Médico Digital

> Documento de referencia para el módulo de historial médico de mascotas.  
> Estado: MVP funcional con gaps identificados · Agosto 2026

---

## 1. Estado actual — lo que está implementado

### 1.1 Backend (completo ✅)

| Capa                      | Archivo                                                             | Estado |
| ------------------------- | ------------------------------------------------------------------- | ------ |
| Dominio — entidades       | `MedicalRecord.cs`, `VetReminder.cs`, `ClinicMedicalAccessGrant.cs` | ✅     |
| Comandos                  | `MedicalCommands.cs` (Add, Get, Complete, Export)                   | ✅     |
| Acceso clínica            | `ClinicAccessGrantCommands.cs` (opciones A, B, C)                   | ✅     |
| Exportación PDF           | `ExportMedicalHistoryCommand.cs` + `IMedicalPdfExporter` (QuestPDF) | ✅     |
| Almacenamiento documentos | Blob Storage container `medical-docs` (PDF/JPEG/PNG, max 5 MB)      | ✅     |
| Controlador REST          | `MedicalController`, `PetClinicAccessController`                    | ✅     |
| Gating de plan            | Requiere Plan Familia para leer/escribir                            | ✅     |

### 1.2 Frontend (completo ✅)

| Componente               | Dónde                                                                                | Estado |
| ------------------------ | ------------------------------------------------------------------------------------ | ------ |
| `MedicalHistoryTab`      | PetDetailPage → tab Salud                                                            | ✅     |
| `ClinicExpedienteTab`    | ClinicDashboard → tab Expediente                                                     | ✅     |
| `PetClinicAccessManager` | PetDetailPage → gestión de accesos                                                   | ✅     |
| `ClinicAccessPanel`      | Portal clínica → solicitar acceso                                                    | ✅     |
| Hooks React Query        | `useMedicalHistory`, `useAddMedicalRecord`, `useVetReminders`, `useExportMedicalPdf` | ✅     |

---

## 2. Modelo de datos

### 2.1 MedicalRecord

```
MedicalRecord
├── Id (Guid v7)
├── PetId → Pets.Id
├── CreatedByUserId → Users.Id
├── ClinicId (nullable) → ClinicProfiles.Id   -- null = agregado por el dueño
├── Type (enum int)
│   ├── 0 = Vaccine
│   ├── 1 = Deworming
│   ├── 2 = Checkup
│   ├── 3 = Surgery
│   ├── 4 = Other
│   ├── 5 = Medication
│   └── 6 = Allergy
├── Date (DateOnly)
├── Description (string)
├── VetName (string, nullable)
├── ClinicName (string, nullable)
├── NextDueDate (DateOnly, nullable) — crea recordatorio automático
├── DocumentUrl (string, nullable) — Blob Storage URL
└── CreatedAt (DateTimeOffset)
```

### 2.2 VetReminder

```
VetReminder
├── Id (Guid v7)
├── PetId → Pets.Id
├── OwnerId → Users.Id
├── Type (MedicalRecordType)
├── DueDate (DateOnly)
├── Title (string)
├── Notes (string, nullable)
├── IsCompleted (bool)
├── CompletedAt (DateTimeOffset, nullable)
├── ReminderSentAt (DateTimeOffset, nullable)  -- rastrear cuándo se envió el aviso
└── CreatedAt (DateTimeOffset)
```

### 2.3 ClinicMedicalAccessGrant

```
ClinicMedicalAccessGrant
├── Id (Guid v7)
├── PetId → Pets.Id
├── ClinicId → ClinicProfiles.Id
├── PetOwnerId → Users.Id
├── InitiatedBy ("Owner" | "Clinic")
├── CodeHash (SHA-256 del código de 8 chars — nunca exponer el hash)
├── CodeExpiresAt (DateTimeOffset — 24 horas después de generar)
├── AcceptedAt (DateTimeOffset, nullable)
├── IsActive (bool)
├── RevokedAt (DateTimeOffset, nullable)
└── CreatedAt (DateTimeOffset)
```

Charset del código: `ABCDEFGHJKMNPQRSTUVWXYZ23456789` — se excluyen I, L, O, 0, 1 para evitar confusión visual.

---

## 3. API Endpoints implementados

### Expediente del dueño

| Método | Ruta                                             | Plan requerido | Descripción                            |
| ------ | ------------------------------------------------ | :------------: | -------------------------------------- |
| `GET`  | `/api/pets/{id}/medical`                         |    Familia     | Historial completo de la mascota       |
| `POST` | `/api/pets/{id}/medical`                         |    Familia     | Agregar registro (multipart, max 5 MB) |
| `GET`  | `/api/pets/{id}/medical/reminders`               |    Familia     | Recordatorios pendientes               |
| `PUT`  | `/api/pets/{id}/medical/reminders/{id}/complete` |    Familia     | Marcar recordatorio como completado    |
| `GET`  | `/api/pets/{id}/medical/export`                  |    Familia     | Exportar historial en PDF              |

### Acceso clínica (gestión del dueño)

| Método   | Ruta                                      | Descripción                                          |
| -------- | ----------------------------------------- | ---------------------------------------------------- |
| `GET`    | `/api/pets/{id}/clinic-access`            | Lista todos los accesos activos y pendientes         |
| `POST`   | `/api/pets/{id}/clinic-access/code`       | Dueño genera código de 8 chars para dar a la clínica |
| `POST`   | `/api/pets/{id}/clinic-access/accept`     | Dueño ingresa el código que generó la clínica        |
| `DELETE` | `/api/pets/{id}/clinic-access/{clinicId}` | Dueño revoca acceso de una clínica                   |

### Expediente desde la clínica

| Método | Ruta                                    | Plan clínica | Descripción                                        |
| ------ | --------------------------------------- | :----------: | -------------------------------------------------- |
| `GET`  | `/api/clinics/patients/{petId}/medical` |  Cualquiera  | Ver historial del paciente (requiere grant activo) |
| `POST` | `/api/clinics/patients/{petId}/medical` |  Cualquiera  | Agregar registro al expediente del paciente        |

---

## 4. Sistema de acceso clínica — Opciones A, B y C

Los tres flujos están implementados y funcionan:

| Opción | Quién genera                   | Quién acepta                    | Cuándo usar                                                    |
| ------ | ------------------------------ | ------------------------------- | -------------------------------------------------------------- |
| **A**  | Dueño genera código            | Clínica lo ingresa en su portal | El dueño planea la visita y quiere que la clínica tenga acceso |
| **B**  | Clínica genera código          | Dueño lo ingresa en su app      | La mascota llega a la clínica sin previa coordinación          |
| **C**  | QR scanning al escanear collar | Sistema auto-propone acceso     | Flujo integrado con escaneo de collar en clínica               |

Todas las opciones crean un `ClinicMedicalAccessGrant` permanente una vez activado. El dueño puede revocar en cualquier momento desde la gestión de accesos.

---

## 5. Plan de acceso (gating)

| Funcionalidad                | Explorador | Plus | **Familia** |       Clínica       |
| ---------------------------- | :--------: | :--: | :---------: | :-----------------: |
| Ver historial médico         |     ❌     |  ❌  |     ✅      | ✅ (requiere grant) |
| Agregar registros            |     ❌     |  ❌  |     ✅      | ✅ (requiere grant) |
| Gestionar recordatorios      |     ❌     |  ❌  |     ✅      |         ❌          |
| Exportar PDF                 |     ❌     |  ❌  |     ✅      |         ❌          |
| Gestionar accesos de clínica |     ❌     |  ❌  |     ✅      |         N/A         |
| Ver expediente como clínica  |    N/A     | N/A  |     N/A     |     ✅ Partner      |

---

## 6. Features faltantes (gaps identificados)

### 6.1 Críticos — bloquean operaciones básicas

| Feature                                                                                                                                                    | Impacto | Esfuerzo         | Prioridad |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ------- | ---------------- | --------- |
| **DELETE registro médico** — no hay forma de eliminar un registro equivocado                                                                               | Alto    | Bajo (~0.5 días) | 🔴 Alta   |
| **PUT/PATCH registro médico** — no hay forma de corregir typos o errores                                                                                   | Alto    | Bajo (~0.5 días) | 🔴 Alta   |
| **DELETE recordatorio** — no hay forma de eliminar un recordatorio incorrecto                                                                              | Medio   | Bajo (~0.5 días) | 🔴 Alta   |
| **POST recordatorio independiente** — solo se crean recordatorios como subproducto de un registro con `NextDueDate`; no hay forma de crear uno manualmente | Medio   | Bajo (~1 día)    | 🟡 Media  |

**Backend:** Faltan estos endpoints en `MedicalController`:

```
DELETE /api/pets/{petId}/medical/{recordId}
PUT    /api/pets/{petId}/medical/{recordId}
DELETE /api/pets/{petId}/medical/reminders/{reminderId}
POST   /api/pets/{petId}/medical/reminders     ← nuevo, standalone
```

**Domain:** Falta lógica de autorización en `MedicalRecord.Update()` para verificar que solo el creador (o familia, o clínica con grant) puede editar.

### 6.2 Importantes — mejoran la calidad del expediente

| Feature                                                  | Descripción                                                                                                                                                                | Esfuerzo                    |
| -------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------- |
| **Peso/medidas por visita**                              | Agregar campos opcionales `WeightKg (decimal?)` y `TemperatureC (decimal?)` a `MedicalRecord`                                                                              | 1 día (migración + UI)      |
| **Medicación con dosis y duración**                      | Tipo `Medication` debería tener: `DosageDescription`, `Frequency`, `DurationDays`, `EndDate`. Actualmente solo hay `Description` libre                                     | 2 días                      |
| **Notificación al dueño cuando clínica agrega registro** | `VetReminder.ReminderSentAt` existe pero no hay lógica que envíe push/email al dueño cuando una clínica añade un registro. Alta importancia para confianza y transparencia | 1 día                       |
| **Múltiples documentos por registro**                    | Solo 1 documento por `MedicalRecord`. Para cirugías o visitas complejas con múltiples estudios es insuficiente                                                             | 2 días (nuevo child entity) |
| **Nota del dueño sobre registro de clínica**             | El dueño no puede anotar observaciones sobre un registro que creó la clínica                                                                                               | 1 día                       |
| **Log de acceso de clínica**                             | No hay rastro de qué registros vio o modificó la clínica. Importante para auditoría y confianza del dueño                                                                  | 1.5 días                    |

### 6.3 Mejoras de experiencia (UX)

| Feature                      | Descripción                                                                                                                | Esfuerzo          |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------------------- | ----------------- |
| **Vista calendario**         | Los recordatorios deberían mostrarse en un calendario interactivo con las visitas pasadas y próximas                       | 2 días frontend   |
| **Filtrado por tipo**        | La `MedicalHistoryTab` no tiene filtros por tipo de registro. Con historial largo es difícil encontrar vacunas específicas | 0.5 días frontend |
| **Búsqueda en historial**    | No hay búsqueda de texto libre en `Description`, `VetName`, `ClinicName`                                                   | 0.5 días          |
| **Vista de línea de tiempo** | Historial visual cronológico con agrupación por año                                                                        | 1 día frontend    |
| **Dashboard multi-mascota**  | Una vista consolidada de todos los recordatorios próximos de todas las mascotas del hogar (relevante para Plan Familia)    | 1.5 días frontend |
| **Gráfico de peso**          | Si se agrega el campo `WeightKg`, mostrar evolución de peso en gráfica de líneas                                           | 1 día frontend    |

### 6.4 Features de visión (v2.0+)

| Feature                           | Descripción                                                                                                                                                                            | Valor                           |
| --------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------- |
| **Pasaporte veterinario digital** | Documento estructurado con vacunas obligatorias + vet signature + código QR de verificación. Útil para viajes, adopciones, competencias                                                | Alto                            |
| **Integración SENASA**            | Los certificados veterinarios oficiales para viajes en CR requieren formularios SENASA. Un flujo guiado que genere el documento correcto en el formato que SENASA acepta               | Muy alto para mascotas viajeras |
| **QR de acceso de emergencia**    | Un QR en el perfil público de la mascota que permite a un veterinario de emergencia ver un resumen del expediente (solo vacunas + alergias + medicación actual) sin necesidad de grant | Alto                            |
| **Protocolo de vacunación**       | Calendario recomendado por especie/raza con notificaciones proactivas (ej: "En 30 días toca la primera dosis de la triple" para cachorros)                                             | Alto                            |
| **Lab results estructurados**     | Tipo dedicado para resultados de laboratorio con campos: analítica, valores de referencia, interpretación                                                                              | Medio                           |
| **Acceso temporal de clínica**    | En lugar de acceso permanente, permitir grants de 30/60/90 días que expiran automáticamente                                                                                            | Medio (seguridad)               |
| **WhatsApp reminder**             | Enviar recordatorio de vacuna o desparasitación via WhatsApp cuando se acerca la fecha (el bot ya existe, solo falta conectar con `VetReminder`)                                       | Alto                            |
| **Firma digital veterinaria**     | Las clínicas Partner podrían firmar digitalmente los registros que crean, añadiendo credibilidad al expediente                                                                         | Medio                           |

---

## 7. Bugs y problemas conocidos

| Problema                                                                                                                                                                                       | Impacto                                | Estado             |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------- | ------------------ |
| `VetReminder.ReminderSentAt` existe en el dominio pero ningún job envía la notificación real cuando se acerca la fecha                                                                         | Medio — los recordatorios no recuerdan | ⛔ No implementado |
| La gating de plan en clínica es inconsistente: actualmente parece que la clínica puede agregar registros sin importar el plan del dueño — ¿es intencional? Debería documentarse explícitamente | Bajo — confusión de reglas de negocio  | ⚠️ Revisar         |
| El tipo `Medication` no tiene campos de dosificación — se captura todo en `Description` como texto libre, lo que hace imposible filtrar mascotas con medicación activa                         | Medio                                  | ⚠️ Deuda técnica   |

---

## 8. Propuesta de mejoras priorizadas

### Sprint A — Gaps críticos (~3 días)

1. **DELETE** `/api/pets/{petId}/medical/{recordId}` — soft delete, solo el creador puede borrar
2. **PUT** `/api/pets/{petId}/medical/{recordId}` — editar campos básicos (description, date, vetName, nextDueDate)
3. **DELETE** `/api/pets/{petId}/medical/reminders/{reminderId}`
4. **POST** `/api/pets/{petId}/medical/reminders` — crear recordatorio independiente
5. **Notificación push al dueño** cuando una clínica agrega un registro

### Sprint B — Calidad del expediente (~5 días)

6. Agregar `WeightKg (decimal?)` a `MedicalRecord` (migración + UI + gráfico)
7. Extender tipo `Medication`: `DosageDescription`, `Frequency`, `DurationDays`, `EndDate`
8. Vista calendario de recordatorios en frontend
9. Filtrado por tipo en `MedicalHistoryTab`
10. Dashboard multi-mascota de recordatorios (Plan Familia)

### Sprint C — Visión (2–4 semanas)

11. QR de acceso de emergencia (resumen público: vacunas + alergias)
12. Protocolo de vacunación automático por especie
13. WhatsApp reminder conectado a `VetReminder`
14. Acceso temporal de clínica (grants con expiración configurable)
15. Pasaporte veterinario digital con formato SENASA

---

## 9. Diseño de los endpoints faltantes (referencia implementación)

### DELETE registro

```csharp
// DeleteMedicalRecordCommand.cs
public sealed record DeleteMedicalRecordCommand(Guid RecordId, Guid RequestingUserId)
    : IRequest<Result<Unit>>;
// Handler: verificar que RequestingUserId == record.CreatedByUserId
//          o que sea miembro de familia del dueño de la mascota
//          o que sea la clínica que creó el registro (si ClinicId matches)
// Acción: soft delete con DeletedAt timestamp (no borrar de DB — auditoría)
//         + eliminar DocumentUrl de Blob Storage si existe
```

### PUT registro

```csharp
public sealed record UpdateMedicalRecordCommand(
    Guid RecordId,
    Guid RequestingUserId,
    MedicalRecordType Type,
    DateOnly Date,
    string Description,
    string? VetName,
    string? ClinicName,
    DateOnly? NextDueDate
) : IRequest<Result<MedicalRecordDto>>;
// No permitir cambiar el documento adjunto via PUT — usar endpoint separado si se necesita
```

### POST recordatorio independiente

```csharp
public sealed record CreateVetReminderCommand(
    Guid PetId,
    Guid RequestingUserId,
    MedicalRecordType Type,
    DateOnly DueDate,
    string Title,
    string? Notes
) : IRequest<Result<VetReminderDto>>;
```

### Notificación al dueño cuando clínica agrega registro

En `AddMedicalRecordCommandHandler`, después de `medicalRepository.AddAsync`:

```csharp
// Si el registro fue creado por una clínica, notificar al dueño
if (request.ClinicId.HasValue)
{
    await sender.Publish(new ClinicAddedMedicalRecordNotification(
        request.PetId, pet.OwnerId, request.ClinicId.Value, record.Type), ct);
}
```

---

## 10. Plan gating — decisiones pendientes

**Pregunta abierta:** ¿Debe una clínica poder agregar registros al expediente de un paciente cuyo dueño tiene solo Plan Explorador o Plus?

**Opciones:**

| Opción                                                                                          | Pro                                                                                                                             | Contra                                                                |
| ----------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| **A (actual implícito):** Clínica puede escribir sin importar el plan del dueño                 | Adopción más fácil en clínicas; el dueño se "sorprende" al ver que tiene expediente                                             | El dueño no puede ver el registro porque le falta el plan — confusión |
| **B:** La clínica solo puede escribir si el dueño tiene Plan Familia                            | Coherente; asegura que el dueño siempre puede ver lo que la clínica escribe                                                     | Reduce adopción en clínicas si sus clientes no tienen Familia         |
| **C (recomendado):** Clínica siempre puede escribir; dueño necesita Familia para leer/gestionar | Mejor UX: el expediente se construye sin fricción; el plan Familia "desbloquea" ver lo que ya existe — CTA natural para upgrade | Requiere mensajería clara en UI                                       |

**Recomendación:** Opción C. Muestra al dueño en el perfil de la mascota: "Tu veterinaria ha agregado N registros médicos. Actualiza al plan Familia para verlos." — es el upsell más natural del sistema.

---

_PawTrack CR · Módulo Expediente Médico Digital · Agosto 2026_
