import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { useMarkAllRead, useNotifications, useRespondResolveCheck } from '../hooks/useNotifications'
import { NotificationItemCard } from './NotificationItem'
import { NotificationPreferencesToggle } from './NotificationPreferencesToggle'
import { usePushSubscription } from '../hooks/usePushSubscription'
import { EmptyState } from '@/shared/ui/Card'
import type { NotificationItem, NotificationType } from '../api/notificationsApi'

type FilterTab = 'all' | 'chats' | 'alerts' | 'sightings'

const CHAT_TYPES: NotificationType[] = ['ChatMessage']
const ALERT_TYPES: NotificationType[] = ['LostPetAlert', 'FraudAlert', 'VerifiedAllyAlert', 'StaleReportReminder', 'ResolveCheck', 'CustodyStarted', 'CustodyClosed']
const SIGHTING_TYPES: NotificationType[] = ['SightingAlert', 'FoundPetMatch', 'PetReunited']

const FILTER_TABS: { key: FilterTab; label: string }[] = [
  { key: 'all', label: 'Todos' },
  { key: 'chats', label: '💬 Chats' },
  { key: 'alerts', label: '🚨 Alertas' },
  { key: 'sightings', label: '📍 Avistamientos' },
]

// ── Date label helper ─────────────────────────────────────────────────────────
function dateLabel(dateStr: string): string {
  const d = new Date(dateStr)
  const today = new Date()
  const yesterday = new Date(today)
  yesterday.setDate(today.getDate() - 1)

  if (d.toDateString() === today.toDateString()) return 'Hoy'
  if (d.toDateString() === yesterday.toDateString()) return 'Ayer'
  return d.toLocaleDateString('es-CR', { weekday: 'long', day: 'numeric', month: 'long' })
}

