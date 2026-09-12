import { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useCreateSubscription, useReportPayment, useSubscriptionCatalog } from "../hooks/useSubscription";
import { subscriptionApi } from "../api/subscriptionApi";
import type { SubscriptionTier } from "../api/subscriptionApi";
import { TIER_PRICE_CRC } from "../api/subscriptionApi";
import { useHaptic } from "@/shared/hooks/useHaptic";
import { SecureCardPaymentForm, type CardPaymentData } from "@/features/payments/components/SecureCardPaymentForm";
import { useChargeCard } from "@/features/payments/hooks/usePaymentProfiles";
import { useBillingProfile } from "@/features/payments/hooks/useBilling";

interface SinpePaymentModalProps {
  tier: SubscriptionTier;
  clinicId?: string;
  onClose: () => void;
  onSuccess?: () => void;
}

const SINPE_NUMBER = import.meta.env.VITE_SINPE_PHONE ?? "7000-0000";
const USER_BILLING_TERMS = [1, 3, 6, 12] as const;

const SINPE_BANKS = [
  { id: "bac", name: "BAC Credomatic", number: "2223" },
  { id: "bncr", name: "Banco Nacional", number: "2627" },
  { id: "bcr", name: "Banco de Costa Rica", number: "2277" },
  { id: "davivienda", name: "Davivienda", number: "7070" },
  { id: "direct", name: "Otro / Directo", number: "" },
] as const;

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

