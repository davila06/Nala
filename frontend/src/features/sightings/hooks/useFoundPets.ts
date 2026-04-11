import { useMutation, useQuery } from '@tanstack/react-query'
import { foundPetsApi, type ReportFoundPetPayload } from '../api/foundPetsApi'

export function useReportFoundPet() {
  return useMutation({
    mutationFn: (payload: ReportFoundPetPayload) =>
      foundPetsApi.reportFoundPet(payload),
  })
}

export function useActiveFoundPets(maxResults = 50) {
  return useQuery({
    queryKey: ['found-pets', 'active', maxResults],
    queryFn: () => foundPetsApi.getActiveFoundPets(maxResults),
    staleTime: 60_000,
  })
}
