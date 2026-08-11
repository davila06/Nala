import { useState } from "react";
import { Drawer } from "@/shared/ui/Drawer";
import { Button } from "@/shared/ui/Button";

const STEPS = [
  {
    icon: "📲",
    title: "Descarga NFC Tools",
    body: (
      <>
        Instala la app gratuita <strong>NFC Tools</strong> en tu teléfono.
        <div className="mt-3 flex gap-3">
          <a
            href="https://apps.apple.com/app/nfc-tools/id1252962749"
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 rounded-xl border border-sand-200 py-2 text-center text-xs font-semibold text-sand-700 hover:bg-sand-50"
          >
            🍎 App Store
          </a>
          <a
            href="https://play.google.com/store/apps/details?id=com.wakdev.wdnfc"
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 rounded-xl border border-sand-200 py-2 text-center text-xs font-semibold text-sand-700 hover:bg-sand-50"
          >
            🤖 Play Store
          </a>
        </div>
      </>
    ),
  },
  {
    icon: "✏️",
    title: "Escribe la URL en el chip",
    body: (
      <>
        Abre NFC Tools → <strong>Escribir</strong> →{" "}
        <strong>Agregar registro</strong> → <strong>URL</strong>.
        <div className="mt-2 rounded-xl border border-sand-100 bg-sand-50 px-3 py-2 font-mono text-xs text-sand-700 break-all select-all">
          https://pawtrack.cr/p/[id-de-tu-mascota]
        </div>
        <p className="mt-2 text-xs text-sand-500">
          Puedes copiar el enlace desde el perfil de tu mascota en PawTrack.
        </p>
      </>
    ),
  },
  {
    icon: "🏷️",
    title: "Acerca el chip al teléfono",
    body: (
      <>
        Toca <strong>Escribir</strong> en NFC Tools y acerca el chip NFC al
        lector de tu teléfono. En la mayoría de dispositivos Android el lector
        está en la parte trasera, cerca del centro.
        <p className="mt-2 text-xs text-sand-500">
          La escritura tarda menos de 1 segundo. Verás una confirmación en
          pantalla.
        </p>
      </>
    ),
  },
  {
    icon: "✅",
    title: "Verifica que funciona",
    body: (
      <>
        Abre NFC Tools → <strong>Leer</strong> y acerca el chip nuevamente.
        Deberías ver la URL de PawTrack registrada.
        <p className="mt-2 text-sm font-medium text-rescue-700">
          ¡Listo! Cualquier teléfono Android puede tocar el collar y ver el
          perfil de tu mascota.
        </p>
        <p className="mt-1 text-xs text-sand-500">
          Los iPhone con iOS 14+ también pueden leer el chip desde la cámara
          nativa.
        </p>
      </>
    ),
  },
];

interface NfcSetupGuideProps {
  isOpen: boolean;
  onClose: () => void;
  petProfileUrl?: string;
}

export function NfcSetupGuide({
  isOpen,
  onClose,
  petProfileUrl,
}: NfcSetupGuideProps) {
  const [step, setStep] = useState(0);
  const current = STEPS[step]!;
  const isLast = step === STEPS.length - 1;

  const handleClose = () => {
    setStep(0);
    onClose();
  };

  return (
    <Drawer
      isOpen={isOpen}
      onClose={handleClose}
      title="Configurar chip NFC"
      side="bottom"
    >
      <div className="space-y-5 pb-safe">
        {/* Step indicators */}
        <div
          className="flex justify-center gap-1.5"
          aria-label={`Paso ${step + 1} de ${STEPS.length}`}
        >
          {STEPS.map((_, i) => (
            <span
              key={i}
              className={`h-1.5 rounded-full transition-all ${
                i === step
                  ? "w-6 bg-brand-500"
                  : i < step
                    ? "w-3 bg-brand-200"
                    : "w-3 bg-sand-200"
              }`}
            />
          ))}
        </div>

        {/* Current step */}
        <div className="rounded-2xl border border-sand-100 bg-sand-50 p-5 space-y-2">
          <div className="text-3xl" aria-hidden="true">
            {current.icon}
          </div>
          <h3 className="font-display text-base font-semibold text-sand-900">
            {current.title}
          </h3>
          <div className="text-sm text-sand-700 leading-relaxed">
            {current.body}
          </div>
        </div>

        {/* Pet profile URL copy (shown on step 1) */}
        {step === 1 && petProfileUrl && (
          <div className="rounded-xl border border-brand-200 bg-brand-50 p-3">
            <p className="text-xs font-medium text-brand-700 mb-1">
              URL de tu mascota:
            </p>
            <div className="flex items-center gap-2">
              <code className="flex-1 text-xs break-all text-brand-900">
                {petProfileUrl}
              </code>
              <button
                type="button"
                onClick={() => navigator.clipboard.writeText(petProfileUrl)}
                className="shrink-0 rounded-lg bg-brand-600 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                Copiar
              </button>
            </div>
          </div>
        )}

        {/* Navigation */}
        <div className="flex gap-3">
          {step > 0 && (
            <Button
              variant="secondary"
              onClick={() => setStep((s) => s - 1)}
              className="flex-1"
            >
              ← Anterior
            </Button>
          )}
          {isLast ? (
            <Button onClick={handleClose} className="flex-1">
              Listo 🎉
            </Button>
          ) : (
            <Button onClick={() => setStep((s) => s + 1)} className="flex-1">
              Siguiente →
            </Button>
          )}
        </div>
      </div>
    </Drawer>
  );
}
