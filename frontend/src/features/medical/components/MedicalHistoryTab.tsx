import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import { Button, Input, Card, Drawer } from "@/shared/ui";
import {
  useMedicalHistory,
  useMedicalCount,
  useAddMedicalRecord,
  useDeleteMedicalRecord,
  useUpdateMedicalRecord,
  useVetReminders,
  useCompleteReminder,
  useCreateVetReminder,
  useDeleteVetReminder,
  useExportMedicalPdf,
  useClinicAccessLog,
} from "@/features/medical/hooks/useMedical";
import { PetClinicAccessManager } from "./PetClinicAccessManager";
import { ReminderCalendar } from "./ReminderCalendar";
import { WeightTrendChart } from "./WeightTrendChart";
import { HealthScoreCard } from "./HealthScoreCard";
import { usePublicClinics } from "@/features/clinics/hooks/useClinics";
import type {
  MedicalRecordType,
  MedicalRecordDto,
  VetReminderDto,
} from "@/features/medical/api/medicalApi";

// ── Locale maps ───────────────────────────────────────────────────────────────

const TYPE_LABEL: Record<MedicalRecordType, string> = {
  Vaccine: "💉 Vacuna",
  Deworming: "🪱 Desparasitación",
  Checkup: "🩺 Consulta",
  Surgery: "🔪 Cirugía",
  Medication: "💊 Medicamento",
  Allergy: "🌿 Alergia",
  Other: "📋 Otro",
};

const ALL_TYPES: MedicalRecordType[] = [
  "Checkup",
  "Vaccine",
  "Deworming",
  "Medication",
  "Surgery",
  "Allergy",
  "Other",
];

// ── Record card ───────────────────────────────────────────────────────────────

