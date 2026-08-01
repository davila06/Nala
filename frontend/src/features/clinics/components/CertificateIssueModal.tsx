import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useIssueCertificate } from "../hooks/useCertificates";
import {
  CERTIFICATE_TYPE_LABELS,
  type CertificateType,
} from "../api/certificateApi";

const TYPES = Object.entries(CERTIFICATE_TYPE_LABELS) as [
  CertificateType,
  string,
][];

interface CertificateIssueModalProps {
  clinicId: string;
  clinicName: string;
  clinicLicense: string;
  onClose: () => void;
}

export function CertificateIssueModal({
  clinicId,
  clinicName,
  clinicLicense,
  onClose,
}: CertificateIssueModalProps) {
  const [step, setStep] = useState<"form" | "done">("form");
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);
  const [verificationCode, setVerificationCode] = useState("");
  const [form, setForm] = useState({
    petId: "",
    petName: "",
    petSpecies: "",
    petBreed: "",
    vetName: "",
    type: "Vaccination" as CertificateType,
    notes: "",
    validUntil: "",
  });

  const { mutateAsync: issue, isPending, error } = useIssueCertificate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const cert = await issue({
        petId: form.petId,
        clinicId,
        type: form.type,
        notes: form.notes || undefined,
        validUntil: form.validUntil || undefined,
        petName: form.petName,
        petSpecies: form.petSpecies,
        petBreed: form.petBreed || undefined,
        clinicName,
        clinicLicense,
        vetName: form.vetName,
      });
      setPdfUrl(cert.pdfUrl);
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
                  Nombre de la mascota
                  <input
                    required
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    placeholder="Firulais"
                    {...field("petName")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700">
                  Especie
                  <input
                    required
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    placeholder="Perro / Gato"
                    {...field("petSpecies")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700">
                  Raza (opcional)
                  <input
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    placeholder="Labrador"
                    {...field("petBreed")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700">
                  Nombre del veterinario
                  <input
                    required
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    placeholder="Dr. Pérez"
                    {...field("vetName")}
                  />
                </label>
                <label className="block text-xs font-semibold text-sand-700">
                  Tipo de certificado
                  <select
                    required
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    value={form.type}
                    onChange={(e) =>
                      setForm((f) => ({
                        ...f,
                        type: e.target.value as CertificateType,
                      }))
                    }
                  >
                    {TYPES.map(([value, label]) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="block text-xs font-semibold text-sand-700 sm:col-span-2">
                  Válido hasta (opcional)
                  <input
                    type="date"
                    className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1"
                    {...field("validUntil")}
                  />
                </label>
              </div>
              <label className="block text-xs font-semibold text-sand-700">
                Observaciones (max. 500 chars)
                <textarea
                  className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400 field-input mt-1 h-20 resize-none"
                  maxLength={500}
                  {...field("notes")}
                />
              </label>
              {error && (
                <p className="text-xs text-danger-600">
                  Error al emitir el certificado. Intenta de nuevo.
                </p>
              )}
              <button
                type="submit"
                disabled={isPending}
                className="w-full rounded-2xl bg-trust-600 py-3 text-sm font-bold text-white hover:bg-trust-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400"
              >
                {isPending ? "Generando PDF…" : "Emitir certificado →"}
              </button>
            </form>
          )}

          {step === "done" && (
            <div className="flex flex-col items-center gap-4 py-4 text-center">
              <div className="flex h-14 w-14 items-center justify-center rounded-full bg-rescue-100 text-3xl">
                ✅
              </div>
              <h3 className="text-lg font-black text-sand-900">
                Certificado emitido
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
              {pdfUrl && (
                <a
                  href={pdfUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="w-full rounded-2xl border border-trust-200 py-2.5 text-sm font-semibold text-trust-700 hover:bg-trust-50 transition-colors"
                >
                  Descargar PDF →
                </a>
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
