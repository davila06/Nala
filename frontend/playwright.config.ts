import { defineConfig, devices } from "@playwright/test";
import { FRONTEND_URL, API_URL } from "./e2e/fixtures/env";

/**
 * E2E suite for the collar/GPS feature set (Fase 4 + Fase 5).
 *
 * Prerequisites (NOT started by this config — see docs/collarFinal.md §E2E):
 *   - Backend running at E2E_API_URL (default http://localhost:5000), with a
 *     database seeded via backend/scripts/seed-test-users.sql.
 *   - Azurite running (photo uploads during pet creation).
 * The frontend IS started automatically via `webServer` below — it builds a
 * production bundle and serves it with `vite preview` (NOT `vite dev`; see the
 * comment on `webServer.command`).
 */

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false, // specs share seeded test accounts — avoid cross-test interference
  forbidOnly: !!process.env.CI,
  // Retries locally too (not just CI): an intermittent, pre-existing app-level
  // race on cold first page load has been observed — sometimes the very first
  // navigation in a fresh browser/page reloads itself dozens to hundreds of
  // times before settling (unrelated to E2E code; reproduced with a bare
  // Playwright script with no test framework involved, service worker
  // blocked, and no HMR/dev-mode client in play). Root cause not isolated;
  // a retry reliably passes. See /memories/repo/pawtrack-notes.md for the
  // full investigation notes.
  retries: process.env.CI ? 1 : 1,
  workers: 1,
  reporter: process.env.CI ? [["github"], ["html", { open: "never" }]] : "list",
  timeout: 30_000,
  use: {
    baseURL: FRONTEND_URL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: {
    // Uses a production build + `vite preview`, NOT `vite dev`. The dev server's
    // HMR/error-overlay client causes an intermittent full-navigation reload loop
    // under Playwright/CDP automation (confirmed via isolated repro: 20-30+
    // `framenavigated` events/4s on `vite dev` vs. 2 on a preview build of the
    // exact same code) — unrelated to any app code, purely a `vite dev` client
    // quirk. `vite preview` is also more representative of production anyway.
    //
    // Writes a throw-away `.env.e2e.local` (gitignored) before building so the
    // bundle points at E2E_API_URL instead of `.env.production`'s real Azure
    // URL — `vite build`'s default mode is "production", which loads
    // `.env.production` REGARDLESS of `VITE_API_URL` already being set in the
    // child process env (confirmed empirically: passing `env` below alone was
    // silently ignored). `--mode e2e` sidesteps `.env.production` entirely.
    //
    // Also runs `vite build` directly, not `npm run build` (which also runs
    // `tsc -b` and currently fails on an unrelated pre-existing type error in
    // tests/setup.ts).
    command: `node -e "require('fs').writeFileSync('.env.e2e.local','VITE_API_URL=${API_URL}\\n')" && npx vite build --mode e2e && npm run preview -- --port 5173 --strictPort`,
    url: FRONTEND_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: { VITE_API_URL: API_URL },
  },
  metadata: { apiUrl: API_URL },
});
