import { apiClient } from '@/shared/lib/apiClient'
import type { LostPetEvent } from './lostPetsApi'
import type { SightingDetail } from '@/features/sightings/api/sightingsApi'

// ── Types ─────────────────────────────────────────────────────────────────────

export interface NearbyAlertSummary {
  notificationId: string
  sentAt: string
  title: string
}

export interface CaseRoomDto {
  event: LostPetEvent
  sightings: SightingDetail[]
  nearbyAlerts: NearbyAlertSummary[]
  totalNearbyAlertsDispatched: number
  generatedAt: string
}

// ── API ───────────────────────────────────────────────────────────────────────

export const caseRoomApi = {
  getCaseRoom: (lostEventId: string) =>
    apiClient
      .get<CaseRoomDto>(`/lost-pets/${lostEventId}/case`)
      .then((r) => r.data),
}
