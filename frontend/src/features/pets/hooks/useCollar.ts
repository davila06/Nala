import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  collarApi,
  type CollarNotificationPreferences,
  type CollarProvider,
  type CollarSafeZonePoint,
} from "../api/collarApi";

export function useCollarStatus(petId: string) {
  return useQuery({
    queryKey: ["collar", petId],
    queryFn: () => collarApi.getStatus(petId),
    enabled: !!petId,
    refetchInterval: 30_000, // poll every 30 s when tab is open
  });
}

export function useCollarHistory(petId: string, hours = 24) {
  return useQuery({
    queryKey: ["collar-history", petId, hours],
    queryFn: () => collarApi.getHistory(petId, hours),
    enabled: !!petId,
    refetchInterval: 60_000, // refresh track every minute
    staleTime: 30_000,
  });
}

export function useRegisterCollar() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      petId,
      provider,
      externalDeviceId,
    }: {
      petId: string;
      provider: CollarProvider;
      externalDeviceId?: string;
    }) => collarApi.register(petId, provider, externalDeviceId),
    onSuccess: (_data, { petId }) => {
      void queryClient.invalidateQueries({ queryKey: ["collar", petId] });
    },
  });
}

export function useCollarConnectivity(collarId: string | undefined) {
  return useQuery({
    queryKey: ["collar-connectivity", collarId],
    queryFn: () => collarApi.getConnectivityStatus(collarId!),
    enabled: !!collarId,
    refetchInterval: 60_000,
  });
}

export function useUpdateCollarNotificationPreferences(petId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      collarId,
      preferences,
    }: {
      collarId: string;
      preferences: CollarNotificationPreferences;
    }) => collarApi.updateNotificationPreferences(collarId, preferences),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["collar", petId] });
      void queryClient.invalidateQueries({ queryKey: ["collar-connectivity"] });
    },
  });
}

export function useCollarAuditLog(collarId: string | undefined) {
  return useQuery({
    queryKey: ["collar-audit-log", collarId],
    queryFn: () => collarApi.getAuditLog(collarId!),
    enabled: !!collarId,
  });
}

export function useGenerateCollarHandoverCode() {
  return useMutation({
    mutationFn: (collarId: string) => collarApi.generateHandoverCode(collarId),
  });
}

export function useCancelCollarHandoverCode() {
  return useMutation({
    mutationFn: (handoverCodeId: string) =>
      collarApi.cancelHandoverCode(handoverCodeId),
  });
}

export function useRedeemCollarHandoverCode() {
  return useMutation({
    mutationFn: ({
      handoverCodeId,
      pin,
    }: {
      handoverCodeId: string;
      pin: string;
    }) => collarApi.redeemHandoverCode(handoverCodeId, pin),
  });
}

export function useCollarLostModeStatus(collarId: string | undefined) {
  return useQuery({
    queryKey: ["collar-lost-mode", collarId],
    queryFn: () => collarApi.getLostModeStatus(collarId!),
    enabled: !!collarId,
    refetchInterval: 30_000,
  });
}

export function useActivateCollarLostMode(petId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (collarId: string) => collarApi.activateLostMode(collarId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["collar-lost-mode"] });
      void queryClient.invalidateQueries({ queryKey: ["collar", petId] });
    },
  });
}

export function useDeactivateCollarLostMode(petId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ collarId, reason }: { collarId: string; reason?: string }) =>
      collarApi.deactivateLostMode(collarId, reason),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["collar-lost-mode"] });
      void queryClient.invalidateQueries({ queryKey: ["collar", petId] });
    },
  });
}

export function useCollarSafeZones(collarId: string | undefined) {
  return useQuery({
    queryKey: ["collar-safe-zones", collarId],
    queryFn: () => collarApi.getSafeZones(collarId!),
    enabled: !!collarId,
  });
}

export function useCreateCollarSafeZone(collarId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      name,
      points,
    }: {
      name: string;
      points: CollarSafeZonePoint[];
    }) => collarApi.createSafeZone(collarId, name, points),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["collar-safe-zones", collarId],
      });
    },
  });
}

export function useUpdateCollarSafeZone(collarId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      zoneId,
      name,
      points,
      enabled,
    }: {
      zoneId: string;
      name: string;
      points: CollarSafeZonePoint[];
      enabled: boolean;
    }) => collarApi.updateSafeZone(zoneId, name, points, enabled),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["collar-safe-zones", collarId],
      });
    },
  });
}

export function useDeleteCollarSafeZone(collarId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (zoneId: string) => collarApi.deleteSafeZone(zoneId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["collar-safe-zones", collarId],
      });
    },
  });
}

export function useCollarLocationHistoryRange(
  collarId: string | undefined,
  from?: string,
  to?: string,
) {
  return useQuery({
    queryKey: ["collar-location-history", collarId, from, to],
    queryFn: () => collarApi.getLocationHistoryRange(collarId!, from, to),
    enabled: !!collarId,
  });
}

export function useCollarLocationHeatmap(
  collarId: string | undefined,
  days = 30,
) {
  return useQuery({
    queryKey: ["collar-location-heatmap", collarId, days],
    queryFn: () => collarApi.getLocationHeatmap(collarId!, days),
    enabled: !!collarId,
  });
}

export function useExportCollarLocationHistory() {
  return useMutation({
    mutationFn: ({
      collarId,
      from,
      to,
    }: {
      collarId: string;
      from?: string;
      to?: string;
    }) => collarApi.exportLocationHistoryCsv(collarId, from, to),
  });
}
