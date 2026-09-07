import { test, expect } from "@playwright/test";
import { loginViaUi, presetCookieConsent } from "./fixtures/ui-auth";
import { TEST_USERS } from "./fixtures/env";

test.describe("Regulatory and NALA portals", () => {
  test("admin can open NALA dashboard", async ({ page }) => {
    await loginViaUi(page, TEST_USERS.admin.email, TEST_USERS.admin.password);
    await page.goto("/nala");
    await expect(page.getByText("NALA Core")).toBeVisible();
    await expect(
      page.getByRole("region", { name: "Indicadores NALA" }),
    ).toBeVisible();
  });

  test("admin can open institutional reports portal", async ({ page }) => {
    await loginViaUi(page, TEST_USERS.admin.email, TEST_USERS.admin.password);
    await page.goto("/reportes-institucionales");
    await expect(page.getByText("Reportes institucionales")).toBeVisible();
    await expect(
      page.getByRole("button", { name: /Solicitar export/ }),
    ).toBeVisible();
    await presetCookieConsent(page);
  });
});
