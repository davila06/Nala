import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AnimalCard } from "@/features/adoptions/components/AnimalCard";
import { renderWithProviders } from "../../utils/renderWithProviders";

describe("AnimalCard", () => {
  it("shows who published the adoption", () => {
    renderWithProviders(
      <AnimalCard
        animal={{
          id: "animal-1",
          organizationUserId: "shelter-1",
          organizationName: "Refugio Esperanza",
          name: "Luna",
          species: "Dog",
          breed: "Mestiza",
          size: "Medium",
          ageCategory: "Adult",
          ageMonthsApprox: null,
          story: "Luna busca una familia tranquila y comprometida.",
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
          refLabel: "San José",
          status: "Available",
          source: "Shelter",
          photoUrls: [],
          publishedAt: "2026-09-10T00:00:00Z",
        }}
      />,
    );

    expect(screen.getByText("Publicado por")).toBeInTheDocument();
    expect(screen.getByText("Refugio Esperanza")).toBeInTheDocument();
  });

  it("guides visitors to the complete adoption notes", () => {
    renderWithProviders(
      <AnimalCard
        animal={{
          id: "animal-1",
          organizationUserId: "shelter-1",
          organizationName: "Refugio Esperanza",
          name: "Luna",
          species: "Dog",
          breed: "Mestiza",
          size: "Medium",
          ageCategory: "Adult",
          ageMonthsApprox: null,
          story: "Luna busca una familia tranquila y comprometida.",
          requirements: "Conocerla antes de adoptar.",
          medicalNotes: "Vacunas al día.",
          isVaccinated: true,
          isSterilized: true,
          isMicrochipped: false,
          okWithKids: true,
          okWithDogs: true,
          okWithCats: false,
          needsYard: false,
          refLat: 9.9,
          refLng: -84.1,
          refLabel: "San José",
          status: "Available",
          source: "Shelter",
          photoUrls: [],
          publishedAt: "2026-09-10T00:00:00Z",
        }}
      />,
    );

    expect(
      screen.getByRole("link", {
        name: "Ver historia, requisitos y notas de Luna",
      }),
    ).toHaveAttribute("href", "/adopciones/animal-1");
  });
});
