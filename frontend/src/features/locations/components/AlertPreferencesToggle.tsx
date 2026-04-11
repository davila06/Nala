import { useAlertPreference } from '../hooks/useAlertPreference'
import { QuietHoursForm } from './QuietHoursForm'

/**
 * Settings card that lets the authenticated user opt in or out of
 * geofenced lost-pet push alerts, and configure an optional quiet-hours window.
 */
export function AlertPreferencesToggle() {
  const { receiveNearbyAlerts, toggle, isSaving, quietHours, setQuietHours } =
    useAlertPreference()

  return (
    <div className="rounded-2xl border border-sand-200 bg-white p-4">
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="text-sm font-semibold text-sand-800">
            🐾 Alertas de mascotas perdidas cerca de mí
          </p>
          <p className="mt-0.5 text-xs text-sand-500">
            Recibirás una notificación cuando una mascota sea reportada como
            perdida en un radio de 1&thinsp;km de tu última ubicación registrada.
            Tu ubicación nunca es compartida públicamente.
          </p>
        </div>

        {/* Toggle switch */}
        <button
          type="button"
          role="switch"
          aria-checked={receiveNearbyAlerts}
          aria-label="Activar alertas de mascotas perdidas cerca de mí"
          disabled={isSaving}
          onClick={() => void toggle()}
          className={[
            'relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent',
            'transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2',
            'disabled:cursor-not-allowed disabled:opacity-50',
            receiveNearbyAlerts ? 'bg-brand-500' : 'bg-sand-300',
          ].join(' ')}
        >
          <span
            aria-hidden="true"
            className={[
              'pointer-events-none inline-block h-5 w-5 rounded-full bg-white shadow',
              'transform transition duration-200 ease-in-out',
              receiveNearbyAlerts ? 'translate-x-5' : 'translate-x-0',
            ].join(' ')}
          />
        </button>
      </div>

      {receiveNearbyAlerts && (
        <>
          <p className="mt-3 rounded-xl bg-brand-50 px-3 py-2 text-xs text-brand-700">
            ✅ Activo — tu ubicación se actualiza automáticamente en segundo plano
            mientras usas la app.
          </p>

          {/* Quiet hours configuration */}
          <QuietHoursForm
            value={quietHours}
            onChange={setQuietHours}
            disabled={isSaving}
          />
        </>
      )}
    </div>
  )
}

