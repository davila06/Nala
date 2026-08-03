import { apiClient } from "@/shared/lib/apiClient";

export type CapturedAnimalStatus =
  | "Received"
  | "OwnerFound"
  | "Transferred"
  | "Released"
  | "Adopted";

export type MunicipalTier = "Basica" | "Full" | "RedRegional";

export interface CapturedAnimalDto {
  id: string;
  canton: string;
  species: string;
  breed: string | null;
  color: string;
  estimatedAge: string | null;
  photoUrl: string | null;
  notes: string | null;
  collarChipNumber: string | null;
  matchedPetId: string | null;
  status: CapturedAnimalStatus;
  capturedAt: string;
}

export interface CapturedAnimalPageDto {
  items: CapturedAnimalDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface MunicipalProfileDto {
  id: string;
  userId: string;
  canton: string;
  orgName: string;
  tier: MunicipalTier;
  isActive: boolean;
  isExpired: boolean;
  allCantons: string[];
  subscribedAt: string;
  expiresAt: string | null;
}

export interface CantonStatisticsDto {
  canton: string;
  totalCaptured: number;
  received: number;
  ownerFound: number;
  transferred: number;
  released: number;
  adopted: number;
  recoveryRate: number;
  last30Days: { date: string; count: number }[];
}

export interface RegionalDashboardDto {
  cantons: string[];
  summary: {
    canton: string;
    total: number;
    active: number;
    ownerFound: number;
    recoveryRate: number;
  }[];
  regionalTotal: number;
  regionalRecoveryRate: number;
}

export interface BulkUpdateResultDto {
  updated: number;
  notFound: number;
}

export const STATUS_LABELS: Record<CapturedAnimalStatus, string> = {
  Received: "Recibido",
  OwnerFound: "Dueño localizado",
  Transferred: "Transferido",
  Released: "Liberado",
  Adopted: "Adoptado",
};

export const TIER_LABELS: Record<MunicipalTier, string> = {
  Basica: "Básica",
  Full: "Full",
  RedRegional: "Red Regional",
};

export const municipalApi = {
  getProfile: (): Promise<MunicipalProfileDto | null> =>
    apiClient
      .get<MunicipalProfileDto | null>("/municipalities/profile")
      .then((r) => r.data),

  search: (canton?: string, status?: CapturedAnimalStatus, page = 1) =>
    apiClient
      .get<CapturedAnimalPageDto>("/municipalities/captures", {
        params: { canton, status, page, pageSize: 20 },
      })
      .then((r) => r.data),

  record: (data: {
    canton: string;
    species: string;
    color: string;
    breed?: string;
    estimatedAge?: string;
    notes?: string;
    collarChipNumber?: string;
    capturedAt?: string;
  }) =>
    apiClient
      .post<CapturedAnimalDto>("/municipalities/captures", data)
      .then((r) => r.data),

  updateStatus: (
    id: string,
    status: CapturedAnimalStatus,
    matchedPetId?: string,
  ) =>
    apiClient
      .put<CapturedAnimalDto>(`/municipalities/captures/${id}/status`, {
        status,
        matchedPetId,
      })
      .then((r) => r.data),

  bulkUpdateStatus: (
    animalIds: string[],
    newStatus: CapturedAnimalStatus,
    matchedPetId?: string,
  ): Promise<BulkUpdateResultDto> =>
    apiClient
      .put<BulkUpdateResultDto>("/municipalities/captures/bulk-status", {
        animalIds,
        newStatus,
        matchedPetId,
      })
      .then((r) => r.data),

  uploadPhoto: (id: string, file: File): Promise<{ photoUrl: string }> => {
    const form = new FormData();
    form.append("photo", file);
    return apiClient
      .post<{ photoUrl: string }>(
        `/municipalities/captures/${id}/photo`,
        form,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      )
      .then((r) => r.data);
  },

  getStats: (canton?: string): Promise<CantonStatisticsDto> =>
    apiClient
      .get<CantonStatisticsDto>("/municipalities/stats", { params: { canton } })
      .then((r) => r.data),

  getRegionalDashboard: (): Promise<RegionalDashboardDto> =>
    apiClient
      .get<RegionalDashboardDto>("/municipalities/regional")
      .then((r) => r.data),

  transfer: (
    id: string,
    destinationCanton: string,
    notes?: string,
  ): Promise<CapturedAnimalDto> =>
    apiClient
      .post<CapturedAnimalDto>(`/municipalities/captures/${id}/transfer`, {
        destinationCanton,
        notes,
      })
      .then((r) => r.data),
};
