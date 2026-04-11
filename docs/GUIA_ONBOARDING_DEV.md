# Guía de Onboarding para Desarrolladores — PawTrack CR

**Versión:** 1.0  
**Audiencia:** Desarrolladores que se incorporan al proyecto  
**Última actualización:** Junio 2026

---

## Tabla de contenidos

1. [Requisitos previos](#1-requisitos-previos)
2. [Clonar el repositorio](#2-clonar-el-repositorio)
3. [Configurar el entorno local](#3-configurar-el-entorno-local)
4. [Levantar los servicios de dependencias](#4-levantar-los-servicios-de-dependencias)
5. [Configurar variables de la API](#5-configurar-variables-de-la-api)
6. [Aplicar migraciones de base de datos](#6-aplicar-migraciones-de-base-de-datos)
7. [Arrancar el proyecto con start-dev.ps1](#7-arrancar-el-proyecto-con-start-devps1)
8. [Verificar que todo funciona](#8-verificar-que-todo-funciona)
9. [Comandos de desarrollo habituales](#9-comandos-de-desarrollo-habituales)
10. [Estructura del proyecto en 5 minutos](#10-estructura-del-proyecto-en-5-minutos)
11. [Convenciones clave que debes conocer](#11-convenciones-clave-que-debes-conocer)
12. [Tests](#12-tests)
13. [Cómo agregar una feature](#13-cómo-agregar-una-feature)
14. [Flujo de trabajo con Git](#14-flujo-de-trabajo-con-git)
15. [Acceso a producción](#15-acceso-a-producción)
16. [Preguntas frecuentes](#16-preguntas-frecuentes)

---

## 1. Requisitos previos

Instala las siguientes herramientas antes de continuar:

| Herramienta | Versión mínima | Verificar |
|-------------|---------------|-----------|
| .NET SDK | 9.0 | `dotnet --version` |
| Node.js | 20 LTS | `node --version` |
| Docker Desktop | 4.x | `docker --version` |
| Git | 2.40+ | `git --version` |
| Azurite CLI | 3.x | `azurite --version` |

### Instalar Azurite globalmente

```bash
npm install -g azurite
```

Azurite es el emulador local de Azure Blob/Queue/Table Storage. El script de arranque ya gestiona su ciclo de vida.

---

## 2. Clonar el repositorio

```bash
git clone <url-del-repo>
cd Nala
```

El repositorio usa la raíz `C:\Nala\` como convención de path en los scripts. Si clonas en otra ruta deberás ajustar las rutas absolutas en `start-dev.ps1`.

---

## 3. Configurar el entorno local

### 3.1 Contraseña de SQL Server

El contenedor de SQL Server lee la contraseña desde `secrets/sa_password.txt`. Este archivo está en `.gitignore` y **nunca debe commitearse**.

Crea el archivo manualmente:

```powershell
"TuContraseñaSegura123!" | Set-Content -Path "secrets\sa_password.txt" -Encoding UTF8 -NoNewline
```

Requisitos de la contraseña de SQL Server:  
- Mínimo 8 caracteres  
- Al menos una mayúscula, una minúscula, un número y un carácter especial

### 3.2 Variables de entorno del frontend

Crea el archivo `frontend/.env.local`:

```env
VITE_API_URL=http://localhost:5199
```

Este archivo también está en `.gitignore`.

---

## 4. Levantar los servicios de dependencias

Los servicios de base de datos y storage se ejecutan en Docker:

```bash
docker compose up -d
```

Esto levanta:

| Contenedor | Puerto | Propósito |
|-----------|--------|-----------|
| `pawtrack-sqlserver` | 1433 | SQL Server 2025 (Developer Edition) |
| `pawtrack-azurite` | 10000, 10001, 10002 | Emulador de Azure Blob/Queue/Table |

Verifica que ambos contenedores estén corriendo:

```bash
docker ps
```

> **Primer arranque:** SQL Server puede tardar 20–30 segundos en estar listo. El healthcheck de Docker lo indica.

---

## 5. Configurar variables de la API

El archivo `appsettings.Development.json` ya incluye la configuración para el entorno local y está versionado. No necesitas modificarlo para el arranque básico.

**Configuración local ya incluida:**

| Variable | Valor |
|----------|-------|
| `ConnectionStrings:DefaultConnection` | SQL Server local (LocalDB o Docker en `localhost:1433`) |
| `Jwt:Key` | Clave de desarrollo (no usar en producción) |
| `Azure:Storage:ConnectionString` | Azurite local (`localhost:10000`) |
| `Cors:AllowedOrigins` | `http://localhost:5173` y `http://localhost:4173` |

Si usas el contenedor Docker (no LocalDB), actualiza la connection string en `appsettings.Development.json`:

```json
"DefaultConnection": "Server=localhost,1433;Database=PawTrackDev;User Id=sa;Password=TuContraseñaSegura123!;TrustServerCertificate=True;"
```

Reemplaza `TuContraseñaSegura123!` con la contraseña que pusiste en `secrets/sa_password.txt`.

> **Secretos de producción:** Nunca pongas secretos reales en `appsettings.json` ni en `appsettings.Development.json`. En producción todos los secretos vienen de Azure Key Vault. Ver sección 15.

---

## 6. Aplicar migraciones de base de datos

Desde la raíz del repositorio, ejecuta las migraciones EF Core:

```bash
dotnet ef database update `
  --project backend/src/PawTrack.Infrastructure `
  --startup-project backend/src/PawTrack.API
```

Si `dotnet ef` no está instalado:

```bash
dotnet tool install --global dotnet-ef
```

Las migraciones crean todas las tablas necesarias en `PawTrackDev`. El proceso es idempotente — puedes ejecutarlo múltiples veces sin problema.

---

## 7. Arrancar el proyecto con start-dev.ps1

El script `start-dev.ps1` en la raíz del repositorio gestiona el ciclo completo de arranque:

```powershell
.\start-dev.ps1
```

Lo que hace el script:

1. Detiene cualquier proceso `PawTrack.API` que esté corriendo (para evitar conflictos de binarios).
2. Verifica si Azurite ya está corriendo en el puerto `10000`. Si no, lo inicia.
3. Lanza el backend (`dotnet run --project src/PawTrack.API --launch-profile http`).
4. Espera hasta 90 segundos a que el backend responda en `http://localhost:5199/health`.
5. Lanza el frontend (`npm run dev -- --host 127.0.0.1 --port 5173`).

### Opciones del script

| Flag | Descripción |
|------|-------------|
| `-NoBackend` | Inicia solo el frontend (útil cuando ya tienes el backend corriendo) |
| `-NoFrontend` | Inicia solo el backend |
| `-RestartAzurite` | Fuerza el reinicio de Azurite aunque ya esté corriendo |
| `-Mobile` | Expone Vite en la red local para probar la PWA desde el celular (misma WiFi) |

Ejemplo para prueba móvil:

```powershell
.\start-dev.ps1 -Mobile
```

El script detecta tu IP LAN automáticamente e imprime la URL para abrir desde el celular.

---

## 8. Verificar que todo funciona

| Endpoint | Resultado esperado |
|----------|--------------------|
| `http://localhost:5199/health` | `200 OK` |
| `http://localhost:5199/openapi/v1.json` | JSON de la especificación OpenAPI |
| `http://localhost:5173` | Pantalla de inicio de la PWA |

Verifica el health del backend:

```powershell
Invoke-WebRequest -Uri "http://localhost:5199/health" -UseBasicParsing
```

---

## 9. Comandos de desarrollo habituales

### Backend

```bash
# Compilar desde la raíz
dotnet build

# Correr tests unitarios
dotnet test backend/tests/PawTrack.UnitTests

# Correr tests de integración
dotnet test backend/tests/PawTrack.IntegrationTests

# Crear una nueva migración
dotnet ef migrations add NombreDeLaMigracion `
  --project backend/src/PawTrack.Infrastructure `
  --startup-project backend/src/PawTrack.API

# Aplicar migraciones
dotnet ef database update `
  --project backend/src/PawTrack.Infrastructure `
  --startup-project backend/src/PawTrack.API
```

### Frontend

```bash
# Desde /frontend
npm install          # instalar dependencias
npm run dev          # servidor de desarrollo
npm test             # tests en modo watch (Vitest)
npm run test:ui      # UI gráfica de Vitest
npm run test:coverage  # cobertura de tests
npm run build        # build de producción
npm run lint         # ESLint (0 warnings permitidos)
```

### Docker

```bash
# Levantar todos los servicios
docker compose up -d

# Ver logs de SQL Server
docker logs pawtrack-sqlserver -f

# Detener servicios
docker compose down

# Detener y borrar volúmenes (¡borra los datos locales!)
docker compose down -v
```

---

## 10. Estructura del proyecto en 5 minutos

```
/
├── PawTrack.sln               # Solución .NET (todos los proyectos)
├── docker-compose.yml         # SQL Server + Azurite para dev local
├── start-dev.ps1              # Script de arranque local (Windows/PowerShell)
├── secrets/
│   └── sa_password.txt        # (gitignored) contraseña SA para SQL local
├── backend/
│   └── src/
│       ├── PawTrack.API/          # Controllers, Middleware, Hubs, Program.cs
│       ├── PawTrack.Application/  # Commands, Queries, Handlers, Validators
│       ├── PawTrack.Domain/       # Entidades, Value Objects, Domain Events
│       └── PawTrack.Infrastructure/ # EF Core, Repos, Azure Services
│   └── tests/
│       ├── PawTrack.UnitTests/
│       └── PawTrack.IntegrationTests/
├── frontend/
│   └── src/
│       ├── app/               # Router, Providers, Layouts
│       ├── features/          # Un directorio por módulo de negocio
│       └── shared/            # Componentes y utilidades compartidas
│   └── tests/
├── infra/
│   └── main.bicep             # Infraestructura Azure
└── docs/                      # Manuales y documentación
```

### Capas del backend

| Capa | Proyecto | Regla |
|------|----------|-------|
| **API** | `PawTrack.API` | Solo recibe HTTP/WS, no contiene lógica, despacha a MediatR |
| **Application** | `PawTrack.Application` | Lógica de negocio pura, sin dependencias de infraestructura |
| **Domain** | `PawTrack.Domain` | Entidades y reglas de dominio, cero dependencias externas |
| **Infrastructure** | `PawTrack.Infrastructure` | Implementaciones de Azure, EF Core, Email |

---

## 11. Convenciones clave que debes conocer

### CQRS con MediatR

- **Commands** → mutan estado, devuelven datos mínimos (nunca la entidad completa).
- **Queries** → solo leen, devuelven DTOs.
- Validación en FluentValidation pipeline behaviors — nunca dentro de un handler.

Estructura de un comando:

```
Auth/Commands/RegisterUser/
├── RegisterUserCommand.cs           # Record del comando + Response
├── RegisterUserCommandHandler.cs    # Handler
└── RegisterUserCommandValidator.cs  # FluentValidation
```

### IDs

Siempre `Guid.CreateVersion7()` en el dominio. Se exponen como `string` en respuestas API.

### Result pattern

Usa `Result<T>` y `Result` para representar éxito/error sin lanzar excepciones de negocio entre módulos.

### Comunicación entre módulos

**Solo por MediatR notifications o domain events.** Nunca llamadas directas entre servicios de distintos módulos.

### Fechas y horas

Siempre `DateTimeOffset` (nunca `DateTime`). El backend opera en UTC.

### Fotos / archivos binarios

Siempre **Azure Blob Storage** (Azurite en local). Jamás binarios en la base de datos.

### Migraciones

**Nunca editar una migración ya aplicada** en ningún entorno compartido. Si cometiste el error, crea una migración nueva que corrija.

---

## 12. Tests

### Unitarios (`PawTrack.UnitTests`)

Cubren handlers, validadores y lógica de dominio. No dependen de infraestructura real.

```bash
dotnet test backend/tests/PawTrack.UnitTests
```

### Integración (`PawTrack.IntegrationTests`)

Usan una base de datos en memoria o un contenedor efímero. Verifican el stack completo desde HTTP hasta base de datos.

```bash
dotnet test backend/tests/PawTrack.IntegrationTests
```

### Frontend (Vitest + Testing Library)

Los tests están en `frontend/tests/`. Usan `msw` para interceptar llamadas a la API.

```bash
cd frontend
npm test
```

> Los tests de frontend usan `jsdom` como entorno y `@testing-library/react` para renderizar componentes. El setup global está en `tests/setup.ts`.

---

## 13. Cómo agregar una feature

### Backend — pasos mínimos

1. **Entidad de dominio** en `PawTrack.Domain/{Módulo}/`.
2. **Migración EF Core** para la nueva entidad.
3. **Command o Query** en `PawTrack.Application/{Módulo}/Commands/` o `Queries/`.
4. **Handler** y **Validator** en el mismo directorio.
5. **Controller** en `PawTrack.API/Controllers/` que despacha a MediatR.
6. **(Opcional)** Servicio de infraestructura en `PawTrack.Infrastructure/{Módulo}/`.

### Frontend — pasos mínimos

1. Agrega la llamada a la API en `features/{módulo}/api/{nombre}Api.ts`.
2. Crea el hook de React Query en `features/{módulo}/hooks/use{Nombre}.ts`.
3. Implementa la página en `features/{módulo}/pages/{Nombre}Page.tsx`.
4. Registra la ruta en `frontend/src/app/routes.tsx`.

---

## 14. Flujo de trabajo con Git

1. Crea una rama desde `main`: `git checkout -b feature/nombre-corto`
2. Implementa y commitea con mensajes descriptivos en español o inglés (consistencia dentro del PR).
3. Abre un Pull Request hacia `main` con descripción de los cambios.
4. Al menos un revisor debe aprobar antes de hacer merge.
5. No hacer `git push --force` en `main` ni en ramas compartidas.
6. No commitear secretos, contraseñas, llaves o archivos `.env.local`.

---

## 15. Acceso a producción

En producción **todos los secretos vienen de Azure Key Vault** (`pawtrack-kv`). Las referencias en `appsettings.json` tienen la forma:

```json
"Key": "@Microsoft.KeyVault(VaultName=pawtrack-kv;SecretName=jwt-signing-key)"
```

Para obtener acceso a Key Vault en producción debes tener:

- Acceso al tenant de Azure del proyecto.
- Rol `Key Vault Secrets User` asignado sobre el vault `pawtrack-kv`.

Solicita acceso al lead técnico del proyecto.

### Endpoints de producción

| Servicio | URL |
|---------|-----|
| API | `https://api.pawtrack.cr` |
| Frontend | `https://pawtrack.cr` |
| Health check | `https://api.pawtrack.cr/health` |

---

## 16. Preguntas frecuentes

**El backend no arranca y aparece un error de SQL.**  
Verifica que el contenedor `pawtrack-sqlserver` esté corriendo (`docker ps`) y que la connection string en `appsettings.Development.json` tenga la contraseña correcta de `secrets/sa_password.txt`.

**Hay un error de migración pendiente al arrancar.**  
Ejecuta `dotnet ef database update` (ver sección 6). El backend lanza una excepción si la base de datos está desactualizada.

**El frontend muestra "Network Error" al hacer login.**  
Confirma que el backend está corriendo en `http://localhost:5199` y que `frontend/.env.local` tiene `VITE_API_URL=http://localhost:5199`.

**Azurite no inicia — el puerto 10000 está ocupado.**  
Ejecuta `.\start-dev.ps1 -RestartAzurite` para forzar el reinicio.

**¿Cómo accedo a la UI de Swagger/OpenAPI en local?**  
La especificación OpenAPI está en `http://localhost:5199/openapi/v1.json`. Puedes importarla en Postman, Bruno o abrirla en el Swagger UI instalado.

**¿Qué IDE se recomienda?**  
VS Code con la extensión C# Dev Kit para el backend. El workspace está configurado para funcionar desde VS Code. JetBrains Rider también funciona sin configuración adicional.

**¿Los tests de integración necesitan Docker?**  
Depende de la configuración del proyecto de tests. Consulta el `README` de `PawTrack.IntegrationTests` para los requisitos específicos.

**¿Cómo agrego una nueva variable de entorno para producción?**  
1. Agrega el secreto en Azure Key Vault (`pawtrack-kv`).
2. Agrega la referencia Key Vault en `appsettings.json`.
3. Para desarrollo local, agrega el valor directo en `appsettings.Development.json` (sin el secreto real — usa un valor de prueba).
4. Documenta la nueva variable en este archivo bajo la sección 5.
