import { apiClient } from "@/shared/lib/apiClient";

export type AllyType = "VeterinaryClinic" | "Shelter" | "PetFriendlyBusiness" | "PrivateSecurity" | "Municipality";

export type AllyVerificationStatus = "Pending" | "Verified" | "Rejected";

export interface AllyProfile {
  userId: string;
  organizationName: string;
  allyType: AllyType;
  coverageLabel: string;
  coverageLat: number;
  coverageLng: number;
  coverageRadiusMetres: number;
  verificationStatus: AllyVerificationStatus;
  appliedAt: string;
  verifiedAt: string | null;
}

export interface AllyAlertItem {
  notificationId: string;
  title: string;
  body: string;
  relatedEntityId: string | null;
  isRead: boolean;
  createdAt: string;
  actionConfirmedAt: string | null;
  actionSummary: string | null;
}

export interface AssignedWelfareCaseSummary {
  id: string;
  publicCode: string;
  type: string;
  status: string;
  severity: string;
  canton: string;
  autoRoutingRequested: boolean;
  suggestedOrganizationUserId: string | null;
  suggestedRole: string | null;
  suggestedDistanceMetres: number | null;
  assignedOrganizationUserId: string | null;
  assignedRole: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface AssignedWelfareCaseDetail {
  id: string;
  publicCode: string;
  type: string;
  status: string;
  severity: string;
  canton: string;
  description: string;
  approxLat: number | null;
  approxLng: number | null;
  createdAt: string;
  evidence: Array<{
    id: string;
    evidenceKind: string;
    contentType: string;
    fileSizeBytes: number;
    isSensitive: boolean;
    uploadedAt: string;
  }>;
}

export interface SubmitAllyApplicationRequest {
  organizationName: string;
  allyType: AllyType;
  coverageLabel: string;
  coverageLat: number;
  coverageLng: number;
  coverageRadiusMetres: number;
}

export const alliesApi = {
  getMyProfile: () => apiClient.get<AllyProfile | null>("/allies/me").then((response) => response.data),

  submitApplication: (payload: SubmitAllyApplicationRequest) =>
    apiClient.post<AllyProfile>("/allies/me/application", payload).then((response) => response.data),

  getMyAlerts: () => apiClient.get<AllyAlertItem[]>("/allies/me/alerts").then((response) => response.data),

  getAssignedWelfareCases: (page = 1, pageSize = 30) =>
    apiClient
      .get<{
        items: AssignedWelfareCaseSummary[];
        page: number;
        pageSize: number;
      }>("/welfare-cases/assigned", { params: { page, pageSize } })
      .then((response) => response.data),

  getAssignedWelfareCase: (caseId: string) =>
    apiClient.get<AssignedWelfareCaseDetail>(`/welfare-cases/assigned/${caseId}`).then((response) => response.data),

  downloadAssignedWelfareEvidence: (caseId: string, evidenceId: string) =>
    apiClient
      .get<Blob>(`/welfare-cases/assigned/${caseId}/evidence/${evidenceId}/download`, {
        responseType: "blob",
      })
      .then((response) => response.data),

  confirmAlertAction: (notificationId: string, actionSummary: string) =>
    apiClient.put(`/allies/me/alerts/${notificationId}/action`, { actionSummary }).then(() => undefined),
};
