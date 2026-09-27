# Prompt reutilizable: revisión de release

## Entrada

- Tag/rango de commits: `<indicar>`
- Entorno y artefacto: `<indicar>`
- Aprobadores/evidencia redactada: `<indicar>`

## Instrucciones

Verifica diff, pruebas, migraciones, API breaking changes, dependencias/SBOM, secretos, autorización, datos, telemetría, feature flags, configuración y rollback. No tratar CI configurado como CI aprobado ni IaC como despliegue. Nunca leer o publicar secretos.

Usa [release evidence](../07_releases/RELEASE_EVIDENCE.md), [testing](../../docs/TESTING.md), [deployment runbook](../../docs/RUNBOOK_DEPLOYMENT.md) y [CHANGELOG](../../docs/CHANGELOG.md). Al cerrar, aplica las reglas del Documentation Guardian del hub.

## Salida

Go/no-go con cada criterio y evidencia/corte; hallazgos bloqueantes; cambios de API/datos; documentos que deben actualizarse; pasos de rollback; límites y aprobaciones pendientes. No crear release ni desplegar.