export function SinpePaymentModal({ tier, clinicId, onClose, onSuccess }: SinpePaymentModalProps) {
  const [step, setStep] = useState<"confirm" | "payment" | "reporting" | "reported" | "card_success">("confirm");
  const [paymentMethod, setPaymentMethod] = useState<"sinpe" | "card">("sinpe");
  const [paymentView, setPaymentView] = useState<"instructions" | "qr">("instructions");
  const [reference, setReference] = useState<string | null>(null);
  const [subscriptionId, setSubscriptionId] = useState<string | null>(null);
  const [billingMonths, setBillingMonths] = useState(1);
  const [receiptNumber, setReceiptNumber] = useState("");
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [qrBlobUrl, setQrBlobUrl] = useState<string | null>(null);
  const [loadingQr, setLoadingQr] = useState(false);
  const [cardAuthCode, setCardAuthCode] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { tap, success, warning } = useHaptic();

  const { mutateAsync: createSub, isPending: isCreating } = useCreateSubscription();
  const { mutateAsync: reportPayment, isPending: isReporting } = useReportPayment();
  const { mutateAsync: chargeCard, isPending: isCharging } = useChargeCard();
  const { data: catalog } = useSubscriptionCatalog();
  const { data: billingProfile } = useBillingProfile();

  const [requiresInvoice, setRequiresInvoice] = useState(false);

  useEffect(() => {
    if (billingProfile?.requiresInvoice) {
      setRequiresInvoice(true);
    }
  }, [billingProfile]);

  const catalogPlan = catalog?.find((plan) => plan.tier === tier);
  const isUserPlan = tier === "UserPlus" || tier === "UserFamilia";
  const monthlyPrice = catalogPlan?.monthlyPriceCrc ?? TIER_PRICE_CRC[tier];
  const basePrice = isUserPlan
    ? monthlyPrice * billingMonths * (billingMonths === 12 ? 0.8 : 1)
    : (catalogPlan?.annualPriceCrc ?? monthlyPrice);
  const ivaAmount = Math.round(basePrice * 0.13);
  const totalPrice = requiresInvoice ? Math.round(basePrice * 1.13) : Math.round(basePrice);
  const price = totalPrice;

  const label = catalogPlan?.displayName ?? TIER_LABELS[tier];
  const pricePeriod = isUserPlan
    ? `${billingMonths} ${billingMonths === 1 ? "mes" : "meses"}`
    : catalogPlan?.annualPriceCrc
      ? "año"
      : "mes";

  const cleanPhone = SINPE_NUMBER.replace(/\D/g, "");
  const formattedAmount = totalPrice;
  const smsBody = `PASE ${formattedAmount} ${cleanPhone} ${reference ?? ""}`;

  useEffect(() => {
    let active = true;
    let localUrl: string | null = null;

    if (step === "payment" && subscriptionId) {
      setLoadingQr(true);
      subscriptionApi
        .getSinpeQrBlob(subscriptionId)
        .then((blob) => {
          if (active) {
            localUrl = URL.createObjectURL(blob);
            setQrBlobUrl(localUrl);
          }
        })
        .catch(() => {
          // Non-blocking: QR tab will show fallback instructions
        })
        .finally(() => {
          if (active) setLoadingQr(false);
        });
    }

    return () => {
      active = false;
      if (localUrl) {
        URL.revokeObjectURL(localUrl);
      }
    };
  }, [step, subscriptionId]);

  async function handleStartPayment() {
    setError(null);
    try {
      const sub = await createSub({ tier, billingMonths, clinicId, requiresInvoice });
      setReference(sub.paymentReference);
      setSubscriptionId(sub.id);
      setStep("payment");
      tap();
    } catch {
      setError("No se pudo generar el código de pago. Intenta de nuevo.");
    }
  }

  async function handleCardPayment(data: CardPaymentData) {
    setError(null);
    try {
      let subId = subscriptionId;
      if (!subId) {
        const sub = await createSub({ tier, billingMonths, clinicId, requiresInvoice });
        subId = sub.id;
        setSubscriptionId(sub.id);
        setReference(sub.paymentReference);
      }

      const result = await chargeCard({
        amountCrc: totalPrice,
        purpose: "Subscription",
        targetEntityId: subId,
        paymentProfileId: data.paymentProfileId,
        transientToken: data.transientToken,
        cardholderName: data.cardholderName,
        saveProfile: data.saveProfile,
      });

      if (!result.success) {
        setError(result.errorMessage ?? "La transacción fue declinada.");
        warning();
        return;
      }

      setCardAuthCode(result.authorizationCode);
      setStep("card_success");
      success();
      onSuccess?.();
    } catch {
      setError("Error de comunicación al procesar el pago con tarjeta. Intenta de nuevo.");
      warning();
    }
  }

  async function handleReportPayment() {
    if (!subscriptionId) return;
    setStep("reporting");
    setError(null);
    try {
      await reportPayment({
        subscriptionId,
        bankReceiptNumber: receiptNumber.trim() || undefined,
      });
      setStep("reported");
      success();
      onSuccess?.();
    } catch {
      setError("No se pudo registrar tu aviso. Intenta de nuevo.");
      setStep("payment");
    }
  }

  const handleCopy = (text: string, fieldId: string) => {
    tap();
    void navigator.clipboard.writeText(text);
    setCopiedField(fieldId);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const getSmsUrl = (bankNumber: string) => {
    const targetNumber = bankNumber || cleanPhone;
    return `sms:${targetNumber}?body=${encodeURIComponent(smsBody)}`;
  };

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
          className="w-full max-w-md rounded-3xl bg-surface p-6 shadow-2xl max-h-[92vh] overflow-y-auto"
          initial={{ y: 40, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: 40, opacity: 0 }}
          transition={{ type: "spring", stiffness: 400, damping: 35 }}
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="mb-4 flex items-start justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-brand-500">
                {paymentMethod === "sinpe" ? "SINPE Móvil" : "Tarjeta Débito / Crédito"}
              </p>
              <h2 className="mt-1 text-xl font-black text-sand-900">Activar {label}</h2>
            </div>
            {step !== "reporting" && (
              <button
                type="button"
                onClick={onClose}
                aria-label="Cerrar"
                className="rounded-xl p-2 text-sand-400 hover:bg-sand-100 hover:text-sand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                <svg viewBox="0 0 16 16" fill="currentColor" className="h-4 w-4" aria-hidden="true">
                  <path d="M3.72 3.72a.75.75 0 0 1 1.06 0L8 6.94l3.22-3.22a.75.75 0 1 1 1.06 1.06L9.06 8l3.22 3.22a.75.75 0 1 1-1.06 1.06L8 9.06l-3.22 3.22a.75.75 0 0 1-1.06-1.06L6.94 8 3.72 4.78a.75.75 0 0 1 0-1.06Z" />
                </svg>
              </button>
            )}
          </div>

          {/* Step: confirm */}
          {step === "confirm" && (
            <div className="space-y-4">
              {/* Payment Method Selector */}
              <div className="grid grid-cols-2 gap-1 rounded-2xl bg-surface-warm p-1 border border-sand-200">
                <button
                  type="button"
                  onClick={() => {
                    setPaymentMethod("sinpe");
                    tap();
                  }}
                  className={`rounded-xl py-2 text-xs font-bold transition-all flex items-center justify-center gap-1.5 ${
                    paymentMethod === "sinpe"
                      ? "bg-surface text-brand-900 shadow-sm border border-sand-200"
                      : "text-sand-600 hover:text-sand-900"
                  }`}
                >
                  <span>📲</span> SINPE Móvil
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setPaymentMethod("card");
                    tap();
                  }}
                  className={`rounded-xl py-2 text-xs font-bold transition-all flex items-center justify-center gap-1.5 ${
                    paymentMethod === "card"
                      ? "bg-surface text-brand-900 shadow-sm border border-sand-200"
                      : "text-sand-600 hover:text-sand-900"
                  }`}
                >
                  <span>💳</span> Tarjeta (Inmediato)
                </button>
              </div>

              <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4">
                {isUserPlan && (
                  <fieldset className="mb-3">
                    <legend className="mb-2 text-sm font-semibold text-brand-900">Duración del plan</legend>
                    <div className="grid grid-cols-4 gap-2">
                      {USER_BILLING_TERMS.map((months) => (
                        <button
                          key={months}
                          type="button"
                          onClick={() => setBillingMonths(months)}
                          aria-pressed={billingMonths === months}
                          className={`rounded-xl border px-2 py-2 text-xs font-bold transition-colors ${
                            billingMonths === months
                              ? "border-brand-600 bg-brand-600 text-white"
                              : "border-brand-200 bg-white text-brand-700 hover:border-brand-400"
                          }`}
                        >
                          {months === 12 ? "1 año" : `${months} mes${months === 1 ? "" : "es"}`}
                          {months === 12 && <span className="mt-0.5 block text-[10px] font-medium">-20%</span>}
                        </button>
                      ))}
                    </div>
                  </fieldset>
                )}
                <p className="text-sm text-brand-800">
                  Activarás el plan <strong>{label}</strong> por {pricePeriod}.
                </p>
                <p className="mt-2 text-xs text-brand-600">
                  {paymentMethod === "sinpe"
                    ? "El pago se realiza vía SINPE Móvil. Se generará un código de referencia único para tu transferencia."
                    : "Activación inmediata con tarjeta de débito o crédito respaldada por CyberSource / Visa."}
                </p>
              </div>

              {/* Opción de Factura Electrónica */}
              <div className="rounded-2xl border border-sand-200 bg-surface-warm p-3.5 text-xs text-sand-700">
                <label className="flex items-start gap-2.5 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={requiresInvoice}
                    onChange={(e) => {
                      setRequiresInvoice(e.target.checked);
                      tap();
                    }}
                    className="mt-0.5 h-4 w-4 rounded border-sand-300 text-brand-600 focus:ring-brand-500"
                  />
                  <div className="flex-1">
                    <span className="font-bold text-sand-900 block">Deseo Factura Electrónica (+13% IVA)</span>
                    <span className="text-sand-600 text-[11px] block mt-0.5 leading-relaxed">
                      Los precios de los servicios son base y no reflejan el 13% de IVA. Si requieres factura con
                      crédito fiscal para deducción tributaria ante Hacienda, se adiciona el 13% de IVA al valor del
                      servicio.
                    </span>
                  </div>
                </label>
              </div>

              {/* Desglose de Precios */}
              <div className="rounded-2xl border border-sand-200 bg-surface p-3.5 space-y-1.5 text-xs">
                <div className="flex justify-between text-sand-600">
                  <span>Costo base del servicio:</span>
                  <span className="font-medium">₡{basePrice.toLocaleString("es-CR")}</span>
                </div>
                {requiresInvoice && (
                  <div className="flex justify-between text-brand-700">
                    <span>IVA (13%):</span>
                    <span className="font-semibold">+₡{ivaAmount.toLocaleString("es-CR")}</span>
                  </div>
                )}
                <div className="flex justify-between text-sand-900 font-bold border-t border-sand-200 pt-1.5 text-sm">
                  <span>Total a pagar:</span>
                  <span className="text-brand-600">₡{totalPrice.toLocaleString("es-CR")}</span>
                </div>
              </div>

              {error && <p className="text-xs text-danger-600">{error}</p>}

              {paymentMethod === "sinpe" ? (
                <button
                  type="button"
                  onClick={() => void handleStartPayment()}
                  disabled={isCreating}
                  className="w-full rounded-2xl bg-brand-600 py-3 text-sm font-bold text-white transition-colors hover:bg-brand-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  {isCreating ? "Generando código…" : `Continuar con SINPE (₡${totalPrice.toLocaleString("es-CR")}) →`}
                </button>
              ) : (
                <SecureCardPaymentForm
                  amountCrc={totalPrice}
                  isProcessing={isCharging || isCreating}
                  onPay={handleCardPayment}
                  buttonLabel={`Pagar ₡${totalPrice.toLocaleString("es-CR")} y Activar Ahora`}
                />
              )}
            </div>
          )}

          {/* Step: payment instructions */}
          {step === "payment" && reference && (
            <div className="space-y-4">
              {/* View Switcher: Instrucciones vs QR */}
              <div className="grid grid-cols-2 gap-1 rounded-xl bg-surface-warm p-1">
                <button
                  type="button"
                  onClick={() => setPaymentView("instructions")}
                  className={`rounded-lg py-1.5 text-xs font-bold transition-colors ${
                    paymentView === "instructions"
                      ? "bg-surface text-sand-900 shadow-sm"
                      : "text-sand-500 hover:text-sand-700"
                  }`}
                >
                  📝 Datos de transferencia
                </button>
                <button
                  type="button"
                  onClick={() => setPaymentView("qr")}
                  className={`rounded-lg py-1.5 text-xs font-bold transition-colors ${
                    paymentView === "qr" ? "bg-surface text-sand-900 shadow-sm" : "text-sand-500 hover:text-sand-700"
                  }`}
                >
                  📲 Escanear QR
                </button>
              </div>

              {paymentView === "instructions" ? (
                <>
                  {/* Reference & Amount Card */}
                  <div className="rounded-2xl bg-surface-warm p-4 text-center space-y-2 border border-sand-200">
                    <p className="text-xs text-sand-500 font-medium">Código de referencia único</p>
                    <div className="flex items-center justify-center gap-2">
                      <span className="font-mono text-2xl font-black tracking-[0.2em] text-sand-900">{reference}</span>
                      <button
                        type="button"
                        onClick={() => handleCopy(reference, "ref")}
                        aria-label="Copiar referencia"
                        className="rounded-lg bg-surface px-2.5 py-1 text-xs font-bold text-brand-600 border border-sand-200 hover:bg-sand-50 shadow-xs"
                      >
                        {copiedField === "ref" ? "✓ Copiado" : "Copiar"}
                      </button>
                    </div>

                    <div className="grid grid-cols-2 gap-2 pt-2 border-t border-sand-200/80 text-left">
                      <div className="rounded-xl bg-surface p-2.5 border border-sand-100">
                        <span className="block text-[10px] uppercase font-bold text-sand-400">Monto a transferir</span>
                        <div className="flex items-center justify-between mt-0.5">
                          <span className="text-sm font-black text-sand-900">₡{price.toLocaleString("es-CR")}</span>
                          <button
                            type="button"
                            onClick={() => handleCopy(formattedAmount.toString(), "amount")}
                            aria-label="Copiar monto"
                            className="text-[11px] font-bold text-brand-600 hover:underline"
                          >
                            {copiedField === "amount" ? "✓" : "Copiar"}
                          </button>
                        </div>
                      </div>

                      <div className="rounded-xl bg-surface p-2.5 border border-sand-100">
                        <span className="block text-[10px] uppercase font-bold text-sand-400">Teléfono SINPE</span>
                        <div className="flex items-center justify-between mt-0.5">
                          <span className="text-sm font-black text-sand-900">{SINPE_NUMBER}</span>
                          <button
                            type="button"
                            onClick={() => handleCopy(cleanPhone, "phone")}
                            aria-label="Copiar teléfono"
                            className="text-[11px] font-bold text-brand-600 hover:underline"
                          >
                            {copiedField === "phone" ? "✓" : "Copiar"}
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* SMS Quick Actions (Deep Links) */}
                  <div className="rounded-2xl border border-trust-200 bg-trust-50/70 p-3.5 space-y-2">
                    <div className="flex items-center justify-between">
                      <p className="text-xs font-bold text-trust-900 flex items-center gap-1.5">
                        <span>📲</span> Envío rápido por SMS
                      </p>
                      <button
                        type="button"
                        onClick={() => handleCopy(smsBody, "sms")}
                        className="text-[11px] font-semibold text-trust-700 hover:underline"
                      >
                        {copiedField === "sms" ? "✓ Comando copiado" : "Copiar comando"}
                      </button>
                    </div>
                    <p className="text-[11px] text-trust-700">
                      Toca tu banco para abrir la app de mensajes con los datos prellenados:
                    </p>
                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-1.5 pt-1">
                      {SINPE_BANKS.map((b) => (
                        <a
                          key={b.id}
                          href={getSmsUrl(b.number)}
                          className="rounded-xl bg-surface border border-trust-200 py-2 px-1 text-center text-[11px] font-bold text-trust-800 hover:bg-trust-100 shadow-2xs transition-colors"
                        >
                          {b.name}
                        </a>
                      ))}
                    </div>
                  </div>

                  {/* Standard instructions */}
                  <ol className="space-y-2.5 text-xs text-sand-700 px-1">
                    <li className="flex gap-2">
                      <span className="flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-sand-200 text-[10px] font-bold text-sand-700">
                        1
                      </span>
                      <span>
                        Transfiere desde tu banca móvil a <strong>{SINPE_NUMBER}</strong> por{" "}
                        <strong>₡{price.toLocaleString("es-CR")}</strong>.
                      </span>
                    </li>
                    <li className="flex gap-2">
                      <span className="flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-sand-200 text-[10px] font-bold text-sand-700">
                        2
                      </span>
                      <span>
                        Coloca exactamente <strong>{reference}</strong> en el detalle o motivo de la transferencia.
                      </span>
                    </li>
                  </ol>
                </>
              ) : (
                /* QR view for desktop / camera scanning */
                <div className="rounded-2xl border border-sand-200 bg-surface-warm p-4 text-center space-y-3">
                  <p className="text-xs font-semibold text-sand-800">Escanea desde la app de tu banco</p>
                  <div className="flex justify-center py-2">
                    {loadingQr ? (
                      <div className="flex h-48 w-48 items-center justify-center rounded-2xl bg-surface border border-sand-200">
                        <div className="h-8 w-8 rounded-full border-3 border-brand-200 border-t-brand-600 animate-spin" />
                      </div>
                    ) : qrBlobUrl ? (
                      <img
                        src={qrBlobUrl}
                        alt="Código QR para pago SINPE"
                        className="h-48 w-48 rounded-2xl bg-white p-2 border border-sand-200 shadow-sm"
                      />
                    ) : (
                      <div className="flex h-48 w-48 flex-col items-center justify-center rounded-2xl bg-surface border border-dashed border-sand-300 p-4 text-sand-400">
                        <span className="text-2xl mb-1">📲</span>
                        <p className="text-[11px]">Código QR disponible para escanear</p>
                      </div>
                    )}
                  </div>
                  <p className="text-[11px] text-sand-500">
                    Escanea este código con tu teléfono para abrir la plantilla con monto y referencia.
                  </p>
                </div>
              )}

              {/* Bank Receipt Number Input */}
              <div className="rounded-2xl bg-surface border border-sand-200 p-3.5 space-y-1.5 text-left">
                <label htmlFor="bank-receipt-input" className="block text-xs font-semibold text-sand-800">
                  Nº de comprobante o autorización (opcional)
                </label>
                <input
                  id="bank-receipt-input"
                  type="text"
                  value={receiptNumber}
                  onChange={(e) => setReceiptNumber(e.target.value)}
                  placeholder="Ej: 84920134 o código bancario"
                  maxLength={64}
                  className="w-full rounded-xl border border-sand-200 bg-surface-warm px-3 py-2 text-xs text-sand-900 outline-none focus:border-brand-500 focus:ring-1 focus:ring-brand-500 transition-colors"
                />
                <p className="text-[10px] text-sand-400">Ayuda a nuestro equipo a validar tu pago en pocos minutos.</p>
              </div>

              {error && <p className="text-xs text-danger-600">{error}</p>}
              <button
                type="button"
                onClick={() => void handleReportPayment()}
                disabled={isReporting}
                className="w-full rounded-2xl bg-rescue-600 py-3 text-sm font-bold text-white transition-colors hover:bg-rescue-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400 shadow-sm"
              >
                {isReporting ? "Registrando…" : "✓ Ya realicé el pago SINPE"}
              </button>
              <p className="text-center text-[11px] text-sand-400">
                Activación manual verificada por administración en horario hábil.
              </p>
            </div>
          )}

          {/* Step: reporting */}
          {step === "reporting" && (
            <div className="flex flex-col items-center gap-4 py-8">
              <div className="h-10 w-10 rounded-full border-4 border-brand-200 border-t-brand-500 animate-spin" />
              <p className="text-sm font-semibold text-sand-700">Registrando tu comprobante y aviso…</p>
            </div>
          )}

          {/* Step: reported */}
          {step === "reported" && (
            <div className="flex flex-col items-center gap-4 py-4 text-center">
              <div className="flex h-14 w-14 items-center justify-center rounded-full bg-trust-100 text-3xl">🕐</div>
              <h3 className="text-lg font-black text-sand-900">¡Aviso recibido con éxito!</h3>
              <p className="text-sm text-sand-600">
                Registramos que realizaste el pago SINPE
                {receiptNumber ? ` (comprobante #${receiptNumber})` : ""}. Activaremos tu plan <strong>{label}</strong>{" "}
                en cuanto sea verificado por administración.
              </p>
              <p className="text-xs text-sand-400">
                Te notificaremos automáticamente por correo y en la app cuando esté activo.
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

          {/* Step: card_success (Immediate Activation) */}
          {step === "card_success" && (
            <div className="flex flex-col items-center gap-4 py-4 text-center">
              <div className="flex h-16 w-16 items-center justify-center rounded-full bg-rescue-100 text-4xl">🎉</div>
              <div>
                <h3 className="text-xl font-black text-sand-900">¡Plan Activado Inmediatamente!</h3>
                <p className="mt-1 text-sm text-sand-600">
                  Tu pago con tarjeta fue aprobado y tu suscripción <strong>{label}</strong> ya se encuentra activa.
                </p>
              </div>

              {cardAuthCode && (
                <div className="w-full rounded-2xl bg-surface-warm p-3 border border-sand-200 text-xs text-sand-600 font-mono">
                  Autorización bancaria: <strong className="text-sand-900">{cardAuthCode}</strong>
                </div>
              )}

              <p className="text-xs text-sand-400">
                Enviamos tu comprobante de pago electrónico a tu correo registrado.
              </p>

              <button
                type="button"
                onClick={onClose}
                className="mt-2 w-full rounded-2xl bg-brand-600 py-3 text-sm font-bold text-white hover:bg-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 shadow-sm"
              >
                Comenzar a disfrutar de {label} →
              </button>
            </div>
          )}
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}
