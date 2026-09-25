import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ClinicOperationsPanel } from "@/features/clinics/components/ClinicOperationsPanel";
import { renderWithProviders } from "../../utils/renderWithProviders";

const updateStatusMutate = vi.fn();
const rescheduleMutate = vi.fn();
const createBlockMutate = vi.fn();
const updateBlockMutate = vi.fn();
const deleteBlockMutate = vi.fn();
const downloadAuditMutate = vi.fn();
const createConsultationMutate = vi.fn();
const closeConsultationMutate = vi.fn();
const uploadConsultationAttachmentMutate = vi.fn();
const downloadPrescriptionMutate = vi.fn();
const addInventoryItemMutate = vi.fn();
const receiveInventoryLotMutate = vi.fn();

vi.mock("@/features/clinics/hooks/useClinics", () => ({
  useClinicAgenda: () => ({
    data: [
      {
        appointmentId: "appt-1",
        clinicId: "clinic-1",
        veterinarianId: "vet-1",
        veterinarianName: "Dra. Ana Mora",
        petId: "pet-1",
        petName: "Nala",
        startsAt: "2026-09-25T15:00:00.000Z",
        endsAt: "2026-09-25T15:30:00.000Z",
        status: "Scheduled",
      },
    ],
    isLoading: false,
  }),
  useUpdateClinicAppointmentStatus: () => ({
    mutate: updateStatusMutate,
    isPending: false,
  }),
  useRescheduleClinicAppointment: () => ({
    mutate: rescheduleMutate,
    isPending: false,
  }),
  useCreateClinicScheduleBlock: () => ({
    mutate: createBlockMutate,
    isPending: false,
  }),
  useClinicScheduleBlocks: () => ({
    data: [
      {
        blockId: "block-1",
        clinicId: "clinic-1",
        veterinarianId: "vet-1",
        veterinarianName: "Dra. Ana Mora",
        startsAt: "2026-09-25T18:00:00.000Z",
        endsAt: "2026-09-25T19:00:00.000Z",
        reason: "Cirugia",
      },
    ],
    isLoading: false,
  }),
  useUpdateClinicScheduleBlock: () => ({
    mutate: updateBlockMutate,
    isPending: false,
  }),
  useDeleteClinicScheduleBlock: () => ({
    mutate: deleteBlockMutate,
    isPending: false,
  }),
  useClinicAgendaAudit: () => ({
    data: [
      {
        id: "audit-1",
        adminUserId: "clinic-user-1",
        action: "ClinicAppointmentStatusChanged",
        entityType: "VeterinarianAppointment",
        entityId: "appt-1",
        details: "Confirmed",
        performedAt: "2026-09-25T15:05:00.000Z",
      },
    ],
    isLoading: false,
  }),
  useDownloadClinicAgendaAuditCsv: () => ({
    mutate: downloadAuditMutate,
    isPending: false,
  }),
  useCreateClinicalConsultation: () => ({
    mutate: createConsultationMutate,
    isPending: false,
  }),
  useCloseClinicalConsultation: () => ({
    mutate: closeConsultationMutate,
    isPending: false,
  }),
  useClinicalConsultationTemplates: () => ({
    data: [
      {
        key: "checkup",
        label: "Consulta general",
        reason: "Control general",
        subjective: "S",
        objective: "O",
        assessment: "A",
        plan: "P",
        diagnosis: "Dx",
        treatment: "Tx",
        ownerSummary: "Resumen",
        prescriptionInstructions: null,
      },
    ],
  }),
  useUploadClinicalConsultationAttachment: () => ({
    mutate: uploadConsultationAttachmentMutate,
    isPending: false,
  }),
  useDownloadClinicalConsultationPrescription: () => ({
    mutate: downloadPrescriptionMutate,
    isPending: false,
  }),
  useClinicInventory: () => ({
    data: [
      {
        id: "inv-1",
        clinicId: "clinic-1",
        name: "Vacuna rabia",
        type: "Vaccine",
        unit: "unidad",
        minimumStock: 2,
        totalAvailable: 5,
        isBelowMinimum: false,
        isActive: true,
        lots: [],
      },
    ],
  }),
  useClinicInventoryValuation: () => ({
    data: {
      clinicId: "clinic-1",
      totalValueCrc: 6000,
      totalUnits: 5,
      lines: [],
      byLocation: [{ locationName: "Principal", availableQuantity: 5, valueCrc: 6000 }],
    },
  }),
  useAddClinicInventoryItem: () => ({
    mutate: addInventoryItemMutate,
    isPending: false,
  }),
  useReceiveClinicInventoryLot: () => ({
    mutate: receiveInventoryLotMutate,
    isPending: false,
  }),
}));

vi.mock("@/features/clinics/api/certificateApi", () => ({
  certificateApi: {
    getMyVeterinarians: vi.fn().mockResolvedValue([
      {
        id: "vet-1",
        fullName: "Dra. Ana Mora",
        licenseNumber: "VET-999",
        status: "Authorized",
        isActive: true,
        permissions: ["medical:read", "medical:write", "certificates:issue"],
      },
    ]),
    setVeterinarianPermissions: vi.fn(),
    scheduleAppointment: vi.fn(),
  },
}));

describe("ClinicOperationsPanel", () => {
  it("shows the daily agenda and advances appointment status", async () => {
    renderWithProviders(<ClinicOperationsPanel />);

    expect(await screen.findByText("Nala")).toBeInTheDocument();
    expect(screen.getByText("Cirugia")).toBeInTheDocument();
    expect(screen.getByText("ClinicAppointmentStatusChanged")).toBeInTheDocument();
    expect(screen.getAllByText("Vacuna rabia").length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Dra\. Ana Mora/).length).toBeGreaterThan(0);

    await userEvent.click(screen.getByRole("button", { name: /Pasar a Confirmada/i }));

    expect(updateStatusMutate).toHaveBeenCalledWith(
      { appointmentId: "appt-1", status: "Confirmed" },
      expect.objectContaining({
        onSuccess: expect.any(Function),
        onError: expect.any(Function),
      }),
    );
  });
});
