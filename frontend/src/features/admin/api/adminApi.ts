import { apiClient } from "@/shared/lib/apiClient";
import type {
  SubscriptionDto,
  SubscriptionStatus,
  SubscriptionTier,
} from "@/features/pets/api/subscriptionApi";
import type { AdoptablePetDto } from "@/features/adoptions/api/adoptionsApi";

export type { SubscriptionDto, SubscriptionStatus, SubscriptionTier };

export interface AdoptionAdminStatsDto {
  totalPublished: number;
  totalAvailable: number;
  totalInProcess: number;
  totalAdopted: number;
  totalPaused: number;
  totalApplications: number;
  totalFairs: number;
}

export interface AdminAdoptionsPage {
  items: AdoptablePetDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface AdminSubscriptionDto {
  id: string;
  userId: string | null;
  clinicId: string | null;
  tier: SubscriptionTier;
  status: SubscriptionStatus;
  paymentReference: string;
  amountCrc: number;
  createdAt: string;
  activatedAt: string | null;
  expiresAt: string | null;
  paymentReportedAt: string | null;
}

export interface PendingAllyDto {
  userId: string;
  organizationName: string;
  allyType: string;
  coverageLabel: string;
  coverageLat: number;
  coverageLng: number;
  coverageRadiusMetres: number;
  verificationStatus: string;
  appliedAt: string;
  verifiedAt: string | null;
}

export interface PendingClinicDto {
  id: string;
  name: string;
  licenseNumber: string;
  address: string;
  lat: number;
  lng: number;
  contactEmail: string;
  status: string;
  registeredAt: string;
}

export const adminApi = {
  getPendingAllies: () =>
    apiClient
      .get<PendingAllyDto[]>("/allies/admin/pending")
      .then((r) => r.data),

  reviewAlly: (userId: string, approve: boolean) =>
    apiClient.post<void>(`/allies/admin/applications/${userId}/review`, {
      approve,
    }),

  getPendingClinics: () =>
    apiClient
      .get<PendingClinicDto[]>("/clinics/admin/pending")
      .then((r) => r.data),

  reviewClinic: (clinicId: string, approve: boolean) =>
    apiClient.put<void>(`/clinics/admin/${clinicId}/review`, { approve }),

  getAdminSubscriptions: (pendingOnly = false, skip = 0, take = 50) =>
    apiClient
      .get<
        AdminSubscriptionDto[]
      >("/subscriptions/admin", { params: { pendingOnly, skip, take } })
      .then((r) => r.data),

  adminActivateSubscription: (id: string, billingMonths = 1) =>
    apiClient
      .put<SubscriptionDto>(`/subscriptions/admin/${id}/activate`, {
        billingMonths,
      })
      .then((r) => r.data),

  adminCancelSubscription: (id: string) =>
    apiClient
      .delete<SubscriptionDto>(`/subscriptions/admin/${id}`)
      .then((r) => r.data),

  // ── Adoptions admin ────────────────────────────────────────────────────────

  getAdoptionStats: () =>
    apiClient
      .get<AdoptionAdminStatsDto>("/admin/adoptions/stats")
      .then((r) => r.data),

  getAdminAnimals: (status?: string, page = 1, pageSize = 20) =>
    apiClient
      .get<AdminAdoptionsPage>("/admin/adoptions/animals", {
        params: { status, page, pageSize },
      })
      .then((r) => r.data),

  moderateAnimal: (id: string, action: "remove" | "pause" | "restore") =>
    apiClient.patch<void>(`/admin/adoptions/animals/${id}/moderate`, {
      action,
    }),
};
