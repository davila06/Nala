# Respuesta a incidentes de agentes

## Incidentes cubiertos

Cambio fuera de alcance; secreto/PII/GPS expuestos; conclusión falsa como confirmada; ejecución sin aprobación; daño o corrupción de datos; contacto externo; uso no autorizado de herramienta; recomendación veterinaria impropia; política/skill alterado sin autorización; prompt injection exitoso; acceso cross-tenant; fallo de separación de deberes.

## Secuencia

1. **Detener:** interrumpir el agente/herramienta sin borrar logs/evidencia. No continuar para “arreglar” en caliente.
2. **Aislar:** deshabilitar la tool/credencial/scope afectado o pedirlo al operador con autoridad. R3 siempre lo ejecuta humano autorizado.
3. **Preservar:** guardar timestamp, run ID, versión de skill/prompt/modelo, fuentes, rutas/diff, herramientas y aprobación. Redactar secretos/PII y restringir acceso.
4. **Alcance:** identificar acciones, sistemas, tenants, datos, destinatarios e impacto observado/no verificado. No inferir exposición a partir de logs incompletos.
5. **Escalar:** owner técnico + seguridad; agregar privacidad/legal, veterinaria, finanzas, producto o institución conforme a datos/impacto. Seguir los canales aprobados de incidentes; no enviar notificaciones externas desde el agente.
6. **Contener/recuperar:** revocar credenciales o accesos por owner; restaurar desde fuente confiable. Revertir solo cuando sea seguro, aprobado y no destruya evidencia.
7. **Comunicar:** solo por responsables autorizados. Documentar audiencia, fundamento y aprobación; no divulgar datos innecesarios.
8. **Aprender:** análisis de causa, brecha de control, acciones con owner/fecha, tests de regresión, decisión de reapertura y riesgo residual.

## Registro

Usar `assets/GOVERNANCE_REVIEW_TEMPLATE.md` como anexo y conservar registro de incidente con ID, severidad, impacto confirmado/no verificado, preservación, owners, decisiones, aprobaciones y acciones. No incluir valores de secretos ni payloads personales completos.

## Umbral de escalamiento

Escalar inmediatamente ante posible fuga de secreto/PII/GPS, acceso a salud, cross-tenant, cobro/comunicación, acción productiva o consejo clínico. Si no se conoce el impacto, clasificarlo como desconocido, mantener controles de contención y no cerrar por falta de evidencia.

El cierre requiere responsable, evidencia de contención, causa/acción correctiva y decisión explícita sobre el riesgo residual.
