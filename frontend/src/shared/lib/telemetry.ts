/**
 * telemetry.ts — Application Insights singleton for PawTrack CR frontend.
 *
 * Initialises the Azure App Insights JS SDK when VITE_APPINSIGHTS_CONNECTION_STRING
 * is set. When the variable is empty (local dev / CI), all telemetry calls are
 * no-ops so the rest of the app can import and call this module unconditionally.
 *
 * Usage:
 *   import { trackEvent, trackException } from '@/shared/lib/telemetry'
 *   trackException(error, { page: 'DashboardPage' })
 */
import {
  ApplicationInsights,
  type ICustomProperties,
  type SeverityLevel,
} from '@microsoft/applicationinsights-web'

const connectionString = import.meta.env.VITE_APPINSIGHTS_CONNECTION_STRING as string | undefined

let appInsights: ApplicationInsights | null = null

if (connectionString) {
  appInsights = new ApplicationInsights({
    config: {
      connectionString,
      enableAutoRouteTracking: true,   // track SPA route changes
      disableFetchTracking: false,      // track fetch() calls
      enableCorsCorrelation: true,      // propagate correlation headers to API
      correlationHeaderExcludedDomains: ['*.openstreetmap.org', 'fonts.googleapis.com'],
      maxBatchInterval: 15_000,
      disableExceptionTracking: false,
    },
  })
  appInsights.loadAppInsights()
  appInsights.trackPageView()
}

// ── Public helpers ─────────────────────────────────────────────────────────────

export function trackException(
  error: Error,
  properties?: ICustomProperties,
): void {
  if (appInsights) {
    appInsights.trackException({ exception: error, properties })
  } else {
    // Structured console output in local dev (preserves stack trace)
    console.error('[telemetry] exception', { error, properties })
  }
}

export function trackEvent(
  name: string,
  properties?: ICustomProperties,
): void {
  if (appInsights) {
    appInsights.trackEvent({ name }, properties)
  } else if (import.meta.env.VITE_DEBUG === 'true') {
    console.info('[telemetry] event', name, properties)
  }
}

export function trackMetric(name: string, average: number): void {
  appInsights?.trackMetric({ name, average })
}

export function setAuthenticatedUser(userId: string): void {
  appInsights?.setAuthenticatedUserContext(userId, undefined, true)
}

export function clearAuthenticatedUser(): void {
  appInsights?.clearAuthenticatedUserContext()
}

export type { SeverityLevel }
