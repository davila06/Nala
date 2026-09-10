import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import AdoptionDirectoryPage from "@/features/adoptions/pages/AdoptionDirectoryPage";
import { renderWithProviders } from "../../utils/renderWithProviders";

const animal = {
  id: "animal-1",
  organizationUserId: "shelter-1",
  organizationName: "Refugio Esperanza",
  name: "Luna",
  species: "Dog" as const,
  breed: null,
  size: "Medium" as const,
  ageCategory: "Adult" as const,
  ageMonthsApprox: null,
  story: "Busca hogar.",
  requirements: null,
  medicalNotes: null,
  isVaccinated: true,
  isSterilized: true,
  isMicrochipped: false,
  okWithKids: true,
  okWithDogs: true,
  okWithCats: false,
  needsYard: false,
  refLat: 9.9,
  refLng: -84.1,
  refLabel: "San Jose",
  status: "Available" as const,
  photoUrls: [],
  publishedAt: "2026-09-10T00:00:00Z",
};

const cat = {
  ...animal,
  id: "animal-2",
  name: "Milo",
  species: "Cat" as const,
};

let receivedFilters: Record<string, unknown> | undefined;
let receivedMapFilters: Record<string, unknown> | undefined;
const refetch = vi.fn();
let directoryState = {
  isLoading: false,
  isError: false,
  totalPages: 1,
  hasNextPage: false,
};

vi.mock("@/features/adoptions/hooks/useAdoptions", () => ({
  useAdoptableAnimals: (filters: Record<string, unknown>) => {
    receivedFilters = filters;
    return {
      data: {
        items: [animal, cat],
        totalCount: 1,
        totalPages: directoryState.totalPages,
        hasNextPage: directoryState.hasNextPage,
      },
      isLoading: directoryState.isLoading,
      isError: directoryState.isError,
      refetch,
    };
  },
  useAdoptableAnimalsForMap: (filters: Record<string, unknown>) => {
    receivedMapFilters = filters;
    return {
      data: filters.species === "Dog" ? [animal] : [animal, cat],
      isLoading: false,
    };
  },
}));

vi.mock("@/features/adoptions/components/AnimalCard", () => ({
  AnimalCard: ({ animal: currentAnimal }: { animal: typeof animal }) => (
    <div>{currentAnimal.name}</div>
  ),
}));

vi.mock("@/features/adoptions/components/AdoptionFiltersBar", () => ({
  AdoptionFiltersBar: () => <div>Filtros</div>,
}));

vi.mock("@/features/map/components/MapContainer", () => ({
  MapContainer: ({ adoptions }: { adoptions?: (typeof animal)[] }) => (
    <div data-testid="adoptions-map">{adoptions?.map((item) => item.name)}</div>
  ),
}));

vi.mock("@/features/advertising/components/BillboardBanner", () => ({
  BillboardBanner: ({ placement }: { placement: string }) => (
    <div data-testid={`billboard-${placement}`} />
  ),
}));

vi.mock("@/shared/lib/telemetry", () => ({
  trackProductEvent: vi.fn(),
}));

describe("AdoptionDirectoryPage", () => {
  it("shows a retry action when the directory request fails", async () => {
    const user = userEvent.setup();
    directoryState = { ...directoryState, isError: true };
    renderWithProviders(<AdoptionDirectoryPage />);

    expect(
      screen.getByText("No pudimos cargar las adopciones"),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(refetch).toHaveBeenCalledOnce();
  });

  it("links visitors to upcoming adoption fairs and renders its billboard", () => {
    renderWithProviders(<AdoptionDirectoryPage />);

    expect(screen.getByRole("link", { name: "Ver ferias" })).toHaveAttribute(
      "href",
      "/adopciones/ferias",
    );
    expect(
      screen.getByTestId("billboard-AdoptionDirectory"),
    ).toBeInTheDocument();
  });

  it("loads adoption markers only when the map view is selected", async () => {
    const user = userEvent.setup();
    renderWithProviders(<AdoptionDirectoryPage />);

    expect(screen.queryByTestId("adoptions-map")).not.toBeInTheDocument();

    await user.click(screen.getByRole("tab", { name: "Mapa" }));

    expect(screen.getByTestId("adoptions-map")).toHaveTextContent("Luna");
  });

  it("restores URL filters and applies them to map markers", () => {
    renderWithProviders(<AdoptionDirectoryPage />, {
      initialEntries: ["/adopciones?species=Dog&view=map"],
    });

    expect(receivedFilters).toMatchObject({ species: "Dog", page: 1 });
    expect(receivedMapFilters).toMatchObject({ species: "Dog", page: 1 });
    expect(screen.getByRole("tab", { name: "Mapa" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(screen.getByTestId("adoptions-map")).toHaveTextContent("Luna");
    expect(screen.getByTestId("adoptions-map")).not.toHaveTextContent("Milo");
  });

  it("moves the paginated directory to the requested page", async () => {
    const user = userEvent.setup();
    directoryState = { ...directoryState, totalPages: 2, hasNextPage: true };
    renderWithProviders(<AdoptionDirectoryPage />);

    await user.click(screen.getByRole("button", { name: "Siguiente →" }));

    expect(receivedFilters).toMatchObject({ page: 2 });
  });
});
