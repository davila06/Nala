/** Shared environment config for the Playwright E2E suite. */
export const FRONTEND_URL = process.env.E2E_BASE_URL ?? "http://localhost:5173";
export const API_URL = process.env.E2E_API_URL ?? "http://localhost:5199";

export const TEST_USERS = {
  owner: { email: "owner@pawtrack.test", password: "Test123!" },
  admin: { email: "admin@pawtrack.test", password: "Admin123!" },
} as const;
