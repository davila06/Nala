import { act, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import ServiceProviderRegistrationPage from "@/features/service-providers/pages/ServiceProviderRegistrationPage";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/lost-pets/components/LastSeenMap", () => ({
  LastSeenMap: () => <div data-testid="location-map" />,
}));

vi.mock("@/features/service-providers/hooks/useServiceProviders", () => ({
  useRegisterServiceProvider: () => ({
    mutate: vi.fn(),
    isPending: false,
    error: null,
  }),
}));

afterEach(() => {
  act(() => useAuthStore.getState().clearAuth());
});

describe("ServiceProviderRegistrationPage", () => {
  it("gives anonymous visitors a way back to the directory and sign-in", () => {
    renderWithProviders(<ServiceProviderRegistrationPage />);

    expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
      "href",
      "/servicios",
    );
    expect(screen.getByRole("link", { name: "Ir al inicio" })).toHaveAttribute(
      "href",
      "/login",
    );
  });

  it("sends authenticated visitors back to their dashboard", () => {
    act(() => {
      useAuthStore.getState().setAuth(
        {
          id: "user-1",
          name: "Pat",
          email: "pat@example.test",
          role: "Owner",
          isAdmin: false,
        },
        "token",
      );
    });

    renderWithProviders(<ServiceProviderRegistrationPage />);

    expect(screen.getByRole("link", { name: "Ir al inicio" })).toHaveAttribute(
      "href",
      "/dashboard",
    );
  });
});