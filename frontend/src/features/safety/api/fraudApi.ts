import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type FraudContext = 'PublicProfile' | 'ChatMessage' | 'PhoneContact' | 'Other'

export interface ReportFraudPayload {
  context: FraudContext
  relatedEntityId?: string | null
  targetUserId?: string | null
  description?: string | null
}

export interface ReportFraudResult {
  message: string
  /** 'None' | 'Elevated' | 'High' | 'Critical' */
  suspicionLevel: string
}

// ── API client ─────────────────────────────────────────────────────────────────

export const fraudApi = {
  reportFraud: (payload: ReportFraudPayload): Promise<ReportFraudResult> =>
    apiClient
      .post<ReportFraudResult>('/fraud-reports', payload)
      .then((r) => r.data),
}
