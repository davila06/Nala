import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { medicalApi, type AddMedicalRecordPayload } from "../api/medicalApi";

export function useMedicalHistory(petId: string) {
  return useQuery({
    queryKey: ["medical", petId],
    queryFn: () => medicalApi.getHistory(petId),
    staleTime: 30_000,
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
