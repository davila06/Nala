import { apiClient } from "@/shared/lib/apiClient";

export type CertificateType =
  | "Vaccination"
  | "GeneralExam"
  | "Deworming"
  | "Neutering"
  | "HealthClearance"
  | "MicrochipRegistration";

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

export const CERTIFICATE_TYPE_LABELS: Record<CertificateType, string> = {
  Vaccination: "Vacunación",
  GeneralExam: "Examen General",
  Deworming: "Desparasitación",
  Neutering: "Esterilización",
  HealthClearance: "Certificado de Salud",
  MicrochipRegistration: "Registro de Microchip",
};

export const certificateApi = {
  getForPet: (petId: string) =>
    apiClient
      .get<CertificateDto[]>(`/certificates/pet/${petId}`)
      .then((r) => r.data),

  verify: (code: string) =>
    apiClient
      .get<CertificateDto | null>(`/certificates/verify/${code}`)
      .then((r) => r.data),

  issue: (request: IssueCertificateRequest) =>
    apiClient
      .post<CertificateDto>("/certificates", request)
      .then((r) => r.data),
};
