# Dominio: mascotas

**Estado:** capacidades técnicas con estados distintos por operación; ver F02 en la [matriz](../../docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md).

La solución registra mascotas, relación con responsable, perfil y acceso al QR desde [PetsController](../../backend/src/PawTrack.API/Controllers/PetsController.cs) y modelos del dominio [Pets](../../backend/src/PawTrack.Domain/Pets/). El alta/consulta API está `IMPLEMENTADO_SIN_PRUEBAS` end-to-end en el corte citado; Blob real no está acreditado.

**Actores:** responsable, finder público y operadores según autorización. **Relaciones:** responsable- mascota, identidad/QR, casos de pérdida, salud y collar. No inferir que toda relación o límite comercial está disponible por aparecer en el modelo. Referencias de negocio: [PRODUCT_SCOPE](../../docs/PRODUCT_SCOPE.md), [diccionario de datos](../../docs/DICCIONARIO_DATOS.md).
