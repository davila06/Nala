# Clasificación de riesgo de agentes

El riesgo es el máximo nivel de los factores relevantes, no un promedio. Subir un nivel cuando haya datos sensibles, usuarios vulnerables, efecto externo, alcance amplio, baja reversibilidad o incertidumbre material. Si falta información de riesgo, no bajar la clasificación: solicitar datos o bloquear.

| Nivel                      | Descripción y ejemplos                                                                                                                                                                                            | Autonomía permitida                                                                                                                                                                 |
| -------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| R0 Informativo             | Búsqueda, resumen o explicación sin mutación ni actuación externa.                                                                                                                                                | Operar sin aprobación previa; citar fuentes, respetar permisos de lectura y declarar incertidumbre.                                                                                 |
| R1 Reversible/bajo impacto | Markdown derivado, enlaces, diagramas, inventario en rutas autorizadas; diff revisable y fácil de revertir.                                                                                                       | Ejecutar dentro del scope permitido; inspección humana del diff antes de integrar.                                                                                                  |
| R2 Significativo           | Código, contratos API, migraciones, permisos, integración, tiers, herramientas/MCP, prompts que cambien comportamiento o ingestión de datos.                                                                      | Preparar plan/diff; aprobación humana explícita antes de aplicar. Validador distinto para cambios de seguridad/datos si es viable.                                                  |
| R3 Crítico/irreversible    | Producción, borrar/migrar datos compartidos, comunicaciones externas, cobros/reembolsos, advice clínico, compartir GPS, acuerdos institucionales, secretos/políticas, acciones por profesionales/municipalidades. | No ejecutar autónomamente. Analizar/proponer/preparar plan; aprobación formal y ejecución por operador autorizado fuera del agente, salvo mecanismo específico aprobado y auditado. |

## Factores

Evaluar: impacto a personas/animales/organizaciones; sensibilidad y volumen de datos; alcance de tenants/ambientes; nivel de privilegio; contacto externo/financiero; efecto clínico/regulatorio; probabilidad/severidad de error; posibilidad y tiempo de reversión; calidad de evidencia; inyección de instrucciones; concentración de deberes.

## Ejemplos NALA

- Resumir arquitectura desde docs públicos internos: R0.
- Corregir enlaces en un archivo documental autorizado: R1.
- Crear nuevo skill que habilita herramientas o cambiar permisos: R2.
- Enviar alerta a usuarios, iniciar cargo, mostrar ubicación precisa o desplegar: R3.
- Asistente de salud: R2 como investigación/prototipo aislado, R3 si produce/entrega recomendación clínica o modifica expediente.

## Registro y cambio de nivel

Usar `assets/RISK_ASSESSMENT_TEMPLATE.md`. Registrar nivel, factores, controles, riesgo residual y aprobador. Reclasificar si cambian datos, autonomía, herramientas, salida, audiencia o ambiente. Ningún R0/R1 permite saltarse authorization/consent ni políticas superiores.

Cuando dos niveles parezcan aplicables, usar el más alto hasta que un owner documente evidencia suficiente para reducirlo.
