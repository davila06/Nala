import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type NotificationType =
  | 'LostPetAlert'
  | 'PetReunited'
  | 'SightingAlert'
  | 'SystemMessage'
  | 'ResolveCheck'
  | 'StaleReportReminder'
  | 'VerifiedAllyAlert'
  | 'ChatMessage'
  | 'FraudAlert'
  | 'FoundPetMatch'
  | 'CustodyStarted'
  | 'CustodyClosed'

export interface NotificationItem {
  id: string
  type: NotificationType
  title: string
  body: string
  isRead: boolean
  relatedEntityId: string | null
  createdAt: string
}

export interface PagedNotifications {
  items: NotificationItem[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

// ── API client methods ─────────────────────────────────────────────────────────

export const notificationsApi = {
  getMyNotifications: (page = 1, pageSize = 20) =>
    apiClient
      .get<PagedNotifications>(`/notifications?page=${page}&pageSize=${pageSize}`)
      .then((r) => r.data),

  markAsRead: (id: string) =>
    apiClient.put(`/notifications/${id}/read`, {}).then(() => undefined),

  markAllAsRead: () =>
    apiClient.put('/notifications/read-all', {}).then(() => undefined),

  respondResolveCheck: (id: string, foundAtHome: boolean) =>
    apiClient
      .post(`/notifications/${id}/resolve-check-response`, { foundAtHome })
      .then(() => undefined),
}
