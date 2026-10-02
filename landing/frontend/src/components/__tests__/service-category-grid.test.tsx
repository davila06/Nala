// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ServiceCategoryGrid } from "../service-category-grid";

describe("ServiceCategoryGrid", () => {
  it("renders an accessible photograph for every public service category", () => {
    const { container } = render(<ServiceCategoryGrid />);

    expect(screen.getAllByRole("heading", { level: 3 })).toHaveLength(8);
    expect(screen.getAllByRole("img")).toHaveLength(8);
    expect(container.querySelectorAll(".service-category-photo")).toHaveLength(8);
    expect(screen.getByRole("heading", { name: "Veterinarias" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Grooming" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Entrenamiento" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Hospedaje" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Paseos" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Cuidado temporal" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Reservas y pagos" })).toBeTruthy();
    expect(screen.getByRole("img", { name: /solicitud de reserva junto a un perro/i })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Registro de prestadores" })).toBeTruthy();
    expect(screen.getByRole("img", { name: /prestadora completa la verificación/i })).toBeTruthy();
  });
});
