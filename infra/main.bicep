@description('PawTrack CR — Azure Infrastructure (MVP) — Región de despliegue')
param location string = resourceGroup().location

@description('Entorno: dev, staging, prod')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Nombre base de la aplicación')
param appName string = 'pawtrack'

@description('URL del frontend desplegado (para CORS)')
param frontendUrl string = 'https://pawtrack.azurestaticapps.net'

@description('Email para recibir alertas de Azure Monitor')
param alertEmailAddress string

// ── Nombres de recursos ────────────────────────────────────────────────────────
var resourcePrefix = '${appName}-${environment}'
var keyVaultName = '${appName}-kv-${environment}'
var storageAccountName = replace('${appName}storage${environment}', '-', '')

// ── Log Analytics Workspace (requerido por Application Insights) ──────────────
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${resourcePrefix}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// ── Application Insights ──────────────────────────────────────────────────────
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourcePrefix}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

// ── Azure SQL Server ──────────────────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${resourcePrefix}-sql'
  location: location
  properties: {
    administratorLogin: 'pawtrackadmin'
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

@secure()
param sqlAdminPassword string

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'pawtrack'
  location: location
  sku: {
    name: 'GP_S_Gen5_1'  // Serverless Gen5 1 vCore — costo-eficiente para MVP
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    autoPauseDelay: 60  // Minutos hasta auto-pause (serverless)
    minCapacity: json('0.5')
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

// ── Storage Account (Pet Photos) ──────────────────────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: true  // Necesario para URLs públicas de fotos
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource petPhotosContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'pet-photos'
  properties: {
    publicAccess: 'Blob'  // Acceso anónimo de lectura para URLs de foto
  }
}

resource sightingPhotosContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'sighting-photos'
  properties: {
    publicAccess: 'Blob'  // URLs de foto de avistamiento son públicas
  }
}

// ── Key Vault ─────────────────────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

// ── Container Registry (ACR) ─────────────────────────────────────────────────
var acrName = replace('${appName}acr${environment}', '-', '')

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

// ── Container Apps Environment ────────────────────────────────────────────────
resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${resourcePrefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ── Container App (API Backend) ───────────────────────────────────────────────
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${resourcePrefix}-api'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerAppsEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          // Imagen placeholder — actualizar con imagen real tras el primer push a ACR:
          // az containerapp update --name ${resourcePrefix}-api --resource-group PawnTrackBeta --image <acr>.azurecr.io/pawtrack-api:<tag>
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Staging'
            }
            {
              name: 'Azure__KeyVaultUri'
              value: keyVault.properties.vaultUri
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: frontendUrl
            }
            // Secrets adicionales (AppInsights, SQL, Storage, JWT) se configuran
            // DESPUÉS de poblar Key Vault — ver docs/PENDIENTES_BETA.md
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
      }
    }
  }
}

// ── RBAC: Container App → Key Vault (Key Vault Secrets User) ──────────────────
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource containerAppKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, containerApp.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── RBAC: Container App → ACR (AcrPull) ───────────────────────────────────
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource containerAppAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, containerApp.id, acrPullRoleId)
  scope: containerRegistry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── Azure Monitor: Action Group (email alerts) ────────────────────────────────
resource alertActionGroup 'Microsoft.Insights/actionGroups@2023-09-01-preview' = {
  name: '${resourcePrefix}-alerts-ag'
  location: 'global'
  properties: {
    groupShortName: 'PawTrack'
    enabled: true
    emailReceivers: [
      {
        name: 'ops-email'
        emailAddress: alertEmailAddress
        useCommonAlertSchema: true
      }
    ]
  }
}

// ── Azure Monitor: Alert — HTTP 5xx error rate > 1% ──────────────────────────
resource alertHttp5xx 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${resourcePrefix}-alert-5xx'
  location: 'global'
  properties: {
    description: 'Failed requests exceeded 5 over 5 minutes'
    severity: 1
    enabled: true
    scopes: [appInsights.id]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'FailedRequests'
          metricName: 'requests/failed'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
    autoMitigate: true
  }
}

// ── Azure Monitor: Alert — P95 response latency > 500ms ──────────────────────
resource alertLatency 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${resourcePrefix}-alert-latency'
  location: 'global'
  properties: {
    description: 'P95 HTTP response latency exceeded 500ms over 5 minutes'
    severity: 2
    enabled: true
    scopes: [appInsights.id]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'requests/duration'
          metricName: 'requests/duration'
          operator: 'GreaterThan'
          threshold: 500
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
    autoMitigate: true
  }
}

// ── Application Insights: Availability Test ───────────────────────────────────
resource availabilityTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: '${resourcePrefix}-availability'
  location: location
  kind: 'ping'
  tags: {
    'hidden-link:${appInsights.id}': 'Resource'
  }
  properties: {
    SyntheticMonitorId: '${resourcePrefix}-availability'
    Name: 'PawTrack Health Check'
    Description: 'Pings /health endpoint every 5 minutes from multiple regions'
    Enabled: true
    Frequency: 300   // seconds (5 min)
    Timeout: 30
    Kind: 'ping'
    Locations: [
      { Id: 'us-va-ash-azr' }   // East US
      { Id: 'latam-br-gru-edge' } // Brazil South (closest to CR)
      { Id: 'us-tx-sn1-azr' }   // South Central US
    ]
    Configuration: {
      WebTest: '<WebTest Name="${resourcePrefix}-availability" Enabled="True" Timeout="30" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Items><Request Method="GET" Guid="health-check" Version="1.1" Url="https://${containerApp.properties.configuration.ingress.fqdn}/health" ThinkTime="0" /></Items></WebTest>'
    }
  }
}

// ── Application Insights: Availability Alert ──────────────────────────────────
resource availabilityAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${resourcePrefix}-alert-availability'
  location: 'global'
  properties: {
    description: 'API availability dropped below 100% (any location failing)'
    severity: 1
    enabled: true
    scopes: [availabilityTest.id, appInsights.id]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.WebtestLocationAvailabilityCriteria'
      webTestId: availabilityTest.id
      componentId: appInsights.id
      failedLocationCount: 1
    }
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
    autoMitigate: true
  }
}

// ── Azure Static Web Apps (Frontend) ─────────────────────────────────────────
// Crea el recurso del frontend. El primer build del dist/ se hace con az staticwebapp deploy.
// El deployment token se recupera después con: az staticwebapp secrets list
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: '${resourcePrefix}-frontend'
  location: location
  sku: {
    name: environment == 'prod' ? 'Standard' : 'Free'
    tier: environment == 'prod' ? 'Standard' : 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output appServiceUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output containerAppName string = containerApp.name
output acrLoginServer string = containerRegistry.properties.loginServer
output keyVaultUri string = keyVault.properties.vaultUri
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output storageAccountName string = storageAccount.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultDomain string = staticWebApp.properties.defaultHostname
