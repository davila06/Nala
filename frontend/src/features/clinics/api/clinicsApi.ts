import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type ClinicStatus = "Pending" | "Active" | "Suspended";
export type ScanInputType = "Qr" | "RfidChip";

export interface ClinicDto {
  id: string;
  name: string;
  licenseNumber: string;
  address: string;
  lat: number;
  lng: number;
  contactEmail: string;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  description: string | null;
  services: string | null;
  openingHours: string | null;
  isFeatured: boolean;
  isEmergency24h: boolean;
  emergencyPhone: string | null;
  whatsAppNumber: string | null;
  isWhatsAppContactEnabled: boolean;
  status: ClinicStatus;
  registeredAt: string;
}

export interface ClinicAccessSearchResultDto {
  id: string;
  name: string;
  licenseNumber: string;
}

export interface PublicClinicDto {
  id: string;
  name: string;
  address: string;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  lat: number;
  lng: number;
  isFeatured: boolean;
  isEmergency24h: boolean;
  emergencyPhone: string | null;
  whatsAppNumber: string | null;
  status: string;
}

export interface PublicClinicProfileDto {
  id: string;
  name: string;
  address: string;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  description: string | null;
  services: string | null;
  openingHours: string | null;
  lat: number;
  lng: number;
  isFeatured: boolean;
  isEmergency24h: boolean;
  emergencyPhone: string | null;
}

export interface EmergencyVetDto {
  id: string;
  name: string;
  address: string;
  emergencyPhone: string | null;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  lat: number;
  lng: number;
  distanceKm: number | null;
}

export interface ClinicScanResultDto {
  scanId: string;
  matched: boolean;
  petId: string | null;
  petName: string | null;
  petPhotoUrl: string | null;
  ownerName: string | null;
  ownerEmail: string | null;
  petSpecies: string | null;
}

export interface ClinicScanDayStat {
  day: string;
  total: number;
  matched: number;
  qrCount: number;
  rfidCount: number;
}

export interface ClinicScanStatsDto {
  year: number;
  month: number;
  totalScans: number;
  matchedScans: number;
  qrScans: number;
  rfidScans: number;
  byDay: ClinicScanDayStat[];
}

export interface ClinicApiKeyDto {
  id: string;
  label: string;
  isRevoked: boolean;
  createdAt: string;
  lastUsedAt: string | null;
  expiresAt: string;
  scopes: string[];
  rawKey?: string;
}

export interface RegisterClinicRequest {
  name: string;
  licenseNumber: string;
  address: string;
  lat: number;
  lng: number;
  contactEmail: string;
  password: string;
}

export interface UpdateClinicProfileRequest {
  name: string;
  address: string;
  phoneNumber: string | null;
  website: string | null;
  isEmergency24h: boolean | null;
  emergencyPhone: string | null;
  description?: string | null;
  services?: string | null;
  openingHours?: string | null;
  whatsAppNumber?: string | null;
  isWhatsAppContactEnabled?: boolean;
}

export interface NearbyAlertDto {
  lostPetEventId: string;
  petName: string;
  petSpecies: string | null;
  lastSeenLat: number | null;
  lastSeenLng: number | null;
  reportedAt: string;
  recentPhotoUrl: string | null;
}

export interface ClinicVisibilityStatsDto {
  periodDays: number;
  profileViews: number;
  mapClicks: number;
  searchAppearances: number;
  alertImpressions: number;
  scanResultViews: number;
}

export type VeterinarianAppointmentStatus =
  | "Scheduled"
  | "Confirmed"
  | "CheckedIn"
  | "InConsultation"
  | "Completed"
  | "NoShow"
  | "Cancelled";

export interface ClinicAgendaItemDto {
  appointmentId: string;
  clinicId: string;
  veterinarianId: string;
  veterinarianName: string;
  petId: string;
  petName: string;
  startsAt: string;
  endsAt: string;
  status: VeterinarianAppointmentStatus;
}

