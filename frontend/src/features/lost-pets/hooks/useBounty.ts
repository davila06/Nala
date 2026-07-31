import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { bountyApi } from '../api/bountyApi'

export function useBountyForEvent(lostEventId: string) {
  return useQuery({
    queryKey: ['bounty', lostEventId],
    queryFn: () => bountyApi.getForEvent(lostEventId),
    enabled: !!lostEventId,
    staleTime: 30_000,
  })
}

export function useCreateBounty() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ lostEventId, amount }: { lostEventId: string; amount: number }) =>
      bountyApi.create(lostEventId, amount),
    onSuccess: (_data, { lostEventId }) => {
      void queryClient.invalidateQueries({ queryKey: ['bounty', lostEventId] })
    },
  })
}

export function useConfirmBountyDeposit() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (depositReference: string) => bountyApi.confirmDeposit(depositReference),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bounty'] })
    },
  })
}
