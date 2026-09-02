import { useState } from "react";
import {
  useCancelCollarHandoverCode,
  useGenerateCollarHandoverCode,
} from "../hooks/useCollar";

interface CollarHandoverDialogProps {
  collarId: string;
  onClose: () => void;
}

/** Generates a one-time PIN to transfer an activated collar to a new owner. */
export function CollarHandoverDialog({
  collarId,
  onClose,
}: CollarHandoverDialogProps) {
  const [handoverCodeId, setHandoverCodeId] = useState<string | null>(null);
  const [pin, setPin] = useState<string | null>(null);
  const [pinCopied, setPinCopied] = useState(false);

  const generate = useGenerateCollarHandoverCode();
  const cancel = useCancelCollarHandoverCode();

  const handleGenerate = () => {
    generate.mutate(collarId, {
      onSuccess: (data) => {
        setHandoverCodeId(data.handoverCodeId);
        setPin(data.pin);
      },
    });
  };

  const handleCopyPin = () => {
    if (!pin) return;
    navigator.clipboard.writeText(pin);
    setPinCopied(true);
    setTimeout(() => setPinCopied(false), 2000);
  };

  const handleCancel = () => {
    if (!handoverCodeId) {
      onClose();
      return;
    }
    cancel.mutate(handoverCodeId, { onSettled: onClose });
  };

  return (
    <div className="space-y-4 rounded-2xl border border-brand-200 bg-surface p-4">
      <p className="text-sm font-semibold text-sand-800">
        Transferir collar a otro propietario
      </p>

      {!pin && (
        <>
          <p className="text-xs text-sand-500">
            Se generará un PIN de 6 dígitos válido por 7 días. Compártelo solo
            con la persona a la que le entregarás el collar físicamente.
          </p>
          {generate.isError && (
            <p className="rounded-xl bg-red-50 px-3 py-2 text-xs text-red-700">
              {String(generate.error)}
            </p>
          )}
          <div className="flex gap-2">
            <button
              type="button"
              disabled={generate.isPending}
              onClick={handleGenerate}
              className="rounded-xl bg-brand-600 px-4 py-2 text-xs font-bold text-white disabled:opacity-40 hover:bg-brand-700 transition-colors"
            >
              {generate.isPending
                ? "Generando…"
                : "Generar PIN de transferencia"}
            </button>
            <button
              type="button"
              onClick={onClose}
              className="text-xs text-sand-400 underline"
            >
              Cancelar
            </button>
          </div>
        </>
      )}

      {pin && (
        <>
          <div className="rounded-2xl border-2 border-amber-400 bg-amber-50 p-4 space-y-2">
            <p className="text-xs font-bold text-amber-800">
              ⚠️ Comparte este PIN solo con el nuevo propietario
            </p>
            <div className="flex items-center gap-2">
              <code className="flex-1 text-center rounded-lg bg-amber-100 px-3 py-2 text-lg font-mono font-bold tracking-widest text-amber-900">
                {pin}
              </code>
              <button
                type="button"
                onClick={handleCopyPin}
                className="shrink-0 rounded-lg bg-amber-200 px-3 py-2 text-xs font-bold text-amber-900 hover:bg-amber-300 transition-colors"
              >
                {pinCopied ? "✓" : "Copiar"}
              </button>
            </div>
            <p className="text-[10px] text-amber-700">
              Válido por 7 días. El nuevo propietario debe ingresarlo en{" "}
              <code className="font-mono">/collars/handover</code> para liberar
              el collar y reactivarlo.
            </p>
          </div>
          <button
            type="button"
            disabled={cancel.isPending}
            onClick={handleCancel}
            className="text-xs text-sand-500 underline hover:text-red-600 disabled:opacity-40"
          >
            {cancel.isPending ? "Cancelando…" : "Cancelar transferencia"}
          </button>
        </>
      )}
    </div>
  );
}
