import { test, expect } from "@playwright/test";
import { activateSearchCoordination, apiLogin, createPet, openMaskedChat, reportLostPet } from "./fixtures/api-setup";
import { COORDINATION_FIXTURE, TEST_USERS } from "./fixtures/env";
import { loginViaUi } from "./fixtures/ui-auth";

test.describe("Search coordination location sharing", () => {
  test("requires consent, exposes recipient count, and stops sharing immediately", async ({
    page: ownerPage,
    request,
    browser,
  }) => {
    const ownerToken = await apiLogin(request, TEST_USERS.owner.email, TEST_USERS.owner.password);
    const finderToken = await apiLogin(request, TEST_USERS.finder.email, TEST_USERS.finder.password);
    const petId =
      COORDINATION_FIXTURE.petId ?? (await createPet(request, ownerToken, `E2E Coordination ${Date.now()}`));
    const lostEventId = COORDINATION_FIXTURE.lostEventId ?? (await reportLostPet(request, ownerToken, petId));
    if (!COORDINATION_FIXTURE.lostEventId) {
      await activateSearchCoordination(request, ownerToken, lostEventId);
    }
    await openMaskedChat(request, finderToken, lostEventId);

    const finderContext = await browser.newContext();
    const finderPage = await finderContext.newPage();
    try {
      await Promise.all([
        loginViaUi(ownerPage, TEST_USERS.owner.email, TEST_USERS.owner.password),
        loginViaUi(finderPage, TEST_USERS.finder.email, TEST_USERS.finder.password),
      ]);
      await finderPage.goto(`/lost/${lostEventId}/busqueda`);
      await expect(finderPage.getByText(/Centro de Búsqueda/i)).toBeVisible();
      await expect(finderPage.getByText(/Tiempo real activo/i)).toBeVisible();

      await ownerPage.goto(`/lost/${lostEventId}/busqueda`);
      await expect(ownerPage.getByText(/Centro de Búsqueda/i)).toBeVisible();
      await expect(ownerPage.getByText(/Tiempo real activo/i)).toBeVisible();
      await expect(ownerPage.getByText(/Tu ubicación no se comparte con el equipo/i)).toBeVisible();

      await ownerPage.getByRole("button", { name: /Compartir ubicación aproximada/i }).click();
      await expect(ownerPage.getByText(/1 participante recibe esta señal/i)).toBeVisible();
      await expect(ownerPage.getByText(/de forma aproximada/i)).toBeVisible();

      await ownerPage.getByRole("button", { name: /Dejar de compartir/i }).click();
      await expect(ownerPage.getByText(/Tu ubicación no se comparte con el equipo/i)).toBeVisible();
    } finally {
      await finderContext.close();
    }
  });
});
