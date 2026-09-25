import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { certificateApi, type ClinicVeterinarianDto } from "../api/certificateApi";
import type {
  ClinicAgendaAuditEntryDto,
  ClinicAgendaItemDto,
  ClinicCommunicationChannel,
  ClinicCommunicationPurpose,
  ClinicCrmDashboardDto,
  ClinicCrmTaskType,
  ClinicInventoryItemDto,
  ClinicInventoryItemType,
  ClinicPaymentMethod,
  ClinicalConsultationTemplateDto,
  ClinicScheduleBlockDto,
  VeterinarianAppointmentStatus,
} from "../api/clinicsApi";
import {
  useClinicAgendaAudit,
  useClinicAgenda,
  useClinicScheduleBlocks,
  useCloseClinicalConsultation,
  useClinicalConsultationTemplates,
  useAddClinicInventoryItem,
  useCreateClinicScheduleBlock,
  useCreateClinicalConsultation,
  useDeleteClinicScheduleBlock,
  useDownloadClinicAgendaAuditCsv,
  useDownloadClinicalConsultationPrescription,
  useAdjustClinicInventoryLot,
  useClinicSalesReport,
  useClinicFinanceMembers,
  useGrantClinicFinanceMember,
  useRevokeClinicFinanceMember,
  useCloseClinicCash,
  useClinicCrmDashboard,
  useClinicCommunicationTemplates,
  useSendClinicCommunicationTemplate,
  useCreateClinicSale,
  useCreateClinicCrmTask,
  useClinicInventory,
  useClinicInventoryValuation,
  useReceiveClinicInventoryLot,
  useRegisterClinicSalePayment,
  useRescheduleClinicAppointment,
  useLogClinicCommunicationActivity,
  useUpdateClinicScheduleBlock,
  useUpdateClinicAppointmentStatus,
  useUpsertClinicCommunicationPreference,
  useUploadClinicalConsultationAttachment,
  useVoidClinicSale,
  useCompleteClinicCrmTask,
} from "../hooks/useClinics";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";

const PERMISSIONS = [
  ["medical:read", "Leer expedientes"],
  ["medical:write", "Agregar registros"],
  ["medical:export", "Exportar expedientes"],
  ["certificates:issue", "Emitir certificados"],
] as const;

const STATUS_LABELS: Record<VeterinarianAppointmentStatus, string> = {
  Scheduled: "Programada",
  Confirmed: "Confirmada",
  CheckedIn: "En sala",
  InConsultation: "En consulta",
  Completed: "Completada",
  NoShow: "No asistió",
  Cancelled: "Cancelada",
};

const NEXT_STATUS: Partial<Record<VeterinarianAppointmentStatus, VeterinarianAppointmentStatus>> = {
  Scheduled: "Confirmed",
  Confirmed: "CheckedIn",
  CheckedIn: "InConsultation",
  InConsultation: "Completed",
};

function dayRange(value: string) {
  const start = new Date(`${value}T00:00:00`);
  const end = new Date(start);
  end.setDate(end.getDate() + 1);
  return { from: start.toISOString(), to: end.toISOString() };
}

function weekRange(value: string) {
  const selected = new Date(`${value}T00:00:00`);
  const day = selected.getDay();
  const mondayOffset = day === 0 ? -6 : 1 - day;
  const start = new Date(selected);
  start.setDate(selected.getDate() + mondayOffset);
  const end = new Date(start);
  end.setDate(start.getDate() + 7);
  return { from: start.toISOString(), to: end.toISOString() };
}

function todayInputValue() {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "America/Costa_Rica",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date());
}

function VeterinarianRow({ veterinarian }: { veterinarian: ClinicVeterinarianDto }) {
  const queryClient = useQueryClient();
  const [permissions, setPermissions] = useState<string[]>(
    veterinarian.permissions ?? ["medical:read", "medical:write", "certificates:issue"],
  );
  const update = useMutation({
    mutationFn: () => certificateApi.setVeterinarianPermissions(veterinarian.id, permissions),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["clinic-veterinarians"],
      });
      toast.success("Permisos actualizados.");
    },
    onError: () => toast.error("No se pudieron actualizar los permisos."),
  });

  return (
    <article className="space-y-3 rounded-2xl border border-sand-200 bg-surface p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="font-bold text-sand-900">{veterinarian.fullName}</p>
          <p className="text-xs text-sand-500">
            {veterinarian.licenseNumber} · {veterinarian.status}
          </p>
        </div>
        <span
          className={`rounded-full px-2 py-1 text-[11px] font-bold ${veterinarian.isActive ? "bg-rescue-100 text-rescue-700" : "bg-sand-100 text-sand-500"}`}
        >
          {veterinarian.isActive ? "Activo" : "No activo"}
        </span>
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        {PERMISSIONS.map(([permission, label]) => (
          <label key={permission} className="flex items-center gap-2 text-xs text-sand-700">
            <input
              type="checkbox"
              checked={permissions.includes(permission)}
              disabled={!veterinarian.isActive}
              onChange={(event) =>
                setPermissions((current) =>
                  event.target.checked ? [...current, permission] : current.filter((item) => item !== permission),
                )
              }
            />
            {label}
          </label>
        ))}
      </div>
      <Button disabled={!veterinarian.isActive || update.isPending} onClick={() => update.mutate()}>
        {update.isPending ? "Guardando..." : "Guardar permisos"}
      </Button>
    </article>
  );
}

