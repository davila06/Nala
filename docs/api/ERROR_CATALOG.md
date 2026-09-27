# Errores observados (muestra)

| Respuesta | Caso respaldado                                                                                                                                                                                                        |
| --------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 401/403   | JWT ausente o ownership fallido en [LostPetsController](../../backend/src/PawTrack.API/Controllers/LostPetsController.cs).                                                                                             |
| 404       | Mascota no encontrada o contacto de caso no activo en [LostPetsController](../../backend/src/PawTrack.API/Controllers/LostPetsController.cs).                                                                          |
| 422       | Validación o transición inválida en [LostPetsController](../../backend/src/PawTrack.API/Controllers/LostPetsController.cs) y [SightingsController](../../backend/src/PawTrack.API/Controllers/SightingsController.cs). |
| 409       | Límite comercial en [ExceptionHandlingMiddleware](../../backend/src/PawTrack.API/Middleware/ExceptionHandlingMiddleware.cs); no todos los handlers usan el mismo camino.                                               |
| 429       | Limitador de avistamientos en [SightingsController](../../backend/src/PawTrack.API/Controllers/SightingsController.cs) y configuración en [Program](../../backend/src/PawTrack.API/Program.cs).                        |
| 500       | Error no controlado sin mensaje interno en [ExceptionHandlingMiddleware](../../backend/src/PawTrack.API/Middleware/ExceptionHandlingMiddleware.cs).                                                                    |

No hay garantía comprobada de un contrato uniforme de error para los 68 controllers; [SubscriptionsController](../../backend/src/PawTrack.API/Controllers/SubscriptionsController.cs) devuelve también arrays/`BadRequest` en algunas rutas. Ejemplos de errores y versionado de [API_REFERENCE](../API_REFERENCE.md) deben verificarse por endpoint antes de publicarse.
