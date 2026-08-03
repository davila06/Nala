import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import { Button, Input, Card } from "@/shared/ui";
import {
  useMedicalHistory,
  useAddMedicalRecord,
  useVetReminders,
  useCompleteReminder,
  useExportMedicalPdf,
} from "@/features/medical/hooks/useMedical";
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

function RecordCard({ record }: { record: MedicalRecordDto }) {
  return (
    <li className="rounded-xl border border-sand-100 bg-surface-warm p-4 space-y-1">
      <div className="flex items-start justify-between gap-2">
        <span className="text-sm font-semibold text-sand-800">
          {TYPE_LABEL[record.type as MedicalRecordType] ?? record.type}
        </span>
        <span className="shrink-0 text-xs text-sand-500">{record.date}</span>
      </div>
      <p className="text-sm text-sand-700">{record.description}</p>
      {(record.vetName || record.clinicName) && (
        <p className="text-xs text-sand-500">
          {[record.vetName, record.clinicName].filter(Boolean).join(" · ")}
        </p>
      )}
      {record.nextDueDate && (
        <p className="text-xs font-medium text-warn-700">
          ⏰ Próxima cita: {record.nextDueDate}
        </p>
      )}
      {record.documentUrl && (
        <a
          href={record.documentUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="text-xs font-medium text-brand-600 hover:underline"
        >
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

// ── Main tab ──────────────────────────────────────────────────────────────────

export function MedicalHistoryTab({ petId }: { petId: string }) {
  const { data: records, isLoading: loadingRecords } = useMedicalHistory(petId);
  const { data: reminders, isLoading: loadingReminders } =
    useVetReminders(petId);
  const exportPdf = useExportMedicalPdf(petId);
  const [showAddForm, setShowAddForm] = useState(false);

  const pendingReminders = reminders?.filter((r) => !r.isCompleted) ?? [];
  const completedReminders = reminders?.filter((r) => r.isCompleted) ?? [];

  return (
    <div className="space-y-5">
      {/* Header actions */}
      <div className="flex items-center justify-between">
        <h2 className="font-display text-base font-semibold text-sand-800">
          🏥 Historial médico
        </h2>
        <div className="flex gap-2">
          <button
            type="button"
            disabled={exportPdf.isPending}
            onClick={() => {
              exportPdf.mutate(undefined, {
                onError: () => toast.error("No se pudo exportar el PDF"),
              });
            }}
            className="rounded-lg border border-sand-300 px-3 py-1.5 text-xs font-semibold text-sand-700 hover:bg-sand-100 disabled:opacity-50"
          >
            {exportPdf.isPending ? "Exportando…" : "📄 Exportar PDF"}
          </button>
          <Button size="sm" onClick={() => setShowAddForm((v) => !v)}>
            {showAddForm ? "Cerrar" : "+ Agregar"}
          </Button>
        </div>
      </div>

      {/* Add form */}
      {showAddForm && (
        <AddRecordForm petId={petId} onClose={() => setShowAddForm(false)} />
      )}

      {/* Upcoming reminders */}
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
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-sand-500">
          Registros
        </p>
        {loadingRecords ? (
          <div className="animate-pulse space-y-2">
            <div className="h-16 rounded-xl bg-sand-100" />
            <div className="h-16 rounded-xl bg-sand-100" />
          </div>
        ) : records && records.length > 0 ? (
          <ul className="space-y-2">
            {records.map((r) => (
              <RecordCard key={r.id} record={r} />
            ))}
          </ul>
        ) : (
          <Card padding="sm">
            <p className="text-center text-sm text-sand-400">
              No hay registros médicos aún. Agrega el primero.
            </p>
          </Card>
        )}
      </div>

      {/* Completed reminders (collapsed) */}
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
    </div>
  );
}
