import type { NotificationItem } from '../api/notificationsApi'
import { useMarkNotificationRead, useRespondResolveCheck } from '../hooks/useNotifications'

const TYPE_ICONS: Record<string, string> = {
  LostPetAlert: '🚨',
  PetReunited: '🎉',
  SightingAlert: '📍',
  SystemMessage: '📣',
  CustodyStarted: '🏠',
  CustodyClosed: '✅',
}

interface NotificationItemProps {
  notification: NotificationItem
}

export function NotificationItemCard({ notification }: NotificationItemProps) {
  const { mutate: markRead } = useMarkNotificationRead()
  const { mutate: respondResolveCheck, isPending: respondingResolveCheck } = useRespondResolveCheck()

  const handleClick = () => {
    if (!notification.isRead) {
      markRead(notification.id)
    }
  }

  const handleResolveResponse = (foundAtHome: boolean) => {
    respondResolveCheck({ id: notification.id, foundAtHome })
  }

  if (notification.type === 'ResolveCheck') {
    return (
      <div
        className={`w-full rounded-xl px-4 py-3 ${
          notification.isRead ? 'opacity-70' : 'bg-brand-50'
        }`}
      >
        <div className="flex items-start gap-3">
          <span className="mt-0.5 text-xl" aria-hidden="true">🐾</span>
          <div className="flex-1 overflow-hidden">
            <p className="truncate text-sm font-bold text-sand-900">{notification.title}</p>
            <p className="mt-0.5 text-xs text-sand-500">{notification.body}</p>
            <p className="mt-1 text-xs text-sand-400">
              {new Date(notification.createdAt).toLocaleString('es-CR', {
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
              })}
            </p>

            {!notification.isRead && (
              <div className="mt-3 flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={() => handleResolveResponse(true)}
                  disabled={respondingResolveCheck}
                  className="rounded-full bg-rescue-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-rescue-700 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400 focus-visible:ring-offset-1"
                >
                  Sí, ya está en casa
                </button>
                <button
                  type="button"
                  onClick={() => handleResolveResponse(false)}
                  disabled={respondingResolveCheck}
                  className="rounded-full border border-sand-300 bg-white px-3 py-1.5 text-xs font-semibold text-sand-700 hover:bg-sand-50 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sand-400 focus-visible:ring-offset-1"
                >
                  No, sigue perdido
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    )
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      className={`w-full rounded-xl px-4 py-3 text-left transition-colors hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-inset ${
        notification.isRead ? 'opacity-60' : 'bg-brand-50'
      }`}
    >
      <div className="flex items-start gap-3">
        <span className="mt-0.5 text-xl" aria-hidden="true">
          {TYPE_ICONS[notification.type] ?? '🔔'}
        </span>
        <div className="flex-1 overflow-hidden">
          <div className="flex items-center justify-between gap-2">
            <p className={`truncate text-sm ${notification.isRead ? 'font-medium' : 'font-bold'} text-sand-900`}>
              {notification.title}
            </p>
            {!notification.isRead && (
              <span className="h-2 w-2 shrink-0 rounded-full bg-brand-500" aria-label="No leída" />
            )}
          </div>
          <p className="mt-0.5 line-clamp-2 text-xs text-sand-500">{notification.body}</p>
          <p className="mt-1 text-xs text-sand-400">
            {new Date(notification.createdAt).toLocaleString('es-CR', {
              month: 'short',
              day: 'numeric',
              hour: '2-digit',
              minute: '2-digit',
            })}
          </p>
        </div>
      </div>
    </button>
  )
}

