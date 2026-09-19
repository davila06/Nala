import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { BottomNav } from "@/app/layout/BottomNav";
import { renderWithProviders } from "../../utils/renderWithProviders";

describe("BottomNav", () => {
  it("exposes only the four primary product modes", () => {
    renderWithProviders(<BottomNav />, { initialEntries: ["/dashboard"] });

    const links = screen.getAllByRole("link");
    expect(links).toHaveLength(4);
    expect(links.map((link) => link.textContent)).toEqual(["Mascota", "Encontrar", "Salud", "Red"]);
  });
});
