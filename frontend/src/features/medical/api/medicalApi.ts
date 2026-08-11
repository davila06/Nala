import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type MedicalRecordType =
  | "Vaccine"
  | "Deworming"
  | "Checkup"
  | "Surgery"
  | "Other"
  | "Medication"
  | "Allergy";

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
  clinicId: string | null;
  source: "Owner" | "Clinic";
  // Per-visit health metrics
  weightKg: number | null;
  // Medication-specific fields
  dosageDescription: string | null;
  frequency: string | null;
  durationDays: number | null;
  medicationEndDate: string | null;
}

export interface MedicalHistoryResultDto {
  records: MedicalRecordDto[];
  totalCount: number;
  /** "familia" | "plus_preview" | "explorador" */
  accessTier: string;
  isLimited: boolean;
  previewLimit: number | null;
}

export interface PetReminderDto {
  reminderId: string;
  petId: string;
  petName: string;
  petPhotoUrl: string | null;
  type: MedicalRecordType;
  dueDate: string;
  title: string;
  notes: string | null;
  isCompleted: boolean;
  isOverdue: boolean;
}

export interface ClinicAccessLogEntryDto {
  logId: string;
  clinicId: string;
  clinicName: string | null;
  accessedAt: string;
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
  weightKg?: number;
  dosageDescription?: string;
  frequency?: string;
  durationDays?: number;
  medicationEndDate?: string;
}

export interface UpdateMedicalRecordPayload {
  type: MedicalRecordType;
  date: string;
  description: string;
  vetName?: string;
  clinicName?: string;
  nextDueDate?: string;
  weightKg?: number;
  dosageDescription?: string;
  frequency?: string;
  durationDays?: number;
  medicationEndDate?: string;
}

export interface CreateVetReminderPayload {
  type: MedicalRecordType;
  dueDate: string;
  title: string;
  notes?: string;
}

// ── Weight history ────────────────────────────────────────────────────────────

export interface WeightEntryDto {
  date: string;
  weightKg: number;
  source: "Owner" | "Clinic";
  clinicName: string | null;
}

export interface WeightReferenceDto {
  minKg: number;
  maxKg: number;
  label: string;
}

export interface WeightHistoryDto {
  entries: WeightEntryDto[];
  reference: WeightReferenceDto | null;
  weightChangeAlert: string | null;
}

// ── Health alerts ─────────────────────────────────────────────────────────────

export type HealthAlertSeverity = "critical" | "warning" | "info";

export interface HealthAlertDto {
  recordType: string;
  protocolName: string;
  lastDate: string | null;
  dueDate: string;
  daysUntilDue: number;
  isOverdue: boolean;
  severity: HealthAlertSeverity;
}

export interface HealthScoreBreakdownItemDto {
  protocolName: string;
  recordType: string;
  isCompliant: boolean;
  lastDate: string | null;
  dueDate: string | null;
}

export interface HealthScoreDto {
  score: number;
  breakdown: HealthScoreBreakdownItemDto[];
}

// ── API ───────────────────────────────────────────────────────────────────────

export interface MedicalRecordCountDto {
  totalRecords: number;
  clinicRecords: number;
}

