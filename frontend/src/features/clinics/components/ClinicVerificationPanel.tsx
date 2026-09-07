import { useRef, useState } from "react";
import {
  useCreateVeterinarian,
  useMyClinicVerification,
  useMyClinicVeterinarians,
  useRevokeVeterinarian,
  useSubmitClinicVerification,
  useUploadClinicVerificationDocument,
  useUploadVeterinarianDocument,
  useUploadVeterinarianSignature,
} from "../hooks/useCertificates";

const STATUS_LABEL: Record<string, string> = {
  Pending: "Pendiente",
  Verified: "Verificada",
  Rejected: "Rechazada",
  Expired: "Vencida",
  PendingReview: "Pendiente",
  Authorized: "Autorizado",
  Suspended: "Suspendido",
  Revoked: "Revocado",
};

function statusClass(status?: string) {
  if (status === "Verified" || status === "Authorized")
    return "bg-rescue-100 text-rescue-800";
  if (status === "Rejected" || status === "Revoked" || status === "Suspended")
    return "bg-danger-100 text-danger-700";
  if (status === "Expired") return "bg-warn-100 text-warn-700";
  return "bg-sand-100 text-sand-700";
}

export function ClinicVerificationPanel() {
  const clinicDocInput = useRef<HTMLInputElement>(null);
  const vetDocInput = useRef<HTMLInputElement>(null);
  const vetSignatureInput = useRef<HTMLInputElement>(null);
  const [selectedVetId, setSelectedVetId] = useState<string | null>(null);
  const [newVet, setNewVet] = useState({ fullName: "", licenseNumber: "" });
  const [revokeReason, setRevokeReason] = useState(
    "Ya no labora en la clínica",
  );

  const { data: verification, isLoading: loadingVerification } =
    useMyClinicVerification();
  const { data: veterinarians, isLoading: loadingVeterinarians } =
    useMyClinicVeterinarians();
  const { mutateAsync: submitVerification, isPending: submittingVerification } =
    useSubmitClinicVerification();
  const {
    mutateAsync: uploadVerificationDocument,
    isPending: uploadingVerification,
  } = useUploadClinicVerificationDocument();
  const { mutateAsync: createVeterinarian, isPending: creatingVeterinarian } =
    useCreateVeterinarian();
  const {
    mutateAsync: uploadVeterinarianDocument,
    isPending: uploadingVetDoc,
  } = useUploadVeterinarianDocument();
  const {
    mutateAsync: uploadVeterinarianSignature,
    isPending: uploadingSignature,
  } = useUploadVeterinarianSignature();
  const { mutateAsync: revokeVeterinarian, isPending: revokingVeterinarian } =
    useRevokeVeterinarian();

  const handleClinicDoc = async (
    event: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const file = event.target.files?.[0];
    if (!file) return;
    await uploadVerificationDocument(file);
    event.target.value = "";
  };

  const handleVetDoc = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file || !selectedVetId) return;
    await uploadVeterinarianDocument({ veterinarianId: selectedVetId, file });
    event.target.value = "";
  };

  const handleVetSignature = async (
    event: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const file = event.target.files?.[0];
    if (!file || !selectedVetId) return;
    await uploadVeterinarianSignature({ veterinarianId: selectedVetId, file });
    event.target.value = "";
  };

  const handleCreateVet = async () => {
    if (!newVet.fullName.trim() || !newVet.licenseNumber.trim()) return;
    await createVeterinarian(newVet);
    setNewVet({ fullName: "", licenseNumber: "" });
  };

  return (
    <section className="space-y-4 rounded-2xl border border-sand-100 bg-surface px-4 py-4 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-bold text-sand-900">
            Verificación SENASA-ready
          </h2>
          <p className="mt-0.5 text-xs text-sand-500">
            Documentos privados, revisión admin y veterinarios autorizados.
          </p>
        </div>
        <span
          className={`rounded-full px-2 py-0.5 text-[10px] font-bold ${statusClass(verification?.status)}`}
        >
          {verification ? STATUS_LABEL[verification.status] : "Sin solicitud"}
        </span>
      </div>

      {loadingVerification ? (
        <p className="text-xs text-sand-500">Cargando verificación…</p>
      ) : (
        <div className="rounded-xl border border-sand-100 bg-surface-warm px-3 py-3 text-xs text-sand-600">
          <p>
            Documento:{" "}
            <strong>
              {verification?.hasDocument ? "Cargado" : "Pendiente"}
            </strong>
          </p>
          {verification?.expiresAt && (
            <p>
              Vence:{" "}
              {new Date(verification.expiresAt).toLocaleDateString("es-CR")}
            </p>
          )}
          {verification?.rejectionReason && (
            <p className="text-danger-700">
              Motivo: {verification.rejectionReason}
            </p>
          )}
        </div>
      )}

      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={() => void submitVerification()}
          disabled={submittingVerification}
          className="rounded-xl border border-trust-200 px-3 py-2 text-xs font-semibold text-trust-700 disabled:opacity-50"
        >
          {submittingVerification ? "Enviando…" : "Solicitar verificación"}
        </button>
        <button
          type="button"
          onClick={() => clinicDocInput.current?.click()}
          disabled={uploadingVerification}
          className="rounded-xl border border-brand-200 px-3 py-2 text-xs font-semibold text-brand-700 disabled:opacity-50"
        >
          {uploadingVerification ? "Subiendo…" : "Subir documento"}
        </button>
        <input
          ref={clinicDocInput}
          type="file"
          accept="application/pdf,image/jpeg,image/png,image/webp"
          hidden
          onChange={(e) => void handleClinicDoc(e)}
        />
      </div>

      <div className="border-t border-sand-100 pt-4">
        <h3 className="text-xs font-bold uppercase tracking-[0.18em] text-sand-400">
          Veterinarios
        </h3>
        <div className="mt-2 grid gap-2 sm:grid-cols-[1fr_0.8fr_auto]">
          <input
            value={newVet.fullName}
            onChange={(e) =>
              setNewVet((v) => ({ ...v, fullName: e.target.value }))
            }
            placeholder="Nombre completo"
            className="rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
          <input
            value={newVet.licenseNumber}
            onChange={(e) =>
              setNewVet((v) => ({ ...v, licenseNumber: e.target.value }))
            }
            placeholder="Licencia"
            className="rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
          <button
            type="button"
            onClick={() => void handleCreateVet()}
            disabled={
              creatingVeterinarian ||
              !newVet.fullName.trim() ||
              !newVet.licenseNumber.trim()
            }
            className="rounded-xl bg-trust-600 px-3 py-2 text-xs font-bold text-white disabled:opacity-50"
          >
            {creatingVeterinarian ? "Guardando…" : "Agregar"}
          </button>
        </div>

        {loadingVeterinarians && (
          <p className="mt-3 text-xs text-sand-500">Cargando veterinarios…</p>
        )}
        {veterinarians && veterinarians.length === 0 && (
          <p className="mt-3 text-xs text-sand-500">
            Aún no hay veterinarios registrados.
          </p>
        )}
        {veterinarians && veterinarians.length > 0 && (
          <ul className="mt-3 space-y-2">
            {veterinarians.map((vet) => (
              <li
                key={vet.id}
                className="rounded-xl border border-sand-100 bg-surface-warm px-3 py-3"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <p className="text-sm font-semibold text-sand-900">
                      {vet.fullName}
                    </p>
                    <p className="text-[11px] text-sand-500">
                      {vet.licenseNumber} · Documento{" "}
                      {vet.hasDocument ? "cargado" : "pendiente"} · Firma{" "}
                      {vet.hasSignature ? "cargada" : "opcional"}
                    </p>
                  </div>
                  <span
                    className={`rounded-full px-2 py-0.5 text-[10px] font-bold ${statusClass(vet.status)}`}
                  >
                    {STATUS_LABEL[vet.status] ?? vet.status}
                  </span>
                </div>
                <div className="mt-2 flex flex-wrap gap-2">
                  <button
                    type="button"
                    onClick={() => {
                      setSelectedVetId(vet.id);
                      vetDocInput.current?.click();
                    }}
                    disabled={uploadingVetDoc}
                    className="text-xs font-semibold text-brand-700 hover:underline"
                  >
                    Documento
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setSelectedVetId(vet.id);
                      vetSignatureInput.current?.click();
                    }}
                    disabled={uploadingSignature}
                    className="text-xs font-semibold text-brand-700 hover:underline"
                  >
                    Firma
                  </button>
                  {vet.status !== "Revoked" && (
                    <button
                      type="button"
                      onClick={() =>
                        void revokeVeterinarian({
                          veterinarianId: vet.id,
                          reason: revokeReason,
                        })
                      }
                      disabled={revokingVeterinarian || !revokeReason.trim()}
                      className="text-xs font-semibold text-danger-700 hover:underline"
                    >
                      Revocar
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
        <input
          ref={vetDocInput}
          type="file"
          accept="application/pdf,image/jpeg,image/png,image/webp"
          hidden
          onChange={(e) => void handleVetDoc(e)}
        />
        <input
          ref={vetSignatureInput}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          hidden
          onChange={(e) => void handleVetSignature(e)}
        />
        <input
          value={revokeReason}
          onChange={(e) => setRevokeReason(e.target.value)}
          className="mt-3 w-full rounded-xl border border-sand-200 px-3 py-2 text-xs"
          aria-label="Motivo de revocación"
        />
      </div>
    </section>
  );
}
