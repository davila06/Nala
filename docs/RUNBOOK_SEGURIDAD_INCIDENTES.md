# Runbook de Seguridad e Incidentes

**Estado:** activo  
**Audiencia:** Admin, Support, DevOps  
**Corte:** 2026-09-09

## 1. Clasificacion inicial

| Nivel | Ejemplos                                                    | Accion inicial                                              |
| ----- | ----------------------------------------------------------- | ----------------------------------------------------------- |
| P0    | secreto expuesto, acceso masivo, SQL indisponible           | contener, escalar a Admin/DevOps y preservar evidencia      |
| P1    | BOLA/IDOR, API key comprometida, abuso de chat              | bloquear actor o clave, abrir incidente y revisar auditoria |
| P2    | fraude aislado, proveedor abusivo, fallo funcional sensible | limitar alcance y resolver con Support                      |
| P3    | error visual o documental sin datos expuestos               | registrar y priorizar backlog                               |

## 2. Primeros 15 minutos

1. Registra hora, tenant, usuario, endpoint, correlation ID y evidencia minima.
2. No copies contrasenas, tokens, API keys, PII o documentos medicos al ticket.
3. Si hay credencial expuesta, revocala o rotala inmediatamente.
4. Si hay abuso activo, aplica bloqueo de cuenta, clave o canal sin borrar logs.
5. Determina si el incidente afecta una persona, organizacion o todos los tenants.

## 3. Playbooks

### Secreto o API key expuesta

- Revocar la clave y generar reemplazo.
- Rotar el secreto en Key Vault y reiniciar el workload afectado.
- Revisar logs de uso desde la ultima rotacion.
- Eliminar el secreto del repositorio y abrir seguimiento de historial Git.

### BOLA/IDOR o acceso indebido

- Capturar request, actor, recurso y respuesta sin incluir PII innecesaria.
- Reproducir con un usuario de prueba, nunca con cuentas reales.
- Verificar ownership, tenant, grant y auditoria.
- Añadir prueba de regresion antes de cerrar.

### Fraude o abuso de chat

- Preservar thread y mensajes mediante el flujo de auditoria.
- No contactar externamente a las partes desde una cuenta de soporte.
- Escalar riesgo fisico o amenaza a autoridades competentes.
- Cerrar con razon y referencia del caso.

## 4. Notificacion y cierre

`Support` opera bienestar e incidentes de proveedores. `Admin` decide bloqueos
globales, roles, planes y accesos. DevOps decide rotaciones, despliegues y
restauraciones. El cierre requiere causa raiz, impacto, acciones y prueba de
regresion.

Consultar [API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md),
[RUNBOOK_OPERACIONES.md](RUNBOOK_OPERACIONES.md) y
[CUMPLIMIENTO_PROTECCION_DATOS.md](CUMPLIMIENTO_PROTECCION_DATOS.md).
