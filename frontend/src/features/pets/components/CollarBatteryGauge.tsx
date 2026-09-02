interface CollarBatteryGaugeProps {
  batteryPercent: number | null;
  thresholdPercent: number;
}

/**
 * Simple current-battery gauge. Full historical charting requires the
 * battery-history endpoint (planned — see docs/COLLAR_IMPLEMENTATION_PLAN.md §Semana 2).
 */
export function CollarBatteryGauge({
  batteryPercent,
  thresholdPercent,
}: CollarBatteryGaugeProps) {
  if (batteryPercent === null) {
    return <div className="text-xs text-sand-400">Sin datos de batería</div>;
  }

  const isLow = batteryPercent <= thresholdPercent;
  const barColor = isLow
    ? "bg-red-500"
    : batteryPercent <= 50
      ? "bg-amber-500"
      : "bg-green-500";

  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between text-xs text-sand-600">
        <span>Batería</span>
        <span
          className={
            isLow ? "font-bold text-red-700" : "font-semibold text-sand-800"
          }
        >
          {batteryPercent}%
        </span>
      </div>
      <div className="h-2 w-full overflow-hidden rounded-full bg-sand-100">
        <div
          className={`h-full rounded-full transition-all ${barColor}`}
          style={{ width: `${Math.max(0, Math.min(100, batteryPercent))}%` }}
        />
      </div>
    </div>
  );
}
