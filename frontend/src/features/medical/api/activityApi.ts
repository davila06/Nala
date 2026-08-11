import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type ActivityType = "Walk" | "Run" | "Play" | "Swim" | "Training" | "Other";
export type ActivitySource = "Manual" | "Tractive";

export interface ActivityLogDto {
  id: string;
  date: string; // DateOnly YYYY-MM-DD
  type: ActivityType;
  durationMinutes: number;
  distanceMeters: number | null;
  notes: string | null;
  source: ActivitySource;
}

export interface ActivityWeekSummaryDto {
  weekStart: string;
  totalMinutes: number;
  totalDistanceMeters: number | null;
  daysActive: number;
}

export interface ActivityBenchmarkDto {
  dailyMinutesMin: number;
  dailyMinutesMax: number;
  dailyKmMin: number;
  dailyKmMax: number;
  energyLevel: "low" | "medium" | "high";
}

export interface ActivitySummaryDto {
  logs: ActivityLogDto[];
  weeklyTotals: ActivityWeekSummaryDto[];
  benchmark: ActivityBenchmarkDto | null;
  streakDays: number;
  bestStreakDays: number;
}

export interface LogActivityPayload {
  date: string;
  type: ActivityType;
  durationMinutes: number;
  distanceMeters?: number;
  notes?: string;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const activityApi = {
  getLogs: (petId: string, from?: string, to?: string): Promise<ActivitySummaryDto> =>
    apiClient
      .get<ActivitySummaryDto>(`/pets/${petId}/activity`, { params: { from, to } })
      .then((r) => r.data),

  logActivity: (petId: string, payload: LogActivityPayload): Promise<ActivityLogDto> =>
    apiClient
      .post<ActivityLogDto>(`/pets/${petId}/activity`, payload)
      .then((r) => r.data),

  deleteActivity: (petId: string, activityId: string): Promise<void> =>
    apiClient.delete(`/pets/${petId}/activity/${activityId}`).then(() => undefined),
};
