import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
} from '../hooks/useNotificationPreferences'

export function NotificationPreferencesToggle() {
  const { data, isLoading } = useNotificationPreferences()
  const { mutate: update, isPending } = useUpdateNotificationPreferences()

  const enabled = data?.enablePreventiveAlerts ?? true
  const busy = isLoading || isPending

  function handleToggle() {
    update(!enabled)
  }

  return (
    <div className="mt-6 rounded-2xl border border-sand-200 bg-white px-4 py-4">
      <div className="flex items-center justify-between gap-4">
        <div className="min-w-0">
          <p className="text-sm font-semibold text-sand-900">
            Alertas preventivas de riesgo
          </p>
          <p className="mt-0.5 truncate text-xs text-sand-500">
            Tope, Año Nuevo, temporada lluviosa…
          </p>
        </div>

        {/* Toggle switch */}
        <button
          type="button"
          role="switch"
          aria-checked={enabled}
          aria-label="Activar o desactivar alertas preventivas"
          disabled={busy}
          onClick={handleToggle}
          className={[
            'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent',
            'transition-colors duration-200 ease-in-out focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2',
            'disabled:opacity-50',
            enabled ? 'bg-brand-500' : 'bg-sand-300',
          ].join(' ')}
        >
          <span
            aria-hidden="true"
            className={[
              'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0',
              'transition duration-200 ease-in-out',
              enabled ? 'translate-x-5' : 'translate-x-0',
            ].join(' ')}
          />
        </button>
      </div>
    </div>
  )
}

