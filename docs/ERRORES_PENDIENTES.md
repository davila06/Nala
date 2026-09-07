# ERRORES PENDIENTES — PawTrack CR

> **Fecha del análisis:** 2026-09-07
> **Alcance:** los 4 proyectos `.csproj` de producción + 2 de pruebas del backend .NET 9, el proyecto orfanado `HashGen`, y el frontend React 19 / TypeScript 5.7 (app, tests, service worker, e2e, configs).
> **Estado del documento:** vivo. Marcar cada casilla del checklist (§10) conforme se resuelva.

---

## 1. Resumen ejecutivo

| Gate                                | Comando                                                 | Estado actual                                                         | Bloquea despliegue                    |
| ----------------------------------- | ------------------------------------------------------- | --------------------------------------------------------------------- | ------------------------------------- |
| Backend — compilación               | `dotnet build PawTrack.sln`                             | ✅ **0 errores** / ⚠️ **44 advertencias** (30 únicas)                 | No (pero sin `TreatWarningsAsErrors`) |
| Backend — paquetes vulnerables      | `dotnet list package --vulnerable --include-transitive` | ✅ 0 vulnerables                                                      | No                                    |
| Frontend — typecheck app            | `tsc --noEmit -p tsconfig.json`                         | ✅ 0 errores                                                          | No                                    |
| Frontend — typecheck service worker | `tsc --noEmit -p tsconfig.worker.json`                  | ❌ **2 errores**                                                      | **SÍ** (tras esta auditoría)          |
| Frontend — typecheck configs/e2e    | `tsc --noEmit -p tsconfig.node.json`                    | ❌ **1 error**                                                        | **SÍ** (tras esta auditoría)          |
| Frontend — ESLint                   | `npm run lint`                                          | ❌ **272 errores + 24 advertencias**                                  | **SÍ** (CI ejecuta `npm run lint`)    |
| Frontend — dependencias             | `npm audit`                                             | ❌ **22 vulnerabilidades** (1 crítica, 14 altas, 6 moderadas, 1 baja) | No                                    |
| Tests backend                       | `dotnet test`                                           | ⚠️ 1 test suite con 3 casos **silenciosamente no ejecutados**         | No                                    |

### ⚠️ Aviso importante sobre el estado del build

Antes de esta auditoría el pipeline **parecía verde pero no lo estaba**:

- `npm run lint` **fallaba con exit code 1 en cada ejecución de CI** porque no existía ningún archivo `eslint.config.js` (ESLint 9 lo exige). El job `Frontend CI/CD → Lint` de [.github/workflows/frontend.yml](.github/workflows/frontend.yml) estaba roto y, por diseño de GitHub Actions, eso ya impedía llegar al job `deploy`. Ningún lint se ejecutó nunca sobre este código.
- El service worker (`src/sw.ts`) estaba **excluido** del único `tsconfig` que el script `build` verificaba, ocultando 2 errores de tipos reales.
- `vite.config.ts`, `playwright.config.ts` y todo `e2e/` **nunca se verificaron con `tsc`** (no estaban en ningún `include`), ocultando 1 error.

Al eliminar esas supresiones (§3), los errores quedaron expuestos. **`npm run build` y `npm run lint` fallan ahora de forma intencional** hasta que se cierren los ítems E-FE-001..003 y el bloque L-FE.

---

## 2. Metodología / cómo reproducir

Ejecutar desde la raíz del repositorio (`C:\Nala`) con PowerShell 7 (`pwsh`):

```powershell
# ── Backend ───────────────────────────────────────────────────────────────────
dotnet build-server shutdown
Get-Process PawTrack.API,MSBuild,VBCSCompiler -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build PawTrack.sln -m:1 -v:n --no-incremental > build-backend.log 2>&1
Select-String -Path build-backend.log -Pattern ': (error|warning) '

dotnet list PawTrack.sln package --vulnerable --include-transitive
dotnet test PawTrack.sln

# ── Frontend ──────────────────────────────────────────────────────────────────
cd frontend
npm run typecheck          # los 3 proyectos TS
npm run lint               # ESLint con type-checking
npx eslint . -f json -o ..\eslint.json   # reporte máquina-legible
npm audit
npm run test -- --run
```

