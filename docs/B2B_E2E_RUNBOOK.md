# B2B E2E runbook

## Smoke seguro

Requiere backend en `http://localhost:5199`, Azurite en `10000` y frontend
preview administrado por Playwright:

```powershell
Set-Location C:\Nala
.\start-dev.ps1 -NoFrontend
Set-Location C:\Nala\frontend
npx playwright test b2b-security-and-export.spec.ts --project=chromium --grep "consumer cannot"
```

Este escenario no necesita seed enterprise y verifica que un usuario Owner
reciba `403` al acceder a analytics Store y API keys Clinic.

## Escenarios enterprise opt-in

```powershell
$env:E2E_B2B_ENABLED = "1"
npx playwright test b2b-security-and-export.spec.ts --project=chromium
```

Antes de activar el modo opt-in deben existir:

- `clinica_partner@test.cr` con ClinicPartner activo;
- `tienda_activa@test.cr` con StorePartner activo;
- al menos un producto perteneciente a la tienda;
- datos de sede si se valida el filtro multi-sucursal.

Las pruebas de API key usan la clave cruda sólo durante la respuesta de creación
o rotación; nunca deben persistirla en fixtures, logs o reportes.

## Evidencia 2026-09-09

- Integración .NET B2B: 1/1.
- Playwright smoke live: 1/1.
- Playwright opt-in: descubierto, pendiente de seed enterprise activo.
