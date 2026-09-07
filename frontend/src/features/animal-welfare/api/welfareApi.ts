import { apiClient } from "@/shared/lib/apiClient";
import type {
  WelfareCaseType,
  WelfareSeverity,
} from "@/features/admin/api/adminApi";

export interface PublicWelfareCaseStatusDto {
  publicCode: string;
  status: string;
  severity: WelfareSeverity;
  canton: string;
  createdAt: string;
  updatedAt: string;
  closedAt: string | null;
}

export interface ReportWelfareCasePayload {
  type: WelfareCaseType;
  severity: WelfareSeverity;
  canton: string;
  description: string;
  reporterIsAnonymous: boolean;
  approxLat?: number | null;
  approxLng?: number | null;
  petId?: string | null;
  lostPetEventId?: string | null;
  sightingId?: string | null;
  capturedAnimalId?: string | null;
  adoptablePetId?: string | null;
}

export const welfareApi = {
  report: (payload: ReportWelfareCasePayload) =>
    apiClient
      .post<PublicWelfareCaseStatusDto>("/public/welfare-cases", payload)
      .then((r) => r.data),

  getPublicStatus: (publicCode: string) =>
    apiClient
      .get<PublicWelfareCaseStatusDto>(`/public/welfare-cases/${publicCode}`)
      .then((r) => r.data),
};
