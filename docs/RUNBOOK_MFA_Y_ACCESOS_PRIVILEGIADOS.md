# Runbook de MFA y Accesos Privilegiados

**Estado:** activo  
**Audiencia:** Admin, Support y operadores de plataforma  
**Corte:** 2026-09-09

## Roles privilegiados

`Admin` tiene acceso global. `Support` solo opera bienestar e incidentes de
proveedores. Las cuentas privilegiadas deben usar MFA fuera de Development
segun la politica configurada.

## Alta

1. Crear o seleccionar una cuenta existente.
2. Asignar el rol minimo necesario; `Support` se asigna por un Admin.
3. Activar MFA TOTP desde el flujo de seguridad.
4. Guardar los codigos de recuperacion en un gestor aprobado, nunca en tickets.
5. Probar login, refresh y acceso permitido.
6. Registrar responsable, fecha y motivo del acceso.

## Baja o rotacion

- Retirar el rol antes de desactivar la cuenta.
- Revocar sesiones y tokens activos.
- Rotar secretos o API keys que el operador haya podido consultar.
- Verificar que el acceso devuelve `403` donde corresponde.

## Perdida del segundo factor

El operador debe usar un codigo de recuperacion de un solo uso. Si no lo tiene,
escalar a otro Admin y verificar identidad por el proceso interno. Support no
puede autoasignarse Admin ni recuperar MFA de otra persona.

## Revision periodica

Cada trimestre revisar usuarios, roles, MFA activo, ultima actividad y necesidad
operativa. Conservar solo las evidencias de auditoria necesarias.
