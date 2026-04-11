// PawTrack CR — Bicep parameter file for production deployment
// Usage: az deployment group create --template-file infra/main.bicep --parameters infra/parameters.prod.bicepparam

using './main.bicep'

param environment = 'prod'
param appName = 'pawtrack'
param location = 'eastus'

// frontendUrl: replace con el dominio real después del primer deploy (o usa el output staticWebAppUrl)
// Ejecuta: az deployment group show --name main --resource-group pawtrack-prod-rg --query properties.outputs.staticWebAppUrl.value -o tsv
// para obtener el dominio auto-generado. Luego vuelve a correr az deployment group create con el valor real.
param frontendUrl = 'https://pawtrack.azurestaticapps.net'

// alertEmailAddress: Set to your ops/on-call email
param alertEmailAddress = 'ops@pawtrack.cr'

// sqlAdminPassword: Provide securely — never commit plain value
// Use: --parameters sqlAdminPassword="$(az keyvault secret show ...)"
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
