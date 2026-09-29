import { useState } from "react";
import { Link } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import { markOnboardingDone } from "./onboardingStorage";
import { useAuthStore } from "@/features/auth/store/authStore";
import { Modal } from "@/shared/ui/Modal";

const STEPS = [
  {
    emoji: "🐾",
    title: "¡Bienvenido a PawTrack CR!",
    body: "Registra a tu mascota y prepara su perfil público para facilitar la búsqueda si se pierde. Estas herramientas ayudan, pero no garantizan encontrarla.",
    cta: "Comenzar",
  },
  {
    emoji: "📋",
    title: "Registra a tu mascota",
    body: "Añade foto, nombre, especie y raza. Si la búsqueda por imagen está disponible para tu cuenta, puede ayudar a comparar avistamientos; no garantiza identificar a tu mascota. Revisa cada coincidencia.",
    cta: "Siguiente",
  },
  {
    emoji: "📲",
    title: "Genera su placa QR",
    body: "El QR abre el perfil público de tu mascota. Puedes ponerlo en una placa o etiqueta para facilitar su consulta; no garantiza el contacto ni la recuperación.",
    cta: "Registrar mi primera mascota",
    finalAction: true,
  },
];

interface OnboardingWizardProps {
  onDismiss?: () => void;
}

export function OnboardingWizard({ onDismiss }: OnboardingWizardProps) {
  const role = useAuthStore((state) => state.user?.role);
  const [step, setStep] = useState(0);

  if (role !== "Owner") return null;

  const dismiss = () => {
    markOnboardingDone();
    onDismiss?.();
  };

  const current = STEPS[step];

  return (
    <Modal isOpen title={current.title} onClose={dismiss} maxWidth={448}>
      <AnimatePresence mode="wait">
        <motion.div
          key={step}
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -16 }}
          transition={{ duration: 0.28, ease: [0.4, 0, 0.2, 1] }}
          className="w-full max-w-sm rounded-3xl bg-surface shadow-2xl overflow-hidden"
        >
          {/* Top gradient band */}
          <div className="bg-linear-to-br from-brand-500 to-brand-700 p-8 text-center">
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
            <div className="mb-4 flex justify-center gap-1.5" aria-label={`Paso ${step + 1} de ${STEPS.length}`}>
              {STEPS.map((_, i) => (
                <span
                  key={i}
                  className={`h-2 rounded-full transition-all ${i === step ? "w-6 bg-brand-500" : "w-2 bg-sand-200"}`}
                />
              ))}
            </div>

            <p aria-live="polite" className="text-center text-sm leading-relaxed text-copy-secondary">
              {current.body}
            </p>

            <div className="mt-6 flex flex-col gap-2">
              {current.finalAction ? (
                <Link
                  to="/pets/new"
                  onClick={dismiss}
                  className="flex items-center justify-center gap-2 rounded-2xl bg-brand-500 py-3.5 text-sm font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
                >
                  <span aria-hidden="true">＋</span> {current.cta}
                </Link>
              ) : (
                <button
                  type="button"
                  onClick={() => setStep((s) => s + 1)}
                  className="rounded-2xl bg-brand-500 py-3.5 text-sm font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
                >
                  {current.cta}
                </button>
              )}
              <button
                type="button"
                onClick={dismiss}
                className="rounded-2xl py-2.5 text-xs font-semibold text-copy-muted hover:text-copy-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sand-300"
              >
                Saltar por ahora
              </button>
            </div>
          </div>
        </motion.div>
      </AnimatePresence>
    </Modal>
  );
}
