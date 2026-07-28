import { useState } from "react";
import { Link } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";

const STEPS = [
  {
    emoji: "🐾",
    title: "¡Bienvenido a PawTrack CR!",
    body: "Tu plataforma para proteger a tus mascotas y reunirte con ellas si alguna vez se pierden. Solo necesitas 3 pasos para estar listo.",
    cta: "Comenzar",
  },
  {
    emoji: "📋",
    title: "Registra a tu mascota",
    body: "Añade foto, nombre, especie y raza. Con esta información, la IA puede reconocerla visualmente en el mapa de avistamientos.",
    cta: "Siguiente",
  },
  {
    emoji: "📲",
    title: "Genera su placa QR",
    body: "Una vez registrada, genera la placa QR de identidad. Imprímela en un collar o en una etiqueta: cualquier persona que la encuentre puede escanearlo para contactarte.",
    cta: "Registrar mi primera mascota",
    finalAction: true,
  },
];

const STORAGE_KEY = "pawtrack_onboarding_done";

interface OnboardingWizardProps {
  onDismiss?: () => void;
}

export function OnboardingWizard({ onDismiss }: OnboardingWizardProps) {
  const [step, setStep] = useState(0);

  const dismiss = () => {
    localStorage.setItem(STORAGE_KEY, "1");
    onDismiss?.();
  };

  const current = STEPS[step]!;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end sm:items-center justify-center bg-black/50 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="onboarding-title"
    >
      <AnimatePresence mode="wait">
        <motion.div
          key={step}
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -16 }}
          transition={{ duration: 0.28, ease: [0.4, 0, 0.2, 1] }}
          className="w-full max-w-sm rounded-3xl bg-white shadow-2xl overflow-hidden"
        >
          {/* Top gradient band */}
          <div className="bg-gradient-to-br from-brand-500 to-brand-700 p-8 text-center">
            <motion.span
              initial={{ scale: 0.6 }}
              animate={{ scale: 1 }}
              transition={{ type: "spring", stiffness: 400, damping: 20 }}
              className="block text-6xl"
              aria-hidden="true"
            >
              {current.emoji}
            </motion.span>
          </div>

          {/* Content */}
          <div className="px-6 py-5">
            {/* Step dots */}
            <div
              className="mb-4 flex justify-center gap-1.5"
              aria-label={`Paso ${step + 1} de ${STEPS.length}`}
            >
              {STEPS.map((_, i) => (
                <span
                  key={i}
                  className={`h-2 rounded-full transition-all ${i === step ? "w-6 bg-brand-500" : "w-2 bg-sand-200"}`}
                />
              ))}
            </div>

            <h2
              id="onboarding-title"
              className="mb-2 text-center font-display text-xl font-bold text-sand-900"
            >
              {current.title}
            </h2>
            <p className="text-center text-sm leading-relaxed text-sand-600">
              {current.body}
            </p>

            <div className="mt-6 flex flex-col gap-2">
              {current.finalAction ? (
                <Link
                  to="/pets/new"
                  onClick={dismiss}
                  className="flex items-center justify-center gap-2 rounded-2xl bg-brand-500 py-3.5 text-sm font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  <span aria-hidden="true">＋</span> {current.cta}
                </Link>
              ) : (
                <button
                  type="button"
                  onClick={() => setStep((s) => s + 1)}
                  className="rounded-2xl bg-brand-500 py-3.5 text-sm font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  {current.cta}
                </button>
              )}
              <button
                type="button"
                onClick={dismiss}
                className="rounded-2xl py-2.5 text-xs font-semibold text-sand-400 hover:text-sand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sand-300"
              >
                Saltar por ahora
              </button>
            </div>
          </div>
        </motion.div>
      </AnimatePresence>
    </div>
  );
}

export function shouldShowOnboarding(): boolean {
  return !localStorage.getItem(STORAGE_KEY);
}