export function ClinicOperationsPanel() {
  const queryClient = useQueryClient();
  const { data: veterinarians = [], isLoading } = useQuery({
    queryKey: ["clinic-veterinarians"],
    queryFn: certificateApi.getMyVeterinarians,
  });
  const [agendaDate, setAgendaDate] = useState(todayInputValue());
  const [agendaMode, setAgendaMode] = useState<"day" | "week">("day");
  const range = agendaMode === "day" ? dayRange(agendaDate) : weekRange(agendaDate);
  const { data: agenda = [], isLoading: agendaLoading } = useClinicAgenda(range.from, range.to);
  const { data: blocks = [], isLoading: blocksLoading } = useClinicScheduleBlocks(range.from, range.to);
  const { data: auditEntries = [], isLoading: auditLoading } = useClinicAgendaAudit(range.from, range.to);
  const { data: consultationTemplates = [] } = useClinicalConsultationTemplates();
  const { data: inventory = [] } = useClinicInventory();
  const { data: inventoryValuation } = useClinicInventoryValuation();
  const businessDate = agendaDate;
  const { data: salesReport } = useClinicSalesReport(businessDate);
  const createSale = useCreateClinicSale(businessDate);
  const registerPayment = useRegisterClinicSalePayment(businessDate);
  const closeCash = useCloseClinicCash(businessDate);
  const voidSale = useVoidClinicSale(businessDate);
  const { data: crmDashboard } = useClinicCrmDashboard(businessDate);
  const { data: communicationTemplates = [] } = useClinicCommunicationTemplates();
  const sendTemplate = useSendClinicCommunicationTemplate(businessDate);
  const upsertPreference = useUpsertClinicCommunicationPreference(businessDate);
  const logActivity = useLogClinicCommunicationActivity(businessDate);
  const createCrmTask = useCreateClinicCrmTask(businessDate);
  const completeCrmTask = useCompleteClinicCrmTask(businessDate);
  const downloadAudit = useDownloadClinicAgendaAuditCsv();
  const uploadAttachment = useUploadClinicalConsultationAttachment();
  const downloadPrescription = useDownloadClinicalConsultationPrescription();
  const addInventoryItem = useAddClinicInventoryItem();
  const adjustInventoryLot = useAdjustClinicInventoryLot();
  const receiveInventoryLot = useReceiveClinicInventoryLot();
  const updateStatus = useUpdateClinicAppointmentStatus(range.from, range.to);
  const reschedule = useRescheduleClinicAppointment(range.from, range.to);
  const createBlock = useCreateClinicScheduleBlock(range.from, range.to);
  const updateBlock = useUpdateClinicScheduleBlock(range.from, range.to);
  const deleteBlock = useDeleteClinicScheduleBlock(range.from, range.to);
  const createConsultation = useCreateClinicalConsultation(range.from, range.to);
  const closeConsultation = useCloseClinicalConsultation(range.from, range.to);
  const [selectedVeterinarian, setSelectedVeterinarian] = useState("");
  const [petId, setPetId] = useState("");
  const [startsAt, setStartsAt] = useState("");
  const [durationMinutes, setDurationMinutes] = useState(30);
  const [blockVeterinarianId, setBlockVeterinarianId] = useState("");
  const [blockStartsAt, setBlockStartsAt] = useState("");
  const [blockEndsAt, setBlockEndsAt] = useState("");
  const [blockReason, setBlockReason] = useState("Bloqueo de agenda");
  const schedule = useMutation({
    mutationFn: () =>
      certificateApi.scheduleAppointment(selectedVeterinarian, {
        petId,
        startsAt: new Date(startsAt).toISOString(),
        durationMinutes,
      }),
    onSuccess: () => {
      toast.success("Cita agendada.");
      setPetId("");
      setStartsAt("");
      void queryClient.invalidateQueries({
        queryKey: ["clinics", "agenda", range.from, range.to],
      });
    },
    onError: () => toast.error("No se pudo agendar. Verifica solapamientos y permisos."),
  });

  if (isLoading) return <div className="h-40 animate-pulse rounded-2xl bg-sand-100" />;

  return (
    <section className="space-y-5">
      <header>
        <h2 className="text-lg font-black text-sand-900">Operación clínica</h2>
        <p className="mt-1 text-sm text-sand-500">
          Administra permisos por veterinario y agenda consultas con protección contra solapamientos.
        </p>
      </header>
      <ClinicAgendaSection
        date={agendaDate}
        onDateChange={setAgendaDate}
        mode={agendaMode}
        onModeChange={setAgendaMode}
        agenda={agenda}
        isLoading={agendaLoading}
        onAdvance={(appointment) => {
          const next = NEXT_STATUS[appointment.status];
          if (!next) return;
          updateStatus.mutate(
            { appointmentId: appointment.appointmentId, status: next },
            {
              onSuccess: () => toast.success("Estado actualizado."),
              onError: () => toast.error("No se pudo actualizar la cita."),
            },
          );
        }}
        onCancel={(appointment) =>
          updateStatus.mutate(
            { appointmentId: appointment.appointmentId, status: "Cancelled" },
            {
              onSuccess: () => toast.success("Cita cancelada."),
              onError: () => toast.error("No se pudo cancelar la cita."),
            },
          )
        }
        onNoShow={(appointment) =>
          updateStatus.mutate(
            { appointmentId: appointment.appointmentId, status: "NoShow" },
            {
              onSuccess: () => toast.success("Cita marcada como no-show."),
              onError: () => toast.error("No se pudo marcar no-show."),
            },
          )
        }
        isUpdating={updateStatus.isPending}
        onReschedule={(appointment, startsAtValue, durationValue) =>
          reschedule.mutate(
            {
              appointmentId: appointment.appointmentId,
              startsAt: new Date(startsAtValue).toISOString(),
              durationMinutes: durationValue,
            },
            {
              onSuccess: () => toast.success("Cita reprogramada."),
              onError: () => toast.error("No se pudo reprogramar la cita."),
            },
          )
        }
        isRescheduling={reschedule.isPending}
        templates={consultationTemplates}
        onCreateConsultation={(appointment, payload, signedByName, attachment) =>
          createConsultation.mutate(
            { appointmentId: appointment.appointmentId, payload },
            {
              onSuccess: (consultation) => {
                const close = () =>
                  closeConsultation.mutate(
                    { consultationId: consultation.id, signedByName },
                    {
                      onSuccess: (closed) => {
                        toast.success("Consulta cerrada y firmada.");
                        downloadPrescription.mutate(closed.id, {
                          onSuccess: (blob) => {
                            const url = URL.createObjectURL(blob);
                            const link = document.createElement("a");
                            link.href = url;
                            link.download = `consulta-${closed.id}-indicaciones.txt`;
                            link.click();
                            URL.revokeObjectURL(url);
                          },
                        });
                      },
                      onError: () => toast.error("La consulta se guardó, pero no se pudo cerrar."),
                    },
                  );

                if (attachment) {
                  uploadAttachment.mutate(
                    { consultationId: consultation.id, file: attachment },
                    {
                      onSuccess: close,
                      onError: () => toast.error("La consulta se guardó, pero no se pudo subir el adjunto."),
                    },
                  );
                } else {
                  close();
                }
              },
              onError: () => toast.error("No se pudo guardar la consulta."),
            },
          )
        }
        isSavingConsultation={createConsultation.isPending || closeConsultation.isPending || uploadAttachment.isPending}
      />
      <div className="space-y-3">
        <h3 className="text-sm font-bold text-sand-800">Veterinarios y permisos</h3>
        {veterinarians.length === 0 ? (
          <p className="rounded-xl border border-dashed border-sand-300 p-4 text-sm text-sand-500">
            No hay veterinarios registrados.
          </p>
        ) : (
          veterinarians.map((veterinarian) => <VeterinarianRow key={veterinarian.id} veterinarian={veterinarian} />)
        )}
      </div>
      <div className="space-y-3 rounded-2xl border border-brand-200 bg-brand-50 p-4">
        <h3 className="text-sm font-bold text-brand-900">Agendar consulta</h3>
        <select
          value={selectedVeterinarian}
          onChange={(event) => setSelectedVeterinarian(event.target.value)}
          className="field-input w-full"
        >
          <option value="">Selecciona un veterinario</option>
          {veterinarians
            .filter((v) => v.isActive)
            .map((veterinarian) => (
              <option key={veterinarian.id} value={veterinarian.id}>
                {veterinarian.fullName}
              </option>
            ))}
        </select>
        <Input
          value={petId}
          onChange={(event) => setPetId(event.target.value)}
          placeholder="ID de mascota"
          aria-label="ID de mascota"
        />
        <Input
          type="datetime-local"
          value={startsAt}
          onChange={(event) => setStartsAt(event.target.value)}
          aria-label="Inicio de la consulta"
        />
        <label className="block text-xs font-semibold text-sand-600">
          Duración (minutos)
          <input
            type="number"
            min={15}
            max={480}
            step={15}
            value={durationMinutes}
            onChange={(event) => setDurationMinutes(Number(event.target.value))}
            className="field-input mt-1 w-full"
          />
        </label>
        <Button
          disabled={!selectedVeterinarian || !petId || !startsAt || schedule.isPending}
          onClick={() => schedule.mutate()}
        >
          {schedule.isPending ? "Agendando..." : "Agendar consulta"}
        </Button>
      </div>
      <div className="space-y-3 rounded-2xl border border-warn-200 bg-warn-50 p-4">
        <h3 className="text-sm font-bold text-warn-900">Bloquear agenda</h3>
        <select
          value={blockVeterinarianId}
          onChange={(event) => setBlockVeterinarianId(event.target.value)}
          className="field-input w-full"
        >
          <option value="">Selecciona un veterinario</option>
          {veterinarians
            .filter((v) => v.isActive)
            .map((veterinarian) => (
              <option key={veterinarian.id} value={veterinarian.id}>
                {veterinarian.fullName}
              </option>
            ))}
        </select>
        <div className="grid gap-2 sm:grid-cols-2">
          <Input
            type="datetime-local"
            value={blockStartsAt}
            onChange={(event) => setBlockStartsAt(event.target.value)}
            aria-label="Inicio del bloqueo"
          />
          <Input
            type="datetime-local"
            value={blockEndsAt}
            onChange={(event) => setBlockEndsAt(event.target.value)}
            aria-label="Fin del bloqueo"
          />
        </div>
        <Input
          value={blockReason}
          onChange={(event) => setBlockReason(event.target.value)}
          placeholder="Motivo"
          aria-label="Motivo del bloqueo"
        />
        <Button
          disabled={!blockVeterinarianId || !blockStartsAt || !blockEndsAt || createBlock.isPending}
          onClick={() =>
            createBlock.mutate(
              {
                veterinarianId: blockVeterinarianId,
                startsAt: new Date(blockStartsAt).toISOString(),
                endsAt: new Date(blockEndsAt).toISOString(),
                reason: blockReason.trim() || "Bloqueo de agenda",
              },
              {
                onSuccess: () => {
                  toast.success("Agenda bloqueada.");
                  setBlockStartsAt("");
                  setBlockEndsAt("");
                  void queryClient.invalidateQueries({
                    queryKey: ["clinics", "schedule-blocks", range.from, range.to],
                  });
                },
                onError: () => toast.error("No se pudo bloquear ese horario."),
              },
            )
          }
        >
          {createBlock.isPending ? "Bloqueando..." : "Bloquear horario"}
        </Button>
        <ClinicScheduleBlocksList
          blocks={blocks}
          isLoading={blocksLoading}
          isUpdating={updateBlock.isPending || deleteBlock.isPending}
          onUpdate={(block, startsAtValue, endsAtValue, reasonValue) =>
            updateBlock.mutate(
              {
                blockId: block.blockId,
                startsAt: new Date(startsAtValue).toISOString(),
                endsAt: new Date(endsAtValue).toISOString(),
                reason: reasonValue.trim() || "Bloqueo de agenda",
              },
              {
                onSuccess: () => toast.success("Bloqueo actualizado."),
                onError: () => toast.error("No se pudo actualizar el bloqueo."),
              },
            )
          }
          onDelete={(block) =>
            deleteBlock.mutate(block.blockId, {
              onSuccess: () => toast.success("Bloqueo eliminado."),
              onError: () => toast.error("No se pudo eliminar el bloqueo."),
            })
          }
        />
      </div>
      <ClinicAgendaAuditSection
        entries={auditEntries}
        isLoading={auditLoading}
        isDownloading={downloadAudit.isPending}
        onExport={() =>
          downloadAudit.mutate(
            { from: range.from, to: range.to },
            {
              onSuccess: (blob) => {
                const url = URL.createObjectURL(blob);
                const link = document.createElement("a");
                link.href = url;
                link.download = "clinic-agenda-audit.csv";
                link.click();
                URL.revokeObjectURL(url);
              },
              onError: () => toast.error("No se pudo exportar la auditoría."),
            },
          )
        }
      />
      <ClinicInventorySection
        inventory={inventory}
        valuation={inventoryValuation}
        isSavingItem={addInventoryItem.isPending}
        isAdjustingLot={adjustInventoryLot.isPending}
        isReceivingLot={receiveInventoryLot.isPending}
        onAddItem={(payload) =>
          addInventoryItem.mutate(payload, {
            onSuccess: () => toast.success("Producto clínico creado."),
            onError: () => toast.error("No se pudo crear el producto clínico."),
          })
        }
        onReceiveLot={(itemId, payload) =>
          receiveInventoryLot.mutate(
            { itemId, payload },
            {
              onSuccess: () => toast.success("Lote recibido."),
              onError: () => toast.error("No se pudo recibir el lote."),
            },
          )
        }
        onAdjustLot={(lotId, quantityDelta, reason) =>
          adjustInventoryLot.mutate(
            { lotId, quantityDelta, reason },
            {
              onSuccess: () => toast.success("Ajuste registrado."),
              onError: () => toast.error("No se pudo registrar el ajuste."),
            },
          )
        }
      />
      <ClinicFinanceSection
        appointments={agenda ?? []}
        report={salesReport}
        isCreatingSale={createSale.isPending}
        isRegisteringPayment={registerPayment.isPending}
        isClosingCash={closeCash.isPending}
        isVoidingSale={voidSale.isPending}
        onCreateSale={(payload, onCreated) =>
          createSale.mutate(payload, {
            onSuccess: (sale) => {
              onCreated(sale.id);
              toast.success("Venta creada.");
            },
            onError: () => toast.error("No se pudo crear la venta."),
          })
        }
        onRegisterPayment={(saleId, payload) =>
          registerPayment.mutate(
            { saleId, payload },
            {
              onSuccess: () => toast.success("Pago registrado."),
              onError: () => toast.error("No se pudo registrar el pago."),
            },
          )
        }
        onVoidSale={(saleId, reason) =>
          voidSale.mutate(
            { saleId, reason },
            {
              onSuccess: () => toast.success("Venta anulada."),
              onError: () => toast.error("No se pudo anular la venta."),
            },
          )
        }
        onCloseCash={() =>
          closeCash.mutate(undefined, {
            onSuccess: () => toast.success("Caja cerrada."),
            onError: () => toast.error("No se pudo cerrar la caja."),
          })
        }
      />
      <ClinicFinanceTeamSection />
      <ClinicCrmSection
        dashboard={crmDashboard}
        templates={communicationTemplates}
        isSendingTemplate={sendTemplate.isPending}
        onSendTemplate={(petId, templateKey, requestId, onAccepted) =>
          sendTemplate.mutate(
            { petId, templateKey, requestId },
            {
              onSuccess: () => {
                onAccepted();
                toast.success("Correo aceptado por proveedor.");
              },
              onError: () => toast.error("El proveedor no confirmó la aceptación del correo."),
            },
          )
        }
        isSavingPreference={upsertPreference.isPending}
        isLoggingActivity={logActivity.isPending}
        isCreatingTask={createCrmTask.isPending}
        isCompletingTask={completeCrmTask.isPending}
        onSavePreference={(payload) =>
          upsertPreference.mutate(payload, {
            onSuccess: () => toast.success("Preferencia guardada."),
            onError: () => toast.error("No se pudo guardar la preferencia."),
          })
        }
        onLogActivity={(payload) =>
          logActivity.mutate(payload, {
            onSuccess: () => toast.success("Contacto registrado."),
            onError: () => toast.error("No se pudo registrar el contacto."),
          })
        }
        onCreateTask={(payload) =>
          createCrmTask.mutate(payload, {
            onSuccess: () => toast.success("Tarea CRM creada."),
            onError: () => toast.error("No se pudo crear la tarea CRM."),
          })
        }
        onCompleteTask={(taskId) =>
          completeCrmTask.mutate(taskId, {
            onSuccess: () => toast.success("Tarea completada."),
            onError: () => toast.error("No se pudo completar la tarea."),
          })
        }
      />
    </section>
  );
}

