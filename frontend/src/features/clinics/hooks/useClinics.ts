import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { clinicsApi, type UpdateClinicProfileRequest } from "../api/clinicsApi";

export function usePublicClinics(
  lat?: number,
  lng?: number,
  enabled = true,
  options?: {
    search?: string;
    emergencyOnly?: boolean;
    page?: number;
    pageSize?: number;
  },
) {
  return useQuery({
    queryKey: ["clinics", "public", lat, lng, options],
    queryFn: () => clinicsApi.getPublicClinics(lat, lng, options),
    staleTime: 60_000,
    enabled,
  });
}

export function usePublicClinicProfile(clinicId: string) {
  return useQuery({
    queryKey: ["clinics", "public-profile", clinicId],
    queryFn: () => clinicsApi.getPublicProfile(clinicId),
    staleTime: 60_000,
    enabled: Boolean(clinicId),
  });
}

/** Search active clinics by name or license number (min 2 chars) to authorize medical access. */
export function useClinicAccessSearch(query: string) {
  const trimmed = query.trim();
  return useQuery({
    queryKey: ["clinics", "search", trimmed],
    queryFn: () => clinicsApi.searchForAccess(trimmed),
    staleTime: 30_000,
    enabled: trimmed.length >= 2,
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

export function useUpdateClinicProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateClinicProfileRequest) =>
      clinicsApi.updateMyProfile(payload),
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
    mutationFn: ({ label, scopes }: { label: string; scopes?: string[] }) =>
      clinicsApi.createApiKey(label, scopes),
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
