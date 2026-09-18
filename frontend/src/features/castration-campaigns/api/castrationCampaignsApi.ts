import { apiClient } from "@/shared/lib/apiClient";

export interface CastrationCampaign {
  id: string;
  title: string;
  venueLabel: string;
  canton: string;
  latitude: number;
  longitude: number;
  startsAt: string;
  endsAt: string;
  reservationsCloseAt: string;
  capacity: number;
  reservedCount: number;
  availableCapacity: number;
  basePriceCrc: number;
  consentVersion: string;
  status: "Published" | "Active";
}

export interface CastrationCampaignPage {
  items: CastrationCampaign[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateCastrationCampaignRequest {
  executingClinicId: string;
  title: string;
  venueLabel: string;
  canton: string;
  latitude: number;
  longitude: number;
  startsAt: string;
  endsAt: string;
  reservationsOpenAt: string;
  reservationsCloseAt: string;
  capacity: number;
  basePriceCrc: number;
  consentVersion: string;
}

export interface CastrationAppointment {
  id: string;
  campaignId: string;
  petId: string;
  ownerUserId: string;
  scheduledAt: string;
  basePriceCrc: number;
  ivaAmountCrc: number;
  totalAmountCrc: number;
  status: "Reserved" | "Confirmed" | "CheckedIn" | "Completed" | "NoShow" | "Cancelled" | "Rejected";
  executingVeterinarianId: string | null;
  clinicalOutcome: string | null;
  postOperativeInstructions: string | null;
}

export interface CastrationAppointmentPage {
  items: CastrationAppointment[];
  total: number;
  page: number;
  pageSize: number;
}
export interface ReserveAppointmentRequest {
  petId: string;
  scheduledAt: string;
  weightKg: number;
  confirmsFastingInstructions: boolean;
  isPregnant: boolean;
  isInHeat: boolean;
  consentAccepted: boolean;
  consentVersion: string;
  requiresInvoice: boolean;
}

export const castrationCampaignsApi = {
  getPublished: (canton: string, page = 1) =>
    apiClient
      .get<CastrationCampaignPage>("/castration-campaigns", {
        params: { canton: canton.trim() || undefined, page, pageSize: 20 },
      })
      .then((response) => response.data),
  create: (request: CreateCastrationCampaignRequest) =>
    apiClient.post<CastrationCampaign>("/castration-campaigns", request).then((r) => r.data),
  submit: (campaignId: string) =>
    apiClient.post<CastrationCampaign>(`/castration-campaigns/${campaignId}/submit`).then((r) => r.data),
  approve: (campaignId: string) =>
    apiClient.post<CastrationCampaign>(`/castration-campaigns/${campaignId}/approve`).then((r) => r.data),
  publish: (campaignId: string) =>
    apiClient.post<CastrationCampaign>(`/castration-campaigns/${campaignId}/publish`).then((r) => r.data),
  reserve: (campaignId: string, request: ReserveAppointmentRequest) =>
    apiClient
      .post<CastrationAppointment>(`/castration-campaigns/${campaignId}/appointments`, request)
      .then((r) => r.data),
  getMine: () =>
    apiClient.get<CastrationAppointmentPage>("/castration-campaigns/appointments/mine").then((r) => r.data),
  getAgenda: (campaignId: string) =>
    apiClient.get<CastrationAppointmentPage>(`/castration-campaigns/${campaignId}/appointments`).then((r) => r.data),
  operate: (
    appointmentId: string,
    operation: string,
    details?: { veterinarianId?: string; clinicalOutcome?: string; postOperativeInstructions?: string },
  ) =>
    apiClient
      .post<CastrationAppointment>(`/castration-campaigns/appointments/${appointmentId}/operate`, {
        operation,
        ...details,
      })
      .then((r) => r.data),
};
