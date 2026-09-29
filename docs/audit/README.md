# Auditoría de producto y runtime

**Corte inicial:** 2026-09-29; actualización de implementación: 2026-09-29. La auditoría inicial fue estática. En esta actualización se ejecutaron build y pruebas focalizadas, pero no se desplegó, no se consultó producción/Azure ni se ejecutaron proveedores reales. Los valores sensibles se omitieron. `UNKNOWN` y `NO_VERIFICADO` son límites de evidencia.

## Entregables

- [REPOSITORY_RUNTIME_INVENTORY.md](REPOSITORY_RUNTIME_INVENTORY.md): productos, librerías, tests, frontend, IaC y emuladores.
- [PRODUCTION_REACHABILITY_MAP.md](PRODUCTION_REACHABILITY_MAP.md): DI, flags, fallbacks y rutas alcanzables por runtime.
- [IMPLEMENTATION_MARKERS.md](IMPLEMENTATION_MARKERS.md): stubs, no-op, simulaciones y marcadores con contexto.
- [TEST_DOUBLE_INVENTORY.md](TEST_DOUBLE_INVENTORY.md): mocks/stubs/fakes y límites de lo que prueban.
- [HARDCODED_DEFAULTS.md](HARDCODED_DEFAULTS.md): placeholders, defaults y hardcodes con impacto.
- [ENVIRONMENT_CONFIGURATION_MATRIX.md](ENVIRONMENT_CONFIGURATION_MATRIX.md): fuentes declaradas por ambiente y configuración no verificada.
- [BACKEND_EXECUTION_TRACE.md](BACKEND_EXECUTION_TRACE.md): recorridos endpoint -> handler -> persistencia/side effect.

## Hallazgos P0/P1

1. La implementación inicial de tarjeta fue reemplazada en el worktree por CyberSource hosted fields, gateway fail-closed, `PaymentIntent`, idempotencia y fulfillment post-settlement. Sandbox, 3-D Secure, contrato comercial, credenciales y operación productiva siguen `NO_VERIFICADO`; `MUST_FIX_BEFORE_BETA` hasta validación autorizada.
2. API/IaC/CI no comparten un contrato compatible para `/health` y `/health/ready`: las rutas exigen Admin JWT y los probes no lo envían. Estado `PARTIALLY_IMPLEMENTED`; `MUST_FIX_BEFORE_PRODUCTION`.
3. ACA API/job conserva imagen de muestra en Bicep. Estado `PLACEHOLDER`; `MUST_FIX_BEFORE_PRODUCTION`.
4. El gateway regulatorio es `NoOp` y deja `Prepared`, mientras algunas acciones institucionales no consultan la flag de reportes. Estado `STUB_ONLY` / `PARTIALLY_IMPLEMENTED`; `MUST_FIX_BEFORE_BETA` para el gate y `LEGITIMATE_DEFERRAL` solo si el envío manual se declara.
5. Faltan pruebas de entorno real para SQL/Blob/RBAC/JWT, multi-réplica, proveedores, 3-D Secure, autorización cross-tenant, conciliación y delivery. Su operación es `NO_VERIFICADO`.

## Método y límites

Se siguieron registros DI, controllers, handlers, configuración, workflows, IaC y tests cercanos al flujo. La clasificación separa implementación, configuración, pruebas y operación. Un test con doble no demuestra integración real; una clase, ruta o recurso declarado no demuestra despliegue. La cobertura funcional de todos los módulos, resultados CI y estado live quedan pendientes de una ronda autorizada de ejecución.
