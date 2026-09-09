import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type ClinicStatus = "Pending" | "Active" | "Suspended";
export type ScanInputType = "Qr" | "RfidChip";

export interface ClinicDto {
  id: string;
  name: string;
  licenseNumber: string;
  address: string;
  lat: number;
  lng: number;
  contactEmail: string;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  description: string | null;
  services: string | null;
  openingHours: string | null;
  isFeatured: boolean;
  isEmergency24h: boolean;
  emergencyPhone: string | null;
  whatsAppNumber: string | null;
  isWhatsAppContactEnabled: boolean;
  status: ClinicStatus;
  registeredAt: string;
}

export interface ClinicAccessSearchResultDto {
  id: string;
  name: string;
  licenseNumber: string;
}

export interface PublicClinicDto {
  id: string;
  name: string;
  address: string;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  lat: number;
  lng: number;
  isFeatured: boolean;
  isEmergency24h: boolean;
  emergencyPhone: string | null;
  whatsAppNumber: string | null;
  status: string;
}

export interface PublicClinicProfileDto {
  id: string;
  name: string;
  address: string;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  description: string | null;
  services: string | null;
  openingHours: string | null;
  lat: number;
  lng: number;
  isFeatured: boolean;
  isEmergency24h: boolean;
  emergencyPhone: string | null;
}

export interface EmergencyVetDto {
  id: string;
  name: string;
  address: string;
  emergencyPhone: string | null;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  lat: number;
  lng: number;
  distanceKm: number | null;
}

export interface ClinicScanResultDto {
  scanId: string;
  matched: boolean;
  petId: string | null;
  petName: string | null;
  petPhotoUrl: string | null;
  ownerName: string | null;
  ownerEmail: string | null;
  petSpecies: string | null;
}

export interface ClinicScanDayStat {
  day: string;
  total: number;
  matched: number;
  qrCount: number;
  rfidCount: number;
}

export interface ClinicScanStatsDto {
  year: number;
  month: number;
  totalScans: number;
  matchedScans: number;
  qrScans: number;
  rfidScans: number;
  byDay: ClinicScanDayStat[];
}

export interface ClinicApiKeyDto {
  id: string;
  label: string;
  isRevoked: boolean;
  createdAt: string;
  lastUsedAt: string | null;
  expiresAt: string;
  scopes: string[];
  rawKey?: string;
}

export interface RegisterClinicRequest {
  name: string;
  licenseNumber: string;
  address: string;
  lat: number;
  lng: number;
  contactEmail: string;
  password: string;
}

export interface UpdateClinicProfileRequest {
  name: string;
  address: string;
  phoneNumber: string | null;
  website: string | null;
  isEmergency24h: boolean | null;
  emergencyPhone: string | null;
  description?: string | null;
  services?: string | null;
  openingHours?: string | null;
  whatsAppNumber?: string | null;
  isWhatsAppContactEnabled?: boolean;
}

export interface NearbyAlertDto {
  lostPetEventId: string;
  petName: string;
  petSpecies: string | null;
  lastSeenLat: number | null;
  lastSeenLng: number | null;
  reportedAt: string;
  recentPhotoUrl: string | null;
}

export interface ClinicVisibilityStatsDto {
  periodDays: number;
  profileViews: number;
  mapClicks: number;
  searchAppearances: number;
  alertImpressions: number;
  scanResultViews: number;
}

// ── API client methods ─────────────────────────────────────────────────────────

export const clinicsApi = {
  register: (payload: RegisterClinicRequest): Promise<ClinicDto> =>
    apiClient.post<ClinicDto>("/clinics/register", payload).then((r) => r.data),

  getMyClinic: (): Promise<ClinicDto> =>
    apiClient.get<ClinicDto>("/clinics/me").then((r) => r.data),

  updateMyProfile: (payload: UpdateClinicProfileRequest): Promise<ClinicDto> =>
    apiClient
      .put<ClinicDto>("/clinics/me/profile", payload)
      .then((r) => r.data),

  scan: (
    input: string,
    inputType: ScanInputType,
  ): Promise<ClinicScanResultDto> =>
    apiClient
      .post<ClinicScanResultDto>("/clinics/scan", { input, inputType })
      .then((r) => r.data),

  getPublicClinics: (
    lat?: number,
    lng?: number,
    options?: {
      search?: string;
      emergencyOnly?: boolean;
      page?: number;
      pageSize?: number;
    },
  ): Promise<PublicClinicDto[]> =>
    apiClient
      .get<PublicClinicDto[]>("/clinics/public", {
        params: { lat, lng, ...options },
      })
      .then((r) => r.data),

  getPublicProfile: (clinicId: string): Promise<PublicClinicProfileDto> =>
    apiClient
      .get<PublicClinicProfileDto>(`/clinics/public/${clinicId}`)
      .then((r) => r.data),

  searchForAccess: (query: string): Promise<ClinicAccessSearchResultDto[]> =>
    apiClient
      .get<
        ClinicAccessSearchResultDto[]
      >("/clinics/search", { params: { query } })
      .then((r) => r.data),

  getScanStats: (year?: number, month?: number): Promise<ClinicScanStatsDto> =>
    apiClient
      .get<ClinicScanStatsDto>("/clinics/me/stats", { params: { year, month } })
      .then((r) => r.data),

  uploadLogo: (file: File): Promise<{ logoUrl: string }> => {
    const form = new FormData();
    form.append("file", file);
    return apiClient
      .post<{ logoUrl: string }>("/clinics/me/logo", form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },

  getApiKeys: (): Promise<ClinicApiKeyDto[]> =>
    apiClient
      .get<ClinicApiKeyDto[]>("/clinics/me/api-keys")
      .then((r) => r.data),

  createApiKey: (label: string, scopes?: string[]): Promise<ClinicApiKeyDto> =>
    apiClient
      .post<ClinicApiKeyDto>("/clinics/me/api-keys", { label, scopes })
      .then((r) => r.data),

  revokeApiKey: (keyId: string): Promise<void> =>
    apiClient.delete(`/clinics/me/api-keys/${keyId}`).then(() => undefined),

  getNearbyAlerts: (radiusKm = 15): Promise<NearbyAlertDto[]> =>
    apiClient
      .get<
        NearbyAlertDto[]
      >("/clinics/me/nearby-alerts", { params: { radiusKm } })
      .then((r) => r.data),

  getVisibilityStats: (days = 30): Promise<ClinicVisibilityStatsDto> =>
    apiClient
      .get<ClinicVisibilityStatsDto>("/clinics/me/visibility-stats", {
        params: { days },
      })
      .then((r) => r.data),

  // Fire-and-forget — called when a user opens a clinic popup or profile
  trackView: (
    clinicId: string,
    source: "map" | "directory" | "search" | "alert",
  ): void => {
    void apiClient
      .post(`/clinics/${clinicId}/view`, null, { params: { source } })
      .catch(() => undefined);
  },

  getEmergencyVets: (
    lat?: number,
    lng?: number,
    radiusKm = 30,
  ): Promise<EmergencyVetDto[]> =>
    apiClient
      .get<
        EmergencyVetDto[]
      >("/public/emergency-vets", { params: { lat, lng, radiusKm } })
      .then((r) => r.data),
};