function ClinicFinanceTeamSection() {
  const { data: members = [] } = useClinicFinanceMembers();
  const grant = useGrantClinicFinanceMember();
  const revoke = useRevokeClinicFinanceMember();
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<"Cashier" | "Administrator">("Cashier");

  return (
    <section className="space-y-3 border-t border-sand-200 pt-4">
      <h3 className="text-sm font-bold text-sand-900">Personal de caja</h3>
      <Link
        to="/clinica/caja"
        className="inline-block text-sm font-semibold text-brand-700 underline underline-offset-2"
      >
        Abrir caja clínica
      </Link>
      <div className="flex flex-wrap gap-2">
        <Input
          type="email"
          aria-label="Correo del colaborador"
          placeholder="Correo verificado"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />
        <select
          className="field-input"
          aria-label="Rol financiero"
          value={role}
          onChange={(event) => setRole(event.target.value as "Cashier" | "Administrator")}
        >
          <option value="Cashier">Cajero</option>
          <option value="Administrator">Administrador</option>
        </select>
        <Button
          disabled={!email.includes("@") || grant.isPending}
          onClick={() =>
            grant.mutate(
              { email, role },
              {
                onSuccess: () => {
                  setEmail("");
                  toast.success("Acceso financiero asignado.");
                },
                onError: () => toast.error("Se requiere titular con MFA y cuenta verificada."),
              },
            )
          }
        >
          Asignar acceso
        </Button>
      </div>
      {members
        .filter((member) => !member.isRevoked)
        .map((member) => (
          <div
            key={member.id}
            className="flex flex-wrap items-center justify-between gap-3 border-b border-sand-100 py-2 text-sm"
          >
            <span>
              {member.email} · {member.role === "Cashier" ? "Cajero" : "Administrador"}
            </span>
            <Button
              variant="secondary"
              disabled={revoke.isPending}
              onClick={() =>
                revoke.mutate(member.userId, {
                  onSuccess: () => toast.success("Acceso revocado."),
                  onError: () => toast.error("No se pudo revocar el acceso."),
                })
              }
            >
              Revocar
            </Button>
          </div>
        ))}
    </section>
  );
}

