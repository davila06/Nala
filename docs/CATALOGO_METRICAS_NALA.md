# Catálogo técnico de métricas NALA

Este catálogo define la versión técnica inicial de Sprint 5. Requiere aprobación
de producto, seguridad y la institución participante antes de publicarse como
catálogo oficial.

| Código                | Fuente                   | Fecha de negocio | Agregación                   | Privacidad                  |
| --------------------- | ------------------------ | ---------------- | ---------------------------- | --------------------------- |
| `active_lost_pets`    | `LostPetEvent`           | estado actual    | conteo                       | no público con propietario  |
| `reunited_pets`       | `LostPetEvent`           | `ResolvedAt`     | conteo por periodo           | suppression pública         |
| `municipal_captures`  | `CapturedAnimal`         | `CapturedAt`     | estado/cantón/especie        | suppression pública         |
| `adopted_animals`     | `AdoptablePet`           | `AdoptedAt`      | conteo por periodo           | suppression pública         |
| `welfare_cases`       | `AnimalWelfareCase`      | `CreatedAt`      | tipo/estado/severidad/cantón | evidencia y PII excluidas   |
| `verified_microchips` | `Pet`                    | estado actual    | conteo                       | solo institucional agregado |
| `valid_certificates`  | `VetCertificate`         | estado actual    | conteo                       | sin datos del propietario   |
| `network_coverage`    | clínicas/municipalidades | estado actual    | conteo                       | no direcciones privadas     |

## Métricas de proveedores propuestas

| Código                  | Fuente                 | Fecha de negocio | Agregación              | Privacidad                  |
| ----------------------- | ---------------------- | ---------------- | ----------------------- | --------------------------- |
| `service_providers`     | `ServiceProvider`      | `RegisteredAt`   | categoría/estado        | sin contacto ni dirección   |
| `provider_bookings`     | `ProviderBooking`      | `StartsAt`       | estado/servicio/periodo | sin cliente, mascota o nota |
| `provider_verification` | `ProviderVerification` | `SubmittedAt`    | estado/vencimiento      | sin evidencia ni revisor    |

## Reglas comunes

- Los periodos se interpretan en `America/Costa_Rica` y se persisten como fechas
  inclusivas en el contrato del reporte.
- Las consultas leen con `AsNoTracking`, proyectan y agregan en SQL.
- Los grupos públicos menores al threshold configurado se devuelven como
  `Suppressed`, nunca como cero ordinario.
- Los exports incluyen `SchemaVersion`, periodo, scope, filtros y hash SHA-256.
- No se incluyen propietarios, reportantes, evidencia, notas clínicas, teléfonos,
  correos, domicilios ni coordenadas exactas.

## Versionado

La versión inicial es `1.0`. Cambios de fórmula, fuente o semántica requieren
nueva versión, migración del catálogo y revisión de compatibilidad.
