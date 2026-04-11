import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type LostPetStatus = 'Active' | 'Reunited' | 'Cancelled'

export interface LostPetEvent {
  id: string
  petId: string
  ownerId: string
  status: LostPetStatus
  description: string | null
  publicMessage: string | null
  lastSeenLat: number | null
  lastSeenLng: number | null
  lastSeenAt: string
  reportedAt: string
  resolvedAt: string | null
  recentPhotoUrl: string | null
  contactName: string | null
  rewardAmount: number | null
  rewardNote: string | null
}

export interface LostPetContactDto {
  lostEventId: string
  contactName: string | null
  contactPhone: string | null
}

export interface ReportLostPetPayload {
  petId: string
  description?: string | null
  publicMessage?: string | null
  lastSeenLat?: number | null
  lastSeenLng?: number | null
  lastSeenAt?: string
  recentPhoto?: File | null
  contactName?: string | null
  contactPhone?: string | null
  rewardAmount?: number | null
  rewardNote?: string | null
}

export interface UpdateLostPetStatusPayload {
  newStatus: LostPetStatus
  confirmedSightingId?: string | null
}

// ── API client methods ─────────────────────────────────────────────────────────

export const lostPetsApi = {
  reportLost: (payload: ReportLostPetPayload) => {
    const form = new FormData()
    form.append('petId', payload.petId)
    if (payload.description != null) form.append('description', payload.description)
    if (payload.publicMessage != null) form.append('publicMessage', payload.publicMessage)
    if (payload.lastSeenLat != null) form.append('lastSeenLat', String(payload.lastSeenLat))
    if (payload.lastSeenLng != null) form.append('lastSeenLng', String(payload.lastSeenLng))
    if (payload.lastSeenAt != null) form.append('lastSeenAt', payload.lastSeenAt)
    if (payload.recentPhoto != null) form.append('recentPhoto', payload.recentPhoto)
    if (payload.contactName != null) form.append('contactName', payload.contactName)
    if (payload.contactPhone != null) form.append('contactPhone', payload.contactPhone)
    if (payload.rewardAmount != null) form.append('rewardAmount', String(payload.rewardAmount))
    if (payload.rewardNote != null) form.append('rewardNote', payload.rewardNote)
    return apiClient.post<{ id: string }>('/lost-pets', form).then((r) => r.data)
  },

  getById: (id: string) =>
    apiClient.get<LostPetEvent>(`/lost-pets/${id}`).then((r) => r.data),

  getActiveByPet: (petId: string) =>
    apiClient
      .get<LostPetEvent | null>(`/lost-pets/by-pet/${petId}`)
      .then((r) => r.data),

  updateStatus: (id: string, payload: UpdateLostPetStatusPayload) =>
    apiClient.put(`/lost-pets/${id}/status`, payload).then(() => undefined),

  getContact: (lostEventId: string) =>
    apiClient.get<LostPetContactDto>(`/lost-pets/${lostEventId}/contact`).then((r) => r.data),
}
