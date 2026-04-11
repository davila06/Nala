import { apiClient } from '@/shared/lib/apiClient'

export interface RecoveryRatesResponse {
  totalReports: number
  recoveredCount: number
  recoveryRate: number
  medianRecoveryHours: number | null
  medianDistanceMeters: number | null
  p90DistanceMeters: number | null
  dataPoints: number
}

export interface RecoveryOverviewByCanton {
  canton: string
  totalReports: number
  recoveredCount: number
  recoveryRate: number
}

export interface RecoveryOverviewBySpecies {
  species: string
  recoveredCount: number
  medianRecoveryHours: number | null
}

export interface RecoveryOverviewResponse {
  totalReports: number
  recoveredCount: number
  overallRecoveryRate: number
  cantonRecovery: RecoveryOverviewByCanton[]
  speciesRecovery: RecoveryOverviewBySpecies[]
}

export interface RecoveryRatesFilters {
  species?: string | null
  breed?: string | null
  canton?: string | null
}

export const recoveryStatsApi = {
  getRecoveryRates: (filters: RecoveryRatesFilters) =>
    apiClient
      .get<RecoveryRatesResponse>('/public/stats/recovery-rates', {
        params: {
          species: filters.species ?? undefined,
          breed: filters.breed ?? undefined,
          canton: filters.canton ?? undefined,
        },
      })
      .then((r) => r.data),

  getRecoveryOverview: () =>
    apiClient
      .get<RecoveryOverviewResponse>('/public/stats/recovery-overview')
      .then((r) => r.data),
}
