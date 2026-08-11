import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { activityApi, type LogActivityPayload } from "../api/activityApi";

const key = (petId: string) => ["activity", petId];

export function useActivityLogs(petId: string, from?: string, to?: string) {
  return useQuery({
    queryKey: [...key(petId), from, to],
    queryFn: () => activityApi.getLogs(petId, from, to),
    staleTime: 5 * 60_000,
    enabled: !!petId,
    retry: (count, err: { response?: { status?: number } } | unknown) =>
      (err as { response?: { status?: number } })?.response?.status !== 403 &&
      count < 2,
  });
}

export function useLogActivity(petId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: LogActivityPayload) =>
      activityApi.logActivity(petId, payload),
    onSuccess: () => void qc.invalidateQueries({ queryKey: key(petId) }),
  });
}

export function useDeleteActivity(petId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (activityId: string) =>
      activityApi.deleteActivity(petId, activityId),
    onSuccess: () => void qc.invalidateQueries({ queryKey: key(petId) }),
  });
}
