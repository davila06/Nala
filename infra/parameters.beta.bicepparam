// PawTrack CR — Bicep parameter file for BETA deployment
// Resource Group : PawnTrackBeta
// Subscription  : Azure subscription 1 (3832b5df-115d-4092-9fc8-2105d7b0af21)
// Account       : davila06@gmail.com
//
// Deploy command:
//   $env:SQL_ADMIN_PASSWORD = "<secret>"
//   az deployment group create \
//     --resource-group PawnTrackBeta \
//     --template-file infra/main.bicep \
//     --parameters infra/parameters.beta.bicepparam \
//     --parameters sqlAdminPassword=$env:SQL_ADMIN_PASSWORD \
//     --name pawtrack-beta

using './main.bicep'

param environment = 'dev'
param appName     = 'pawtrack'
param location    = 'eastus2'

// Se actualiza con el dominio real después del primer deploy.
// Ejecuta: az deployment group show --name pawtrack-beta --resource-group PawnTrackBeta \
//            --query properties.outputs.staticWebAppUrl.value -o tsv
param frontendUrl = 'https://pawtrack.azurestaticapps.net'

// Email para alertas de Azure Monitor
param alertEmailAddress = 'davila06@gmail.com'

// sqlAdminPassword: NO hacer commit del valor real.
// Pasar siempre como variable de entorno:
//   --parameters sqlAdminPassword=$env:SQL_ADMIN_PASSWORD
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
