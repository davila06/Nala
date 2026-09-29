import { useEffect, useRef, useState } from "react";
import { useCaptureContext, usePaymentProfiles } from "../hooks/usePaymentProfiles";
import { useHaptic } from "@/shared/hooks/useHaptic";

export interface CardPaymentData {
  paymentProfileId?: string;
  transientToken?: string;
  cardholderName?: string;
  saveProfile?: boolean;
}

interface SecureCardPaymentFormProps {
  amountCrc: number;
  isProcessing: boolean;
  onPay: (data: CardPaymentData) => void | Promise<void>;
  buttonLabel?: string;
}

function formatExpiry(val: string): string {
  const clean = val.replace(/\D/g, "").slice(0, 4);
  if (clean.length > 2) {
    return `${clean.slice(0, 2)}/${clean.slice(2)}`;
  }
  return clean;
}

type HostedField = {
  load: (element: HTMLElement) => void;
};

type Microform = {
  createField: (name: "number" | "securityCode", options?: Record<string, string>) => HostedField;
  createToken: (
    options: { cardExpirationMonth: string; cardExpirationYear: string },
    callback: (error: Error | null, token?: string) => void,
  ) => void;
};

type Flex = new (captureContextJwt: string) => {
  microform: (options?: { styles?: Record<string, Record<string, string>> }) => Microform;
};

declare global {
  interface Window {
    Flex?: Flex;
  }
}

async function loadCyberSourceScript(url: string): Promise<void> {
  if (window.Flex) return;

  const existing = document.querySelector<HTMLScriptElement>('script[data-cybersource-flex="true"]');
  if (existing) {
    await new Promise<void>((resolve, reject) => {
      existing.addEventListener("load", () => resolve(), { once: true });
      existing.addEventListener("error", () => reject(new Error("No fue posible cargar CyberSource Flex.")), {
        once: true,
      });
    });
    return;
  }

  await new Promise<void>((resolve, reject) => {
    const script = document.createElement("script");
    script.src = url;
    script.async = true;
    script.dataset.cybersourceFlex = "true";
    script.onload = () => resolve();
    script.onerror = () => reject(new Error("No fue posible cargar CyberSource Flex."));
    document.head.appendChild(script);
  });
}

