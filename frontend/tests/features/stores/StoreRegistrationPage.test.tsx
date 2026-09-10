import { act, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import StoreRegistrationPage from "@/features/stores/pages/StoreRegistrationPage";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/lost-pets/components/LastSeenMap", () => ({
  LastSeenMap: () => <div data-testid="location-map" />,
}));

vi.mock("@/features/stores/hooks/useStores", () => ({
  useRegisterStore: () => ({ mutate: vi.fn(), isPending: false, error: null }),
}));

afterEach(() => {
  act(() => useAuthStore.getState().clearAuth());
});

describe("StoreRegistrationPage", () => {
  it("offers anonymous visitors a way back to business registration", () => {
    renderWithProviders(<StoreRegistrationPage />);

    expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
      "href",
      "/registro-negocio",
    );
    expect(screen.getByRole("link", { name: "Ir al inicio" })).toHaveAttribute(
      "href",
      "/login",
    );
  });
});
