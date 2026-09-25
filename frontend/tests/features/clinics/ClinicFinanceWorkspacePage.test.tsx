import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ClinicFinanceWorkspacePage from "@/features/clinics/pages/ClinicFinanceWorkspacePage";
import { clinicsApi } from "@/features/clinics/api/clinicsApi";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/clinics/api/clinicsApi", () => ({
  clinicsApi: {
    getFinanceWorkspaces: vi.fn(),
    getStaffSalesReport: vi
      .fn()
      .mockResolvedValue({ totalPaidCrc: 1000, byPaymentMethod: {}, byService: {}, byVeterinarian: {} }),
    getStaffSaleLedger: vi.fn().mockResolvedValue(null),
  },
}));

describe("ClinicFinanceWorkspacePage", () => {
  it("shows collection commands but hides administrator commands from cashier", async () => {
    vi.mocked(clinicsApi.getFinanceWorkspaces).mockResolvedValueOnce([
      { clinicId: "clinic-1", clinicName: "Clinica Norte", role: "Cashier" },
    ]);
    renderWithProviders(<ClinicFinanceWorkspacePage />);

    expect(await screen.findByText("Clinica Norte")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Registrar pago" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Registrar devolución" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Cerrar caja" })).not.toBeInTheDocument();
  });
});
