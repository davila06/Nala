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

export interface SubscriptionPlanDto {
  id: string;
  tier: SubscriptionTier;
  displayName: string;
  description: string;
  monthlyPriceCrc: number | null;
  annualPriceCrc: number | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  version: string;
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

export interface AdminClinicVerificationDto {
  id: string;
  clinicId: string;
  licenseNumberSnapshot: string;
  status: "Pending" | "Verified" | "Rejected" | "Expired";
  submittedAt: string;
  verifiedAt: string | null;
  reviewedAt: string | null;
  verifiedByAdminUserId: string | null;
  reviewedByAdminUserId: string | null;
  expiresAt: string | null;
  hasDocument: boolean;
  rejectionReason: string | null;
  reviewNotes: string | null;
  revalidationRequestedAt: string | null;
}

export interface AdminClinicVeterinarianDto {
  id: string;
  clinicId: string;
  fullName: string;
  licenseNumber: string;
  status:
    | "PendingReview"
    | "Authorized"
    | "Rejected"
    | "Suspended"
    | "Revoked"
    | "Expired";
  canIssueCertificates: boolean;
  isActive: boolean;
  hasDocument: boolean;
  hasSignature: boolean;
  expiresAt: string | null;
  rejectionReason: string | null;
  suspensionReason: string | null;
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

  getClinicVerifications: (page = 1, pageSize = 20) =>
    apiClient
      .get<
        AdminClinicVerificationDto[]
      >("/clinics/admin/verifications", { params: { page, pageSize } })
      .then((r) => r.data),

  reviewClinicVerification: (
    verificationId: string,
    payload: {
      approve: boolean;
      expiresAt?: string;
      reason?: string;
      notes?: string;
    },
  ) =>
    apiClient
      .put<AdminClinicVerificationDto>(
        `/clinics/admin/verifications/${verificationId}/review`,
        payload,
      )
      .then((r) => r.data),

  getClinicVeterinariansForReview: (page = 1, pageSize = 20) =>
    apiClient
      .get<
        AdminClinicVeterinarianDto[]
      >("/clinics/admin/veterinarians", { params: { page, pageSize } })
      .then((r) => r.data),

  reviewClinicVeterinarian: (
    veterinarianId: string,
    payload: {
      approve: boolean;
      expiresAt?: string;
      reason?: string;
      notes?: string;
    },
  ) =>
    apiClient
      .put<AdminClinicVeterinarianDto>(
        `/clinics/admin/veterinarians/${veterinarianId}/review`,
        payload,
      )
      .then((r) => r.data),

  suspendClinicVeterinarian: (veterinarianId: string, reason: string) =>
    apiClient
      .post<AdminClinicVeterinarianDto>(
        `/clinics/admin/veterinarians/${veterinarianId}/suspend`,
        { reason },
      )
      .then((r) => r.data),

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

  getSubscriptionPlans: (includeInactive = true) =>
    apiClient
      .get<SubscriptionPlanDto[]>("/admin/subscription-plans", {
        params: { includeInactive, skip: 0, take: 100 },
      })
      .then((r) => r.data),

  createSubscriptionPlan: (
    payload: Omit<
      SubscriptionPlanDto,
      "id" | "isActive" | "createdAt" | "updatedAt" | "version"
    >,
  ) =>
    apiClient
      .post<SubscriptionPlanDto>("/admin/subscription-plans", payload)
      .then((r) => r.data),

  updateSubscriptionPlan: (
    id: string,
    payload: Omit<
      SubscriptionPlanDto,
      "id" | "tier" | "isActive" | "createdAt" | "updatedAt"
    >,
  ) =>
    apiClient
      .put<SubscriptionPlanDto>(`/admin/subscription-plans/${id}`, payload)
      .then((r) => r.data),

  deleteSubscriptionPlan: (id: string, version: string) =>
    apiClient
      .delete<SubscriptionPlanDto>(`/admin/subscription-plans/${id}`, {
        data: { version },
      })
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