> **Nota de entorno (Windows):** compilar con MSBuild en paralelo (`-m` por defecto) mientras hay una instancia de `PawTrack.API` corriendo produce falsos `MSB3026` / `MSB3501` / `SourceLink` por bloqueo de archivos en `obj\`. Siempre matar el host .NET y usar `-m:1` antes de auditar. **Estos NO son errores de código.**

---

## 3. Supresiones de errores eliminadas en esta auditoría

| #    | Supresión                                                                                              | Ubicación                                   | Qué ocultaba                                                                                                            | Acción tomada                                                                                                                                                 |
| ---- | ------------------------------------------------------------------------------------------------------ | ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| S-01 | Ausencia total de `eslint.config.js`                                                                   | `frontend/`                                 | **100 % del linting**. 296 hallazgos, incluidos 3 bugs de React reales                                                  | Creado [frontend/eslint.config.js](frontend/eslint.config.js) con `typescript-eslint` _recommendedTypeChecked_ + `react-hooks` + `jsx-a11y` + `react-refresh` |
| S-02 | `"exclude": ["src/sw.ts"]` sin proyecto sustituto en el build                                          | `frontend/tsconfig.json`                    | 2 errores `TS2769`/`TS2353` en el service worker                                                                        | `tsconfig.worker.json` reescrito y añadido al script `typecheck`                                                                                              |
| S-03 | `vite.config.ts`, `playwright.config.ts` y `e2e/**` fuera de todo `include` de TS                      | `frontend/`                                 | 1 error `TS2769` en `vite.config.ts`                                                                                    | Creado [frontend/tsconfig.node.json](frontend/tsconfig.node.json), añadido al script `typecheck`                                                              |
| S-04 | `"build": "tsc -b && vite build"` — `tsc -b` sobre un tsconfig sin `references` sólo verificaba la app | `frontend/package.json`                     | Los proyectos worker y node                                                                                             | Nuevo script `typecheck` que corre los 3 proyectos; `build` = `npm run typecheck && vite build`                                                               |
| S-05 | `"ignoreDeprecations": "5.0"`                                                                          | `frontend/tsconfig.json`                    | Opciones deprecadas del compilador                                                                                      | Eliminado (no era necesario)                                                                                                                                  |
| S-06 | `// @ts-expect-error` sobre `import { useRegisterSW } from "virtual:pwa-register/react"`               | `frontend/src/shared/ui/UpdateBanner.tsx:2` | Tipado del módulo virtual de la PWA                                                                                     | Sustituido por `/// <reference types="vite-plugin-pwa/react" />` en [frontend/src/vite-env.d.ts](frontend/src/vite-env.d.ts)                                  |
| S-07 | 20 comentarios `// eslint-disable-next-line` / `// eslint-disable-line` en 13 archivos                 | `frontend/src/**`                           | 12 avisos `react-hooks/exhaustive-deps`, 3 `@typescript-eslint/no-unsafe-member-access`, y 4 directivas **ya inútiles** | **Todos eliminados.** Los hallazgos resultantes están catalogados en §6                                                                                       |
| S-08 | `eslint . --ext ts,tsx` (flag obsoleto en ESLint 9, ignorado silenciosamente)                          | `frontend/package.json`                     | Cobertura de archivos del lint                                                                                          | Reemplazado por resolución de flat config                                                                                                                     |

### Supresiones evaluadas y **conservadas** (con justificación)

| Supresión                          | Ubicación                                                                       | Por qué se conserva                                                                                                                                              |
| ---------------------------------- | ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `#pragma warning disable 612, 618` | `backend/src/PawTrack.Infrastructure/**/Migrations/*.Designer.cs` (63 archivos) | Código **autogenerado por `dotnet ef`**. Editarlo se pierde en la siguiente migración. No oculta código escrito a mano.                                          |
| `#pragma warning disable CA1814`   | `backend/.../Migrations/20260811203640_AddHealthProtocols.cs:6`                 | Autogenerado (`InsertData` con array multidimensional).                                                                                                          |
| `"skipLibCheck": true`             | `frontend/tsconfig.json`                                                        | Práctica estándar; desactivarlo introduce ruido de `node_modules` (`three`, `recharts`, `leaflet`) ajeno al código propio. Ver **C-005** si se quiere endurecer. |

> **Verificado y sin hallazgos:** no existe ningún `[Fact(Skip=...)]` / `[Theory(Skip=...)]` en el backend, ni `it.skip` / `describe.skip` / `test.fixme` en el frontend, ni `continue-on-error` sobre pasos de test en los workflows de CI.

---

## 4. Backend .NET — advertencias de compilación (0 errores)

Los 4 proyectos (`PawTrack.Domain`, `PawTrack.Application`, `PawTrack.Infrastructure`, `PawTrack.API`) y los 2 de test compilan sin errores. Se emiten **44 advertencias (30 únicas)**. Ninguna está suprimida, pero tampoco ninguna rompe el build porque **no hay `TreatWarningsAsErrors` ni analizadores habilitados** (ver **C-001**).

### E-BE-001 · `CS8600` ×4 — asignación de `float[]?` a variable no anulable

**Severidad:** Alta — riesgo de `NullReferenceException` en producción.

| Archivo                                                                                                                                                                     | Línea    |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- |
| [backend/src/PawTrack.Application/Sightings/VisualMatch/MatchSightingPhotoQuery.cs](backend/src/PawTrack.Application/Sightings/VisualMatch/MatchSightingPhotoQuery.cs#L144) | 144, 155 |
| [backend/src/PawTrack.Application/Sightings/VisualMatch/MatchSightingByIdQuery.cs](backend/src/PawTrack.Application/Sightings/VisualMatch/MatchSightingByIdQuery.cs#L129)   | 129, 139 |

**Causa raíz:** `RegenerateEmbeddingAsync(...)` devuelve una tupla cuyo miembro `Vector` es `float[]?` (puede ser `null` cuando Azure Vision falla o devuelve un embedding corrupto). Se asigna directamente a `petVector`, declarada como `float[]` no anulable:

```csharp
var regenerated = await RegenerateEmbeddingAsync(profile, photoUrlHash, cancellationToken);
if (regenerated.Persisted) hasNewEmbeddings = true;
petVector = regenerated.Vector;      // ← CS8600: float[]? → float[]
if (petVector is null) continue;     // guard existe, pero el compilador ya perdió la información
```

**Impacto real:** hoy el guard `if (petVector is null) continue;` protege el flujo, por lo que **no hay bug activo**. Sin embargo, el estado de nulabilidad queda "envenenado" para el resto del método y `VectorMath.CosineSimilarity(probeVector, petVector)` deja de estar verificado por el compilador. Cualquier refactor futuro que mueva o borre ese guard introducirá un NRE silencioso en el matching visual de avistamientos.

**Solución (enterprise):** declarar la variable como anulable y dejar que el flow analysis del compilador haga el narrowing.

```csharp
// Declaración (buscar la declaración de petVector más arriba en el bucle)
- float[] petVector;
+ float[]? petVector;

// ... y en ambos sitios de asignación no cambia nada; el guard existente
//     `if (petVector is null) continue;` ahora SÍ estrecha el tipo para el compilador,
//     y la llamada posterior a CosineSimilarity queda verificada estáticamente.
```

**Criterio de aceptación:** `dotnet build` sin `CS8600` en esos 4 puntos y sin nuevos `CS8604` aguas abajo.

---

### E-BE-002 · `CS8604` — posible `null` pasado a `ChatMessage.Create(..., string body)`

**Severidad:** Alta — impacta el módulo de chat entre finder y dueño.

**Ubicación:** [backend/src/PawTrack.Application/Chat/Commands/SendChatMessage/SendChatMessageCommand.cs](backend/src/PawTrack.Application/Chat/Commands/SendChatMessage/SendChatMessageCommand.cs#L109) línea 109, col 82.

```csharp
var safeBody = piiScrubber.Scrub(command.Body);            // IPiiScrubber.Scrub devuelve string?
var message = ChatMessage.Create(command.ThreadId, command.SenderUserId, safeBody);  // ← CS8604
```

**Causa raíz:** `IPiiScrubber.Scrub` está declarado con retorno `string?` (o recibe `string?` y propaga), mientras que `ChatMessage.Create` exige `string body` no anulable. Si el scrubber elimina el 100 % del contenido (mensaje que era **sólo** un teléfono o un correo), puede devolver `null`/vacío y se persistiría un mensaje inválido.

**Solución (elegir una, no ambas):**

_Opción A — preferida, respeta el patrón `Result<T>` de la casa:_

```csharp
var safeBody = piiScrubber.Scrub(command.Body);
if (string.IsNullOrWhiteSpace(safeBody))
    return Result.Failure<Guid>("El mensaje no puede quedar vacío tras remover datos de contacto.");

var message = ChatMessage.Create(command.ThreadId, command.SenderUserId, safeBody);
```

_Opción B — endurecer el contrato:_ cambiar la firma de `IPiiScrubber.Scrub` a `string Scrub(string input)` (no anulable en entrada y salida) y devolver `string.Empty` en lugar de `null`. Requiere revisar todos los implementadores y mocks (`_pii.Scrub(Arg.Any<string?>())` en los tests).

**Criterio de aceptación:** `CS8604` eliminado + test unitario nuevo que envíe un cuerpo compuesto sólo por un número de teléfono y verifique que el handler devuelve `Result.Failure`.

---

### E-BE-003 · `CS8602` ×3 en controladores — desreferencia de `Result<T>.Value` sin verificar

**Severidad:** Media-Alta — potencial `500 Internal Server Error` en lugar de una respuesta de dominio.

| Archivo                                                                                                                                        | Línea/Col | Expresión                                                  |
| ---------------------------------------------------------------------------------------------------------------------------------------------- | --------- | ---------------------------------------------------------- |
| [backend/src/PawTrack.API/Controllers/SubscriptionPlansController.cs](backend/src/PawTrack.API/Controllers/SubscriptionPlansController.cs#L45) | 45,60     | `result.Value.Id`                                          |
| [backend/src/PawTrack.API/Controllers/CollarTagAdminController.cs](backend/src/PawTrack.API/Controllers/CollarTagAdminController.cs#L54)       | 54,49     | `result.Value.Serial`                                      |
| [backend/src/PawTrack.API/Controllers/PublicMapController.cs](backend/src/PawTrack.API/Controllers/PublicMapController.cs#L111)                | 111,13    | `var v = result.Value;` seguido de `v.HasEnoughData`, etc. |

**Causa raíz:** `Result<T>.Value` está tipado como `T?`. Los controladores comprueban `result.IsFailure` y hacen `return`, pero el compilador **no puede correlacionar** `IsFailure == false` con `Value != null` porque `Result<T>` no está anotado con atributos de análisis de flujo.

```csharp
if (result.IsFailure)
    return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value); // ← CS8602
```

**Solución (enterprise, arregla las 3 de una vez y previene futuras):** anotar `Result<T>` con atributos de análisis de nulabilidad. En `PawTrack.Domain` (o donde viva `Result<T>`):

```csharp
using System.Diagnostics.CodeAnalysis;

public class Result<T> : Result
{
    [MemberNotNullWhen(true, nameof(Value))]
    public new bool IsSuccess => base.IsSuccess;

    [MemberNotNullWhen(false, nameof(Value))]
    public new bool IsFailure => base.IsFailure;

    public T? Value { get; }
}
```

Con `[MemberNotNullWhen]`, tras `if (result.IsFailure) return ...;` el compilador sabe que `result.Value` no es `null` y las 3 advertencias desaparecen **sin tocar los controladores**.

> ⚠️ Este cambio puede exponer advertencias nuevas en otros puntos donde hoy se accede a `Value` _antes_ de verificar. Es exactamente el objetivo: son bugs latentes.

**Alternativa táctica** (si no se quiere tocar `Result<T>`): patrón explícito por controlador.

```csharp
if (result.IsFailure || result.Value is null)
    return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
```

**Criterio de aceptación:** 0 `CS8602` en `PawTrack.API`; suite de integración verde.

---

### E-BE-004 · `CS9113` ×4 — parámetros primarios inyectados y nunca usados

**Severidad:** Media — dependencias fantasma en el grafo de DI, coste de resolución innecesario y señal de refactor incompleto.

| Archivo                                                                                                                                                                            | Línea | Parámetro        |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ---------------- |
| [backend/src/PawTrack.Application/Chat/Queries/GetChatMessages/GetChatMessagesQuery.cs](backend/src/PawTrack.Application/Chat/Queries/GetChatMessages/GetChatMessagesQuery.cs#L35) | 35,17 | `unitOfWork`     |
| [backend/src/PawTrack.Application/Medical/ClinicAccess/ClinicAccessGrantCommands.cs](backend/src/PawTrack.Application/Medical/ClinicAccess/ClinicAccessGrantCommands.cs#L85)       | 85,21 | `userRepository` |
| [backend/src/PawTrack.API/Controllers/WidgetController.cs](backend/src/PawTrack.API/Controllers/WidgetController.cs#L18)                                                           | 18,46 | `sender`         |
| [backend/src/PawTrack.API/Controllers/AdoptionsController.cs](backend/src/PawTrack.API/Controllers/AdoptionsController.cs#L15)                                                     | 15,77 | `blobStorage`    |

**Análisis por caso — requiere decisión, no borrado ciego:**

1. **`GetChatMessagesQuery.unitOfWork`** — Un _query_ CQRS es de solo lectura y **por convención del repo nunca debe llamar a `SaveChangesAsync`**. La presencia de `IUnitOfWork` es un error de diseño. → **Eliminar el parámetro.**
2. **`ClinicAccessGrantCommands.userRepository`** — Sospechoso: un comando de otorgamiento de acceso médico probablemente _debería_ validar que el usuario solicitante existe / está activo. Revisar si falta lógica de autorización antes de eliminar. → **Revisar reglas de negocio primero.**
3. **`WidgetController.sender`** — Un controlador sin `ISender` no ejecuta MediatR; si el widget sirve contenido estático es correcto eliminarlo. → **Eliminar** salvo que haya endpoints pendientes.
4. **`AdoptionsController.blobStorage`** — La subida de fotos de adopción debería enrutarse por Blob Storage (convención del proyecto: _"Photos: Always route through Azure Blob Storage"_). Verificar si la subida se movió a un handler de Application. → **Verificar antes de eliminar.**

**Solución para los casos confirmados:**

```csharp
// Antes
internal sealed class GetChatMessagesQueryHandler(
    IChatRepository chatRepository,
    IUnitOfWork unitOfWork)                      // ← CS9113
    : IRequestHandler<GetChatMessagesQuery, Result<IReadOnlyList<ChatMessageDto>>>

// Después
internal sealed class GetChatMessagesQueryHandler(
    IChatRepository chatRepository)
    : IRequestHandler<GetChatMessagesQuery, Result<IReadOnlyList<ChatMessageDto>>>
```

**Criterio de aceptación:** 0 `CS9113`; registros de DI intactos; tests unitarios que construyen estos handlers manualmente actualizados.

---

### E-BE-005 · `CS0105` ×7 — directivas `using` duplicadas

**Severidad:** Baja (higiene) — pero indica ediciones automatizadas sin revisión y ensucia los diffs.

| Archivo                                                                                                                                                                  | Líneas | Namespace duplicado                                           |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------ | ------------------------------------------------------------- |
| [backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs](backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs#L39) | 39     | `PawTrack.Infrastructure.Sightings`                           |
| ídem                                                                                                                                                                     | 41     | `PawTrack.Application.Common.Settings`                        |
| ídem                                                                                                                                                                     | 53     | `PawTrack.Infrastructure.Subscriptions`                       |
| [backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs](backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs#L37)                         | 37     | `PawTrack.Domain.Bot`                                         |
| ídem                                                                                                                                                                     | 38     | `PawTrack.Domain.Medical`                                     |
| ídem                                                                                                                                                                     | 39     | `PawTrack.Domain.Outbox`                                      |
| [backend/src/PawTrack.API/Controllers/WebhooksController.cs](backend/src/PawTrack.API/Controllers/WebhooksController.cs#L7)                                              | 7      | `PawTrack.Application.Bounties.Commands.ConfirmBountyDeposit` |
| [backend/src/PawTrack.API/Program.cs](backend/src/PawTrack.API/Program.cs#L4)                                                                                            | 4      | `PawTrack.API.Hubs`                                           |

**Solución:** eliminar la línea duplicada de cada punto indicado. Puede automatizarse con `dotnet format --diagnostics IDE0005 CS0105` o _Remove and Sort Usings_ en el IDE. **Verificar que no se elimine el `using` original por error.**

**Prevención:** añadir al `.editorconfig` (ver **C-002**):

```ini
dotnet_diagnostic.CS0105.severity = error
dotnet_diagnostic.IDE0005.severity = warning
```

---

### E-BE-006 · `CS0219` — variable asignada y nunca usada

**Severidad:** Baja.

**Ubicación:** [backend/src/PawTrack.Infrastructure/Certificates/QuestPdfCertificateService.cs](backend/src/PawTrack.Infrastructure/Certificates/QuestPdfCertificateService.cs#L190) línea 190, col 22 — `const string Green = "#17a26d";`.

**Contexto:** el método `GeneratePassportPdf` declara una paleta de constantes (`Navy`, `Orange`, `Sand`, `Green`, `Gray`). `Green` nunca se usa en el layout del pasaporte.

**Decisión requerida:** ¿falta un elemento visual verde (p. ej. el badge de "vacunas al día" del pasaporte SENASA)? Si el diseño lo contempla, **es un bug de implementación incompleta**, no código muerto.

- Si el badge falta → implementarlo y usar `Green`.
- Si no aplica → eliminar la constante.

---

### E-BE-007 · `CS0618` — API obsoleta de QuestPDF

**Severidad:** Media — riesgo de rotura en la próxima actualización mayor de QuestPDF.

**Ubicación:** [backend/src/PawTrack.Infrastructure/Pets/QuestPdfIdCardService.cs](backend/src/PawTrack.Infrastructure/Pets/QuestPdfIdCardService.cs#L50) línea 50, col 33.

```
'ImageExtensions.Image(IContainer, string, ImageScaling)' está obsoleto:
'This element has been changed since version 2023.5. Please use the Image method
overload that returns the ImageDescriptor object.'
```

**Solución:**

```csharp
// Antes
container.Image(imagePath, ImageScaling.FitArea);

// Después (API de ImageDescriptor)
container.Image(imagePath).FitArea();
// Otras opciones equivalentes según el escalado anterior:
//   .FitWidth() / .FitHeight() / .FitUnproportionally()
```

**Riesgo de regresión:** el carnet de identidad de la mascota (`IdCard`) es un PDF de layout fijo. Tras el cambio **verificar visualmente el PDF generado** (o añadir una prueba de snapshot con Verify sobre el binario / dimensiones).

**Relacionado:** ver **C-004** (`NU1603`, versión de QuestPDF no fijada).

---

### E-BE-008 · 🔴 `xUnit1013` + `xUnit1008` — 3 casos de prueba de seguridad **nunca se ejecutan**

**Severidad:** 🔴 **Crítica** — es una supresión de facto de cobertura de seguridad.

**Ubicación:** [backend/tests/PawTrack.UnitTests/Chat/SendChatMessageTests.cs](backend/tests/PawTrack.UnitTests/Chat/SendChatMessageTests.cs#L176) línea 176, col 23 — clase `ChatContactGuardTests`.

```csharp
    [InlineData("Escríbeme a user@example.com")]
    [InlineData("llama al 8888-1234")]
    [InlineData("mi tel: +506 8881 2345")]
    public async Task Handle_BodyWithContactDetail_ReturnsFailure(string body)   // ← falta [Theory]
```

**Causa raíz:** el atributo `[Theory]` fue eliminado (o nunca añadido) al insertar el método `MakeSut()` justo encima. Sin `[Theory]`, xUnit **ignora completamente** el método y sus 3 `[InlineData]`. El test aparece como "no ejecutado", no como "fallido", por lo que la suite sigue en verde.

**Qué se dejó de proteger:** el guard anti-fuga de datos de contacto del chat enmascarado — precisamente el control que impide que un finder y un dueño intercambien teléfono/correo fuera de la plataforma (vector de fraude en la recuperación de mascotas y requisito del modelo de protección de datos, Ley 8968 CR).

**Solución:**

```csharp
    private SendChatMessageCommandHandler MakeSut() =>
        new(_chatRepo, _userRepo, _notifications, _lostPetRepo, _petRepo,
          _pii, Substitute.For<IChatNotifier>(), _uow, NullLogger<SendChatMessageCommandHandler>.Instance);

+   [Theory]
    [InlineData("Escríbeme a user@example.com")]
    [InlineData("llama al 8888-1234")]
    [InlineData("mi tel: +506 8881 2345")]
    public async Task Handle_BodyWithContactDetail_ReturnsFailure(string body)
```

**⚠️ Al reactivarlo es muy probable que los 3 casos fallen** (el guard puede haberse degradado sin que nadie lo notara). **Registrar los fallos resultantes como ítems nuevos en este documento antes de "arreglar el test".** No relajar las aserciones para que pase.

**Prevención (obligatoria):** convertir los analizadores de xUnit en errores. En `PawTrack.UnitTests.csproj`:

```xml
<PropertyGroup>
  <WarningsAsErrors>$(WarningsAsErrors);xUnit1013;xUnit1008;xUnit1026;xUnit2013</WarningsAsErrors>
</PropertyGroup>
```

---

### E-BE-009 · `CS8602` ×11 y `CS8604` ×2 en proyectos de prueba

**Severidad:** Baja — no afectan producción, pero degradan la señal de los tests (un NRE en el test se lee como fallo confuso en vez de aserción clara).

| Archivo                                                                                         | Líneas              |
| ----------------------------------------------------------------------------------------------- | ------------------- |
| `tests/PawTrack.UnitTests/Subscriptions/Commands/ManageSubscriptionPlansCommandHandlerTests.cs` | 29                  |
| `tests/PawTrack.UnitTests/Bounties/BountyTests.cs`                                              | 145                 |
| `tests/PawTrack.UnitTests/Stores/StoreOrderTests.cs`                                            | 225                 |
| `tests/PawTrack.UnitTests/ServiceProviders/RegisterServiceProviderCommandHandlerTests.cs`       | 44                  |
| `tests/PawTrack.UnitTests/Medical/Handlers/MedicalCommandHandlerTests.cs`                       | 214, 304            |
| `tests/PawTrack.UnitTests/Collars/Handlers/ActivateCollarTagCommandHandlerTests.cs`             | 65                  |
| `tests/PawTrack.UnitTests/Collars/Handlers/GenerateCollarDeviceKeyCommandHandlerTests.cs`       | 46                  |
| `tests/PawTrack.UnitTests/Security/Round49SecurityRegressionTests.cs`                           | 30, 44, 56          |
| `tests/PawTrack.UnitTests/Security/Round37SecurityRegressionTests.cs`                           | 110, 130 (`CS8604`) |

**Patrón común (CS8602):** `result.Value.Algo.Should()...` sin verificar `result.IsSuccess` primero — mismo origen que **E-BE-003**. **Resolver E-BE-003 con `[MemberNotNullWhen]` elimina la mayoría de estas automáticamente.** Para las restantes:

```csharp
// Antes
result.Value.Name.Should().Be("Plan Plus");

// Después — falla con mensaje claro si el Result vino en Failure
result.IsSuccess.Should().BeTrue(because: string.Join(", ", result.Errors));
result.Value!.Name.Should().Be("Plan Plus");
```

**Patrón CS8604 (`Round37SecurityRegressionTests.cs:110,130`):** se pasa un array potencialmente nulo a `IClientProxy.SendCoreAsync(string method, object?[] args, ...)`. Usar `Arg.Any<object?[]>()` o materializar el array antes.

---

## 5. Frontend — errores de compilación TypeScript (3)

> Estos 3 errores estaban **ocultos por las supresiones S-02, S-03 y S-04**. Ahora rompen `npm run build`.

### E-FE-001 · 🔴 `TS2769` — handler `message` incompatible en el Service Worker

**Severidad:** 🔴 Alta — es el mecanismo que aplica actualizaciones de la PWA.

**Ubicación:** [frontend/src/sw.ts](frontend/src/sw.ts#L17) línea 17, col 34.

```ts
self.addEventListener("message", (event: MessageEvent) => {
  // ← TS2769
  if ((event.data as { type?: string })?.type === "SKIP_WAITING")
    self.skipWaiting();
});
```

**Causa raíz:** en el ámbito `ServiceWorkerGlobalScope` el evento `message` es de tipo **`ExtendableMessageEvent`**, no `MessageEvent` (el DOM `MessageEvent` exige `initMessageEvent`, que `ExtendableMessageEvent` no tiene). El tipo se anotó a mano con el tipo de ventana en lugar del de worker.

**Impacto:** funcionalmente el JS emitido funciona (esbuild borra tipos), pero el contrato está mal descrito: el código no puede acceder a `event.waitUntil()` ni a `event.source`, y cualquier refactor que lo asuma fallará en runtime. Además hace inauditable el flujo `SKIP_WAITING` que dispara [frontend/src/shared/ui/UpdateBanner.tsx](frontend/src/shared/ui/UpdateBanner.tsx).

**Solución:**

```ts
self.addEventListener("message", (event: ExtendableMessageEvent) => {
  if ((event.data as { type?: string } | undefined)?.type === "SKIP_WAITING")
    self.skipWaiting();
});
```

Requiere que `tsconfig.worker.json` incluya `"lib": ["ES2022", "WebWorker"]` (ya lo hace).

---

### E-FE-002 · 🔴 `TS2353` — `actions` no existe en `NotificationOptions`

**Severidad:** 🔴 Alta — **rompe una función de producto documentada**.

**Ubicación:** [frontend/src/sw.ts](frontend/src/sw.ts#L71) línea 71, col 7.

```ts
const actions = resolveCheckNotificationId
  ? [
      { action: "resolve-yes", title: "Sí, ya está en casa" },
      { action: "resolve-no", title: "No, sigue perdido" },
    ]
  : undefined;

event.waitUntil(
  self.registration.showNotification(title, {
    body,
    icon: "/pwa-192x192.png",
    badge: "/pwa-192x192.png",
    tag: `pawtrack-alert-${Date.now()}`,
    data: { url: url ?? "/", resolveCheckNotificationId },
    actions, // ← TS2353
    requireInteraction: false,
  }),
);
```

**Causa raíz:** el `lib.dom.d.ts` de TypeScript declara `actions` en `NotificationOptions` **solo cuando la librería `WebWorker` está activa** y bajo el tipo `NotificationAction[]`; con `"types": []` y el `lib` actual la propiedad no está visible. Es un hueco conocido de las definiciones de TS para service workers.

**Impacto de producto:** los botones "Sí, ya está en casa" / "No, sigue perdido" de la notificación push de _resolve-check_ — el flujo que cierra el ciclo de reunificación — dependen de este campo. Si el navegador los ignora, el `notificationclick` con `event.action` **nunca se dispara** y el usuario no puede resolver el reporte desde la notificación.

**Solución (enterprise — tipar en vez de castear a `any`):** añadir un archivo de aumento de tipos, p. ej. `frontend/src/sw-types.d.ts`:

```ts
// El lib.dom de TypeScript aún no expone `actions` en NotificationOptions
// dentro del ámbito de Service Worker, pese a estar en la especificación
// de Notifications API y soportado por Chromium/Edge/Android.
// https://developer.mozilla.org/docs/Web/API/ServiceWorkerRegistration/showNotification
interface NotificationAction {
  action: string;
  title: string;
  icon?: string;
}

interface NotificationOptions {
  actions?: NotificationAction[];
}

export {};
```

E incluirlo en `tsconfig.worker.json`:

```json
"include": ["src/sw.ts", "src/sw-types.d.ts"]
```

**Verificación manual obligatoria:** enviar un push real con `resolveCheckNotificationId` en Chrome Android y confirmar que aparecen los 2 botones y que al pulsarlos se navega a `/notifications?resolveCheckNotificationId=...`.

---

### E-FE-003 · `TS2769` — bloque `test` no reconocido en `vite.config.ts`

**Severidad:** Media — impide type-checkear la configuración de Vite/Vitest y todo `e2e/`.

**Ubicación:** [frontend/vite.config.ts](frontend/vite.config.ts#L86) línea 86, col 3.

```
Object literal may only specify known properties, and 'test' does not exist in type 'UserConfigExport'.
```

**Causa raíz:** el archivo importa `defineConfig` desde `"vite"`, cuyo tipo no conoce la clave `test` (propiedad de Vitest). El bloque `test` que contiene `environment`, `setupFiles`, `exclude: [..., "e2e/**"]` y la configuración de cobertura **no está siendo verificado por el compilador**, de modo que un typo en `exclude` (crítico: es lo que evita que Vitest intente ejecutar las specs de Playwright) pasaría desapercibido.

**Solución (recomendada):**

```ts
// Antes
import { defineConfig } from "vite";

// Después
import { defineConfig } from "vitest/config";
```

`vitest/config` reexporta `defineConfig` de Vite con el tipo `UserConfig` extendido con `test`. No cambia el comportamiento en runtime.

**Alternativa** (si se quiere mantener el import de `vite`): añadir `/// <reference types="vitest" />` en la primera línea del archivo.

---

## 6. Frontend — ESLint (272 errores + 24 advertencias)

Reporte completo reproducible con `cd frontend && npx eslint . -f json -o ..\eslint.json`.

| #        | Regla                                                                                                                                                                  | Cant. | Severidad      | Naturaleza                              |
| -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----: | -------------- | --------------------------------------- |
| L-FE-001 | `react-hooks/rules-of-hooks`                                                                                                                                           |     3 | 🔴 **Crítica** | Bug de runtime en React                 |
| L-FE-002 | `@typescript-eslint/no-misused-promises`                                                                                                                               |    40 | 🔴 Alta        | Errores no capturados                   |
| L-FE-003 | `@typescript-eslint/no-floating-promises`                                                                                                                              |    31 | 🟠 Alta        | Errores silenciados                     |
| L-FE-004 | `jsx-a11y/label-has-associated-control`                                                                                                                                |   112 | 🟠 Alta        | Accesibilidad (WCAG 2.1 A)              |
| L-FE-005 | `react-hooks/exhaustive-deps`                                                                                                                                          |    17 | 🟠 Media-Alta  | Estado obsoleto / efectos perdidos      |
| L-FE-006 | `@typescript-eslint/no-unsafe-*` (member-access, assignment, call, return, argument)                                                                                   |    30 | 🟠 Media       | `any` implícito, pérdida de tipos       |
| L-FE-007 | `@typescript-eslint/no-unnecessary-type-assertion`                                                                                                                     |    24 | 🟡 Baja        | Ruido / oculta cambios de tipo          |
| L-FE-008 | `@typescript-eslint/no-unused-vars`                                                                                                                                    |     9 | 🟡 Baja        | Código muerto                           |
| L-FE-009 | `@typescript-eslint/no-unused-expressions`                                                                                                                             |     3 | 🟠 Media       | **Posible lógica que nunca se ejecuta** |
| L-FE-010 | `react-refresh/only-export-components`                                                                                                                                 |     7 | 🟡 Baja        | Degrada HMR en desarrollo               |
| L-FE-011 | Resto de `jsx-a11y` (`no-autofocus`, `click-events-have-key-events`, `no-static-element-interactions`, `no-redundant-roles`, `no-noninteractive-element-interactions`) |    12 | 🟠 Media       | Accesibilidad                           |
| L-FE-012 | `@typescript-eslint/no-redundant-type-constituents`                                                                                                                    |     3 | 🟡 Baja        | Tipos que colapsan a `any`/`unknown`    |
| L-FE-013 | `@typescript-eslint/require-await`                                                                                                                                     |     2 | 🟡 Baja        | `async` sin `await`                     |
| L-FE-014 | `@typescript-eslint/prefer-promise-reject-errors`                                                                                                                      |     1 | 🟠 Media       | Rechazo con no-Error                    |
| L-FE-015 | `@typescript-eslint/restrict-template-expressions`                                                                                                                     |     1 | 🟡 Baja        | Interpolación insegura                  |
| L-FE-016 | `@typescript-eslint/no-empty-object-type`                                                                                                                              |     1 | 🟡 Baja        | Tipo `{}` (equivale a `any` no nulo)    |

---

### L-FE-001 · 🔴 Hooks llamados condicionalmente — crash garantizado de React

**Severidad:** 🔴 **Crítica.** Produce `Error: Rendered more hooks than during the previous render` y **desmonta el árbol de React** (pantalla en blanco) en el momento exacto en que la consulta pasa de `isLoading` a `success`.

#### 1. [frontend/src/features/lost-pets/pages/ReportLostPage.tsx](frontend/src/features/lost-pets/pages/ReportLostPage.tsx#L179) — líneas 179 y 180

```ts
// línea 74
if (isLoading) { return ( ...spinner... ); }     // ← early return #1
// línea 94
if (!pet)      { return ( ...not found... ); }   // ← early return #2

// ...

// línea 179-180  ⚠️ hooks DESPUÉS de los early returns
const [step, setStep]           = useState<1 | 2 | 3>(1);
const [direction, setDirection] = useState<1 | -1>(1);
```

En el primer render `isLoading === true` → React registra sólo los 13 `useState` de las líneas 41-53. En el segundo render `isLoading === false` → se ejecutan además los `useState` de 179-180. React detecta 15 hooks donde antes había 13 y lanza el error.

**Este es el formulario de "Reportar mascota perdida" — la ruta más crítica del producto.**

**Solución:** mover ambos `useState` al bloque de hooks del tope del componente (junto a los de las líneas 41-53), **antes** de cualquier `return` condicional.

```ts
export function ReportLostPage() {
  // ── Wizard state (debe ir ANTES de cualquier early return) ──
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [direction, setDirection] = useState<1 | -1>(1);

  const [description, setDescription] = useState("");
  // ...resto de estado existente...

  if (isLoading) return <Spinner />;
  if (!pet) return <NotFound />;
  // ...
}
```

#### 2. [frontend/src/features/lost-pets/pages/LostReportConfirmationPage.tsx](frontend/src/features/lost-pets/pages/LostReportConfirmationPage.tsx#L271) — línea 271

```ts
const { data: localRecoveryStats } = useRecoveryRates({
  species: pet.species,
  breed: pet.breed,
  canton: null,
});
```

Mismo patrón: `useRecoveryRates` (que envuelve `useQuery`) se invoca después de un early return que garantiza `pet`.

**Solución:** subir la llamada al bloque de hooks y usar la opción `enabled` de React Query para no disparar la petición mientras faltan los datos:

```ts
const { data: localRecoveryStats } = useRecoveryRates(
  { species: pet?.species, breed: pet?.breed, canton: null },
  { enabled: Boolean(pet) }, // requiere que useRecoveryRates acepte opciones
);
```

**Criterio de aceptación:** 0 hallazgos de `react-hooks/rules-of-hooks` + prueba manual de ambas páginas con red lenta (DevTools → Slow 3G) confirmando que la transición loading→loaded no rompe la UI.

---

### L-FE-002 · `no-misused-promises` (40) — funciones `async` pasadas donde se espera `void`

**Severidad:** 🔴 Alta.

**Distribución (top):**

| Archivo                                                   | Cant. |
| --------------------------------------------------------- | ----: |
| `src/features/admin/pages/AdminPage.tsx`                  |     5 |
| `src/features/lost-pets/pages/SearchCoordinationPage.tsx` |     4 |
| `src/app/layout/AuthenticatedLayout.tsx`                  |     2 |
| `src/features/sightings/pages/QuickFoundPetPage.tsx`      |     2 |
| `src/features/lost-pets/components/SharePetButton.tsx`    |     2 |
| `src/features/clinics/components/ScanInput.tsx`           |     2 |
| `src/features/chat/pages/ChatThreadPage.tsx`              |     2 |
| `src/features/chat/pages/ChatPage.tsx`                    |     2 |
| `src/shared/hooks/usePullToRefresh.ts`                    |     2 |
| `src/features/chat/components/ChatPanel.tsx`              |     1 |
| `src/features/sightings/pages/ReportSightingPage.tsx`     |     1 |
| `src/features/sightings/pages/ReportFoundPetPage.tsx`     |     1 |
| _(y ~14 más con 1 cada uno)_                              |       |

**Causa raíz y por qué importa:** cuando se pasa `async () => {...}` a un `onClick`, `onSubmit` o `addEventListener`, React recibe una `Promise` que **nunca observa**. Si la función lanza (fallo de red, 401, validación), el rechazo:

- **no** activa ningún `ErrorBoundary` de React,
- **no** activa el `onError` de React Query,
- se convierte en un `unhandledrejection` global — invisible salvo que Application Insights lo capture.

Efecto práctico: el usuario pulsa "Reportar avistamiento", falla la petición, y **no ve nada**: ni error, ni spinner detenido.

**Soluciones por caso de uso:**

_a) Handler de evento que dispara una mutación (caso mayoritario):_

```tsx
// Antes
<form onSubmit={async (e) => { e.preventDefault(); await mutateAsync(payload); }}>

// Después — usar la variante no-async de React Query
<form onSubmit={(e) => { e.preventDefault(); mutate(payload); }}>
```

`mutate` (a diferencia de `mutateAsync`) no devuelve promesa y enruta los errores a `onError` / al estado `error` de la mutación.

_b) Cuando sí se necesita `await` dentro del handler:_

```tsx
<button
  onClick={() => {
    void (async () => {
      try {
        await doWork();
      } catch (err) {
        toast.error(extractApiError(err));
      }
    })();
  }}
>
```

_c) Handler pasado a un `useEffect`:_

```ts
useEffect(() => {
  void loadData(); // `void` explícito documenta la intención
}, [loadData]);
```

> ❌ **No** desactivar la regla ni añadir `checksVoidReturn: false` a la configuración: eso reintroduce exactamente la supresión que esta auditoría eliminó.

---

### L-FE-003 · `no-floating-promises` (31) — promesas sin `await`, `.catch()` ni `void`

**Severidad:** 🟠 Alta.

| Archivo                                                       | Cant. |
| ------------------------------------------------------------- | ----: |
| `src/features/admin/components/CollarTagInventorySection.tsx` |     6 |
| `src/features/lost-pets/components/ReuniteButton.tsx`         |     4 |
| `src/features/auth/hooks/useAuth.ts`                          |     3 |
| `src/features/pets/pages/CreatePetPage.tsx`                   |     2 |
| `src/features/pets/pages/ActivateCollarTagPage.tsx`           |     2 |
| `src/features/pets/components/CollarGpsTab.tsx`               |     2 |
| `src/features/adoptions/pages/ShelterPublishPage.tsx`         |     2 |
| `src/features/lost-pets/pages/ReportLostPage.tsx`             |     1 |
| `src/features/notifications/components/NotificationItem.tsx`  |     1 |
| `src/features/stores/components/CheckoutModal.tsx`            |     1 |
| `src/features/pets/components/CollarHandoverDialog.tsx`       |     1 |
| `src/features/auth/pages/ProfilePage.tsx`                     |     1 |
| _(y ~5 más)_                                                  |       |

**⚠️ Prioridad máxima:** los 3 casos en [frontend/src/features/auth/hooks/useAuth.ts](frontend/src/features/auth/hooks/useAuth.ts) — un rechazo silencioso en el flujo de autenticación puede dejar la sesión en un estado inconsistente (token en memoria pero store sin actualizar), que es la clase de bug que ya se documentó en el interceptor de `apiClient`.

**Solución:** para cada llamada, decidir explícitamente:

```ts
// 1. Nos importa el resultado → await (dentro de async)
await queryClient.invalidateQueries({ queryKey: ["pets"] });

// 2. Fire-and-forget aceptable → void + manejo de error
void queryClient
  .invalidateQueries({ queryKey: ["pets"] })
  .catch((err) => logger.warn("invalidate failed", err));

// 3. Navegación de react-router v7 (devuelve Promise)
void navigate("/pets");
```

---

### L-FE-004 · `jsx-a11y/label-has-associated-control` (112) — etiquetas sin control asociado

**Severidad:** 🟠 Alta — **incumplimiento de WCAG 2.1 nivel A (criterio 1.3.1 / 4.1.2)**. Bloquea el uso con lectores de pantalla y afecta a formularios institucionales (municipal, SENASA, clínicas) donde la accesibilidad puede ser un requisito contractual.

**Distribución (top 12 de ~30 archivos):**

| Archivo                                                               | Cant. |
| --------------------------------------------------------------------- | ----: |
| `src/features/service-providers/pages/ProviderServicesPage.tsx`       |    13 |
| `src/features/medical/components/MedicalHistoryTab.tsx`               |    10 |
| `src/features/promotions/components/AdminPromotionManager.tsx`        |     9 |
| `src/features/advertising/components/AdminBillboardsTab.tsx`          |     8 |
| `src/features/stores/pages/StoreRegistrationPage.tsx`                 |     7 |
| `src/features/admin/pages/MunicipalDashboardPage.tsx`                 |     7 |
| `src/features/bundles/components/BundleOrderModal.tsx`                |     6 |
| `src/features/clinics/components/ClinicExpedienteTab.tsx`             |     6 |
| `src/features/adoptions/pages/ShelterPublishPage.tsx`                 |     6 |
| `src/features/sightings/pages/ReportFoundPetPage.tsx`                 |     6 |
| `src/features/service-providers/pages/ServiceProviderProfilePage.tsx` |     6 |
| `src/features/stores/pages/StoreLocationsPage.tsx`                    |     5 |

**Patrón erróneo predominante:**

```tsx
<label className="text-sm font-medium">Nombre de la mascota</label>
<input type="text" value={name} onChange={...} />
```

**Solución A — anidado (más simple, no requiere id único):**

```tsx
<label className="text-sm font-medium">
  Nombre de la mascota
  <input type="text" value={name} onChange={...} />
</label>
```

**Solución B — `htmlFor` + `id` (preferida cuando el layout exige separarlos):**

```tsx
const nameId = useId();          // React 18+, garantiza unicidad en SSR y listas
...
<label htmlFor={nameId} className="text-sm font-medium">Nombre de la mascota</label>
<input id={nameId} type="text" value={name} onChange={...} />
```

> **Nota crítica para tests:** varias suites usan `getByLabelText(...)`. Corregir esta regla **mejora** esos tests, pero cuidado con el patrón ya documentado en el repo: `getByLabelText(/contraseña/i)` puede coincidir con el input **y** con el botón mostrar/ocultar. Usar `{ selector: "input" }` o `{ exact: true }`.

**Estrategia sugerida:** abordar por archivo, empezando por los formularios de cara al público (`ReportFoundPetPage`, `StoreRegistrationPage`, `ServiceProviderRegistrationPage`) antes que los paneles de admin.

---

### L-FE-005 · `react-hooks/exhaustive-deps` (17) — dependencias de efectos incompletas

**Severidad:** 🟠 Media-Alta. **12 de estos 17 estaban activamente suprimidos** con `// eslint-disable-next-line` (supresión S-07).

| Archivo                                                        |        Línea | ¿Estaba suprimido?  |
| -------------------------------------------------------------- | -----------: | ------------------- |
| `src/features/auth/hooks/useAuthInit.ts`                       |           40 | ✅ sí               |
| `src/features/auth/pages/ProfilePage.tsx`                      |          436 | ✅ sí               |
| `src/features/chat/components/ChatPanel.tsx`                   |          171 | no                  |
| `src/features/family/pages/AcceptFamilyInvitationPage.tsx`     |           46 | ✅ sí               |
| `src/features/lost-pets/components/ReuniteButton.tsx`          |           81 | no                  |
| `src/features/lost-pets/hooks/useSearchCoordinationHub.ts`     |           71 | ✅ sí               |
| `src/features/lost-pets/pages/LostReportConfirmationPage.tsx`  |     104, 189 | ✅ sí (ambas)       |
| `src/features/lost-pets/pages/ReportLostPage.tsx`              |           64 | ✅ sí               |
| `src/features/map/components/MapContainer.tsx`                 | 81, 101, 129 | 81 sí; 101 y 129 no |
| `src/features/map/hooks/useMovementPrediction.ts`              |      42 (×2) | ✅ sí               |
| `src/features/notifications/components/NotificationCenter.tsx` |           98 | no                  |
| `src/features/pets/components/QRCodeDisplay.tsx`               |           29 | ✅ sí               |
| `src/features/sightings/pages/ReportFoundPetPage.tsx`          |           66 | ✅ sí               |

**Cómo abordarlo — NO añadir la dependencia a ciegas.** Para cada caso, clasificar:

1. **"Solo al montar"** (el caso más común aquí) → sustituir el `useEffect` con deps vacías por el patrón explícito:
   ```ts
   const didInit = useRef(false);
   useEffect(() => {
     if (didInit.current) return;
     didInit.current = true;
     void initialise();
   }, [initialise]); // ahora las deps SÍ son exhaustivas
   ```
2. **Función recreada en cada render** → envolverla en `useCallback` con sus propias deps, y luego incluirla.
3. **Valor que sólo se lee, no se observa** → capturarlo en un `useRef` actualizado en un efecto aparte.
4. **Dependencia genuinamente olvidada** → **es un bug de estado obsoleto (stale closure)**; añadirla y probar.

Los casos de `useSearchCoordinationHub.ts` (SignalR) y `MapContainer.tsx` (Leaflet) son los de mayor riesgo: efectos que crean y destruyen conexiones/instancias externas; una dependencia mal puesta genera fugas de conexión o mapas duplicados.

---

### L-FE-006 · `no-unsafe-*` (30) — pérdida de tipos hacia `any`

**Severidad:** 🟠 Media. Anula la garantía de `strict: true`.

| Archivo                                                      | member-access | assignment | otros                          |
| ------------------------------------------------------------ | ------------: | ---------: | ------------------------------ |
| `src/shared/lib/apiClient.ts`                                |             7 |          2 | +1 `no-unsafe-argument` (L-76) |
| `src/features/lost-pets/components/CantonChoroplethMap.tsx`  |             3 |          1 |                                |
| `src/sw.ts`                                                  |             2 |          — |                                |
| `src/features/auth/api/authApi.ts`                           |             1 |          1 |                                |
| `src/features/medical/components/WeightTrendChart.tsx`       |             1 |          — |                                |
| `src/features/advertising/components/AdminBillboardsTab.tsx` |             — |          1 |                                |
| `src/features/medical/components/ReminderCalendar.tsx`       |             — |          1 |                                |
| `src/features/stores/**` (5 archivos)                        |             — |          5 |                                |
| `src/features/promotions/api/promotionApi.ts`                |             — |          — | 1 `no-unsafe-return`           |
| `src/features/service-providers/api/serviceProvidersApi.ts`  |             — |          — | 1 `no-unsafe-return`           |

**Foco prioritario: [frontend/src/shared/lib/apiClient.ts](frontend/src/shared/lib/apiClient.ts)** — 10 hallazgos concentrados en el interceptor de errores de Axios, que es la pieza que enruta **todos** los 401 y el refresh silencioso. Es el mismo archivo donde ya se corrigió un bug de producción (redirección dura en el 401 del login). Tiparlo correctamente es prevención directa.

**Solución para el interceptor de Axios:**

```ts
import axios, { AxiosError, isAxiosError } from "axios";

interface ApiProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

function extractApiError(error: unknown): string {
  if (isAxiosError<ApiProblemDetails>(error)) {
    return (
      error.response?.data?.detail ??
      error.response?.data?.title ??
      error.message
    );
  }
  return error instanceof Error ? error.message : "Error desconocido";
}
```

**Para los `no-unsafe-return` en `promotionApi.ts:59` y `serviceProvidersApi.ts:254`:** tipar explícitamente el genérico de la llamada:

```ts
// Antes
const { data } = await apiClient.get(`/api/promotions/${code}`);
return data; // ← any

// Después
const { data } = await apiClient.get<PromotionDto>(`/api/promotions/${code}`);
return data;
```

**Para `src/sw.ts` (2):** provienen de `self.__WB_MANIFEST` (inyectado por vite-plugin-pwa). Declarar el tipo en `sw-types.d.ts` (el mismo archivo de E-FE-002):

```ts
declare global {
  interface ServiceWorkerGlobalScope {
    __WB_MANIFEST: Array<{ url: string; revision: string | null }>;
  }
}
```

---

### L-FE-007 · `no-unnecessary-type-assertion` (24) — `as` redundantes

**Severidad:** 🟡 Baja, pero **peligrosa a futuro**: una aserción innecesaria hoy se convierte en una aserción _incorrecta_ mañana cuando el tipo subyacente cambie, y el compilador ya no avisará.

| Archivo                                                   | Cant. |
| --------------------------------------------------------- | ----: |
| `src/features/map/pages/PublicMapPage.tsx`                |     4 |
| `src/features/medical/components/MedicalHistoryTab.tsx`   |     3 |
| `src/features/clinics/components/ClinicExpedienteTab.tsx` |     2 |
| `src/features/clinics/components/ClinicTiersModal.tsx`    |     2 |
| `src/features/pets/components/FreemiumModal.tsx`          |     2 |
| _(11 archivos más con 1 cada uno)_                        |    11 |

**Solución:** eliminar el `as X`. ESLint puede autocorregirlo:

```powershell
cd frontend
npx eslint . --fix --rule '{"@typescript-eslint/no-unnecessary-type-assertion":"error"}'
npm run typecheck   # verificar que no se rompió nada
```

---

### L-FE-008 · `no-unused-vars` (9) — código muerto

| Archivo                                                |          Línea |
| ------------------------------------------------------ | -------------: |
| `src/app/layout/BottomNav.tsx`                         |             60 |
| `src/features/bundles/components/BundleOrderModal.tsx` |            504 |
| `src/features/clinics/pages/ClinicDashboardPage.tsx`   |            723 |
| `tests/features/map/PublicMapPage.test.tsx`            |         9 (×2) |
| `tests/setup.ts`                                       | 11, 12, 15, 16 |

**Solución:** eliminar las variables/imports. Si un parámetro es obligatorio por firma pero no se usa, prefijarlo con `_` (la config de `typescript-eslint` lo permite por defecto vía `argsIgnorePattern`).

Los 4 de `tests/setup.ts` (líneas 11-16) probablemente son parámetros de los polyfills de pointer/scroll para Radix en JSDOM → prefijar con `_`.

---

### L-FE-009 · 🟠 `no-unused-expressions` (3) — expresiones sin efecto

**Severidad:** 🟠 Media. **Puede indicar lógica que el desarrollador creía que se ejecutaba y no se ejecuta.**

| Archivo                                                                                                                           | Línea |
| --------------------------------------------------------------------------------------------------------------------------------- | ----: |
| [frontend/src/features/admin/pages/AdminPage.tsx](frontend/src/features/admin/pages/AdminPage.tsx#L221)                           |   221 |
| [frontend/src/features/admin/pages/AdminPage.tsx](frontend/src/features/admin/pages/AdminPage.tsx#L284)                           |   284 |
| [frontend/src/features/admin/pages/MunicipalDashboardPage.tsx](frontend/src/features/admin/pages/MunicipalDashboardPage.tsx#L105) |   105 |

**Acción:** **inspeccionar manualmente cada línea antes de tocarla.** Patrones típicos y su corrección:

```ts
// a) Optional-call mal escrito — la función NUNCA se llama
onClose?.;            // ❌  →  onClose?.();

// b) Cortocircuito sin efecto
isOpen && setOpen(false);      // ⚠️ válido en JS pero prohibido por la regla
// → if (isOpen) setOpen(false);

// c) Acceso a propiedad "por si acaso" — código muerto real
result.data;          // ❌ eliminar
```

Dado que ambos archivos son paneles administrativos (`AdminPage`, `MunicipalDashboardPage`), un handler que no se dispara puede significar **una acción de moderación o un reporte municipal que nunca se ejecuta**.

---

### L-FE-010 · `react-refresh/only-export-components` (7) — degradación de HMR

**Severidad:** 🟡 Baja (solo afecta la experiencia de desarrollo: el módulo se recarga completo en vez de hacer hot-reload, perdiendo el estado).

| Archivo                                             |  Línea |
| --------------------------------------------------- | -----: |
| `src/app/routes.tsx`                                | 12, 22 |
| `src/features/pets/components/OnboardingWizard.tsx` |    130 |
| `src/shared/hooks/useProgressiveImage.tsx`          |     13 |
| `src/shared/ui/AmbientPaws.tsx`                     | 39, 47 |
| `src/shared/ui/CookieConsentBanner.tsx`             |     78 |

**Solución:** mover las exportaciones no-componente (constantes, hooks, helpers) a su propio archivo. Ejemplo:

```
src/shared/ui/AmbientPaws.tsx        → sólo el componente <AmbientPaws />
src/shared/ui/ambientPaws.helpers.ts → las constantes/funciones exportadas
```

Para `src/app/routes.tsx` es aceptable dejarlo (es un módulo de configuración de rutas, no de componentes); alternativamente añadir un override acotado en `eslint.config.js` **con justificación escrita**:

```js
{
  files: ["src/app/routes.tsx"],
  rules: { "react-refresh/only-export-components": "off" },
}
```

---

### L-FE-011 · Resto de accesibilidad `jsx-a11y` (12)

| Regla                                                                     | Archivo                                                        |    Línea | Solución                                                                                                                                                                                                     |
| ------------------------------------------------------------------------- | -------------------------------------------------------------- | -------: | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `no-autofocus`                                                            | `src/features/auth/pages/LoginPage.tsx`                        | 447, 603 | Quitar `autoFocus`; usar `useEffect` + `ref.current?.focus()` solo si el foco es realmente esperado por el usuario. `autoFocus` desorienta a usuarios de lector de pantalla y hace saltar la vista en móvil. |
| `no-autofocus`                                                            | `src/features/auth/pages/ProfilePage.tsx`                      |      548 | ídem                                                                                                                                                                                                         |
| `no-autofocus`                                                            | `src/features/pets/pages/CreatePetPage.tsx`                    |      215 | ídem                                                                                                                                                                                                         |
| `no-redundant-roles`                                                      | `src/features/lost-pets/components/SearchChecklist.tsx`        |      195 | Quitar `role="list"` de un `<ul>` (rol implícito).                                                                                                                                                           |
| `no-redundant-roles`                                                      | `src/features/notifications/components/NotificationCenter.tsx` |      204 | ídem                                                                                                                                                                                                         |
| `click-events-have-key-events` + `no-static-element-interactions`         | `src/features/notifications/components/NotificationCenter.tsx` |      228 | Convertir el `<div onClick>` en `<button type="button">` con estilos, o añadir `role="button"`, `tabIndex={0}` y `onKeyDown` (Enter/Space).                                                                  |
| `click-events-have-key-events` + `no-noninteractive-element-interactions` | `src/features/notifications/components/NotificationCenter.tsx` |      232 | ídem                                                                                                                                                                                                         |
| `click-events-have-key-events` + `no-static-element-interactions`         | `src/features/sightings/components/VisualMatchPanel.tsx`       |      196 | ídem — **crítico**: es el panel de confirmación de coincidencia visual de una mascota perdida.                                                                                                               |

**Patrón de corrección recomendado (preferir el elemento semántico):**

```tsx
// Antes
<div className="cursor-pointer …" onClick={handleSelect}> … </div>

// Después
<button type="button" className="text-left w-full …" onClick={handleSelect}> … </button>
```

---

### L-FE-012 a L-FE-016 · Hallazgos individuales

| ID           | Regla                            | Ubicación                                                                                                  | Problema y solución                                                                                                                                                                                                                                                                                                |
| ------------ | -------------------------------- | ---------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **L-FE-012** | `no-redundant-type-constituents` | `src/features/medical/hooks/useActivity.ts:12`, `src/features/medical/hooks/useMedical.ts:27,48`           | Una unión del estilo `string \| any` o `X \| unknown` colapsa al miembro más ancho, anulando el tipado. Revisar la unión y eliminar el constituyente redundante (normalmente un `any` heredado de una respuesta de API sin tipar).                                                                                 |
| **L-FE-013** | `require-await`                  | `src/features/locations/hooks/useAlertPreference.ts:85`, `tests/features/allies/AllyPanelPage.test.tsx:63` | Función marcada `async` sin ningún `await`. Quitar `async` (y ajustar el tipo de retorno) o añadir el `await` que falta — verificar si se olvidó esperar una promesa.                                                                                                                                              |
| **L-FE-014** | `prefer-promise-reject-errors`   | [frontend/src/shared/lib/apiClient.ts](frontend/src/shared/lib/apiClient.ts#L83) línea 83                  | `Promise.reject(algoQueNoEsError)`. Los rechazos con no-`Error` pierden el stack trace, y Application Insights los registra sin contexto. Solución: `return Promise.reject(error instanceof Error ? error : new Error(String(error)));`. **Alta prioridad relativa** por estar en el interceptor global de la API. |
| **L-FE-015** | `restrict-template-expressions`  | `src/features/medical/components/ActivityTab.tsx:211`                                                      | Interpolación de un valor no-string en un template literal (produce `"[object Object]"` o `"undefined"` en la UI). Convertir explícitamente: `${String(v)}` o `${v?.toFixed(1) ?? "—"}`.                                                                                                                           |
| **L-FE-016** | `no-empty-object-type`           | `src/features/pets/api/petsApi.ts:66`                                                                      | El tipo `{}` acepta cualquier valor no-`null`/`undefined`. Sustituir por el DTO real, por `Record<string, never>` (objeto vacío estricto) o por `void` si el endpoint no devuelve cuerpo.                                                                                                                          |

---

## 7. Configuración, CI y deuda estructural

### C-001 · 🔴 Backend sin `TreatWarningsAsErrors` ni analizadores

**Severidad:** 🔴 Alta (causa raíz de que existan las 30 advertencias de §4).

Ninguno de los 6 `.csproj` define `TreatWarningsAsErrors`, `EnableNETAnalyzers`, `AnalysisMode` ni `EnforceCodeStyleInBuild`. Tampoco existe un `Directory.Build.props` ni un `.editorconfig` en el repositorio. **No hay ninguna barrera contra la acumulación de advertencias.**

**Solución (enterprise) — crear `backend/Directory.Build.props`:**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>

    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>Recommended</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <!-- CS1591: comentarios XML faltantes — no aplicable a una API interna -->
    <NoWarn>$(NoWarn);CS1591</NoWarn>

    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

**Plan de adopción (no activar `TreatWarningsAsErrors` de golpe):**

1. Cerrar E-BE-001 … E-BE-009 (30 advertencias únicas).
2. Activar `TreatWarningsAsErrors` sólo en `PawTrack.Domain` (el proyecto más limpio).
3. Extender a `Application` → `Infrastructure` → `API` → proyectos de test.
4. Excluir las migraciones generadas si aparecen advertencias nuevas:
   ```xml
   <ItemGroup>
     <Compile Update="**/Migrations/**/*.cs">
       <NoWarn>$(NoWarn);CS8618;CS8625;CA1814</NoWarn>
     </Compile>
   </ItemGroup>
   ```

---

### C-002 · No existe `.editorconfig`

**Severidad:** 🟠 Media.

Sin `.editorconfig`, las reglas de estilo IDExxxx no se aplican y cada desarrollador/agente formatea distinto. Crear `.editorconfig` en la raíz con, como mínimo:

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
insert_final_newline = true

dotnet_diagnostic.CS0105.severity = error   # using duplicado
dotnet_diagnostic.IDE0005.severity = warning # using innecesario
dotnet_diagnostic.CA2007.severity  = none    # ConfigureAwait no aplica en ASP.NET Core

csharp_style_namespace_declarations = file_scoped:warning
csharp_prefer_braces = when_multiline:suggestion

[*.{ts,tsx,js,json,yml,md}]
indent_style = space
indent_size = 2
charset = utf-8
insert_final_newline = true
```

