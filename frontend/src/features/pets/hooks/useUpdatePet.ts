import { useMutation, useQueryClient } from '@tanstack/react-query'
import { petsApi, type UpdatePetRequest } from '../api/petsApi'
import { PETS_QUERY_KEY } from './usePets'

export const useUpdatePet = (petId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdatePetRequest) => petsApi.updatePet(petId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PETS_QUERY_KEY })
      void queryClient.invalidateQueries({ queryKey: [...PETS_QUERY_KEY, petId] })
    },
  })
}
