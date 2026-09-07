import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  useCertificateIssuers,
  useCreateVeterinarian,
  useDownloadCertificatePdf,
  useIssueVaccinePassport,
} from "../hooks/useCertificates";
import { type ClinicVeterinarianDto } from "../api/certificateApi";

interface CertificateIssueModalProps {
  clinicId: string;
  onClose: () => void;
}

export function CertificateIssueModal({
  clinicId,
  onClose,
}: CertificateIssueModalProps) {
  const [step, setStep] = useState<"form" | "done">("form");
  const [certificateId, setCertificateId] = useState<string | null>(null);
  const [verificationCode, setVerificationCode] = useState("");
  const [newVet, setNewVet] = useState({ fullName: "", licenseNumber: "" });
  const [form, setForm] = useState({
    petId: "",
    veterinarianId: "",
    petColor: "",
    vaccineName: "Rabia",
    vaccineBrand: "",
    vaccineLot: "",
    vaccineDate: "",
    vaccineValidUntil: "",
    parasiteProduct: "",
    parasiteDate: "",
    parasiteNextDue: "",
  });

  const { data: issuers, isLoading: issuersLoading } = useCertificateIssuers();
  const { mutateAsync: issue, isPending, error } = useIssueVaccinePassport();
  const { mutateAsync: createVeterinarian, isPending: creatingVet } =
    useCreateVeterinarian();
  const { mutateAsync: downloadPdf, isPending: downloadingPdf } =
    useDownloadCertificatePdf();

  const activeVeterinarians = issuers?.veterinarians ?? [];
  const selectedVeterinarian = activeVeterinarians.find(
    (vet: ClinicVeterinarianDto) => vet.id === form.veterinarianId,
  );
  const isVerified = issuers?.verification?.status === "Verified";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const cert = await issue({
        petId: form.petId,
        clinicId,
        veterinarianId: form.veterinarianId,
        vetName: selectedVeterinarian?.fullName ?? "",
        vetLicense: selectedVeterinarian?.licenseNumber,
        petColor: form.petColor || undefined,
        vaccines: [
          {
            vaccineName: form.vaccineName,
            brand: form.vaccineBrand || undefined,
            lotNumber: form.vaccineLot || undefined,
            applicationDate: form.vaccineDate,
            validUntil: form.vaccineValidUntil || undefined,
          },
        ],
        parasiteControl: form.parasiteProduct
          ? {
              productName: form.parasiteProduct,
              applicationDate: form.parasiteDate,
              nextDueDate: form.parasiteNextDue || undefined,
            }
          : undefined,
      });
      setCertificateId(cert.id);
      setVerificationCode(cert.verificationCode);
      setStep("done");
    } catch {
      // error shown via mutation state
    }
  };

  const field = (key: keyof typeof form) => ({
    value: form[key],
    onChange: (
      e: React.ChangeEvent<
        HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement
      >,
    ) => setForm((f) => ({ ...f, [key]: e.target.value })),
  });

  const handleCreateVeterinarian = async () => {
    if (!newVet.fullName.trim() || !newVet.licenseNumber.trim()) return;
    await createVeterinarian(newVet);
    setNewVet({ fullName: "", licenseNumber: "" });
  };

  const handleDownload = async () => {
    if (!certificateId) return;
    const blob = await downloadPdf(certificateId);
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `pawtrack-certificate-${verificationCode}.pdf`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <AnimatePresence>
      <motion.div
        className="fixed inset-0 z-50 flex items-end bg-black/50 sm:items-center sm:justify-center p-4"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
      >
        <motion.div
          className="w-full max-w-lg rounded-3xl bg-surface p-6 shadow-2xl max-h-[90vh] overflow-y-auto"
          initial={{ y: 40, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: 40, opacity: 0 }}
          transition={{ type: "spring", stiffness: 400, damping: 35 }}
          onClick={(e) => e.stopPropagation()}
        >
          <div className="mb-5 flex items-start justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-trust-600">
                Tier Partner
              </p>
              <h2 className="mt-1 text-xl font-black text-sand-900">
                Emitir certificado veterinario
              </h2>
              <p className="mt-1 text-xs text-sand-500">
                SENASA-ready, verificable y sin integración oficial directa.
              </p>
            </div>
            <button
              type="button"
              onClick={onClose}
              aria-label="Cerrar"
              className="rounded-xl p-2 text-sand-400 hover:bg-sand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              <svg
                viewBox="0 0 16 16"
                fill="currentColor"
                className="h-4 w-4"
                aria-hidden="true"
              >
                <path d="M3.72 3.72a.75.75 0 0 1 1.06 0L8 6.94l3.22-3.22a.75.75 0 1 1 1.06 1.06L9.06 8l3.22 3.22a.75.75 0 1 1-1.06 1.06L8 9.06l-3.22 3.22a.75.75 0 0 1-1.06-1.06L6.94 8 3.72 4.78a.75.75 0 0 1 0-1.06Z" />
              </svg>
            </button>
          </div>

          {step === "form" && (
            <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
              {!isVerified && (
                <div className="rounded-2xl border border-warn-200 bg-warn-50 px-4 py-3 text-xs text-warn-800">
                  La clínica debe estar verificada por administración para
                  emitir pasaportes.
                </div>
              )}
              {issuersLoading && (
                <p className="text-xs text-sand-500">
                  Cargando autorización de emisión…
                </p>
              )}
              <div className="grid gap-3 sm:grid-cols-2">
                <label className="block text-xs font-semibold text-sand-700">
                  ID de mascota (PawTrack)
                  <input
                    required
                    pattern="[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"
                    title="Debe ser un UUID válido (ej. 550e8400-e29b-41d4-a716-446655440000)"
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    placeholder="550e8400-e29b-41d4-a716-446655440000"
                    {...field("petId")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700">
                  Color / señas visibles
                  <input
                    required
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    placeholder="Dorado con pecho blanco"
                    {...field("petColor")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700 sm:col-span-2">
                  Veterinario autorizado
                  <select
                    required
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    {...field("veterinarianId")}
                  >
                    <option value="">Seleccionar veterinario</option>
                    {activeVeterinarians.map((vet) => (
                      <option key={vet.id} value={vet.id}>
                        {vet.fullName} · {vet.licenseNumber}
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              <div className="rounded-2xl border border-sand-100 bg-surface-warm p-3">
                <p className="mb-2 text-xs font-bold text-sand-700">
                  Solicitar revisión de veterinario
                </p>
                <div className="grid gap-2 sm:grid-cols-2">
                  <input
                    value={newVet.fullName}
                    onChange={(e) =>
                      setNewVet((v) => ({ ...v, fullName: e.target.value }))
                    }
                    placeholder="Nombre completo"
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                  />
                  <input
                    value={newVet.licenseNumber}
                    onChange={(e) =>
                      setNewVet((v) => ({
                        ...v,
                        licenseNumber: e.target.value,
                      }))
                    }
                    placeholder="Licencia veterinaria"
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                  />
                </div>
                <button
                  type="button"
                  onClick={() => void handleCreateVeterinarian()}
                  disabled={
                    creatingVet ||
                    !newVet.fullName.trim() ||
                    !newVet.licenseNumber.trim()
                  }
                  className="mt-2 rounded-xl border border-trust-200 px-3 py-2 text-xs font-semibold text-trust-700 disabled:opacity-50"
                >
                  {creatingVet ? "Guardando…" : "Enviar a revisión"}
                </button>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <label className="block text-xs font-semibold text-sand-700">
                  Vacuna
                  <input
                    required
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    {...field("vaccineName")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700">
                  Marca
                  <input
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    {...field("vaccineBrand")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700">
                  Lote
                  <input
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    {...field("vaccineLot")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700">
                  Fecha de aplicación
                  <input
                    required
                    type="date"
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    {...field("vaccineDate")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700 sm:col-span-2">
                  Vigencia de la vacuna
                  <input
                    type="date"
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    {...field("vaccineValidUntil")}
                  />
                </label>
              </div>
              <div className="grid gap-3 sm:grid-cols-3">
                <label className="block text-xs font-semibold text-sand-700 sm:col-span-3">
                  Control antiparasitario opcional
                </label>
                <input
                  placeholder="Producto"
                  className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input"
                  {...field("parasiteProduct")}
                />
                <input
                  type="date"
                  className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input"
                  {...field("parasiteDate")}
                />
                <input
                  type="date"
                  className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input"
                  {...field("parasiteNextDue")}
                />
              </div>
              <p className="rounded-2xl border border-sand-100 bg-sand-50 px-4 py-3 text-xs text-sand-600">
                Documento preparado para trazabilidad sanitaria. No sustituye
                trámites o certificaciones oficiales de la autoridad competente.
              </p>
              {error && (
                <p className="text-xs text-danger-600">
                  Error al emitir el pasaporte. Verifica plan Partner, acceso al
                  expediente, clínica verificada, veterinario autorizado y
                  vacuna requerida.
                </p>
              )}
              <button
                type="submit"
                disabled={isPending || !isVerified || !form.veterinarianId}
                className="w-full rounded-2xl bg-trust-600 py-3 text-sm font-bold text-white hover:bg-trust-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400"
              >
                {isPending
                  ? "Generando PDF…"
                  : "Emitir pasaporte SENASA-ready →"}
              </button>
            </form>
          )}

          {step === "done" && (
            <div className="flex flex-col items-center gap-4 py-4 text-center">
              <div className="flex h-14 w-14 items-center justify-center rounded-full bg-rescue-100 text-3xl">
                ✅
              </div>
              <h3 className="text-lg font-black text-sand-900">
                Pasaporte emitido
              </h3>
              <div className="w-full rounded-2xl bg-surface-warm p-4">
                <p className="text-xs text-sand-500 mb-1">
                  Código de verificación
                </p>
                <p className="font-mono text-2xl font-black tracking-[0.2em] text-sand-900">
                  {verificationCode}
                </p>
                <p className="mt-1 text-[10px] text-sand-400">
                  pawtrack.cr/verificar/{verificationCode}
                </p>
              </div>
              {certificateId && (
                <button
                  type="button"
                  onClick={() => void handleDownload()}
                  disabled={downloadingPdf}
                  className="w-full rounded-2xl border border-trust-200 py-2.5 text-sm font-semibold text-trust-700 hover:bg-trust-50 transition-colors"
                >
                  {downloadingPdf ? "Descargando…" : "Descargar PDF →"}
                </button>
              )}
              <button
                type="button"
                onClick={onClose}
                className="w-full rounded-2xl bg-brand-600 py-3 text-sm font-bold text-white hover:bg-brand-700"
              >
                Cerrar
              </button>
            </div>
          )}
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}
