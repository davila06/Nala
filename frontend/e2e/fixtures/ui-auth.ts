import type { Page } from "@playwright/test";

/**
 * Dismisses the bottom-fixed cookie consent banner if present.
 * Deliberately uses a real UI click (not page.addInitScript to preset
 * localStorage) — addInitScript's CDP-injected script triggers a dev-mode
 * PWA service-worker reload loop in this app (vite-plugin-pwa devOptions),
 * causing every subsequent navigation to bounce repeatedly.
 */
export async function presetCookieConsent(page: Page): Promise<void> {
  const acceptButton = page.getByRole("button", { name: "Aceptar todo" });
  try {
    await acceptButton.click({ timeout: 2_000 });
  } catch {
    // Banner not shown (e.g. already dismissed) — nothing to do.
  }
}

/** Logs in through the real login form (not a token injection) and waits for redirect off /login. */
export async function loginViaUi(
  page: Page,
  email: string,
  password: string,
): Promise<void> {
  await page.goto("/login");
  // Let the initial SW registration / precache activity on a cold preview-server
  // page settle before interacting — otherwise the page can intermittently
  // re-navigate to itself a few times right after the very first load.
  await page.waitForLoadState("networkidle");
  await presetCookieConsent(page);
  await page.getByLabel("Correo electrónico").fill(email);
  await page.getByLabel("Contraseña", { exact: true }).fill(password);
  await page.getByRole("button", { name: /Ingresar/ }).click();
  await page.waitForURL((url) => !url.pathname.startsWith("/login"), {
    timeout: 15_000,
  });
}
