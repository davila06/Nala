import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Select } from "@/shared/ui/Input";

describe("Select", () => {
  it("connects its label and hint to the native control", () => {
    render(
      <Select label="Modalidad" hint="Elige cómo se presta el servicio." defaultValue="InPerson">
        <option value="InPerson">Presencial</option>
      </Select>,
    );

    const select = screen.getByRole("combobox", { name: "Modalidad" });
    expect(select).toHaveAttribute("aria-describedby");
    expect(screen.getByText("Elige cómo se presta el servicio.")).toBeInTheDocument();
  });
});
