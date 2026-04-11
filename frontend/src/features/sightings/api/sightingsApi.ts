import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export interface SightingDetail {
  id: string
  petId: string
  lostPetEventId: string | null
  lat: number
  lng: number
  photoUrl: string | null
  note: string | null
  sightedAt: string
  reportedAt: string
  priorityScore: number
  priorityBadge: 'Urgent' | 'Validate' | 'Observe'
  recommendedAction: string
}

export interface ReportSightingPayload {
  petId: string
  lat: number
  lng: number
  note?: string | null
  sightedAt?: string
  photo?: File | null
}

// ── API client ─────────────────────────────────────────────────────────────────

export const sightingsApi = {
  reportSighting: (payload: ReportSightingPayload) => {
    const form = new FormData()
    form.append('petId', payload.petId)
    form.append('lat', String(payload.lat))
    form.append('lng', String(payload.lng))
    if (payload.note) form.append('note', payload.note)
    if (payload.sightedAt) form.append('sightedAt', payload.sightedAt)
    if (payload.photo) form.append('Photo', payload.photo)

    return apiClient
      .post<{ id: string }>('/sightings', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data)
  },

  getSightingsByPet: (petId: string) =>
    apiClient
      .get<SightingDetail[]>(`/sightings/pet/${petId}`)
      .then((r) => r.data),
}
