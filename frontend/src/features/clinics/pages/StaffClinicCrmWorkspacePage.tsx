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
  type VeterinarianAppointmentStatus,
} from "../api/clinicsApi";
import {
  daysFromClinicToday,
  formatCostaRicaDate,
  formatCostaRicaTime,
  getClinicDateInputValue,
  getClinicDayRange,
} from "../clinicDateTime";

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

function nextAppointmentStatus(status: VeterinarianAppointmentStatus, roles: ClinicInternalTaskRole[]) {
  if (roles.includes("Receptionist")) {
    if (status === "Scheduled") return "Confirmed";
    if (status === "Confirmed") return "CheckedIn";
  }
  if (roles.includes("Veterinarian")) {
    if (status === "CheckedIn") return "InConsultation";
    if (status === "InConsultation") return "Completed";
  }
  return null;
}

const APPOINTMENT_STATUS_LABELS: Record<VeterinarianAppointmentStatus, string> = {
  Scheduled: "Programada",
  Confirmed: "Confirmada",
  CheckedIn: "En sala",
  InConsultation: "En consulta",
  Completed: "Completada",
  NoShow: "No asistió",
  Cancelled: "Cancelada",
};

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
  const [assignedToUserId, setAssignedToUserId] = useState("");
  const [businessDate, setBusinessDate] = useState(getClinicDateInputValue);
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
  const activeTaskRole = TASK_OWNER_ROLE_BY_TYPE[activeTaskType];
  const canUseCrm = allowedTaskTypes.length > 0;
  const dayRange = getClinicDayRange(businessDate);
  const dashboardKey = ["clinics", "staff-crm-dashboard", activeClinicId, businessDate];
  const { data: dashboard, isLoading: isLoadingDashboard } = useQuery({
    queryKey: dashboardKey,
    queryFn: () => clinicsApi.getStaffCrmDashboard(activeClinicId, businessDate),
    enabled: Boolean(activeClinicId && canUseCrm),
  });
  const agendaKey = ["clinics", "staff-agenda", activeClinicId, dayRange.from, dayRange.to];
  const { data: agenda = [], isLoading: isLoadingAgenda } = useQuery({
    queryKey: agendaKey,
    queryFn: () => clinicsApi.getStaffAgenda(activeClinicId, dayRange.from, dayRange.to),
    enabled: Boolean(activeClinicId),
  });
  const hasFinanceWorkspace =
    selectedWorkspace?.roles.some((role) => role === "Cashier" || role === "Manager") ?? false;
  const { data: salesReport, isLoading: isLoadingSales } = useQuery({
    queryKey: ["clinics", "staff-sales-report", activeClinicId, businessDate],
    queryFn: () => clinicsApi.getStaffSalesReport(activeClinicId, businessDate),
    enabled: Boolean(activeClinicId && hasFinanceWorkspace),
  });
  const canAssignIndividuals = selectedWorkspace?.roles.includes("Manager") ?? false;
  const { data: assignees = [] } = useQuery({
    queryKey: ["clinics", "staff-task-assignees", activeClinicId],
    queryFn: () => clinicsApi.getStaffTaskAssignees(activeClinicId),
    enabled: Boolean(activeClinicId && canAssignIndividuals),
  });
  const taskAssignees = assignees.filter((assignee) => assignee.role === activeTaskRole);
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
        assignedRole: activeTaskRole,
        assignedToUserId: assignedToUserId || null,
      }),
    onSuccess: () => {
      setPetId("");
      setTitle("");
      setNotes("");
      setAssignedToUserId("");
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
  const updateAppointmentStatus = useMutation({
    mutationFn: ({ appointmentId, status }: { appointmentId: string; status: VeterinarianAppointmentStatus }) =>
      clinicsApi.updateStaffAppointmentStatus(activeClinicId, appointmentId, status),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: agendaKey });
      toast.success("Estado de cita actualizado.");
    },
    onError: () => toast.error("Se requiere MFA reciente y permiso de agenda para cambiar el estado."),
  });

  const todayMetrics = {
    appointments: agenda.filter(
      (appointment) => appointment.status === "Scheduled" || appointment.status === "Confirmed",
    ).length,
    inProgress: agenda.filter(
      (appointment) => appointment.status === "CheckedIn" || appointment.status === "InConsultation",
    ).length,
    completed: agenda.filter((appointment) => appointment.status === "Completed").length,
    openTasks: dashboard?.openTasks.length ?? 0,
  };

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
          <section aria-labelledby="staff-daily-heading" className="space-y-4 border-b border-sand-200 pb-5">
            <div className="flex flex-wrap items-end justify-between gap-3">
              <div>
                <h2 id="staff-daily-heading" className="text-base font-semibold text-sand-900">
                  Agenda de hoy
                </h2>
                <p className="mt-1 text-xs text-sand-500">Horario local de Costa Rica</p>
              </div>
              <input
                aria-label="Fecha del resumen operativo"
                className="field-input"
                type="date"
                value={businessDate}
                onChange={(event) => setBusinessDate(event.target.value)}
              />
            </div>
            <div className="grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
              <div className="border-l-2 border-brand-500 pl-3">
                <p className="text-xs text-sand-500">Citas por atender</p>
                <p className="text-lg font-semibold text-sand-900">{todayMetrics.appointments}</p>
              </div>
              <div className="border-l-2 border-warn-400 pl-3">
                <p className="text-xs text-sand-500">En sala / consulta</p>
                <p className="text-lg font-semibold text-sand-900">{todayMetrics.inProgress}</p>
              </div>
              <div className="border-l-2 border-rescue-500 pl-3">
                <p className="text-xs text-sand-500">Completadas</p>
                <p className="text-lg font-semibold text-sand-900">{todayMetrics.completed}</p>
              </div>
              <div className="border-l-2 border-sand-400 pl-3">
                <p className="text-xs text-sand-500">Tareas abiertas</p>
                <p className="text-lg font-semibold text-sand-900">{todayMetrics.openTasks}</p>
              </div>
              {hasFinanceWorkspace && (
                <div className="border-l-2 border-warn-500 pl-3">
                  <p className="text-xs text-sand-500">Saldo pendiente</p>
                  <p className="text-lg font-semibold text-sand-900">
                    ₡{(salesReport?.pendingBalanceCrc ?? 0).toLocaleString("es-CR")}
                  </p>
                  <p className="text-[11px] text-sand-500">{salesReport?.pendingSaleCount ?? 0} ventas</p>
                </div>
              )}
            </div>
            {isLoadingAgenda || (hasFinanceWorkspace && isLoadingSales) ? (
              <div className="h-16 animate-pulse rounded-lg bg-sand-100" />
            ) : agenda.length === 0 ? (
              <p className="border-l-2 border-sand-300 py-2 pl-3 text-sm text-sand-600">No hay citas en esta fecha.</p>
            ) : (
              <ul className="divide-y divide-sand-200">
                {agenda.map((appointment) => {
                  const nextStatus = nextAppointmentStatus(appointment.status, selectedWorkspace.roles);
                  return (
                    <li
                      key={appointment.appointmentId}
                      className="flex flex-wrap items-center justify-between gap-3 py-3"
                    >
                      <div className="min-w-0">
                        <p className="text-sm font-semibold text-sand-900">{appointment.petName}</p>
                        <p className="mt-1 text-xs text-sand-600">
                          {formatCostaRicaTime(appointment.startsAt)} · {appointment.veterinarianName}
                        </p>
                      </div>
                      <div className="flex items-center gap-3">
                        <span className="text-xs font-semibold text-sand-600">
                          {APPOINTMENT_STATUS_LABELS[appointment.status]}
                        </span>
                        {nextStatus && (
                          <Button
                            variant="secondary"
                            disabled={updateAppointmentStatus.isPending}
                            onClick={() =>
                              updateAppointmentStatus.mutate({
                                appointmentId: appointment.appointmentId,
                                status: nextStatus,
                              })
                            }
                          >
                            {APPOINTMENT_STATUS_LABELS[nextStatus]}
                          </Button>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
            <p className="text-xs text-sand-500">
              El cambio de estado clínico requiere la membresía veterinaria asignada y MFA vigente.
            </p>
          </section>

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
                onChange={(event) => {
                  setTaskType(event.target.value as ClinicCrmTaskType);
                  setAssignedToUserId("");
                }}
              >
                {allowedTaskTypes.map((type) => (
                  <option key={type} value={type}>
                    {TASK_TYPE_LABELS[type]}
                  </option>
                ))}
              </select>
              {canAssignIndividuals && (
                <select
                  aria-label="Responsable individual"
                  className="field-input"
                  value={assignedToUserId}
                  onChange={(event) => setAssignedToUserId(event.target.value)}
                >
                  <option value="">Cola de {INTERNAL_ROLE_LABELS[activeTaskRole]}</option>
                  {taskAssignees.map((assignee) => (
                    <option key={assignee.userId} value={assignee.userId}>
                      {assignee.displayName}
                    </option>
                  ))}
                </select>
              )}
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
