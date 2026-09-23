import { expect, test } from "@playwright/test";
import { loginViaUi } from "./fixtures/ui-auth";
import { TEST_USERS } from "./fixtures/env";

test.describe("UI accessibility foundations", () => {
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

    await expect(page.getByRole("link", { name: "Elegir mascota perdida" }).first()).toHaveAttribute(
      "href",
      "/dashboard?action=report-lost",
    );
  });
});
