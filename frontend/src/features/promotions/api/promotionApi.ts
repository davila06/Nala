import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type PromotionType = "PercentageDiscount" | "FreeTier" | "FreeMonths";

export interface PromotionCodeDto {
  id: string;
  code: string;
  type: PromotionType;
  discountPercent: number | null;
  freeMonths: number | null;
  targetTier: string | null;
  maxRedemptions: number;
  redeemedCount: number;
  expiresAt: string | null;
  isActive: boolean;
  canRedeem: boolean;
  adminNote: string | null;
  createdAt: string;
}

export interface PromotionValidationDto {
  code: string;
  type: PromotionType;
  benefitDescription: string;
  discountPercent: number | null;
  freeMonths: number | null;
  targetTier: string | null;
  isFullyFree: boolean;
  requiresPayment: boolean;
}

export interface PromotionSpecRequest {
  type: PromotionType;
  discountPercent?: number;
  freeMonths?: number;
  targetTier?: string;
  maxRedemptions: number;
  expiresAt?: string;
  adminNote?: string;
  quantity: number;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const promotionApi = {
  // User
  validate: (code: string): Promise<PromotionValidationDto> =>
    apiClient
      .get<PromotionValidationDto>(
        `/promotions/validate/${encodeURIComponent(code)}`,
      )
      .then((r) => r.data),

  redeem: (code: string, selectedTier?: string): Promise<unknown> =>
    apiClient
      .post("/promotions/redeem", { code, selectedTier: selectedTier ?? null })
      .then((r) => r.data),

  // Admin
  getAll: (): Promise<PromotionCodeDto[]> =>
    apiClient.get<PromotionCodeDto[]>("/admin/promotions").then((r) => r.data),

  createBatch: (specs: PromotionSpecRequest[]): Promise<PromotionCodeDto[]> =>
    apiClient
      .post<PromotionCodeDto[]>("/admin/promotions/batch", { specs })
      .then((r) => r.data),

  toggle: (id: string, activate: boolean): Promise<PromotionCodeDto> =>
    apiClient
      .put<PromotionCodeDto>(`/admin/promotions/${id}/toggle`, { activate })
      .then((r) => r.data),
};
