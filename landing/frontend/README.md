# NALA Landing

Independent Next.js marketing site in the NALA monorepo. The product PWA remains in `../../frontend`.

## Local development

```powershell
Copy-Item .env.example .env.local
npm ci
npm run dev
```

Open `http://localhost:3000`. The local app URL defaults to the Vite PWA at `http://localhost:5173`.

## Checks

```powershell
npm run lint
npm test
npm run build
```

The build uses `output: "export"` and writes the static site to `out/`. Build-time configuration requires both `NEXT_PUBLIC_SITE_URL` and `NEXT_PUBLIC_APP_URL`; see `.env.example`. Use HTTPS origins outside localhost. Production values must be provided by the deployment environment and must not be committed.

`public/staticwebapp.config.json` carries Azure Static Web Apps response headers and the 404 override into the exported site. `.github/workflows/landing.yml` validates lint, tests, export output and config inclusion; it does not deploy. Azure resource, production domain and deployment credentials remain environment-specific and are not configured in this repository.

The landing never collects pet, location, health or contact data. Account creation and real reports continue in the product PWA.
