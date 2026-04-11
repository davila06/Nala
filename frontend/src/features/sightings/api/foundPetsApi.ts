import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type PetSpecies = 'Dog' | 'Cat' | 'Bird' | 'Rabbit' | 'Other'

export interface MatchCandidate {
  lostPetEventId: string
  petId: string
  petName: string
  petPhotoUrl: string | null
  lastSeenLat: number | null
  lastSeenLng: number | null
  lastSeenAt: string
  scorePercent: number
}

export interface ReportFoundPetResult {
  reportId: string
  candidates: MatchCandidate[]
}

export interface ReportFoundPetPayload {
  foundSpecies: PetSpecies
  breedEstimate?: string | null
  colorDescription?: string | null
  sizeEstimate?: string | null
  foundLat: number
  foundLng: number
  contactName: string
  contactPhone: string
  note?: string | null
  photo?: File | null
}

export interface FoundPetReportDto {
  id: string
  foundSpecies: string
  breedEstimate: string | null
  colorDescription: string | null
  sizeEstimate: string | null
  foundLat: number
  foundLng: number
  photoUrl: string | null
  note: string | null
  status: string
  matchScore: number | null
  reportedAt: string
}

// ── API client ────────────────────────────────────────────────────────────────

export const foundPetsApi = {
  reportFoundPet: (payload: ReportFoundPetPayload): Promise<ReportFoundPetResult> => {
    const form = new FormData()
    form.append('FoundSpecies', payload.foundSpecies)
    form.append('FoundLat', String(payload.foundLat))
    form.append('FoundLng', String(payload.foundLng))
    form.append('ContactName', payload.contactName)
    form.append('ContactPhone', payload.contactPhone)
    if (payload.breedEstimate) form.append('BreedEstimate', payload.breedEstimate)
    if (payload.colorDescription) form.append('ColorDescription', payload.colorDescription)
    if (payload.sizeEstimate) form.append('SizeEstimate', payload.sizeEstimate)
    if (payload.note) form.append('Note', payload.note)
    if (payload.photo) form.append('Photo', payload.photo)

    return apiClient
      .post<ReportFoundPetResult>('/api/found-pets', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data)
  },

  getActiveFoundPets: (maxResults = 50): Promise<FoundPetReportDto[]> =>
    apiClient
      .get<FoundPetReportDto[]>('/api/found-pets/active', { params: { maxResults } })
      .then((r) => r.data),
}
