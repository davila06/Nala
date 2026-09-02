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
  petId = await createPet(request, ownerToken, "E2E Handover Dog");

  const serial = uniqueSerial();
  await registerCollarSerial(request, adminToken, serial);
  await activateCollar(request, ownerToken, serial, petId);
});

test.describe("Collar — Handover PIN transfer", () => {
  test("owner generates a PIN, then redeems it to release the serial", async ({
    page,
  }) => {
    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
    await page.goto(`/pets/${petId}`);
    await page.getByRole("button", { name: /GPS/ }).click();

    await page
      .getByRole("button", { name: /Transferir a otro propietario/ })
      .click();

    const [generateResponse] = await Promise.all([
      page.waitForResponse((res) => res.url().includes("/handover/generate")),
      page
        .getByRole("button", { name: /Generar PIN de transferencia/ })
        .click(),
    ]);
    const { handoverCodeId, pin } = (await generateResponse.json()) as {
      handoverCodeId: string;
      pin: string;
    };
    expect(handoverCodeId).toBeTruthy();
    expect(pin).toMatch(/^\d{6}$/);

    await expect(page.locator("code").filter({ hasText: pin })).toBeVisible();

    // Redeem flow: same owner account can redeem its own transfer code to
    // exercise the full release path end-to-end (a real transfer would use a
    // second account, but the backend does not require a different user).
    await page.goto(`/collars/handover?id=${handoverCodeId}`);
    await page.getByPlaceholder("PIN de 6 dígitos").fill(pin);
    await page.getByRole("button", { name: /Recibir collar/ }).click();

    await expect(page.getByText("¡Collar liberado!")).toBeVisible({
      timeout: 10_000,
    });
  });
});