---

### C-003 · Proyecto `HashGen` huérfano

**Severidad:** 🟡 Baja.

[backend/HashGen/HashGen.csproj](backend/HashGen/HashGen.csproj) **no está referenciado en [PawTrack.sln](PawTrack.sln)**, por lo que:

- nunca se compila en CI (`dotnet build` sobre la solución lo ignora),
- puede haberse degradado sin que nadie lo note,
- sus artefactos `bin/` y `obj/` están versionados o al menos presentes en el árbol de trabajo.

**Acción requerida — decidir:**

- Si es una herramienta de desarrollo vigente (generar hashes BCrypt para seeds) → **añadirla a la solución** (`dotnet sln add backend/HashGen/HashGen.csproj`) y a un `ItemGroup` de solución "tools", para que se compile en CI.
- Si es un script de un solo uso → **eliminarla** del repositorio y documentar el comando equivalente (p. ej. `dotnet script` o un endpoint de dev) en [docs/GUIA_ONBOARDING_DEV.md](docs/GUIA_ONBOARDING_DEV.md).

Además, confirmar que `backend/HashGen/bin/` y `backend/HashGen/obj/` estén cubiertos por [.gitignore](.gitignore).

---

### C-004 · `NU1603` ×4 — versión de QuestPDF no resoluble

