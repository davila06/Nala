import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type SearchZoneStatus = 'Free' | 'Taken' | 'Clear'

export interface SearchZone {
  id: string
  lostPetEventId: string
  label: string
  geoJsonPolygon: string
  status: SearchZoneStatus
  assignedToUserId: string | null
  takenAt: string | null
  clearedAt: string | null
}

export interface ActivateSearchCoordinationResponse {
  lostPetEventId: string
  zoneCount: number
}

// ── API client methods ─────────────────────────────────────────────────────────

export const searchCoordinationApi = {
  activateCoordination: (lostEventId: string): Promise<ActivateSearchCoordinationResponse> =>
    apiClient
      .post<ActivateSearchCoordinationResponse>(`/search-coordination/${lostEventId}/activate`)
      .then((r) => r.data),

  getZones: (lostEventId: string): Promise<SearchZone[]> =>
    apiClient
      .get<SearchZone[]>(`/search-coordination/${lostEventId}/zones`)
      .then((r) => r.data),
}
