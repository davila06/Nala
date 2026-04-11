import { apiClient } from '@/shared/lib/apiClient'

export type PetSpecies = 'Dog' | 'Cat' | 'Bird' | 'Rabbit' | 'Other'

export interface FosterProfile {
  userId: string
  fullName: string
  homeLat: number
  homeLng: number
  acceptedSpecies: PetSpecies[]
  sizePreference: string | null
  maxDays: number
  isAvailable: boolean
  availableUntil: string | null
  totalFostersCompleted: number
}

export interface UpsertFosterProfilePayload {
  homeLat: number
  homeLng: number
  acceptedSpecies: PetSpecies[]
  sizePreference?: string | null
  maxDays: number
  isAvailable: boolean
  availableUntil?: string | null
}

export interface FosterSuggestion {
  userId: string
  volunteerName: string
  distanceMetres: number
  distanceLabel: string
  sizePreference: string | null
  maxDays: number
  speciesMatch: boolean
}

export const fostersApi = {
  getMyProfile: () => apiClient.get<FosterProfile>('/fosters/me').then((r) => r.data),

  upsertMyProfile: (payload: UpsertFosterProfilePayload) =>
    apiClient.put('/fosters/me', {
      homeLat: payload.homeLat,
      homeLng: payload.homeLng,
      acceptedSpecies: payload.acceptedSpecies,
      sizePreference: payload.sizePreference ?? null,
      maxDays: payload.maxDays,
      isAvailable: payload.isAvailable,
      availableUntil: payload.availableUntil ?? null,
    }),

  getSuggestionsFromFoundReport: (foundReportId: string, maxResults = 3) =>
    apiClient
      .get<FosterSuggestion[]>(`/fosters/suggestions/from-found-report/${foundReportId}`, {
        params: { maxResults },
      })
      .then((r) => r.data),
}
