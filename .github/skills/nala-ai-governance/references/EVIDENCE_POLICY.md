# Política de evidencia

## Evidencia aceptable

Según la afirmación, pueden respaldarla: código ejecutable y referencias de llamada; configuración activa; migración/esquema; contrato API implementado; pruebas automatizadas ejecutadas con comando/resultado; build o ejecución reproducible; IaC (solo intención/configuración); telemetría autorizada y fechada; ADR aprobado; documento contractual/institucional vigente y accesible; evidencia operacional redactada y fechada.

Una prueba verifica solo el comportamiento/alcance cubierto. DI, existencia de tabla o endpoint no prueba uso, despliegue ni disponibilidad externa. Infraestructura como código no prueba recurso aprovisionado. Un ADR solo tiene fuerza decisoria si su estado/aprobación/owner están documentados.

## Evidencia insuficiente por sí sola

Nombres de archivos/carpetas; interfaces sin implementación; mocks/stubs; TODO/comentarios; prototipos/menús; código sin referencia; ejemplos; documentación antigua; roadmaps/propuestas; branch names; respuestas de otra IA; paquetes instalados; configuración no activada; screenshot sin origen/corte; prueba no ejecutada; métricas sin denominador/telemetría autorizada.

## Clasificación de conclusión

- `CONFIRMADO`: evidencia primaria vigente demuestra exactamente la afirmación y alcance.
- `PARCIALMENTE_CONFIRMADO`: solo parte, capa o escenario está probado; nombrar el límite.
- `INFERIDO`: deducción razonable pero evidencia indirecta; no presentarla como hecho.
- `PROPUESTO`: idea/roadmap explícito sin implementación/aprobación que lo haga actual.
- `CONTRADICTORIO`: fuentes plausibles vigentes discrepan; conservar ambas atribuciones y detener cambios derivados.
- `OBSOLETO`: evidencia fechada quedó sustituida; enlazar fuente que la reemplaza.
- `NO_VERIFICADO`: no hay evidencia suficiente o no fue posible comprobarla.

Estos estados califican evidencia/conclusiones de gobernanza. No sustituyen los estados funcionales de la matriz de NALA (`IMPLEMENTADO_Y_VERIFICADO`, `PARCIALMENTE_IMPLEMENTADO`, etc.).

## Registro mínimo

Por afirmación: ID; texto atómico; clasificación; ruta/versión/fecha; evidencia observada; prueba y resultado si existe; confianza cualitativa; contradicciones; sensibilidad; owner y fecha de revisión. No usar una única cita para respaldar varias afirmaciones no relacionadas.

## Citas y auditoría

Preferir rutas relativas exactas y símbolos/rangos mínimos. Anotar commit/corte cuando puede cambiar. Para telemetría o evidencia externa, citar ID redactado, fecha, ambiente y responsable, no copiar PII/secretos. Si no puede citarse sin exponer datos, registrar la existencia y owner de la evidencia bajo acceso autorizado.

Al comunicar resultados, separar la observación de la interpretación y mantener cada cita junto a la afirmación que respalda.
