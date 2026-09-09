import { apiClient } from "@/shared/lib/apiClient";

export const BILLBOARD_PLACEMENTS = [
  "Map",
  "Dashboard",
  "Directory",
  "Feed",
  "PublicPetProfile",
  "ScanHistory",
  "CaseRoom",
  "ClinicDirectory",
  "ClinicProfile",
  "ServiceProviderDirectory",
  "ServiceProviderProfile",
  "AdoptionDirectory",
  "AdoptionFair",
  "PetRegistration",
  "CollarActivation",
] as const;

export type BillboardPlacement = (typeof BILLBOARD_PLACEMENTS)[number];
export type BillboardStatus = "Draft" | "Active" | "Paused" | "Expired";
export type BillboardCategory =
  | "EmergencyVeterinary"
  | "RecoveryService"
  | "GpsAndIdentification"
  | "PetInsurance"
  | "FoodAndNutrition"
  | "PetCare"
  | "Training"
  | "Boarding"
  | "AdoptionSupport";

export interface BillboardDto {
  id: string;
  title: string;
  body: string | null;
  imageUrl: string | null;
  ctaLabel: string | null;
  ctaUrl: string | null;
  placement: BillboardPlacement;
  status: BillboardStatus;
  startsAt: string;
  endsAt: string;
  priority: number;
  createdAt: string;
  advertiserName: string;
  category: BillboardCategory;
  targetCanton: string | null;
  contractReference: string | null;
  budgetCrc: number;
  frequencyCapPerDay: number;
  isCategoryExclusive: boolean;
  isVip: boolean;
  campaignStatus: "Draft" | "PendingReview" | "Approved" | "Rejected";
  reviewNote: string | null;
}

export interface BillboardCampaignMetrics {
  billboardId: string;
  impressions: number;
  clicks: number;
  conversions: number;
  clickThroughRate: number;
  impressionsByCanton: Record<string, number>;
  impressionsByPlacement: Record<string, number>;
}

export const billboardsApi = {
  getActive: (placement: BillboardPlacement): Promise<BillboardDto[]> =>
    apiClient
      .get<BillboardDto[]>("/billboards", { params: { placement } })
      .then((r) => r.data),

  getAll: (page = 1, pageSize = 20) =>
    apiClient
      .get<{
        items: BillboardDto[];
        totalCount: number;
        pageNumber: number;
        pageSize: number;
        totalPages: number;
        hasNextPage: boolean;
      }>("/billboards/admin", {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  create: (data: {
    title: string;
    body?: string;
    placement: BillboardPlacement;
    startsAt: string;
    endsAt: string;
    ctaLabel?: string;
    ctaUrl?: string;
    priority?: number;
    advertiserName?: string;
    category?: BillboardCategory;
    targetCanton?: string;
    contractReference?: string;
    budgetCrc?: number;
    frequencyCapPerDay?: number;
    isCategoryExclusive?: boolean;
    isVip?: boolean;
  }) => apiClient.post<BillboardDto>("/billboards", data).then((r) => r.data),

  update: (
    id: string,
    data: {
      title: string;
      body?: string;
      ctaLabel?: string;
      ctaUrl?: string;
      startsAt: string;
      endsAt: string;
      priority: number;
      advertiserName: string;
      category: BillboardCategory;
      targetCanton?: string;
      contractReference?: string;
      budgetCrc: number;
      frequencyCapPerDay: number;
      isCategoryExclusive: boolean;
      isVip: boolean;
    },
  ) =>
    apiClient.put<BillboardDto>(`/billboards/${id}`, data).then((r) => r.data),

  delete: (id: string) => apiClient.delete(`/billboards/${id}`),

  setStatus: (id: string, status: "active" | "paused" | "expired") =>
    apiClient
      .patch<BillboardDto>(`/billboards/${id}/status`, { status })
      .then((r) => r.data),

  submit: (id: string) =>
    apiClient
      .post<BillboardDto>(`/billboards/${id}/submit`)
      .then((r) => r.data),

  review: (id: string, approve: boolean, note?: string) =>
    apiClient
      .post<BillboardDto>(`/billboards/${id}/review`, { approve, note })
      .then((r) => r.data),

  trackDelivery: (
    id: string,
    eventType: "Impression" | "Click" | "Conversion",
    eventKey: string,
    canton?: string,
  ) =>
    apiClient.post(`/billboards/${id}/events`, { eventType, eventKey, canton }),

  getMetrics: (id: string, from?: string, to?: string) =>
    apiClient
      .get<BillboardCampaignMetrics>(`/billboards/${id}/metrics`, {
        params: { from, to },
      })
      .then((r) => r.data),

  uploadImage: (id: string, file: File) => {
    const form = new FormData();
    form.append("image", file);
    return apiClient
      .post<BillboardDto>(`/billboards/${id}/image`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },
};