export function SecureCardPaymentForm({ amountCrc, isProcessing, onPay, buttonLabel }: SecureCardPaymentFormProps) {
  const { data: savedProfiles = [], isLoading: loadingProfiles } = usePaymentProfiles();
  const { data: captureContext, isLoading: loadingCaptureContext } = useCaptureContext();
  const { tap, warning } = useHaptic();

  const [useNewCard, setUseNewCard] = useState(false);
  const [selectedProfileId, setSelectedProfileId] = useState<string | null>(null);

  const [cardholderName, setCardholderName] = useState("");
  const [expiry, setExpiry] = useState("");
  const [saveProfile, setSaveProfile] = useState(true);
  const [formError, setFormError] = useState<string | null>(null);
  const [microform, setMicroform] = useState<Microform | null>(null);
  const cardNumberFieldRef = useRef<HTMLDivElement>(null);
  const securityCodeFieldRef = useRef<HTMLDivElement>(null);

  const hasSavedCards = savedProfiles.length > 0;
  const activeUseNewCard = useNewCard || !hasSavedCards;

  useEffect(() => {
    if (!captureContext?.isConfigured || !captureContext.captureContextJwt || !captureContext.clientLibraryUrl) return;

    let cancelled = false;
    const initializeMicroform = async () => {
      try {
        await loadCyberSourceScript(captureContext.clientLibraryUrl);
        if (cancelled || !window.Flex) return;

        const flex = new window.Flex(captureContext.captureContextJwt);
        setMicroform(
          flex.microform({
            styles: {
              input: { "font-family": "inherit", "font-size": "14px", color: "#292524" },
              "input.invalid": { color: "#b91c1c" },
            },
          }),
        );
      } catch {
        if (!cancelled) setFormError("El pago seguro no está disponible en este momento.");
      }
    };

    void initializeMicroform();
    return () => {
      cancelled = true;
    };
  }, [captureContext]);

  useEffect(() => {
    if (!microform || !cardNumberFieldRef.current || !securityCodeFieldRef.current) return;

    microform.createField("number", { placeholder: "Número de tarjeta" }).load(cardNumberFieldRef.current);
    microform.createField("securityCode", { placeholder: "Código de seguridad" }).load(securityCodeFieldRef.current);
  }, [microform]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!activeUseNewCard) {
      const profileId = selectedProfileId ?? savedProfiles[0]?.id;
      if (!profileId) {
        setFormError("Por favor selecciona una tarjeta guardada.");
        warning();
        return;
      }
      tap();
      void onPay({ paymentProfileId: profileId });
      return;
    }

    const [mm, yy] = expiry.split("/");
    const month = parseInt(mm ?? "0", 10);
    const year = parseInt(yy ?? "0", 10);
    if (!month || month < 1 || month > 12 || !year) {
      setFormError("Fecha de vencimiento inválida (MM/AA).");
      warning();
      return;
    }

    if (!microform) {
      setFormError("El pago seguro no está disponible en este momento.");
      warning();
      return;
    }

    if (!cardholderName.trim()) {
      setFormError("Por favor ingresa el nombre impreso en la tarjeta.");
      warning();
      return;
    }

    tap();
    microform.createToken(
      {
        cardExpirationMonth: month.toString().padStart(2, "0"),
        cardExpirationYear: yy.length === 2 ? `20${yy}` : yy,
      },
      (error, transientToken) => {
        if (error || !transientToken) {
          setFormError("CyberSource no pudo validar la tarjeta.");
          warning();
          return;
        }

        void onPay({
          transientToken,
          cardholderName: cardholderName.trim(),
          saveProfile,
        });
      },
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Selector: Tarjetas guardadas vs Nueva tarjeta */}
      {hasSavedCards && (
        <div className="space-y-2">
          <p className="text-xs font-semibold text-sand-700">Método de pago guardado</p>
          <div className="space-y-1.5">
            {savedProfiles.map((p) => {
              const isSelected = !useNewCard && (selectedProfileId === p.id || (!selectedProfileId && p.isDefault));
              return (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => {
                    setUseNewCard(false);
                    setSelectedProfileId(p.id);
                  }}
                  className={`w-full flex items-center justify-between p-3 rounded-2xl border text-left transition-colors ${
                    isSelected
                      ? "border-brand-600 bg-brand-50/60 ring-1 ring-brand-500 shadow-xs"
                      : "border-sand-200 bg-surface hover:bg-sand-50"
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <span className="text-xl" aria-hidden="true">
                      💳
                    </span>
                    <div>
                      <p className="text-xs font-bold text-sand-900">
                        {p.cardBrand} •••• {p.lastFourDigits}
                      </p>
                      <p className="text-[10px] text-copy-secondary">
                        {p.cardholderName || "Titular"} · Vence {p.expirationMonth?.toString().padStart(2, "0")}/
                        {p.expirationYear}
                      </p>
                    </div>
                  </div>
                  {p.isDefault && (
                    <span className="text-[10px] font-bold uppercase bg-sand-200 text-sand-700 px-2 py-0.5 rounded-full">
                      Predeterminada
                    </span>
                  )}
                </button>
              );
            })}

            <button
              type="button"
              onClick={() => setUseNewCard(true)}
              className={`w-full p-2.5 rounded-2xl border text-center text-xs font-bold transition-colors ${
                useNewCard
                  ? "border-brand-600 bg-brand-50/60 text-brand-900"
                  : "border-dashed border-sand-300 text-copy-secondary hover:bg-sand-50"
              }`}
            >
              + Usar una tarjeta de crédito o débito distinta
            </button>
          </div>
        </div>
      )}

      {/* Formulario de Nueva Tarjeta */}
      {activeUseNewCard && (
        <div className="space-y-3 rounded-2xl border border-sand-200 bg-surface-warm p-4 text-left">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-sand-900 flex items-center gap-1.5">
              <span>💳</span> Datos de la tarjeta
            </span>
            <span className="text-[10px] font-bold text-brand-700 bg-brand-100/70 px-2 py-0.5 rounded-md">
              CyberSource Flex
            </span>
          </div>

          <div>
            <label htmlFor="card-name" className="block text-[11px] font-semibold text-sand-700 mb-1">
              Nombre en la tarjeta
            </label>
            <input
              id="card-name"
              type="text"
              value={cardholderName}
              onChange={(e) => setCardholderName(e.target.value)}
              placeholder="Ej: MARÍA MORA ROJAS"
              autoComplete="cc-name"
              disabled={isProcessing}
              className="w-full rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-sand-900 outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 uppercase"
            />
          </div>

          <div>
            <span className="block text-[11px] font-semibold text-sand-700 mb-1">Campo seguro de tarjeta</span>
            <div
              ref={cardNumberFieldRef}
              data-testid="cybersource-card-number"
              className="min-h-9 rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-copy-muted"
              aria-label="Campo seguro de número de tarjeta"
            />
          </div>

          <div className="grid grid-cols-2 gap-2">
            <div>
              <label htmlFor="card-expiry" className="block text-[11px] font-semibold text-sand-700 mb-1">
                Vencimiento (MM/AA)
              </label>
              <input
                id="card-expiry"
                type="text"
                value={expiry}
                onChange={(e) => setExpiry(formatExpiry(e.target.value))}
                placeholder="MM/AA"
                autoComplete="cc-exp"
                inputMode="numeric"
                disabled={isProcessing}
                className="w-full font-mono rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-sand-900 outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 text-center"
              />
            </div>

            <div>
              <span className="block text-[11px] font-semibold text-sand-700 mb-1">Código seguro</span>
              <div
                ref={securityCodeFieldRef}
                data-testid="cybersource-security-code"
                className="min-h-9 rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-copy-muted"
                aria-label="Campo seguro de código de tarjeta"
              />
            </div>
          </div>

          <label className="flex items-center gap-2 pt-1 cursor-pointer">
            <input
              type="checkbox"
              checked={saveProfile}
              onChange={(e) => setSaveProfile(e.target.checked)}
              disabled={isProcessing}
              className="h-4 w-4 rounded text-brand-600 focus:ring-brand-500 border-sand-300"
            />
            <span className="text-[11px] text-copy-secondary font-medium">
              Guardar de forma segura para renovaciones automáticas
            </span>
          </label>
        </div>
      )}

      {formError && <p className="text-xs text-danger-600 font-medium">{formError}</p>}

      {/* Botón de pago */}
      <button
        type="submit"
        disabled={isProcessing || loadingProfiles || loadingCaptureContext}
        className="w-full rounded-2xl bg-brand-600 py-3 text-sm font-bold text-white transition-colors hover:bg-brand-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring shadow-sm"
      >
        {isProcessing ? (
          <span className="inline-flex items-center gap-2">
            <span className="h-4 w-4 rounded-full border-2 border-white/40 border-t-white animate-spin" />
            Procesando cobro seguro…
          </span>
        ) : (
          buttonLabel || `Pagar ₡${amountCrc.toLocaleString("es-CR")}`
        )}
      </button>

      {/* Certificaciones y cumplimiento PCI */}
      <div className="pt-1 border-t border-sand-200/60 flex items-center justify-between text-[10px] text-copy-muted">
        <span className="flex items-center gap-1">
          🔒 <span>Cifrado TLS 256-bit</span>
        </span>
        <span>Cumplimiento PCI-DSS SAQ A</span>
        <span>CyberSource · Visa</span>
      </div>
    </form>
  );
}
