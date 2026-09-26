import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import {
  clinicsApi,
  type ClinicCrmTaskPriority,
  type ClinicCrmTaskType,
  type ClinicFinanceWorkspaceDto,
  type ClinicInternalTaskRole,
  type ClinicStaffWorkspaceDto,
} from "../api/clinicsApi";
import { daysFromClinicToday, formatCostaRicaDate, getClinicDateInputValue } from "../clinicDateTime";

const TASK_TYPES_BY_ROLE: Record<ClinicInternalTaskRole, ClinicCrmTaskType[]> = {
  Receptionist: ["CallClient", "ConfirmAppointment"],
  Veterinarian: ["FollowUpTreatment", "SendDocument", "Reactivation"],
  Assistant: ["PrepareConsultation", "ReviewInventory"],
  Cashier: ["CollectPayment"],
  Manager: [
    "CallClient",
    "ConfirmAppointment",
    "FollowUpTreatment",
    "SendDocument",
    "CollectPayment",
    "Reactivation",
    "PrepareConsultation",
    "ReviewInventory",
    "ProcessRefund",
    "CloseCash",
    "ReviewOperations",
  ],
};

const TASK_OWNER_ROLE_BY_TYPE: Record<ClinicCrmTaskType, ClinicInternalTaskRole> = {
  CallClient: "Receptionist",
  ConfirmAppointment: "Receptionist",
  FollowUpTreatment: "Veterinarian",
  SendDocument: "Veterinarian",
  Reactivation: "Veterinarian",
  PrepareConsultation: "Assistant",
  ReviewInventory: "Assistant",
  CollectPayment: "Cashier",
  ProcessRefund: "Manager",
  CloseCash: "Manager",
  ReviewOperations: "Manager",
};

const TASK_TYPE_LABELS: Record<ClinicCrmTaskType, string> = {
  CallClient: "Llamar al cliente",
  ConfirmAppointment: "Confirmar cita",
  FollowUpTreatment: "Seguimiento de tratamiento",
  SendDocument: "Enviar documento",
  CollectPayment: "Gestionar cobro",
  Reactivation: "Reactivar cliente",
  PrepareConsultation: "Preparar consulta",
  ReviewInventory: "Revisar inventario",
  ProcessRefund: "Procesar devolución",
  CloseCash: "Cerrar caja",
  ReviewOperations: "Revisar operación diaria",
};

const PRIORITY_LABELS: Record<ClinicCrmTaskPriority, string> = {
  Urgent: "Urgente",
  High: "Alta",
  Normal: "Normal",
  Low: "Baja",
};

const INTERNAL_ROLE_LABELS: Record<ClinicInternalTaskRole, string> = {
  Receptionist: "Recepción",
  Veterinarian: "Veterinario",
  Assistant: "Asistencia",
  Cashier: "Caja",
  Manager: "Gerencia",
};

interface TaskWorkspace {
  clinicId: string;
  clinicName: string;
  roles: ClinicInternalTaskRole[];
}

function buildTaskWorkspaces(staff: ClinicStaffWorkspaceDto[], finance: ClinicFinanceWorkspaceDto[]): TaskWorkspace[] {
  const byClinic = new Map<string, TaskWorkspace>();
  for (const workspace of staff) {
    if (workspace.role === "ReadOnly") continue;
    byClinic.set(workspace.clinicId, {
      clinicId: workspace.clinicId,
      clinicName: workspace.clinicName,
      roles: [workspace.role],
    });
  }
  for (const workspace of finance) {
    const role: ClinicInternalTaskRole = workspace.role === "Cashier" ? "Cashier" : "Manager";
    const existing = byClinic.get(workspace.clinicId);
    if (existing) {
      if (!existing.roles.includes(role)) existing.roles.push(role);
    } else {
      byClinic.set(workspace.clinicId, {
        clinicId: workspace.clinicId,
        clinicName: workspace.clinicName,
        roles: [role],
      });
    }
  }
  return [...byClinic.values()].sort((left, right) => left.clinicName.localeCompare(right.clinicName));
}

function staffRoleLabel(roles: ClinicInternalTaskRole[]) {
  return roles.map((role) => INTERNAL_ROLE_LABELS[role]).join(" · ");
}

function taskPriorityLabel(dueDate: string, priority: ClinicCrmTaskPriority) {
  const days = daysFromClinicToday(dueDate);
  if (days < 0) return "Vencida";
  return PRIORITY_LABELS[priority];
}