export interface ClinicScheduleBlockDto {
  blockId: string;
  clinicId: string;
  veterinarianId: string;
  veterinarianName: string;
  startsAt: string;
  endsAt: string;
  reason: string;
}

export interface ClinicAgendaAuditEntryDto {
  id: string;
  adminUserId: string;
  action: string;
  entityType: string;
  entityId: string;
  details: string | null;
  performedAt: string;
}

export interface ClinicalConsultationDto {
  id: string;
  appointmentId: string;
  petId: string;
  veterinarianId: string;
  status: "Draft" | "Closed";
  reason: string;
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
  weightKg: number | null;
  temperatureC: number | null;
  heartRateBpm: number | null;
  respiratoryRateRpm: number | null;
  bodyConditionScore: number | null;
  painScore: number | null;
  hydrationStatus: string | null;
  diagnosis: string;
  treatment: string;
  ownerSummary: string;
  prescriptionInstructions: string | null;
  attachmentUrl: string | null;
  signedByName: string | null;
  closedAt: string | null;
}

export type ClinicalConsultationCreatePayload = Omit<
  ClinicalConsultationDto,
  "id" | "appointmentId" | "petId" | "veterinarianId" | "status" | "attachmentUrl" | "signedByName" | "closedAt"
>;

export interface ClinicalConsultationTemplateDto {
  key: string;
  label: string;
  reason: string;
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
  diagnosis: string;
  treatment: string;
  ownerSummary: string;
  prescriptionInstructions: string | null;
}

export type ClinicInventoryItemType = "Vaccine" | "Medication" | "Dewormer" | "Supply" | "Food" | "Service";
export type ClinicInventoryMovementReason =
  | "StockReceived"
  | "ConsultationUse"
  | "Sale"
  | "Adjustment"
  | "Expired"
  | "Damaged";

export interface ClinicInventoryItemDto {
  id: string;
  clinicId: string;
  name: string;
  type: ClinicInventoryItemType;
  unit: string;
  minimumStock: number;
  totalAvailable: number;
  isBelowMinimum: boolean;
  isActive: boolean;
  lots: ClinicInventoryLotDto[] | null;
}

export interface ClinicInventoryLotDto {
  id: string;
  itemId: string;
  lotNumber: string;
  expiresAt: string | null;
  initialQuantity: number;
  availableQuantity: number;
  unitCostCrc: number;
  supplierName: string | null;
  locationName: string;
}

export interface ClinicInventoryValuationDto {
  clinicId: string;
  totalValueCrc: number;
  totalUnits: number;
  lines: Array<{
    itemId: string;
    itemName: string;
    type: ClinicInventoryItemType;
    availableQuantity: number;
    valueCrc: number;
    isBelowMinimum: boolean;
  }>;
  byLocation: Array<{ locationName: string; availableQuantity: number; valueCrc: number }>;
}

export interface ClinicalInventoryUseInput {
  itemId: string;
  quantity: number;
  reason: ClinicInventoryMovementReason;
}

export type ClinicSaleLineType = "Service" | "InventoryItem" | "Other";
export type ClinicPaymentMethod = "Cash" | "Card" | "Sinpe" | "Transfer" | "InternalCredit";

export interface ClinicSaleCreatePayload {
  appointmentId?: string | null;
  consultationId?: string | null;
  petId?: string | null;
  receiptNumber: string;
  lines: Array<{
    description: string;
    type: ClinicSaleLineType;
    quantity: number;
    unitPriceCrc: number;
    inventoryItemId?: string | null;
    inventoryLotId?: string | null;
  }>;
  discountCrc?: number;
  discountReason?: string | null;
}

export interface ClinicSaleDto {
  id: string;
  receiptNumber: string;
  status: string;
  subtotalCrc: number;
  discountCrc: number;
  totalCrc: number;
  paidCrc: number;
  balanceCrc: number;
}

