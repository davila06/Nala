import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export interface UserLocationPayload {
  lat: number
  lng: number
  receiveNearbyAlerts: boolean
  /** Quiet-hours window start in Costa Rica local time (HH:mm). Null = no window. */
  quietHoursStart?: string | null
  /** Quiet-hours window end in Costa Rica local time (HH:mm). Null = no window. */
  quietHoursEnd?: string | null
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
