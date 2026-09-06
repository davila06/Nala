import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  useCreateSubscription,
  useReportPayment,
  useSubscriptionCatalog,
} from "../hooks/useSubscription";
import type { SubscriptionTier } from "../api/subscriptionApi";
import { TIER_PRICE_CRC } from "../api/subscriptionApi";

interface SinpePaymentModalProps {
  tier: SubscriptionTier;
  clinicId?: string;
  onClose: () => void;
  onSuccess?: () => void;
}

const SINPE_NUMBER = import.meta.env.VITE_SINPE_PHONE ?? "7000-0000";
const TIER_LABELS: Record<SubscriptionTier, string> = {
  Free: "Explorador",
  UserPlus: "Plus",
  UserFamilia: "Familia",
  ClinicBasic: "Básica",
  ClinicPlus: "Clínica Plus",
  ClinicPartner: "Clínica Partner",
  StoreBasic: "Tienda Básica",
  StorePlus: "Tienda Plus",
  StorePartner: "Tienda Partner",
  ShelterBasic: "Refugio Básico",
  ShelterPlus: "Refugio Plus",
  MuniBasica: "Municipalidad Básica",
  MuniFull: "Municipalidad Full",
  MuniRedRegional: "Red Regional",
};

export function SinpePaymentModal({
  tier,
  clinicId,
  onClose,
  onSuccess,
}: SinpePaymentModalProps) {
  const [step, setStep] = useState<
    "confirm" | "payment" | "reporting" | "reported"
  >("confirm");
  const [reference, setReference] = useState<string | null>(null);
  const [subscriptionId, setSubscriptionId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { mutateAsync: createSub, isPending: isCreating } =
    useCreateSubscription();
  const { mutateAsync: reportPayment, isPending: isReporting } =
    useReportPayment();
  const { data: catalog } = useSubscriptionCatalog();

  const catalogPlan = catalog?.find((plan) => plan.tier === tier);
  const price =
    catalogPlan?.annualPriceCrc ??
    catalogPlan?.monthlyPriceCrc ??
    TIER_PRICE_CRC[tier];
  const label = catalogPlan?.displayName ?? TIER_LABELS[tier];
  const pricePeriod = catalogPlan?.annualPriceCrc ? "año" : "mes";

  async function handleStartPayment() {
    setError(null);
    try {
      const sub = await createSub({ tier, clinicId });
      setReference(sub.paymentReference);
      setSubscriptionId(sub.id);
      setStep("payment");
    } catch {
      setError("No se pudo generar el código de pago. Intenta de nuevo.");
    }
  }

  async function handleReportPayment() {
    if (!subscriptionId) return;
    setStep("reporting");
    setError(null);
    try {
      await reportPayment(subscriptionId);
      setStep("reported");
      onSuccess?.();
    } catch {
      setError("No se pudo registrar tu aviso. Intenta de nuevo.");
      setStep("payment");
    }
  }

  return (
    <AnimatePresence>
      <motion.div
        className="fixed inset-0 z-50 flex items-end bg-black/60 sm:items-center sm:justify-center p-4"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={step !== "reporting" ? onClose : undefined}
      >
        <motion.div
          className="w-full max-w-md rounded-3xl bg-surface p-6 shadow-2xl"
          initial={{ y: 40, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: 40, opacity: 0 }}
          transition={{ type: "spring", stiffness: 400, damping: 35 }}
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="mb-5 flex items-start justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-brand-500">
                SINPE Móvil
              </p>
              <h2 className="mt-1 text-xl font-black text-sand-900">
                Activar {label}
              </h2>
            </div>
            {step !== "reporting" && (
              <button
                type="button"
                onClick={onClose}
                aria-label="Cerrar"
                className="rounded-xl p-2 text-sand-400 hover:bg-sand-100 hover:text-sand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
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
            )}
          </div>

          {/* Step: confirm */}
          {step === "confirm" && (
            <div className="space-y-4">
              <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4">
                <p className="text-sm text-brand-800">
                  Activarás el plan <strong>{label}</strong> por{" "}
                  <strong>
                    ₡{price.toLocaleString("es-CR")}/{pricePeriod}
                  </strong>
                  .
                </p>
                <p className="mt-2 text-xs text-brand-600">
                  El pago se realiza vía SINPE Móvil. Se generará un código de
                  referencia único para tu transferencia.
                </p>
              </div>
              {error && <p className="text-xs text-danger-600">{error}</p>}
              <button
                type="button"
                onClick={() => void handleStartPayment()}
                disabled={isCreating}
                className="w-full rounded-2xl bg-brand-600 py-3 text-sm font-bold text-white transition-colors hover:bg-brand-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                {isCreating ? "Generando código…" : "Continuar con SINPE →"}
              </button>
            </div>
          )}

          {/* Step: payment instructions */}
          {step === "payment" && reference && (
            <div className="space-y-4">
              <div className="rounded-2xl bg-surface-warm p-4 text-center">
                <p className="text-xs text-sand-500 mb-1">
                  Código de referencia
                </p>
                <p className="font-mono text-3xl font-black tracking-[0.2em] text-sand-900">
                  {reference}
                </p>
              </div>
              <ol className="space-y-3 text-sm text-sand-700">
                <li className="flex gap-3">
                  <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-brand-100 text-[10px] font-bold text-brand-700">
                    1
                  </span>
                  Abre tu app de banco y selecciona <strong>SINPE Móvil</strong>
                  .
                </li>
                <li className="flex gap-3">
                  <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-brand-100 text-[10px] font-bold text-brand-700">
                    2
                  </span>
                  Envía <strong>₡{price.toLocaleString("es-CR")}</strong> al
                  número <strong>{SINPE_NUMBER}</strong>.
                </li>
                <li className="flex gap-3">
                  <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-brand-100 text-[10px] font-bold text-brand-700">
                    3
                  </span>
                  Escribe el código <strong>{reference}</strong> en el campo de
                  asunto o descripción de la transferencia SINPE. Debe quedar
                  exactamente igual, sin espacios ni caracteres adicionales.
                </li>
                <li className="flex gap-3">
                  <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-brand-100 text-[10px] font-bold text-brand-700">
                    4
                  </span>
                  Vuelve aquí y presiona <strong>"Ya pagué"</strong>.
                </li>
              </ol>
              {error && <p className="text-xs text-danger-600">{error}</p>}
              <button
                type="button"
                onClick={() => void handleReportPayment()}
                disabled={isReporting}
                className="w-full rounded-2xl bg-rescue-600 py-3 text-sm font-bold text-white transition-colors hover:bg-rescue-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
              >
                {isReporting ? "Registrando…" : "✓ Ya realicé el pago SINPE"}
              </button>
              <p className="text-center text-xs text-sand-400">
                Un administrador verificará tu pago y activará el plan.
              </p>
            </div>
          )}

          {/* Step: reporting */}
          {step === "reporting" && (
            <div className="flex flex-col items-center gap-4 py-6">
              <div className="h-10 w-10 rounded-full border-4 border-brand-200 border-t-brand-500 animate-spin" />
              <p className="text-sm font-semibold text-sand-700">
                Registrando tu aviso…
              </p>
            </div>
          )}

          {/* Step: reported */}
          {step === "reported" && (
            <div className="flex flex-col items-center gap-4 py-4 text-center">
              <div className="flex h-14 w-14 items-center justify-center rounded-full bg-trust-100 text-3xl">
                🕐
              </div>
              <h3 className="text-lg font-black text-sand-900">
                ¡Aviso recibido!
              </h3>
              <p className="text-sm text-sand-600">
                Registramos que realizaste el pago SINPE. Un administrador lo
                verificará y activará el plan <strong>{label}</strong> en las
                próximas horas.
              </p>
              <p className="text-xs text-sand-400">
                Puedes cerrar esta ventana. Te notificaremos cuando se active.
              </p>
              <button
                type="button"
                onClick={onClose}
                className="mt-2 w-full rounded-2xl bg-brand-600 py-3 text-sm font-bold text-white hover:bg-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                Entendido
              </button>
            </div>
          )}
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}
