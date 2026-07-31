import { apiClient } from "@/shared/lib/apiClient";

export type BountyStatus =
  | "PendingDeposit"
  | "Active"
  | "Claimed"
  | "Released"
  | "Refunded"
  | "Expired";

export interface BountyDto {
  id: string;
  lostPetEventId: string;
  amount: number;
  currencyCode: string;
  status: BountyStatus;
  depositReference: string;
  platformFee: number;
  netPayoutAmount: number;
  claimedByUserId: string | null;
  createdAt: string;
  depositedAt: string | null;
  claimedAt: string | null;
  releasedAt: string | null;
}

export const bountyApi = {
  getForEvent: (lostEventId: string) =>
    apiClient
      .get<BountyDto | null>(`/bounties/event/${lostEventId}`)
      .then((r) => r.data),

  create: (lostPetEventId: string, amount: number, currencyCode = "CRC") =>
    apiClient
      .post<BountyDto>("/bounties", { lostPetEventId, amount, currencyCode })
      .then((r) => r.data),

  confirmDeposit: (depositReference: string) =>
    apiClient
      .put<BountyDto>("/bounties/confirm-deposit", { depositReference })
      .then((r) => r.data),

  release: (bountyId: string) =>
    apiClient
      .put<BountyDto>(`/bounties/${bountyId}/release`, {})
      .then((r) => r.data),
};