function RecordCard({
  record,
  petId,
}: {
  record: MedicalRecordDto;
  petId: string;
}) {
  const isClinic = record.source === "Clinic";
  const deleteMutation = useDeleteMedicalRecord(petId);
  const updateMutation = useUpdateMedicalRecord(petId);

  const [confirmDelete, setConfirmDelete] = useState(false);
  const [editOpen, setEditOpen] = useState(false);

  // Edit form state — kept in RecordCard so it resets cleanly
  const [editType, setEditType] = useState<MedicalRecordType>(
    record.type as MedicalRecordType,
  );
  const [editDate, setEditDate] = useState(record.date);
  const [editDesc, setEditDesc] = useState(record.description);
  const [editDescError, setEditDescError] = useState("");
  const [editVet, setEditVet] = useState(record.vetName ?? "");
  const [editClinic, setEditClinic] = useState(record.clinicName ?? "");
  const [editNextDue, setEditNextDue] = useState(record.nextDueDate ?? "");

  const today = new Date().toISOString().slice(0, 10);

  const handleDelete = () => {
    deleteMutation.mutate(record.id, {
      onSuccess: () => toast.success("Registro eliminado"),
      onError: () => {
        toast.error("No se pudo eliminar");
        setConfirmDelete(false);
      },
    });
  };

  const handleUpdate = () => {
    if (!editDesc.trim()) {
      setEditDescError("La descripción es requerida");
      return;
    }
    setEditDescError("");
    updateMutation.mutate(
      {
        recordId: record.id,
        payload: {
          type: editType,
          date: editDate,
          description: editDesc.trim(),
          vetName: editVet.trim() || undefined,
          clinicName: editClinic.trim() || undefined,
          nextDueDate: editNextDue || undefined,
        },
      },
      {
        onSuccess: () => {
          toast.success("Registro actualizado");
          setEditOpen(false);
        },
        onError: () => toast.error("No se pudo actualizar"),
      },
    );
  };

  const closeEdit = () => {
    setEditOpen(false);
    setEditDescError("");
    setEditType(record.type as MedicalRecordType);
    setEditDate(record.date);
    setEditDesc(record.description);
    setEditVet(record.vetName ?? "");
    setEditClinic(record.clinicName ?? "");
    setEditNextDue(record.nextDueDate ?? "");
  };

  return (
    <>
      {/* ── Edit record drawer ─────────────────────────────────────────── */}
      <Drawer
        isOpen={editOpen}
        onClose={closeEdit}
        title="Editar registro médico"
        side="bottom"
      >
        <div className="space-y-4 pb-safe">
          <div>
            <label
              htmlFor={`edit-type-${record.id}`}
              className="mb-1 block text-xs font-medium text-sand-600"
            >
              Tipo
            </label>
            <select
              id={`edit-type-${record.id}`}
              value={editType}
              onChange={(e) => setEditType(e.target.value as MedicalRecordType)}
              className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2.5 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
            >
              {ALL_TYPES.map((t) => (
                <option key={t} value={t}>
                  {TYPE_LABEL[t]}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label
              htmlFor={`edit-date-${record.id}`}
              className="mb-1 block text-xs font-medium text-sand-600"
            >
              Fecha
            </label>
            <Input
              id={`edit-date-${record.id}`}
              type="date"
              value={editDate}
              max={today}
              onChange={(e) => setEditDate(e.target.value)}
            />
          </div>

          <div>
            <label
              htmlFor={`edit-desc-${record.id}`}
              className="mb-1 block text-xs font-medium text-sand-600"
            >
              Descripción{" "}
              <span aria-hidden="true" className="text-danger-500">
                *
              </span>
            </label>
            <textarea
              id={`edit-desc-${record.id}`}
              value={editDesc}
              onChange={(e) => {
                setEditDesc(e.target.value);
                if (editDescError) setEditDescError("");
              }}
              rows={3}
              aria-describedby={
                editDescError ? `edit-desc-err-${record.id}` : undefined
              }
              aria-invalid={!!editDescError}
              className={`w-full rounded-xl border px-3 py-2 text-sm text-sand-800 placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400 ${
                editDescError
                  ? "border-danger-400 bg-danger-50"
                  : "border-sand-200 bg-white"
              }`}
              placeholder="Ej. Vacuna anti-rábica administrada sin reacciones"
            />
            {editDescError && (
              <p
                id={`edit-desc-err-${record.id}`}
                role="alert"
                className="mt-1 text-xs text-danger-600"
              >
                {editDescError}
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label
                htmlFor={`edit-vet-${record.id}`}
                className="mb-1 block text-xs font-medium text-sand-600"
              >
                Veterinario
              </label>
              <Input
                id={`edit-vet-${record.id}`}
                placeholder="Dr/a. Nombre"
                value={editVet}
                onChange={(e) => setEditVet(e.target.value)}
              />
            </div>
            <div>
              <label
                htmlFor={`edit-clinic-${record.id}`}
                className="mb-1 block text-xs font-medium text-sand-600"
              >
                Clínica
              </label>
              <Input
                id={`edit-clinic-${record.id}`}
                placeholder="Nombre clínica"
                value={editClinic}
                onChange={(e) => setEditClinic(e.target.value)}
              />
            </div>
          </div>

          <div>
            <label
              htmlFor={`edit-next-${record.id}`}
              className="mb-1 block text-xs font-medium text-sand-600"
            >
              Próxima cita (opcional)
            </label>
            <Input
              id={`edit-next-${record.id}`}
              type="date"
              value={editNextDue}
              min={today}
              onChange={(e) => setEditNextDue(e.target.value)}
            />
          </div>

          <Button
            fullWidth
            onClick={handleUpdate}
            loading={updateMutation.isPending}
            disabled={!editDesc.trim()}
          >
            Guardar cambios
          </Button>
        </div>
      </Drawer>

      {/* ── Record card ────────────────────────────────────────────────── */}
      <li className="rounded-xl border border-sand-100 bg-surface-warm p-4 space-y-1">
        <div className="flex items-start justify-between gap-2">
          <div className="flex items-center gap-2 min-w-0">
            <span className="text-sm font-semibold text-sand-800">
              {TYPE_LABEL[record.type as MedicalRecordType] ?? record.type}
            </span>
            {isClinic && (
              <span className="shrink-0 rounded-full bg-trust-100 px-2 py-0.5 text-xs font-medium text-trust-700">
                <span aria-hidden="true">🏥</span>{" "}
                {record.clinicName ?? "Clínica"}
              </span>
            )}
          </div>
          <div className="flex items-center gap-1 shrink-0">
            <span className="text-xs text-sand-500">{record.date}</span>
            {!isClinic && (
              <button
                type="button"
                onClick={() => setEditOpen(true)}
                aria-label={`Editar: ${record.description}`}
                className="flex h-7 w-7 items-center justify-center rounded-lg text-brand-400 hover:bg-brand-50 hover:text-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                <svg
                  viewBox="0 0 16 16"
                  fill="currentColor"
                  className="h-3.5 w-3.5"
                  aria-hidden="true"
                >
                  <path d="M11.013 1.427a1.75 1.75 0 0 1 2.474 0l1.086 1.086a1.75 1.75 0 0 1 0 2.474l-8.61 8.61c-.21.21-.47.364-.756.445l-3.251.93a.75.75 0 0 1-.927-.928l.929-3.25c.081-.286.235-.547.445-.758l8.61-8.61Zm.176 4.823L9.75 4.81l-6.286 6.287a.253.253 0 0 0-.064.108l-.558 1.953 1.953-.558a.253.253 0 0 0 .108-.064Zm1.238-3.763a.25.25 0 0 0-.354 0L10.811 3.75l1.439 1.44 1.263-1.263a.25.25 0 0 0 0-.354Z" />
                </svg>
              </button>
            )}
            {confirmDelete ? (
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  onClick={handleDelete}
                  disabled={deleteMutation.isPending}
                  className="text-xs font-semibold text-danger-600 hover:text-danger-800 disabled:opacity-50"
                >
                  {deleteMutation.isPending ? "…" : "Confirmar"}
                </button>
                <button
                  type="button"
                  onClick={() => setConfirmDelete(false)}
                  className="text-xs text-sand-400 hover:text-sand-600"
                >
                  No
                </button>
              </div>
            ) : (
              <button
                type="button"
                onClick={() => setConfirmDelete(true)}
                aria-label={`Eliminar: ${record.description}`}
                className="flex h-7 w-7 items-center justify-center rounded-lg text-sand-300 hover:bg-danger-50 hover:text-danger-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400"
              >
                <svg
                  viewBox="0 0 16 16"
                  fill="currentColor"
                  className="h-3.5 w-3.5"
                  aria-hidden="true"
                >
                  <path d="M11 1.75V3h2.25a.75.75 0 0 1 0 1.5H2.75a.75.75 0 0 1 0-1.5H5V1.75C5 .784 5.784 0 6.75 0h2.5C10.216 0 11 .784 11 1.75ZM4.496 6.675l.66 6.6a.25.25 0 0 0 .249.225h5.19a.25.25 0 0 0 .249-.225l.66-6.6a.75.75 0 0 1 1.492.149l-.66 6.6A1.748 1.748 0 0 1 10.595 15h-5.19a1.75 1.75 0 0 1-1.741-1.575l-.66-6.6a.75.75 0 1 1 1.492-.15ZM6.5 1.75V3h3V1.75a.25.25 0 0 0-.25-.25h-2.5a.25.25 0 0 0-.25.25Z" />
                </svg>
              </button>
            )}
          </div>
        </div>
        <p className="text-sm text-sand-700">{record.description}</p>
        {record.weightKg != null && (
          <p className="text-xs font-medium text-sand-600">
            <span aria-hidden="true">⚖️</span> Peso: {record.weightKg} kg
          </p>
        )}
        {record.type === "Medication" && record.dosageDescription && (
          <p className="text-xs text-sand-600">
            <span aria-hidden="true">💊</span> {record.dosageDescription}
            {record.frequency ? ` — ${record.frequency}` : ""}
            {record.durationDays ? ` (${record.durationDays} días)` : ""}
          </p>
        )}
        {record.vetName && (
          <p className="text-xs text-sand-500">Dr/a. {record.vetName}</p>
        )}
        {record.nextDueDate && (
          <p className="text-xs font-medium text-warn-700">
            <span aria-hidden="true">⏰</span> Próxima cita:{" "}
            {record.nextDueDate}
          </p>
        )}
        {record.documentUrl && (
          <a
            href={record.documentUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs font-medium text-brand-600 hover:underline"
          >
            <span aria-hidden="true">📄</span> Ver documento adjunto
          </a>
        )}
      </li>
    </>
  );
}

// ── Reminder card ─────────────────────────────────────────────────────────────

function ReminderCard({
  reminder,
  petId,
}: {
  reminder: VetReminderDto;
  petId: string;
}) {
  const complete = useCompleteReminder(petId);
  const deleteReminder = useDeleteVetReminder(petId);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const isOverdue =
    !reminder.isCompleted &&
    reminder.dueDate < new Date().toISOString().slice(0, 10);

  return (
    <li
      className={`rounded-xl border p-3 space-y-1 ${
        reminder.isCompleted
          ? "border-sand-100 bg-sand-50 opacity-60"
          : isOverdue
            ? "border-danger-200 bg-danger-50"
            : "border-trust-200 bg-trust-50"
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <p
          className={`text-sm font-semibold ${
            reminder.isCompleted
              ? "line-through text-sand-400"
              : isOverdue
                ? "text-danger-700"
                : "text-trust-800"
          }`}
        >
          {reminder.title}
        </p>
        <div className="flex items-center gap-2 shrink-0">
          <span className="text-xs text-sand-500">{reminder.dueDate}</span>
          {confirmDelete ? (
            <div className="flex items-center gap-1">
              <button
                type="button"
                onClick={() =>
                  deleteReminder.mutate(reminder.id, {
                    onSuccess: () => toast.success("Recordatorio eliminado"),
                    onError: () => {
                      toast.error("Error al eliminar");
                      setConfirmDelete(false);
                    },
                  })
                }
                disabled={deleteReminder.isPending}
                className="text-xs font-semibold text-danger-600 hover:text-danger-800 disabled:opacity-50"
              >
                {deleteReminder.isPending ? "…" : "Sí, eliminar"}
              </button>
              <button
                type="button"
                onClick={() => setConfirmDelete(false)}
                className="text-xs text-sand-400"
              >
                No
              </button>
            </div>
          ) : (
            <button
              type="button"
              onClick={() => setConfirmDelete(true)}
              className="text-xs text-sand-300 hover:text-danger-400"
            >
              🗑️
            </button>
          )}
        </div>
      </div>
      {reminder.notes && (
        <p className="text-xs text-sand-600">{reminder.notes}</p>
      )}
      {!reminder.isCompleted && (
        <button
          type="button"
          disabled={complete.isPending}
          onClick={() =>
            complete.mutate(reminder.id, {
              onSuccess: () => toast.success("Recordatorio completado"),
              onError: () => toast.error("Error al completar"),
            })
          }
          className="rounded-lg bg-trust-600 px-3 py-1 text-xs font-semibold text-white hover:bg-trust-700 disabled:opacity-50"
        >
          ✓ Marcar como hecho
        </button>
      )}
    </li>
  );
}

// ── Add record form ───────────────────────────────────────────────────────────

function AddRecordForm({
  petId,
  onClose,
}: {
  petId: string;
  onClose: () => void;
}) {
  const add = useAddMedicalRecord(petId);
  const today = new Date().toISOString().slice(0, 10);
  const [type, setType] = useState<MedicalRecordType>("Checkup");
  const [date, setDate] = useState(today);
  const [description, setDescription] = useState("");
  const [vetName, setVetName] = useState("");
  const [clinicName, setClinicName] = useState("");
  const [nextDueDate, setNextDueDate] = useState("");
  const [document, setDocument] = useState<File | null>(null);

  const handleSubmit = () => {
    if (!description.trim()) {
      toast.error("Agrega una descripción");
      return;
    }
    add.mutate(
      {
        type,
        date,
        description: description.trim(),
        vetName: vetName.trim() || undefined,
        clinicName: clinicName.trim() || undefined,
        nextDueDate: nextDueDate || undefined,
        document: document ?? undefined,
      },
      {
        onSuccess: () => {
          toast.success("Registro agregado");
          onClose();
        },
        onError: () => toast.error("No se pudo guardar"),
      },
    );
  };

  return (
    <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4 space-y-3">
      <h3 className="text-sm font-semibold text-brand-800">
        Nuevo registro médico
      </h3>

      {/* Type */}
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Tipo
        </label>
        <select
          value={type}
          onChange={(e) => setType(e.target.value as MedicalRecordType)}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          {ALL_TYPES.map((t) => (
            <option key={t} value={t}>
              {TYPE_LABEL[t]}
            </option>
          ))}
        </select>
      </div>

      {/* Date */}
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Fecha
        </label>
        <Input
          type="date"
          value={date}
          max={today}
          onChange={(e) => setDate(e.target.value)}
        />
      </div>

      {/* Description */}
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Descripción *
        </label>
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={2}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400"
          placeholder="Ej. Vacuna anti-rábica anual administrada sin reacciones"
        />
      </div>

      {/* Vet / Clinic */}
      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Veterinario
          </label>
          <Input
            placeholder="Dr. Nombre"
            value={vetName}
            onChange={(e) => setVetName(e.target.value)}
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Clínica
          </label>
          <Input
            placeholder="Nombre clínica"
            value={clinicName}
            onChange={(e) => setClinicName(e.target.value)}
          />
        </div>
      </div>

      {/* Next due */}
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Próxima cita (opcional)
        </label>
        <Input
          type="date"
          value={nextDueDate}
          min={today}
          onChange={(e) => setNextDueDate(e.target.value)}
        />
      </div>

      {/* Document */}
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Documento (PDF / foto, máx. 5MB)
        </label>
        <input
          type="file"
          accept=".pdf,image/jpeg,image/png"
          onChange={(e) => setDocument(e.target.files?.[0] ?? null)}
          className="block w-full text-xs text-sand-600 file:mr-3 file:rounded-lg file:border-0 file:bg-brand-100 file:px-3 file:py-1.5 file:text-xs file:font-semibold file:text-brand-700 hover:file:bg-brand-200"
        />
      </div>

      <div className="flex gap-2 pt-1">
        <Button
          onClick={handleSubmit}
          loading={add.isPending}
          disabled={!description.trim()}
          className="flex-1"
        >
          Guardar
        </Button>
        <Button variant="secondary" onClick={onClose} className="flex-1">
          Cancelar
        </Button>
      </div>
    </div>
  );
}

// ── Standalone reminder form ──────────────────────────────────────────────────

function AddReminderForm({
  petId,
  onClose,
}: {
  petId: string;
  onClose: () => void;
}) {
  const create = useCreateVetReminder(petId);
  const tomorrow = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10);
  const [type, setType] = useState<MedicalRecordType>("Vaccine");
  const [dueDate, setDueDate] = useState(tomorrow);
  const [title, setTitle] = useState("");
  const [notes, setNotes] = useState("");

  const handleSubmit = () => {
    if (!title.trim()) {
      toast.error("El título es requerido");
      return;
    }
    create.mutate(
      { type, dueDate, title: title.trim(), notes: notes.trim() || undefined },
      {
        onSuccess: () => {
          toast.success("Recordatorio creado");
          onClose();
        },
        onError: () => toast.error("No se pudo crear el recordatorio"),
      },
    );
  };

  return (
    <div className="rounded-2xl border border-trust-200 bg-trust-50 p-4 space-y-3">
      <h3 className="text-sm font-semibold text-trust-800">
        Nuevo recordatorio
      </h3>
      <select
        value={type}
        onChange={(e) => setType(e.target.value as MedicalRecordType)}
        className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-trust-400"
      >
        {ALL_TYPES.map((t) => (
          <option key={t} value={t}>
            {TYPE_LABEL[t]}
          </option>
        ))}
      </select>
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Título *
        </label>
        <Input
          placeholder="Ej. Refuerzo vacuna antirrábica"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
      </div>
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Fecha *
        </label>
        <Input
          type="date"
          value={dueDate}
          min={tomorrow}
          onChange={(e) => setDueDate(e.target.value)}
        />
      </div>
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Notas
        </label>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-trust-400"
          placeholder="Instrucciones, dosis, etc."
        />
      </div>
      <div className="flex gap-2">
        <Button
          onClick={handleSubmit}
          loading={create.isPending}
          disabled={!title.trim()}
          className="flex-1"
        >
          Crear
        </Button>
        <Button variant="secondary" onClick={onClose} className="flex-1">
          Cancelar
        </Button>
      </div>
    </div>
  );
}

// ── Main tab ──────────────────────────────────────────────────────────────────

const ALL_FILTER_OPTIONS = [
  "Todos",
  ...Object.keys({
    Checkup: 1,
    Vaccine: 1,
    Deworming: 1,
    Medication: 1,
    Surgery: 1,
    Allergy: 1,
    Other: 1,
  }),
] as const;

// ── Clinic access audit log section ──────────────────────────────────────────

function ClinicAccessLogSection({ petId }: { petId: string }) {
  const { data: logs } = useClinicAccessLog(petId, 10);
  if (!logs || logs.length === 0) return null;

  return (
    <details className="text-sm">
      <summary className="cursor-pointer text-xs font-semibold text-sand-400 hover:text-sand-600">
        🔐 Historial de acceso veterinario ({logs.length} acceso
        {logs.length !== 1 ? "s" : ""})
      </summary>
      <ul className="mt-2 space-y-1.5">
        {logs.map((l) => (
          <li
            key={l.logId}
            className="flex items-center justify-between rounded-lg border border-sand-100 px-3 py-2"
          >
            <span className="text-xs font-medium text-sand-700">
              🏥 {l.clinicName ?? "Clínica"}
            </span>
            <span className="text-xs text-sand-400">
              {new Date(l.accessedAt).toLocaleDateString("es-CR", {
                year: "numeric",
                month: "short",
                day: "numeric",
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
          </li>
        ))}
      </ul>
    </details>
  );
}

export function MedicalHistoryTab({
  petId,
  petName = "",
}: {
  petId: string;
  petName?: string;
}) {
  const { data: historyResult, isLoading: loadingRecords } =
    useMedicalHistory(petId);
  const { data: count } = useMedicalCount(petId);
  const { data: reminders, isLoading: loadingReminders } =
    useVetReminders(petId);
  const { data: publicClinics } = usePublicClinics();
  const exportPdf = useExportMedicalPdf(petId);

  const [showAddForm, setShowAddForm] = useState(false);
  const [showReminderForm, setShowReminderForm] = useState(false);
  const [showCalendar, setShowCalendar] = useState(false);
  const [typeFilter, setTypeFilter] = useState<string>("Todos");
  const [searchQuery, setSearchQuery] = useState("");

  const records = historyResult?.records ?? [];
  const historyIsLimited = historyResult?.isLimited ?? false;
  const accessTier = historyResult?.accessTier ?? "explorador";
  const totalCount = historyResult?.totalCount ?? count?.totalRecords ?? 0;
  const completedReminders = reminders?.filter((r) => r.isCompleted) ?? [];

  const pendingReminders = reminders?.filter((r) => !r.isCompleted) ?? [];

  const filteredRecords = records
    .filter((r) => typeFilter === "Todos" || r.type === typeFilter)
    .filter((r) => {
      if (!searchQuery.trim()) return true;
      const q = searchQuery.toLowerCase();
      return (
        r.description.toLowerCase().includes(q) ||
        (r.vetName?.toLowerCase().includes(q) ?? false) ||
        (r.clinicName?.toLowerCase().includes(q) ?? false)
      );
    });

  const availableClinics = publicClinics?.map((c) => ({
    id: c.id,
    name: c.name,
  }));

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h2 className="font-display text-base font-semibold text-sand-800">
          🏥 Historial médico
        </h2>
        <div className="flex gap-2">
          <button
            type="button"
            disabled={exportPdf.isPending}
            onClick={() =>
              exportPdf.mutate(undefined, {
                onError: () => toast.error("No se pudo exportar el PDF"),
              })
            }
            className="rounded-lg border border-sand-300 px-3 py-1.5 text-xs font-semibold text-sand-700 hover:bg-sand-100 disabled:opacity-50"
          >
            {exportPdf.isPending ? "Exportando…" : "📄 Exportar PDF"}
          </button>
          <Button
            size="sm"
            variant="secondary"
            onClick={() => {
              setShowCalendar((v) => !v);
            }}
          >
            {showCalendar ? "Lista" : "📅 Calendario"}
          </Button>
          <Button
            size="sm"
            variant="secondary"
            onClick={() => {
              setShowReminderForm((v) => !v);
              setShowAddForm(false);
            }}
          >
            {showReminderForm ? "Cerrar" : "⏰ Recordatorio"}
          </Button>
          <Button
            size="sm"
            onClick={() => {
              setShowAddForm((v) => !v);
              setShowReminderForm(false);
            }}
          >
            {showAddForm ? "Cerrar" : "+ Registro"}
          </Button>
        </div>
      </div>

      {/* Forms */}
      {showAddForm && (
        <AddRecordForm petId={petId} onClose={() => setShowAddForm(false)} />
      )}
      {showReminderForm && (
        <AddReminderForm
          petId={petId}
          onClose={() => setShowReminderForm(false)}
        />
      )}

      {/* Calendar view */}
      {showCalendar && reminders && (
        <ReminderCalendar
          reminders={[...pendingReminders, ...completedReminders]}
        />
      )}

      {/* Pending reminders */}
      {pendingReminders.length > 0 && (
        <div>
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-sand-500">
            Recordatorios pendientes
          </p>
          <ul className="space-y-2">
            {pendingReminders.map((r) => (
              <ReminderCard key={r.id} reminder={r} petId={petId} />
            ))}
          </ul>
        </div>
      )}

      {/* Records list */}
      <div>
        {/* Health score (Plus+) then weight trend (Familia) */}
        <HealthScoreCard petId={petId} petName={petName} />
        <WeightTrendChart petId={petId} petName={petName} />

        {/* Search input */}
        <div className="relative mb-3 mt-4">
          <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-sand-400 text-sm">
            🔍
          </span>
          <input
            type="search"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Buscar por descripción, veterinario o clínica…"
            className="w-full rounded-xl border border-sand-200 bg-white py-2 pl-8 pr-4 text-sm text-sand-800 placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        {/* Type filter — horizontal scroll on mobile */}
        <div
          className="mb-3 flex gap-1.5 overflow-x-auto pb-1 [scrollbar-width:none] [-webkit-overflow-scrolling:touch]"
          role="group"
          aria-label="Filtrar por tipo de registro"
        >
          {ALL_FILTER_OPTIONS.map((opt) => {
            const label =
              opt === "Todos"
                ? "Todos"
                : (TYPE_LABEL[opt as MedicalRecordType] ?? opt);
            const emoji = label.match(/^(\S+)\s/)?.[1];
            const text = emoji ? label.slice(emoji.length + 1) : label;
            return (
              <button
                key={opt}
                type="button"
                onClick={() => setTypeFilter(opt)}
                aria-pressed={typeFilter === opt}
                className={`shrink-0 rounded-full px-3 py-1 text-xs font-semibold transition-colors ${
                  typeFilter === opt
                    ? "bg-brand-600 text-white"
                    : "bg-sand-100 text-sand-600 hover:bg-sand-200"
                }`}
              >
                {opt !== "Todos" && emoji && (
                  <span aria-hidden="true">{emoji} </span>
                )}
                {text}
              </button>
            );
          })}
        </div>
        {loadingRecords ? (
          <div className="animate-pulse space-y-2">
            <div className="h-16 rounded-xl bg-sand-100" />
            <div className="h-16 rounded-xl bg-sand-100" />
          </div>
        ) : accessTier === "explorador" &&
          totalCount === 0 &&
          !loadingRecords ? (
          <Card padding="sm">
            <p className="text-center text-sm text-sand-400">
              No hay registros médicos aún. Agrega el primero.
            </p>
          </Card>
        ) : accessTier === "explorador" && totalCount > 0 ? (
          // Explorador with clinic records: show count teaser
          <div className="rounded-2xl border border-warn-200 bg-warn-50 p-5 text-center space-y-2">
            <p className="text-2xl">🔒</p>
            <p className="text-sm font-semibold text-warn-800">
              Tu veterinaria tiene registros médicos para esta mascota
            </p>
            {count && (
              <p className="text-sm text-warn-700">
                <strong>
                  {totalCount} registro{totalCount !== 1 ? "s" : ""}
                </strong>
                {count.clinicRecords > 0 &&
                  ` (${count.clinicRecords} de tu veterinaria)`}
                . Actualiza al plan Familia para verlos.
              </p>
            )}
            <a
              href="/planes"
              className="inline-block rounded-lg bg-warn-600 px-4 py-2 text-xs font-semibold text-white hover:bg-warn-700"
            >
              Ver planes →
            </a>
          </div>
        ) : filteredRecords.length > 0 ? (
          <>
            <ul className="space-y-2">
              {filteredRecords.map((r) => (
                <RecordCard key={r.id} record={r} petId={petId} />
              ))}
            </ul>
            {/* Plus preview banner */}
            {historyIsLimited && accessTier === "plus_preview" && (
              <div className="mt-3 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 flex items-center justify-between gap-3">
                <p className="text-xs text-brand-700">
                  Viendo{" "}
                  <strong>
                    {records.length} de {totalCount}
                  </strong>{" "}
                  registros. Actualiza a Plan Familia para ver el historial
                  completo.
                </p>
                <a
                  href="/planes"
                  className="shrink-0 rounded-lg bg-brand-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-brand-700"
                >
                  Ver Familia →
                </a>
              </div>
            )}
          </>
        ) : (
          <Card padding="sm">
            <p className="text-center text-sm text-sand-400">
              {typeFilter === "Todos"
                ? "No hay registros médicos aún. Agrega el primero."
                : `No hay registros de tipo "${typeFilter}".`}
            </p>
          </Card>
        )}
      </div>

      {/* Completed reminders */}
      {completedReminders.length > 0 && (
        <details className="text-sm">
          <summary className="cursor-pointer text-xs font-semibold text-sand-400 hover:text-sand-600">
            {completedReminders.length} recordatorio
            {completedReminders.length !== 1 ? "s" : ""} completado
            {completedReminders.length !== 1 ? "s" : ""}
          </summary>
          <ul className="mt-2 space-y-2">
            {completedReminders.map((r) => (
              <ReminderCard key={r.id} reminder={r} petId={petId} />
            ))}
          </ul>
        </details>
      )}

      {loadingReminders && (
        <div className="animate-pulse">
          <div className="h-10 rounded-xl bg-sand-100" />
        </div>
      )}

      {/* ── Clinic access (Option C) ───────────────────────────────────── */}
      <hr className="border-sand-100" />
      <PetClinicAccessManager
        petId={petId}
        availableClinics={availableClinics}
      />

      {/* ── Clinic access audit log ────────────────────────────────────── */}
      <ClinicAccessLogSection petId={petId} />
    </div>
  );
}
