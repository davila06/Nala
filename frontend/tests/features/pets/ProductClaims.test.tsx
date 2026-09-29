import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { NfcSetupGuide } from "@/features/bundles/components/NfcSetupGuide";
import { BundleOrderModal } from "@/features/bundles/components/BundleOrderModal";
import { CollarGpsTab } from "@/features/pets/components/CollarGpsTab";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/pets/hooks/useCollar", () => ({
  useCollarStatus: () => ({ data: null, isLoading: false }),
  useCollarHistory: () => ({ data: null, isFetching: false }),
}));
vi.mock("@/features/bundles/hooks/useBundles", () => ({
  useMyBundleOrders: () => ({
    data: [
      {
        id: "order-test",
        productType: "NfcQrCombo",
        productTypeLabel: "NFC y QR",
        createdAt: "2026-09-28",
        status: "Delivered",
        statusLabel: "Entregado",
      },
    ],
    isLoading: false,
  }),
  useReportBundlePayment: () => ({ mutate: vi.fn() }),
  useCancelBundleOrder: () => ({ mutate: vi.fn() }),
}));
vi.mock("@/features/payments/hooks/usePaymentProfiles", () => ({
  useChargeCard: () => ({ mutateAsync: vi.fn() }),
}));

describe("product capability claims", () => {
  it("labels the delivered NFC order shortcut as an external URL writing guide", () => {
    render(<BundleOrderModal />);
    expect(screen.getByText(/guía para escribir la URL en un chip NFC compatible/i)).toBeInTheDocument();
    expect(screen.queryByText(/activar el collar NFC/i)).not.toBeInTheDocument();
  });

  it("uses a real profile URL and states NFC writing and reading depend on compatible hardware", () => {
    render(<NfcSetupGuide isOpen onClose={vi.fn()} />);
    fireEvent.click(screen.getByRole("button", { name: /siguiente/i }));

    expect(screen.queryByText(/\[id-de-tu-mascota\]/)).not.toBeInTheDocument();
    expect(screen.getByText(/copia el enlace real de tu perfil público/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /siguiente/i }));
    expect(screen.getByText(/chip nfc compatible/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /siguiente/i }));
    expect(screen.getByText(/depende del modelo y la configuración del teléfono/i)).toBeInTheDocument();
  });

  it("does not promise real-time GPS without a registered reporting device", () => {
    renderWithProviders(<CollarGpsTab petId="pet-test" isOwner={false} />);
    expect(screen.getByText(/última posición reportada/i)).toBeInTheDocument();
    expect(screen.queryByText(/posición en tiempo real/i)).not.toBeInTheDocument();
  });
});
