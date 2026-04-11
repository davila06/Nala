import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { notificationPreferencesApi } from '../api/notificationPreferencesApi'

const PREFERENCES_KEY = ['notification-preferences'] as const

export function useNotificationPreferences() {
  return useQuery({
    queryKey: PREFERENCES_KEY,
    queryFn: () => notificationPreferencesApi.getPreferences(),
    staleTime: 5 * 60_000, // 5 minutes — preferences change rarely
  })
}

export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (enablePreventiveAlerts: boolean) =>
      notificationPreferencesApi.updatePreferences({ enablePreventiveAlerts }),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: PREFERENCES_KEY }),
  })
}
