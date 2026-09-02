import { useState } from "react";
import { useUpdateCollarNotificationPreferences } from "../hooks/useCollar";

interface CollarNotificationPreferencesPanelProps {
  petId: string;
  collarId: string;
  offlineAlertsEnabled: boolean;
  offlineThresholdMinutes: number;
  batteryAlertsEnabled: boolean;
  batteryAlertThresholdPercent: number;
}

/** Settings panel for collar offline/battery alert preferences. */
export function CollarNotificationPreferencesPanel({
  petId,
  collarId,
  offlineAlertsEnabled,
  offlineThresholdMinutes,
  batteryAlertsEnabled,
  batteryAlertThresholdPercent,
}: CollarNotificationPreferencesPanelProps) {
  const [offlineEnabled, setOfflineEnabled] = useState(offlineAlertsEnabled);
  const [offlineMinutes, setOfflineMinutes] = useState(offlineThresholdMinutes);
  const [batteryEnabled, setBatteryEnabled] = useState(batteryAlertsEnabled);
  const [batteryThreshold, setBatteryThreshold] = useState(
    batteryAlertThresholdPercent,
  );

  const update = useUpdateCollarNotificationPreferences(petId);

  const handleSave = () => {
    update.mutate({
      collarId,
      preferences: {
        offlineAlertsEnabled: offlineEnabled,
        offlineThresholdMinutes: offlineMinutes,
        batteryAlertsEnabled: batteryEnabled,
        batteryAlertThresholdPercent: batteryThreshold,
      },
    });
  };

  return (
    <div className="space-y-4 rounded-2xl border border-sand-200 bg-surface p-4">
      <p className="text-sm font-semibold text-sand-800">
        Notificaciones del collar
      </p>

      <label className="flex items-center justify-between gap-3 text-sm text-sand-700">
        <span>Avisarme si el collar se desconecta</span>
        <input
          type="checkbox"
          checked={offlineEnabled}
          onChange={(e) => setOfflineEnabled(e.target.checked)}
          className="h-4 w-4 rounded border-sand-300 text-brand-600 focus:ring-brand-400"
        />
      </label>
      {offlineEnabled && (
        <label className="block text-xs text-sand-600">
          Umbral de desconexión (minutos)
          <input
            type="number"
            min={15}
            max={1440}
            value={offlineMinutes}
            onChange={(e) => setOfflineMinutes(Number(e.target.value))}
            className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400"
          />
        </label>
      )}

      <label className="flex items-center justify-between gap-3 text-sm text-sand-700">
        <span>Avisarme si la batería está baja</span>
        <input
          type="checkbox"
          checked={batteryEnabled}
          onChange={(e) => setBatteryEnabled(e.target.checked)}
          className="h-4 w-4 rounded border-sand-300 text-brand-600 focus:ring-brand-400"
        />
      </label>
      {batteryEnabled && (
        <label className="block text-xs text-sand-600">
          Umbral de batería (%)
          <input
            type="number"
            min={5}
            max={50}
            value={batteryThreshold}
            onChange={(e) => setBatteryThreshold(Number(e.target.value))}
            className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400"
          />
        </label>
      )}

      {update.isError && (
        <p className="rounded-xl bg-red-50 px-3 py-2 text-xs text-red-700">
          {String(update.error)}
        </p>
      )}
      {update.isSuccess && (
        <p className="rounded-xl bg-green-50 px-3 py-2 text-xs text-green-700">
          Preferencias guardadas.
        </p>
      )}

      <button
        type="button"
        disabled={update.isPending}
        onClick={handleSave}
        className="w-full rounded-xl bg-brand-600 px-4 py-2 text-xs font-bold text-white disabled:opacity-40 hover:bg-brand-700 transition-colors"
      >
        {update.isPending ? "Guardando…" : "Guardar preferencias"}
      </button>
    </div>
  );
}
