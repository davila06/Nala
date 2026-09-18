# Operacion Enterprise de SuperAdmin

> Estado: ACTIVO  
> Fecha: 2026-09-18

## Proposito

`SuperAdmin` es un rol excepcional para gobierno de plataforma. Hereda las capacidades operativas de `Admin`, pero es el unico rol autorizado para asignar o revocar otros SuperAdmins.

No debe utilizarse como cuenta cotidiana. Las tareas de moderacion, aprobacion, catalogos, reportes y soporte deben ejecutarse con `Admin` o `Support` segun corresponda.

## Controles implementados

- Rol persistido independiente `UserRole.SuperAdmin`.
- JWT jerarquico: claims `SuperAdmin` y `Admin`, con `platform_role=SuperAdmin` para conservar la identidad primaria.
- Politica exclusiva `SuperAdmin` que exige autenticacion, rol primario y claim MFA.
- MFA obligatorio para iniciar sesion como SuperAdmin.
- Verificacion TOTP fresca en cada elevacion o revocacion.
- Cuenta objetivo con correo verificado y MFA configurado antes de elevar.
- Prohibicion de autoelevacion y autorrevocacion.
- Prohibicion de revocar al ultimo SuperAdmin.
- Motivo obligatorio de 10 a 500 caracteres.
- Auditoria persistente `SuperAdminAssigned` / `SuperAdminRevoked`; el codigo MFA nunca se almacena.
- Rate limiting de operaciones privilegiadas.
- Bootstrap inicial idempotente protegido por `IDistributedJobLock`.

## Bootstrap del primer SuperAdmin

1. Crear una cuenta normal y verificar su correo.
1. Activar MFA desde el perfil y guardar los codigos de recuperacion fuera de linea.
1. Configurar temporalmente el Container App:

```text
Security__SuperAdmin__BootstrapEnabled=true
Security__SuperAdmin__BootstrapEmail=correo-verificado@pawtrack.cr
```

1. Reiniciar una sola revision y verificar el log critico `SuperAdmin bootstrap completed`.
1. Desactivar inmediatamente:

```text
Security__SuperAdmin__BootstrapEnabled=false
Security__SuperAdmin__BootstrapEmail=
```

La operacion no se ejecuta si falta la cuenta, el correo no esta verificado o MFA no esta configurado. El lock distribuido evita ejecucion duplicada durante scale-out.

## Operacion posterior

Ruta de consola: `/super-admin`.

API:

```http
PUT /api/super-admin/users/{userId}/role
DELETE /api/super-admin/users/{userId}/role
```

Ambas reciben:

```json
{
  "reason": "Referencia de ticket o aprobacion de control dual",
  "mfaCode": "123456"
}
```

La eliminacion degrada la cuenta a `Admin`; no elimina la cuenta ni sus registros.

## Procedimiento recomendado

- Mantener al menos dos SuperAdmins nominales y nunca compartir cuentas.
- Usar passkey o MFA con dispositivo corporativo administrado.
- Revisar mensualmente `AuditLog` para acciones privilegiadas.
- Revocar inmediatamente acceso ante salida del equipo o incidente.
- Mantener una cuenta break-glass separada, sin uso diario, con credenciales custodiadas fuera de linea.
- Asociar cada elevacion a ticket, acta o aprobacion verificable.
