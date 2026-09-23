[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BaselinePath,

    [Parameter(Mandatory)]
    [string]$CurrentPath,

    [switch]$WriteSurface
)

$ErrorActionPreference = "Stop"

function Get-Surface([string]$path) {
    $document = Get-Content -Raw $path | ConvertFrom-Json
    $operations = [ordered]@{}
    foreach ($property in $document.paths.PSObject.Properties | Where-Object { $_.Name.StartsWith('/') }) {
        $methods = if ($property.Value -is [System.Array]) {
            @($property.Value | ForEach-Object { [string]$_ } | Sort-Object)
        } else {
            @($property.Value.PSObject.Properties.Name | Where-Object {
                $_ -in @("get", "post", "put", "patch", "delete", "head", "options", "trace")
            } | Sort-Object)
        }
        $operations[$property.Name] = $methods
    }

    [pscustomobject]@{
        openapiMajor = ([string]$document.openapi).Split('.')[0]
        paths = $operations
    }
}

$baseline = Get-Surface $BaselinePath
$current = Get-Surface $CurrentPath

if ($baseline.openapiMajor -ne $current.openapiMajor) {
    throw "OpenAPI major version changed from $($baseline.openapiMajor) to $($current.openapiMajor). Create a new versioned contract."
}

$removed = [System.Collections.Generic.List[string]]::new()
foreach ($path in $baseline.paths.PSObject.Properties | Where-Object { $_.Name.StartsWith('/') }) {
    $currentPath = $current.paths.PSObject.Properties[$path.Name]
    if ($null -eq $currentPath) {
        $removed.Add("Removed path: $($path.Name)")
        continue
    }

    foreach ($method in @($path.Value)) {
        $methodName = [string]$method
        if ($methodName -notin @($currentPath.Value | ForEach-Object { [string]$_ })) {
            $removed.Add("Removed operation: $($methodName.ToUpperInvariant()) $($path.Name)")
        }
    }
}

if ($removed.Count -gt 0) {
    $removed | ForEach-Object { Write-Error $_ }
    throw "OpenAPI breaking changes detected. Add a new API version or update the approved baseline explicitly."
}

if ($WriteSurface) {
    $current | ConvertTo-Json -Depth 10
} else {
    Write-Host "OpenAPI breaking-change check passed."
}
