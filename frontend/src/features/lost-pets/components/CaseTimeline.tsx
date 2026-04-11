import type { LostPetEvent } from '../api/lostPetsApi'
import type { NearbyAlertSummary } from '../api/caseRoomApi'
import type { SightingDetail } from '@/features/sightings/api/sightingsApi'

// ── Types ─────────────────────────────────────────────────────────────────────

type TimelineEventKind = 'report' | 'sighting' | 'alert'

interface TimelineEntry {
  id: string
  kind: TimelineEventKind
  at: Date
  title: string
  subtitle?: string
  badge?: 'Urgent' | 'Validate' | 'Observe'
}

interface CaseTimelineProps {
  event: LostPetEvent
  sightings: SightingDetail[]
  nearbyAlerts: NearbyAlertSummary[]
}

// ── Helpers ───────────────────────────────────────────────────────────────────

const BADGE_STYLES: Record<NonNullable<TimelineEntry['badge']>, string> = {
  Urgent: 'background:#fef2f2;color:#991b1b;border:1px solid #fecaca',
  Validate: 'background:#fffbeb;color:#92400e;border:1px solid #fde68a',
  Observe: 'background:#f0fdf4;color:#166534;border:1px solid #bbf7d0',
}

const KIND_ICON: Record<TimelineEventKind, string> = {
  report: '🚨',
  sighting: '🐾',
  alert: '🔔',
}

const KIND_COLOR: Record<TimelineEventKind, string> = {
  report: '#d42020',
  sighting: '#e8521e',
  alert: '#3056c2',
}

const BADGE_LABEL: Record<NonNullable<TimelineEntry['badge']>, string> = {
  Urgent: 'Urgente',
  Validate: 'Validar',
  Observe: 'Observar',
}

function formatTime(date: Date): string {
  return date.toLocaleTimeString('es-CR', { hour: '2-digit', minute: '2-digit' })
}

function formatDate(date: Date): string {
  return date.toLocaleDateString('es-CR', { day: '2-digit', month: 'short', year: 'numeric' })
}

function formatReportSubtitle(event: LostPetEvent): string | undefined {
  if (event.description) {
    return event.description
  }

  if (event.lastSeenLat != null && event.lastSeenLng != null) {
    return `${event.lastSeenLat.toFixed(5)}, ${event.lastSeenLng.toFixed(5)}`
  }

  return undefined
}

// ── Component ─────────────────────────────────────────────────────────────────

export function CaseTimeline({ event, sightings, nearbyAlerts }: CaseTimelineProps) {
  // Build unified, sorted timeline
  const entries: TimelineEntry[] = [
    {
      id: event.id,
      kind: 'report' as const,
      at: new Date(event.reportedAt),
      title: 'Reporte de mascota perdida',
      subtitle: formatReportSubtitle(event),
    },
    ...sightings.map<TimelineEntry>((s) => ({
      id: s.id,
      kind: 'sighting' as const,
      at: new Date(s.sightedAt),
      title: 'Avistamiento reportado',
      subtitle: s.note ?? undefined,
      badge: s.priorityBadge,
    })),
    ...nearbyAlerts.map<TimelineEntry>((a) => ({
      id: a.notificationId,
      kind: 'alert' as const,
      at: new Date(a.sentAt),
      title: 'Alerta enviada',
      subtitle: a.title,
    })),
  ].sort((a, b) => a.at.getTime() - b.at.getTime())

  if (entries.length === 0) {
    return (
      <div className="py-8 px-4 text-center text-sm text-sand-400">
        No hay eventos en el historial aún.
      </div>
    )
  }

  return (
    <ol className="list-none p-0 m-0 relative">
      {/* Vertical guide rail */}
      <li
        aria-hidden="true"
        className="absolute left-[19px] top-6 bottom-0 w-0.5 bg-sand-200 rounded"
      />

      {entries.map((entry) => (
        <li
          key={entry.id}
          className="flex gap-3.5 pb-5 relative"
        >
          {/* Icon dot */}
          <span
            aria-hidden="true"
            className="shrink-0 z-[1] flex h-10 w-10 items-center justify-center rounded-full bg-white text-lg shadow-sm"
            style={{ border: `2px solid ${KIND_COLOR[entry.kind]}` }}
          >
            {KIND_ICON[entry.kind]}
          </span>

          {/* Content */}
          <div className="flex-1 min-w-0">
            <div className="flex items-baseline gap-2 flex-wrap">
              <span className="text-sm font-semibold text-sand-900">
                {entry.title}
              </span>
              {entry.badge && (
                <span
                  style={{
                    fontSize: '0.7rem',
                    fontWeight: 700,
                    padding: '1px 6px',
                    borderRadius: '4px',
                    ...Object.fromEntries(
                      BADGE_STYLES[entry.badge]
                        .split(';')
                        .filter(Boolean)
                        .map((rule) => {
                          const [k, v] = rule.split(':')
                          const key = k.trim().replace(/-([a-z])/g, (_, c: string) => c.toUpperCase())
                          return [key, v.trim()]
                        }),
                    ),
                  }}
                >
                  {BADGE_LABEL[entry.badge]}
                </span>
              )}
            </div>

            {entry.subtitle && (
              <p
                className="mt-0.5 text-xs text-sand-500 truncate"
                title={entry.subtitle}
              >
                {entry.subtitle}
              </p>
            )}

            <time
              dateTime={entry.at.toISOString()}
              className="block mt-0.5 text-[0.72rem] text-sand-400"
            >
              {formatDate(entry.at)} · {formatTime(entry.at)}
            </time>
          </div>
        </li>
      ))}
    </ol>
  )
}
