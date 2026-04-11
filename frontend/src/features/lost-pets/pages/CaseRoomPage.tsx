import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { FraudReportButton } from '@/features/safety/components/FraudReportButton'
import { OwnerHandoverPanel, RescuerHandoverPanel } from '@/features/safety/components/HandoverCodePanel'
import { useAuthStore } from '@/features/auth/store/authStore'
import { CaseActionsPanel } from '../components/CaseActionsPanel'
import { CaseTimeline } from '../components/CaseTimeline'
import { SightingHeatMap } from '../components/SightingHeatMap'
import { useCaseRoom } from '../hooks/useCaseRoom'
import { EmptyState } from '@/shared/ui/Card'

// ── Tab definitions ───────────────────────────────────────────────────────────

type Tab = 'timeline' | 'map' | 'actions' | 'alerts'

const TABS: { id: Tab; label: string; icon: string }[] = [
  { id: 'timeline', label: 'Cronología', icon: '📋' },
  { id: 'map', label: 'Mapa', icon: '🗺️' },
  { id: 'actions', label: 'Acciones', icon: '⚡' },
  { id: 'alerts', label: 'Alertas', icon: '🔔' },
]

// ── Helpers ───────────────────────────────────────────────────────────────────

// Styles moved to external CSS file: CaseRoomPage.css

