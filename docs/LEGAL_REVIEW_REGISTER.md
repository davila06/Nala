# Registro de Revisión Legal — PawTrack CR

> Corte técnico: 23 de septiembre de 2026. Este registro no es una opinión
> legal ni certifica cumplimiento. Impide publicar un borrador como contrato
> final sin revisión profesional en Costa Rica.

## Estado de las superficies

| Superficie          | Documento                                                                            | Estado             | Bloqueador                                                                  |
| ------------------- | ------------------------------------------------------------------------------------ | ------------------ | --------------------------------------------------------------------------- |
| Uso general         | [TERMINOS_DE_USO.md](TERMINOS_DE_USO.md)                                             | `draft`            | Identidad jurídica, aceptación/versionado y aprobación local                |
| Datos personales    | [POLITICA_DE_PRIVACIDAD.md](POLITICA_DE_PRIVACIDAD.md)                               | `draft`            | Responsable formal, encargados/DPA y revisión Ley 8968                      |
| Tiendas/marketplace | [legal.md](legal.md)                                                                 | `draft`            | Consumidor, fiscalidad, cancelaciones, pagos y responsabilidad              |
| Castración          | [castracion-enterprise-todolist.md](castracion-enterprise-todolist.md)               | `pending`          | Consentimiento informado, cancelación, no-show y revisión veterinaria/legal |
| Proveedores B2B     | [SERVICE_PROVIDERS_ENTERPRISE_TODOLIST.md](SERVICE_PROVIDERS_ENTERPRISE_TODOLIST.md) | `pending`          | Pagos, reembolsos, SLA, firma y disputas                                    |
| Proveedores cloud   | [EXTERNAL_PROVIDER_VALIDATION.md](EXTERNAL_PROVIDER_VALIDATION.md)                   | `blocked-external` | Contratos, DPA, regiones, cuentas y evidencia de staging                    |
| SENASA-ready        | [senasa.md](senasa.md)                                                               | `draft`            | No es certificación ni integración oficial; revisión de claims pendiente    |
| Publicidad          | [VALLAS_COMERCIALES.md](VALLAS_COMERCIALES.md)                                       | `draft`            | Orden de compra, claims, cancelación, creativos y responsabilidad           |

## Hallazgos corregidos

- Términos y Privacidad fueron actualizados al 23 de septiembre de 2026 y
  marcados como `draft` hasta aprobación.
- Se eliminó la apariencia de que `PawTrack CR` es una entidad jurídica ya
  identificada; razón social, cédula, representante y domicilio siguen siendo
  obligatorios antes del go-live.
- La Política de Privacidad ahora refleja que Application Insights y los
  eventos de producto están condicionados a consentimiento explícito.
- Se mantiene consentimiento diferenciado para salud y confirmación de
  mayoría de edad/autorización de tutor, sin afirmar verificación formal.
- Los claims `SENASA-ready` siguen separados de aprobación, certificación o
  integración oficial.

## Aprobaciones requeridas

1. Abogado costarricense: términos, privacidad, consumidores, menores, Ley
   8968, transferencias internacionales y derechos de titulares.
2. Fiscal/contable: IVA, facturación, SINPE, marketplace, proveedores y
   publicidad.
3. Profesional veterinario: consentimiento informado, salud animal,
   adopciones, castración y límites de certificados.
4. Operaciones/Seguridad: DPA, Key Vault, retención, incidentes,
   subencargados y continuidad.
5. Producto: versión efectiva, superficies que muestran cada claim y
   aceptación renovada para cambios materiales.

## Regla de publicación

No publicar como “vigente”, “aceptado” o “contrato” ningún documento con estado
`draft`, `pending` o `blocked-external`. Cada aprobación debe registrar
responsable, fecha, versión aprobada, alcance geográfico y evidencia adjunta.
