// @vitest-environment jsdom

import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { PublicPlansCatalog } from "../public-plans-catalog";

describe("public plans catalog", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it("fails closed when no public API is configured", () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "");

    render(<PublicPlansCatalog />);

    expect(screen.getByText(/catálogo público no está configurado/i)).toBeTruthy();
  });

  it("renders only plans returned by the public API", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://api.example.test");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => [
          {
            id: "plan-1",
            tier: "UserPlus",
            displayName: "Plan Plus",
            description: "Perfil y recuperación",
            monthlyPriceCrc: 3000,
          },
        ],
      }),
    );

    render(<PublicPlansCatalog />);

    await waitFor(() => expect(screen.getByText("Plan Plus")).toBeTruthy());
    expect(screen.getByText(/₡3\s?000 \/ mes/i)).toBeTruthy();
    expect(screen.getByText(/Hasta 3 mascotas, QR e identidad digital/i)).toBeTruthy();
    expect(fetch).toHaveBeenCalledWith(
      "https://api.example.test/api/catalog/subscription-plans",
      expect.objectContaining({ headers: { Accept: "application/json" } }),
    );
  });

  it("keeps plan essentials visible without hover and clarifies GPS hardware limits", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://api.example.test");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => [
          {
            id: "plan-1",
            tier: "UserPlus",
            displayName: "Plan Plus",
            monthlyPriceCrc: 3000,
          },
        ],
      }),
    );
    const { container } = render(<PublicPlansCatalog />);

    await waitFor(() => expect(screen.getByText("Plan Plus")).toBeTruthy());
    const card = container.querySelector(".catalog-plan");
    const front = card?.querySelector(".catalog-plan-front");

    expect(front?.textContent).toMatch(/3 mascotas/i);
    expect(front?.textContent).toMatch(/3 casos perdidos/i);
      expect(card?.textContent).toMatch(/no incluye hardware ni garantiza un proveedor compatible/i);
  });

  it("shows a compact household summary without listing business plans", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "https://api.example.test");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => [
          { id: "user-plan", tier: "UserFamilia", displayName: "Plan Familia", monthlyPriceCrc: 5000 },
          { id: "clinic-plan", tier: "ClinicPartner", displayName: "Plan Clínica", monthlyPriceCrc: 15000 },
        ],
      }),
    );

    const { container } = render(<PublicPlansCatalog audience="household" variant="summary" />);

    await waitFor(() => expect(screen.getByRole("heading", { name: "Plan Familia" })).toBeTruthy());
    expect(screen.getByText(/₡5\s?000 \/ mes/i)).toBeTruthy();
    expect(screen.getByText(/25 mascotas activas, miembros de familia/i)).toBeTruthy();
    expect(screen.queryByText("Plan Clínica")).toBeNull();
    expect(container.querySelectorAll(".home-plan-row")).toHaveLength(1);
  });
});
