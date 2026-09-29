# PawTrack.E2ETests

This project contains a deliberately small .NET Playwright smoke test for the public map. The established feature E2E suite remains the TypeScript suite under [`frontend/e2e`](../../../frontend/e2e).

## Local prerequisites

- Local API running at `http://localhost:5199`.
- Local frontend preview/dev server running at `http://localhost:5173`.
- Synthetic/local data only.
- Playwright Chromium installed through the generated `playwright.ps1` installer.

Set `PAWTRACK_FRONTEND_URL` to test another local URL. Do not point this project at production or a shared environment.

## Run

```powershell
dotnet test backend/tests/PawTrack.E2ETests/PawTrack.E2ETests.csproj
```

The test validates the visible page, successful map data rendering and absence of the visible API error state. It does not replace the existing frontend Playwright recovery, collar, authentication and accessibility scenarios.
