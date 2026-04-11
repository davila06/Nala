/**
 * Date formatting utilities using Intl.DateTimeFormat with Costa Rica locale (es-CR).
 *
 * Functions are pure and safe to call with any ISO-8601 date string.
 * All output is in Spanish as required by PawTrack CR's UX.
 */

const LOCALE = 'es-CR'

/** e.g. "7 abr 2026" */
export function formatDate(iso: string): string {
  return new Intl.DateTimeFormat(LOCALE, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(iso))
}

/** e.g. "7 abr 2026, 10:30" */
export function formatDateTime(iso: string): string {
  return new Intl.DateTimeFormat(LOCALE, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(iso))
}

/** e.g. "hace 2 horas" | "hace 3 días" | "hace 1 mes" */
export function formatRelative(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime()
  const diffSec = Math.floor(diffMs / 1000)

  if (diffSec < 60) return 'hace un momento'

  const diffMin = Math.floor(diffSec / 60)
  if (diffMin < 60) {
    return new Intl.RelativeTimeFormat(LOCALE, { numeric: 'auto' }).format(-diffMin, 'minute')
  }

  const diffHour = Math.floor(diffMin / 60)
  if (diffHour < 24) {
    return new Intl.RelativeTimeFormat(LOCALE, { numeric: 'auto' }).format(-diffHour, 'hour')
  }

  const diffDay = Math.floor(diffHour / 24)
  if (diffDay < 30) {
    return new Intl.RelativeTimeFormat(LOCALE, { numeric: 'auto' }).format(-diffDay, 'day')
  }

  const diffMonth = Math.floor(diffDay / 30)
  if (diffMonth < 12) {
    return new Intl.RelativeTimeFormat(LOCALE, { numeric: 'auto' }).format(-diffMonth, 'month')
  }

  const diffYear = Math.floor(diffMonth / 12)
  return new Intl.RelativeTimeFormat(LOCALE, { numeric: 'auto' }).format(-diffYear, 'year')
}
