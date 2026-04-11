import { useQuery } from '@tanstack/react-query'
import { incentivesApi } from '../api/incentivesApi'

const LEADERBOARD_KEY = (take: number) => ['incentives', 'leaderboard', take] as const
const MY_SCORE_KEY = () => ['incentives', 'my-score'] as const

export function useLeaderboard(take = 10) {
  return useQuery({
    queryKey: LEADERBOARD_KEY(take),
    queryFn: () => incentivesApi.getLeaderboard(take),
    staleTime: 60_000,
  })
}

export function useMyScore(enabled = true) {
  return useQuery({
    queryKey: MY_SCORE_KEY(),
    queryFn: () => incentivesApi.getMyScore(),
    staleTime: 30_000,
    enabled,
  })
}
