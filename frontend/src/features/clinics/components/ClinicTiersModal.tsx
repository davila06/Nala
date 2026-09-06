import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { SinpePaymentModal } from "@/features/pets/components/SinpePaymentModal";
import type { SubscriptionTier } from "@/features/pets/api/subscriptionApi";
import { useSubscriptionCatalog } from "@/features/pets/hooks/useSubscription";

interface Tier {
  id: "basic" | "plus" | "partner";
  subscriptionTier?: SubscriptionTier;
  name: string;
  price: string;
  period: string;
  color: string;
  badge: string;
  features: { label: string; included: boolean }[];
  cta: string;
  popular?: boolean;
}

const TIERS: Tier[] = [
  {
    id: "basic",
    name: "Afiliada básica",
    price: "Gratis",
    period: "siempre",
    color: "border-sand-200",
    badge: "bg-sand-100 text-sand-600",
    features: [
      { label: "Aparece en directorio de clínicas", included: true },
      { label: "Escanear QR y microchip RFID", included: true },
      { label: "Portal de identificación de mascotas", included: true },
      { label: "Posición destacada en mapa", included: false },
      { label: 'Badge "Clínica Verificada"', included: false },
      { label: "Estadísticas de escaneos", included: false },
      { label: "Logo en alertas de pérdida cercanas", included: false },
      { label: "Widget embebible para tu sitio", included: false },
    ],
    cta: "Plan actual",
  },
  {
    id: "plus",
    name: "Clínica Plus",
    price: "₡15,000",
    period: "por mes",
    color: "border-trust-400",
    badge: "bg-trust-100 text-trust-700",
    popular: true,
    features: [
      { label: "Todo lo del plan básico", included: true },
      { label: "Posición destacada en mapa", included: true },
      { label: 'Badge "Clínica Verificada"', included: true },
      { label: "Estadísticas de escaneos mensuales", included: true },
      { label: "Logo en alertas de pérdida cercanas", included: true },
      { label: "Widget embebible para tu sitio", included: false },
      { label: "Soporte prioritario", included: false },
      { label: "Banner en Case Rooms cercanos", included: false },
    ],
    cta: "Activar Plus",
    subscriptionTier: "ClinicPlus" as SubscriptionTier,
  },
  {
    id: "partner",
    name: "Clínica Partner",
    price: "₡35,000",
    period: "por mes",
    color: "border-brand-400",
    badge: "bg-brand-100 text-brand-700",
    features: [
      { label: "Todo lo del plan Plus", included: true },
      { label: "Widget embebible para tu sitio", included: true },
      { label: "Soporte prioritario 24/7", included: true },
      { label: "Banner en Case Rooms cercanos", included: true },
      { label: "Certificado veterinario PDF (próximamente)", included: true },
      { label: "API de consulta directa", included: true },
      { label: "Integración microchip RFID avanzada", included: true },
      { label: "Gestor de cuenta dedicado", included: true },
    ],
    cta: "Activar Partner",
    subscriptionTier: "ClinicPartner" as SubscriptionTier,
  },
];

interface ClinicTiersModalProps {
  currentTier?: "basic" | "plus" | "partner";
  onClose: () => void;
}

export function ClinicTiersModal({
  currentTier = "basic",
  onClose,
}: ClinicTiersModalProps) {
  const [pendingTier, setPendingTier] = useState<SubscriptionTier | null>(null);
  const { data: catalog } = useSubscriptionCatalog();

  if (pendingTier) {
    return (
      <SinpePaymentModal
        tier={pendingTier}
        onClose={() => setPendingTier(null)}
        onSuccess={onClose}
      />
    );
  }

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
          className="w-full max-w-3xl rounded-3xl bg-surface p-6 shadow-2xl max-h-[90vh] overflow-y-auto"
          initial={{ y: 40, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: 40, opacity: 0 }}
          transition={{ type: "spring", stiffness: 400, damping: 35 }}
          onClick={(e) => e.stopPropagation()}
        >
          <div className="mb-6 flex items-start justify-between">
            <div>
              <h2 className="font-display text-xl font-bold text-sand-900">
                Planes PawTrack para Clínicas
              </h2>
              <p className="mt-1 text-sm text-sand-500">
                Potencia tu clínica y llega a más dueños de mascotas
              </p>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="rounded-xl p-2 text-sand-400 hover:bg-sand-100 hover:text-sand-700 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              aria-label="Cerrar"
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

          <div className="grid gap-4 sm:grid-cols-3">
            {TIERS.map((tier) => {
              const isCurrent = tier.id === currentTier;
              return (
                <div
                  key={tier.id}
                  className={[
                    "relative rounded-2xl border-2 p-5 flex flex-col",
                    tier.color,
                    tier.popular ? "shadow-lg" : "",
                  ].join(" ")}
                >
                  {tier.popular && (
                    <span className="absolute -top-3 left-1/2 -translate-x-1/2 rounded-full bg-trust-600 px-3 py-0.5 text-[10px] font-bold uppercase tracking-widest text-white">
                      Más popular
                    </span>
                  )}

                  <div className="mb-4">
                    <span
                      className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-semibold ${tier.badge}`}
                    >
                      {tier.name}
                    </span>
                    <p className="mt-2 text-2xl font-extrabold text-sand-900">
                      {(() => {
                        const plan = tier.subscriptionTier
                          ? catalog?.find(
                              (item) => item.tier === tier.subscriptionTier,
                            )
                          : undefined;
                        return plan?.monthlyPriceCrc
                          ? `₡${plan.monthlyPriceCrc.toLocaleString("es-CR")}`
                          : tier.price;
                      })()}
                    </p>
                    <p className="text-xs text-sand-400">{tier.period}</p>
                  </div>

                  <ul className="mb-5 flex-1 space-y-2">
                    {tier.features.map((f) => (
                      <li
                        key={f.label}
                        className="flex items-start gap-2 text-xs text-sand-700"
                      >
                        <span
                          className={`mt-0.5 shrink-0 ${f.included ? "text-rescue-600" : "text-sand-300"}`}
                          aria-hidden="true"
                        >
                          {f.included ? "✓" : "✗"}
                        </span>
                        <span
                          className={
                            f.included ? "" : "text-sand-400 line-through"
                          }
                        >
                          {f.label}
                        </span>
                      </li>
                    ))}
                  </ul>

                  {isCurrent ? (
                    <span className="block rounded-xl border border-sand-200 py-2.5 text-center text-xs font-semibold text-sand-400">
                      Plan actual
                    </span>
                  ) : (
                    <button
                      type="button"
                      onClick={() =>
                        tier.subscriptionTier &&
                        setPendingTier(tier.subscriptionTier)
                      }
                      className={[
                        "block w-full rounded-xl py-2.5 text-center text-xs font-bold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400",
                        tier.popular
                          ? "bg-trust-600 text-white hover:bg-trust-700"
                          : "bg-sand-900 text-white hover:bg-sand-700",
                      ].join(" ")}
                    >
                      {tier.cta}
                    </button>
                  )}
                </div>
              );
            })}
          </div>

          <p className="mt-5 text-center text-xs text-sand-400">
            ¿Preguntas? Escríbenos a{" "}
            <a
              href="mailto:alianzas@pawtrack.cr"
              className="text-brand-600 hover:underline"
            >
              alianzas@pawtrack.cr
            </a>
          </p>
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}
