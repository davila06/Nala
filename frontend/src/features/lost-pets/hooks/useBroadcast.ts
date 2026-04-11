import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { broadcastApi } from '../api/broadcastApi'

const BROADCAST_STATUS_KEY = (lostEventId: string) =>
  ['broadcast-status', lostEventId] as const

export function useBroadcastStatus(lostEventId: string | null) {
  return useQuery({
    queryKey: BROADCAST_STATUS_KEY(lostEventId ?? ''),
    queryFn: () => broadcastApi.getStatus(lostEventId!),
    staleTime: 30_000,
    enabled: lostEventId != null && lostEventId.length > 0,
  })
}

export function useTriggerBroadcast(lostEventId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => broadcastApi.trigger(lostEventId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: BROADCAST_STATUS_KEY(lostEventId),
      })
    },
  })
}
