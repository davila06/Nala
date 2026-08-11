import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { neighborApi } from "../api/neighborApi";

const QUERY_KEY = ["neighbor-status"];

export function useNeighborStatus() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: neighborApi.getStatus,
    staleTime: 5 * 60_000,
    retry: false,
  });
}

export function useEnrollNeighbor() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      phone,
      radiusMeters,
    }: {
      phone: string;
      radiusMeters: number;
    }) => neighborApi.enroll(phone, radiusMeters),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: QUERY_KEY });
    },
  });
}

export function useUpdateNeighborSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      radiusMeters,
      isActive,
    }: {
      radiusMeters: number;
      isActive: boolean;
    }) => neighborApi.updateSettings(radiusMeters, isActive),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: QUERY_KEY });
    },
  });
}

export function useNeighborCountInArea(
  lat?: number,
  lng?: number,
  radius = 500,
) {
  return useQuery({
    queryKey: ["neighbor-count", lat, lng, radius],
    queryFn: () => neighborApi.getCountInArea(lat!, lng!, radius),
    enabled: lat != null && lng != null,
    staleTime: 5 * 60_000,
  });
}
