import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ClinicTiersModal } from "@/features/clinics/components/ClinicTiersModal";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/pets/hooks/useSubscription", () => ({
  useSubscriptionCatalog: () => ({ data: [] }),
}));

describe("ClinicTiersModal", () => {
  it("shows technical prices without offering unapproved clinical benefits or checkout", () => {
    renderWithProviders(<ClinicTiersModal onClose={() => undefined} />);

    expect(screen.getByText("₡15,000")).toBeInTheDocument();
    expect(screen.getByText("₡35,000")).toBeInTheDocument();
    expect(screen.getByText(/precios técnicos de referencia/i)).toBeInTheDocument();
    expect(screen.queryByText(/soporte prioritario 24\/7/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/gestor de cuenta dedicado/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/certificado veterinario pdf \(próximamente\)/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /activar (plus|partner)/i })).not.toBeInTheDocument();
  });
});
