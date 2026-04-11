import { useOfflineReportQueue } from '../hooks/useOfflineReportQueue'
import type { QueuedReport } from '@/shared/lib/offlineQueue'

// ── Sub-components ────────────────────────────────────────────────────────────

function PendingBanner({
  items,
  isSyncing,
  onRetry,
}: {
  items: QueuedReport[]
  isSyncing: boolean
  onRetry: () => void
}) {
  const count = items.length
  return (
    <div
      role="status"
      aria-live="polite"
      className="flex items-center justify-between gap-3 bg-brand-50 px-4 py-2.5 text-sm border-b border-brand-200"
    >
      <span className="flex items-center gap-2 text-brand-800">
        {isSyncing ? (
          <>
            <span
              className="inline-block size-3.5 animate-spin rounded-full border-2 border-brand-400 border-t-transparent"
              aria-hidden="true"
            />
            Sincronizando {count} reporte{count !== 1 ? 's' : ''}…
          </>
        ) : (
          <>
            <span aria-hidden="true">📵</span>
            {count} reporte{count !== 1 ? 's' : ''} pendiente{count !== 1 ? 's' : ''} de sincronización
          </>
        )}
      </span>
      {!isSyncing && (
        <button
          type="button"
          onClick={onRetry}
          className="shrink-0 rounded-lg border border-brand-400 px-2.5 py-2 text-xs font-semibold text-brand-800 hover:bg-brand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
        >
          Reintentar
        </button>
      )}
    </div>
  )
}

function ConflictBanner({
  item,
  onDismiss,
}: {
  item: QueuedReport
  onDismiss: () => void
}) {
  return (
    <div
      role="alert"
      className="flex items-start justify-between gap-3 bg-warn-50 px-4 py-2.5 text-sm border-b border-warn-200"
    >
      <span className="text-warn-800">
        <span aria-hidden="true">⚠️</span>{' '}
        <strong>{item.petName}</strong> ya tiene un reporte activo — el reporte guardado el{' '}
        {new Date(item.capturedAt).toLocaleString('es-CR', {
          day: 'numeric',
          month: 'short',
          hour: '2-digit',
          minute: '2-digit',
        })}{' '}
        no se enviará.
      </span>
      <button
        type="button"
        onClick={onDismiss}
        aria-label="Descartar conflicto"
        className="shrink-0 rounded-full p-2 text-warn-600 hover:bg-warn-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-warn-400 focus-visible:ring-offset-1"
      >
        ✕
      </button>
    </div>
  )
}

function DoneBanner({
  item,
  onDismiss,
}: {
  item: QueuedReport
  onDismiss: () => void
}) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="flex items-center justify-between gap-3 bg-rescue-50 px-4 py-2.5 text-sm border-b border-rescue-200"
    >
      <span className="text-rescue-800">
        <span aria-hidden="true">✅</span>{' '}
        Reporte de <strong>{item.petName}</strong> sincronizado correctamente.
      </span>
      <button
        type="button"
        onClick={onDismiss}
        aria-label="Descartar"
        className="shrink-0 rounded-full p-2 text-rescue-600 hover:bg-rescue-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400 focus-visible:ring-offset-1"
      >
        ✕
      </button>
    </div>
  )
}

// ── Main component ────────────────────────────────────────────────────────────

/**
 * Renders status banners for the offline report queue.
 * Mount exactly once, at the authenticated layout root.
 * This component owns the `useOfflineReportQueue` hook call (single instance).
 */
export function OfflineQueueBanner() {
  const { pendingItems, conflictItems, doneSinceMount, isSyncing, retryNow, dismiss } =
    useOfflineReportQueue()

  const hasAnything =
    pendingItems.length > 0 || conflictItems.length > 0 || doneSinceMount.length > 0

  if (!hasAnything) return null

  return (
    <div className="offline-queue-banners" aria-label="Estado de sincronización offline">
      {pendingItems.length > 0 && (
        <PendingBanner items={pendingItems} isSyncing={isSyncing} onRetry={retryNow} />
      )}
      {conflictItems.map((item) => (
        <ConflictBanner key={item.id} item={item} onDismiss={() => void dismiss(item.id)} />
      ))}
      {doneSinceMount.map((item) => (
        <DoneBanner key={item.id} item={item} onDismiss={() => void dismiss(item.id)} />
      ))}
    </div>
  )
}

