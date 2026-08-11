import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import { Button, Input } from "@/shared/ui";
import {
  useValidatePromotion,
  useRedeemPromotion,
} from "../hooks/usePromotion";
import type { PromotionValidationDto } from "../api/promotionApi";

const TIER_LABELS: Record<string, string> = {
  UserPlus: "Plan Plus",
  UserFamilia: "Plan Familia",
};

export function PromotionCodeRedeemer() {
  const [code, setCode] = useState("");
  const [validation, setValidation] = useState<PromotionValidationDto | null>(
    null,
  );
  const [selectedTier, setSelectedTier] = useState<string>("");

  const validate = useValidatePromotion();
  const redeem = useRedeemPromotion();

  const handleValidate = () => {
    if (!code.trim()) return;
    validate.mutate(code.trim().toUpperCase(), {
      onSuccess: (data) => {
        setValidation(data);
        if (data.targetTier) setSelectedTier(data.targetTier);
      },
      onError: () => {
        setValidation(null);
        toast.error("Código no válido o expirado");
      },
    });
  };

  const handleRedeem = () => {
    const tier = validation?.targetTier || selectedTier || undefined;
    redeem.mutate(
      { code: code.trim().toUpperCase(), selectedTier: tier },
      {
        onSuccess: () => {
          toast.success("¡Código aplicado! Tu suscripción fue activada.");
          setCode("");
          setValidation(null);
        },
        onError: (err: unknown) => {
          const msg =
            (err as { response?: { data?: { detail?: string } } })?.response
              ?.data?.detail ?? "No se pudo aplicar el código";
          toast.error(msg);
        },
      },
    );
  };

  return (
    <div className="rounded-2xl border border-sand-200 bg-sand-50 p-5 space-y-3">
      <h3 className="text-sm font-semibold text-sand-800">
        🎁 Tengo un código de promoción
      </h3>

      <div className="flex gap-2">
        <Input
          value={code}
          onChange={(e) => {
            setCode(e.target.value.toUpperCase());
            setValidation(null);
          }}
          placeholder="Ej: FREEPL4B"
          maxLength={8}
          className="flex-1 font-mono tracking-widest"
        />
        <Button
          onClick={handleValidate}
          loading={validate.isPending}
          disabled={code.trim().length < 6}
          variant="secondary"
          size="sm"
        >
          Validar
        </Button>
      </div>

      {/* Validation result */}
      {validation && (
        <div className="rounded-xl border border-green-200 bg-green-50 p-4 space-y-3">
          <div className="flex items-start gap-2">
            <span className="text-green-600 text-lg">✅</span>
            <div>
              <p className="text-sm font-semibold text-green-800">
                Código válido
              </p>
              <p className="text-sm text-green-700">
                {validation.benefitDescription}
              </p>
            </div>
          </div>

          {/* Tier selector only needed when discount has no fixed tier */}
          {validation.type === "PercentageDiscount" &&
            !validation.targetTier && (
              <div>
                <label className="mb-1 block text-xs font-medium text-sand-600">
                  Seleccioná el plan al que aplicar el descuento
                </label>
                <select
                  value={selectedTier}
                  onChange={(e) => setSelectedTier(e.target.value)}
                  className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
                >
                  <option value="">— Elegir plan —</option>
                  <option value="UserPlus">Plan Plus (₡2,990/mes)</option>
                  <option value="UserFamilia">Plan Familia (₡4,990/mes)</option>
                </select>
              </div>
            )}

          {validation.targetTier && (
            <p className="text-xs text-sand-500">
              Plan:{" "}
              <strong>
                {TIER_LABELS[validation.targetTier] ?? validation.targetTier}
              </strong>
              {!validation.requiresPayment &&
                " — sin costo, activación inmediata"}
            </p>
          )}

          <Button
            onClick={handleRedeem}
            loading={redeem.isPending}
            disabled={
              validation.type === "PercentageDiscount" &&
              !validation.targetTier &&
              !selectedTier
            }
            className="w-full"
          >
            {validation.requiresPayment
              ? `Aplicar descuento (irás a pagar con SINPE)`
              : `Activar ahora — sin costo`}
          </Button>
        </div>
      )}
    </div>
  );
}
