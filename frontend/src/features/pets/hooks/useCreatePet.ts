import { useMutation, useQueryClient } from '@tanstack/react-query'
import { petsApi, type CreatePetRequest } from '../api/petsApi'
import { PETS_QUERY_KEY } from './usePets'

export const useCreatePet = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreatePetRequest) => petsApi.createPet(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PETS_QUERY_KEY })
    },
  })
}
