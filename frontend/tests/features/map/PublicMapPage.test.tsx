import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { act } from "react";
import PublicMapPage from "@/features/map/pages/PublicMapPage";
import { renderWithProviders } from "../../utils/renderWithProviders";
import { useAuthStore } from "@/features/auth/store/authStore";

const mapPageMocks = vi.hoisted(() => ({ usePublicMapEvents: vi.fn() }));

vi.mock("@/features/map/hooks/usePublicMap", () => ({
  useDebouncedBBox: () => ({ debounce: () => {} }),
  usePublicMapEvents: mapPageMocks.usePublicMapEvents,
}));

vi.mock("@/features/map/hooks/useMovementPrediction", () => ({
  useMovementPredictions: () => ({}),
}));

vi.mock("@/features/map/components/MapContainer", () => ({
  MapContainer: () => <div data-testid="map-container" />,
}));

vi.mock("@/features/advertising/components/BillboardBanner", () => ({
  BillboardBanner: () => null,
}));

describe("PublicMapPage", () => {
  it("offers the same visible events in an accessible list without the map canvas", async () => {
    mapPageMocks.usePublicMapEvents.mockReturnValue({
      data: [
        {
          id: "lost-1",
          eventType: "LostPet",
          petId: "pet-1",
          petName: "Luna",
          species: "dog",
          lat: 9.9,
          lng: -84.1,
          photoUrl: null,
          occurredAt: "2026-09-28T10:00:00Z",
        },
      ],
      isFetching: false,
      isError: false,
    });
    const user = userEvent.setup();
    renderWithProviders(<PublicMapPage />);

    await user.click(screen.getByRole("button", { name: "Lista" }));

    expect(screen.queryByTestId("map-container")).not.toBeInTheDocument();
    expect(await screen.findByRole("main", { name: "Eventos visibles en el mapa" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /luna.*perdida/i })).toHaveAttribute("href", "/p/pet-1");
  });

  it("shows dashboard button when user is authenticated", () => {
    act(() => {
      useAuthStore.getState().setAuth(
        {
          id: "u-1",
          name: "Owner",
          email: "owner@test.cr",
          role: "Owner",
          isAdmin: false,
        },
        "token",
      );
    });

    renderWithProviders(<PublicMapPage />);

    const dashboardLink = screen.getByRole("link", { name: /dashboard/i });
    expect(dashboardLink).toBeInTheDocument();
    expect(dashboardLink).toHaveAttribute("href", "/dashboard");
  });

  it("hides dashboard button when user is not authenticated", () => {
    act(() => {
      useAuthStore.getState().clearAuth();
    });

    renderWithProviders(<PublicMapPage />);

    expect(screen.queryByRole("link", { name: /dashboard/i })).not.toBeInTheDocument();
  });

  it("links visitors to the adoption directory", () => {
    renderWithProviders(<PublicMapPage />);

    expect(screen.getByRole("link", { name: "Ver adopciones" })).toHaveAttribute("href", "/adopciones");
  });
});
