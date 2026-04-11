import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type BroadcastChannel = 'Email' | 'WhatsApp' | 'Telegram' | 'Facebook'
export type BroadcastStatus = 'Pending' | 'Sent' | 'Failed' | 'Skipped'

export interface BroadcastAttemptDto {
  id: string
  lostPetEventId: string
  channel: BroadcastChannel
  status: BroadcastStatus
  externalId: string | null
  trackingUrl: string | null
  trackingClicks: number
  errorMessage: string | null
  createdAt: string
  sentAt: string | null
}

export interface BroadcastStatusDto {
  lostPetEventId: string
  attempts: BroadcastAttemptDto[]
  sentCount: number
  failedCount: number
  skippedCount: number
  totalClicks: number
}

// ── API client methods ─────────────────────────────────────────────────────────

export const broadcastApi = {
  /** Trigger multi-channel broadcast for the given lost-pet event. */
  trigger: (lostEventId: string) =>
    apiClient
      .post<BroadcastAttemptDto[]>(`/broadcast/lost-pets/${lostEventId}`)
      .then((r) => r.data),

  /** Get the current broadcast status (all channel attempts) for a lost-pet event. */
  getStatus: (lostEventId: string) =>
    apiClient
      .get<BroadcastStatusDto>(`/broadcast/lost-pets/${lostEventId}`)
      .then((r) => r.data),
}
