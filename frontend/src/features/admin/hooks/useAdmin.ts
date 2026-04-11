import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminApi } from '../api/adminApi'

export function usePendingAllies() {
  return useQuery({
    queryKey: ['admin', 'allies', 'pending'],
    queryFn: adminApi.getPendingAllies,
  })
}

export function usePendingClinics() {
  return useQuery({
    queryKey: ['admin', 'clinics', 'pending'],
    queryFn: adminApi.getPendingClinics,
  })
}

export function useReviewAlly() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, approve }: { userId: string; approve: boolean }) =>
      adminApi.reviewAlly(userId, approve),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'allies', 'pending'] })
    },
  })
}

export function useReviewClinic() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ clinicId, approve }: { clinicId: string; approve: boolean }) =>
      adminApi.reviewClinic(clinicId, approve),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'clinics', 'pending'] })
    },
  })
}
