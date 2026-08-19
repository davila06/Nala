import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { clinicsApi } from "../api/clinicsApi";

export function usePublicClinics(lat?: number, lng?: number, enabled = true) {
  return useQuery({
    queryKey: ["clinics", "public", lat, lng],
    queryFn: () => clinicsApi.getPublicClinics(lat, lng),
    staleTime: 60_000,
    enabled,
  });
}

export function useClinicScanStats(year?: number, month?: number) {
  const now = new Date();
  return useQuery({
    queryKey: [
      "clinics",
      "stats",
      year ?? now.getFullYear(),
      month ?? now.getMonth() + 1,
    ],
    queryFn: () => clinicsApi.getScanStats(year, month),
    staleTime: 30_000,
  });
}

export function useUploadClinicLogo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => clinicsApi.uploadLogo(file),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["my-clinic"] });
    },
  });
}

export function useClinicApiKeys() {
  return useQuery({
    queryKey: ["clinics", "api-keys"],
    queryFn: clinicsApi.getApiKeys,
    staleTime: 30_000,
  });
}

export function useCreateClinicApiKey() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (label: string) => clinicsApi.createApiKey(label),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "api-keys"] });
    },
  });
}

export function useRevokeClinicApiKey() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (keyId: string) => clinicsApi.revokeApiKey(keyId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "api-keys"] });
    },
  });
}
export function useClinicNearbyAlerts(radiusKm = 15) {
  return useQuery({
    queryKey: ["clinics", "nearby-alerts", radiusKm],
    queryFn: () => clinicsApi.getNearbyAlerts(radiusKm),
    staleTime: 30_000,
    refetchInterval: 60_000,
  });
}

export function useClinicVisibilityStats(days = 30) {
  return useQuery({
    queryKey: ["clinics", "visibility-stats", days],
    queryFn: () => clinicsApi.getVisibilityStats(days),
    staleTime: 300_000,
    retry: (count, err: { response?: { status?: number } }) =>
      err?.response?.status !== 402 && count < 2,
  });
}

export function useEmergencyVets(lat?: number, lng?: number, radiusKm = 30) {
  return useQuery({
    queryKey: ["clinics", "emergency", lat, lng, radiusKm],
    queryFn: () => clinicsApi.getEmergencyVets(lat, lng, radiusKm),
    staleTime: 5 * 60_000,
    enabled: true,
  });
}
