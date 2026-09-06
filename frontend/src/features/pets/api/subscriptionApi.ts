import { apiClient } from "@/shared/lib/apiClient";

export type SubscriptionTier =
  | "Free"
  | "UserPlus"
  | "UserFamilia"
  | "ClinicBasic"
  | "ClinicPlus"
  | "ClinicPartner"
  | "StoreBasic"
  | "StorePlus"
  | "StorePartner"
  | "ShelterBasic"
  | "ShelterPlus"
  | "MuniBasica"
  | "MuniFull"
  | "MuniRedRegional";

export type SubscriptionStatus =
  | "PendingPayment"
  | "Active"
  | "Cancelled"
  | "Expired";

export interface SubscriptionDto {
  id: string;
  tier: SubscriptionTier;
  status: SubscriptionStatus;
  paymentReference: string;
  amountCrc: number;
  createdAt: string;
  activatedAt: string | null;
  expiresAt: string | null;
  paymentReportedAt: string | null;
  isActive: boolean;
}

export interface SubscriptionPlanCatalogDto {
  id: string;
  tier: SubscriptionTier;
  displayName: string;
  description: string;
  monthlyPriceCrc: number | null;
  annualPriceCrc: number | null;
  isActive: boolean;
  version: string;
}

export const TIER_PRICE_CRC: Record<SubscriptionTier, number> = {
  Free: 0,
  UserPlus: 2990,
  UserFamilia: 4990,
  ClinicBasic: 0,
  ClinicPlus: 15000,
  ClinicPartner: 35000,
  StoreBasic: 0,
  StorePlus: 12000,
  StorePartner: 25000,
  ShelterBasic: 0,
  ShelterPlus: 8000,
  MuniBasica: 150000,
  MuniFull: 300000,
  MuniRedRegional: 500000,
};

export const subscriptionApi = {
  getCatalog: () =>
    apiClient
      .get<SubscriptionPlanCatalogDto[]>("/catalog/subscription-plans")
      .then((r) => r.data),

  getMine: (clinicId?: string) =>
    apiClient
      .get<SubscriptionDto | null>("/subscriptions/me", {
        params: clinicId ? { clinicId } : undefined,
      })
      .then((r) => r.data),

  create: (tier: SubscriptionTier, clinicId?: string) =>
    apiClient
      .post<SubscriptionDto>("/subscriptions", {
        tier,
        clinicId: clinicId ?? null,
      })
      .then((r) => r.data),

  activate: (paymentReference: string) =>
    apiClient
      .put<SubscriptionDto>("/subscriptions/activate", { paymentReference })
      .then((r) => r.data),

  cancel: (subscriptionId: string) =>
    apiClient
      .delete<SubscriptionDto>(`/subscriptions/${subscriptionId}`)
      .then((r) => r.data),

  reportPayment: (subscriptionId: string) =>
    apiClient
      .put<SubscriptionDto>(`/subscriptions/${subscriptionId}/report-payment`)
      .then((r) => r.data),
};
