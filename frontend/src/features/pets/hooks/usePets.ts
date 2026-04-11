import { useQuery } from '@tanstack/react-query'
import { petsApi } from '../api/petsApi'

export const PETS_QUERY_KEY = ['pets'] as const

export const usePets = () =>
  useQuery({
    queryKey: PETS_QUERY_KEY,
    queryFn: () => petsApi.getMyPets(),
    staleTime: 30_000,
  })

export const usePetDetail = (id: string) =>
  useQuery({
    queryKey: [...PETS_QUERY_KEY, id] as const,
    queryFn: () => petsApi.getPetDetail(id),
    staleTime: 30_000,
  })

export const usePublicPetProfile = (id: string) =>
  useQuery({
    queryKey: ['public-pet', id] as const,
    queryFn: () => petsApi.getPublicProfile(id),
    staleTime: 60_000,
  })

export const usePetScanHistory = (id: string) =>
  useQuery({
    queryKey: [...PETS_QUERY_KEY, id, 'scan-history'] as const,
    queryFn: () => petsApi.getScanHistory(id),
    staleTime: 30_000,
    enabled: id.length > 0,
  })
