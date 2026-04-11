import { useSightingsByPet } from '../hooks/useSightings'

interface SightingListProps {
  petId: string
}

const PRIORITY_STYLES = {
  Urgent: {
    label: 'Urgente',
    badgeClassName: 'bg-danger-50 text-danger-700 ring-danger-200',
    cardClassName: 'border-danger-100',
  },
  Validate: {
    label: 'Validar',
    badgeClassName: 'bg-brand-50 text-brand-700 ring-brand-200',
    cardClassName: 'border-brand-100',
  },
  Observe: {
    label: 'Observar',
    badgeClassName: 'bg-trust-50 text-trust-700 ring-trust-200',
    cardClassName: 'border-trust-100',
  },
} as const

export function SightingList({ petId }: SightingListProps) {
  const { data: sightings, isLoading, isError } = useSightingsByPet(petId)

  if (isLoading) {
    return (
      <ul className="space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <li key={i} className="h-16 animate-pulse rounded-xl bg-sand-100" />
        ))}
      </ul>
    )
  }

  if (isError) {
    return (
      <p className="text-sm text-danger-500">No se pudieron cargar los avistamientos.</p>
    )
  }

  if (!sightings || sightings.length === 0) {
    return (
      <p className="rounded-xl bg-sand-50 px-4 py-6 text-center text-sm text-sand-400">
        Sin avistamientos reportados aún.
      </p>
    )
  }

  return (
    <ul className="space-y-2">
      {sightings.map((s) => (
        <li
          key={s.id}
          className={`rounded-xl border bg-white p-3 shadow-sm ${PRIORITY_STYLES[s.priorityBadge].cardClassName}`}
        >
          <div className="flex items-start gap-3">
            {s.photoUrl && (
              <img
                src={s.photoUrl}
                alt="Avistamiento"
                className="h-14 w-14 flex-shrink-0 rounded-lg object-cover"
              />
            )}
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <span
                  className={`inline-flex items-center rounded-full px-2 py-1 text-[11px] font-bold uppercase tracking-[0.12em] ring-1 ${PRIORITY_STYLES[s.priorityBadge].badgeClassName}`}
                >
                  {PRIORITY_STYLES[s.priorityBadge].label}
                </span>
                <span className="text-xs font-semibold text-sand-500">{s.priorityScore}/100</span>
              </div>
              <p className="mt-2 text-xs font-semibold text-sand-700">
                <span aria-hidden="true">📍</span> {s.lat.toFixed(4)}, {s.lng.toFixed(4)}
              </p>
              {s.note && (
                <p className="mt-0.5 line-clamp-2 text-xs text-sand-500">{s.note}</p>
              )}
              <p className="mt-2 text-xs text-sand-600">{s.recommendedAction}</p>
              <p className="mt-1 text-xs text-sand-400">
                {new Date(s.sightedAt).toLocaleString()}
              </p>
            </div>
          </div>
        </li>
      ))}
    </ul>
  )
}

