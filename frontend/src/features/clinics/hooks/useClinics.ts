import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { clinicsApi, type UpdateClinicProfileRequest, type VeterinarianAppointmentStatus } from "../api/clinicsApi";

export function usePublicClinics(
  lat?: number,
  lng?: number,
  enabled = true,
  options?: {
    search?: string;
    emergencyOnly?: boolean;
    page?: number;
    pageSize?: number;
  },
) {
  return useQuery({
    queryKey: ["clinics", "public", lat, lng, options],
    queryFn: () => clinicsApi.getPublicClinics(lat, lng, options),
    staleTime: 60_000,
    enabled,
  });
}

export function usePublicClinicProfile(clinicId: string) {
  return useQuery({
    queryKey: ["clinics", "public-profile", clinicId],
    queryFn: () => clinicsApi.getPublicProfile(clinicId),
    staleTime: 60_000,
    enabled: Boolean(clinicId),
  });
}

/** Search active clinics by name or license number (min 2 chars) to authorize medical access. */
export function useClinicAccessSearch(query: string) {
  const trimmed = query.trim();
  return useQuery({
    queryKey: ["clinics", "search", trimmed],
    queryFn: () => clinicsApi.searchForAccess(trimmed),
    staleTime: 30_000,
    enabled: trimmed.length >= 2,
  });
}

export function useClinicScanStats(year?: number, month?: number) {
  const now = new Date();
  return useQuery({
    queryKey: ["clinics", "stats", year ?? now.getFullYear(), month ?? now.getMonth() + 1],
    queryFn: () => clinicsApi.getScanStats(year, month),
    staleTime: 30_000,
  });
}

export function useUploadClinicLogo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => clinicsApi.uploadLogo(file),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["my-clinic"] });
    },
  });
}

export function useUpdateClinicProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateClinicProfileRequest) => clinicsApi.updateMyProfile(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["my-clinic"] });
    },
  });
}

export function useClinicApiKeys() {
  return useQuery({
    queryKey: ["clinics", "api-keys"],
    queryFn: clinicsApi.getApiKeys,
    staleTime: 30_000,
  });
}

export function useCreateClinicApiKey() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ label, scopes }: { label: string; scopes?: string[] }) => clinicsApi.createApiKey(label, scopes),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "api-keys"] });
    },
  });
}

export function useRevokeClinicApiKey() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (keyId: string) => clinicsApi.revokeApiKey(keyId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "api-keys"] });
    },
  });
}
export function useClinicNearbyAlerts(radiusKm = 15) {
  return useQuery({
    queryKey: ["clinics", "nearby-alerts", radiusKm],
    queryFn: () => clinicsApi.getNearbyAlerts(radiusKm),
    staleTime: 30_000,
    refetchInterval: 60_000,
  });
}

export function useClinicVisibilityStats(days = 30) {
  return useQuery({
    queryKey: ["clinics", "visibility-stats", days],
    queryFn: () => clinicsApi.getVisibilityStats(days),
    staleTime: 300_000,
    retry: (count, err: { response?: { status?: number } }) => err?.response?.status !== 402 && count < 2,
  });
}

export function useClinicAgenda(from: string, to: string) {
  return useQuery({
    queryKey: ["clinics", "agenda", from, to],
    queryFn: () => clinicsApi.getAgenda(from, to),
    staleTime: 15_000,
    enabled: Boolean(from && to),
  });
}

export function useUpdateClinicAppointmentStatus(from: string, to: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ appointmentId, status }: { appointmentId: string; status: VeterinarianAppointmentStatus }) =>
      clinicsApi.updateAppointmentStatus(appointmentId, status),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["clinics", "agenda", from, to],
      });
    },
  });
}

export function useRescheduleClinicAppointment(from: string, to: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      appointmentId,
      startsAt,
      durationMinutes,
    }: {
      appointmentId: string;
      startsAt: string;
      durationMinutes: number;
    }) => clinicsApi.rescheduleAppointment(appointmentId, startsAt, durationMinutes),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["clinics", "agenda", from, to],
      });
    },
  });
}

export function useCreateClinicScheduleBlock(from: string, to: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      veterinarianId,
      startsAt,
      endsAt,
      reason,
    }: {
      veterinarianId: string;
      startsAt: string;
      endsAt: string;
      reason: string;
    }) => clinicsApi.createScheduleBlock(veterinarianId, startsAt, endsAt, reason),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["clinics", "agenda", from, to],
      });
    },
  });
}

