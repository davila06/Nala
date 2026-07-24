import { motion } from 'framer-motion'
import { useLeaderboard } from '../hooks/useIncentives'
import { BadgeDisplay } from './BadgeDisplay'
import { Alert } from '@/shared/ui/Alert'

const MEDALS = ['🥇', '🥈', '🥉']

// Shimmer classes for top 3 podium spots
const PODIUM_GLOW = [
  'shadow-warn-200 ring-1 ring-warn-300/50',    // gold
  'shadow-zinc-200 ring-1 ring-zinc-300/50',    // silver
  'shadow-brand-100 ring-1 ring-brand-200/40',  // bronze
]

export function LeaderboardWidget() {
  const { data: entries, isLoading, isError } = useLeaderboard(10)

  return (
    <section className="rounded-2xl border border-trust-100 bg-white shadow-sm overflow-hidden">
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
                <div className="h-6 w-6 skeleton-shimmer rounded-full" />
                <div className="flex-1 space-y-1.5">
                  <div className="h-3.5 w-32 skeleton-shimmer rounded-lg" />
                  <div className="h-3 w-20 skeleton-shimmer rounded-lg" />
                </div>
                <div className="h-4 w-8 skeleton-shimmer rounded-lg" />
              </li>
            ))}
          </ul>
        )}

        {isError && (
          <div className="px-4 py-3"><Alert variant="error">No se pudo cargar el leaderboard.</Alert></div>
        )}

        {!isLoading && !isError && entries?.length === 0 && (
          <p className="px-5 py-6 text-sm text-sand-400 text-center">
            Aún no hay rescatistas registrados. ¡Sé el primero!
          </p>
        )}

        {entries?.map((entry, idx) => (
          <motion.div
            key={entry.userId}
            initial={{ opacity: 0, x: -10 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: idx * 0.05, duration: 0.2 }}
            className={[
              'flex items-center gap-3 px-5 py-3 transition-colors hover:bg-trust-50',
              idx < 3 ? `shadow-sm ${PODIUM_GLOW[idx]}` : '',
            ].join(' ')}
          >
            {/* Rank */}
            <span className="w-6 shrink-0 text-center text-lg" aria-label={`Posición ${idx + 1}`}>
              {idx < 3
                ? <motion.span initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ delay: idx * 0.05 + 0.1, type: 'spring', stiffness: 400 }}>{MEDALS[idx]}</motion.span>
                : <span className="text-sm font-semibold text-sand-400">{idx + 1}</span>}
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
          </motion.div>
        ))}
      </div>
    </section>
  )
}
