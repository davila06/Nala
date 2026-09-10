import { act, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import BusinessRegistrationHubPage from "@/features/auth/pages/BusinessRegistrationHubPage";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

afterEach(() => {
  act(() => useAuthStore.getState().clearAuth());
});

describe("BusinessRegistrationHubPage", () => {
  it("offers anonymous visitors a way back to account registration", () => {
    renderWithProviders(<BusinessRegistrationHubPage />);

    expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
      "href",
      "/register",
    );
    expect(screen.getByRole("link", { name: "Ir al inicio" })).toHaveAttribute(
      "href",
      "/login",
    );
  });
});
