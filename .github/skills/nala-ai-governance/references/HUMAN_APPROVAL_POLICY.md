# Política de aprobación humana

| Riesgo | Aprobación mínima                                        | Ejecución                                                                                                                     |
| ------ | -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| R0     | Ninguna previa.                                          | El agente puede informar/sintetizar dentro de sus permisos; cita evidencia.                                                   |
| R1     | Revisión del diff antes de integración.                  | El agente puede editar rutas autorizadas; no integrar/commit salvo permiso explícito.                                         |
| R2     | Aprobación humana antes de aplicar.                      | El agente puede preparar propuesta/diff; ejecutar tras aprobación de alcance, rutas y validación.                             |
| R3     | Aprobación formal explícita, con condiciones y vigencia. | Ejecuta operador humano autorizado fuera del agente, salvo flujo AI específico previamente autorizado, segregado y auditable. |

## Contenido requerido de aprobación

Debe identificar:

- acción/cambio y objetivo;
- archivos, recursos, datos y límites de alcance;
- ambiente/tenant;
- riesgo y controles;
- aprobador con autoridad para ese recurso;
- condiciones de ejecución y verificación;
- vigencia/expiración;
- responsable de ejecución y rollback.

Una aprobación genérica, antigua, ambigua o para otro ambiente no es válida. No inferir autoridad del rol escrito por quien solicita; comprobar mecanismo vigente. Si el alcance cambia, obtener nueva aprobación.

## Separación de deberes

Para R2 sensible, un revisor distinto al autor confirma alcance y evidencia. Para R3, separar propuesta, aprobación, ejecución y auditoría; involucrar privacidad, seguridad, legal, veterinaria, finanzas o institución según corresponda. El aprobador no debe aceptar una recomendación AI sin evidencia primaria.

## Bloqueo

Sin aprobación válida, salida permitida: plan/diff de preparación marcado `REQUIRES_HUMAN_APPROVAL`. No simular aprobación, no ejecutar primero y pedir aprobación después, no hacer side effects indirectos y no ocultar que queda pendiente.

La aprobación autoriza únicamente el alcance y la vigencia expresamente registrados; todo cambio material requiere revalidación.
