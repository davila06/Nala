import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "@/shared/lib/toast";
import { Button, Input, Card } from "@/shared/ui";
import {
  clinicMedicalApi,
  type MedicalRecordDto,
  type MedicalRecordType,
} from "@/features/medical/api/medicalApi";
import { ClinicAccessPanel } from "./ClinicAccessPanel";

// ── Locale helpers ────────────────────────────────────────────────────────────

const TYPE_LABEL: Record<MedicalRecordType, string> = {
  Vaccine: "💉 Vacuna",
  Deworming: "🪱 Desparasitación",
  Checkup: "🩺 Consulta",
  Surgery: "🔪 Cirugía",
  Medication: "💊 Medicamento",
  Allergy: "🌿 Alergia",
  Other: "📋 Otro",
};

const RECORD_TYPES: MedicalRecordType[] = [
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
  const isClinicRecord = record.source === "Clinic";
  return (
    <li className="rounded-xl border border-sand-100 bg-surface-warm p-3 space-y-1">
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2">
          <span className="text-sm font-semibold text-sand-800">
            {TYPE_LABEL[record.type as MedicalRecordType] ?? record.type}
          </span>
          {isClinicRecord ? (
            <span className="rounded-full bg-trust-100 px-2 py-0.5 text-xs font-medium text-trust-700">
              🏥 {record.clinicName ?? "Clínica"}
            </span>
          ) : (
            <span className="rounded-full bg-sand-100 px-2 py-0.5 text-xs font-medium text-sand-500">
              👤 Dueño
            </span>
          )}
        </div>
        <span className="shrink-0 text-xs text-sand-500">{record.date}</span>
      </div>
      <p className="text-sm text-sand-700">{record.description}</p>
      {record.vetName && (
        <p className="text-xs text-sand-500">Dr/a. {record.vetName}</p>
      )}
      {record.nextDueDate && (
        <p className="text-xs font-medium text-warn-700">
          ⏰ Próxima: {record.nextDueDate}
        </p>
      )}
      {record.documentUrl && (
        <a
          href={record.documentUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="text-xs font-medium text-brand-600 hover:underline"
        >
          📄 Ver documento
        </a>
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
  const qc = useQueryClient();
  const add = useMutation({
    mutationFn: clinicMedicalApi.addRecord,
    onSuccess: () => {
      void qc.invalidateQueries({
        queryKey: ["clinic-patient-history", petId],
      });
      toast.success("Registro guardado en el expediente");
      onClose();
    },
    onError: (err: unknown) => {
      const apiErr = err as {
        response?: { data?: { detail?: string }; status?: number };
      };
      if (apiErr?.response?.status === 403)
        toast.error("Sin acceso: escanee el QR o chip de la mascota primero.");
      else toast.error(apiErr?.response?.data?.detail ?? "No se pudo guardar");
    },
  });

  const today = new Date().toISOString().slice(0, 10);
  const [type, setType] = useState<MedicalRecordType>("Checkup");
  const [date, setDate] = useState(today);
  const [description, setDescription] = useState("");
  const [vetName, setVetName] = useState("");
  const [nextDueDate, setNextDueDate] = useState("");
  const [document, setDocument] = useState<File | null>(null);

  return (
    <div className="rounded-2xl border border-trust-200 bg-trust-50 p-4 space-y-3">
      <h3 className="text-sm font-semibold text-trust-800">
        Agregar registro al expediente
      </h3>

      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Tipo
        </label>
        <select
          value={type}
          onChange={(e) => setType(e.target.value as MedicalRecordType)}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-trust-400"
        >
          {RECORD_TYPES.map((t) => (
            <option key={t} value={t}>
              {TYPE_LABEL[t]}
            </option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-2 gap-2">
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
        <div>
          <label className="mb-1 block text-xs font-medium text-sand-600">
            Próxima cita
          </label>
          <Input
            type="date"
            value={nextDueDate}
            min={today}
            onChange={(e) => setNextDueDate(e.target.value)}
          />
        </div>
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Descripción *
        </label>
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={2}
          className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-trust-400"
          placeholder="Diagnóstico, tratamiento aplicado, observaciones…"
        />
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Veterinario
        </label>
        <Input
          placeholder="Dr/a. Nombre"
          value={vetName}
          onChange={(e) => setVetName(e.target.value)}
        />
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Documento (PDF/foto, máx. 5 MB)
        </label>
        <input
          type="file"
          accept=".pdf,image/jpeg,image/png"
          onChange={(e) => setDocument(e.target.files?.[0] ?? null)}
          className="block w-full text-xs text-sand-600 file:mr-3 file:rounded-lg file:border-0 file:bg-trust-100 file:px-3 file:py-1.5 file:text-xs file:font-semibold file:text-trust-700 hover:file:bg-trust-200"
        />
      </div>

      <div className="flex gap-2 pt-1">
        <Button
          onClick={() =>
            add.mutate({
              petId,
              recordType: type,
              date,
              description: description.trim(),
              vetName: vetName.trim() || undefined,
              nextDueDate: nextDueDate || undefined,
              document: document ?? undefined,
            })
          }
          loading={add.isPending}
          disabled={!description.trim()}
          className="flex-1"
        >
          Guardar en expediente
        </Button>
        <Button variant="secondary" onClick={onClose} className="flex-1">
          Cancelar
        </Button>
      </div>
    </div>
  );
}

// ── Main component ────────────────────────────────────────────────────────────

export function ClinicExpedienteTab({
  petId,
  onSwitchToPet,
}: {
  petId: string;
  onSwitchToPet?: (petId: string, petName: string) => void;
}) {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["clinic-patient-history", petId],
    queryFn: () => clinicMedicalApi.getPatientHistory(petId),
    staleTime: 30_000,
    retry: (count, err: { response?: { status?: number } }) =>
      err?.response?.status !== 403 && count < 2,
  });

  const [showAddForm, setShowAddForm] = useState(false);
  const forbidden =
    (error as { response?: { status?: number } } | null)?.response?.status ===
    403;

  if (isLoading) {
    return (
      <div className="animate-pulse space-y-2">
        <div className="h-16 rounded-xl bg-sand-100" />
        <div className="h-16 rounded-xl bg-sand-100" />
      </div>
    );
  }

  if (forbidden || isError) {
    return (
      <div className="space-y-4">
        <div className="rounded-2xl border border-warn-200 bg-warn-50 p-4 text-center space-y-2">
          <p className="text-sm font-semibold text-warn-800">
            📋 Sin acceso al expediente
          </p>
          <p className="text-xs text-warn-700">
            Escanea el QR o chip de la mascota para acceso temporal (90 días), o
            pide al dueño un código de acceso permanente.
          </p>
        </div>
        {onSwitchToPet && (
          <ClinicAccessPanel currentPetId={petId} onSelectPet={onSwitchToPet} />
        )}
      </div>
    );
  }

  if (!data) return null;

  const clinicRecords = data.records.filter((r) => r.source === "Clinic");
  const ownerRecords = data.records.filter((r) => r.source === "Owner");

  return (
    <div className="space-y-4">
      {/* Patient header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          {data.photoUrl && (
            <img
              src={data.photoUrl}
              alt={data.petName}
              className="h-10 w-10 rounded-full object-cover border border-sand-200"
            />
          )}
          <div>
            <p className="font-semibold text-sand-900">{data.petName}</p>
            <p className="text-xs text-sand-500">
              {data.species}
              {data.breed ? ` · ${data.breed}` : ""}
              {data.lastSeenAt && (
                <>
                  {" "}
                  · última visita{" "}
                  {new Date(data.lastSeenAt).toLocaleDateString("es-CR")}
                </>
              )}
            </p>
          </div>
        </div>
        <Button size="sm" onClick={() => setShowAddForm((v) => !v)}>
          {showAddForm ? "Cerrar" : "+ Registrar"}
        </Button>
      </div>

      {showAddForm && (
        <AddRecordForm petId={petId} onClose={() => setShowAddForm(false)} />
      )}

      {/* Clinic records */}
      <div>
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-sand-500">
          Registros de esta clínica ({clinicRecords.length})
        </p>
        {clinicRecords.length > 0 ? (
          <ul className="space-y-2">
            {clinicRecords.map((r) => (
              <RecordCard key={r.id} record={r} />
            ))}
          </ul>
        ) : (
          <Card padding="sm">
            <p className="text-center text-sm text-sand-400">
              Esta clínica aún no tiene registros para {data.petName}.
            </p>
          </Card>
        )}
      </div>

      {/* Owner records (read-only) */}
      {ownerRecords.length > 0 && (
        <details>
          <summary className="cursor-pointer text-xs font-semibold text-sand-400 hover:text-sand-600">
            {ownerRecords.length} registro{ownerRecords.length !== 1 ? "s" : ""}{" "}
            del dueño (solo lectura)
          </summary>
          <ul className="mt-2 space-y-2">
            {ownerRecords.map((r) => (
              <RecordCard key={r.id} record={r} />
            ))}
          </ul>
        </details>
      )}

      {/* ── Acceso permanente (Option C) ────────────────────────────── */}
      {onSwitchToPet && (
        <>
          <hr className="border-sand-100" />
          <ClinicAccessPanel currentPetId={petId} onSelectPet={onSwitchToPet} />
        </>
      )}
    </div>
  );
}
