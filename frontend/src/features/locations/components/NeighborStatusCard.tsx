import { useState } from "react";
import { toast } from "@/shared/lib/toast";
import {
  useNeighborStatus,
  useUpdateNeighborSettings,
} from "../hooks/useNeighbor";
import { NeighborNetworkSetup } from "./NeighborNetworkSetup";
import { Skeleton } from "@/shared/ui/Spinner";

export function NeighborStatusCard() {
  const { data: status, isLoading } = useNeighborStatus();
  const updateSettings = useUpdateNeighborSettings();
  const [setupOpen, setSetupOpen] = useState(false);

  if (isLoading) return <Skeleton className="h-24 rounded-2xl" />;

  if (!status?.isEnrolled) {
    return (
      <>
        <button
          type="button"
          onClick={() => setSetupOpen(true)}
          className="flex w-full items-start gap-3 rounded-2xl border border-sand-200 bg-surface p-4 text-left transition-colors hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          <span
            className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-trust-100 text-xl"
            aria-hidden="true"
          >
            🏘️
          </span>
          <div>
            <p className="text-sm font-semibold text-sand-900">
              Activar Guardia Vecinal
            </p>
            <p className="text-xs text-sand-500 mt-0.5">
              Recibe alertas cuando una mascota se pierde en tu cuadra
            </p>
          </div>
          <svg
            viewBox="0 0 16 16"
            fill="currentColor"
            className="ml-auto h-4 w-4 shrink-0 text-sand-300 mt-1"
            aria-hidden="true"
          >
            <path
              fillRule="evenodd"
              d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z"
              clipRule="evenodd"
            />
          </svg>
        </button>
        <NeighborNetworkSetup
          isOpen={setupOpen}
          onClose={() => setSetupOpen(false)}
        />
      </>
    );
  }

  const toggleActive = () => {
    updateSettings.mutate(
      { radiusMeters: status.radiusMeters, isActive: !status.isActive },
      {
        onSuccess: () =>
          toast.success(
            status.isActive
              ? "Guardia Vecinal desactivada"
              : "Guardia Vecinal activada",
          ),
        onError: () => toast.error("No se pudo actualizar"),
      },
    );
  };

  return (
    <div className="rounded-2xl border border-trust-200 bg-trust-50 p-4 space-y-3">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2.5">
          <span
            className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-trust-100 text-lg"
            aria-hidden="true"
          >
            🏘️
          </span>
          <div>
            <p className="text-sm font-semibold text-trust-900">
              Guardia Vecinal
            </p>
            <p className="text-xs text-trust-600">
              {status.neighborsInRange > 0
                ? `${status.neighborsInRange} vecino${status.neighborsInRange !== 1 ? "s" : ""} activo${status.neighborsInRange !== 1 ? "s" : ""} en tu radio`
                : `Radio: ${status.radiusMeters} m`}
            </p>
          </div>
        </div>

        {/* Toggle */}
        <button
          type="button"
          role="switch"
          aria-checked={status.isActive}
          aria-label={
            status.isActive
              ? "Desactivar Guardia Vecinal"
              : "Activar Guardia Vecinal"
          }
          disabled={updateSettings.isPending}
          onClick={toggleActive}
          className={[
            "relative inline-flex h-6 w-11 shrink-0 rounded-full border-2 border-transparent transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400 disabled:opacity-50",
            status.isActive ? "bg-trust-600" : "bg-sand-300",
          ].join(" ")}
        >
          <span
            className={[
              "pointer-events-none inline-block h-5 w-5 rounded-full bg-white shadow transition-transform",
              status.isActive ? "translate-x-5" : "translate-x-0",
            ].join(" ")}
          />
        </button>
      </div>

      {status.phone && (
        <p className="text-xs text-trust-700 flex items-center gap-1.5">
          <span aria-hidden="true">📱</span>
          {status.phone}
          <span className="rounded-full bg-trust-200 px-1.5 py-0.5 text-[10px] font-semibold text-trust-800">
            Registrado
          </span>
        </p>
      )}
    </div>
  );
}
