import { test, expect } from "@playwright/test";
import {
  apiLogin,
  createClinicApiKey,
  getB2BSeedStatus,
  lookupWithClinicApiKey,
  rotateClinicApiKey,
} from "./fixtures/api-setup";
import { API_URL, TEST_USERS } from "./fixtures/env";

function authHeaders(token: string) {
  return { Authorization: `Bearer ${token}` };
}

test.describe("B2B authorization and enterprise contracts", () => {
  test("consumer cannot access Store Partner analytics or clinic API-key operations", async ({
    request,
  }) => {
    const ownerToken = await apiLogin(
      request,
      TEST_USERS.owner.email,
      TEST_USERS.owner.password,
    );

    const storeAnalytics = await request.get(
      `${API_URL}/api/stores/me/analytics/export`,
      {
        headers: authHeaders(ownerToken),
      },
    );
    expect(storeAnalytics.status()).toBe(403);

    const clinicKeys = await request.get(`${API_URL}/api/clinics/me/api-keys`, {
      headers: authHeaders(ownerToken),
    });
    expect(clinicKeys.status()).toBe(403);
  });

  test("Store Partner analytics export returns CSV when the seeded enterprise account is active", async ({
    request,
  }) => {
    test.skip(
      !process.env.E2E_B2B_ENABLED,
      "Requires extended B2B seed data and active StorePartner subscription.",
    );
    const storeToken = await apiLogin(
      request,
      TEST_USERS.store.email,
      TEST_USERS.store.password,
    );
    const response = await request.get(
      `${API_URL}/api/stores/me/analytics/export`,
      {
        headers: authHeaders(storeToken),
      },
    );
    expect(response.ok()).toBe(true);
    expect(response.headers()["content-type"]).toContain("text/csv");
    expect(await response.text()).toContain("total_orders");
  });

  test("Clinic API key rotation revokes the old key", async ({ request }) => {
    const seed = getB2BSeedStatus();
    test.skip(!seed.enabled, seed.reason);
    const clinicToken = await apiLogin(
      request,
      TEST_USERS.clinic.email,
      TEST_USERS.clinic.password,
    );
    const original = await createClinicApiKey(request, clinicToken);
    const rotated = await rotateClinicApiKey(request, clinicToken, original.id);
    expect(rotated.key).not.toBe(original.key);
    expect(await lookupWithClinicApiKey(request, original.key)).toBe(401);
    expect([401, 404]).toContain(
      await lookupWithClinicApiKey(request, rotated.key),
    );
  });
});