function ElapsedTime({ from }: { from: string }) {
  const ms = Date.now() - new Date(from).getTime()
  const hours = Math.floor(ms / 3_600_000)
  const days = Math.floor(hours / 24)
  if (days > 0) return <>{days}d {hours % 24}h perdido/a</>
  return <>{hours}h perdido/a</>
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function CaseRoomPage() {
  const { id } = useParams<{ id: string }>()
  const lostEventId = id ?? ''

  const { data, isLoading, isError, isFetching, refetch } = useCaseRoom(lostEventId)
  const [activeTab, setActiveTab] = useState<Tab>('timeline')
  const currentUserId = useAuthStore((s) => s.user?.id)

  // ── Loading skeleton ───────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="mx-auto max-w-[680px] px-4 py-8">
        <div className="mb-4 h-6 w-48 animate-pulse rounded-lg bg-sand-200" />
        <div className="mb-3 h-16 animate-pulse rounded-2xl bg-sand-100" />
        <div className="h-12 animate-pulse rounded-2xl bg-sand-100" />
      </div>
    )
  }

  // ── Error state ────────────────────────────────────────────────────────────
  if (isError || !data) {
    return (
      <div className="mx-auto max-w-[680px] px-4 py-12 text-center">
        <p className="mb-4 text-sm text-sand-500">
          No se pudo cargar el centro de comando. El reporte puede haber sido cerrado o no tienes
          acceso.
        </p>
        <Link to="/dashboard" className="text-sm font-semibold text-trust-600 underline hover:text-trust-700">
          ← Volver al tablero
        </Link>
      </div>
    )
  }

  const { event, sightings, nearbyAlerts, totalNearbyAlertsDispatched } = data

  return (
    <div className="mx-auto max-w-[680px] px-4 pb-16 pt-6 animate-fade-in-up">
      <h1 className="sr-only">Centro de Mando — Mascota perdida</h1>

      {/* ── Back nav ──────────────────────────────────────────────── */}
      <Link
        to={`/pets/${event.petId}`}
        className="mb-4 flex items-center gap-1 text-xs font-medium text-sand-500 hover:text-sand-800 transition-base"
      >
        ← Perfil de mascota
      </Link>

      {/* ── Header card ─────────────────────────────────────────────────────── */}
      <div className="mb-5 flex items-center gap-3.5 rounded-2xl border border-danger-200 bg-gradient-to-br from-danger-50 to-warn-50 p-4">
        {event.recentPhotoUrl ? (
          <img
            src={event.recentPhotoUrl}
            alt="Mascota"
            className="h-16 w-16 shrink-0 rounded-full border-2 border-danger-300 object-cover"
          />
        ) : (
          <div
            aria-hidden="true"
            className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-danger-200 text-3xl"
          >
            🐾
          </div>
        )}

        <div className="min-w-0 flex-1">
          <p className="text-xs font-bold uppercase tracking-[0.05em] text-danger-600">
            🚨 Centro de Comando
          </p>
          <p className="mt-0.5 truncate text-base font-extrabold text-sand-900">
            Caso activo
          </p>
          <p className="mt-0.5 text-xs text-sand-500">
            <ElapsedTime from={event.reportedAt} />
          </p>
        </div>

        <button
          type="button"
          onClick={() => void refetch()}
          aria-label="Actualizar datos"
          className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full border border-sand-200 bg-white text-base hover:bg-sand-50 active:scale-95 transition-base"
        >
          <span aria-hidden="true" className={isFetching ? 'animate-spin inline-block' : 'inline-block'}>🔄</span>
        </button>
      </div>

      {/* ── Tab bar ─────────────────────────────────────────────────────────── */}
      <div
        role="tablist"
        className="mb-4 grid grid-cols-4 gap-1 rounded-2xl bg-sand-100 p-1.5 sm:grid-cols-4"
      >
        {TABS.map((tab) => (
          <button
            key={tab.id}
            role="tab"
            type="button"
            aria-selected={activeTab === tab.id}
            aria-controls={`panel-${tab.id}`}
            onClick={() => setActiveTab(tab.id)}
            className={[
              'inline-flex items-center justify-center gap-1.5 rounded-xl border-0 px-2 py-3 text-sm font-bold transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 sm:text-[0.82rem]',
              activeTab === tab.id
                ? 'bg-white text-sand-900 shadow-sm'
                : 'bg-transparent text-sand-500 hover:bg-white/70 hover:text-sand-800',
            ].join(' ')}
          >
            <span aria-hidden="true">{tab.icon}</span>
            {tab.label}
          </button>
        ))}
      </div>

      {/* ── Tab panels ──────────────────────────────────────────────────────── */}
      <div id={`panel-timeline`} role="tabpanel" hidden={activeTab !== 'timeline'}>
        <CaseTimeline event={event} sightings={sightings} nearbyAlerts={nearbyAlerts} />
      </div>

      <div id={`panel-map`} role="tabpanel" hidden={activeTab !== 'map'}>
        {activeTab === 'map' && (
          <SightingHeatMap
            sightings={sightings}
            defaultCenter={
              event.lastSeenLat && event.lastSeenLng
                ? [event.lastSeenLat, event.lastSeenLng]
                : undefined
            }
          />
        )}
        {sightings.length === 0 && (
          <EmptyState
            icon={<span className="text-3xl" aria-hidden="true">🗺️</span>}
            title="Sin avistamientos aún"
            description="Cuando alguien reporte haber visto a la mascota, los puntos aparecerán aquí."
            className="mt-4"
          />
        )}
      </div>

      <div id={`panel-actions`} role="tabpanel" hidden={activeTab !== 'actions'}>
        <CaseActionsPanel
          event={event}
          sightings={sightings}
          totalNearbyAlertsDispatched={totalNearbyAlertsDispatched}
        />

        {/* Handover code — only visible to the pet owner */}
        {currentUserId === event.ownerId && (
          <div className="mt-6">
            <OwnerHandoverPanel lostPetEventId={lostEventId} />
          </div>
        )}

        {/* Rescuer verification panel — authenticated non-owners can verify handover */}
        {currentUserId && currentUserId !== event.ownerId && (
          <div className="mt-6">
            <RescuerHandoverPanel lostPetEventId={lostEventId} />
          </div>
        )}

        {/* Fraud report — visible to everyone except the pet owner */}
        {currentUserId !== event.ownerId && (
          <div className="mt-6">
            <FraudReportButton
              context="PublicProfile"
              relatedEntityId={lostEventId}
              targetUserId={event.ownerId}
            />
          </div>
        )}
      </div>

      <div id={`panel-alerts`} role="tabpanel" hidden={activeTab !== 'alerts'}>
        {nearbyAlerts.length === 0 ? (
          <p className="py-8 text-center text-sm text-sand-400">
            Aún no se han enviado alertas a usuarios cercanos.
          </p>
        ) : (
          <ul className="flex flex-col gap-2.5 p-0">
            {nearbyAlerts.map((alert) => (
              <li
                key={alert.notificationId}
                className="rounded-2xl border border-trust-200 bg-trust-50 p-3"
              >
                <p className="text-[0.82rem] font-semibold text-trust-900">
                  🔔 {alert.title}
                </p>
                <time
                  dateTime={alert.sentAt}
                  className="mt-0.5 block text-[0.72rem] text-sand-500"
                >
                  {new Date(alert.sentAt).toLocaleString('es-CR')}
                </time>
              </li>
            ))}
          </ul>
        )}
        <p className="mt-3 text-center text-[0.72rem] text-sand-400">
          {totalNearbyAlertsDispatched} alerta(s) enviadas en total
        </p>
      </div>
    </div>
  )
}
