import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { fostersApi, type FosterProfile, type UpsertFosterProfilePayload } from '../api/fostersApi'

const FOSTER_PROFILE_KEY = ['fosters', 'me'] as const

export function useMyFosterProfile() {
  return useQuery<FosterProfile | null>({
    queryKey: FOSTER_PROFILE_KEY,
    queryFn: async () => {
      try {
        return await fostersApi.getMyProfile()
      } catch (err) {
        if (axios.isAxiosError(err) && err.response?.status === 404) return null
        throw err
      }
    },
    retry: false,
  })
}

export function useUpsertMyFosterProfile() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: UpsertFosterProfilePayload) => fostersApi.upsertMyProfile(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: FOSTER_PROFILE_KEY })
    },
  })
}

export function useFosterSuggestions(foundReportId: string | null) {
  return useQuery({
    queryKey: ['fosters', 'suggestions', foundReportId] as const,
    queryFn: () => fostersApi.getSuggestionsFromFoundReport(foundReportId!, 3),
    enabled: !!foundReportId,
    staleTime: 30_000,
  })
}