export interface ClinicSaleLedgerDto {
  sale: ClinicSaleDto;
  payments: Array<{
    id: string;
    amountCrc: number;
    method: ClinicPaymentMethod;
    reference: string | null;
    receivedAt: string;
  }>;
  refunds: Array<{
    id: string;
    paymentId: string;
    amountCrc: number;
    method: ClinicPaymentMethod;
    evidenceReference: string;
    refundedAt: string;
  }>;
}

export interface ClinicFinanceWorkspaceDto {
  clinicId: string;
  clinicName: string;
  role: "Cashier" | "Administrator";
}

export type ClinicStaffRole = "Veterinarian" | "Receptionist" | "Assistant" | "ReadOnly";

export interface ClinicStaffMemberDto {
  userId: string;
  email: string;
  role: ClinicStaffRole;
  veterinarianId: string | null;
  isRevoked: boolean;
}

export interface ClinicStaffWorkspaceDto {
  clinicId: string;
  clinicName: string;
  role: ClinicStaffRole;
}

export interface ClinicFinanceMembershipDto {
  id: string;
  userId: string;
  email: string;
  role: ClinicFinanceWorkspaceDto["role"];
  isRevoked: boolean;
  grantedAt: string;
}

export interface ClinicFiscalSubmissionDto {
  id: string;
  status: "PendingProvider" | "SubmittedToProvider" | "Failed";
  providerReference: string | null;
}

export interface ClinicCommunicationTemplateDto {
  key: string;
  label: string;
  purpose: ClinicCommunicationPurpose;
  whatsAppTemplateName: string;
}

export interface ClinicSalesReportDto {
  totalPaidCrc: number;
  byPaymentMethod: Record<string, number>;
  byService: Record<string, number>;
  byVeterinarian: Record<string, number>;
}

export type ClinicCommunicationChannel = "WhatsApp" | "Email" | "Phone" | "InApp";
export type ClinicCommunicationPurpose =
  | "AppointmentConfirmation"
  | "ClinicalFollowUp"
  | "VaccineReminder"
  | "PrescriptionDelivery"
  | "Billing"
  | "Marketing";
export type ClinicCommunicationDirection = "Outbound" | "Inbound";
export type ClinicCommunicationStatus = "Draft" | "Queued" | "Sent" | "Delivered" | "Failed" | "LoggedExternally";
export type ClinicCrmTaskType =
  | "CallClient"
  | "ConfirmAppointment"
  | "FollowUpTreatment"
  | "SendDocument"
  | "CollectPayment"
  | "Reactivation";

export interface ClinicCrmDashboardDto {
  preferences: Array<{
    id: string;
    petId: string;
    petName: string;
    ownerUserId: string;
    ownerName: string;
    channel: ClinicCommunicationChannel;
    purpose: ClinicCommunicationPurpose;
    isOptedIn: boolean;
    consentSource: string;
    updatedAt: string;
  }>;
  recentActivities: Array<{
    id: string;
    petId: string;
    petName: string;
    ownerUserId: string;
    ownerName: string;
    channel: ClinicCommunicationChannel;
    purpose: ClinicCommunicationPurpose;
    direction: ClinicCommunicationDirection;
    status: ClinicCommunicationStatus;
    subject: string;
    body: string;
    createdAt: string;
  }>;
  openTasks: Array<{
    id: string;
    petId: string;
    petName: string;
    ownerUserId: string;
    ownerName: string;
    type: ClinicCrmTaskType;
    status: string;
    dueDate: string;
    title: string;
    notes: string | null;
  }>;
  segments: Array<{ key: string; label: string; count: number; petIds: string[] }>;
}

export interface OwnerClinicCommunicationPreferenceDto {
  clinicId: string;
  clinicName: string;
  channel: ClinicCommunicationChannel | null;
  purpose: ClinicCommunicationPurpose | null;
  isOptedIn: boolean;
}

