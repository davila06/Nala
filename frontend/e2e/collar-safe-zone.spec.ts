import { test, expect } from "@playwright/test";
import { loginViaUi } from "./fixtures/ui-auth";
import { TEST_USERS } from "./fixtures/env";
import {
  apiLogin,
  createPet,
  grantPlusSubscription,
  registerCollarSerial,
  activateCollar,
  recordManualLocation,
  createSafeZone,
  uniqueSerial,
} from "./fixtures/api-setup";

const HOME_LAT = 9.9281;
const HOME_LNG = -84.0907;
const ZONE_NAME = `E2E Casa ${Date.now()}`;

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
  petId = await createPet(request, ownerToken, "E2E Safe Zone Dog");

  const serial = uniqueSerial();
  await registerCollarSerial(request, adminToken, serial);
  const collarId = await activateCollar(request, ownerToken, serial, petId);

  // Safe zone panel only renders once the collar has a last-known location.
  await recordManualLocation(request, ownerToken, petId, HOME_LAT, HOME_LNG);

  await createSafeZone(request, ownerToken, collarId, ZONE_NAME, [
    { lat: HOME_LAT + 0.001, lng: HOME_LNG + 0.001 },
    { lat: HOME_LAT + 0.001, lng: HOME_LNG - 0.001 },
    { lat: HOME_LAT - 0.001, lng: HOME_LNG - 0.001 },
  ]);
});

test.describe("Collar — Safe zones", () => {
  test("owner sees the API-created zone and can toggle it inactive", async ({
    page,
  }) => {
    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
    await page.goto(`/pets/${petId}`);
    await page.getByRole("button", { name: /GPS/ }).click();

    const zoneRow = page.locator("li", { hasText: ZONE_NAME });
    await expect(zoneRow).toBeVisible();
    await expect(zoneRow.getByRole("button", { name: "Activa" })).toBeVisible();

    await zoneRow.getByRole("button", { name: "Activa" }).click();
    await expect(zoneRow.getByRole("button", { name: "Inactiva" })).toBeVisible(
      { timeout: 10_000 },
    );
  });
});
