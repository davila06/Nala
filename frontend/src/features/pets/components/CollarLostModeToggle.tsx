import { useState } from "react";
import {
  useActivateCollarLostMode,
  useCollarLostModeStatus,
  useDeactivateCollarLostMode,
} from "../hooks/useCollar";

interface CollarLostModeToggleProps {
  petId: string;
  collarId: string;
}

/** Toggle + status badge for GPS lost mode: faster tracking + auto-links a LostPetEvent. */
export function CollarLostModeToggle({
  petId,
  collarId,
}: CollarLostModeToggleProps) {
  const { data: status } = useCollarLostModeStatus(collarId);
  const activate = useActivateCollarLostMode(petId);
  const deactivate = useDeactivateCollarLostMode(petId);
  const [showConfirm, setShowConfirm] = useState(false);
  const [reason, setReason] = useState("");

  if (status?.isLost) {
    const activatedAt = status.lostModeActivatedAt
      ? new Date(status.lostModeActivatedAt)
      : null;

    return (
      <div className="space-y-2 rounded-2xl border-2 border-red-300 bg-red-50 p-4">
        <div className="flex items-center gap-2">
          <span
            className="h-2 w-2 animate-pulse rounded-full bg-red-500"
            aria-hidden="true"
          />
          <p className="text-sm font-bold text-red-800">Modo perdido activo</p>
        </div>
        {activatedAt && (
          <p className="text-xs text-red-600">
            Activado {activatedAt.toLocaleString("es-CR")} — el collar reporta
            con mayor frecuencia.
          </p>
        )}
        {!showConfirm ? (
          <button
            type="button"
            onClick={() => setShowConfirm(true)}
            className="text-xs font-semibold text-red-700 underline hover:text-red-900"
          >
            Desactivar modo perdido
          </button>
        ) : (
          <div className="space-y-2">
            <input
              type="text"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="¿La encontraste? (opcional)"
              className="w-full rounded-xl border border-red-200 bg-surface px-3 py-2 text-xs outline-none focus:border-red-400"
            />
            <div className="flex gap-2">
              <button
                type="button"
                disabled={deactivate.isPending}
                onClick={() =>
                  deactivate.mutate(
                    { collarId, reason: reason || undefined },
                    { onSettled: () => setShowConfirm(false) },
                  )
                }
                className="rounded-xl bg-red-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-red-700 disabled:opacity-50"
              >
                {deactivate.isPending ? "Desactivando…" : "Confirmar"}
              </button>
              <button
                type="button"
                onClick={() => setShowConfirm(false)}
                className="text-xs text-sand-500 underline"
              >
                Cancelar
              </button>
            </div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div>
      {activate.isError && (
        <p className="mb-2 rounded-xl bg-red-50 px-3 py-2 text-xs text-red-700">
          {String(activate.error)}
        </p>
      )}
      <button
        type="button"
        disabled={activate.isPending}
        onClick={() => activate.mutate(collarId)}
        className="rounded-xl bg-red-600 px-4 py-2 text-xs font-bold text-white disabled:opacity-40 hover:bg-red-700 transition-colors"
      >
        {activate.isPending
          ? "Activando…"
          : "🚨 Marcar mascota como perdida (modo GPS activo)"}
      </button>
    </div>
  );
}
