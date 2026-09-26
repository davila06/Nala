import type { ReactNode } from "react";
import { act, render } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MapContainer } from "@/features/map/components/MapContainer";

const { map } = vi.hoisted(() => ({
  map: {
    flyTo: vi.fn(),
    getBounds: () => ({
      getNorth: () => 10,
      getSouth: () => 9,
      getEast: () => -83,
      getWest: () => -84,
    }),
  },
}));

vi.mock("react-leaflet", () => ({
  MapContainer: ({ children }: { children: ReactNode }) => <div>{children}</div>,
  TileLayer: () => null,
  useMap: () => map,
  useMapEvents: () => map,
}));

vi.mock("react-leaflet-cluster", () => ({
  default: ({ children }: { children: ReactNode }) => <>{children}</>,
}));

vi.mock("@/features/map/components/LostPetMarker", () => ({ LostPetMarker: () => null }));
vi.mock("@/features/map/components/PredictionTrail", () => ({ PredictionTrail: () => null }));
vi.mock("@/features/map/components/SightingMarker", () => ({ SightingMarker: () => null }));
vi.mock("@/features/map/components/ClinicMarker", () => ({ ClinicMarker: () => null }));
vi.mock("@/features/stores/components/StoreMarker", () => ({ StoreMarker: () => null }));
vi.mock("@/features/adoptions/components/AdoptionMarker", () => ({ AdoptionMarker: () => null }));
vi.mock("@/features/map/components/ServiceProviderMarker", () => ({ ServiceProviderMarker: () => null }));

const originalGeolocation = navigator.geolocation;

afterEach(() => {
  vi.clearAllMocks();
  Object.defineProperty(navigator, "geolocation", {
    configurable: true,
    value: originalGeolocation,
  });
});

describe("MapContainer", () => {
  it("ignores a geolocation result received after unmount", () => {
    let onSuccess: PositionCallback | undefined;
    Object.defineProperty(navigator, "geolocation", {
      configurable: true,
      value: {
        getCurrentPosition: (success: PositionCallback) => {
          onSuccess = success;
        },
      },
    });
    const onLocated = vi.fn();

    const { unmount } = render(<MapContainer events={[]} onBBoxChange={vi.fn()} onLocated={onLocated} />);
    unmount();

    act(() => {
      onSuccess?.({
        coords: { latitude: 9.9, longitude: -84.1 },
      } as GeolocationPosition);
    });

    expect(map.flyTo).not.toHaveBeenCalled();
    expect(onLocated).not.toHaveBeenCalled();
  });
});