function ClinicCrmSection({
  dashboard,
  templates,
  isSendingTemplate,
  onSendTemplate,
  isSavingPreference,
  isLoggingActivity,
  isCreatingTask,
  isCompletingTask,
  onSavePreference,
  onLogActivity,
  onCreateTask,
  onCompleteTask,
}: {
  dashboard?: ClinicCrmDashboardDto;
  templates: Array<{ key: string; label: string }>;
  isSendingTemplate: boolean;
  onSendTemplate: (petId: string, templateKey: string, requestId: string, onAccepted: () => void) => void;
  isSavingPreference: boolean;
  isLoggingActivity: boolean;
  isCreatingTask: boolean;
  isCompletingTask: boolean;
  onSavePreference: (
    payload: Parameters<typeof import("../api/clinicsApi").clinicsApi.upsertCommunicationPreference>[0],
  ) => void;
  onLogActivity: (
    payload: Parameters<typeof import("../api/clinicsApi").clinicsApi.logCommunicationActivity>[0],
  ) => void;
  onCreateTask: (payload: Parameters<typeof import("../api/clinicsApi").clinicsApi.createCrmTask>[0]) => void;
  onCompleteTask: (taskId: string) => void;
}) {
  const today = new Date().toISOString().slice(0, 10);
  const [petId, setPetId] = useState("");
  const [templateKey, setTemplateKey] = useState("clinical-follow-up");
  const [templateRequestId, setTemplateRequestId] = useState(() => crypto.randomUUID());
  const [channel, setChannel] = useState<ClinicCommunicationChannel>("WhatsApp");
  const [purpose, setPurpose] = useState<ClinicCommunicationPurpose>("ClinicalFollowUp");
  const [subject, setSubject] = useState("Seguimiento clínico");
  const [body, setBody] = useState("Se contactó al tutor para seguimiento posterior a consulta.");
  const [taskType, setTaskType] = useState<ClinicCrmTaskType>("FollowUpTreatment");
  const [taskDueDate, setTaskDueDate] = useState(today);
  const [taskTitle, setTaskTitle] = useState("Llamar al tutor");

  return (
    <section className="space-y-3 rounded-2xl border border-emerald-200 bg-emerald-50 p-4">
      <h3 className="text-sm font-bold text-emerald-950">Comunicación y CRM clínico</h3>
      <div className="grid gap-2 sm:grid-cols-4">
        {(dashboard?.segments ?? []).map((segment) => (
          <div key={segment.key} className="rounded-xl border border-emerald-100 bg-surface p-3 text-xs text-sand-700">
            <p className="font-bold text-sand-900">{segment.count}</p>
            <p>{segment.label}</p>
          </div>
        ))}
      </div>
      <div className="grid gap-2 sm:grid-cols-3">
        <Input
          value={petId}
          onChange={(event) => setPetId(event.target.value)}
          placeholder="ID mascota"
          aria-label="ID mascota CRM"
        />
        <select
          value={channel}
          onChange={(event) => setChannel(event.target.value as ClinicCommunicationChannel)}
          className="field-input"
          aria-label="Canal CRM"
        >
          <option value="WhatsApp">WhatsApp</option>
          <option value="Email">Email</option>
          <option value="Phone">Teléfono</option>
          <option value="InApp">App</option>
        </select>
        <select
          value={purpose}
          onChange={(event) => setPurpose(event.target.value as ClinicCommunicationPurpose)}
          className="field-input"
          aria-label="Propósito CRM"
        >
          <option value="AppointmentConfirmation">Confirmación cita</option>
          <option value="ClinicalFollowUp">Seguimiento clínico</option>
          <option value="VaccineReminder">Recordatorio vacuna</option>
          <option value="PrescriptionDelivery">Entrega receta</option>
          <option value="Billing">Cobro</option>
          <option value="Marketing">Marketing</option>
        </select>
      </div>
      <div className="grid gap-2 sm:grid-cols-4">
        <Input value={subject} onChange={(event) => setSubject(event.target.value)} aria-label="Asunto comunicación" />
        <Input value={body} onChange={(event) => setBody(event.target.value)} aria-label="Detalle comunicación" />
        <Button
          disabled={!petId || isLoggingActivity}
          onClick={() =>
            onLogActivity({ petId, channel, purpose, direction: "Outbound", status: "LoggedExternally", subject, body })
          }
        >
          Registrar contacto
        </Button>
        <Button
          variant="secondary"
          disabled={!petId || isSavingPreference}
          onClick={() =>
            onSavePreference({ petId, channel, purpose, isOptedIn: false, consentSource: "Opt-out solicitado" })
          }
        >
          Registrar opt-out
        </Button>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <select
          className="field-input"
          aria-label="Plantilla clínica"
          value={templateKey}
          onChange={(event) => {
            setTemplateKey(event.target.value);
            setTemplateRequestId(crypto.randomUUID());
          }}
        >
          {templates.map((template) => (
            <option key={template.key} value={template.key}>
              {template.label}
            </option>
          ))}
        </select>
        <Button
          disabled={!petId || !templates.some((template) => template.key === templateKey) || isSendingTemplate}
          onClick={() =>
            onSendTemplate(petId, templateKey, templateRequestId, () => setTemplateRequestId(crypto.randomUUID()))
          }
        >
          Enviar correo clínico
        </Button>
      </div>
      {(dashboard?.preferences ?? []).slice(0, 10).map((preference) => (
        <p key={preference.id} className="text-xs text-sand-700">
          {preference.petName} · {preference.channel} · {preference.purpose}:{" "}
          {preference.isOptedIn ? "Autorizado" : "No autorizado"}
        </p>
      ))}
      <div className="grid gap-2 sm:grid-cols-4">
        <select
          value={taskType}
          onChange={(event) => setTaskType(event.target.value as ClinicCrmTaskType)}
          className="field-input"
          aria-label="Tipo tarea CRM"
        >
          <option value="CallClient">Llamar cliente</option>
          <option value="ConfirmAppointment">Confirmar cita</option>
          <option value="FollowUpTreatment">Seguimiento tratamiento</option>
          <option value="SendDocument">Enviar documento</option>
          <option value="CollectPayment">Gestionar cobro</option>
          <option value="Reactivation">Reactivación</option>
        </select>
        <input
          type="date"
          value={taskDueDate}
          onChange={(event) => setTaskDueDate(event.target.value)}
          className="field-input"
          aria-label="Fecha tarea CRM"
        />
        <Input value={taskTitle} onChange={(event) => setTaskTitle(event.target.value)} aria-label="Título tarea CRM" />
        <Button
          disabled={!petId || !taskTitle || isCreatingTask}
          onClick={() => onCreateTask({ petId, type: taskType, dueDate: taskDueDate, title: taskTitle })}
        >
          Crear tarea
        </Button>
      </div>
      <div className="space-y-2">
        {(dashboard?.openTasks ?? []).slice(0, 5).map((task) => (
          <div
            key={task.id}
            className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-emerald-100 bg-surface p-3 text-xs"
          >
            <span>
              <strong>{task.petName}</strong> · {task.title} · {task.dueDate}
            </span>
            <Button variant="secondary" disabled={isCompletingTask} onClick={() => onCompleteTask(task.id)}>
              Completar
            </Button>
          </div>
        ))}
      </div>
      {(dashboard?.recentActivities ?? []).slice(0, 5).map((activity) => (
        <p key={activity.id} className="text-xs text-sand-700">
          {activity.petName} · {activity.channel} · {activity.subject} · {activity.status}
        </p>
      ))}
    </section>
  );
}

