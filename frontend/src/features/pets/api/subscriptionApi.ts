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

export type SubscriptionStatus = "PendingPayment" | "Active" | "Cancelled" | "Expired";

export interface SubscriptionDto {
  id: string;
  tier: SubscriptionTier;
  status: SubscriptionStatus;
  billingMonths: number;
  paymentReference: string;
  amountCrc: number;
  createdAt: string;
  activatedAt: string | null;
  startsAt: string | null;
  expiresAt: string | null;
  paymentReportedAt: string | null;
  cancellationRequestedAt: string | null;
  isActive: boolean;
  bankReceiptNumber?: string | null;
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

export interface EntitlementValueDto {
  key: string;
  valueType: "Numeric" | "Boolean" | "Text";
  numericValue: number | null;
  booleanValue: boolean | null;
  textValue: string | null;
  unit: string | null;
  resetPeriod: string | null;
  consumed: number;
}

export interface EntitlementSnapshotDto {
  subjectId: string;
  subscriptionId: string | null;
  tier: SubscriptionTier;
  cycleStart: string | null;
  cycleEnd: string | null;
  entitlements: Record<string, EntitlementValueDto>;
}

export const IVA_RATE = 0.13;

export function calculateIva(baseAmountCrc: number): number {
  return Math.round(baseAmountCrc * IVA_RATE);
}

export function calculateTotalWithIva(baseAmountCrc: number): number {
  return Math.round(baseAmountCrc * (1 + IVA_RATE));
}

export function calculateEffectivePrice(baseAmountCrc: number, requiresInvoice: boolean): number {
  return requiresInvoice ? calculateTotalWithIva(baseAmountCrc) : Math.round(baseAmountCrc);
}

export const subscriptionApi = {
  getCatalog: () => apiClient.get<SubscriptionPlanCatalogDto[]>("/catalog/subscription-plans").then((r) => r.data),

  getMine: (clinicId?: string) =>
    apiClient
      .get<SubscriptionDto | null>("/subscriptions/me", {
        params: clinicId ? { clinicId } : undefined,
      })
      .then((r) => r.data),

  getEntitlements: () => apiClient.get<EntitlementSnapshotDto>("/subscriptions/entitlements").then((r) => r.data),

  create: (tier: SubscriptionTier, billingMonths: number, clinicId?: string, requiresInvoice?: boolean) =>
    apiClient
      .post<SubscriptionDto>("/subscriptions", {
        tier,
        clinicId: clinicId ?? null,
        billingMonths,
        requiresInvoice,
      })
      .then((r) => r.data),

  activate: (paymentReference: string) =>
    apiClient.put<SubscriptionDto>("/subscriptions/activate", { paymentReference }).then((r) => r.data),

  cancel: (subscriptionId: string) =>
    apiClient.delete<SubscriptionDto>(`/subscriptions/${subscriptionId}`).then((r) => r.data),

  downgrade: (subscriptionId: string, targetTier: SubscriptionTier) =>
    apiClient
      .post<SubscriptionDto>(`/subscriptions/${subscriptionId}/downgrade`, {
        targetTier,
      })
      .then((r) => r.data),

  reportPayment: (subscriptionId: string, bankReceiptNumber?: string) =>
    apiClient
      .put<SubscriptionDto>(`/subscriptions/${subscriptionId}/report-payment`, {
        bankReceiptNumber: bankReceiptNumber?.trim() || null,
      })
      .then((r) => r.data),

  getSinpeQrBlob: (subscriptionId: string) =>
    apiClient.get(`/subscriptions/${subscriptionId}/sinpe-qr`, { responseType: "blob" }).then((r) => r.data as Blob),
};
