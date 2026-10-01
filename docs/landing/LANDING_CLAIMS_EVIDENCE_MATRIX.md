# Matriz de claims y evidencia

| Afirmación                             | Fuente                                        | Evidencia                            | Estado            | Permitida     | Redacción recomendada                                        |
| -------------------------------------- | --------------------------------------------- | ------------------------------------ | ----------------- | ------------- | ------------------------------------------------------------ |
| NALA organiza identidad y recuperación | `PRODUCT_SCOPE.md`, controllers Pets/LostPets | Rutas, commands y pruebas            | VERIFIED_IN_CODE  | Sí            | “Herramientas para identificar y organizar una recuperación” |
| QR abre un perfil                      | `PRODUCT_SCOPE.md`, QrCodeService             | QR y perfil público                  | VERIFIED_IN_CODE  | Sí            | “Un QR puede abrir el perfil configurado”                    |
| NFC rastrea ubicación                  | código/docs                                   | No existe                            | FALSE             | No            | “NFC no es GPS; configuración manual parcial”                |
| Video veterinario                      | `PRODUCT_SCOPE.md`, ClinicsController         | Sin ACS/video                        | NOT_IMPLEMENTED   | No            | “Telemedicina audiovisual no disponible”                     |
| Matching IA                            | AzureVisionEmbeddingService                   | Embeddings condicionados             | PARTIAL           | Sí con límite | “Matching visual condicionado por configuración”             |
| Red de aliados activa                  | `CLAIM_EVIDENCE_MATRIX.md`                    | No hay cobertura operativa           | REQUIRES_APPROVAL | No como claim | “Capacidades para aliados; operación externa no verificada”  |
| Precio disponible                      | `PRICING_AND_PLANS.md`                        | Gate comercial pendiente             | BLOCKED           | No            | “Catálogo sujeto a aprobación”                               |
| Recuperación garantizada               | claim matrix                                  | No hay métricas operativas aprobadas | BLOCKED           | No            | “Puede ayudar a coordinar información”                       |
| Producción enterprise                  | claim matrix                                  | No hay evidencia de operación        | BLOCKED           | No            | No publicar                                                  |
