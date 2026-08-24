#!/usr/bin/env pwsh
# PawTrack CR — Stryker Mutation Testing Helper
# Usage:
#   .\run-stryker.ps1              # mutate Application layer (default)
#   .\run-stryker.ps1 -Layer Domain
#   .\run-stryker.ps1 -Layer Application -Open

param(
    [ValidateSet("Application", "Domain")]
    [string]$Layer = "Application",
    [switch]$Open
)

$ErrorActionPreference = "Stop"

$target = switch ($Layer) {
    "Application" { "src\PawTrack.Application" }
    "Domain"      { "src\PawTrack.Domain" }
}

Write-Host "🧬 Running Stryker on $Layer layer..." -ForegroundColor Cyan
Set-Location (Join-Path $PSScriptRoot $target)

dotnet stryker

if ($Open) {
    $report = Get-ChildItem "StrykerOutput\reports\*.html" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($report) {
        Write-Host "📊 Opening report: $($report.FullName)" -ForegroundColor Green
        Start-Process $report.FullName
    }
}

Write-Host "✅ Stryker complete. Report in $target\StrykerOutput\" -ForegroundColor Green
