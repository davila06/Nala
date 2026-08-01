import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type MedicalRecordType =
  | "Vaccine"
  | "Deworming"
  | "Checkup"
  | "Surgery"
  | "Medication"
  | "Allergy"
  | "Other";

export interface MedicalRecordDto {
  id: string;
  petId: string;
  type: MedicalRecordType;
  date: string; // DateOnly as "YYYY-MM-DD"
  description: string;
  vetName: string | null;
  clinicName: string | null;
  nextDueDate: string | null;
  documentUrl: string | null;
  createdAt: string;
}

export interface VetReminderDto {
  id: string;
  petId: string;
  type: MedicalRecordType;
  dueDate: string; // DateOnly
  title: string;
  notes: string | null;
  isCompleted: boolean;
}

export interface AddMedicalRecordPayload {
  type: MedicalRecordType;
  date: string;
  description: string;
  vetName?: string;
  clinicName?: string;
  nextDueDate?: string;
  document?: File;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const medicalApi = {
  getHistory: (petId: string): Promise<MedicalRecordDto[]> =>
    apiClient
      .get<MedicalRecordDto[]>(`/pets/${petId}/medical`)
      .then((r) => r.data),

  addRecord: (
    petId: string,
    payload: AddMedicalRecordPayload,
  ): Promise<MedicalRecordDto> => {
    const form = new FormData();
    form.append("type", payload.type);
    form.append("date", payload.date);
    form.append("description", payload.description);
    if (payload.vetName) form.append("vetName", payload.vetName);
    if (payload.clinicName) form.append("clinicName", payload.clinicName);
    if (payload.nextDueDate) form.append("nextDueDate", payload.nextDueDate);
    if (payload.document) form.append("document", payload.document);

    return apiClient
      .post<MedicalRecordDto>(`/pets/${petId}/medical`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },

  getReminders: (petId: string): Promise<VetReminderDto[]> =>
    apiClient
      .get<VetReminderDto[]>(`/pets/${petId}/medical/reminders`)
      .then((r) => r.data),

  completeReminder: (petId: string, reminderId: string): Promise<void> =>
    apiClient
      .put(`/pets/${petId}/medical/reminders/${reminderId}/complete`)
      .then(() => undefined),

  exportPdf: (petId: string): Promise<Blob> =>
    apiClient
      .get(`/pets/${petId}/medical/export`, { responseType: "blob" })
      .then((r) => r.data as Blob),
};
