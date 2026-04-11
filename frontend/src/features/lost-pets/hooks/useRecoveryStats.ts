import { useQuery } from '@tanstack/react-query'
import { recoveryStatsApi, type RecoveryRatesFilters } from '../api/recoveryStatsApi'

export function useRecoveryRates(filters: RecoveryRatesFilters) {
  return useQuery({
    queryKey: ['recovery-rates', filters.species ?? null, filters.breed ?? null, filters.canton ?? null],
    queryFn: () => recoveryStatsApi.getRecoveryRates(filters),
    staleTime: 60_000,
  })
}

export function useRecoveryOverview() {
  return useQuery({
    queryKey: ['recovery-overview'],
    queryFn: () => recoveryStatsApi.getRecoveryOverview(),
    staleTime: 60_000,
  })
}
