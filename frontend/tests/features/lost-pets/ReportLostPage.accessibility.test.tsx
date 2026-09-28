import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ReportLostPage from "@/features/lost-pets/pages/ReportLostPage";

const lostPageMocks = vi.hoisted(() => ({
  usePetDetail: vi.fn(),
  useReportLost: vi.fn(),
  useGeolocation: vi.fn(),
  useNeighborCountInArea: vi.fn(),
  useRecoveryRates: vi.fn(),
  addQueuedReport: vi.fn(),
  trackProductEvent: vi.fn(),
}));

vi.mock("@/features/pets/hooks/usePets", () => ({ usePetDetail: lostPageMocks.usePetDetail }));
vi.mock("@/features/lost-pets/hooks/useLostPets", () => ({ useReportLost: lostPageMocks.useReportLost }));
vi.mock("@/features/lost-pets/hooks/useGeolocation", () => ({ useGeolocation: lostPageMocks.useGeolocation }));
vi.mock("@/features/locations/hooks/useNeighbor", () => ({
  useNeighborCountInArea: lostPageMocks.useNeighborCountInArea,
}));
vi.mock("@/features/lost-pets/hooks/useRecoveryStats", () => ({ useRecoveryRates: lostPageMocks.useRecoveryRates }));
vi.mock("@/shared/lib/offlineQueue", () => ({ addQueuedReport: lostPageMocks.addQueuedReport }));
vi.mock("@/shared/lib/telemetry", () => ({ trackProductEvent: lostPageMocks.trackProductEvent }));
vi.mock("@/features/lost-pets/components/LastSeenMap", () => ({
  LastSeenMap: () => <div role="img" aria-label="Mapa de última ubicación" />,
}));
vi.mock("@/features/pets/components/PhotoUpload", () => ({
  PhotoUpload: () => <button type="button">Subir foto reciente</button>,
}));
vi.mock("@/features/lost-pets/components/useGeolocation", () => ({}));

beforeEach(() => {
  vi.clearAllMocks();
  lostPageMocks.usePetDetail.mockReturnValue({
    data: { id: "pet-1", name: "Luna", species: "Dog", breed: "Mestiza" },
    isLoading: false,
  });
  lostPageMocks.useReportLost.mockReturnValue({ mutateAsync: vi.fn(), isPending: false, error: null });
  lostPageMocks.useGeolocation.mockReturnValue({
    status: "denied",
    coords: null,
    error: "Permiso denegado",
    request: vi.fn(),
  });
  lostPageMocks.useNeighborCountInArea.mockReturnValue({ data: undefined });
  lostPageMocks.useRecoveryRates.mockReturnValue({ data: undefined });
  lostPageMocks.addQueuedReport.mockResolvedValue(undefined);
});

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/pets/pet-1/report-lost"]}>
      <Routes>
        <Route path="/pets/:id/report-lost" element={<ReportLostPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("ReportLostPage accessibility and keyboard alternatives", () => {
  it("announces the active step and validates before leaving it", async () => {
    const user = userEvent.setup();
    renderPage();

    expect(screen.getByRole("status")).toHaveTextContent("Paso 1 de 3");
    await user.clear(screen.getByLabelText("¿Cuándo fue visto por última vez?"));
    await user.click(screen.getByRole("button", { name: /siguiente/i }));

    expect(screen.getByRole("status")).toHaveTextContent("Paso 1 de 3");
    expect(screen.getByRole("alert")).toHaveTextContent(/fecha/i);
    expect(screen.getByLabelText("¿Cuándo fue visto por última vez?")).toHaveFocus();
  });

  it("provides a keyboard-accessible coordinate alternative to placing the map pin", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: /introducir ubicación manual/i }));
    await user.type(screen.getByLabelText("Latitud"), "9.9281");
    await user.type(screen.getByLabelText("Longitud"), "-84.0907");
    await user.click(screen.getByRole("button", { name: /usar estas coordenadas/i }));

    expect(screen.getByText(/9\.92810, -84\.09070/)).toBeInTheDocument();
  });
});
