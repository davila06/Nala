import { apiClient } from "@/shared/lib/apiClient";

export interface PaymentProfileDto {
  id: string;
  userId: string;
  methodType: string;
  cardBrand: string;
  lastFourDigits: string;
  expirationMonth: number | null;
  expirationYear: number | null;
  cardholderName: string | null;
  isDefault: boolean;
  createdAt: string;
  lastUsedAt: string | null;
}

export interface CaptureContextDto {
  clientLibraryUrl: string;
  captureContextJwt: string;
  keyId: string;
  isConfigured: boolean;
}

export interface ChargeCardRequest {
  amountCrc: number;
  purpose: "Subscription" | "BundleOrder" | "Bounty";
  targetEntityId?: string;
  paymentProfileId?: string;
  transientToken?: string;
  cardholderName?: string;
  saveProfile?: boolean;
}

export interface ChargeCardResultDto {
  success: boolean;
  transactionReference: string;
  authorizationCode: string | null;
  errorMessage: string | null;
  activatedSubscriptionId?: string | null;
  confirmedBundleOrderId?: string | null;
}

export const paymentApi = {
  getCaptureContext: () => apiClient.get<CaptureContextDto>("/payments/capture-context").then((r) => r.data),

  getProfiles: () => apiClient.get<PaymentProfileDto[]>("/payments/profiles").then((r) => r.data),

  saveProfile: (data: { transientToken: string; cardholderName?: string; setAsDefault?: boolean }) =>
    apiClient.post<PaymentProfileDto>("/payments/profiles", data).then((r) => r.data),

  deleteProfile: (id: string) => apiClient.delete<void>(`/payments/profiles/${id}`).then((r) => r.data),

  chargeCard: (data: ChargeCardRequest) =>
    apiClient.post<ChargeCardResultDto>("/payments/charge", data).then((r) => r.data),
};
