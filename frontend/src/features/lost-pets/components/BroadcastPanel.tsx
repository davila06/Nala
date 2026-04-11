import { useBroadcastStatus, useTriggerBroadcast } from '../hooks/useBroadcast'
import { Alert } from '@/shared/ui/Alert'
import type { BroadcastAttemptDto, BroadcastChannel } from '../api/broadcastApi'

// ── Channel display helpers ───────────────────────────────────────────────────

const CHANNEL_LABEL: Record<BroadcastChannel, string> = {
  Email: 'Correo',
  WhatsApp: 'WhatsApp',
  Telegram: 'Telegram',
  Facebook: 'Facebook',
}

const CHANNEL_ICON: Record<BroadcastChannel, string> = {
  Email: '✉️',
  WhatsApp: '💬',
  Telegram: '📨',
  Facebook: '📘',
}

function StatusChip({ attempt }: { attempt: BroadcastAttemptDto }) {
  const label = CHANNEL_LABEL[attempt.channel]
  const icon = CHANNEL_ICON[attempt.channel]

  if (attempt.status === 'Sent') {
    return (
      <div className="flex items-center justify-between rounded-xl border border-rescue-200 bg-rescue-50 px-3 py-2">
        <span className="flex items-center gap-2 text-sm font-medium text-rescue-800">
          <span aria-hidden="true">{icon}</span>
          {label}
        </span>
        <span className="flex items-center gap-1 text-xs font-semibold text-rescue-700">
          ✅ Enviado
          {attempt.trackingClicks > 0 && (
            <span className="ml-2 rounded-full bg-rescue-100 px-2 py-0.5 text-xs text-rescue-800">
              {attempt.trackingClicks} clic{attempt.trackingClicks !== 1 ? 's' : ''}
            </span>
          )}
        </span>
      </div>
    )
  }

  if (attempt.status === 'Failed') {
    return (
      <div className="flex items-center justify-between rounded-xl border border-danger-200 bg-danger-50 px-3 py-2">
        <span className="flex items-center gap-2 text-sm font-medium text-danger-800">
          <span aria-hidden="true">{icon}</span>
          {label}
        </span>
        <span className="text-xs font-semibold text-danger-700">❌ Error</span>
      </div>
    )
  }

  if (attempt.status === 'Skipped') {
    return (
      <div className="flex items-center justify-between rounded-xl border border-sand-200 bg-sand-50 px-3 py-2">
        <span className="flex items-center gap-2 text-sm font-medium text-sand-500">
          <span aria-hidden="true">{icon}</span>
          {label}
        </span>
        <span className="text-xs text-sand-400">⏭ Omitido</span>
      </div>
    )
  }

  // Pending
  return (
    <div className="flex items-center justify-between rounded-xl border border-brand-200 bg-brand-50 px-3 py-2">
      <span className="flex items-center gap-2 text-sm font-medium text-brand-800">
        <span aria-hidden="true">{icon}</span>
        {label}
      </span>
      <span className="text-xs text-brand-600">⏳ Pendiente</span>
    </div>
  )
}

// ── Main component ────────────────────────────────────────────────────────────

interface BroadcastPanelProps {
  lostEventId: string
  className?: string
}

export function BroadcastPanel({ lostEventId, className }: BroadcastPanelProps) {
  const { data: status, isLoading: isLoadingStatus } = useBroadcastStatus(lostEventId)
  const { mutate: trigger, isPending: isTriggering, isError, error } = useTriggerBroadcast(lostEventId)

  const hasBroadcast = status != null && status.attempts.length > 0
  const errorMessage =
    isError && error instanceof Error ? error.message : isError ? 'Error al difundir' : null

  return (
    <div
      className={`rounded-2xl border border-trust-200 bg-trust-50 p-5 ${className ?? ''}`}
      role="region"
      aria-label="Difusión multicanal"
    >
      <h2 className="mb-1 text-sm font-bold text-trust-900"><span aria-hidden="true">📡</span> Difusión multicanal</h2>
      <p className="mb-4 text-xs text-trust-700">
        Notifica automáticamente a través de todos los canales configurados (correo, WhatsApp,
        Telegram y Facebook) para ampliar el alcance de la búsqueda.
      </p>

      {/* ── Aggregate metrics ──────────────────────────────────────────────── */}
      {hasBroadcast && (
        <div className="mb-4 flex gap-3 text-center">
          <div className="flex-1 rounded-xl bg-white/70 py-2">
            <p className="text-lg font-bold text-rescue-700">{status.sentCount}</p>
            <p className="text-xs text-sand-500">Enviados</p>
          </div>
          <div className="flex-1 rounded-xl bg-white/70 py-2">
            <p className="text-lg font-bold text-sand-600">{status.skippedCount}</p>
            <p className="text-xs text-sand-500">Omitidos</p>
          </div>
          {status.failedCount > 0 && (
            <div className="flex-1 rounded-xl bg-white/70 py-2">
              <p className="text-lg font-bold text-danger-600">{status.failedCount}</p>
              <p className="text-xs text-sand-500">Errores</p>
            </div>
          )}
          {status.totalClicks > 0 && (
            <div className="flex-1 rounded-xl bg-white/70 py-2">
              <p className="text-lg font-bold text-trust-700">{status.totalClicks}</p>
              <p className="text-xs text-sand-500">Clics</p>
            </div>
          )}
        </div>
      )}

      {/* ── Per-channel status chips ───────────────────────────────────────── */}
      {isLoadingStatus ? (
        <div className="mb-4 space-y-2">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-10 animate-pulse rounded-xl bg-trust-100" />
          ))}
        </div>
      ) : hasBroadcast ? (
        <div className="mb-4 space-y-2">
          {status.attempts.map((attempt) => (
            <StatusChip key={attempt.id} attempt={attempt} />
          ))}
        </div>
      ) : (
        <p className="mb-4 text-xs text-trust-600">
          Aún no se ha enviado ninguna difusión. Usa el botón de abajo para notificar a todos los canales.
        </p>
      )}

      {/* ── Error feedback ─────────────────────────────────────────────────── */}
      {errorMessage && (
        <Alert variant="error" className="mb-3 text-xs">{errorMessage}</Alert>
      )}

      {/* ── Trigger button ─────────────────────────────────────────────────── */}
      <button
        type="button"
        onClick={() => trigger()}
        disabled={isTriggering}
        className="flex w-full items-center justify-center gap-2 rounded-xl bg-trust-600 px-4 py-3 text-sm font-semibold text-white hover:bg-trust-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400 focus-visible:ring-offset-1"
        aria-label={hasBroadcast ? 'Volver a difundir por todos los canales' : 'Difundir por todos los canales'}
      >
        {isTriggering ? (
          <>
            <span
              className="inline-block size-4 animate-spin rounded-full border-2 border-white border-t-transparent"
              aria-hidden="true"
            />
            Difundiendo…
          </>
        ) : hasBroadcast ? (
          <><span aria-hidden="true">🔁</span> Volver a difundir</>
        ) : (
          <><span aria-hidden="true">📡</span> Difundir ahora</>
        )}
      </button>

      {hasBroadcast && (
        <p className="mt-2 text-center text-xs text-trust-600">
          Los canales omitidos no están configurados actualmente.
        </p>
      )}
    </div>
  )
}

