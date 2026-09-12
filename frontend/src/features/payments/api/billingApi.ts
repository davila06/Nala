import { apiClient } from "@/shared/lib/apiClient";

export type TaxIdentificationType = "Fisica" | "Juridica" | "Dimex" | "Nite" | "Extranjero";

export interface UserBillingProfileDto {
  id: string;
  userId: string;
  identificationType: TaxIdentificationType;
  identificationNumber: string;
  legalName: string;
  billingEmail: string;
  requiresInvoice: boolean;
  province?: string | null;
  canton?: string | null;
  district?: string | null;
  addressDetails?: string | null;
  phoneNumber?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface ElectronicInvoiceDto {
  id: string;
  userId: string;
  documentType: string;
  status: string;
  claveNumerica: string;
  numeroConsecutivo: string;
  codigoCabys: string;
  serviceDescription: string;
  subtotalCrc: number;
  ivaAmountCrc: number;
  totalAmountCrc: number;
  paymentMethodCode: string;
  receiverName: string;
  receiverEmail: string;
  receiverIdNumber?: string | null;
  pdfUrl?: string | null;
  xmlUrl?: string | null;
  issuedAt: string;
  processedAt?: string | null;
}

export interface UpsertBillingProfileRequest {
  identificationType: TaxIdentificationType;
  identificationNumber: string;
  legalName: string;
  billingEmail: string;
  requiresInvoice: boolean;
  province?: string | null;
  canton?: string | null;
  district?: string | null;
  addressDetails?: string | null;
  phoneNumber?: string | null;
}

export const billingApi = {
  getProfile: () => apiClient.get<UserBillingProfileDto | null>("/billing/profile").then((r) => r.data),

  upsertProfile: (data: UpsertBillingProfileRequest) =>
    apiClient.put<UserBillingProfileDto>("/billing/profile", data).then((r) => r.data),

  getInvoices: () => apiClient.get<ElectronicInvoiceDto[]>("/billing/invoices").then((r) => r.data),

  downloadInvoicePdf: (id: string) =>
    apiClient.get(`/billing/invoices/${id}/pdf`, { responseType: "blob" }).then((r) => r.data as Blob),
};
