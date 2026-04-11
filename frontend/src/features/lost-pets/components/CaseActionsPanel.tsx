import { Link } from 'react-router-dom'
import type { LostPetEvent } from '../api/lostPetsApi'
import type { SightingDetail } from '@/features/sightings/api/sightingsApi'

// ── Types ─────────────────────────────────────────────────────────────────────

interface CaseActionsPanelProps {
  event: LostPetEvent
  sightings: SightingDetail[]
  totalNearbyAlertsDispatched: number
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function buildShareUrl(petId: string): string {
  return `${window.location.origin}/public/pets/${petId}`
}

// ── Sub-components ────────────────────────────────────────────────────────────

function StatBadge({ value, label, color }: { value: number; label: string; color: string }) {
  return (
    <div className="flex-1 rounded-xl bg-sand-100 py-3 px-2 text-center">
      <p className="m-0 text-2xl font-extrabold" style={{ color }}>{value}</p>
      <p className="mt-0.5 text-[10px] leading-snug text-sand-500">{label}</p>
    </div>
  )
}

// ── Component ─────────────────────────────────────────────────────────────────

export function CaseActionsPanel({
  event,
  sightings,
  totalNearbyAlertsDispatched,
}: CaseActionsPanelProps) {
  const urgentCount = sightings.filter((s) => s.priorityBadge === 'Urgent').length
  const latestUrgent = sightings.find((s) => s.priorityBadge === 'Urgent')

  const handleShareProfile = async () => {
    const url = buildShareUrl(event.petId)
    if (navigator.share) {
      try {
        await navigator.share({ title: '¡Ayuda a encontrar mi mascota!', url })
      } catch {
        // User dismissed share sheet — no-op
      }
    } else {
      await navigator.clipboard.writeText(url)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {/* ── Stats row ──────────────────────────────────────────────────── */}
      <div className="flex gap-2">
        <StatBadge value={sightings.length} label="Avistamientos" color="#e8521e" />
        <StatBadge value={urgentCount} label="Urgentes" color="#d42020" />
        <StatBadge value={totalNearbyAlertsDispatched} label="Alertas enviadas" color="#3056c2" />
      </div>

      {/* ── Urgent recommendation ─────────────────────────────────────── */}
      {latestUrgent && (
        <div className="rounded-xl bg-danger-50 border border-danger-200 p-3.5">
          <p className="mb-1 text-xs font-bold text-danger-800">
            🚨 Acción recomendada
          </p>
          <p className="text-[0.82rem] text-danger-900">
            {latestUrgent.recommendedAction}
          </p>
          <Link
            to={`/pets/${event.petId}/sightings/${latestUrgent.id}`}
            className="mt-2 inline-block text-xs font-semibold text-danger-600 underline hover:text-danger-800"
          >
            Ver avistamiento urgente →
          </Link>
        </div>
      )}

      {/* ── Quick actions ─────────────────────────────────────────────── */}
      <div className="flex flex-col gap-2">
        <button
          type="button"
          onClick={() => void handleShareProfile()}
          className="w-full rounded-xl bg-trust-700 py-3 text-sm font-bold text-white hover:bg-trust-800 focus-visible:ring-2 focus-visible:ring-trust-400 focus-visible:outline-none"
        >
          📤 Compartir perfil de mascota
        </button>

        <Link
          to={`/pets/${event.petId}`}
          className="block rounded-xl bg-sand-100 py-3 text-center text-sm font-semibold text-sand-900 hover:bg-sand-200"
        >
          🐾 Ver perfil de mascota
        </Link>

        {event.status === 'Active' && (
          <Link
            to={`/pets/${event.petId}`}
            state={{ openReunite: true }}
            className="block rounded-xl border border-rescue-200 bg-rescue-50 py-3 text-center text-sm font-bold text-rescue-800 hover:bg-rescue-100"
          >
            🎉 Marcar como reunido
          </Link>
        )}
      </div>
    </div>
  )
}
