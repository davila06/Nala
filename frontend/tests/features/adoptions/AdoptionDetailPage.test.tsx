import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import AdoptionDetailPage from "@/features/adoptions/pages/AdoptionDetailPage";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/adoptions/hooks/useAdoptions", () => ({
  useAdoptableAnimal: () => ({
    data: {
      id: "animal-1",
      organizationUserId: "shelter-1",
      organizationName: "Refugio Esperanza",
      name: "Luna",
      species: "Dog",
      breed: null,
      size: "Medium",
      ageCategory: "Adult",
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
      status: "Available",
      source: "Shelter",
      photoUrls: [],
      publishedAt: "2026-09-10T00:00:00Z",
    },
    isLoading: false,
  }),
  useApplyToAdopt: () => ({ mutate: vi.fn(), isPending: false }),
}));

vi.mock("@/features/auth/store/authStore", () => ({
  useAuthStore: () => ({ user: null }),
}));

vi.mock("@/shared/lib/telemetry", () => ({ trackProductEvent: vi.fn() }));

describe("AdoptionDetailPage", () => {
  it("shows a readable publisher section", () => {
    renderWithProviders(<AdoptionDetailPage />, {
      initialEntries: ["/adopciones/animal-1"],
      routePath: "/adopciones/:id",
    });

    expect(
      screen.getByRole("heading", { name: "Publicado por" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Refugio Esperanza")).toBeInTheDocument();
  });
});