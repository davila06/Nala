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
  isFeatured: boolean;
  status: ClinicStatus;
  registeredAt: string;
}

export interface PublicClinicDto {
  id: string;
  name: string;
  address: string;
  contactEmail: string;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  lat: number;
  lng: number;
  isFeatured: boolean;
  status: string;
}

export interface ClinicScanResultDto {
  scanId: string;
  matched: boolean;
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

export interface NearbyAlertDto {
  lostPetEventId: string;
  petName: string;
  petSpecies: string | null;
  lastSeenLat: number | null;
  lastSeenLng: number | null;
  reportedAt: string;
  recentPhotoUrl: string | null;
}

// ── API client methods ─────────────────────────────────────────────────────────

export const clinicsApi = {
  register: (payload: RegisterClinicRequest): Promise<ClinicDto> =>
    apiClient.post<ClinicDto>("/clinics/register", payload).then((r) => r.data),

  getMyClinic: (): Promise<ClinicDto> =>
    apiClient.get<ClinicDto>("/clinics/me").then((r) => r.data),

  scan: (
    input: string,
    inputType: ScanInputType,
  ): Promise<ClinicScanResultDto> =>
    apiClient
      .post<ClinicScanResultDto>("/clinics/scan", { input, inputType })
      .then((r) => r.data),

  getPublicClinics: (lat?: number, lng?: number): Promise<PublicClinicDto[]> =>
    apiClient
      .get<PublicClinicDto[]>("/clinics/public", { params: { lat, lng } })
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

  createApiKey: (label: string): Promise<ClinicApiKeyDto> =>
    apiClient
      .post<ClinicApiKeyDto>("/clinics/me/api-keys", { label })
      .then((r) => r.data),

  revokeApiKey: (keyId: string): Promise<void> =>
    apiClient.delete(`/clinics/me/api-keys/${keyId}`).then(() => undefined),

  getNearbyAlerts: (radiusKm = 15): Promise<NearbyAlertDto[]> =>
    apiClient
      .get<
        NearbyAlertDto[]
      >("/clinics/me/nearby-alerts", { params: { radiusKm } })
      .then((r) => r.data),
};
