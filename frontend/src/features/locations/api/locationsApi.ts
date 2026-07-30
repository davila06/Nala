import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export interface UserLocationPayload {
  lat: number
  lng: number
  receiveNearbyAlerts: boolean
  quietHoursStart?: string | null
  quietHoursEnd?: string | null
  /** IANA timezone ID from the browser (e.g. "America/New_York"). */
  timeZoneId?: string
}

export interface UserLocationResponse {
  lat: number
  lng: number
  receiveNearbyAlerts: boolean
  updatedAt: string
  quietHoursStart?: string | null
  quietHoursEnd?: string | null
}

// ── API client methods ─────────────────────────────────────────────────────────

export const locationsApi = {
  /**
   * Upserts the authenticated user's last known location and alert opt-in.
   * Maps to PUT /api/me/location.
   */
  upsertLocation: (payload: UserLocationPayload) =>
    apiClient.put<void>('/me/location', payload).then(() => undefined),
}
