# Diccionario de Datos PawTrack CR

**Estado:** activo como referencia conceptual  
**Corte:** 2026-09-09

Este documento describe agregados y reglas de datos. Para columnas exactas,
consultar entidades de `backend/src/PawTrack.Domain` y el modelo EF.

## Agregados principales

| Agregado                   | Datos principales                             | Sensibilidad |
| -------------------------- | --------------------------------------------- | ------------ |
| `User`                     | identidad, rol, MFA, preferencias             | alta         |
| `Pet`                      | identidad, especie, foto, microchip           | alta         |
| `LostPetEvent`             | estado, ubicacion, contacto relay, recompensa | alta         |
| `Sighting`                 | GPS, foto y nota sanitizada                   | alta         |
| `ChatThread/Message`       | participantes y mensajes enmascarados         | alta         |
| `MedicalRecord`            | vacunas, peso, medicacion y citas             | muy alta     |
| `ClinicMedicalAccessGrant` | permisos y expiracion de clinica              | muy alta     |
| `Subscription`             | tier, estado, importe y vigencia              | media/alta   |
| `Collar/Location`          | serial, device key, GPS y bateria             | muy alta     |
| `Store/Order`              | catalogo, pedido y estados                    | media        |
| `ServiceProvider/Booking`  | perfil, servicio, agenda y reserva            | media        |
| `CapturedAnimal`           | captura municipal y coincidencia              | alta         |
| `AnimalWelfareCase`        | severidad, evidencia y derivacion             | muy alta     |
| `AuditLogEntry`            | actor, recurso, accion y resultado            | alta         |

## Reglas de modelado

- IDs de dominio: `Guid` v7; respuestas API exponen strings.
- Fotos y documentos: Blob Storage, nunca binarios en SQL.
- PII de reportantes de avistamientos: no persistir en claro.
- Salud: requiere consentimiento diferenciado.
- Ownership y tenant siempre se resuelven desde claims/repositorio.
- Estados y enums no deben renombrarse o reordenarse sin migracion compatible.

## Relaciones de seguridad

`User -> Pet -> LostPetEvent`, `Pet -> MedicalRecord`, `Clinic -> Grant -> Pet`,
`User -> ServiceProvider/Store/Ally`, `Municipality -> CapturedAnimal` y
`Collar -> Pet` deben validarse en el mismo limite de autorizacion. Un GUID
conocido no es permiso.

## Cambios

Cada nueva entidad debe documentar propietario del modulo, sensibilidad,
retencion, indice, auditoria y pruebas BOLA. Actualizar
[MATRIZ_RETENCION_DATOS.md](MATRIZ_RETENCION_DATOS.md) si cambia la ventana.
