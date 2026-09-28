# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: frontend\e2e\ui-accessibility.spec.ts >> UI accessibility foundations >> /login has no WCAG 2.2 A/AA axe violations in dark mode
- Location: frontend\e2e\ui-accessibility.spec.ts:24:7

# Error details

```
Error: page.goto: Protocol error (Page.navigate): Cannot navigate to invalid URL
Call log:
  - navigating to "/login", waiting until "load"

```

# Test source

```ts
  1   | import AxeBuilder from "@axe-core/playwright";
  2   | import { expect, test } from "@playwright/test";
  3   | import { loginViaUi } from "./fixtures/ui-auth";
  4   | import { TEST_USERS } from "./fixtures/env";
  5   | import { apiLogin, createPet, grantPlusSubscription } from "./fixtures/api-setup";
  6   | 
  7   | async function expectAxeClean(page: import("@playwright/test").Page, include?: string) {
  8   |   let builder = new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]);
  9   |   if (include) builder = builder.include(include);
  10  |   const results = await builder.analyze();
  11  |   expect(
  12  |     results.violations.map(({ id, impact, help, nodes }) => ({
  13  |       id,
  14  |       impact,
  15  |       help,
  16  |       targets: nodes.map((node) => node.target),
  17  |     })),
  18  |   ).toEqual([]);
  19  | }
  20  | 
  21  | test.describe("UI accessibility foundations", () => {
  22  |   for (const route of ["/login", "/register"]) {
  23  |     for (const colorScheme of ["light", "dark"] as const) {
  24  |       test(`${route} has no WCAG 2.2 A/AA axe violations in ${colorScheme} mode`, async ({ page }) => {
  25  |         await page.emulateMedia({ colorScheme });
> 26  |         await page.goto(route);
      |                    ^ Error: page.goto: Protocol error (Page.navigate): Cannot navigate to invalid URL
  27  |         await page.waitForLoadState("networkidle");
  28  |         await expectAxeClean(page);
  29  |       });
  30  |     }
  31  |   }
  32  | 
  33  |   test("first-run onboarding dialog has no WCAG 2.2 A/AA axe violations", async ({ page }) => {
  34  |     await page.route("**/api/pets", async (route) => {
  35  |       if (route.request().method() === "GET") {
  36  |         await route.fulfill({ status: 200, contentType: "application/json", body: "[]" });
  37  |         return;
  38  |       }
  39  |       await route.continue();
  40  |     });
  41  |     await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
  42  |     await expect(page.getByRole("dialog", { name: /bienvenido/i })).toBeVisible();
  43  |     await expectAxeClean(page, "[role='dialog']");
  44  |   });
  45  | 
  46  |   test("lost-pet report, photo upload and manual map alternative have no WCAG 2.2 A/AA axe violations", async ({
  47  |     page,
  48  |     request,
  49  |   }) => {
  50  |     const ownerToken = await apiLogin(request, TEST_USERS.owner.email, TEST_USERS.owner.password);
  51  |     const adminToken = await apiLogin(request, TEST_USERS.admin.email, TEST_USERS.admin.password);
  52  |     await grantPlusSubscription(request, ownerToken, adminToken);
  53  |     const petName = `A11y ${Date.now()}`;
  54  |     const petId = await createPet(request, ownerToken, petName);
  55  | 
  56  |     await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
  57  |     await page.goto(`/pets/${petId}/report-lost`);
  58  |     await expect(
  59  |       page.getByRole("heading", { name: new RegExp(`reportar a ${petName} como perdido`, "i") }),
  60  |     ).toBeVisible();
  61  |     await expectAxeClean(page);
  62  | 
  63  |     await page.getByRole("button", { name: /introducir ubicación manualmente/i }).click();
  64  |     await expectAxeClean(page, "fieldset");
  65  |     await page.getByRole("button", { name: /siguiente/i }).click();
  66  |     await expect(page.getByText("Foto reciente (opcional)")).toBeVisible();
  67  |     await expectAxeClean(page);
  68  |   });
  69  | 
  70  |   test("QR flip control has no WCAG 2.2 A/AA axe violations", async ({ page, request }) => {
  71  |     const ownerToken = await apiLogin(request, TEST_USERS.owner.email, TEST_USERS.owner.password);
  72  |     const adminToken = await apiLogin(request, TEST_USERS.admin.email, TEST_USERS.admin.password);
  73  |     await grantPlusSubscription(request, ownerToken, adminToken);
  74  |     const petName = `A11y QR ${Date.now()}`;
  75  |     const petId = await createPet(request, ownerToken, petName);
  76  | 
  77  |     await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
  78  |     await page.goto(`/pets/${petId}`);
  79  |     await page.getByRole("tab", { name: "QR" }).click();
  80  |     await page.getByRole("button", { name: `Mostrar código QR de ${petName}` }).focus();
  81  |     await page.keyboard.press("Space");
  82  |     await expect(page.getByRole("img", { name: `QR de ${petName}` })).toBeVisible();
  83  |     await expectAxeClean(page, ".flip-scene");
  84  |   });
  85  | 
  86  |   test("public map has no WCAG 2.2 A/AA axe violations", async ({ page }) => {
  87  |     await page.goto("/map");
  88  |     await page.waitForLoadState("networkidle");
  89  |     await expectAxeClean(page);
  90  |   });
  91  | 
  92  |   test("authenticated shell exposes skip navigation and a focusable main landmark", async ({ page }) => {
  93  |     await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
  94  | 
  95  |     const skipLink = page.getByRole("link", { name: "Saltar al contenido" });
  96  |     await skipLink.focus();
  97  |     await expect(skipLink).toBeVisible();
  98  |     await expect(skipLink).toHaveAttribute("href", "#main-content");
  99  |     await expect(page.getByRole("main")).toHaveAttribute("id", "main-content");
  100 |   });
  101 | 
  102 |   test("lost-pet CTA deep-links to active-pet selection", async ({ page }) => {
  103 |     await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
  104 | 
  105 |     await expect(page.getByRole("link", { name: "Elegir mascota perdida" }).first()).toHaveAttribute(
  106 |       "href",
  107 |       "/dashboard?action=report-lost",
  108 |     );
  109 |   });
  110 | });
  111 | 
```