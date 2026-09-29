import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";
import { loginViaUi } from "./fixtures/ui-auth";
import { TEST_USERS } from "./fixtures/env";
import { apiLogin, createPet, grantPlusSubscription } from "./fixtures/api-setup";

async function expectAxeClean(page: import("@playwright/test").Page, include?: string) {
  let builder = new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]);
  if (include) builder = builder.include(include);
  const results = await builder.analyze();
    expect(
      results.violations.map(({ id, impact, help, nodes }) => ({
        id,
        impact,
        help,
        nodes: nodes.map((node) => ({ target: node.target, html: node.html, failureSummary: node.failureSummary })),
      })),
    ).toEqual([]);
}

test.describe("UI accessibility foundations", () => {
  for (const route of ["/login", "/register"]) {
    for (const colorScheme of ["light", "dark"] as const) {
      test(`${route} has no WCAG 2.2 A/AA axe violations in ${colorScheme} mode`, async ({ page }) => {
        await page.emulateMedia({ colorScheme });
        await page.goto(route);
        await page.waitForLoadState("networkidle");
        await expectAxeClean(page);
      });
    }
  }

  test("first-run onboarding dialog has no WCAG 2.2 A/AA axe violations", async ({ page }) => {
    await page.route("**/api/pets", async (route) => {
      if (route.request().method() === "GET") {
        await route.fulfill({ status: 200, contentType: "application/json", body: "[]" });
        return;
      }
      await route.continue();
    });
    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
    await expect(page.getByRole("dialog", { name: /bienvenido/i })).toBeVisible();
    await expectAxeClean(page, "[role='dialog']");
  });

  test("lost-pet report, photo upload and manual map alternative have no WCAG 2.2 A/AA axe violations", async ({
    page,
    request,
  }) => {
    const ownerToken = await apiLogin(request, TEST_USERS.owner.email, TEST_USERS.owner.password);
    const adminToken = await apiLogin(request, TEST_USERS.admin.email, TEST_USERS.admin.password);
    await grantPlusSubscription(request, ownerToken, adminToken);
    const petName = `A11y ${Date.now()}`;
    const petId = await createPet(request, ownerToken, petName);

    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
    await page.goto(`/pets/${petId}/report-lost`);
    await expect(
      page.getByRole("heading", { name: new RegExp(`reportar a ${petName} como perdido`, "i") }),
    ).toBeVisible();
    await expectAxeClean(page);

    await page.getByRole("button", { name: /introducir ubicación manualmente/i }).click();
    await expectAxeClean(page, "fieldset");
    await page.getByRole("button", { name: /siguiente/i }).click();
    await expect(page.getByText("Foto reciente (opcional)")).toBeVisible();
    await expectAxeClean(page);
  });

  test("QR flip control has no WCAG 2.2 A/AA axe violations", async ({ page, request }) => {
    const ownerToken = await apiLogin(request, TEST_USERS.owner.email, TEST_USERS.owner.password);
    const adminToken = await apiLogin(request, TEST_USERS.admin.email, TEST_USERS.admin.password);
    await grantPlusSubscription(request, ownerToken, adminToken);
    const petName = `A11y QR ${Date.now()}`;
    const petId = await createPet(request, ownerToken, petName);

    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
    await page.goto(`/pets/${petId}`);
    await page.getByRole("tab", { name: "QR" }).click();
    await page.getByRole("button", { name: `Mostrar código QR de ${petName}` }).focus();
    await page.keyboard.press("Space");
    await expect(page.getByRole("img", { name: `QR de ${petName}` })).toBeVisible();
    await expectAxeClean(page, ".flip-scene");
  });

  test("public map has no WCAG 2.2 A/AA axe violations", async ({ page }) => {
    await page.goto("/map");
    await page.waitForLoadState("networkidle");
    await expectAxeClean(page);
    await page.getByRole("button", { name: "Lista" }).click();
    await expect(page.getByRole("main", { name: "Eventos visibles en el mapa" })).toBeVisible();
    await expectAxeClean(page);
  });

  test("authenticated shell exposes skip navigation and a focusable main landmark", async ({ page }) => {
    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);

    const skipLink = page.getByRole("link", { name: "Saltar al contenido" });
    await skipLink.focus();
    await expect(skipLink).toBeVisible();
    await expect(skipLink).toHaveAttribute("href", "#main-content");
    await expect(page.getByRole("main")).toHaveAttribute("id", "main-content");
  });

  test("lost-pet CTA deep-links to active-pet selection", async ({ page }) => {
    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);

    await page.setViewportSize({ width: 390, height: 844 });
    const mobileCta = page.getByRole("link", { name: "Reportar mascota perdida" });
    await expect(mobileCta).toBeVisible();
    await expect(mobileCta).toHaveAttribute("href", "/dashboard?action=report-lost");

    await page.setViewportSize({ width: 1280, height: 900 });
    await expect(page.getByRole("link", { name: "Elegir mascota perdida" }).first()).toHaveAttribute(
      "href",
      "/dashboard?action=report-lost",
    );
  });

  for (const { account, label, route } of [
    { account: TEST_USERS.owner, label: "Elegir mascota perdida", route: "/dashboard?action=report-lost" },
    { account: TEST_USERS.clinic, label: "Panel Clínica", route: "/clinica/portal" },
    { account: TEST_USERS.store, label: "Portal Tienda", route: "/tienda/portal" },
    { account: TEST_USERS.provider, label: "Portal de servicios", route: "/servicio/portal" },
  ]) {
    test(`mobile menu reaches ${label} by keyboard without axe violations`, async ({ page }) => {
      await page.setViewportSize({ width: 390, height: 844 });
      await loginViaUi(page, account.email, account.password);
      const onboarding = page.getByRole("dialog", { name: /bienvenido/i });
      if (await onboarding.isVisible()) {
        await onboarding.getByRole("button", { name: "Saltar por ahora" }).click();
      }
      const trigger = page.getByRole("button", { name: "Menú de usuario" });
      await trigger.focus();
      await page.keyboard.press("Enter");
      const menu = page.getByRole("navigation", { name: "Navegación móvil" });
      await expect(menu.getByRole("link", { name: label })).toHaveAttribute("href", route);
      await page.keyboard.press("Tab");
      await expect(menu.getByRole("link", { name: "Mascota" })).toBeFocused();
      await expectAxeClean(page, "nav[aria-label='Navegación móvil']");
    });
  }
});
