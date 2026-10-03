import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AnimalCard } from "@/features/adoptions/components/AnimalCard";
import { renderWithProviders } from "../../utils/renderWithProviders";

function renderAnimalCard(status: "Available" | "Adopted" = "Available") {
  return renderWithProviders(
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
        status,
        source: "Shelter",
        photoUrls: [],
        publishedAt: "2026-09-10T00:00:00Z",
      }}
    />,
  );
}

describe("AnimalCard", () => {
  it("shows who published the adoption", () => {
    renderAnimalCard();

    expect(screen.getByText("Publicado por")).toBeInTheDocument();
    expect(screen.getByText("Refugio Esperanza")).toBeInTheDocument();
  });

  it("guides visitors to the complete adoption notes", () => {
    renderAnimalCard();

    expect(
      screen.getByRole("link", {
        name: "Ver historia, requisitos y notas de Luna",
      }),
    ).toHaveAttribute("href", "/adopciones/animal-1");
  });

  it("uses a readable text color for the adopted status badge", () => {
    renderAnimalCard("Adopted");

    expect(screen.getByText("Adoptado ✓")).toHaveClass("text-sand-900");
  });
});
