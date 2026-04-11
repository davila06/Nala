import type { MatchCandidate } from '../api/foundPetsApi'

// ── Helpers ───────────────────────────────────────────────────────────────────

function scoreLabel(score: number): { text: string; color: string; bg: string } {
  if (score >= 80)
    return { text: 'Alta coincidencia', color: 'text-rescue-700', bg: 'bg-rescue-50' }
  if (score >= 50)
    return { text: 'Posible coincidencia', color: 'text-brand-700', bg: 'bg-brand-50' }
  return { text: 'Coincidencia parcial', color: 'text-sand-500', bg: 'bg-sand-50' }
}

// ── Component ─────────────────────────────────────────────────────────────────

interface Props {
  candidate: MatchCandidate
}

export function FoundPetMatchCard({ candidate }: Props) {
  const { text, color, bg } = scoreLabel(candidate.scorePercent)

  return (
    <article
      className={`flex items-start gap-4 rounded-xl border border-sand-200 p-4 ${bg} shadow-sm`}
    >
      {/* Pet photo */}
      <div className="size-16 shrink-0 overflow-hidden rounded-lg bg-sand-200">
        {candidate.petPhotoUrl ? (
          <img
            src={candidate.petPhotoUrl}
            alt={candidate.petName}
            className="size-full object-cover"
          />
        ) : (
          <div className="flex size-full items-center justify-center text-2xl" aria-hidden="true">🐾</div>
        )}
      </div>

      {/* Info */}
      <div className="flex-1 space-y-1">
        <p className="font-semibold text-sand-900">{candidate.petName}</p>

        <p className={`text-sm font-medium ${color}`}>
          {text} — {candidate.scorePercent}%
        </p>

        {candidate.lastSeenLat && candidate.lastSeenLng && (
          <p className="text-xs text-sand-500">
            Última vez visto cerca de ({candidate.lastSeenLat.toFixed(4)},{' '}
            {candidate.lastSeenLng.toFixed(4)})
          </p>
        )}

        <p className="text-xs text-sand-400">
          Reporte:{' '}
          {new Date(candidate.lastSeenAt).toLocaleDateString('es-CR', {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
          })}
        </p>
      </div>

      {/* Score badge */}
      <div
        className={`flex size-12 shrink-0 flex-col items-center justify-center rounded-full border-2 ${
          candidate.scorePercent >= 70
            ? 'border-rescue-500 text-rescue-700'
            : 'border-sand-300 text-sand-500'
        }`}
      >
        <span className="text-sm font-bold leading-none">{candidate.scorePercent}</span>
        <span className="text-[10px] leading-none">%</span>
      </div>
    </article>
  )
}

