import { fireEvent, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ClinicFinanceWorkspacePage from "@/features/clinics/pages/ClinicFinanceWorkspacePage";
import { clinicsApi } from "@/features/clinics/api/clinicsApi";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/clinics/api/clinicsApi", () => ({
  clinicsApi: {
    getFinanceWorkspaces: vi.fn(),
    getActiveClinicSite: vi.fn().mockResolvedValue(null),
    selectActiveClinicSite: vi.fn().mockResolvedValue({ clinicId: "clinic-1" }),
    getStaffSalesReport: vi
      .fn()
      .mockResolvedValue({ totalPaidCrc: 1000, byPaymentMethod: {}, byService: {}, byVeterinarian: {} }),
    getStaffSaleLedger: vi.fn().mockResolvedValue(null),
    closeStaffCash: vi.fn().mockResolvedValue({}),
  },
}));

describe("ClinicFinanceWorkspacePage", () => {
  it("requires an accessible confirmation before closing cash for the selected clinic and day", async () => {
    vi.mocked(clinicsApi.getFinanceWorkspaces).mockResolvedValueOnce([
      { clinicId: "clinic-1", clinicName: "Clinica Norte", role: "Administrator" },
    ]);
    renderWithProviders(<ClinicFinanceWorkspacePage />);

    fireEvent.click(await screen.findByRole("button", { name: "Cerrar caja" }));
    const dialog = screen.getByRole("dialog", { name: "Confirmar cierre de caja" });
    expect(dialog).toHaveTextContent("Clinica Norte");
    expect(clinicsApi.closeStaffCash).not.toHaveBeenCalled();
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(clinicsApi.closeStaffCash).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Cerrar caja" }));
    fireEvent.click(screen.getByRole("button", { name: "Confirmar cierre" }));
    await waitFor(() => expect(clinicsApi.closeStaffCash).toHaveBeenCalledOnce());
  });

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
