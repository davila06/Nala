import { apiClient } from "@/shared/lib/apiClient";

export type CertificateType =
  | "Vaccination"
  | "GeneralExam"
  | "Deworming"
  | "Neutering"
  | "HealthClearance"
  | "MicrochipRegistration"
  | "VaccinePassport";

export type CertificateStatus = "valid" | "expired" | "revoked";

export interface CertificateDto {
  id: string;
  petId: string;
  clinicId: string;
  type: CertificateType;
  verificationCode: string;
  pdfUrl: string | null;
  notes: string | null;
  issuedAt: string;
  validUntil: string | null;
  isRevoked: boolean;
  isValid: boolean;
}

export interface CertificateVerificationDto {
  id: string;
  type: CertificateType;
  petName: string;
  petSpecies: string;
  clinicName: string;
  verificationCode: string;
  issuedAt: string;
  validUntil: string | null;
  isRevoked: boolean;
  isValid: boolean;
}

export interface IssueCertificateRequest {
  petId: string;
  clinicId: string;
  type: CertificateType;
  notes?: string;
  validUntil?: string;
  petName: string;
  petSpecies: string;
  petBreed?: string;
  clinicName: string;
  clinicLicense: string;
  vetName: string;
}

export interface PassportVaccineEntryRequest {
  vaccineName: string;
  brand?: string;
  lotNumber?: string;
  applicationDate: string;
  validUntil?: string;
}

export interface PassportParasiteControlRequest {
  productName: string;
  applicationDate: string;
  nextDueDate?: string;
}

export interface IssueVaccinePassportRequest {
  petId: string;
  clinicId: string;
  veterinarianId: string;
  vetName: string;
  vetLicense?: string;
  petColor?: string;
  vaccines: PassportVaccineEntryRequest[];
  parasiteControl?: PassportParasiteControlRequest;
}

export interface ClinicVerificationDto {
  id: string;
  clinicId: string;
  licenseNumberSnapshot: string;
  status: "Pending" | "Verified" | "Rejected" | "Expired";
  submittedAt: string;
  verifiedAt: string | null;
  reviewedAt: string | null;
  verifiedByAdminUserId: string | null;
  reviewedByAdminUserId: string | null;
  expiresAt: string | null;
  hasDocument: boolean;
  rejectionReason: string | null;
  reviewNotes: string | null;
  revalidationRequestedAt: string | null;
}

export interface ClinicVeterinarianDto {
  id: string;
  clinicId: string;
  fullName: string;
  licenseNumber: string;
  status:
    | "PendingReview"
    | "Authorized"
    | "Rejected"
    | "Suspended"
    | "Revoked"
    | "Expired";
  canIssueCertificates: boolean;
  isActive: boolean;
  hasDocument: boolean;
  hasSignature: boolean;
  expiresAt: string | null;
  rejectionReason: string | null;
  suspensionReason: string | null;
}

export interface ClinicCertificateIssuersDto {
  verification: ClinicVerificationDto | null;
  veterinarians: ClinicVeterinarianDto[];
}

export const CERTIFICATE_TYPE_LABELS: Record<CertificateType, string> = {
  Vaccination: "Vacunación",
  GeneralExam: "Examen General",
  Deworming: "Desparasitación",
  Neutering: "Esterilización",
  HealthClearance: "Certificado de Salud",
  MicrochipRegistration: "Registro de Microchip",
  VaccinePassport: "Pasaporte veterinario",
};

export const certificateApi = {
  getForPet: (petId: string) =>
    apiClient
      .get<CertificateDto[]>(`/certificates/pet/${petId}`)
      .then((r) => r.data),

  verify: (code: string) =>
    apiClient
      .get<CertificateVerificationDto | null>(`/certificates/verify/${code}`)
      .then((r) => r.data),

  issue: (request: IssueCertificateRequest) =>
    apiClient
      .post<CertificateDto>("/certificates", request)
      .then((r) => r.data),

  getForClinic: (clinicId: string, page = 1) =>
    apiClient
      .get<
        CertificateDto[]
      >(`/certificates/clinic/${clinicId}`, { params: { page, pageSize: 10 } })
      .then((r) => r.data),

  issuePassport: (request: IssueVaccinePassportRequest) =>
    apiClient
      .post<CertificateDto>("/certificates/passport", request)
      .then((r) => r.data),

  getCertificateIssuers: () =>
    apiClient
      .get<ClinicCertificateIssuersDto>("/clinics/me/certificate-issuers")
      .then((r) => r.data),

  getMyVerification: () =>
    apiClient
      .get<ClinicVerificationDto | null>("/clinics/me/verification")
      .then((r) => r.data),

  submitVerification: () =>
    apiClient
      .post<ClinicVerificationDto>("/clinics/me/verification", {})
      .then((r) => r.data),

  uploadVerificationDocument: (file: File) => {
    const form = new FormData();
    form.append("file", file);
    return apiClient
      .post<ClinicVerificationDto>("/clinics/me/verification/document", form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },

  getMyVeterinarians: () =>
    apiClient
      .get<ClinicVeterinarianDto[]>("/clinics/me/veterinarians")
      .then((r) => r.data),

  createVeterinarian: (fullName: string, licenseNumber: string) =>
    apiClient
      .post<ClinicVeterinarianDto>("/clinics/me/veterinarians", {
        fullName,
        licenseNumber,
      })
      .then((r) => r.data),

  uploadVeterinarianDocument: (veterinarianId: string, file: File) => {
    const form = new FormData();
    form.append("file", file);
    return apiClient
      .post<ClinicVeterinarianDto>(
        `/clinics/me/veterinarians/${veterinarianId}/document`,
        form,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      )
      .then((r) => r.data);
  },

  uploadVeterinarianSignature: (veterinarianId: string, file: File) => {
    const form = new FormData();
    form.append("file", file);
    return apiClient
      .post<ClinicVeterinarianDto>(
        `/clinics/me/veterinarians/${veterinarianId}/signature`,
        form,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      )
      .then((r) => r.data);
  },

  revokeVeterinarian: (veterinarianId: string, reason: string) =>
    apiClient
      .post<ClinicVeterinarianDto>(
        `/clinics/me/veterinarians/${veterinarianId}/revoke`,
        { reason },
      )
      .then((r) => r.data),

  downloadPdf: async (certificateId: string) => {
    const response = await apiClient.get<Blob>(
      `/certificates/${certificateId}/download`,
      {
        responseType: "blob",
      },
    );
    return response.data;
  },
};
