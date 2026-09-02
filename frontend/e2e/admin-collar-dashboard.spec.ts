import { test, expect } from "@playwright/test";
import { loginViaUi } from "./fixtures/ui-auth";
import { TEST_USERS } from "./fixtures/env";
import {
  apiLogin,
  registerCollarSerial,
  uniqueSerial,
} from "./fixtures/api-setup";

let serial: string;

test.beforeAll(async ({ request }) => {
  const adminToken = await apiLogin(
    request,
    TEST_USERS.admin.email,
    TEST_USERS.admin.password,
  );
  serial = uniqueSerial();
  await registerCollarSerial(request, adminToken, serial);
});

test.describe("Admin — Collar tag dashboard", () => {
  test("metrics render and bulk mark-sold succeeds for a searched serial", async ({
    page,
  }) => {
    await loginViaUi(page, TEST_USERS.admin.email, TEST_USERS.admin.password);
    await page.goto("/admin");
    await page.getByRole("tab", { name: "CollarTags" }).click();

    await expect(page.getByText("Total", { exact: true })).toBeVisible();
    await expect(page.getByText("Sin activar")).toBeVisible();

    await page.getByPlaceholder("Buscar por serial…").fill(serial);

    const row = page.locator("tr", { hasText: serial });
    await expect(row).toBeVisible({ timeout: 10_000 });

    await row.locator('input[type="checkbox"]').check();
    await page.getByRole("button", { name: "Marcar vendidos" }).click();

    await expect(page.getByText(/exitosos/)).toBeVisible({ timeout: 10_000 });
  });
});