export function useClinicScheduleBlocks(from: string, to: string) {
  return useQuery({
    queryKey: ["clinics", "schedule-blocks", from, to],
    queryFn: () => clinicsApi.getScheduleBlocks(from, to),
    staleTime: 15_000,
    enabled: Boolean(from && to),
  });
}

export function useUpdateClinicScheduleBlock(from: string, to: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      blockId,
      startsAt,
      endsAt,
      reason,
    }: {
      blockId: string;
      startsAt: string;
      endsAt: string;
      reason: string;
    }) => clinicsApi.updateScheduleBlock(blockId, startsAt, endsAt, reason),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "schedule-blocks", from, to] });
    },
  });
}

export function useDeleteClinicScheduleBlock(from: string, to: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (blockId: string) => clinicsApi.deleteScheduleBlock(blockId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "schedule-blocks", from, to] });
    },
  });
}

export function useClinicAgendaAudit(from: string, to: string) {
  return useQuery({
    queryKey: ["clinics", "agenda-audit", from, to],
    queryFn: () => clinicsApi.getAgendaAudit(from, to),
    staleTime: 30_000,
    enabled: Boolean(from && to),
  });
}

export function useDownloadClinicAgendaAuditCsv() {
  return useMutation({
    mutationFn: ({ from, to }: { from: string; to: string }) => clinicsApi.downloadAgendaAuditCsv(from, to),
  });
}

export function useCreateClinicalConsultation(from: string, to: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      appointmentId,
      payload,
    }: {
      appointmentId: string;
      payload: Parameters<typeof clinicsApi.createConsultation>[1];
    }) => clinicsApi.createConsultation(appointmentId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "agenda", from, to] });
      void queryClient.invalidateQueries({ queryKey: ["clinics", "agenda-audit", from, to] });
    },
  });
}

export function useCloseClinicalConsultation(from: string, to: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      consultationId,
      signedByName,
      inventoryUses = [],
    }: {
      consultationId: string;
      signedByName: string;
      inventoryUses?: Parameters<typeof clinicsApi.closeConsultationWithInventory>[2];
    }) => clinicsApi.closeConsultationWithInventory(consultationId, signedByName, inventoryUses),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "agenda", from, to] });
      void queryClient.invalidateQueries({ queryKey: ["clinics", "agenda-audit", from, to] });
    },
  });
}

export function useClinicInventory() {
  return useQuery({
    queryKey: ["clinics", "inventory"],
    queryFn: clinicsApi.getInventory,
    staleTime: 30_000,
  });
}

export function useClinicInventoryValuation() {
  return useQuery({
    queryKey: ["clinics", "inventory", "valuation"],
    queryFn: clinicsApi.getInventoryValuation,
    staleTime: 30_000,
  });
}

export function useAddClinicInventoryItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: clinicsApi.addInventoryItem,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "inventory"] });
      void queryClient.invalidateQueries({ queryKey: ["clinics", "inventory", "valuation"] });
    },
  });
}

export function useReceiveClinicInventoryLot() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      itemId,
      payload,
    }: {
      itemId: string;
      payload: Parameters<typeof clinicsApi.receiveInventoryLot>[1];
    }) => clinicsApi.receiveInventoryLot(itemId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "inventory"] });
      void queryClient.invalidateQueries({ queryKey: ["clinics", "inventory", "valuation"] });
    },
  });
}

export function useAdjustClinicInventoryLot() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ lotId, quantityDelta, reason }: { lotId: string; quantityDelta: number; reason: string }) =>
      clinicsApi.adjustInventoryLot(lotId, quantityDelta, reason),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinics", "inventory"] });
      void queryClient.invalidateQueries({ queryKey: ["clinics", "inventory", "valuation"] });
    },
  });
}

export function useUploadClinicalConsultationAttachment() {
  return useMutation({
    mutationFn: ({ consultationId, file }: { consultationId: string; file: File }) =>
      clinicsApi.uploadConsultationAttachment(consultationId, file),
  });
}

export function useClinicalConsultationTemplates() {
  return useQuery({
    queryKey: ["clinics", "consultation-templates"],
    queryFn: clinicsApi.getConsultationTemplates,
    staleTime: 300_000,
  });
}

export function useDownloadClinicalConsultationPrescription() {
  return useMutation({
    mutationFn: (consultationId: string) => clinicsApi.downloadConsultationPrescription(consultationId),
  });
}

export function useEmergencyVets(lat?: number, lng?: number, radiusKm = 30) {
  return useQuery({
    queryKey: ["clinics", "emergency", lat, lng, radiusKm],
    queryFn: () => clinicsApi.getEmergencyVets(lat, lng, radiusKm),
    staleTime: 5 * 60_000,
    enabled: true,
  });
}
