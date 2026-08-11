import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export interface NeighborStatusDto {
  isEnrolled: boolean;
  isActive: boolean;
  phone: string | null;
  radiusMeters: number;
  neighborsInRange: number;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const neighborApi = {
  getStatus: (): Promise<NeighborStatusDto> =>
    apiClient.get<NeighborStatusDto>("/neighbor/status").then((r) => r.data),

  enroll: (phone: string, radiusMeters: number): Promise<NeighborStatusDto> =>
    apiClient
      .post<NeighborStatusDto>("/neighbor/enroll", { phone, radiusMeters })
      .then((r) => r.data),

  updateSettings: (radiusMeters: number, isActive: boolean): Promise<void> =>
    apiClient
      .put("/neighbor/settings", { radiusMeters, isActive })
      .then(() => undefined),

  getCountInArea: (
    lat: number,
    lng: number,
    radius = 500,
  ): Promise<{ count: number }> =>
    apiClient
      .get<{ count: number }>("/public/neighbor-count", {
        params: { lat, lng, radius },
      })
      .then((r) => r.data),
};