export const medicalApi = {
  getHistory: (petId: string): Promise<MedicalHistoryResultDto> =>
    apiClient
      .get<MedicalHistoryResultDto>(`/pets/${petId}/medical`)
      .then((r) => r.data),

  getWeightHistory: (petId: string): Promise<WeightHistoryDto> =>
    apiClient
      .get<WeightHistoryDto>(`/pets/${petId}/medical/weight-history`)
      .then((r) => r.data),

  getHealthAlerts: (petId: string): Promise<HealthAlertDto[]> =>
    apiClient
      .get<HealthAlertDto[]>(`/pets/${petId}/medical/health-alerts`)
      .then((r) => r.data),

  getHealthScore: (petId: string): Promise<HealthScoreDto> =>
    apiClient
      .get<HealthScoreDto>(`/pets/${petId}/medical/health-score`)
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
    if (payload.weightKg != null)
      form.append("weightKg", String(payload.weightKg));
    if (payload.dosageDescription)
      form.append("dosageDescription", payload.dosageDescription);
    if (payload.frequency) form.append("frequency", payload.frequency);
    if (payload.durationDays != null)
      form.append("durationDays", String(payload.durationDays));
    if (payload.medicationEndDate)
      form.append("medicationEndDate", payload.medicationEndDate);

    return apiClient
      .post<MedicalRecordDto>(`/pets/${petId}/medical`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },

  getCount: (petId: string): Promise<MedicalRecordCountDto> =>
    apiClient
      .get<MedicalRecordCountDto>(`/pets/${petId}/medical/count`)
      .then((r) => r.data),

  getMyReminders: (daysAhead = 30): Promise<PetReminderDto[]> =>
    apiClient
      .get<PetReminderDto[]>(`/me/medical/reminders`, { params: { daysAhead } })
      .then((r) => r.data),

  getAccessLog: (
    petId: string,
    limit = 50,
  ): Promise<ClinicAccessLogEntryDto[]> =>
    apiClient
      .get<
        ClinicAccessLogEntryDto[]
      >(`/pets/${petId}/medical/access-log`, { params: { limit } })
      .then((r) => r.data),

  getReminders: (petId: string): Promise<VetReminderDto[]> =>
    apiClient
      .get<VetReminderDto[]>(`/pets/${petId}/medical/reminders`)
      .then((r) => r.data),

  completeReminder: (petId: string, reminderId: string): Promise<void> =>
    apiClient
      .put(`/pets/${petId}/medical/reminders/${reminderId}/complete`)
      .then(() => undefined),

  deleteRecord: (petId: string, recordId: string): Promise<void> =>
    apiClient
      .delete(`/pets/${petId}/medical/${recordId}`)
      .then(() => undefined),

  updateRecord: (
    petId: string,
    recordId: string,
    payload: UpdateMedicalRecordPayload,
  ): Promise<MedicalRecordDto> =>
    apiClient
      .put<MedicalRecordDto>(`/pets/${petId}/medical/${recordId}`, payload)
      .then((r) => r.data),

  createReminder: (
    petId: string,
    payload: CreateVetReminderPayload,
  ): Promise<VetReminderDto> =>
    apiClient
      .post<VetReminderDto>(`/pets/${petId}/medical/reminders`, payload)
      .then((r) => r.data),

  deleteReminder: (petId: string, reminderId: string): Promise<void> =>
    apiClient
      .delete(`/pets/${petId}/medical/reminders/${reminderId}`)
      .then(() => undefined),

  exportPdf: (petId: string): Promise<Blob> =>
    apiClient
      .get(`/pets/${petId}/medical/export`, { responseType: "blob" })
      .then((r) => r.data as Blob),
};

// ── Clinic patient history ─────────────────────────────────────────────────────

export interface ClinicPatientHistoryDto {
  petId: string;
  petName: string;
  species: string;
  breed: string | null;
  photoUrl: string | null;
  lastSeenAt: string | null;
  records: MedicalRecordDto[];
}

export interface AddClinicMedicalRecordPayload {
  petId?: string; // Option A — prior scan required
  qrOrChipInput?: string; // Option B — inline scan
  inputType?: "Qr" | "RfidChip";
  recordType: MedicalRecordType;
  date: string;
  description: string;
  vetName?: string;
  nextDueDate?: string;
  document?: File;
}

export const clinicMedicalApi = {
  getPatientHistory: (petId: string): Promise<ClinicPatientHistoryDto> =>
    apiClient
      .get<ClinicPatientHistoryDto>(`/clinics/patients/${petId}/medical`)
      .then((r) => r.data),

  addRecord: (
    payload: AddClinicMedicalRecordPayload,
  ): Promise<MedicalRecordDto> => {
    const form = new FormData();
    if (payload.petId) form.append("petId", payload.petId);
    if (payload.qrOrChipInput)
      form.append("qrOrChipInput", payload.qrOrChipInput);
    if (payload.inputType) form.append("inputType", payload.inputType);
    form.append("recordType", payload.recordType);
    form.append("date", payload.date);
    form.append("description", payload.description);
    if (payload.vetName) form.append("vetName", payload.vetName);
    if (payload.nextDueDate) form.append("nextDueDate", payload.nextDueDate);
    if (payload.document) form.append("document", payload.document);

    return apiClient
      .post<MedicalRecordDto>("/clinics/patients/medical", form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },
};
