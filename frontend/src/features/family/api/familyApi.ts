import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export interface FamilyMemberDto {
  userId: string;
  name: string;
  email: string;
  role: "Owner" | "Member";
  joinedAt: string;
}

export interface FamilyAccountDto {
  id: string;
  name: string;
  members: FamilyMemberDto[];
}

export interface FamilyInvitationDto {
  token: string;
  invitedEmail: string;
  expiresAt: string;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const familyApi = {
  getMyFamily: (): Promise<FamilyAccountDto> =>
    apiClient.get<FamilyAccountDto>("/family").then((r) => r.data),

  createAccount: (name: string): Promise<FamilyAccountDto> =>
    apiClient.post<FamilyAccountDto>("/family", { name }).then((r) => r.data),

  invite: (email: string): Promise<FamilyInvitationDto> =>
    apiClient
      .post<FamilyInvitationDto>("/family/invite", { email })
      .then((r) => r.data),

  acceptInvitation: (token: string): Promise<void> =>
    apiClient.post(`/family/invitations/${token}/accept`).then(() => undefined),

  removeMember: (memberId: string): Promise<void> =>
    apiClient.delete(`/family/members/${memberId}`).then(() => undefined),
};
