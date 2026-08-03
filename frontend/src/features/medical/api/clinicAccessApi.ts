import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export interface ClinicAccessGrantDto {
  id: string;
  petId: string;
  clinicId: string;
  clinicName: string;
  initiatedBy: "Owner" | "Clinic";
  isPending: boolean;
  isActive: boolean;
  acceptedAt: string | null;
  codeExpiresAt: string;
  createdAt: string;
}

export interface GeneratedAccessCodeDto {
  grantId: string;
  rawCode: string;
  expiresAt: string;
  initiatedBy: "Owner" | "Clinic";
}

export interface AuthorizedPetDto {
  petId: string;
  petName: string;
  species: string;
  photoUrl: string | null;
  grantedAt: string;
  grantId: string;
}

// ── Owner API ─────────────────────────────────────────────────────────────────

export const clinicAccessApi = {
  // Owner: list all grants for a pet
  getGrantsForPet: (petId: string): Promise<ClinicAccessGrantDto[]> =>
    apiClient
      .get<ClinicAccessGrantDto[]>(`/pets/${petId}/clinic-access`)
      .then((r) => r.data),

  // Owner: generate a code to hand to the clinic
  ownerGenerateCode: (
    petId: string,
    clinicId: string,
  ): Promise<GeneratedAccessCodeDto> =>
    apiClient
      .post<GeneratedAccessCodeDto>(`/pets/${petId}/clinic-access/code`, {
        clinicId,
      })
      .then((r) => r.data),

  // Owner: accept a code from the clinic
  ownerAcceptCode: (
    petId: string,
    code: string,
  ): Promise<ClinicAccessGrantDto> =>
    apiClient
      .post<ClinicAccessGrantDto>(`/pets/${petId}/clinic-access/accept`, {
        code: code.trim().toUpperCase(),
      })
      .then((r) => r.data),

  // Owner: revoke a clinic's access
  revokeAccess: (petId: string, clinicId: string): Promise<void> =>
    apiClient
      .delete(`/pets/${petId}/clinic-access/${clinicId}`)
      .then(() => undefined),

  // Clinic: generate a code to hand to the owner
  clinicGenerateCode: (petId: string): Promise<GeneratedAccessCodeDto> =>
    apiClient
      .post<GeneratedAccessCodeDto>("/clinics/access-grants/code", { petId })
      .then((r) => r.data),

  // Clinic: accept a code from the owner
  clinicAcceptCode: (code: string): Promise<ClinicAccessGrantDto> =>
    apiClient
      .post<ClinicAccessGrantDto>("/clinics/access-grants/accept", {
        code: code.trim().toUpperCase(),
      })
      .then((r) => r.data),

  // Clinic: list all pets with active grants
  getAuthorizedPets: (): Promise<AuthorizedPetDto[]> =>
    apiClient
      .get<AuthorizedPetDto[]>("/clinics/access-grants")
      .then((r) => r.data),
};