export function NotificationCenter() {
  const { data, isLoading } = useNotifications()
  const { mutate: markAll, isPending: markingAll } = useMarkAllRead()
  const { mutate: respondResolveCheck, isPending: isRespondingResolveCheck } = useRespondResolveCheck()
  const [searchParams, setSearchParams] = useSearchParams()
  const { status: pushStatus, subscribe: pushSubscribe } = usePushSubscription()

  const unreadCount = data?.totalCount ?? 0
  const resolveCheckNotificationId = searchParams.get('resolveCheckNotificationId')
  const [activeTab, setActiveTab] = useState<FilterTab>('all')

  const closeResolveSheet = () => {
    const next = new URLSearchParams(searchParams)
    next.delete('resolveCheckNotificationId')
    setSearchParams(next)
  }

  const handleResolveSheetAction = (foundAtHome: boolean) => {
    if (!resolveCheckNotificationId) return
    respondResolveCheck({ id: resolveCheckNotificationId, foundAtHome }, { onSuccess: () => closeResolveSheet() })
  }

  useEffect(() => {
    if (!resolveCheckNotificationId) return
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === 'Escape') closeResolveSheet() }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [resolveCheckNotificationId])

  const groups = useMemo(() => {
    if (!data?.items.length) return []
    const filtered = data.items.filter((item) => {
      if (activeTab === 'chats') return CHAT_TYPES.includes(item.type)
      if (activeTab === 'alerts') return ALERT_TYPES.includes(item.type)
      if (activeTab === 'sightings') return SIGHTING_TYPES.includes(item.type)
      return true
    })
    const map = new Map<string, NotificationItem[]>()
    for (const item of filtered) {
      const label = dateLabel(item.createdAt)
      const arr = map.get(label) ?? []
      arr.push(item)
      map.set(label, arr)
    }
    return Array.from(map.entries())
  }, [data?.items, activeTab])

  return (
    <div className="mx-auto max-w-lg px-4 py-6">
      <div className="mb-5 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <h1 className="text-xl font-bold text-sand-900">Notificaciones</h1>
          <AnimatePresence>
            {unreadCount > 0 && (
              <motion.span
                key={unreadCount}
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                exit={{ scale: 0 }}
                transition={{ type: 'spring', stiffness: 500, damping: 28 }}
                aria-live="polite"
                aria-atomic="true"
                className="rounded-full bg-brand-500 px-2 py-0.5 text-xs font-bold text-white"
              >
                {unreadCount} nuevas
              </motion.span>
            )}
          </AnimatePresence>
        </div>
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

      {/* Filter tabs */}
      <div className="mb-4 flex gap-2 overflow-x-auto pb-1" role="tablist" aria-label="Filtrar notificaciones">
        {FILTER_TABS.map((tab) => (
          <button
            key={tab.key}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={[
              'flex-shrink-0 rounded-full px-3.5 py-1.5 text-xs font-semibold transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400',
              activeTab === tab.key
                ? 'bg-brand-500 text-white shadow-sm'
                : 'bg-sand-100 text-sand-600 hover:bg-sand-200',
            ].join(' ')}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {isLoading && (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-16 skeleton-shimmer rounded-xl" />
          ))}
        </div>
      )}

      {!isLoading && !data?.items.length && (
        <EmptyState
          icon={<span className="text-4xl" aria-hidden="true">🔔</span>}
          title="Bandeja vacía"
          description="Cuando alguien reporte un avistamiento, te notificaremos aquí."
        />
      )}

      {!isLoading && groups.length > 0 && (
        <div className="space-y-6">
          {groups.map(([label, items], gIdx) => (
            <div key={label}>
              <p className="mb-2 text-xs font-bold uppercase tracking-wider text-sand-400 px-1">{label}</p>
              <ul role="list" className="list-none divide-y divide-sand-100 rounded-2xl border border-sand-200 field-input p-0 m-0 overflow-hidden">
                {items.map((n, i) => (
                  <motion.li
                    key={n.id}
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: (gIdx * 0.05) + (i * 0.04), duration: 0.2 }}
                  >
                    <NotificationItemCard notification={n} />
                  </motion.li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      )}

      {resolveCheckNotificationId && (
        <div
          className="fixed inset-0 z-50 flex items-end bg-black/40 p-4 sm:items-center sm:justify-center"
          onClick={closeResolveSheet}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="resolve-title"
            className="w-full max-w-md rounded-3xl field-input p-5 shadow-2xl max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="resolve-title" className="mt-1 text-lg font-extrabold text-sand-900">¿Encontraste a tu mascota?</h2>
            <p className="mt-2 text-sm text-sand-600">
              Detectamos actividad reciente compatible con recuperación.
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
                className="rounded-2xl border border-sand-300 field-input px-4 py-2.5 text-sm font-semibold text-sand-700 hover:bg-sand-50 disabled:opacity-50"
              >
                No, sigue perdido
              </button>
              <button type="button" onClick={closeResolveSheet} className="mt-1 text-xs font-semibold text-sand-500 hover:text-sand-800">
                Cerrar
              </button>
            </div>
          </div>
        </div>
      )}

      <NotificationPreferencesToggle />

      {pushStatus !== 'unsupported' && pushStatus !== 'subscribed' && (
        <div className="mt-4 rounded-2xl border border-brand-200 bg-brand-50 p-4">
          <p className="text-sm font-semibold text-brand-800">Notificaciones push</p>
          <p className="mt-0.5 text-xs text-brand-700">Recibe alertas aunque no tengas la app abierta.</p>
          <button
            type="button"
            onClick={() => void pushSubscribe()}
            disabled={pushStatus === 'loading' || pushStatus === 'denied'}
            className="mt-3 rounded-xl bg-brand-500 px-4 py-2 text-xs font-semibold text-white hover:bg-brand-600 disabled:opacity-60"
          >
            {pushStatus === 'loading' ? 'Activando...' : pushStatus === 'denied' ? 'Permiso denegado' : 'Activar notificaciones'}
          </button>
        </div>
      )}
    </div>
  )
}

