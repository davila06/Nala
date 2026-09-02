import { apiClient } from "@/shared/lib/apiClient";

export type CollarProvider = "Own" | "Tractive" | "Kippy" | "Generic";

export interface CollarDto {
  id: string;
  petId: string;
  provider: CollarProvider;
  externalDeviceId: string | null;
  batteryPercent: number | null;
  lastLat: number | null;
  lastLng: number | null;
  lastSeenAt: string | null;
  isActive: boolean;
  collarTagSerial: string | null;
  isOffline: boolean;
  offlineAlertsEnabled: boolean;
  offlineThresholdMinutes: number;
  batteryAlertsEnabled: boolean;
  batteryAlertThresholdPercent: number;
}

export interface CollarConnectivityStatusDto {
  collarId: string;
  isOffline: boolean;
  lastSeenAt: string | null;
  batteryPercent: number | null;
  offlineAlertsEnabled: boolean;
  offlineThresholdMinutes: number;
  batteryAlertsEnabled: boolean;
  batteryAlertThresholdPercent: number;
}

export interface CollarNotificationPreferences {
  offlineAlertsEnabled: boolean;
  offlineThresholdMinutes: number;
  batteryAlertsEnabled: boolean;
  batteryAlertThresholdPercent: number;
}

export interface CollarAuditEntryDto {
  id: string;
  collarId: string | null;
  serial: string | null;
  userId: string | null;
  event: string;
  details: string;
  createdAt: string;
}

export interface GenerateCollarHandoverCodeResultDto {
  handoverCodeId: string;
  pin: string;
  expiresAt: string;
}

export interface RedeemCollarHandoverCodeResultDto {
  serial: string;
}

export interface CollarLostModeStatusDto {
  isLost: boolean;
  lostModeActivatedAt: string | null;
  lostPetEventId: string | null;
}

export interface CollarSafeZonePoint {
  lat: number;
  lng: number;
}

export interface CollarSafeZoneDto {
  id: string;
  collarId: string;
  name: string;
  polygonJson: string;
  enabled: boolean;
  createdAt: string;
}

export interface CollarLocationPointDto {
  lat: number;
  lng: number;
  accuracy: number | null;
  recordedAt: string;
}

export interface LocationPointDto {
  lat: number;
  lng: number;
  recordedAt: string;
}

