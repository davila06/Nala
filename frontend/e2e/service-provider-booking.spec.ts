import { test, expect } from "@playwright/test";
import {
  apiLogin,
  createPet,
  grantPlusSubscription,
} from "./fixtures/api-setup";
import { API_URL, TEST_USERS } from "./fixtures/env";
import { loginViaUi } from "./fixtures/ui-auth";

const PROVIDER_ID = "c81a79f1-6a56-4bd5-be64-328e978786aa";
const SERVICE_ID = "116079ab-3431-4246-b538-3b123b293b74";

function tomorrowCostaRica(): string {
  const today = new Intl.DateTimeFormat("en-CA", {
    timeZone: "America/Costa_Rica",
  }).format(new Date());
  const tomorrow = new Date(`${today}T12:00:00-06:00`);
  tomorrow.setDate(tomorrow.getDate() + 1);
  return tomorrow.toLocaleDateString("en-CA", {
    timeZone: "America/Costa_Rica",
  });
}

test.describe("Service providers - booking workflow", () => {
  let petId: string;
  const bookingDate = tomorrowCostaRica();

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
    const providerToken = await apiLogin(
      request,
      TEST_USERS.provider.email,
      TEST_USERS.provider.password,
    );
    await grantPlusSubscription(request, ownerToken, adminToken);
    petId = await createPet(request, ownerToken, `E2E Booking ${Date.now()}`);
    const dayOfWeek = new Date(`${bookingDate}T12:00:00-06:00`).getDay();
    const response = await request.post(
      `${API_URL}/api/service-providers/services/${SERVICE_ID}/availability`,
      {
        headers: { Authorization: `Bearer ${providerToken}` },
        data: {
          dayOfWeek,
          startsAtLocalTime: "09:00",
          endsAtLocalTime: "17:00",
        },
      },
    );
    expect(response.ok()).toBeTruthy();
  });

  test("owner requests and provider confirms a booking", async ({ page }) => {
    await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
    await page.goto(`/servicios/${PROVIDER_ID}`);
    const serviceCard = page.getByText("Bano E2E", { exact: true });
    await expect(serviceCard).toBeVisible({ timeout: 10_000 });
    await serviceCard.click();
    await page.getByLabel("Fecha").fill(bookingDate);
    await page
      .getByRole("button", { name: /\(\d+\)$/ })
      .first()
      .click();
    await page.getByLabel("Mascota").selectOption(petId);
    await page.getByRole("button", { name: "Solicitar reserva" }).click();
    await expect(page.getByText("Solicitud de reserva enviada")).toBeVisible();

    await loginViaUi(
      page,
      TEST_USERS.provider.email,
      TEST_USERS.provider.password,
    );
    await page.goto("/servicio/portal/reservas");
    const requestedBooking = page
      .getByRole("listitem")
      .filter({ hasText: "Bano E2E" })
      .filter({ hasText: "Requested" })
      .first();
    await expect(requestedBooking).toBeVisible();
    const bookingDateTime = await requestedBooking
      .locator("p")
      .nth(1)
      .innerText();
    await requestedBooking.getByRole("button", { name: "Confirmar" }).click();
    await expect(
      page
        .getByRole("listitem")
        .filter({ hasText: "Bano E2E" })
        .filter({ hasText: bookingDateTime })
        .filter({ hasText: "Confirmed" })
        .first(),
    ).toBeVisible();
  });
});
