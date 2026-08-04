import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { SinpePaymentModal } from "./SinpePaymentModal";
import { BundleOrderModal } from "@/features/bundles/components/BundleOrderModal";
import type { SubscriptionTier } from "../api/subscriptionApi";

interface Tier {
  id: "free" | "plus" | "familia";
  subscriptionTier?: SubscriptionTier;
  name: string;
  price: string;
  period: string;
  color: string;
  badge: string;
  glow?: string;
  features: { label: string; included: boolean }[];
  cta: string;
  current?: boolean;
}

const TIERS: Tier[] = [
  {
    id: "free",
    name: "Explorador",
    price: "Gratis",
    period: "siempre",
    color: "border-sand-200",
    badge: "bg-sand-100 text-sand-600",
    features: [
      { label: "1 mascota registrada", included: true },
      { label: "Placa QR de identidad", included: true },
      { label: "Historial: últimos 5 escaneos", included: true },
      { label: "Reporte de mascota perdida", included: true },
      { label: "Búsqueda IA por foto (3/mes)", included: true },
      { label: "Alertas en radio de 3 km", included: false },
      { label: "SMS/WhatsApp de alerta instantánea", included: false },
      { label: "Predicción de movimiento IA", included: false },
      { label: "Historial completo ilimitado", included: false },
      { label: "Hasta 3 mascotas", included: false },
    ],
    cta: "Plan actual",
    current: true,
  },
  {
    id: "plus",
    name: "Plus",
    price: "₡2,990",
    period: "por mes",
    color: "border-brand-400",
    badge: "bg-brand-100 text-brand-700",
    glow: "shadow-brand-200",
    features: [
      { label: "Todo lo del plan Explorador", included: true },
      { label: "Hasta 3 mascotas", included: true },
      { label: "Historial completo ilimitado", included: true },
      { label: "Alertas en radio de 10 km", included: true },
      { label: "SMS/WhatsApp de alerta instantánea", included: true },
      { label: "Búsqueda IA por foto ilimitada", included: true },
      { label: "Predicción de movimiento IA", included: true },
      { label: "Sala de coordinación activa", included: true },
      { label: "Mascotas ilimitadas", included: false },
      { label: "Multi-usuario (familia)", included: false },
    ],
    cta: "Activar Plus",
    subscriptionTier: "UserPlus" as SubscriptionTier,
  },
  {
    id: "familia",
    name: "Familia",
    price: "₡4,990",
    period: "por mes",
    color: "border-rescue-400",
    badge: "bg-rescue-100 text-rescue-700",
    features: [
      { label: "Todo lo del plan Plus", included: true },
      { label: "Mascotas ilimitadas", included: true },
      { label: "Multi-usuario (hasta 5 miembros)", included: true },
      { label: "Registros médicos y vacunas", included: true },
      { label: "Recordatorios veterinarios", included: true },
      { label: "Radio de alertas sin límite", included: true },
      { label: "Exportar historial en PDF", included: true },
      { label: "Soporte prioritario", included: true },
      { label: "", included: true },
      { label: "", included: true },
    ],
    cta: "Activar Familia",
    subscriptionTier: "UserFamilia" as SubscriptionTier,
  },
];

interface FreemiumModalProps {
  onClose: () => void;
}

export function FreemiumModal({ onClose }: FreemiumModalProps) {
  const [pendingTier, setPendingTier] = useState<SubscriptionTier | null>(null);
  const [showBundle, setShowBundle] = useState(false);

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
          className="w-full max-w-4xl rounded-3xl bg-surface p-6 shadow-2xl max-h-[92vh] overflow-y-auto"
          initial={{ y: 48, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: 48, opacity: 0 }}
          transition={{ type: "spring", stiffness: 380, damping: 34 }}
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="mb-6 flex items-start justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-brand-500">
                PawTrack Plus
              </p>
              <h2 className="mt-1 font-display text-2xl font-black text-sand-900">
                Más protección para tus mascotas
              </h2>
              <p className="mt-1 text-sm text-sand-500">
                Activa alertas instantáneas, IA de búsqueda sin límite y más
                desde ₡2,990/mes.
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

          {/* Tier cards */}
          <div className="grid gap-4 sm:grid-cols-3">
            {TIERS.map((tier, idx) => (
              <motion.div
                key={tier.id}
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: idx * 0.06 }}
                className={[
                  "relative flex flex-col rounded-2xl border-2 p-5",
                  tier.color,
                  tier.glow ? `shadow-lg shadow-brand-100` : "",
                ].join(" ")}
              >
                {tier.id === "plus" && (
                  <span className="absolute -top-3 left-1/2 -translate-x-1/2 rounded-full bg-brand-600 px-3 py-0.5 text-[10px] font-bold uppercase tracking-widest text-white">
                    Recomendado
                  </span>
                )}

                <div className="mb-4">
                  <span
                    className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-semibold ${tier.badge}`}
                  >
                    {tier.name}
                  </span>
                  <p className="mt-2 text-2xl font-extrabold text-sand-900">
                    {tier.price}
                  </p>
                  <p className="text-xs text-sand-400">{tier.period}</p>
                </div>

                <ul className="mb-5 flex-1 space-y-1.5">
                  {tier.features
                    .filter((f) => f.label)
                    .map((f) => (
                      <li
                        key={f.label}
                        className="flex items-start gap-2 text-xs text-sand-700"
                      >
                        <span
                          className={`mt-0.5 shrink-0 text-sm leading-none ${f.included ? "text-rescue-600" : "text-sand-300"}`}
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

                {tier.current ? (
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
                      "block w-full rounded-xl py-2.5 text-center text-xs font-bold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400",
                      tier.id === "plus"
                        ? "bg-brand-600 text-white hover:bg-brand-700"
                        : "bg-rescue-600 text-white hover:bg-rescue-700",
                    ].join(" ")}
                  >
                    {tier.cta}
                  </button>
                )}
              </motion.div>
            ))}
          </div>

          <p className="mt-5 text-center text-xs text-sand-400">
            Pagos seguros vía SINPE Móvil · Sin contrato · Cancela cuando
            quieras ·{" "}
            <a
              href="mailto:soporte@pawtrack.cr"
              className="text-brand-600 hover:underline"
            >
              soporte@pawtrack.cr
            </a>
          </p>

          {/* Bundle GPS CTA */}
          <div className="mt-5 rounded-2xl border border-brand-200 bg-gradient-to-r from-brand-50 to-rescue-50 p-4 flex items-center gap-4">
            <span className="text-3xl shrink-0" aria-hidden="true">
              📡
            </span>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-bold text-brand-800">
                Bundle Collar GPS + 12 meses Plus
              </p>
              <p className="text-xs text-brand-600 opacity-80">
                Tractive GPS + PawTrack Plus todo incluido · Envío a CR
              </p>
            </div>
            <div className="text-right shrink-0">
              <p className="text-sm font-black text-brand-900">₡49,900</p>
              <button
                type="button"
                onClick={() => setShowBundle(true)}
                className="mt-1 rounded-xl bg-brand-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                Pedir collar →
              </button>
            </div>
          </div>

          {/* Bundle modal (shown inline) */}
          {showBundle && (
            <div className="mt-4 rounded-2xl border border-sand-200 bg-surface p-4">
              <BundleOrderModal onClose={() => setShowBundle(false)} />
            </div>
          )}
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}
