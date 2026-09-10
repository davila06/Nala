import { act, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ReactNode } from "react";
import ClinicRegisterPage from "@/features/clinics/pages/ClinicRegisterPage";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("react-leaflet", () => ({
  MapContainer: ({ children }: { children: ReactNode }) => (
    <div data-testid="clinic-location-map-inner">{children}</div>
  ),
  TileLayer: () => null,
  Marker: () => null,
  useMapEvents: () => null,
  useMap: () => ({ setView: () => undefined, getZoom: () => 13 }),
}));

afterEach(() => {
  act(() => useAuthStore.getState().clearAuth());
});

describe("ClinicRegisterPage", () => {
  it("offers anonymous visitors a way back to business registration", () => {
    renderWithProviders(<ClinicRegisterPage />);

    expect(screen.getByRole("link", { name: "Volver" })).toHaveAttribute(
      "href",
      "/registro-negocio",
    );
    expect(screen.getByRole("link", { name: "Ir al inicio" })).toHaveAttribute(
      "href",
      "/login",
    );
  });

  it("shows a map to select clinic coordinates", () => {
    renderWithProviders(<ClinicRegisterPage />);

    expect(
      screen.getByText(/selecciona la ubicacion en el mapa/i),
    ).toBeInTheDocument();
    expect(screen.getByTestId("clinic-location-map")).toBeInTheDocument();
  });
});
