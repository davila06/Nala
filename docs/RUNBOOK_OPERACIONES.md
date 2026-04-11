# Runbook de Operaciones — PawTrack CR

**Versión:** 1.0  
**Audiencia:** Equipo de operaciones, developers on-call  
**Última actualización:** Abril 2026

> Este runbook describe procedimientos paso a paso para responder a incidentes, realizar tareas operativas recurrentes y ejecutar cambios de alto riesgo en producción.  
> Sigue siempre el orden exacto de los pasos. En caso de duda, **detente y escala** antes de continuar.

---

## Tabla de contenidos

1. [Recursos de producción](#1-recursos-de-producción)
2. [Verificar estado general](#2-verificar-estado-general)
3. [Escalada de errores 5xx](#3-escalada-de-errores-5xx)
4. [Reiniciar el App Service sin downtime](#4-reiniciar-el-app-service-sin-downtime)
5. [Escalar el App Service](#5-escalar-el-app-service)
6. [Rotar secretos comprometidos](#6-rotar-secretos-comprometidos)
   - 6.1 [Rotar la clave JWT (sesiones comprometidas)](#61-rotar-la-clave-jwt-sesiones-comprometidas)
   - 6.2 [Rotar el connection string de SQL](#62-rotar-el-connection-string-de-sql)
   - 6.3 [Rotar otros secretos de Key Vault](#63-rotar-otros-secretos-de-key-vault)
7. [Invalidar todas las sesiones activas](#7-invalidar-todas-las-sesiones-activas)
8. [Rollback de una migración EF Core](#8-rollback-de-una-migración-ef-core)
9. [Monitorizar notificaciones push en cola](#9-monitorizar-notificaciones-push-en-cola)
10. [Diagnosticar latencia elevada](#10-diagnosticar-latencia-elevada)
11. [Base de datos — operaciones de emergencia](#11-base-de-datos--operaciones-de-emergencia)
12. [Desbloquear una cuenta de usuario manualmente](#12-desbloquear-una-cuenta-de-usuario-manualmente)
13. [Suspender o reactivar una clínica manualmente](#13-suspender-o-reactivar-una-clínica-manualmente)
14. [Procedimientos de mantenimiento programado](#14-procedimientos-de-mantenimiento-programado)
15. [Contactos y escalada](#15-contactos-y-escalada)

---

## 1. Recursos de producción

| Recurso | Nombre en Azure | URL / Referencia |
|---------|----------------|-----------------|
| App Service | `pawtrack-prod-api` | `https://api.pawtrack.cr` |
| Frontend (Static Web App) | — | `https://pawtrack.cr` |
| Azure SQL | `pawtrack-prod-sql` / DB `pawtrack` | — |
| Key Vault | `pawtrack-kv-prod` | `https://pawtrack-kv-prod.vault.azure.net/` |
| Blob Storage | `pawtrackstorprod` | Contenedores: `pet-photos`, `sighting-photos`, `found-pet-photos`, `lost-pet-photos` |
| Application Insights | `pawtrack-prod-insights` | Portal Azure → Log Analytics |
| Log Analytics Workspace | `pawtrack-prod-logs` | Retención: 30 días |
| App Service Plan | `pawtrack-prod-plan` | SKU: B3 Linux |

> **Acceso rápido:** Portal Azure → Resource Group `pawtrack-prod` → seleccionar recurso.

---

## 2. Verificar estado general

### Health check básico

```bash
curl -f https://api.pawtrack.cr/health
# Respuesta esperada: 200 OK
```

### Verificar métricas en Application Insights

1. Portal Azure → `pawtrack-prod-insights` → **Failures** (panel izquierdo).
2. Revisar tasa de errores por operación en los últimos 30 minutos.
3. Si hay alertas disparadas, revisar la sección **Alerts** del workspace.

### Consultar errores recientes con KQL

En Log Analytics (`pawtrack-prod-logs`) ejecuta:

```kql
requests
| where timestamp > ago(30m)
| where resultCode >= 500
| summarize count() by name, resultCode
| order by count_ desc
```

Para ver los últimos errores con detalle:

```kql
exceptions
| where timestamp > ago(1h)
| project timestamp, type, outerMessage, operation_Name
| order by timestamp desc
| take 50
```

---

## 3. Escalada de errores 5xx

### Triage inicial (primeros 5 minutos)

1. **Verificar el health check:** `curl -f https://api.pawtrack.cr/health`  
   - Si responde `200` → el proceso está vivo, el error puede ser parcial.  
   - Si no responde → ir al paso 4 (reinicio).

2. **Consultar Application Insights** (sección 2) para identificar qué operación está fallando.

3. **Revisar los logs del App Service:**

```bash
az webapp log tail \
  --name pawtrack-prod-api \
  --resource-group pawtrack-prod
```

4. **Identificar la causa:**

| Síntoma | Causa probable | Acción |
|---------|---------------|--------|
| `SqlException: connection refused` | SQL auto-paused o no disponible | Ver sección 11.1 |
| `Azure.RequestFailedException` en Storage | Connection string de Storage inválida o servicio degradado | Verificar Key Vault y Azure Storage status |
| `SecurityTokenExpiredException` masivo | Reloj del servidor desincronizado | Reiniciar App Service (sección 4) |
| Errores 503 desde el load balancer | App Service sin instancias disponibles | Escalar (sección 5) o reiniciar (sección 4) |
| `Could not load file or assembly` | Deploy incompleto | Hacer redeploy desde la pipeline |

5. Si la causa no es identificable en 10 minutos, **escala** a la persona on-call senior (sección 15).

---

## 4. Reiniciar el App Service sin downtime

El App Service usa un solo slot en MVP. El reinicio tiene ~30 segundos de downtime.

### Reinicio suave (warm restart)

```bash
az webapp restart \
  --name pawtrack-prod-api \
  --resource-group pawtrack-prod
```

Espera 60 segundos y verifica el health check:

```bash
curl -f https://api.pawtrack.cr/health
```

### Reinicio forzado (si el suave no responde)

1. Portal Azure → `pawtrack-prod-api` → **Overview** → botón **Stop**.
2. Esperar 10 segundos.
3. Botón **Start**.
4. Verificar health check cada 10 segundos hasta obtener `200 OK`.

### Verificar que la aplicación arrancó correctamente

```kql
traces
| where timestamp > ago(10m)
| where message contains "Application started"
| order by timestamp desc
| take 5
```

---

## 5. Escalar el App Service

### Scale up (más CPU y RAM — mismo número de instancias)

El plan actual en producción es **B3** (SKU máximo del tier Basic). Para subir a un tier superior:

```bash
az appservice plan update \
  --name pawtrack-prod-plan \
  --resource-group pawtrack-prod \
  --sku S1
```

> Pasar de Basic a Standard (S-tier) permite deployment slots y escalado automático.

### Scale out manual (más instancias)

```bash
az appservice plan update \
  --name pawtrack-prod-plan \
  --resource-group pawtrack-prod \
  --number-of-workers 2
```

> Aumentar instancias en un plan Basic con SignalR activo puede causar problemas de sticky sessions. Considerar migración a plan Standard con affinity habilitada o un Azure SignalR Service dedicado antes de hacer scale out.

---

## 6. Rotar secretos comprometidos

### 6.1 Rotar la clave JWT (sesiones comprometidas)

**Efecto:** Todos los access tokens y refresh tokens existentes quedan inmediatamente inválidos. Todos los usuarios serán desconectados y deberán volver a iniciar sesión.

**Pasos:**

1. Genera una nueva clave segura (mínimo 64 caracteres, CSPRNG):

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

2. Actualiza el secreto en Key Vault:

```bash
az keyvault secret set \
  --vault-name pawtrack-kv-prod \
  --name jwt-signing-key \
  --value "<nueva-clave-generada>"
```

3. Reinicia el App Service para que tome el nuevo valor (sección 4).

4. Verifica que el login funciona correctamente:

```bash
curl -X POST https://api.pawtrack.cr/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"..."}'
```

5. Purga la tabla `RefreshTokens` si los tokens anteriores pueden haberse filtrado:

```sql
-- Conectar a Azure SQL (pawtrack DB)
DELETE FROM RefreshTokens WHERE IsRevoked = 0;
```

6. Registra el incidente (quién, cuándo, por qué se rotó la clave).

### 6.2 Rotar el connection string de SQL

1. Genera una nueva contraseña para el usuario de la base de datos en Azure SQL:

```bash
az sql server update \
  --name pawtrack-prod-sql \
  --resource-group pawtrack-prod \
  --admin-password "<nueva-contraseña>"
```

2. Construye el nuevo connection string:

```
Server=tcp:pawtrack-prod-sql.database.windows.net,1433;Initial Catalog=pawtrack;User Id=pawtrackadmin;Password=<nueva-contraseña>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

3. Actualiza Key Vault:

```bash
az keyvault secret set \
  --vault-name pawtrack-kv-prod \
  --name sql-connection-string \
  --value "<nuevo-connection-string>"
```

4. Reinicia el App Service (sección 4).

5. Verifica el health check. Si el health check incluye una verificación de DB (`/health/ready`), confírmalo también.

### 6.3 Rotar otros secretos de Key Vault

Para cualquier otro secreto (`storage-connection-string`, `vision-key`, `whatsapp-access-token`, etc.):

1. Genera o consigue el nuevo valor desde el proveedor correspondiente.
2. Actualiza en Key Vault:

```bash
az keyvault secret set \
  --vault-name pawtrack-kv-prod \
  --name <nombre-del-secreto> \
  --value "<nuevo-valor>"
```

3. Reinicia el App Service (sección 4).
4. El App Service toma el nuevo valor en el siguiente ciclo de caché de Key Vault (máximo 24 h si no se reinicia).

---

## 7. Invalidar todas las sesiones activas

Útil ante sospecha de compromiso de tokens sin necesidad de rotar la clave JWT (por ejemplo, si solo hubo acceso a la tabla `RefreshTokens`).

```sql
-- Conectar a Azure SQL (pawtrack DB)
-- Revocar todos los refresh tokens activos
UPDATE RefreshTokens
SET IsRevoked = 1, RevokedAt = GETUTCDATE()
WHERE IsRevoked = 0 AND ExpiresAt > GETUTCDATE();
```

Los usuarios serán desconectados en su próximo intento de renovar sesión (máximo 15 minutos, que es el TTL del access token).

---

## 8. Rollback de una migración EF Core

> **Advertencia:** Revertir una migración en producción puede resultar en pérdida de datos si la migración añadió columnas con datos. Evalúa siempre si es preferible un forward-fix.

### Identificar la migración objetivo

Consulta el historial de migraciones aplicadas:

```bash
dotnet ef migrations list \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API
```

La última migración aplicada tiene el prefijo `[applied]`.

### Revertir a la migración anterior

```bash
dotnet ef database update <NombreDeLaMigraciónAnterior> \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API
```

Reemplaza `<NombreDeLaMigraciónAnterior>` con el nombre exacto (sin timestamp).

### Eliminar la migración del código (solo si aún no se aplicó en ningún entorno compartido)

```bash
dotnet ef migrations remove \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API
```

> **Regla irrevocable:** Si una migración ya se aplicó en staging o producción, **nunca la borres del código**. Crea una nueva migración que corrija el estado.

---

## 9. Monitorizar notificaciones push en cola

Las suscripciones push están almacenadas en la tabla `PushSubscriptions`. Las notificaciones fallidas quedan registradas en Application Insights como excepciones con el prefijo `PushNotification`.

### Ver notificaciones push con errores recientes

```kql
exceptions
| where timestamp > ago(24h)
| where type contains "Push" or operation_Name contains "Push"
| summarize count() by type, outerMessage
| order by count_ desc
```

### Limpiar suscripciones push inválidas

Con el tiempo, algunos endpoints push quedan obsoletos (usuario desinstaló la PWA, etc.). Limpia los más antiguos:

```sql
-- Eliminar suscripciones que no han tenido actividad en 90 días
DELETE FROM PushSubscriptions
WHERE CreatedAt < DATEADD(DAY, -90, GETUTCDATE());
```

---

## 10. Diagnosticar latencia elevada

### Identificar operaciones lentas

```kql
requests
| where timestamp > ago(1h)
| where duration > 2000  -- más de 2 segundos
| summarize avg(duration), count() by name
| order by avg_duration desc
| take 20
```

### Identificar dependencias lentas (SQL, Storage, etc.)

```kql
dependencies
| where timestamp > ago(1h)
| where duration > 1000
| summarize avg(duration), count() by type, name
| order by avg_duration desc
| take 20
```

### Causas comunes y soluciones

| Causa | Síntoma en App Insights | Solución |
|-------|------------------------|--------|
| SQL Database auto-paused (serverless) | Primera petición lenta ~5–10 s | Configurar `autoPauseDelay: -1` para deshabilitar el auto-pause en prod, o aceptar el cold start |
| N+1 en una query | Muchas dependencias `SQL` con duración baja pero alta frecuencia | Revisar el handler con `AsNoTracking()` y `Include()` apropiados |
| Embedding generation bloqueando la respuesta | Petición `/sightings` lenta | El `EmbeddingRefreshHostedService` debe correr en background, no en el request path |
| Blobs en contenedor equivocado | `StorageException` en dependencias | Verificar nombres de contenedores en configuración |

---

## 11. Base de datos — operaciones de emergencia

### 11.1 SQL Database auto-paused

La base de datos en producción usa SKU Serverless. Tras un período de inactividad puede entrar en auto-pause. La primera petición "despierta" la base con una latencia de 5–10 segundos.

Si esto es un problema recurrente, deshabilita el auto-pause:

```bash
az sql db update \
  --server pawtrack-prod-sql \
  --resource-group pawtrack-prod \
  --name pawtrack \
  --auto-pause-delay -1
```

### 11.2 Verificar conexiones activas

```sql
SELECT
  COUNT(*) AS ActiveConnections,
  DB_NAME() AS DatabaseName
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID();
```

### 11.3 Terminar una sesión colgada

```sql
-- Obtener el session_id en cuestión primero
SELECT session_id, status, command, blocking_session_id, wait_type, wait_time
FROM sys.dm_exec_requests
WHERE blocking_session_id <> 0;

-- Terminar la sesión bloqueante
KILL <session_id>;
```

### 11.4 Backup manual (point-in-time restore disponible en Azure SQL)

Azure SQL retiene backups automáticos. Para restaurar:

1. Portal Azure → `pawtrack-prod-sql` → `pawtrack` (database) → **Restore**.
2. Selecciona el punto en el tiempo deseado.
3. Introduce un nuevo nombre de base de datos de destino (no restaures sobre la producción directamente).
4. Valida los datos en la base restaurada.
5. Si es correcto, redirige las conexiones.

---

## 12. Desbloquear una cuenta de usuario manualmente

Cuando un usuario reporta que no puede iniciar sesión por bloqueo (5 intentos fallidos → 15 minutos), y el tiempo de espera ya pasó pero el bloqueo persiste:

```sql
UPDATE Users
SET FailedLoginAttempts = 0,
    LockoutEnd = NULL
WHERE Email = '<email-del-usuario>';
```

Verifica antes de ejecutar:

```sql
SELECT Email, FailedLoginAttempts, LockoutEnd, IsEmailVerified
FROM Users
WHERE Email = '<email-del-usuario>';
```

---

## 13. Suspender o reactivar una clínica manualmente

Si el panel de administración no está disponible temporalmente, puedes gestionar el estado de una clínica directamente en la base de datos.

### Verificar estado actual

```sql
SELECT Id, Name, LicenseNumber, Status, ContactEmail
FROM ClinicProfiles
WHERE Name LIKE '%<nombre-parcial>%'
   OR LicenseNumber = '<licencia>';
```

Los valores posibles de `Status` son: `Pending`, `Active`, `Suspended`.

### Activar una clínica

```sql
UPDATE ClinicProfiles
SET Status = 'Active'
WHERE Id = '<guid-de-la-clinica>';
```

### Suspender una clínica

```sql
UPDATE ClinicProfiles
SET Status = 'Suspended'
WHERE Id = '<guid-de-la-clinica>';
```

> Preferir siempre usar el panel de administración (`/admin`). Este procedimiento es solo para emergencias cuando el panel no está disponible.

---

## 14. Procedimientos de mantenimiento programado

### Antes del mantenimiento

1. Publica un aviso en la plataforma si el downtime afecta a usuarios (si está implementado).
2. Toma nota del estado actual del health check.
3. Verifica que Application Insights está capturando métricas.

### Deploy de una nueva versión

El deploy se realiza desde la pipeline de CI/CD (GitHub Actions o Azure DevOps). Para un deploy manual de emergencia:

```bash
# Desde la raíz del repositorio, publicar el backend
dotnet publish backend/src/PawTrack.API \
  -c Release \
  -o ./publish

# Desplegar via Azure CLI
az webapp deploy \
  --name pawtrack-prod-api \
  --resource-group pawtrack-prod \
  --src-path ./publish \
  --type zip
```

### Actualización de secretos en Key Vault (rotación periódica)

Se recomienda rotar los siguientes secretos cada **90 días**:

| Secreto | Procedimiento |
|---------|--------------|
| `jwt-signing-key` | Sección 6.1 (sin purgar refresh tokens si no hay compromiso) |
| `sql-connection-string` | Sección 6.2 |
| `whatsapp-access-token` | Regenerar en Meta Business Suite → actualizar en Key Vault → reiniciar App Service |
| `vision-key` | Regenerar en Azure Portal → Azure AI → Key regeneration → actualizar en Key Vault |

### Limpieza de datos antiguos (mensual)

```sql
-- Tokens de refresh expirados
DELETE FROM RefreshTokens
WHERE ExpiresAt < DATEADD(DAY, -7, GETUTCDATE());

-- Notificaciones leídas con más de 90 días
DELETE FROM NotificationItems
WHERE IsRead = 1 AND CreatedAt < DATEADD(DAY, -90, GETUTCDATE());

-- Suscripciones push inactivas (ver sección 9)
DELETE FROM PushSubscriptions
WHERE CreatedAt < DATEADD(DAY, -90, GETUTCDATE());
```

---

## 15. Contactos y escalada

| Rol | Responsabilidad | Cuándo contactar |
|-----|----------------|-----------------|
| **Lead técnico** | Arquitectura, decisiones de rollback, rotación de secretos críticos | Siempre que haya riesgo de pérdida de datos o compromiso de seguridad |
| **DevOps / Infraestructura** | App Service, Key Vault, escalado, deploys | Incidentes de disponibilidad que no se resuelven con reinicio |
| **DBA** | Operaciones de base de datos de emergencia, backups | Secciones 8 y 11 |
| **Soporte de usuario** | Desbloqueo de cuentas, suspensión de clínicas | Secciones 12 y 13 |

### Niveles de escalada

| Severidad | Definición | Tiempo de respuesta objetivo |
|-----------|-----------|----------------------------|
| **P1 — Crítico** | Plataforma completamente caída o compromiso de seguridad confirmado | 15 minutos |
| **P2 — Alto** | Funcionalidad principal degradada (no se pueden reportar mascotas perdidas, login falla) | 1 hora |
| **P3 — Medio** | Funcionalidad secundaria afectada (notificaciones push, estadísticas) | 4 horas hábiles |
| **P4 — Bajo** | Comportamiento inesperado sin impacto en usuarios activos | Próximo sprint |

### Recursos de estado de servicios externos

| Servicio | Status page |
|---------|------------|
| Azure (global) | `https://azure.status.microsoft` |
| Meta / WhatsApp Cloud API | `https://metastatus.com` |
| Azure Maps | Panel de Azure Portal → Azure Maps → Resource health |
