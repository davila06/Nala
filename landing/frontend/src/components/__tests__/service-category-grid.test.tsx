// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ServiceCategoryGrid } from "../service-category-grid";

describe("ServiceCategoryGrid", () => {
  it("renders an accessible photograph for every public service category", () => {
    const { container } = render(<ServiceCategoryGrid />);

    expect(screen.getAllByRole("heading", { level: 3 })).toHaveLength(6);
    expect(screen.getAllByRole("img")).toHaveLength(6);
    expect(container.querySelectorAll(".service-category-photo")).toHaveLength(6);
    expect(screen.getByRole("heading", { name: "Veterinarias" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Grooming" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Entrenamiento" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Hospedaje" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Paseos" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Cuidado temporal" })).toBeTruthy();
  });
});