function ClinicFinanceSection({
  appointments,
  report,
  isCreatingSale,
  isRegisteringPayment,
  isClosingCash,
  isVoidingSale,
  onCreateSale,
  onRegisterPayment,
  onVoidSale,
  onCloseCash,
}: {
  appointments: ClinicAgendaItemDto[];
  report?: {
    totalPaidCrc: number;
    byPaymentMethod: Record<string, number>;
    byService: Record<string, number>;
    byVeterinarian: Record<string, number>;
  };
  isCreatingSale: boolean;
  isRegisteringPayment: boolean;
  isClosingCash: boolean;
  isVoidingSale: boolean;
  onCreateSale: (
    payload: Parameters<typeof import("../api/clinicsApi").clinicsApi.createSale>[0],
    onCreated: (saleId: string) => void,
  ) => void;
  onRegisterPayment: (
    saleId: string,
    payload: { amountCrc: number; method: ClinicPaymentMethod; reference?: string | null },
  ) => void;
  onVoidSale: (saleId: string, reason: string) => void;
  onCloseCash: () => void;
}) {
  const [receiptNumber, setReceiptNumber] = useState(`REC-${Date.now().toString().slice(-6)}`);
  const [description, setDescription] = useState("Consulta veterinaria");
  const [amount, setAmount] = useState(15000);
  const [discount, setDiscount] = useState(0);
  const [lastSaleId, setLastSaleId] = useState("");
  const [appointmentId, setAppointmentId] = useState("");
  const [paymentAmount, setPaymentAmount] = useState(15000);
  const [paymentMethod, setPaymentMethod] = useState<ClinicPaymentMethod>("Sinpe");
  const [paymentReference, setPaymentReference] = useState("");
  const [voidReason, setVoidReason] = useState("Anulación solicitada");

  return (
    <section className="space-y-3 rounded-2xl border border-brand-200 bg-brand-50 p-4">
      <h3 className="text-sm font-bold text-brand-900">Caja y cobros</h3>
      <div className="rounded-xl border border-brand-100 bg-surface p-3 text-xs text-sand-700">
        <p className="font-bold text-sand-900">
          Cobros registrados: ₡{(report?.totalPaidCrc ?? 0).toLocaleString("es-CR")}
        </p>
        <p className="mt-2 font-semibold text-sand-900">Por método de pago</p>
        {Object.entries(report?.byPaymentMethod ?? {}).map(([method, value]) => (
          <p key={method}>
            {method}: ₡{value.toLocaleString("es-CR")}
          </p>
        ))}
        <p className="mt-2 font-semibold text-sand-900">Ventas por servicio</p>
        {Object.entries(report?.byService ?? {})
          .slice(0, 5)
          .map(([service, value]) => (
            <p key={service}>
              {service}: ₡{value.toLocaleString("es-CR")}
            </p>
          ))}
      </div>
      <div className="border-t border-brand-200 pt-3">
        <h4 className="text-sm font-semibold text-brand-900">Dashboard por veterinario</h4>
        <p className="text-xs text-sand-600">Cobros vinculados a citas, agrupados por veterinario</p>
        <dl className="mt-2 divide-y divide-brand-100 text-sm">
          {Object.entries(report?.byVeterinarian ?? {}).map(([veterinarian, value]) => (
            <div key={veterinarian} className="flex justify-between gap-3 py-2">
              <dt>{veterinarian}</dt>
              <dd>₡{value.toLocaleString("es-CR")}</dd>
            </div>
          ))}
        </dl>
      </div>
      <div className="grid gap-2 sm:grid-cols-4">
        <Input
          value={receiptNumber}
          onChange={(event) => setReceiptNumber(event.target.value)}
          aria-label="Número de recibo"
        />
        <Input
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          aria-label="Servicio cobrado"
        />
        <input
          type="number"
          min={0}
          value={amount}
          onChange={(event) => setAmount(Number(event.target.value))}
          className="field-input"
          aria-label="Monto"
        />
        <input
          type="number"
          min={0}
          value={discount}
          onChange={(event) => setDiscount(Number(event.target.value))}
          className="field-input"
          aria-label="Descuento"
        />
      </div>
      <select
        value={appointmentId}
        onChange={(event) => setAppointmentId(event.target.value)}
        className="field-input"
        aria-label="Cita para venta"
      >
        <option value="">Venta sin cita</option>
        {appointments.map((appointment) => (
          <option key={appointment.appointmentId} value={appointment.appointmentId}>
            {appointment.petName} · {appointment.veterinarianName}
          </option>
        ))}
      </select>
      <Button
        disabled={
          !receiptNumber.trim() ||
          !description.trim() ||
          amount <= 0 ||
          discount < 0 ||
          discount > amount ||
          isCreatingSale
        }
        onClick={() =>
          onCreateSale(
            {
              appointmentId: appointmentId || null,
              petId: appointments.find((appointment) => appointment.appointmentId === appointmentId)?.petId ?? null,
              receiptNumber,
              lines: [{ description, type: "Service", quantity: 1, unitPriceCrc: amount }],
              discountCrc: discount,
              discountReason: discount > 0 ? "Descuento autorizado" : null,
            },
            (saleId) => {
              setLastSaleId(saleId);
              setPaymentAmount(amount - discount);
            },
          )
        }
      >
        {isCreatingSale ? "Creando..." : "Crear venta"}
      </Button>
      <div className="grid gap-2 sm:grid-cols-4">
        <Input
          value={lastSaleId}
          onChange={(event) => setLastSaleId(event.target.value)}
          placeholder="ID venta"
          aria-label="ID venta"
        />
        <input
          type="number"
          min={1}
          value={paymentAmount}
          onChange={(event) => setPaymentAmount(Number(event.target.value))}
          className="field-input"
          aria-label="Monto pago"
        />
        <select
          value={paymentMethod}
          onChange={(event) => setPaymentMethod(event.target.value as ClinicPaymentMethod)}
          className="field-input"
          aria-label="Método de pago"
        >
          <option value="Cash">Efectivo</option>
          <option value="Card">Tarjeta</option>
          <option value="Sinpe">SINPE</option>
          <option value="Transfer">Transferencia</option>
          <option value="InternalCredit">Crédito interno</option>
        </select>
        <Input
          value={paymentReference}
          onChange={(event) => setPaymentReference(event.target.value)}
          placeholder="Referencia"
          aria-label="Referencia pago"
        />
      </div>
      <div className="flex flex-wrap gap-2">
        <Button
          disabled={!lastSaleId || isRegisteringPayment}
          onClick={() =>
            onRegisterPayment(lastSaleId, {
              amountCrc: paymentAmount,
              method: paymentMethod,
              reference: paymentReference || null,
            })
          }
        >
          Registrar pago
        </Button>
        <Button
          variant="secondary"
          disabled={!lastSaleId || isVoidingSale}
          onClick={() => onVoidSale(lastSaleId, voidReason)}
        >
          Anular venta
        </Button>
        <Input
          value={voidReason}
          onChange={(event) => setVoidReason(event.target.value)}
          aria-label="Motivo de anulación"
        />
        <Button variant="secondary" disabled={isClosingCash} onClick={onCloseCash}>
          Cerrar caja diaria
        </Button>
      </div>
    </section>
  );
}