// ── API client methods ─────────────────────────────────────────────────────────

export const clinicsApi = {
  register: (payload: RegisterClinicRequest): Promise<ClinicDto> =>
    apiClient.post<ClinicDto>("/clinics/register", payload).then((r) => r.data),

  getMyClinic: (): Promise<ClinicDto> => apiClient.get<ClinicDto>("/clinics/me").then((r) => r.data),

  updateMyProfile: (payload: UpdateClinicProfileRequest): Promise<ClinicDto> =>
    apiClient.put<ClinicDto>("/clinics/me/profile", payload).then((r) => r.data),

  scan: (input: string, inputType: ScanInputType): Promise<ClinicScanResultDto> =>
    apiClient.post<ClinicScanResultDto>("/clinics/scan", { input, inputType }).then((r) => r.data),

  getPublicClinics: (
    lat?: number,
    lng?: number,
    options?: {
      search?: string;
      emergencyOnly?: boolean;
      page?: number;
      pageSize?: number;
    },
  ): Promise<PublicClinicDto[]> =>
    apiClient
      .get<PublicClinicDto[]>("/clinics/public", {
        params: { lat, lng, ...options },
      })
      .then((r) => r.data),

  getPublicProfile: (clinicId: string): Promise<PublicClinicProfileDto> =>
    apiClient.get<PublicClinicProfileDto>(`/clinics/public/${clinicId}`).then((r) => r.data),

  searchForAccess: (query: string): Promise<ClinicAccessSearchResultDto[]> =>
    apiClient.get<ClinicAccessSearchResultDto[]>("/clinics/search", { params: { query } }).then((r) => r.data),

  getScanStats: (year?: number, month?: number): Promise<ClinicScanStatsDto> =>
    apiClient.get<ClinicScanStatsDto>("/clinics/me/stats", { params: { year, month } }).then((r) => r.data),

  uploadLogo: (file: File): Promise<{ logoUrl: string }> => {
    const form = new FormData();
    form.append("file", file);
    return apiClient
      .post<{ logoUrl: string }>("/clinics/me/logo", form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },

  getApiKeys: (): Promise<ClinicApiKeyDto[]> =>
    apiClient.get<ClinicApiKeyDto[]>("/clinics/me/api-keys").then((r) => r.data),

  createApiKey: (label: string, scopes?: string[]): Promise<ClinicApiKeyDto> =>
    apiClient.post<ClinicApiKeyDto>("/clinics/me/api-keys", { label, scopes }).then((r) => r.data),

  revokeApiKey: (keyId: string): Promise<void> =>
    apiClient.delete(`/clinics/me/api-keys/${keyId}`).then(() => undefined),

  getNearbyAlerts: (radiusKm = 15): Promise<NearbyAlertDto[]> =>
    apiClient.get<NearbyAlertDto[]>("/clinics/me/nearby-alerts", { params: { radiusKm } }).then((r) => r.data),

  getVisibilityStats: (days = 30): Promise<ClinicVisibilityStatsDto> =>
    apiClient
      .get<ClinicVisibilityStatsDto>("/clinics/me/visibility-stats", {
        params: { days },
      })
      .then((r) => r.data),

  getAgenda: (from: string, to: string): Promise<ClinicAgendaItemDto[]> =>
    apiClient
      .get<ClinicAgendaItemDto[]>("/clinics/me/appointments", {
        params: { from, to },
      })
      .then((r) => r.data),

  updateAppointmentStatus: (appointmentId: string, status: VeterinarianAppointmentStatus): Promise<void> =>
    apiClient.patch(`/clinics/me/appointments/${appointmentId}/status`, { status }).then(() => undefined),

  rescheduleAppointment: (appointmentId: string, startsAt: string, durationMinutes: number): Promise<void> =>
    apiClient
      .patch(`/clinics/me/appointments/${appointmentId}/time`, { startsAt, durationMinutes })
      .then(() => undefined),

  createScheduleBlock: (
    veterinarianId: string,
    startsAt: string,
    endsAt: string,
    reason: string,
  ): Promise<{ blockId: string }> =>
    apiClient
      .post<{ blockId: string }>(`/clinics/me/veterinarians/${veterinarianId}/blocks`, { startsAt, endsAt, reason })
      .then((r) => r.data),

  getScheduleBlocks: (from: string, to: string): Promise<ClinicScheduleBlockDto[]> =>
    apiClient
      .get<ClinicScheduleBlockDto[]>("/clinics/me/schedule-blocks", { params: { from, to } })
      .then((r) => r.data),

  updateScheduleBlock: (blockId: string, startsAt: string, endsAt: string, reason: string): Promise<void> =>
    apiClient.patch(`/clinics/me/schedule-blocks/${blockId}`, { startsAt, endsAt, reason }).then(() => undefined),

  deleteScheduleBlock: (blockId: string): Promise<void> =>
    apiClient.delete(`/clinics/me/schedule-blocks/${blockId}`).then(() => undefined),

  createConsultation: (
    appointmentId: string,
    payload: ClinicalConsultationCreatePayload,
  ): Promise<ClinicalConsultationDto> =>
    apiClient
      .post<ClinicalConsultationDto>(`/clinics/me/appointments/${appointmentId}/consultation`, payload)
      .then((r) => r.data),

  closeConsultation: (consultationId: string, signedByName: string): Promise<ClinicalConsultationDto> =>
    apiClient
      .post<ClinicalConsultationDto>(`/clinics/me/consultations/${consultationId}/close`, { signedByName })
      .then((r) => r.data),

  closeConsultationWithInventory: (
    consultationId: string,
    signedByName: string,
    inventoryUses: ClinicalInventoryUseInput[],
  ): Promise<ClinicalConsultationDto> =>
    apiClient
      .post<ClinicalConsultationDto>(`/clinics/me/consultations/${consultationId}/close`, {
        signedByName,
        inventoryUses,
      })
      .then((r) => r.data),

  getInventory: (): Promise<ClinicInventoryItemDto[]> =>
    apiClient.get<ClinicInventoryItemDto[]>("/clinics/me/inventory").then((r) => r.data),

  getInventoryValuation: (): Promise<ClinicInventoryValuationDto> =>
    apiClient.get<ClinicInventoryValuationDto>("/clinics/me/inventory/valuation").then((r) => r.data),

  addInventoryItem: (payload: {
    name: string;
    type: ClinicInventoryItemType;
    unit: string;
    minimumStock: number;
  }): Promise<ClinicInventoryItemDto> =>
    apiClient.post<ClinicInventoryItemDto>("/clinics/me/inventory/items", payload).then((r) => r.data),

  receiveInventoryLot: (
    itemId: string,
    payload: {
      lotNumber: string;
      expiresAt: string | null;
      quantity: number;
      unitCostCrc: number;
      supplierName: string | null;
      locationName?: string | null;
    },
  ): Promise<ClinicInventoryLotDto> =>
    apiClient.post<ClinicInventoryLotDto>(`/clinics/me/inventory/items/${itemId}/lots`, payload).then((r) => r.data),

  adjustInventoryLot: (lotId: string, quantityDelta: number, reason: string): Promise<ClinicInventoryMovementReason> =>
    apiClient
      .post<ClinicInventoryMovementReason>(`/clinics/me/inventory/lots/${lotId}/adjustments`, { quantityDelta, reason })
      .then((r) => r.data),

  createSale: (payload: ClinicSaleCreatePayload): Promise<ClinicSaleDto> =>
    apiClient.post<ClinicSaleDto>("/clinics/me/sales", payload).then((r) => r.data),

  registerSalePayment: (
    saleId: string,
    payload: { amountCrc: number; method: ClinicPaymentMethod; reference?: string | null },
  ): Promise<ClinicSaleDto> =>
    apiClient.post<ClinicSaleDto>(`/clinics/me/sales/${saleId}/payments`, payload).then((r) => r.data),

  voidSale: (saleId: string, reason: string): Promise<ClinicSaleDto> =>
    apiClient.post<ClinicSaleDto>(`/clinics/me/sales/${saleId}/void`, { reason }).then((r) => r.data),

  closeCash: (businessDate: string): Promise<{ cashCloseId: string }> =>
    apiClient.post<{ cashCloseId: string }>("/clinics/me/cash-closes", { businessDate }).then((r) => r.data),

  getSalesReport: (businessDate: string): Promise<ClinicSalesReportDto> =>
    apiClient.get<ClinicSalesReportDto>("/clinics/me/sales-report", { params: { businessDate } }).then((r) => r.data),

  getSaleLedger: (saleId: string): Promise<ClinicSaleLedgerDto> =>
    apiClient.get<ClinicSaleLedgerDto>(`/clinics/me/sales/${saleId}/ledger`).then((r) => r.data),

  recordSaleRefund: (
    saleId: string,
    paymentId: string,
    amountCrc: number,
    reason: string,
    evidenceReference: string,
  ): Promise<ClinicSaleDto> =>
    apiClient
      .post<ClinicSaleDto>(`/clinics/me/sales/${saleId}/refunds`, { paymentId, amountCrc, reason, evidenceReference })
      .then((r) => r.data),

  submitFiscalSale: (saleId: string): Promise<ClinicFiscalSubmissionDto> =>
    apiClient.post<ClinicFiscalSubmissionDto>(`/clinics/me/sales/${saleId}/fiscal-submission`).then((r) => r.data),

  getFinanceMembers: (): Promise<ClinicFinanceMembershipDto[]> =>
    apiClient.get<ClinicFinanceMembershipDto[]>("/clinics/me/finance/members").then((r) => r.data),

  getStaffMembers: (): Promise<ClinicStaffMemberDto[]> =>
    apiClient.get<ClinicStaffMemberDto[]>("/clinics/me/staff/members").then((response) => response.data),

  grantStaffMember: (email: string, role: ClinicStaffRole, veterinarianId: string | null): Promise<{ membershipId: string }> =>
    apiClient.put<{ membershipId: string }>("/clinics/me/staff/members", { email, role, veterinarianId }).then((response) => response.data),

  revokeStaffMember: (memberUserId: string): Promise<void> =>
    apiClient.delete(`/clinics/me/staff/members/${memberUserId}`).then(() => undefined),

  getStaffWorkspaces: (): Promise<ClinicStaffWorkspaceDto[]> =>
    apiClient.get<ClinicStaffWorkspaceDto[]>("/clinics/staff-workspaces").then((response) => response.data),

  getStaffAgenda: (clinicId: string, from: string, to: string): Promise<ClinicAgendaItemDto[]> =>
    apiClient.get<ClinicAgendaItemDto[]>(`/clinics/${clinicId}/staff/appointments`, { params: { from, to } }).then((response) => response.data),

  updateStaffAppointmentStatus: (clinicId: string, appointmentId: string, status: VeterinarianAppointmentStatus): Promise<void> =>
    apiClient.patch(`/clinics/${clinicId}/staff/appointments/${appointmentId}/status`, { status }).then(() => undefined),

  createStaffConsultation: (clinicId: string, appointmentId: string, payload: ClinicalConsultationCreatePayload): Promise<ClinicalConsultationDto> =>
    apiClient.post<ClinicalConsultationDto>(`/clinics/${clinicId}/staff/appointments/${appointmentId}/consultation`, payload).then((response) => response.data),

  closeStaffConsultation: (clinicId: string, consultationId: string, signedByName: string): Promise<ClinicalConsultationDto> =>
    apiClient.post<ClinicalConsultationDto>(`/clinics/${clinicId}/staff/consultations/${consultationId}/close`, { signedByName }).then((response) => response.data),

  grantFinanceMember: (email: string, role: ClinicFinanceWorkspaceDto["role"]): Promise<{ membershipId: string }> =>
    apiClient.put<{ membershipId: string }>("/clinics/me/finance/members", { email, role }).then((r) => r.data),

  revokeFinanceMember: (memberUserId: string): Promise<void> =>
    apiClient.delete(`/clinics/me/finance/members/${memberUserId}`).then(() => undefined),

  getFinanceWorkspaces: (): Promise<ClinicFinanceWorkspaceDto[]> =>
    apiClient.get<ClinicFinanceWorkspaceDto[]>("/clinics/finance-workspaces").then((r) => r.data),

  getStaffSalesReport: (clinicId: string, businessDate: string): Promise<ClinicSalesReportDto> =>
    apiClient
      .get<ClinicSalesReportDto>(`/clinics/${clinicId}/finance/sales-report`, { params: { businessDate } })
      .then((r) => r.data),

  createStaffSale: (clinicId: string, payload: ClinicSaleCreatePayload): Promise<ClinicSaleDto> =>
    apiClient.post<ClinicSaleDto>(`/clinics/${clinicId}/finance/sales`, payload).then((r) => r.data),

  registerStaffPayment: (
    clinicId: string,
    saleId: string,
    amountCrc: number,
    method: ClinicPaymentMethod,
    reference: string,
  ): Promise<ClinicSaleDto> =>
    apiClient
      .post<ClinicSaleDto>(`/clinics/${clinicId}/finance/sales/${saleId}/payments`, { amountCrc, method, reference })
      .then((r) => r.data),

  getStaffSaleLedger: (clinicId: string, saleId: string): Promise<ClinicSaleLedgerDto> =>
    apiClient.get<ClinicSaleLedgerDto>(`/clinics/${clinicId}/finance/sales/${saleId}/ledger`).then((r) => r.data),

  recordStaffRefund: (
    clinicId: string,
    saleId: string,
    paymentId: string,
    amountCrc: number,
    reason: string,
    evidenceReference: string,
  ): Promise<ClinicSaleDto> =>
    apiClient
      .post<ClinicSaleDto>(`/clinics/${clinicId}/finance/sales/${saleId}/refunds`, {
        paymentId,
        amountCrc,
        reason,
        evidenceReference,
      })
      .then((r) => r.data),

  voidStaffSale: (clinicId: string, saleId: string, reason: string): Promise<ClinicSaleDto> =>
    apiClient.post<ClinicSaleDto>(`/clinics/${clinicId}/finance/sales/${saleId}/void`, { reason }).then((r) => r.data),

  closeStaffCash: (clinicId: string, businessDate: string): Promise<{ cashCloseId: string }> =>
    apiClient
      .post<{ cashCloseId: string }>(`/clinics/${clinicId}/finance/cash-closes`, { businessDate })
      .then((r) => r.data),

  submitStaffFiscalSale: (clinicId: string, saleId: string): Promise<ClinicFiscalSubmissionDto> =>
    apiClient
      .post<ClinicFiscalSubmissionDto>(`/clinics/${clinicId}/finance/sales/${saleId}/fiscal-submission`)
      .then((r) => r.data),

  getCommunicationTemplates: (): Promise<ClinicCommunicationTemplateDto[]> =>
    apiClient.get<ClinicCommunicationTemplateDto[]>("/clinics/me/crm/templates").then((r) => r.data),

  sendCommunicationTemplate: (
    petId: string,
    templateKey: string,
    channel: ClinicCommunicationChannel,
    requestId: string,
  ): Promise<{ activityId: string }> =>
    apiClient
      .post<{ activityId: string }>("/clinics/me/crm/send-template", { petId, templateKey, channel, requestId })
      .then((r) => r.data),

  getCrmDashboard: (today: string): Promise<ClinicCrmDashboardDto> =>
    apiClient.get<ClinicCrmDashboardDto>("/clinics/me/crm-dashboard", { params: { today } }).then((r) => r.data),

  upsertCommunicationPreference: (payload: {
    petId: string;
    channel: ClinicCommunicationChannel;
    purpose: ClinicCommunicationPurpose;
    isOptedIn: boolean;
    consentSource: string;
  }): Promise<void> => apiClient.put("/clinics/me/crm/preferences", payload).then(() => undefined),

  logCommunicationActivity: (payload: {
    petId: string;
    channel: ClinicCommunicationChannel;
    purpose: ClinicCommunicationPurpose;
    direction: ClinicCommunicationDirection;
    status: ClinicCommunicationStatus;
    subject: string;
    body: string;
    providerMessageId?: string | null;
  }): Promise<void> => apiClient.post("/clinics/me/crm/activities", payload).then(() => undefined),

  createCrmTask: (payload: {
    petId: string;
    type: ClinicCrmTaskType;
    dueDate: string;
    title: string;
    notes?: string | null;
  }): Promise<{ taskId: string }> =>
    apiClient.post<{ taskId: string }>("/clinics/me/crm/tasks", payload).then((r) => r.data),

  completeCrmTask: (taskId: string): Promise<void> =>
    apiClient.post(`/clinics/me/crm/tasks/${taskId}/complete`).then(() => undefined),

  getOwnerCommunicationPreferences: (petId: string): Promise<OwnerClinicCommunicationPreferenceDto[]> =>
    apiClient
      .get<OwnerClinicCommunicationPreferenceDto[]>(`/clinics/pets/${petId}/communication-preferences`)
      .then((r) => r.data),

  setOwnerCommunicationPreference: (
    clinicId: string,
    petId: string,
    payload: { channel: ClinicCommunicationChannel; purpose: ClinicCommunicationPurpose; isOptedIn: boolean },
  ): Promise<void> =>
    apiClient.put(`/clinics/${clinicId}/pets/${petId}/communication-preferences`, payload).then(() => undefined),

  uploadConsultationAttachment: (consultationId: string, file: File): Promise<{ attachmentUrl: string }> => {
    const form = new FormData();
    form.append("file", file);
    return apiClient
      .post<{ attachmentUrl: string }>(`/clinics/me/consultations/${consultationId}/attachment`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },

  getConsultationTemplates: (): Promise<ClinicalConsultationTemplateDto[]> =>
    apiClient.get<ClinicalConsultationTemplateDto[]>("/clinics/me/consultation-templates").then((r) => r.data),

  downloadConsultationPrescription: (consultationId: string): Promise<Blob> =>
    apiClient
      .get(`/clinics/me/consultations/${consultationId}/prescription`, { responseType: "blob" })
      .then((r) => r.data as Blob),

  getAgendaAudit: (from: string, to: string): Promise<ClinicAgendaAuditEntryDto[]> =>
    apiClient
      .get<ClinicAgendaAuditEntryDto[]>("/clinics/me/agenda-audit", { params: { from, to } })
      .then((r) => r.data),

  downloadAgendaAuditCsv: (from: string, to: string): Promise<Blob> =>
    apiClient
      .get("/clinics/me/agenda-audit", {
        params: { from, to, format: "csv" },
        responseType: "blob",
      })
      .then((r) => r.data as Blob),

  // Fire-and-forget — called when a user opens a clinic popup or profile
  trackView: (clinicId: string, source: "map" | "directory" | "search" | "alert"): void => {
    void apiClient.post(`/clinics/${clinicId}/view`, null, { params: { source } }).catch(() => undefined);
  },

  getEmergencyVets: (lat?: number, lng?: number, radiusKm = 30): Promise<EmergencyVetDto[]> =>
    apiClient.get<EmergencyVetDto[]>("/public/emergency-vets", { params: { lat, lng, radiusKm } }).then((r) => r.data),
};
