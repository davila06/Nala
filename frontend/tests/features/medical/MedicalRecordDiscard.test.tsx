import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { MedicalHistoryTab } from "@/features/medical/components/MedicalHistoryTab";
import { renderWithProviders } from "../../utils/renderWithProviders";

const consentState = vi.hoisted(() => ({ granted: true, request: vi.fn() }));
const recordsState = vi.hoisted(() => ({ show: false }));

vi.mock("@/features/medical/hooks/useMedical", () => ({
  useMedicalHistory: () => ({
    data: {
      pages: [
        {
          records: recordsState.show
            ? [
                {
                  id: "record-test",
                  type: "Checkup",
                  date: "2026-09-28",
                  description: "Control general",
                  source: "Owner",
                },
              ]
            : [],
          totalCount: recordsState.show ? 1 : 0,
          accessTier: recordsState.show ? "plus_preview" : "explorador",
        },
      ],
    },
    isLoading: false,
    hasNextPage: false,
  }),
  useMedicalCount: () => ({ data: { totalRecords: 0 } }),
  useVetReminders: () => ({ data: [], isLoading: false }),
  useExportMedicalPdf: () => ({ mutate: vi.fn(), isPending: false }),
  useAddMedicalRecord: () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteMedicalRecord: () => ({ mutate: vi.fn(), isPending: false }),
  useUpdateMedicalRecord: () => ({ mutate: vi.fn(), isPending: false }),
  usePetSanitaryIdentity: () => ({ data: null }),
  useUpdatePetSanitaryIdentity: () => ({ mutate: vi.fn(), isPending: false }),
  useClinicAccessLog: () => ({ data: [] }),
}));
vi.mock("@/features/auth/hooks/useProfile", () => ({
  useMyProfile: () => ({ data: { hasHealthDataConsent: consentState.granted } }),
  useGrantHealthDataConsent: () => ({ mutate: consentState.request, isPending: false }),
}));
vi.mock("@/features/clinics/hooks/useCertificates", () => ({
  useCertificatesForPet: () => ({ data: { pages: [] }, isLoading: false, isError: false, hasNextPage: false }),
  useDownloadCertificatePdf: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));
vi.mock("@/features/medical/components/WeightTrendChart", () => ({ WeightTrendChart: () => null }));
vi.mock("@/features/medical/components/HealthScoreCard", () => ({ HealthScoreCard: () => null }));
vi.mock("@/features/medical/components/PetClinicAccessManager", () => ({ PetClinicAccessManager: () => null }));

describe("MedicalHistoryTab", () => {
  it("protects unsaved changes when closing the existing-record drawer", () => {
    recordsState.show = true;
    renderWithProviders(<MedicalHistoryTab petId="pet-test" />);
    fireEvent.click(screen.getByRole("button", { name: "Editar: Control general" }));
    fireEvent.change(screen.getByRole("textbox", { name: /descripción/i }), {
      target: { value: "Control actualizado" },
    });
    fireEvent.click(
      screen
        .getByRole("dialog", { name: "Editar registro médico" })
        .querySelector<HTMLButtonElement>('button[aria-label="Cerrar"]')!,
    );
    expect(screen.getByRole("dialog", { name: "Descartar cambios del expediente" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: /descripción/i })).toHaveValue("Control actualizado");
    fireEvent.click(screen.getByRole("button", { name: "Seguir editando" }));
    expect(screen.getByRole("textbox", { name: /descripción/i })).toHaveValue("Control actualizado");
    fireEvent.click(
      screen
        .getByRole("dialog", { name: "Editar registro médico" })
        .querySelector<HTMLButtonElement>('button[aria-label="Cerrar"]')!,
    );
    fireEvent.click(screen.getByRole("button", { name: "Descartar cambios" }));
    fireEvent.click(screen.getByRole("button", { name: "Editar: Control general" }));
    expect(screen.getByRole("textbox", { name: /descripción/i })).toHaveValue("Control general");
    recordsState.show = false;
  });

  it("requires explicit consent before exposing the medical record form", () => {
    recordsState.show = false;
    consentState.granted = false;
    consentState.request.mockClear();
    render(<MedicalHistoryTab petId="pet-test" />);
    fireEvent.click(screen.getByRole("button", { name: /registro/i }));
    expect(screen.getByText(/consentimiento explícito/i)).toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: /descripción/i })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Acepto" }));
    expect(consentState.request).toHaveBeenCalledOnce();
    consentState.granted = true;
  });

  it("asks before discarding a changed medical record through Cancel or Close", () => {
    recordsState.show = false;
    consentState.granted = true;
    render(<MedicalHistoryTab petId="pet-test" />);
    fireEvent.click(screen.getByRole("button", { name: /registro/i }));
    fireEvent.change(screen.getByRole("textbox", { name: /descripción/i }), { target: { value: "Texto de prueba" } });
    fireEvent.click(screen.getByRole("button", { name: "Cancelar" }));

    const dialog = screen.getByRole("dialog", { name: "Descartar cambios del expediente" });
    expect(screen.getByRole("textbox", { name: /descripción/i })).toHaveValue("Texto de prueba");
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(screen.getByRole("textbox", { name: /descripción/i })).toHaveValue("Texto de prueba");

    fireEvent.click(screen.getAllByRole("button", { name: "Cerrar" })[0]);
    expect(screen.getByRole("dialog", { name: "Descartar cambios del expediente" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Descartar cambios" }));
    expect(screen.queryByRole("textbox", { name: /descripción/i })).not.toBeInTheDocument();
  });
});
