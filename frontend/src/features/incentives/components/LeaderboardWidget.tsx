import { useLeaderboard } from '../hooks/useIncentives'
import { BadgeDisplay } from './BadgeDisplay'
import { Alert } from '@/shared/ui/Alert'

const MEDALS = ['🥇', '🥈', '🥉']

export function LeaderboardWidget() {
  const { data: entries, isLoading, isError } = useLeaderboard(10)

  return (
    <section className="rounded-2xl border border-trust-100 bg-white shadow-sm">
      {/* Header */}
      <div className="flex items-center gap-2 border-b border-trust-100 px-5 py-4">
        <span className="text-xl" aria-hidden="true">🏆</span>
        <h2 className="text-base font-bold text-sand-900">Rescatistas del mes</h2>
      </div>

      {/* Body */}
      <div className="divide-y divide-sand-100">
        {isLoading && (
          <ul className="divide-y divide-sand-100">
            {Array.from({ length: 5 }).map((_, i) => (
              <li key={i} className="flex items-center gap-3 px-5 py-3">
                <div className="h-6 w-6 animate-pulse rounded-full bg-sand-200" />
                <div className="flex-1 space-y-1.5">
                  <div className="h-3.5 w-32 animate-pulse rounded-lg bg-sand-200" />
                  <div className="h-3 w-20 animate-pulse rounded-lg bg-sand-100" />
                </div>
                <div className="h-4 w-8 animate-pulse rounded-lg bg-sand-200" />
              </li>
            ))}
          </ul>
        )}

        {isError && (
          <div className="px-4 py-3">
            <Alert variant="error">No se pudo cargar el leaderboard.</Alert>
          </div>
        )}

        {!isLoading && !isError && entries?.length === 0 && (
          <p className="px-5 py-6 text-sm text-sand-400 text-center">
            Aún no hay rescatistas registrados. ¡Sé el primero!
          </p>
        )}

        {entries?.map((entry, idx) => (
          <div
            key={entry.userId}
            className="flex items-center gap-3 px-5 py-3 hover:bg-trust-50 transition-colors"
          >
            {/* Rank */}
            <span className="w-6 shrink-0 text-center text-lg" aria-label={`Posición ${idx + 1}`}>
              {idx < 3 ? MEDALS[idx] : <span className="text-sm font-semibold text-sand-400">{idx + 1}</span>}
            </span>

            {/* Name + badge */}
            <div className="flex-1 min-w-0">
              <p className="truncate text-sm font-semibold text-sand-900">{entry.ownerName}</p>
              <BadgeDisplay badge={entry.badge} size="sm" />
            </div>

            {/* Stats */}
            <div className="shrink-0 text-right">
              <p className="text-sm font-bold text-rescue-700">{entry.reunificationCount}</p>
              <p className="text-xs text-sand-400">reuniones</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
