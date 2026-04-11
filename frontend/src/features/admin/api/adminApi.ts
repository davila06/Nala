import { apiClient } from '@/shared/lib/apiClient'

export interface PendingAllyDto {
  userId: string
  organizationName: string
  allyType: string
  coverageLabel: string
  coverageLat: number
  coverageLng: number
  coverageRadiusMetres: number
  verificationStatus: string
  appliedAt: string
  verifiedAt: string | null
}

export interface PendingClinicDto {
  id: string
  name: string
  licenseNumber: string
  address: string
  lat: number
  lng: number
  contactEmail: string
  status: string
  registeredAt: string
}

export const adminApi = {
  getPendingAllies: () =>
    apiClient.get<PendingAllyDto[]>('/allies/admin/pending').then((r) => r.data),

  reviewAlly: (userId: string, approve: boolean) =>
    apiClient.post<void>(`/allies/admin/applications/${userId}/review`, { approve }),

  getPendingClinics: () =>
    apiClient.get<PendingClinicDto[]>('/clinics/admin/pending').then((r) => r.data),

  reviewClinic: (clinicId: string, approve: boolean) =>
    apiClient.put<void>(`/clinics/admin/${clinicId}/review`, { approve }),
}