function ClinicInventorySection({
  inventory,
  valuation,
  isSavingItem,
  isAdjustingLot,
  isReceivingLot,
  onAddItem,
  onReceiveLot,
  onAdjustLot,
}: {
  inventory: ClinicInventoryItemDto[];
  valuation?: {
    totalValueCrc: number;
    totalUnits: number;
    byLocation: Array<{ locationName: string; availableQuantity: number; valueCrc: number }>;
  };
  isSavingItem: boolean;
  isAdjustingLot: boolean;
  isReceivingLot: boolean;
  onAddItem: (payload: { name: string; type: ClinicInventoryItemType; unit: string; minimumStock: number }) => void;
  onReceiveLot: (
    itemId: string,
    payload: {
      lotNumber: string;
      expiresAt: string | null;
      quantity: number;
      unitCostCrc: number;
      supplierName: string | null;
    },
  ) => void;
  onAdjustLot: (lotId: string, quantityDelta: number, reason: string) => void;
}) {
  const [name, setName] = useState("");
  const [type, setType] = useState<ClinicInventoryItemType>("Vaccine");
  const [unit, setUnit] = useState("unidad");
  const [minimumStock, setMinimumStock] = useState(1);
  const [lotItemId, setLotItemId] = useState("");
  const [lotNumber, setLotNumber] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [unitCostCrc, setUnitCostCrc] = useState(0);
  const [supplierName, setSupplierName] = useState("");
  const [adjustLotId, setAdjustLotId] = useState("");
  const [adjustDelta, setAdjustDelta] = useState(0);
  const [adjustReason, setAdjustReason] = useState("Ajuste manual");
  const lots = inventory.flatMap((item) => (item.lots ?? []).map((lot) => ({ ...lot, itemName: item.name })));

  return (
    <section className="space-y-3 rounded-2xl border border-rescue-200 bg-rescue-50 p-4">
      <h3 className="text-sm font-bold text-rescue-900">Inventario clínico</h3>
      {valuation && (
        <div className="rounded-xl border border-rescue-100 bg-surface p-3 text-xs text-sand-700">
          <p className="font-bold text-sand-900">
            Valor inventario: ₡{valuation.totalValueCrc.toLocaleString("es-CR")}
          </p>
          <p>{valuation.totalUnits} unidades disponibles</p>
          {valuation.byLocation.map((location) => (
            <p key={location.locationName}>
              {location.locationName}: {location.availableQuantity} uds · ₡{location.valueCrc.toLocaleString("es-CR")}
            </p>
          ))}
        </div>
      )}
      <div className="grid gap-2 sm:grid-cols-4">
        <Input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Producto"
          aria-label="Producto clínico"
        />
        <select
          value={type}
          onChange={(event) => setType(event.target.value as ClinicInventoryItemType)}
          className="field-input"
          aria-label="Tipo de producto clínico"
        >
          <option value="Vaccine">Vacuna</option>
          <option value="Medication">Medicamento</option>
          <option value="Dewormer">Antiparasitario</option>
          <option value="Supply">Insumo</option>
          <option value="Food">Alimento</option>
          <option value="Service">Servicio</option>
        </select>
        <Input
          value={unit}
          onChange={(event) => setUnit(event.target.value)}
          placeholder="Unidad"
          aria-label="Unidad"
        />
        <input
          type="number"
          min={0}
          value={minimumStock}
          onChange={(event) => setMinimumStock(Number(event.target.value))}
          className="field-input"
          aria-label="Stock mínimo"
        />
      </div>
      <Button
        disabled={!name.trim() || !unit.trim() || isSavingItem}
        onClick={() => onAddItem({ name, type, unit, minimumStock })}
      >
        {isSavingItem ? "Creando..." : "Crear producto"}
      </Button>
      <div className="grid gap-2 sm:grid-cols-3">
        {inventory.map((item) => (
          <article
            key={item.id}
            className={`rounded-xl border p-3 text-xs ${item.isBelowMinimum ? "border-warn-300 bg-warn-50 text-warn-800" : "border-rescue-100 bg-surface text-sand-700"}`}
          >
            <p className="font-bold text-sand-900">{item.name}</p>
            <p>
              {item.type} · {item.totalAvailable} {item.unit}
            </p>
            <p>Mínimo: {item.minimumStock}</p>
          </article>
        ))}
      </div>
      <div className="grid gap-2 sm:grid-cols-3">
        <select
          value={lotItemId}
          onChange={(event) => setLotItemId(event.target.value)}
          className="field-input"
          aria-label="Producto para lote"
        >
          <option value="">Producto para lote</option>
          {inventory.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
        <Input
          value={lotNumber}
          onChange={(event) => setLotNumber(event.target.value)}
          placeholder="Lote"
          aria-label="Lote"
        />
        <Input
          type="date"
          value={expiresAt}
          onChange={(event) => setExpiresAt(event.target.value)}
          aria-label="Vencimiento"
        />
        <input
          type="number"
          min={1}
          value={quantity}
          onChange={(event) => setQuantity(Number(event.target.value))}
          className="field-input"
          aria-label="Cantidad lote"
        />
        <input
          type="number"
          min={0}
          value={unitCostCrc}
          onChange={(event) => setUnitCostCrc(Number(event.target.value))}
          className="field-input"
          aria-label="Costo unitario"
        />
        <Input
          value={supplierName}
          onChange={(event) => setSupplierName(event.target.value)}
          placeholder="Proveedor"
          aria-label="Proveedor"
        />
      </div>
      <Button
        disabled={!lotItemId || !lotNumber.trim() || isReceivingLot}
        onClick={() =>
          onReceiveLot(lotItemId, {
            lotNumber,
            expiresAt: expiresAt || null,
            quantity,
            unitCostCrc,
            supplierName: supplierName || null,
          })
        }
      >
        {isReceivingLot ? "Recibiendo..." : "Recibir lote"}
      </Button>
      <div className="grid gap-2 sm:grid-cols-[1fr_100px_1fr_auto]">
        <select
          value={adjustLotId}
          onChange={(event) => setAdjustLotId(event.target.value)}
          className="field-input"
          aria-label="Lote para ajuste"
        >
          <option value="">Lote para ajuste</option>
          {lots.map((lot) => (
            <option key={lot.id} value={lot.id}>
              {lot.itemName} · {lot.lotNumber} ({lot.availableQuantity})
            </option>
          ))}
        </select>
        <input
          type="number"
          value={adjustDelta}
          onChange={(event) => setAdjustDelta(Number(event.target.value))}
          className="field-input"
          aria-label="Cantidad ajuste"
        />
        <Input
          value={adjustReason}
          onChange={(event) => setAdjustReason(event.target.value)}
          placeholder="Motivo ajuste"
          aria-label="Motivo ajuste"
        />
        <Button
          disabled={!adjustLotId || adjustDelta === 0 || !adjustReason.trim() || isAdjustingLot}
          onClick={() => onAdjustLot(adjustLotId, adjustDelta, adjustReason)}
        >
          {isAdjustingLot ? "Ajustando..." : "Ajustar"}
        </Button>
      </div>
    </section>
  );
}

function ClinicAgendaAuditSection({
  entries,
  isLoading,
  isDownloading,
  onExport,
}: {
  entries: ClinicAgendaAuditEntryDto[];
  isLoading: boolean;
  isDownloading: boolean;
  onExport: () => void;
}) {
  return (
    <section className="space-y-3 rounded-2xl border border-sand-200 bg-surface p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-bold text-sand-900">Auditoría de agenda</h3>
          <p className="text-xs text-sand-500">Cambios recientes exportables para control interno.</p>
        </div>
        <Button variant="secondary" disabled={isDownloading} onClick={onExport}>
          {isDownloading ? "Exportando..." : "CSV"}
        </Button>
      </div>
      {isLoading ? (
        <div className="h-14 animate-pulse rounded-xl bg-sand-100" />
      ) : entries.length === 0 ? (
        <p className="rounded-xl border border-dashed border-sand-300 p-3 text-xs text-sand-500">
          No hay movimientos de agenda en este rango.
        </p>
      ) : (
        <ul className="space-y-2">
          {entries.slice(0, 8).map((entry) => (
            <li key={entry.id} className="rounded-xl border border-sand-100 bg-surface-warm p-3 text-xs text-sand-600">
              <p className="font-bold text-sand-900">{entry.action}</p>
              <p>{new Date(entry.performedAt).toLocaleString("es-CR")}</p>
              {entry.details && <p className="truncate">{entry.details}</p>}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function toLocalDateTimeInput(value: string) {
  return new Date(value).toISOString().slice(0, 16);
}

function ClinicScheduleBlocksList({
  blocks,
  isLoading,
  isUpdating,
  onUpdate,
  onDelete,
}: {
  blocks: ClinicScheduleBlockDto[];
  isLoading: boolean;
  isUpdating: boolean;
  onUpdate: (block: ClinicScheduleBlockDto, startsAt: string, endsAt: string, reason: string) => void;
  onDelete: (block: ClinicScheduleBlockDto) => void;
}) {
  const [editingId, setEditingId] = useState<string | null>(null);
  const [startsAt, setStartsAt] = useState("");
  const [endsAt, setEndsAt] = useState("");
  const [reason, setReason] = useState("");

  if (isLoading) return <div className="h-16 animate-pulse rounded-xl bg-warn-100" />;
  if (blocks.length === 0)
    return (
      <p className="rounded-xl border border-dashed border-warn-300 p-3 text-xs text-warn-700">
        No hay bloqueos para este rango.
      </p>
    );

  return (
    <ul className="space-y-2">
      {blocks.map((block) => (
        <li key={block.blockId} className="space-y-2 rounded-xl border border-warn-200 bg-surface p-3">
          <div className="flex items-start justify-between gap-2">
            <div>
              <p className="text-sm font-bold text-warn-900">{block.reason}</p>
              <p className="text-xs text-warn-700">
                {block.veterinarianName} ·{" "}
                {new Date(block.startsAt).toLocaleTimeString("es-CR", { hour: "2-digit", minute: "2-digit" })}-
                {new Date(block.endsAt).toLocaleTimeString("es-CR", { hour: "2-digit", minute: "2-digit" })}
              </p>
            </div>
            <div className="flex gap-2">
              <Button
                variant="secondary"
                disabled={isUpdating}
                onClick={() => {
                  setEditingId(block.blockId);
                  setStartsAt(toLocalDateTimeInput(block.startsAt));
                  setEndsAt(toLocalDateTimeInput(block.endsAt));
                  setReason(block.reason);
                }}
              >
                Editar
              </Button>
              <Button variant="secondary" disabled={isUpdating} onClick={() => onDelete(block)}>
                Eliminar
              </Button>
            </div>
          </div>
          {editingId === block.blockId && (
            <div className="grid gap-2 sm:grid-cols-[1fr_1fr_1fr_auto]">
              <Input
                type="datetime-local"
                value={startsAt}
                onChange={(event) => setStartsAt(event.target.value)}
                aria-label="Nuevo inicio del bloqueo"
              />
              <Input
                type="datetime-local"
                value={endsAt}
                onChange={(event) => setEndsAt(event.target.value)}
                aria-label="Nuevo fin del bloqueo"
              />
              <Input
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                aria-label="Nuevo motivo del bloqueo"
              />
              <Button
                disabled={!startsAt || !endsAt || isUpdating}
                onClick={() => onUpdate(block, startsAt, endsAt, reason)}
              >
                Guardar
              </Button>
            </div>
          )}
        </li>
      ))}
    </ul>
  );
}

function ClinicAgendaSection({
  date,
  onDateChange,
  mode,
  onModeChange,
  agenda,
  isLoading,
  onAdvance,
  onCancel,
  onNoShow,
  isUpdating,
  onReschedule,
  isRescheduling,
  templates,
  onCreateConsultation,
  isSavingConsultation,
}: {
  date: string;
  onDateChange: (value: string) => void;
  mode: "day" | "week";
  onModeChange: (value: "day" | "week") => void;
  agenda: ClinicAgendaItemDto[];
  isLoading: boolean;
  onAdvance: (appointment: ClinicAgendaItemDto) => void;
  onCancel: (appointment: ClinicAgendaItemDto) => void;
  onNoShow: (appointment: ClinicAgendaItemDto) => void;
  isUpdating: boolean;
  onReschedule: (appointment: ClinicAgendaItemDto, startsAt: string, durationMinutes: number) => void;
  isRescheduling: boolean;
  templates: ClinicalConsultationTemplateDto[];
  onCreateConsultation: (
    appointment: ClinicAgendaItemDto,
    payload: {
      reason: string;
      subjective: string;
      objective: string;
      assessment: string;
      plan: string;
      weightKg: number | null;
      temperatureC: number | null;
      heartRateBpm: number | null;
      respiratoryRateRpm: number | null;
      bodyConditionScore: number | null;
      painScore: number | null;
      hydrationStatus: string | null;
      diagnosis: string;
      treatment: string;
      ownerSummary: string;
      prescriptionInstructions: string | null;
    },
    signedByName: string,
    attachment: File | null,
  ) => void;
  isSavingConsultation: boolean;
}) {
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingStartsAt, setEditingStartsAt] = useState("");
  const [editingDuration, setEditingDuration] = useState(30);
  const [consultationId, setConsultationId] = useState<string | null>(null);

  return (
    <section className="space-y-3 rounded-2xl border border-sand-200 bg-surface p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-bold text-sand-900">Agenda {mode === "day" ? "del día" : "semanal"}</h3>
          <p className="text-xs text-sand-500">Controla recepción, sala de espera y consultas activas.</p>
        </div>
        <div className="flex items-center gap-2">
          <select
            value={mode}
            onChange={(event) => onModeChange(event.target.value as "day" | "week")}
            className="field-input max-w-28"
            aria-label="Modo de agenda"
          >
            <option value="day">Día</option>
            <option value="week">Semana</option>
          </select>
          <input
            type="date"
            value={date}
            onChange={(event) => onDateChange(event.target.value)}
            className="field-input max-w-36"
            aria-label="Fecha de agenda"
          />
        </div>
      </div>
      {isLoading ? (
        <div className="h-24 animate-pulse rounded-xl bg-sand-100" />
      ) : agenda.length === 0 ? (
        <p className="rounded-xl border border-dashed border-sand-300 p-4 text-center text-sm text-sand-500">
          No hay citas para este día.
        </p>
      ) : (
        <ul className="space-y-2">
          {agenda.map((appointment) => {
            const next = NEXT_STATUS[appointment.status];
            const startsAt = new Date(appointment.startsAt);
            const endsAt = new Date(appointment.endsAt);
            return (
              <li
                key={appointment.appointmentId}
                className="space-y-3 rounded-xl border border-sand-100 bg-surface-warm p-3"
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-bold text-sand-900">{appointment.petName}</p>
                    <p className="text-xs text-sand-500">
                      {mode === "week" && `${startsAt.toLocaleDateString("es-CR")} · `}
                      {appointment.veterinarianName} ·{" "}
                      {startsAt.toLocaleTimeString("es-CR", { hour: "2-digit", minute: "2-digit" })}-
                      {endsAt.toLocaleTimeString("es-CR", { hour: "2-digit", minute: "2-digit" })}
                    </p>
                  </div>
                  <span className="rounded-full bg-brand-100 px-2 py-1 text-[11px] font-bold text-brand-700">
                    {STATUS_LABELS[appointment.status]}
                  </span>
                </div>
                <div className="flex flex-wrap gap-2">
                  {next && (
                    <Button onClick={() => onAdvance(appointment)} disabled={isUpdating}>
                      Pasar a {STATUS_LABELS[next]}
                    </Button>
                  )}
                  {appointment.status === "Confirmed" && (
                    <Button variant="secondary" onClick={() => onNoShow(appointment)} disabled={isUpdating}>
                      No-show
                    </Button>
                  )}
                  {appointment.status !== "Completed" &&
                    appointment.status !== "Cancelled" &&
                    appointment.status !== "NoShow" && (
                      <>
                        <Button
                          variant="secondary"
                          onClick={() => {
                            setEditingId(appointment.appointmentId);
                            const local = new Date(appointment.startsAt);
                            setEditingStartsAt(local.toISOString().slice(0, 16));
                            setEditingDuration(
                              Math.max(
                                15,
                                Math.round((new Date(appointment.endsAt).getTime() - local.getTime()) / 60000),
                              ),
                            );
                          }}
                          disabled={isUpdating || isRescheduling}
                        >
                          Reprogramar
                        </Button>
                        <Button variant="secondary" onClick={() => onCancel(appointment)} disabled={isUpdating}>
                          Cancelar
                        </Button>
                      </>
                    )}
                  {appointment.status === "InConsultation" && (
                    <Button
                      variant="secondary"
                      disabled={isSavingConsultation}
                      onClick={() => setConsultationId(appointment.appointmentId)}
                    >
                      Documentar consulta
                    </Button>
                  )}
                </div>
                {editingId === appointment.appointmentId && (
                  <div className="grid gap-2 rounded-xl border border-sand-200 bg-surface p-3 sm:grid-cols-[1fr_90px_auto]">
                    <Input
                      type="datetime-local"
                      value={editingStartsAt}
                      onChange={(event) => setEditingStartsAt(event.target.value)}
                      aria-label="Nueva hora de cita"
                    />
                    <input
                      type="number"
                      min={15}
                      max={480}
                      step={15}
                      value={editingDuration}
                      onChange={(event) => setEditingDuration(Number(event.target.value))}
                      className="field-input"
                      aria-label="Duración nueva"
                    />
                    <Button
                      disabled={!editingStartsAt || isRescheduling}
                      onClick={() => onReschedule(appointment, editingStartsAt, editingDuration)}
                    >
                      Guardar
                    </Button>
                  </div>
                )}
                {consultationId === appointment.appointmentId && (
                  <ClinicalConsultationInlineForm
                    appointment={appointment}
                    templates={templates}
                    isSaving={isSavingConsultation}
                    onCancel={() => setConsultationId(null)}
                    onSubmit={(payload, signedByName, attachment) =>
                      onCreateConsultation(appointment, payload, signedByName, attachment)
                    }
                  />
                )}
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}

function nullableNumber(value: string) {
  if (!value.trim()) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function nullableInt(value: string) {
  if (!value.trim()) return null;
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : null;
}

function ClinicalConsultationInlineForm({
  appointment,
  templates,
  isSaving,
  onSubmit,
  onCancel,
}: {
  appointment: ClinicAgendaItemDto;
  templates: ClinicalConsultationTemplateDto[];
  isSaving: boolean;
  onSubmit: (
    payload: {
      reason: string;
      subjective: string;
      objective: string;
      assessment: string;
      plan: string;
      weightKg: number | null;
      temperatureC: number | null;
      heartRateBpm: number | null;
      respiratoryRateRpm: number | null;
      bodyConditionScore: number | null;
      painScore: number | null;
      hydrationStatus: string | null;
      diagnosis: string;
      treatment: string;
      ownerSummary: string;
      prescriptionInstructions: string | null;
    },
    signedByName: string,
    attachment: File | null,
  ) => void;
  onCancel: () => void;
}) {
  const [reason, setReason] = useState("Consulta veterinaria");
  const [subjective, setSubjective] = useState("");
  const [objective, setObjective] = useState("");
  const [assessment, setAssessment] = useState("");
  const [plan, setPlan] = useState("");
  const [weightKg, setWeightKg] = useState("");
  const [temperatureC, setTemperatureC] = useState("");
  const [heartRateBpm, setHeartRateBpm] = useState("");
  const [respiratoryRateRpm, setRespiratoryRateRpm] = useState("");
  const [bodyConditionScore, setBodyConditionScore] = useState("");
  const [painScore, setPainScore] = useState("");
  const [hydrationStatus, setHydrationStatus] = useState("");
  const [diagnosis, setDiagnosis] = useState("");
  const [treatment, setTreatment] = useState("");
  const [ownerSummary, setOwnerSummary] = useState("");
  const [prescriptionInstructions, setPrescriptionInstructions] = useState("");
  const [attachment, setAttachment] = useState<File | null>(null);
  const [signedByName, setSignedByName] = useState(appointment.veterinarianName);

  function applyTemplate(key: string) {
    const template = templates.find((item) => item.key === key);
    if (!template) return;
    setReason(template.reason);
    setSubjective(template.subjective);
    setObjective(template.objective);
    setAssessment(template.assessment);
    setPlan(template.plan);
    setDiagnosis(template.diagnosis);
    setTreatment(template.treatment);
    setOwnerSummary(template.ownerSummary);
    setPrescriptionInstructions(template.prescriptionInstructions ?? "");
  }

  return (
    <div className="space-y-3 rounded-xl border border-trust-200 bg-trust-50 p-3">
      <p className="text-sm font-bold text-trust-900">Consulta estructurada</p>
      <select
        className="field-input w-full"
        defaultValue=""
        onChange={(event) => applyTemplate(event.target.value)}
        aria-label="Plantilla de consulta"
      >
        <option value="">Aplicar plantilla</option>
        {templates.map((template) => (
          <option key={template.key} value={template.key}>
            {template.label}
          </option>
        ))}
      </select>
      <Input value={reason} onChange={(event) => setReason(event.target.value)} aria-label="Motivo de consulta" />
      <textarea
        className="field-input w-full"
        rows={2}
        value={subjective}
        onChange={(event) => setSubjective(event.target.value)}
        placeholder="Subjetivo"
      />
      <textarea
        className="field-input w-full"
        rows={2}
        value={objective}
        onChange={(event) => setObjective(event.target.value)}
        placeholder="Objetivo"
      />
      <textarea
        className="field-input w-full"
        rows={2}
        value={assessment}
        onChange={(event) => setAssessment(event.target.value)}
        placeholder="Evaluación"
      />
      <textarea
        className="field-input w-full"
        rows={2}
        value={plan}
        onChange={(event) => setPlan(event.target.value)}
        placeholder="Plan"
      />
      <div className="grid gap-2 sm:grid-cols-3">
        <Input
          value={weightKg}
          onChange={(event) => setWeightKg(event.target.value)}
          placeholder="Peso kg"
          aria-label="Peso kg"
        />
        <Input
          value={temperatureC}
          onChange={(event) => setTemperatureC(event.target.value)}
          placeholder="Temp C"
          aria-label="Temperatura"
        />
        <Input
          value={heartRateBpm}
          onChange={(event) => setHeartRateBpm(event.target.value)}
          placeholder="FC"
          aria-label="Frecuencia cardíaca"
        />
        <Input
          value={respiratoryRateRpm}
          onChange={(event) => setRespiratoryRateRpm(event.target.value)}
          placeholder="FR"
          aria-label="Frecuencia respiratoria"
        />
        <Input
          value={bodyConditionScore}
          onChange={(event) => setBodyConditionScore(event.target.value)}
          placeholder="CC 1-9"
          aria-label="Condición corporal"
        />
        <Input
          value={painScore}
          onChange={(event) => setPainScore(event.target.value)}
          placeholder="Dolor 0-10"
          aria-label="Dolor"
        />
      </div>
      <Input
        value={hydrationStatus}
        onChange={(event) => setHydrationStatus(event.target.value)}
        placeholder="Hidratación"
        aria-label="Hidratación"
      />
      <Input
        value={diagnosis}
        onChange={(event) => setDiagnosis(event.target.value)}
        placeholder="Diagnóstico"
        aria-label="Diagnóstico"
      />
      <Input
        value={treatment}
        onChange={(event) => setTreatment(event.target.value)}
        placeholder="Tratamiento"
        aria-label="Tratamiento"
      />
      <textarea
        className="field-input w-full"
        rows={2}
        value={prescriptionInstructions}
        onChange={(event) => setPrescriptionInstructions(event.target.value)}
        placeholder="Receta / indicaciones imprimibles"
      />
      <textarea
        className="field-input w-full"
        rows={2}
        value={ownerSummary}
        onChange={(event) => setOwnerSummary(event.target.value)}
        placeholder="Resumen para el dueño"
      />
      <input
        type="file"
        accept="application/pdf,image/jpeg,image/png"
        onChange={(event) => setAttachment(event.target.files?.[0] ?? null)}
        className="block w-full text-xs text-sand-600"
        aria-label="Adjunto de consulta"
      />
      <Input
        value={signedByName}
        onChange={(event) => setSignedByName(event.target.value)}
        aria-label="Firma veterinaria"
      />
      <div className="flex gap-2">
        <Button
          disabled={!reason.trim() || !ownerSummary.trim() || !signedByName.trim() || isSaving}
          onClick={() =>
            onSubmit(
              {
                reason,
                subjective,
                objective,
                assessment,
                plan,
                weightKg: nullableNumber(weightKg),
                temperatureC: nullableNumber(temperatureC),
                heartRateBpm: nullableInt(heartRateBpm),
                respiratoryRateRpm: nullableInt(respiratoryRateRpm),
                bodyConditionScore: nullableInt(bodyConditionScore),
                painScore: nullableInt(painScore),
                hydrationStatus: hydrationStatus.trim() || null,
                diagnosis,
                treatment,
                ownerSummary,
                prescriptionInstructions: prescriptionInstructions.trim() || null,
              },
              signedByName,
              attachment,
            )
          }
        >
          {isSaving ? "Guardando..." : "Cerrar y firmar"}
        </Button>
        <Button variant="secondary" onClick={onCancel} disabled={isSaving}>
          Cancelar
        </Button>
      </div>
    </div>
  );
}
