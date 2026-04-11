import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { notificationsApi } from '../api/notificationsApi'

const NOTIFICATIONS_KEY = ['notifications'] as const

export function useNotifications(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [...NOTIFICATIONS_KEY, page, pageSize],
    queryFn: () => notificationsApi.getMyNotifications(page, pageSize),
    staleTime: 30_000,
    // Polling every 30 seconds for real-time-ish updates
    refetchInterval: 30_000,
  })
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY }),
  })
}

export function useMarkAllRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY }),
  })
}

export function useRespondResolveCheck() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, foundAtHome }: { id: string; foundAtHome: boolean }) =>
      notificationsApi.respondResolveCheck(id, foundAtHome),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY }),
  })
}

/** Derived helper — returns unread count from cached query data */
export function useUnreadCount() {
  return useQuery({
    queryKey: [...NOTIFICATIONS_KEY, 1, 1],
    queryFn: () => notificationsApi.getMyNotifications(1, 1),
    staleTime: 30_000,
    refetchInterval: 30_000,
    select: (data) => data.totalCount,
  })
}
