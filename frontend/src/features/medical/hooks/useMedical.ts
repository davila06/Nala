import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  medicalApi,
  type AddMedicalRecordPayload,
  type UpdateMedicalRecordPayload,
  type CreateVetReminderPayload,
} from "../api/medicalApi";

export function useMedicalHistory(petId: string) {
  return useQuery({
    queryKey: ["medical", petId],
    queryFn: () => medicalApi.getHistory(petId),
    staleTime: 30_000,
    enabled: !!petId,
    retry: false,
  });
}

export function useWeightHistory(petId: string) {
  return useQuery({
    queryKey: ["medical-weight", petId],
    queryFn: () => medicalApi.getWeightHistory(petId),
    staleTime: 5 * 60_000,
    enabled: !!petId,
    retry: (count, err: { response?: { status?: number } } | unknown) =>
      (err as { response?: { status?: number } })?.response?.status !== 403 &&
      count < 2,
  });
}

export function useHealthAlerts(petId: string) {
  return useQuery({
    queryKey: ["health-alerts", petId],
    queryFn: () => medicalApi.getHealthAlerts(petId),
    staleTime: 10 * 60_000,
    enabled: !!petId,
  });
}

export function useHealthScore(petId: string) {
  return useQuery({
    queryKey: ["health-score", petId],
    queryFn: () => medicalApi.getHealthScore(petId),
    staleTime: 10 * 60_000,
    enabled: !!petId,
    retry: (count, err: { response?: { status?: number } } | unknown) =>
      (err as { response?: { status?: number } })?.response?.status !== 403 &&
      count < 2,
  });
}

export function useMedicalCount(petId: string) {
  return useQuery({
    queryKey: ["medical-count", petId],
    queryFn: () => medicalApi.getCount(petId),
    staleTime: 60_000,
    enabled: !!petId,
  });
}

export function useMyReminders(daysAhead = 30) {
  return useQuery({
    queryKey: ["my-reminders", daysAhead],
    queryFn: () => medicalApi.getMyReminders(daysAhead),
    staleTime: 30_000,
  });
}

export function useClinicAccessLog(petId: string, limit = 50) {
  return useQuery({
    queryKey: ["clinic-access-log", petId],
    queryFn: () => medicalApi.getAccessLog(petId, limit),
    staleTime: 60_000,
    enabled: !!petId,
  });
}

export function useAddMedicalRecord(petId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: AddMedicalRecordPayload) =>
      medicalApi.addRecord(petId, payload),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["medical", petId] });
      void qc.invalidateQueries({ queryKey: ["medical-reminders", petId] });
    },
  });
}

export function useDeleteMedicalRecord(petId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (recordId: string) => medicalApi.deleteRecord(petId, recordId),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["medical", petId] }),
  });
}

export function useUpdateMedicalRecord(petId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      recordId,
      payload,
    }: {
      recordId: string;
      payload: UpdateMedicalRecordPayload;
    }) => medicalApi.updateRecord(petId, recordId, payload),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["medical", petId] }),
  });
}

export function useVetReminders(petId: string) {
  return useQuery({
    queryKey: ["medical-reminders", petId],
    queryFn: () => medicalApi.getReminders(petId),
    staleTime: 30_000,
    enabled: !!petId,
  });
}

export function useCompleteReminder(petId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (reminderId: string) =>
      medicalApi.completeReminder(petId, reminderId),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["medical-reminders", petId] }),
  });
}

export function useCreateVetReminder(petId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateVetReminderPayload) =>
      medicalApi.createReminder(petId, payload),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["medical-reminders", petId] }),
  });
}

export function useDeleteVetReminder(petId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (reminderId: string) =>
      medicalApi.deleteReminder(petId, reminderId),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["medical-reminders", petId] }),
  });
}

export function useExportMedicalPdf(petId: string) {
  return useMutation({
    mutationFn: () => medicalApi.exportPdf(petId),
    onSuccess: (blob) => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `historial-medico-${petId}.pdf`;
      a.click();
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    },
  });
}

export function useDownloadAnnualReport(petId: string) {
  return useMutation({
    mutationFn: (year: number) => medicalApi.downloadAnnualReport(petId, year),
    onSuccess: (blob, year) => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `pawtrack-informe-${year}.pdf`;
      a.click();
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    },
  });
}
