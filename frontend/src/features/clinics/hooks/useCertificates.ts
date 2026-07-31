import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { certificateApi, type IssueCertificateRequest } from '../api/certificateApi'

export function useCertificatesForPet(petId: string) {
  return useQuery({
    queryKey: ['certificates', petId],
    queryFn: () => certificateApi.getForPet(petId),
    enabled: !!petId,
  })
}

export function useIssueCertificate() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: IssueCertificateRequest) => certificateApi.issue(request),
    onSuccess: (_data, req) => {
      void queryClient.invalidateQueries({ queryKey: ['certificates', req.petId] })
    },
  })
}
