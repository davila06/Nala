import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { sightingsApi, type ReportSightingPayload } from '../api/sightingsApi'

export function useSightingsByPet(petId: string) {
  return useQuery({
    queryKey: ['sightings', 'pet', petId],
    queryFn: () => sightingsApi.getSightingsByPet(petId),
    enabled: !!petId,
    staleTime: 30_000,
  })
}

export function useReportSighting(petId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: ReportSightingPayload) =>
      sightingsApi.reportSighting(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['sightings', 'pet', petId] })
    },
  })
}