**Severidad:** 🟠 Media — **build no reproducible**.

```
PawTrack.Infrastructure depende de QuestPDF (>= 2025.3.2), pero no se encontró
QuestPDF 2025.3.2. QuestPDF 2025.4.0 se resolvió en su lugar.
```

**Causa raíz:** `PawTrack.Infrastructure.csproj` referencia una versión de QuestPDF que **no existe en nuget.org**. NuGet aplica su regla de "versión mínima más cercana" y sube silenciosamente a 2025.4.0. Esto significa que:

- el build local y el de CI pueden resolver versiones distintas si NuGet publica una 2025.3.x posteriormente,
- el warning `CS0618` de **E-BE-007** proviene precisamente de esta versión no elegida.

**Solución:**

```xml
<!-- backend/src/PawTrack.Infrastructure/PawTrack.Infrastructure.csproj -->
- <PackageReference Include="QuestPDF" Version="2025.3.2" />
+ <PackageReference Include="QuestPDF" Version="2025.4.0" />
```

**Prevención (recomendado para un proyecto enterprise):** adoptar _Central Package Management_. Crear `Directory.Packages.props` en la raíz del backend:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="QuestPDF" Version="2025.4.0" />
    <!-- … resto de paquetes … -->
  </ItemGroup>
</Project>
```

y quitar los atributos `Version` de todos los `PackageReference`. Adicionalmente, habilitar _lock files_ (`RestorePackagesWithLockFile=true`) — el workflow [.github/workflows/backend.yml](.github/workflows/backend.yml) **ya cachea por `hashFiles('**/packages.lock.json')`, pero esos archivos no existen\*\*, así que la clave de caché es constante y el caché nunca se invalida correctamente.

---

### C-005 · `skipLibCheck: true` en el frontend

**Severidad:** 🟡 Baja / opcional.

Desactiva la verificación de tipos de todos los `.d.ts` de `node_modules`. Es práctica estándar y quitarlo introduciría ruido de `three`, `recharts` y `leaflet`.

**Recomendación:** conservarlo, pero programar una revisión trimestral ejecutando `tsc --noEmit --skipLibCheck false` para detectar incompatibilidades reales entre versiones de `@types/*`.

---

### C-006 · La caché de NuGet en CI nunca se invalida

**Severidad:** 🟡 Baja. Ver el último párrafo de **C-004**. `hashFiles('**/packages.lock.json')` sobre cero archivos devuelve siempre el mismo hash → la caché queda congelada indefinidamente y puede servir paquetes obsoletos. Se resuelve al habilitar lock files.

