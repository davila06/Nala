# Límites y propiedad de agentes/skills

## Regla

Un skill especializado gobierna sus resultados y documentos declarados. `nala-ai-governance` gobierna el proceso común: riesgo, evidencia, permisos, aprobación, instrucciones no confiables, excepciones e incidentes. El meta-skill no es propietario del contenido funcional y no expande alcance de escritura.

## Owners y coordinación NALA

| Skill                      | Propiedad que conserva                                                 |
| -------------------------- | ---------------------------------------------------------------------- |
| `nala-product-auditor`     | Inventario integral, alcance, matriz consolidada, brechas y changelog. |
| `nala-feature-inventory`   | Estado funcional y trazabilidad de features.                           |
| `nala-architecture-review` | Arquitectura, componentes, dependencias y deployment.                  |
| `nala-security-audit`      | Controles técnicos, findings de seguridad y mapas de datos.            |
| `nala-api-documenter`      | Contratos, catálogo, autorización y errores API.                       |
| `nala-subscription-plans`  | Planes, entitlements, gates comerciales.                               |
| `nala-marketplace-audit`   | Marketplace, proveedores, adopción, reservas y pagos asociados.        |
| `nala-gps-platform-review` | GPS, collares, IoT y proveedores de dispositivo.                       |
| `nala-health-platform`     | Salud animal, registros y recordatorios.                               |
| `nala-telemedicine-review` | Telemedicina y controles profesionales relacionados.                   |
| `nala-municipal-edition`   | Capacidad institucional/municipal y pilotos documentados.              |
| `nala-growth-engine`       | Growth, campañas, referidos y atribución.                              |
| `nala-ai-readiness`        | Evaluación técnica de casos AI, datos, modelos y roadmap AI.           |
| `nala-release-manager`     | Evidencia/checklist/notas de releases según su scope.                  |
| `nala-business-analyst`    | Traducción de capacidad verificada a procesos/valor de negocio.        |

El registro de agentes debe enlazar al `SKILL.md` dueño y declarar permisos propios. La tabla no acredita que un agente sea invocable ni que se haya ejecutado.

## Procedimiento de conflicto

1. Identificar recurso, afirmaciones, versiones y owners.
2. Congelar edición derivada y preservar contenido existente.
3. Comparar evidencia primaria, scope declarado y versiones vigentes.
4. Registrar cada posición y la contradicción; no resolver por precedencia de nombre solamente.
5. Solicitar decisión a owners funcional/técnico/seguridad según efecto y clasificación de riesgo.
6. Aplicar solo el cambio aprobado, revisar diff y documentar resolución.

## No permitido

- Sobrescribir secciones ajenas o asumir ownership por enlazar un archivo.
- Alterar los estados de `nala-feature-inventory` para hacerlos coincidir con estados de evidencia AI.
- Encargar a un skill especializado tareas fuera de sus restricciones.
- Cambiar precios, contratos, arquitectura, roadmap, permisos o políticas de seguridad sin owner/aprobación.
- Tratar el meta-skill como aprobador legal/clínico/comercial o como ejecutor de producción.

Registrar toda resolución de ownership con responsables y referencias; la decisión no amplía permisos técnicos automáticamente.
