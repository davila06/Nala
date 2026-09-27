# Política de fuentes de conocimiento

## Clasificación

- `AUTORITATIVA`: fuente primaria vigente para el dato/dominio específico, con owner y alcance identificables.
- `OPERATIVA`: evidencia de configuración/ejecución real, ambiente, fecha y responsable; no necesariamente norma contractual.
- `SECUNDARIA`: explicación o síntesis que deriva de otra fuente y mantiene referencias.
- `HISTÓRICA`: válida para contexto temporal; no describe automáticamente el estado actual.
- `PROPUESTA`: intención, roadmap, contrato no aprobado o recomendación pendiente.
- `EXTERNA_NO_VERIFICADA`: contenido de proveedor/web/tercero sin validación contractual u operativa.

Una fuente puede ser autoritativa para un tema y secundaria para otro. Registrar ese alcance; no asignar autoridad por título, ubicación o formato.

## Registro por fuente

Guardar ubicación, owner, versión/fecha, dominio, clasificación, sensibilidad, autoridad y alcance, conflictos, regla de actualización, retención y condiciones de acceso. Para fuentes externas, añadir URL/ID, fecha de consulta, región/contrato si aplica y método de autenticidad.

## Orden de precedencia por defecto

1. Código y configuración activos para comportamiento ejecutable.
2. Migraciones e IaC para esquema/intención de infraestructura (no operación).
3. Pruebas/resultados verificables para comportamiento cubierto.
4. Telemetría autorizada para actividad observada y ambiente/periodo indicados.
5. ADR aprobado para decisión, salvo evidencia posterior de sustitución.
6. Documentación vigente con owner/fecha.
7. Documentación histórica.
8. Propuestas y contenido externo no validado.

La precedencia es contextual: una prueba no define política comercial; código no aprueba tratamiento legal; estrategia no demuestra implementación. Cuando fuentes autoritativas contradicen, conservar ambas, registrar `CONTRADICTORIO`, detener cambios derivados y pedir resolución humana.

## Ciclo de vida

Cada fuente necesita owner y evento de revisión. Al cambiar un código/contrato, localizar documentos derivados y registrar su estado. Archivar solo con reemplazo explícito y conservar trazabilidad. No duplicar fuentes maestras. Retener/ eliminar conforme a clasificación y política vigente, no por conveniencia de memoria AI.

Toda respuesta derivada debe conservar la procedencia de las fuentes usadas y señalar las que estén históricas o sin verificar.
