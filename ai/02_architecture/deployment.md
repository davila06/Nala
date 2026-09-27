# Despliegue observado

La infraestructura como código está bajo [infra/](../../infra/) y define recursos mediante [main.bicep](../../infra/main.bicep), módulos y parámetros de beta/producción. El backend incluye [Dockerfile](../../backend/Dockerfile); workflows de CI/CD están bajo [.github/workflows/](../../.github/workflows/). La topología y pasos operativos vigentes se describen en [deployment architecture](../../docs/architecture/DEPLOYMENT.md) y [runbook](../../docs/RUNBOOK_DEPLOYMENT.md).

**Límite de evidencia:** estos archivos prueban artefactos declarativos versionados, no existencia/estado de Azure resources, despliegue vigente, dominios, secretos, SLO, backups probados o acceso de agentes a producción. No se ejecutaron `az`, `azd`, Bicep deploy ni workflow como parte de esta tarea.

Antes de habilitar un agente en cualquier entorno, definir identidad separada, secreto gestionado, red/egress, límites de datos, telemetría no sensible, apagado, versionado de prompts/modelos y aprobación por ambiente. El deployment de agente permanece `PROPUESTO`.
