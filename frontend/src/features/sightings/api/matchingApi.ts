import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export interface VisualMatchResult {
  petId: string
  lostEventId: string
  petName: string
  species: string
  photoUrl: string | null
  /** Combined cosine + geo score in [0, 1]. Higher is better. */
  similarityScore: number
  /** Distance in km from probe location to pet's last-seen location. Null when location was not provided. */
  distanceKm: number | null
  publicProfileUrl: string
}

export interface VisualMatchPayload {
  photo: File
  lat?: number
  lng?: number
}

// ── API client ─────────────────────────────────────────────────────────────────

export const matchingApi = {
  visualMatch: (payload: VisualMatchPayload): Promise<VisualMatchResult[]> => {
    const form = new FormData()
    form.append('Photo', payload.photo)
    if (payload.lat != null) form.append('lat', String(payload.lat))
    if (payload.lng != null) form.append('lng', String(payload.lng))

    return apiClient
      .post<VisualMatchResult[]>('/sightings/visual-match', form)
      .then((r) => r.data)
  },

  /** Auto-match using the photo already stored on a persisted sighting. */
  visualMatchBySightingId: (
    sightingId: string,
    lat?: number,
    lng?: number,
  ): Promise<VisualMatchResult[]> => {
    const params: Record<string, string> = {}
    if (lat != null) params['lat'] = String(lat)
    if (lng != null) params['lng'] = String(lng)

    return apiClient
      .post<VisualMatchResult[]>(`/sightings/${sightingId}/visual-match`, null, { params })
      .then((r) => r.data)
  },
}
