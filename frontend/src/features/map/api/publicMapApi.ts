import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export interface PublicMapEvent {
  id: string
  /** "LostPet" | "Sighting" */
  eventType: 'LostPet' | 'Sighting'
  petId: string
  lat: number
  lng: number
  photoUrl: string | null
  occurredAt: string
}

export interface MapBBox {
  north: number
  south: number
  east: number
  west: number
}

/** A single point along a pet's chronological sighting trail. */
export interface SightingPoint {
  lat: number
  lng: number
  occurredAt: string
  sequenceIndex: number
}

/**
 * Predictive movement projection for a lost-pet event.
 * When `hasEnoughData` is false the projected fields are null and only
 * `trailPoints` (if any sightings exist) and `explanationText` are populated.
 */
export interface MovementPrediction {
  hasEnoughData: boolean
  projectedLat: number | null
  projectedLng: number | null
  /** Uncertainty circle radius in metres. */
  radiusMeters: number | null
  /** 5–80 % confidence estimate. */
  confidencePercent: number | null
  trailPoints: SightingPoint[]
  /** Human-readable explanation in Spanish. */
  explanationText: string
}

// ── API client ─────────────────────────────────────────────────────────────────

export const publicMapApi = {
  getMapEvents: (bbox: MapBBox) =>
    apiClient
      .get<PublicMapEvent[]>('/public/map', { params: bbox })
      .then((r) => r.data),

  getMovementPrediction: (lostPetEventId: string) =>
    apiClient
      .get<MovementPrediction>(`/public/movement/${lostPetEventId}`)
      .then((r) => r.data),
}

