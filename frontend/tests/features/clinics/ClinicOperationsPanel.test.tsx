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
const adjustInventoryLotMutate = vi.fn();
const createSaleMutate = vi.fn();
const registerPaymentMutate = vi.fn();
const closeCashMutate = vi.fn();
const voidSaleMutate = vi.fn();
const saveCrmPreferenceMutate = vi.fn();
const logCrmActivityMutate = vi.fn();
const createCrmTaskMutate = vi.fn();
const completeCrmTaskMutate = vi.fn();

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
        lots: [
          {
            id: "lot-1",
            itemId: "inv-1",
            lotNumber: "RAB-001",
            expiresAt: "2027-09-25",
            initialQuantity: 5,
            availableQuantity: 5,
            unitCostCrc: 1200,
            supplierName: "Proveedor CR",
            locationName: "Principal",
          },
        ],
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
  useAdjustClinicInventoryLot: () => ({
    mutate: adjustInventoryLotMutate,
    isPending: false,
  }),
  useClinicSalesReport: () => ({
    data: {
      totalPaidCrc: 15000,
      byPaymentMethod: { Sinpe: 15000 },
      byService: { "Consulta veterinaria": 15000 },
      byVeterinarian: { "Dra. Mora": 15000 },
    },
  }),
  useCreateClinicSale: () => ({ mutate: createSaleMutate, isPending: false }),
  useRegisterClinicSalePayment: () => ({ mutate: registerPaymentMutate, isPending: false }),
  useCloseClinicCash: () => ({ mutate: closeCashMutate, isPending: false }),
  useVoidClinicSale: () => ({ mutate: voidSaleMutate, isPending: false }),
  useClinicFinanceMembers: () => ({ data: [] }),
  useClinicStaffMembers: () => ({ data: [] }),
  useGrantClinicStaffMember: () => ({ mutate: vi.fn(), isPending: false }),
  useRevokeClinicStaffMember: () => ({ mutate: vi.fn(), isPending: false }),
  useGrantClinicFinanceMember: () => ({ mutate: vi.fn(), isPending: false }),
  useRevokeClinicFinanceMember: () => ({ mutate: vi.fn(), isPending: false }),
  useClinicCrmDashboard: () => ({
    data: {
      preferences: [],
      recentActivities: [],
      openTasks: [
        {
          id: "crm-task-1",
          petId: "pet-1",
          petName: "Max",
          ownerUserId: "owner-1",
          ownerName: "Ana",
          type: "FollowUpTreatment",
          status: "Open",
          dueDate: "2026-09-25",
          title: "Llamar al tutor",
          notes: null,
        },
      ],
      segments: [{ key: "vaccines-due", label: "Vacunas o controles próximos", count: 1, petIds: ["pet-1"] }],
    },
  }),
  useClinicCommunicationTemplates: () => ({ data: [{ key: "clinical-follow-up", label: "Seguimiento clínico" }] }),
  useSendClinicCommunicationTemplate: () => ({ mutate: vi.fn(), isPending: false }),
  useUpsertClinicCommunicationPreference: () => ({ mutate: saveCrmPreferenceMutate, isPending: false }),
  useLogClinicCommunicationActivity: () => ({ mutate: logCrmActivityMutate, isPending: false }),
  useCreateClinicCrmTask: () => ({ mutate: createCrmTaskMutate, isPending: false }),
  useCompleteClinicCrmTask: () => ({ mutate: completeCrmTaskMutate, isPending: false }),
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
    expect(screen.getByText(/Cobros registrados/)).toBeInTheDocument();
    expect(screen.getByText("Comunicación y CRM clínico")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Enviar correo clínico" })).toBeInTheDocument();
    expect(screen.getByText("Personal de caja")).toBeInTheDocument();
    expect(screen.getByText("Equipo clínico")).toBeInTheDocument();
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

  it("groups internal tasks by role and shows CR-local due dates with priority ordering", async () => {
    renderWithProviders(<ClinicOperationsPanel />);

    expect(await screen.findByText("Tareas internas por rol")).toBeInTheDocument();
    expect(screen.getByLabelText("Filtrar tareas por rol")).toBeInTheDocument();
    expect(screen.getByText("Urgente")).toBeInTheDocument();
    expect(screen.getAllByText("25/09/2026").length).toBeGreaterThan(0);

    await userEvent.selectOptions(screen.getByLabelText("Filtrar tareas por rol"), "Reception");

    expect((screen.getByLabelText("Filtrar tareas por rol") as HTMLSelectElement).value).toBe("Reception");
    expect(screen.getAllByText("Recepción").length).toBeGreaterThan(0);
  });
});