export default function StaffClinicCrmWorkspacePage() {
  const queryClient = useQueryClient();
  const [clinicId, setClinicId] = useState("");
  const [petId, setPetId] = useState("");
  const [taskType, setTaskType] = useState<ClinicCrmTaskType>("CallClient");
  const [dueDate, setDueDate] = useState(getClinicDateInputValue);
  const [title, setTitle] = useState("");
  const [notes, setNotes] = useState("");
  const [priority, setPriority] = useState<ClinicCrmTaskPriority>("Normal");
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const { data: staffWorkspaces = [], isLoading: isLoadingStaffWorkspaces } = useQuery({
    queryKey: ["clinics", "staff-workspaces"],
    queryFn: clinicsApi.getStaffWorkspaces,
  });
  const { data: financeWorkspaces = [], isLoading: isLoadingFinanceWorkspaces } = useQuery({
    queryKey: ["clinics", "finance-workspaces"],
    queryFn: clinicsApi.getFinanceWorkspaces,
  });
  const workspaces = buildTaskWorkspaces(staffWorkspaces, financeWorkspaces);
  const isLoadingWorkspaces = isLoadingStaffWorkspaces || isLoadingFinanceWorkspaces;
  const selectedWorkspace = workspaces.find((workspace) => workspace.clinicId === clinicId) ?? workspaces[0];
  const activeClinicId = selectedWorkspace?.clinicId ?? "";
  const allowedTaskTypes = selectedWorkspace
    ? [...new Set(selectedWorkspace.roles.flatMap((role) => TASK_TYPES_BY_ROLE[role]))]
    : [];
  const activeTaskType = allowedTaskTypes.includes(taskType) ? taskType : (allowedTaskTypes[0] ?? "CallClient");
  const canUseCrm = allowedTaskTypes.length > 0;
  const today = getClinicDateInputValue();
  const dashboardKey = ["clinics", "staff-crm-dashboard", activeClinicId, today];
  const { data: dashboard, isLoading: isLoadingDashboard } = useQuery({
    queryKey: dashboardKey,
    queryFn: () => clinicsApi.getStaffCrmDashboard(activeClinicId, today),
    enabled: Boolean(activeClinicId && canUseCrm),
  });
  const createTask = useMutation({
    mutationFn: () =>
      clinicsApi.createStaffCrmTask(activeClinicId, {
        petId: petId.trim() || null,
        type: activeTaskType,
        dueDate,
        title: title.trim(),
        notes: notes.trim() || null,
        idempotencyKey,
        priority,
        assignedRole: TASK_OWNER_ROLE_BY_TYPE[activeTaskType],
      }),
    onSuccess: () => {
      setPetId("");
      setTitle("");
      setNotes("");
      setIdempotencyKey(crypto.randomUUID());
      void queryClient.invalidateQueries({ queryKey: dashboardKey });
      toast.success("Tarea asignada al equipo.");
    },
    onError: () => toast.error("No se pudo crear la tarea para este rol."),
  });
  const completeTask = useMutation({
    mutationFn: (taskId: string) => clinicsApi.completeStaffCrmTask(activeClinicId, taskId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: dashboardKey });
      toast.success("Tarea completada.");
    },
    onError: () => toast.error("No se pudo completar la tarea o ya no tienes acceso."),
  });

  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-8">
      <header className="border-b border-sand-200 pb-4">
        <p className="text-xs font-bold uppercase tracking-[0.14em] text-sand-500">Equipo clínico</p>
        <div className="mt-2 flex flex-wrap items-center justify-between gap-3">
          <h1 className="font-display text-2xl font-semibold text-sand-900">Tareas de operación</h1>
          <label className="flex items-center gap-2 text-xs font-semibold text-sand-700">
            <span>Clínica</span>
            <select
              aria-label="Clínica del equipo"
              className="field-input min-w-[200px]"
              value={activeClinicId}
              onChange={(event) => setClinicId(event.target.value)}
            >
              {workspaces.map((workspace) => (
                <option key={workspace.clinicId} value={workspace.clinicId}>
                  {workspace.clinicName}
                </option>
              ))}
            </select>
          </label>
        </div>
        {selectedWorkspace && <p className="mt-2 text-sm text-sand-600">{staffRoleLabel(selectedWorkspace.roles)}</p>}
      </header>

      {isLoadingWorkspaces && <div className="h-24 animate-pulse rounded-xl bg-sand-100" />}
      {!isLoadingWorkspaces && !selectedWorkspace && (
        <p className="border-l-2 border-sand-300 py-2 pl-3 text-sm text-sand-600">
          No tienes una membresía activa de equipo clínico.
        </p>
      )}
      {selectedWorkspace && !canUseCrm && (
        <p className="border-l-2 border-warn-400 py-2 pl-3 text-sm text-sand-700">
          CRM no habilitado para este rol. Solicita al titular que revise tus permisos.
        </p>
      )}
      {selectedWorkspace && canUseCrm && (
        <>
          <section className="space-y-3 border-b border-sand-200 pb-5">
            <div className="flex items-end justify-between gap-3">
              <div>
                <h2 className="text-base font-semibold text-sand-900">Crear tarea</h2>
                <p className="mt-1 text-xs text-sand-500">La tarea solo se mostrará a roles autorizados.</p>
              </div>
            </div>
            <div className="grid gap-2 sm:grid-cols-2">
              <Input
                aria-label="ID de mascota para tarea"
                placeholder="ID de mascota vinculada (opcional)"
                value={petId}
                onChange={(event) => setPetId(event.target.value)}
              />
              <select
                aria-label="Tipo de tarea interna"
                className="field-input"
                value={activeTaskType}
                onChange={(event) => setTaskType(event.target.value as ClinicCrmTaskType)}
              >
                {allowedTaskTypes.map((type) => (
                  <option key={type} value={type}>
                    {TASK_TYPE_LABELS[type]}
                  </option>
                ))}
              </select>
              <input
                aria-label="Vencimiento de tarea"
                className="field-input"
                type="date"
                value={dueDate}
                onChange={(event) => setDueDate(event.target.value)}
              />
              <select
                aria-label="Prioridad de tarea"
                className="field-input"
                value={priority}
                onChange={(event) => setPriority(event.target.value as ClinicCrmTaskPriority)}
              >
                <option value="Urgent">Urgente</option>
                <option value="High">Alta</option>
                <option value="Normal">Normal</option>
                <option value="Low">Baja</option>
              </select>
              <Input
                aria-label="Título de tarea interna"
                placeholder="Título"
                value={title}
                onChange={(event) => setTitle(event.target.value)}
              />
            </div>
            <Input
              aria-label="Notas de tarea interna"
              placeholder="Notas opcionales"
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
            />
            <Button disabled={!title.trim() || !dueDate || createTask.isPending} onClick={() => createTask.mutate()}>
              {createTask.isPending ? "Guardando..." : "Crear tarea"}
            </Button>
          </section>

          <section className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-base font-semibold text-sand-900">Pendientes del rol</h2>
              <span className="text-xs font-semibold text-sand-500">{dashboard?.openTasks.length ?? 0} abiertas</span>
            </div>
            {isLoadingDashboard ? (
              <div className="h-24 animate-pulse rounded-xl bg-sand-100" />
            ) : (dashboard?.openTasks.length ?? 0) === 0 ? (
              <p className="border-l-2 border-sand-300 py-2 pl-3 text-sm text-sand-600">No hay tareas abiertas.</p>
            ) : (
              <ul className="divide-y divide-sand-200">
                {dashboard?.openTasks.map((task) => (
                  <li key={task.id} className="flex flex-wrap items-start justify-between gap-3 py-3">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="text-sm font-semibold text-sand-900">{task.title}</h3>
                        <span className="rounded-full bg-brand-100 px-2 py-1 text-[10px] font-bold text-brand-700">
                          {taskPriorityLabel(task.dueDate, task.priority)}
                        </span>
                        <span className="text-[10px] font-semibold text-sand-500">
                          {INTERNAL_ROLE_LABELS[task.assignedRole]}
                        </span>
                      </div>
                      <p className="mt-1 text-xs text-sand-600">{TASK_TYPE_LABELS[task.type]}</p>
                      {task.petName && (
                        <p className="mt-1 text-xs text-sand-600">
                          {task.petName}
                          {task.ownerName ? ` · ${task.ownerName}` : ""}
                        </p>
                      )}
                      <p className="mt-1 text-xs text-sand-500">Responsable: {task.assignedToName ?? "cola del rol"}</p>
                      <p className="mt-1 text-xs text-sand-500">Vence {formatCostaRicaDate(task.dueDate)}</p>
                      {task.notes && <p className="mt-1 text-xs text-sand-600">{task.notes}</p>}
                    </div>
                    <div className="flex flex-wrap items-center gap-3">
                      <Button
                        variant="secondary"
                        disabled={completeTask.isPending}
                        onClick={() => completeTask.mutate(task.id)}
                      >
                        Completar tarea
                      </Button>
                      {task.type === "CollectPayment" && (
                        <Link
                          className="text-xs font-semibold text-brand-700 underline underline-offset-2"
                          to="/clinica/caja"
                        >
                          Abrir caja
                        </Link>
                      )}
                      {task.type === "CloseCash" && (
                        <Link
                          className="text-xs font-semibold text-brand-700 underline underline-offset-2"
                          to="/clinica/caja"
                        >
                          Abrir cierre de caja
                        </Link>
                      )}
                      {task.type === "ProcessRefund" && (
                        <Link
                          className="text-xs font-semibold text-brand-700 underline underline-offset-2"
                          to="/clinica/caja"
                        >
                          Abrir devoluciones
                        </Link>
                      )}
                      {task.type === "ReviewOperations" && (
                        <Link
                          className="text-xs font-semibold text-brand-700 underline underline-offset-2"
                          to="/clinica/portal"
                        >
                          Abrir operación
                        </Link>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </>
      )}
    </main>
  );
}
