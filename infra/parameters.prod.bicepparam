// PawTrack CR — Bicep parameter file for production deployment
// Usage: az deployment group create --template-file infra/main.bicep --parameters infra/parameters.prod.bicepparam

using './main.bicep'

param environment = 'prod'
param appName = 'pawtrack'
param location = 'eastus'

// frontendUrl: dominio definitivo de producción
param frontendUrl = 'https://pawtrack.cr'

// alertEmailAddress: Set to your ops/on-call email
param alertEmailAddress = 'ops@pawtrack.cr'

// sqlAdminPassword: Provide securely — never commit plain value
// Use: --parameters sqlAdminPassword="$(az keyvault secret show ...)"
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