---

### C-007 · Fallos de prueba del frontend preexistentes

**Severidad:** 🟠 Media. Documentados previamente y **aún abiertos**:

| Test                                                                                     | Problema                                                                                                                                                                                                                                             |
| ---------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `tests/features/auth/LoginPage.test.tsx` → _"shows error alert when server returns 401"_ | El test espera el texto `/credenciales incorrectas/i`, pero `extractLoginError()` en `LoginPage.tsx` renderiza `"Correo o contraseña incorrectos."`. **Requiere decisión de producto sobre cuál es el texto correcto**, luego alinear código y test. |
| `tests/features/admin/AdminPage.test.tsx`                                                | Fallo preexistente confirmado, sin diagnóstico.                                                                                                                                                                                                      |
| `tests/features/auth/ProfilePage.test.tsx` → _"saves name via PATCH /auth/me"_           | Handler de MSW que no coincide + errores de CORS en jsdom contra `http://localhost:3000`. Mock desactualizado.                                                                                                                                       |

**Acción:** ejecutar `cd frontend && npm run test -- --run` y abrir un ítem por cada fallo aún vigente.

---

## 8. Vulnerabilidades de dependencias

### V-001 · 🔴 npm — 22 vulnerabilidades (1 crítica, 14 altas, 6 moderadas, 1 baja)

