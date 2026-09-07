import { test, expect } from "@playwright/test";
import { TEST_USERS } from "./fixtures/env";
import { loginViaUi } from "./fixtures/ui-auth";

test("local login session survives reload through refresh cookie", async ({
  page,
  context,
}) => {
  await loginViaUi(page, TEST_USERS.owner.email, TEST_USERS.owner.password);
  const cookies = await context.cookies(
    "http://localhost:5199/api/auth/refresh",
  );
  expect(
    cookies.find((cookie) => cookie.name === "refreshToken")?.secure,
  ).toBeFalsy();

  await page.reload();
  await page.waitForURL((url) => !url.pathname.startsWith("/login"), {
    timeout: 15_000,
  });
});
