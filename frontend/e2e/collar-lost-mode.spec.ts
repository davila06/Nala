import { test, expect } from "@playwright/test";
import { loginViaUi } from "./fixtures/ui-auth";
import { TEST_USERS } from "./fixtures/env";
import {
  apiLogin,
  createPet,
  grantPlusSubscription,
  registerCollarSerial,
  activateCollar,
  uniqueSerial,
} from "./fixtures/api-setup";

let petId: string;

test.beforeAll(async ({ request }) => {
  const ownerToken = await apiLogin(
    request,
    TEST_USERS.owner.email,
    TEST_USERS.owner.password,
  );
  const adminToken = await apiLogin(
    request,
    TEST_USERS.admin.email,
    TEST_USERS.admin.password,
  );

  // Grant Plus first: the owner test user may already own a pet from prior
  // runs/seeds, and the Free tier caps pets at 1.
  await grantPlusSubscription(request, ownerToken, adminToken);
  petId = await createPet(request, ownerToken, "E2E Lost Mode Dog");

  const serial = uniqueSerial();
  await registerCollarSerial(request, adminToken, serial);
  await activateCollar(request, ownerToken, serial, petId);
});

test.describe("Collar — Lost Mode", () => {
  test("owner activates and deactivates lost mode from the GPS tab", async ({
    page,
  }) => {
    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
    await page.goto(`/pets/${petId}`);
    await page.getByRole("button", { name: /GPS/ }).click();

    const activateBtn = page.getByRole("button", {
      name: /Marcar mascota como perdida/,
    });
    await expect(activateBtn).toBeVisible();
    await activateBtn.click();

    await expect(page.getByText("Modo perdido activo")).toBeVisible({
      timeout: 10_000,
    });

    await page.getByRole("button", { name: /Desactivar modo perdido/ }).click();
    await page.getByRole("button", { name: /Confirmar/ }).click();

    await expect(page.getByText("Modo perdido activo")).not.toBeVisible({
      timeout: 10_000,
    });
  });
});