export const collarApi = {
  getStatus: (petId: string) =>
    apiClient
      .get<CollarDto | null>(`/collars/pet/${petId}`)
      .then((r) => r.data),

  getHistory: (petId: string, hours = 24, maxPoints = 500) =>
    apiClient
      .get<LocationPointDto[]>(`/collars/pet/${petId}/history`, {
        params: { hours, maxPoints },
      })
      .then((r) => r.data),

  register: (
    petId: string,
    provider: CollarProvider,
    externalDeviceId?: string,
  ) =>
    apiClient
      .post<CollarDto>("/collars", {
        petId,
        provider,
        externalDeviceId: externalDeviceId ?? null,
      })
      .then((r) => r.data),

  // ── CollarTag activation ───────────────────────────────────────────────────

  checkSerial: (serial: string) =>
    apiClient
      .get<{ available: boolean; status: string }>(`/collars/tag/${serial}`)
      .then((r) => r.data),

  activate: (serial: string, petId: string) =>
    apiClient
      .post<{
        collarId: string;
        serial: string;
        collarApiKey: string;
      }>(`/collars/tag/${serial}/activate`, { petId })
      .then((r) => r.data),

  deactivate: (serial: string) =>
    apiClient.delete(`/collars/tag/${serial}/deactivate`).then(() => undefined),

  generateDeviceKey: (collarId: string) =>
    apiClient
      .post<{
        collarId: string;
        collarDeviceKey: string;
      }>(`/collars/${collarId}/generate-key`)
      .then((r) => r.data),

  // ── Connectivity alerts (offline + battery) ────────────────────────────────

  getConnectivityStatus: (collarId: string) =>
    apiClient
      .get<CollarConnectivityStatusDto>(
        `/collars/${collarId}/connectivity-status`,
      )
      .then((r) => r.data),

  updateNotificationPreferences: (
    collarId: string,
    preferences: CollarNotificationPreferences,
  ) =>
    apiClient
      .put<CollarNotificationPreferences>(
        `/collars/${collarId}/notification-preferences`,
        preferences,
      )
      .then((r) => r.data),

  // ── Audit log ───────────────────────────────────────────────────────────────

  getAuditLog: (collarId: string, skip = 0, take = 50) =>
    apiClient
      .get<CollarAuditEntryDto[]>(`/collars/${collarId}/audit-log`, {
        params: { skip, take },
      })
      .then((r) => r.data),

  // ── Handover (ownership transfer) ────────────────────────────────────────

  generateHandoverCode: (collarId: string) =>
    apiClient
      .post<GenerateCollarHandoverCodeResultDto>(
        `/collars/${collarId}/handover/generate`,
      )
      .then((r) => r.data),

  cancelHandoverCode: (handoverCodeId: string) =>
    apiClient
      .post(`/collars/handover/${handoverCodeId}/cancel`)
      .then(() => undefined),

  redeemHandoverCode: (handoverCodeId: string, pin: string) =>
    apiClient
      .post<RedeemCollarHandoverCodeResultDto>("/collars/handover/redeem", {
        handoverCodeId,
        pin,
      })
      .then((r) => r.data),

  // ── Lost mode ─────────────────────────────────────────────────────

  getLostModeStatus: (collarId: string) =>
    apiClient
      .get<CollarLostModeStatusDto>(`/collars/${collarId}/lost-mode-status`)
      .then((r) => r.data),

  activateLostMode: (collarId: string) =>
    apiClient
      .post<{
        lostPetEventId: string;
        wasNewlyCreated: boolean;
      }>(`/collars/${collarId}/lost-mode/activate`)
      .then((r) => r.data),

  deactivateLostMode: (collarId: string, reason?: string) =>
    apiClient
      .post(`/collars/${collarId}/lost-mode/deactivate`, { reason })
      .then(() => undefined),

  // ── Safe zones (geofencing) ──────────────────────────────────────

  getSafeZones: (collarId: string) =>
    apiClient
      .get<CollarSafeZoneDto[]>(`/collars/${collarId}/safe-zones`)
      .then((r) => r.data),

  createSafeZone: (
    collarId: string,
    name: string,
    points: CollarSafeZonePoint[],
  ) =>
    apiClient
      .post<CollarSafeZoneDto>(`/collars/${collarId}/safe-zones`, {
        name,
        polygonJson: JSON.stringify(points),
      })
      .then((r) => r.data),

  updateSafeZone: (
    zoneId: string,
    name: string,
    points: CollarSafeZonePoint[],
    enabled: boolean,
  ) =>
    apiClient
      .put<CollarSafeZoneDto>(`/collars/safe-zones/${zoneId}`, {
        name,
        polygonJson: JSON.stringify(points),
        enabled,
      })
      .then((r) => r.data),

  deleteSafeZone: (zoneId: string) =>
    apiClient.delete(`/collars/safe-zones/${zoneId}`).then(() => undefined),

  // ── Location history / export / heatmap ──────────────────────────

  getLocationHistoryRange: (
    collarId: string,
    from?: string,
    to?: string,
    maxPoints = 2000,
  ) =>
    apiClient
      .get<CollarLocationPointDto[]>(`/collars/${collarId}/location-history`, {
        params: { from, to, maxPoints },
      })
      .then((r) => r.data),

  exportLocationHistoryCsv: (collarId: string, from?: string, to?: string) =>
    apiClient
      .get<Blob>(`/collars/${collarId}/location-history/export.csv`, {
        params: { from, to },
        responseType: "blob",
      })
      .then((r) => r.data),

  getLocationHeatmap: (collarId: string, days = 30) =>
    apiClient
      .get<CollarLocationPointDto[]>(`/collars/${collarId}/location-heatmap`, {
        params: { days },
      })
      .then((r) => r.data),
};
