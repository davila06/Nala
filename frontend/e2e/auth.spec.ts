import { test, expect } from "@playwright/test";
import { loginViaUi, presetCookieConsent } from "./fixtures/ui-auth";
import { TEST_USERS } from "./fixtures/env";

test.describe("Auth — login", () => {
  test("owner can log in with seeded credentials and lands off /login", async ({
    page,
  }) => {
    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
    await expect(page).not.toHaveURL(/\/login/);
  });

  test("shows an error for invalid credentials", async ({ page }) => {
    await page.goto("/login");
    await page.waitForLoadState("networkidle");
    await presetCookieConsent(page);
    await page.getByLabel("Correo electrónico").fill(TEST_USERS.owner.email);
    await page.getByLabel("Contraseña", { exact: true }).fill("wrong-password");
    const [response] = await Promise.all([
      page.waitForResponse((res) => res.url().includes("/auth/login")),
      page.getByRole("button", { name: /Ingresar/ }).click(),
    ]);
    expect(response.status()).toBe(401);
    await expect(page.getByText(/incorrectos/i)).toBeVisible();
  });
});
