import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export interface GenerateCodeResult {
  code: string
  expiresInHours: number
}

export interface VerifyCodeResult {
  verified: boolean
}

// ── API client ─────────────────────────────────────────────────────────────────

export const handoverApi = {
  generateCode: (lostPetEventId: string): Promise<GenerateCodeResult> =>
    apiClient
      .post<GenerateCodeResult>(`/lost-pets/${lostPetEventId}/handover/code`)
      .then((r) => r.data),

  verifyCode: (lostPetEventId: string, code: string): Promise<VerifyCodeResult> =>
    apiClient
      .post<VerifyCodeResult>(`/lost-pets/${lostPetEventId}/handover/verify`, { code })
      .then((r) => r.data),
}
