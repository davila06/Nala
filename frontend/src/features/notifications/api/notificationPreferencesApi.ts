import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export interface NotificationPreferences {
  enablePreventiveAlerts: boolean
}

// ── API client methods ─────────────────────────────────────────────────────────

export const notificationPreferencesApi = {
  getPreferences: (): Promise<NotificationPreferences> =>
    apiClient
      .get<NotificationPreferences>('/notifications/preferences')
      .then((r) => r.data),

  updatePreferences: (preferences: NotificationPreferences): Promise<void> =>
    apiClient
      .put('/notifications/preferences', preferences)
      .then(() => undefined),
}
