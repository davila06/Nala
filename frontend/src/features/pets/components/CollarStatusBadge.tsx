interface CollarStatusBadgeProps {
  isActive: boolean;
  isOffline: boolean;
  batteryPercent: number | null;
  batteryAlertThresholdPercent: number;
}

/**
 * Compact status pill for a collar: Active / Offline / Low battery.
 * Offline takes priority over low battery when both conditions are true.
 */
export function CollarStatusBadge({
  isActive,
  isOffline,
  batteryPercent,
  batteryAlertThresholdPercent,
}: CollarStatusBadgeProps) {
  if (!isActive) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-sand-100 px-2.5 py-1 text-xs font-semibold text-sand-500">
        <span
          className="h-1.5 w-1.5 rounded-full bg-sand-400"
          aria-hidden="true"
        />
        Inactivo
      </span>
    );
  }

  if (isOffline) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2.5 py-1 text-xs font-semibold text-red-700">
        <span
          className="h-1.5 w-1.5 rounded-full bg-red-500"
          aria-hidden="true"
        />
        Sin conexión
      </span>
    );
  }

  const isLowBattery =
    batteryPercent !== null && batteryPercent <= batteryAlertThresholdPercent;

  if (isLowBattery) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-800">
        <span
          className="h-1.5 w-1.5 rounded-full bg-amber-500"
          aria-hidden="true"
        />
        Batería baja ({batteryPercent}%)
      </span>
    );
  }

  return (
    <span className="inline-flex items-center gap-1 rounded-full bg-green-50 px-2.5 py-1 text-xs font-semibold text-green-700">
      <span
        className="h-1.5 w-1.5 rounded-full bg-green-500"
        aria-hidden="true"
      />
      Activo
    </span>
  );
}
