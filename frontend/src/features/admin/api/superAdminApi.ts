import { apiClient } from "@/shared/lib/apiClient";

export const superAdminApi = {
  assignRole: (userId: string, reason: string, mfaCode: string) =>
    apiClient.put<void>(`/super-admin/users/${userId}/role`, { reason, mfaCode }),
  revokeRole: (userId: string, reason: string, mfaCode: string) =>
    apiClient.delete<void>(`/super-admin/users/${userId}/role`, { data: { reason, mfaCode } }),
};
