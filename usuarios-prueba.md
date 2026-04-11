# PawTrack CR — Usuarios de Prueba

> **SOLO USO LOCAL / DEV** — Nunca usar estas credenciales en staging o producción.

---

## Credenciales por rol

| Rol | Nombre | Email | Contraseña |
|-----|--------|-------|------------|
| `Owner` | Ana Pérez (Owner) | `owner@pawtrack.test` | `Test123!` |
| `Ally` | Carlos Mora (Ally) | `ally@pawtrack.test` | `Ally123!` |
| `Admin` | Denis Admin | `admin@pawtrack.test` | `Admin123!` |
| `Clinic` | Clínica VetCare CR | `clinic@pawtrack.test` | `Clinic123!` |

---

## IDs (GUID) asignados

| Rol | ID |
|-----|-----|
| `Owner` | `D73FC5EA-6F8F-4ADF-9756-07480962EAF3` |
| `Ally` | `E2984533-3A78-4C84-8286-7C91E69AE1B3` |
| `Admin` | `DAD661E5-7B58-4A5A-ABD4-280ACA9B7C72` |
| `Clinic` | `2B9B9F17-39DD-42A7-B138-A00632ABE55A` |

---

## Hashes BCrypt (cost factor 12)

| Email | Hash almacenado |
|-------|-----------------|
| `owner@pawtrack.test` | `$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES` |
| `ally@pawtrack.test` | `$2a$12$1BbkfBTccZR17WAmeXQoK.it6ig5SSyAk1hoPD3kl.YQG/u8lul7i` |
| `admin@pawtrack.test` | `$2a$12$sapENPrv3mwah7zOBZvjfOIZK8PoV0h2YIt11KW4u6l1F3Ah2Mis6` |
| `clinic@pawtrack.test` | `$2a$12$Zc2bzcB9S7nfYbiB8Gq6QePRRlhy6tVgs1jeRm2SejAhwxWn3IY3W` |

---

## Cómo aplicar el seed

### Opción A — LocalDB via sqlcmd (entorno dev por defecto)

```powershell
# Desde C:\Nala (raíz del proyecto)
sqllocaldb start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev -i backend\scripts\seed-test-users.sql
```

### Opción B — Docker SQL Server (si el contenedor está activo)

```powershell
# El contenedor usa la contraseña en secrets/sa_password.txt
sqlcmd -S "localhost,1433" -U sa -P "PawTrack_Dev!2026" -d PawTrackDev -C -i backend\scripts\seed-test-users.sql
```

### Opción C — desde SQL Server Management Studio / Azure Data Studio

1. Abrir el archivo `backend/scripts/seed-test-users.sql`
2. Conectar a `(localdb)\MSSQLLocalDB` → base de datos `PawTrackDev`
3. Ejecutar (F5)

> **Nota:** El script es idempotente — un segundo run borra y re-inserta sin error.

---

## Notas técnicas

- Todos los usuarios tienen `IsEmailVerified = 1` (pueden iniciar sesión de inmediato).
- `EmailVerificationToken`, `PasswordResetToken`, `LockoutEnd` = `NULL`.
- El seed es **idempotente**: borra y re-inserta los 4 registros si se ejecuta varias veces.
- El rol se almacena como string en la columna `Role` (`HasConversion<string>()`):  
  `"Owner"` | `"Ally"` | `"Admin"` | `"Clinic"`
- No es posible crear usuarios con rol distinto a `Owner` a través de la API register  
  (Register siempre llama `User.Create()` que fuerza `Role = UserRole.Owner`).  
  Por eso se usa INSERT directo para los roles Ally, Admin y Clinic.

---

## Política de contraseñas de prueba

Las contraseñas cumplen la política mínima de PawTrack:

- ≥ 8 caracteres ✔  
- Al menos 1 mayúscula ✔  
- Al menos 1 número ✔  
- Al menos 1 símbolo (`!`) ✔