| Paquete                               | Severidad      | Aviso                                                                                                                                                                                                                                                                              | Vía                                                                            |
| ------------------------------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| `vitest` `<3.2.6`                     | 🔴 **Crítica** | [GHSA-5xrq-8626-4rwp](https://github.com/advisories/GHSA-5xrq-8626-4rwp) — con el servidor de Vitest UI escuchando, se puede leer y ejecutar un archivo arbitrario                                                                                                                 | directa (devDependency)                                                        |
| `vite` `<=6.4.2`                      | 🟠 Alta        | [GHSA-fx2h-pf6j-xcff](https://github.com/advisories/GHSA-fx2h-pf6j-xcff) — bypass de `server.fs.deny` en rutas alternativas de Windows · [GHSA-v6wh-96g9-6wx3](https://github.com/advisories/GHSA-v6wh-96g9-6wx3) — `launch-editor` filtra el hash NTLMv2 vía rutas UNC en Windows | directa                                                                        |
| `ws` `7.0.0-7.5.10 \|\| 8.0.0-8.20.1` | 🟠 Alta        | [GHSA-58qx-3vcg-4xpx](https://github.com/advisories/GHSA-58qx-3vcg-4xpx) divulgación de memoria no inicializada · [GHSA-96hv-2xvq-fx4p](https://github.com/advisories/GHSA-96hv-2xvq-fx4p) DoS por agotamiento de memoria                                                          | transitiva (`jsdom`, otros)                                                    |
| `serialize-javascript` `<=7.0.4`      | 🟠 Alta        | [GHSA-5c6j-r48x-rmvq](https://github.com/advisories/GHSA-5c6j-r48x-rmvq) RCE vía `RegExp.flags` / `Date.toISOString` · [GHSA-qj8w-gfj5-8c6v](https://github.com/advisories/GHSA-qj8w-gfj5-8c6v) DoS por agotamiento de CPU                                                         | transitiva: `workbox-build` → `@rollup/plugin-terser` → `serialize-javascript` |

**⚠️ Contexto de riesgo:** las 4 son cadenas de **build/desarrollo**, no se empaquetan en el bundle servido al usuario. Sin embargo `serialize-javascript` entra vía `workbox-build`, que **sí participa en la generación del service worker de producción** → un RCE en tiempo de build compromete el artefacto desplegado (ataque de cadena de suministro).

**Solución:**

```powershell
cd frontend
npm audit fix              # cubre las 4 cadenas anteriores según el propio reporte
npm run typecheck
npm run test -- --run
npm run build
npx playwright test        # vite y vitest son piezas del pipeline E2E
```

Si `npm audit fix` propone cambios mayores (`--force`), aplicarlos **de a un paquete** verificando la suite completa entre cada uno. Prestar atención especial a `vite` (el modo `e2e` y `vite preview` del pipeline de Playwright dependen de su comportamiento) y a `vite-plugin-pwa` ↔ `workbox-build` (versiones acopladas).

**Prevención:** añadir un job de auditoría al workflow del frontend:

```yaml
- name: Audit dependencies
  run: npm audit --audit-level=high
```

---

### V-002 · ✅ NuGet — sin paquetes vulnerables

`dotnet list PawTrack.sln package --vulnerable --include-transitive` reporta **0 paquetes vulnerables** en los 6 proyectos. Sin acción requerida.

**Prevención:** añadir al workflow del backend:

```yaml
- name: Audit NuGet packages
  run: dotnet list PawTrack.sln package --vulnerable --include-transitive
```

---

## 9. Orden de ejecución recomendado

| Fase                            | Objetivo                        | Ítems                                                                | Resultado esperado                                                    |
| ------------------------------- | ------------------------------- | -------------------------------------------------------------------- | --------------------------------------------------------------------- |
| **0 — Desbloqueo (urgente)**    | Restaurar un CI verde y honesto | E-FE-001, E-FE-002, E-FE-003, L-FE-001                               | `npm run typecheck` en verde; sin crashes de hooks                    |
| **1 — Seguridad y correctitud** | Cerrar riesgos activos          | E-BE-008, L-FE-002, L-FE-003, L-FE-009, L-FE-014, V-001              | Tests de seguridad del chat ejecutándose; errores visibles al usuario |
| **2 — Nulabilidad backend**     | Blindar el patrón `Result<T>`   | E-BE-003 (`[MemberNotNullWhen]`), E-BE-001, E-BE-002, E-BE-009       | ~18 advertencias eliminadas de una vez                                |
| **3 — Accesibilidad**           | Cumplimiento WCAG 2.1 A         | L-FE-004, L-FE-011                                                   | 124 hallazgos cerrados; formularios usables con lector de pantalla    |
| **4 — Tipado y estado**         | Recuperar garantías de `strict` | L-FE-005, L-FE-006, L-FE-012, L-FE-013, L-FE-015, L-FE-016           | Sin `any` implícitos en las rutas de API                              |
| **5 — Higiene**                 | Limpieza                        | E-BE-004, E-BE-005, E-BE-006, E-BE-007, L-FE-007, L-FE-008, L-FE-010 | Diffs limpios                                                         |
| **6 — Barreras**                | Impedir que vuelva a pasar      | C-001, C-002, C-004, C-006, V-001/V-002 (jobs de CI)                 | Warnings-as-errors activo; build reproducible                         |
| **7 — Estructura**              | Deuda residual                  | C-003, C-005, C-007                                                  | Solución completa y consistente                                       |

---

## 10. Checklist

### Fase 0 — Desbloqueo del build (urgente)

- [ ] **E-FE-001** · `src/sw.ts:17` — cambiar `MessageEvent` por `ExtendableMessageEvent`
- [ ] **E-FE-002** · `src/sw.ts:71` — crear `src/sw-types.d.ts` con la ampliación de `NotificationOptions.actions` e incluirlo en `tsconfig.worker.json`
- [ ] **E-FE-002b** · Verificar manualmente en Chrome Android que los botones "Sí, ya está en casa" / "No, sigue perdido" aparecen y navegan correctamente
- [ ] **E-FE-003** · `vite.config.ts` — importar `defineConfig` desde `vitest/config`
- [ ] **L-FE-001a** · `ReportLostPage.tsx:179-180` — mover `useState(step)` y `useState(direction)` antes de los early returns
- [ ] **L-FE-001b** · `LostReportConfirmationPage.tsx:271` — mover `useRecoveryRates` al bloque de hooks + `enabled`
- [ ] **L-FE-001c** · Prueba manual de ambas páginas con Slow 3G (transición loading→loaded)
- [ ] Verificar: `cd frontend && npm run typecheck` termina con exit 0

### Fase 1 — Seguridad y correctitud

- [ ] **E-BE-008** · Añadir `[Theory]` a `SendChatMessageTests.cs:176` (`ChatContactGuardTests`)
- [ ] **E-BE-008b** · Ejecutar la suite; **documentar aquí como ítems nuevos** los fallos que aparezcan de los 3 `[InlineData]`
- [ ] **E-BE-008c** · Añadir `<WarningsAsErrors>$(WarningsAsErrors);xUnit1013;xUnit1008;xUnit1026;xUnit2013</WarningsAsErrors>` a `PawTrack.UnitTests.csproj`
- [ ] **L-FE-002** · Corregir los 40 `no-misused-promises` (empezar por `AdminPage.tsx`, `SearchCoordinationPage.tsx`, `AuthenticatedLayout.tsx`)
- [ ] **L-FE-003a** · Corregir los 3 `no-floating-promises` de `src/features/auth/hooks/useAuth.ts` ⚠️ prioridad
- [ ] **L-FE-003b** · Corregir los 28 `no-floating-promises` restantes
- [ ] **L-FE-009** · Inspeccionar y corregir `AdminPage.tsx:221`, `AdminPage.tsx:284`, `MunicipalDashboardPage.tsx:105`
- [ ] **L-FE-014** · `apiClient.ts:83` — rechazar siempre con una instancia de `Error`
- [ ] **V-001** · `npm audit fix` + verificación completa (typecheck, unit, build, e2e)
- [ ] **V-001b** · Confirmar `vitest >= 3.2.6`, `vite > 6.4.2`, `ws` parcheado, `serialize-javascript > 7.0.4`

### Fase 2 — Nulabilidad backend

- [ ] **E-BE-003a** · Anotar `Result<T>` con `[MemberNotNullWhen(true, nameof(Value))]` / `[MemberNotNullWhen(false, ...)]`
- [ ] **E-BE-003b** · Recompilar y catalogar las advertencias **nuevas** que exponga (son bugs latentes)
- [ ] **E-BE-003c** · Verificar 0 `CS8602` en `SubscriptionPlansController.cs:45`, `CollarTagAdminController.cs:54`, `PublicMapController.cs:111`
- [ ] **E-BE-001** · Declarar `petVector` como `float[]?` en `MatchSightingPhotoQuery.cs` y `MatchSightingByIdQuery.cs`
- [ ] **E-BE-002** · `SendChatMessageCommand.cs:109` — guard de `IsNullOrWhiteSpace` + `Result.Failure`
- [ ] **E-BE-002b** · Test unitario: cuerpo compuesto sólo por un teléfono ⇒ `Result.Failure`
- [ ] **E-BE-009** · Corregir los 13 `CS8602`/`CS8604` restantes en los proyectos de test

### Fase 3 — Accesibilidad (WCAG 2.1 A)

- [ ] **L-FE-004a** · `ReportFoundPetPage.tsx` (6) — formulario público
- [ ] **L-FE-004b** · `StoreRegistrationPage.tsx` (7)
- [ ] **L-FE-004c** · `ServiceProviderProfilePage.tsx` (6) + `ServiceProviderRegistrationPage.tsx`
- [ ] **L-FE-004d** · `ProviderServicesPage.tsx` (13)
- [ ] **L-FE-004e** · `MedicalHistoryTab.tsx` (10)
- [ ] **L-FE-004f** · `ClinicExpedienteTab.tsx` (6)
- [ ] **L-FE-004g** · `ShelterPublishPage.tsx` (6) + `BundleOrderModal.tsx` (6)
- [ ] **L-FE-004h** · `AdminPromotionManager.tsx` (9) + `AdminBillboardsTab.tsx` (8) + `MunicipalDashboardPage.tsx` (7)
- [ ] **L-FE-004i** · `StoreLocationsPage.tsx` (5) + resto de archivos
- [ ] **L-FE-004j** · Verificar que las suites que usan `getByLabelText` siguen pasando
- [ ] **L-FE-011a** · Quitar `autoFocus` de `LoginPage.tsx:447,603`, `ProfilePage.tsx:548`, `CreatePetPage.tsx:215`
- [ ] **L-FE-011b** · Quitar `role="list"` redundante en `SearchChecklist.tsx:195`, `NotificationCenter.tsx:204`
- [ ] **L-FE-011c** · Convertir `<div onClick>` en `<button>` en `NotificationCenter.tsx:228,232`
- [ ] **L-FE-011d** · Ídem en `VisualMatchPanel.tsx:196`

### Fase 4 — Tipado y estado

- [ ] **L-FE-005a** · `useSearchCoordinationHub.ts:71` (SignalR) — clasificar y corregir ⚠️ riesgo de fuga de conexión
- [ ] **L-FE-005b** · `MapContainer.tsx:81,101,129` (Leaflet) ⚠️ riesgo de instancias duplicadas
- [ ] **L-FE-005c** · `useAuthInit.ts:40`, `ProfilePage.tsx:436`, `AcceptFamilyInvitationPage.tsx:46`
- [ ] **L-FE-005d** · `LostReportConfirmationPage.tsx:104,189`, `ReportLostPage.tsx:64`
- [ ] **L-FE-005e** · `useMovementPrediction.ts:42` (×2), `QRCodeDisplay.tsx:29`, `ReportFoundPetPage.tsx:66`
- [ ] **L-FE-005f** · `ChatPanel.tsx:171`, `ReuniteButton.tsx:81`, `NotificationCenter.tsx:98`
- [ ] **L-FE-006a** · `apiClient.ts` (10 hallazgos) — tipar el interceptor con `AxiosError<ApiProblemDetails>`
- [ ] **L-FE-006b** · `src/sw.ts` (2) — declarar `__WB_MANIFEST` en `sw-types.d.ts`
- [ ] **L-FE-006c** · `CantonChoroplethMap.tsx` (4), `authApi.ts` (2), `WeightTrendChart.tsx` (1)
- [ ] **L-FE-006d** · `promotionApi.ts:59` y `serviceProvidersApi.ts:254` — genéricos explícitos en `apiClient.get<T>()`
- [ ] **L-FE-006e** · Resto de `no-unsafe-assignment` en `src/features/stores/**` y `advertising`/`medical`
- [ ] **L-FE-012** · `useActivity.ts:12`, `useMedical.ts:27,48`
- [ ] **L-FE-013** · `useAlertPreference.ts:85`, `AllyPanelPage.test.tsx:63`
- [ ] **L-FE-015** · `ActivityTab.tsx:211`
- [ ] **L-FE-016** · `petsApi.ts:66`

### Fase 5 — Higiene

- [ ] **E-BE-004a** · `GetChatMessagesQuery.cs:35` — eliminar `unitOfWork` (query CQRS de solo lectura)
- [ ] **E-BE-004b** · `ClinicAccessGrantCommands.cs:85` — **revisar** si falta validación de usuario antes de eliminar `userRepository`
- [ ] **E-BE-004c** · `WidgetController.cs:18` — eliminar `sender` si no hay endpoints MediatR pendientes
- [ ] **E-BE-004d** · `AdoptionsController.cs:15` — **verificar** que la subida de fotos usa Blob Storage antes de eliminar `blobStorage`
- [ ] **E-BE-005** · Eliminar los 7 `using` duplicados (`InfrastructureServiceCollectionExtensions.cs` ×3, `PawTrackDbContext.cs` ×3, `WebhooksController.cs`, `Program.cs`)
- [ ] **E-BE-006** · `QuestPdfCertificateService.cs:190` — decidir si falta el badge verde o eliminar la constante
- [ ] **E-BE-007** · `QuestPdfIdCardService.cs:50` — migrar a la sobrecarga `ImageDescriptor` de QuestPDF
- [ ] **E-BE-007b** · Verificar visualmente el PDF del carnet tras el cambio
- [ ] **L-FE-007** · Ejecutar `npx eslint . --fix` para los 24 `no-unnecessary-type-assertion` + `npm run typecheck`
- [ ] **L-FE-008** · Eliminar/prefijar las 9 variables sin usar
- [ ] **L-FE-010** · Separar exportaciones no-componente en los 6 archivos (o override justificado para `routes.tsx`)

### Fase 6 — Barreras contra regresión

- [ ] **C-001a** · Crear `backend/Directory.Build.props` con analizadores habilitados
- [ ] **C-001b** · Activar `TreatWarningsAsErrors` progresivamente: Domain → Application → Infrastructure → API → tests
- [ ] **C-002** · Crear `.editorconfig` en la raíz
- [ ] **C-004a** · Corregir la versión de QuestPDF a `2025.4.0` en `PawTrack.Infrastructure.csproj`
- [ ] **C-004b** · Adoptar Central Package Management (`Directory.Packages.props`)
- [ ] **C-004c** · Habilitar `RestorePackagesWithLockFile` y commitear los `packages.lock.json`
- [ ] **C-006** · Verificar que la caché de NuGet en CI se invalida con los lock files
- [ ] **V-001c** · Añadir `npm audit --audit-level=high` a `.github/workflows/frontend.yml`
- [ ] **V-002b** · Añadir `dotnet list package --vulnerable` a `.github/workflows/backend.yml`
- [ ] Verificar: `npm run lint` termina con exit 0 y `--max-warnings 0`

### Fase 7 — Estructura

- [ ] **C-003** · Decidir sobre `backend/HashGen`: añadir a `PawTrack.sln` o eliminar del repositorio
- [ ] **C-003b** · Confirmar que `HashGen/bin` y `HashGen/obj` están en `.gitignore`
- [ ] **C-005** · Programar revisión trimestral con `tsc --noEmit --skipLibCheck false`
- [ ] **C-007a** · `LoginPage.test.tsx` — decidir el texto correcto ("Credenciales incorrectas" vs "Correo o contraseña incorrectos.") y alinear
- [ ] **C-007b** · Diagnosticar y corregir `AdminPage.test.tsx`
- [ ] **C-007c** · Corregir el handler de MSW de `ProfilePage.test.tsx` ("saves name via PATCH /auth/me")

### Verificación final

- [ ] `dotnet build PawTrack.sln -m:1` → 0 errores, 0 advertencias
- [ ] `dotnet test PawTrack.sln` → todo verde, **sin tests omitidos silenciosamente**
- [ ] `cd frontend && npm run typecheck` → exit 0 en los 3 proyectos
- [ ] `cd frontend && npm run lint` → exit 0 con `--max-warnings 0`
- [ ] `cd frontend && npm run test -- --run` → todo verde
- [ ] `cd frontend && npm run build` → exit 0
- [ ] `cd frontend && npx playwright test` → todo verde
- [ ] `npm audit --audit-level=high` → sin hallazgos
- [ ] `dotnet list PawTrack.sln package --vulnerable --include-transitive` → sin hallazgos
- [ ] Los 6 workflows de GitHub Actions en verde
