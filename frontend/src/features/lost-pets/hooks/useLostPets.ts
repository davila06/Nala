import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { lostPetsApi, type ReportLostPetPayload } from '../api/lostPetsApi'

const LOST_PET_KEY = (petId: string) => ['lost-pet', petId] as const
const LOST_EVENT_KEY = (id: string) => ['lost-event', id] as const
const CONTACT_KEY = (lostEventId: string) => ['lost-pet-contact', lostEventId] as const

export function useActiveLostReport(petId: string) {
  return useQuery({
    queryKey: LOST_PET_KEY(petId),
    queryFn: () => lostPetsApi.getActiveByPet(petId),
    staleTime: 30_000,
    enabled: petId.length > 0,
  })
}

export function useLostEventById(id: string) {
  return useQuery({
    queryKey: LOST_EVENT_KEY(id),
    queryFn: () => lostPetsApi.getById(id),
    staleTime: 30_000,
    enabled: id.length > 0,
  })
}

export function useReportLost() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: ReportLostPetPayload) => lostPetsApi.reportLost(payload),
    onSuccess: (_data, variables) => {
      // Invalidate the active report for this pet and the pet detail (status changed)
      void queryClient.invalidateQueries({ queryKey: LOST_PET_KEY(variables.petId) })
      void queryClient.invalidateQueries({ queryKey: ['pets'] })
      void queryClient.invalidateQueries({ queryKey: ['pet', variables.petId] })
    },
  })
}

export function useUpdateLostPetStatus(lostEventId: string, petId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (newStatus: 'Reunited' | 'Cancelled') =>
      lostPetsApi.updateStatus(lostEventId, { newStatus }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LOST_PET_KEY(petId) })
      void queryClient.invalidateQueries({ queryKey: LOST_EVENT_KEY(lostEventId) })
      void queryClient.invalidateQueries({ queryKey: ['pets'] })
      void queryClient.invalidateQueries({ queryKey: ['pet', petId] })
    },
  })
}

export function useGetLostPetContact(lostEventId: string | null) {
  return useQuery({
    queryKey: CONTACT_KEY(lostEventId ?? ''),
    queryFn: () => lostPetsApi.getContact(lostEventId!),
    enabled: !!lostEventId,
    staleTime: 60_000,
    retry: false,
  })
}
