import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import MunicipalDashboardPage from "@/features/admin/pages/MunicipalDashboardPage";

const recordCapture = vi.fn();

vi.mock("@/features/admin/hooks/useMunicipal", () => ({
  useMunicipalProfile: () => ({
    data: { orgName: "Municipalidad", canton: "Cantón de prueba", allCantons: ["Cantón de prueba"], tier: "Basica" },
    isLoading: false,
  }),
  useCapturedAnimals: () => ({ data: { items: [], total: 0 }, isLoading: false }),
  useRecordCapture: () => ({ mutate: recordCapture, isPending: false }),
  useUpdateCaptureStatus: () => ({ mutate: vi.fn() }),
  useBulkUpdateStatus: () => ({ mutate: vi.fn(), isPending: false }),
  useCantonStats: () => ({ data: null }),
  useRegionalDashboard: () => ({ data: null }),
}));

describe("MunicipalDashboardPage", () => {
  it("confirms the capture before submission and preserves fields when cancelled with Escape", () => {
    recordCapture.mockClear();
    render(<MunicipalDashboardPage />);

    fireEvent.click(screen.getByRole("button", { name: /registrar captura/i }));
    fireEvent.change(screen.getByRole("textbox", { name: /especie/i }), { target: { value: "Perro" } });
    fireEvent.change(screen.getByRole("textbox", { name: /color/i }), { target: { value: "Negro" } });
    fireEvent.click(screen.getByRole("button", { name: "Guardar" }));

    const dialog = screen.getByRole("dialog", { name: "Confirmar captura" });
    expect(dialog).toHaveTextContent("Cantón de prueba");
    expect(recordCapture).not.toHaveBeenCalled();
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(screen.getByRole("textbox", { name: /especie/i })).toHaveValue("Perro");
    expect(recordCapture).not.toHaveBeenCalled();
  });
});
