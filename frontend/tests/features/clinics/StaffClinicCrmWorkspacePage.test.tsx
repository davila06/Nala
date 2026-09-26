import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import StaffClinicCrmWorkspacePage from "@/features/clinics/pages/StaffClinicCrmWorkspacePage";
import { clinicsApi } from "@/features/clinics/api/clinicsApi";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/clinics/api/clinicsApi", () => ({
  clinicsApi: {
    getStaffWorkspaces: vi.fn(),
    getFinanceWorkspaces: vi.fn().mockResolvedValue([]),
    getStaffCrmDashboard: vi.fn(),
    createStaffCrmTask: vi.fn(),
    completeStaffCrmTask: vi.fn(),
  },
}));

describe("StaffClinicCrmWorkspacePage", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows authorized role tasks and completes them through the clinic-scoped API", async () => {
    vi.mocked(clinicsApi.getStaffWorkspaces).mockResolvedValueOnce([
      { clinicId: "clinic-1", clinicName: "Clinica Norte", role: "Receptionist" },
    ]);
    vi.mocked(clinicsApi.getStaffCrmDashboard).mockResolvedValueOnce({
      preferences: [],
      recentActivities: [],
      openTasks: [
        {
          id: "task-1",
          petId: "pet-1",
          petName: "Nala",
          ownerUserId: "owner-1",
          ownerName: "Ana",
          type: "ConfirmAppointment",
          assignedRole: "Receptionist",
          assignedToUserId: "staff-1",
          assignedToName: "María",
          priority: "High",
          status: "Open",
          dueDate: "2026-09-25",
          title: "Confirmar cita",
          notes: null,
        },
      ],
      segments: [],
    });
    vi.mocked(clinicsApi.completeStaffCrmTask).mockResolvedValueOnce(undefined);
    renderWithProviders(<StaffClinicCrmWorkspacePage />);

    expect(await screen.findByRole("heading", { name: "Confirmar cita" })).toBeInTheDocument();
    expect(screen.getAllByText("Recepción").length).toBeGreaterThan(1);
    expect(screen.getByText("Vence 25/09/2026")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Completar tarea" }));

    expect(clinicsApi.completeStaffCrmTask).toHaveBeenCalledWith("clinic-1", "task-1");
  });

  it("shows assistant operational tasks without exposing communication data", async () => {
    vi.mocked(clinicsApi.getStaffWorkspaces).mockResolvedValueOnce([
      { clinicId: "clinic-2", clinicName: "Clinica Sur", role: "Assistant" },
    ]);
    vi.mocked(clinicsApi.getStaffCrmDashboard).mockResolvedValueOnce({
      preferences: [],
      recentActivities: [],
      openTasks: [
        {
          id: "task-assistant",
          petId: null,
          petName: null,
          ownerUserId: null,
          ownerName: null,
          type: "ReviewInventory",
          assignedRole: "Assistant",
          assignedToUserId: "assistant-1",
          assignedToName: "Luis",
          priority: "High",
          status: "Open",
          dueDate: "2026-09-25",
          title: "Revisar inventario",
          notes: null,
        },
      ],
      segments: [],
    });
    renderWithProviders(<StaffClinicCrmWorkspacePage />);

    expect(await screen.findByRole("heading", { name: "Revisar inventario" })).toBeInTheDocument();
    expect(screen.getAllByText("Asistencia").length).toBeGreaterThan(1);
  });

  it("opens the cashier queue from finance membership", async () => {
    vi.mocked(clinicsApi.getStaffWorkspaces).mockResolvedValueOnce([]);
    vi.mocked(clinicsApi.getFinanceWorkspaces).mockResolvedValueOnce([
      { clinicId: "clinic-cash", clinicName: "Clinica Caja", role: "Cashier" },
    ]);
    vi.mocked(clinicsApi.getStaffCrmDashboard).mockResolvedValueOnce({
      preferences: [],
      recentActivities: [],
      openTasks: [],
      segments: [],
    });
    renderWithProviders(<StaffClinicCrmWorkspacePage />);

    expect(await screen.findByText("Caja")).toBeInTheDocument();
    expect(clinicsApi.getStaffCrmDashboard).toHaveBeenCalledWith("clinic-cash", expect.any(String));
    expect(screen.getByRole("option", { name: "Gestionar cobro" })).toBeInTheDocument();
  });

  it("creates a role-allowed task through the selected clinic workspace", async () => {
    vi.mocked(clinicsApi.getStaffWorkspaces).mockResolvedValueOnce([
      { clinicId: "clinic-3", clinicName: "Clinica Este", role: "Receptionist" },
    ]);
    vi.mocked(clinicsApi.getStaffCrmDashboard).mockResolvedValue({
      preferences: [],
      recentActivities: [],
      openTasks: [],
      segments: [],
    });
    vi.mocked(clinicsApi.createStaffCrmTask).mockResolvedValueOnce({ taskId: "new-task" });
    const user = userEvent.setup();
    renderWithProviders(<StaffClinicCrmWorkspacePage />);

    await screen.findByText("Pendientes del rol");
    await user.type(screen.getByLabelText("ID de mascota para tarea"), "pet-3");
    await user.type(screen.getByLabelText("Título de tarea interna"), "Llamar al tutor");
    await user.click(screen.getByRole("button", { name: "Crear tarea" }));

    await waitFor(() =>
      expect(clinicsApi.createStaffCrmTask).toHaveBeenCalledWith(
        "clinic-3",
        expect.objectContaining({ petId: "pet-3", type: "CallClient", title: "Llamar al tutor" }),
      ),
    );
  });

  it("assigns manager-created operational work to the task's owning role", async () => {
    vi.mocked(clinicsApi.getStaffWorkspaces).mockResolvedValueOnce([]);
    vi.mocked(clinicsApi.getFinanceWorkspaces).mockResolvedValueOnce([
      { clinicId: "clinic-manager", clinicName: "Clinica Central", role: "Administrator" },
    ]);
    vi.mocked(clinicsApi.getStaffCrmDashboard).mockResolvedValueOnce({
      preferences: [],
      recentActivities: [],
      openTasks: [],
      segments: [],
    });
    vi.mocked(clinicsApi.createStaffCrmTask).mockResolvedValueOnce({ taskId: "assistant-task" });
    const user = userEvent.setup();
    renderWithProviders(<StaffClinicCrmWorkspacePage />);

    await screen.findByText("Pendientes del rol");
    await user.selectOptions(screen.getByLabelText("Tipo de tarea interna"), "PrepareConsultation");
    await user.type(screen.getByLabelText("Título de tarea interna"), "Preparar consultorio");
    await user.click(screen.getByRole("button", { name: "Crear tarea" }));

    await waitFor(() =>
      expect(clinicsApi.createStaffCrmTask).toHaveBeenCalledWith(
        "clinic-manager",
        expect.objectContaining({ type: "PrepareConsultation", assignedRole: "Assistant", petId: null }),
      ),
    );
  });
});
