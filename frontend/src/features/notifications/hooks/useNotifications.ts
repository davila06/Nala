import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notificationsApi } from "../api/notificationsApi";

const NOTIFICATIONS_KEY = ["notifications"] as const;

export function useNotifications(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [...NOTIFICATIONS_KEY, page, pageSize],
    queryFn: () => notificationsApi.getMyNotifications(page, pageSize),
    staleTime: 30_000,
    refetchInterval: 30_000,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY }),
  });
}

export function useMarkAllRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY }),
  });
}

export function useRespondResolveCheck() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, foundAtHome }: { id: string; foundAtHome: boolean }) =>
      notificationsApi.respondResolveCheck(id, foundAtHome),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY }),
  });
}

/**
 * Returns unread count from the main notifications query cache — no extra request.
 * Falls back to a direct query when the main cache is cold (e.g. badge on a page
 * that doesn't mount useNotifications).
 */
export function useUnreadCount() {
  return useQuery({
    queryKey: [...NOTIFICATIONS_KEY, 1, 20],
    queryFn: () => notificationsApi.getMyNotifications(1, 20),
    staleTime: 30_000,
    refetchInterval: 30_000,
    select: (data) => data.unreadCount ?? 0,
  });
}
