import { useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMarkAllRead, useNotifications, useRespondResolveCheck } from '../hooks/useNotifications'
import { NotificationItemCard } from './NotificationItem'
import { NotificationPreferencesToggle } from './NotificationPreferencesToggle'
import { usePushSubscription } from '../hooks/usePushSubscription'
import { EmptyState } from '@/shared/ui/Card'

export function NotificationCenter() {
  const { data, isLoading } = useNotifications()
  const { mutate: markAll, isPending: markingAll } = useMarkAllRead()
  const { mutate: respondResolveCheck, isPending: isRespondingResolveCheck } = useRespondResolveCheck()
  const [searchParams, setSearchParams] = useSearchParams()
  const { status: pushStatus, subscribe: pushSubscribe } = usePushSubscription()

  const unreadCount = data?.totalCount ?? 0
  const resolveCheckNotificationId = searchParams.get('resolveCheckNotificationId')

  const closeResolveSheet = () => {
    const next = new URLSearchParams(searchParams)
    next.delete('resolveCheckNotificationId')
    setSearchParams(next)
  }

  const handleResolveSheetAction = (foundAtHome: boolean) => {
    if (!resolveCheckNotificationId) return

    respondResolveCheck(
      { id: resolveCheckNotificationId, foundAtHome },
      {
        onSuccess: () => closeResolveSheet(),
      },
    )
  }

  useEffect(() => {
    if (!resolveCheckNotificationId) return
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') closeResolveSheet()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [resolveCheckNotificationId])

  return (
    <div className="mx-auto max-w-lg px-4 py-6">
      <div className="mb-5 flex items-center justify-between">
        <h1 className="text-xl font-bold text-sand-900">
          Notificaciones
          {unreadCount > 0 && (
            <span
              aria-live="polite"
              aria-atomic="true"
              className="ml-2 rounded-full bg-brand-500 px-2 py-0.5 text-xs font-bold text-white"
            >
              {unreadCount} nuevas
            </span>
          )}
        </h1>
        {unreadCount > 0 && (
          <button
            type="button"
            onClick={() => markAll()}
            disabled={markingAll}
            className="-mx-2 -my-2 rounded px-2 py-2.5 text-xs font-semibold text-brand-600 hover:underline disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Marcar todo leído
          </button>
        )}
      </div>

      {isLoading && (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 animate-pulse rounded-xl bg-sand-100" />
          ))}
        </div>
      )}

      {!isLoading && (!data?.items.length) && (
        <EmptyState
          icon={<span className="text-4xl" aria-hidden="true">🔔</span>}
          title="No hay notificaciones aún"
          description="Aquí aparecerán alertas sobre tus mascotas."
        />
      )}

      {!isLoading && data?.items && data.items.length > 0 && (
        <ul role="list" className="list-none divide-y divide-sand-100 rounded-2xl border border-sand-200 bg-white p-0 m-0">
          {data.items.map((n) => (
            <li key={n.id}>
              <NotificationItemCard notification={n} />
            </li>
          ))}
        </ul>
      )}

      {resolveCheckNotificationId && (        <div
          className="fixed inset-0 z-50 flex items-end bg-black/40 p-4 sm:items-center sm:justify-center"
          onClick={closeResolveSheet}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="resolve-title"
            className="w-full max-w-md rounded-3xl bg-white p-5 shadow-2xl max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <p className="text-xs font-semibold uppercase tracking-wide text-sand-500">Auto-resolución</p>
            <h2 id="resolve-title" className="mt-1 text-lg font-extrabold text-sand-900">¿Encontraste a tu mascota?</h2>
            <p className="mt-2 text-sm text-sand-600">
              Detectamos actividad reciente compatible con recuperación. Confirma para mantener el mapa limpio.
            </p>

            <div className="mt-4 flex flex-col gap-2">
              <button
                type="button"
                onClick={() => handleResolveSheetAction(true)}
                disabled={isRespondingResolveCheck}
                className="rounded-2xl bg-rescue-600 px-4 py-2.5 text-sm font-bold text-white hover:bg-rescue-700 disabled:opacity-50"
              >
                Sí, ya está en casa
              </button>
              <button
                type="button"
                onClick={() => handleResolveSheetAction(false)}
                disabled={isRespondingResolveCheck}
                className="rounded-2xl border border-sand-300 bg-white px-4 py-2.5 text-sm font-semibold text-sand-700 hover:bg-sand-50 disabled:opacity-50"
              >
                No, sigue perdido
              </button>
              <button
                type="button"
                onClick={closeResolveSheet}
                className="mt-1 text-xs font-semibold text-sand-500 hover:text-sand-800"
              >
                Cerrar
              </button>
            </div>
          </div>
        </div>
      )}

      <NotificationPreferencesToggle />

      {/* Push notification opt-in */}
      {pushStatus !== 'unsupported' && pushStatus !== 'subscribed' && (
        <div className="mt-4 rounded-2xl border border-brand-200 bg-brand-50 p-4">
          <p className="text-sm font-semibold text-brand-800">Notificaciones push</p>
          <p className="mt-0.5 text-xs text-brand-700">
            Recibe alertas en tu dispositivo aunque no tengas la app abierta.
          </p>
          <button
            type="button"
            onClick={() => void pushSubscribe()}
            disabled={pushStatus === 'loading' || pushStatus === 'denied'}
            className="mt-3 rounded-xl bg-brand-500 px-4 py-2 text-xs font-semibold text-white hover:bg-brand-600 disabled:opacity-60"
          >
            {pushStatus === 'loading'
              ? 'Activando...'
              : pushStatus === 'denied'
              ? 'Permiso denegado'
              : 'Activar notificaciones'}
          </button>
        </div>
      )}
    </div>
  )
}

