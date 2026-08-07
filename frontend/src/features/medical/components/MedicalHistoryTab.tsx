import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import { Button, Input, Card } from "@/shared/ui";
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
} from "@/features/medical/hooks/useMedical";
import { PetClinicAccessManager } from "./PetClinicAccessManager";
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
  const [editing, setEditing] = useState(false);

  // Edit form state
  const [editType, setEditType] = useState<MedicalRecordType>(record.type as MedicalRecordType);
  const [editDate, setEditDate] = useState(record.date);
  const [editDesc, setEditDesc] = useState(record.description);
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
    if (!editDesc.trim()) { toast.error("La descripción es requerida"); return; }
    updateMutation.mutate(
      { recordId: record.id, payload: {
        type: editType, date: editDate, description: editDesc.trim(),
        vetName: editVet.trim() || undefined,
        clinicName: editClinic.trim() || undefined,
        nextDueDate: editNextDue || undefined,
      }},
      {
        onSuccess: () => { toast.success("Registro actualizado"); setEditing(false); },
        onError: () => toast.error("No se pudo actualizar"),
      },
    );
  };

  if (editing) {
    return (
      <li className="rounded-xl border border-brand-200 bg-brand-50 p-4 space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-brand-700">Editando registro</span>
          <button type="button" onClick={() => setEditing(false)} className="text-xs text-sand-500 hover:text-sand-700">✕ Cancelar</button>
        </div>
        <select value={editType} onChange={(e) => setEditType(e.target.value as MedicalRecordType)}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-brand-400">
          {ALL_TYPES.map((t) => <option key={t} value={t}>{TYPE_LABEL[t]}</option>)}
        </select>
        <Input type="date" value={editDate} max={today} onChange={(e) => setEditDate(e.target.value)} />
        <textarea value={editDesc} onChange={(e) => setEditDesc(e.target.value)} rows={2}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-brand-400" />
        <div className="grid grid-cols-2 gap-2">
          <Input placeholder="Veterinario" value={editVet} onChange={(e) => setEditVet(e.target.value)} />
          <Input placeholder="Clínica" value={editClinic} onChange={(e) => setEditClinic(e.target.value)} />
        </div>
        <Input type="date" value={editNextDue} min={today} onChange={(e) => setEditNextDue(e.target.value)} />
        <Button onClick={handleUpdate} loading={updateMutation.isPending} disabled={!editDesc.trim()} size="sm">
          Guardar cambios
        </Button>
      </li>
    );
  }

  return (
    <li className="rounded-xl border border-sand-100 bg-surface-warm p-4 space-y-1">
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2 min-w-0">
          <span className="text-sm font-semibold text-sand-800">
            {TYPE_LABEL[record.type as MedicalRecordType] ?? record.type}
          </span>
          {isClinic && (
            <span className="shrink-0 rounded-full bg-trust-100 px-2 py-0.5 text-xs font-medium text-trust-700">
              🏥 {record.clinicName ?? "Clínica"}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <span className="text-xs text-sand-500">{record.date}</span>
          {/* Only owner-created records can be edited */}
          {!isClinic && (
            <button type="button" onClick={() => setEditing(true)}
              className="text-xs text-brand-500 hover:text-brand-700 font-medium">
              ✏️
            </button>
          )}
          {confirmDelete ? (
            <div className="flex items-center gap-1">
              <button type="button" onClick={handleDelete} disabled={deleteMutation.isPending}
                className="text-xs font-semibold text-danger-600 hover:text-danger-800 disabled:opacity-50">
                {deleteMutation.isPending ? "…" : "Confirmar"}
              </button>
              <button type="button" onClick={() => setConfirmDelete(false)} className="text-xs text-sand-400 hover:text-sand-600">
                No
              </button>
            </div>
          ) : (
            <button type="button" onClick={() => setConfirmDelete(true)}
              className="text-xs text-sand-400 hover:text-danger-500 font-medium">
              🗑️
            </button>
          )}
        </div>
      </div>
      <p className="text-sm text-sand-700">{record.description}</p>
      {record.weightKg != null && (
        <p className="text-xs font-medium text-sand-600">⚖️ Peso: {record.weightKg} kg</p>
      )}
      {record.type === "Medication" && record.dosageDescription && (
        <p className="text-xs text-sand-600">💊 {record.dosageDescription}
          {record.frequency ? ` — ${record.frequency}` : ""}
          {record.durationDays ? ` (${record.durationDays} días)` : ""}
        </p>
      )}
      {record.vetName && <p className="text-xs text-sand-500">Dr/a. {record.vetName}</p>}
      {record.nextDueDate && (
        <p className="text-xs font-medium text-warn-700">⏰ Próxima cita: {record.nextDueDate}</p>
      )}
      {record.documentUrl && (
        <a href={record.documentUrl} target="_blank" rel="noopener noreferrer"
          className="text-xs font-medium text-brand-600 hover:underline">
          📄 Ver documento adjunto
        </a>
      )}
    </li>
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
    <li className={`rounded-xl border p-3 space-y-1 ${
      reminder.isCompleted
        ? "border-sand-100 bg-sand-50 opacity-60"
        : isOverdue
          ? "border-danger-200 bg-danger-50"
          : "border-trust-200 bg-trust-50"
    }`}>
      <div className="flex items-start justify-between gap-2">
        <p className={`text-sm font-semibold ${
          reminder.isCompleted ? "line-through text-sand-400"
            : isOverdue ? "text-danger-700" : "text-trust-800"
        }`}>
          {reminder.title}
        </p>
        <div className="flex items-center gap-2 shrink-0">
          <span className="text-xs text-sand-500">{reminder.dueDate}</span>
          {confirmDelete ? (
            <div className="flex items-center gap-1">
              <button type="button"
                onClick={() => deleteReminder.mutate(reminder.id, {
                  onSuccess: () => toast.success("Recordatorio eliminado"),
                  onError: () => { toast.error("Error al eliminar"); setConfirmDelete(false); },
                })}
                disabled={deleteReminder.isPending}
                className="text-xs font-semibold text-danger-600 hover:text-danger-800 disabled:opacity-50">
                {deleteReminder.isPending ? "…" : "Sí, eliminar"}
              </button>
              <button type="button" onClick={() => setConfirmDelete(false)} className="text-xs text-sand-400">No</button>
            </div>
          ) : (
            <button type="button" onClick={() => setConfirmDelete(true)}
              className="text-xs text-sand-300 hover:text-danger-400">🗑️</button>
          )}
        </div>
      </div>
      {reminder.notes && <p className="text-xs text-sand-600">{reminder.notes}</p>}
      {!reminder.isCompleted && (
        <button type="button" disabled={complete.isPending}
          onClick={() => complete.mutate(reminder.id, {
            onSuccess: () => toast.success("Recordatorio completado"),
            onError: () => toast.error("Error al completar"),
          })}
          className="rounded-lg bg-trust-600 px-3 py-1 text-xs font-semibold text-white hover:bg-trust-700 disabled:opacity-50">
          ✓ Marcar como hecho
        </button>
      )}
    </li>
  );
}
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
        <span className="shrink-0 text-xs text-sand-500">
          {reminder.dueDate}
        </span>
      </div>
      {reminder.notes && (
        <p className="text-xs text-sand-600">{reminder.notes}</p>
      )}
      {!reminder.isCompleted && (
        <button
          type="button"
          disabled={complete.isPending}
          onClick={() => {
            complete.mutate(reminder.id, {
              onSuccess: () => toast.success("Recordatorio completado"),
              onError: () => toast.error("Error al completar"),
            });
          }}
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

function AddReminderForm({ petId, onClose }: { petId: string; onClose: () => void }) {
  const create = useCreateVetReminder(petId);
  const tomorrow = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10);
  const [type, setType] = useState<MedicalRecordType>("Vaccine");
  const [dueDate, setDueDate] = useState(tomorrow);
  const [title, setTitle] = useState("");
  const [notes, setNotes] = useState("");

  const handleSubmit = () => {
    if (!title.trim()) { toast.error("El título es requerido"); return; }
    create.mutate(
      { type, dueDate, title: title.trim(), notes: notes.trim() || undefined },
      {
        onSuccess: () => { toast.success("Recordatorio creado"); onClose(); },
        onError: () => toast.error("No se pudo crear el recordatorio"),
      },
    );
  };

  return (
    <div className="rounded-2xl border border-trust-200 bg-trust-50 p-4 space-y-3">
      <h3 className="text-sm font-semibold text-trust-800">Nuevo recordatorio</h3>
      <select value={type} onChange={(e) => setType(e.target.value as MedicalRecordType)}
        className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-trust-400">
        {ALL_TYPES.map((t) => <option key={t} value={t}>{TYPE_LABEL[t]}</option>)}
      </select>
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">Título *</label>
        <Input placeholder="Ej. Refuerzo vacuna antirrábica" value={title} onChange={(e) => setTitle(e.target.value)} />
      </div>
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">Fecha *</label>
        <Input type="date" value={dueDate} min={tomorrow} onChange={(e) => setDueDate(e.target.value)} />
      </div>
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">Notas</label>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-trust-400"
          placeholder="Instrucciones, dosis, etc." />
      </div>
      <div className="flex gap-2">
        <Button onClick={handleSubmit} loading={create.isPending} disabled={!title.trim()} className="flex-1">Crear</Button>
        <Button variant="secondary" onClick={onClose} className="flex-1">Cancelar</Button>
      </div>
    </div>
  );
}

// ── Main tab ──────────────────────────────────────────────────────────────────

const ALL_FILTER_OPTIONS = ["Todos", ...Object.keys({
  Checkup: 1, Vaccine: 1, Deworming: 1, Medication: 1, Surgery: 1, Allergy: 1, Other: 1
})] as const;

export function MedicalHistoryTab({ petId }: { petId: string }) {
  const { data: records, isLoading: loadingRecords, isError: historyError } = useMedicalHistory(petId);
  const { data: count } = useMedicalCount(petId);
  const { data: reminders, isLoading: loadingReminders } = useVetReminders(petId);
  const { data: publicClinics } = usePublicClinics();
  const exportPdf = useExportMedicalPdf(petId);

  const [showAddForm, setShowAddForm] = useState(false);
  const [showReminderForm, setShowReminderForm] = useState(false);
  const [typeFilter, setTypeFilter] = useState<string>("Todos");

  const pendingReminders = reminders?.filter((r) => !r.isCompleted) ?? [];
  const completedReminders = reminders?.filter((r) => r.isCompleted) ?? [];

  const filteredRecords = typeFilter === "Todos"
    ? (records ?? [])
    : (records ?? []).filter((r) => r.type === typeFilter);

  const availableClinics = publicClinics?.map((c) => ({ id: c.id, name: c.name }));

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h2 className="font-display text-base font-semibold text-sand-800">🏥 Historial médico</h2>
        <div className="flex gap-2">
          <button type="button" disabled={exportPdf.isPending}
            onClick={() => exportPdf.mutate(undefined, { onError: () => toast.error("No se pudo exportar el PDF") })}
            className="rounded-lg border border-sand-300 px-3 py-1.5 text-xs font-semibold text-sand-700 hover:bg-sand-100 disabled:opacity-50">
            {exportPdf.isPending ? "Exportando…" : "📄 Exportar PDF"}
          </button>
          <Button size="sm" variant="secondary"
            onClick={() => { setShowReminderForm((v) => !v); setShowAddForm(false); }}>
            {showReminderForm ? "Cerrar" : "⏰ Recordatorio"}
          </Button>
          <Button size="sm" onClick={() => { setShowAddForm((v) => !v); setShowReminderForm(false); }}>
            {showAddForm ? "Cerrar" : "+ Registro"}
            {showAddForm ? "Cerrar" : "+ Agregar"}
          </Button>
        </div>
      </div>

      {/* Forms */}
      {showAddForm && <AddRecordForm petId={petId} onClose={() => setShowAddForm(false)} />}
      {showReminderForm && <AddReminderForm petId={petId} onClose={() => setShowReminderForm(false)} />}

      {/* Pending reminders */}
      {pendingReminders.length > 0 && (
        <div>
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-sand-500">Recordatorios pendientes</p>
          <ul className="space-y-2">
            {pendingReminders.map((r) => <ReminderCard key={r.id} reminder={r} petId={petId} />)}
          </ul>
        </div>
      )}

      {/* Records list */}
      <div>
        {/* Type filter */}
        <div className="mb-3 flex flex-wrap gap-1.5">
          {ALL_FILTER_OPTIONS.map((opt) => (
            <button key={opt} type="button"
              onClick={() => setTypeFilter(opt)}
              className={`rounded-full px-3 py-1 text-xs font-semibold transition-colors ${
                typeFilter === opt
                  ? "bg-brand-600 text-white"
                  : "bg-sand-100 text-sand-600 hover:bg-sand-200"
              }`}>
              {opt === "Todos" ? "Todos" : (TYPE_LABEL[opt as MedicalRecordType] ?? opt)}
            </button>
          ))}
        </div>
        {loadingRecords ? (
          <div className="animate-pulse space-y-2">
            <div className="h-16 rounded-xl bg-sand-100" />
            <div className="h-16 rounded-xl bg-sand-100" />
          </div>
        ) : historyError ? (
          // Plan Familia gate — show upgrade teaser with count if available
          <div className="rounded-2xl border border-warn-200 bg-warn-50 p-5 text-center space-y-2">
            <p className="text-2xl">🔒</p>
            <p className="text-sm font-semibold text-warn-800">
              El historial médico requiere el plan Familia
            </p>
            {count && count.totalRecords > 0 && (
              <p className="text-sm text-warn-700">
                Tu mascota tiene <strong>{count.totalRecords} registro{count.totalRecords !== 1 ? "s" : ""}</strong>
                {count.clinicRecords > 0 && ` (${count.clinicRecords} de tu veterinaria)`}.
                Actualiza para verlos.
              </p>
            )}
            <a href="/planes" className="inline-block rounded-lg bg-warn-600 px-4 py-2 text-xs font-semibold text-white hover:bg-warn-700">
              Ver planes →
            </a>
          </div>
        ) : filteredRecords.length > 0 ? (
          <ul className="space-y-2">
            {filteredRecords.map((r) => <RecordCard key={r.id} record={r} petId={petId} />)}
          </ul>
        ) : (
          <Card padding="sm">
            <p className="text-center text-sm text-sand-400">
              {typeFilter === "Todos" ? "No hay registros médicos aún. Agrega el primero." : `No hay registros de tipo "${typeFilter}".`}
            </p>
          </Card>
        )}
      </div>

      {/* Completed reminders */}
      {completedReminders.length > 0 && (
        <details className="text-sm">
          <summary className="cursor-pointer text-xs font-semibold text-sand-400 hover:text-sand-600">
            {completedReminders.length} recordatorio{completedReminders.length !== 1 ? "s" : ""} completado{completedReminders.length !== 1 ? "s" : ""}
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
    </div>
  );
}
