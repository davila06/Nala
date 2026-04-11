import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type ContributorBadge = 'None' | 'Helper' | 'Rescuer' | 'Guardian' | 'Legend'

export interface ContributorScoreDto {
  userId: string
  ownerName: string
  reunificationCount: number
  badge: ContributorBadge
  totalPoints: number
  updatedAt: string
}

// ── API client methods ─────────────────────────────────────────────────────────

export const incentivesApi = {
  getLeaderboard: (take = 10) =>
    apiClient
      .get<ContributorScoreDto[]>('/incentives/leaderboard', { params: { take } })
      .then((r) => r.data),

  getMyScore: () =>
    apiClient
      .get<ContributorScoreDto | null>('/incentives/my-score')
      .then((r) => r.data),
}
