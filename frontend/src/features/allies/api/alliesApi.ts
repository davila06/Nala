import { apiClient } from '@/shared/lib/apiClient'

export type AllyType =
  | 'VeterinaryClinic'
  | 'Shelter'
  | 'PetFriendlyBusiness'
  | 'PrivateSecurity'
  | 'Municipality'

export type AllyVerificationStatus = 'Pending' | 'Verified' | 'Rejected'

export interface AllyProfile {
  userId: string
  organizationName: string
  allyType: AllyType
  coverageLabel: string
  coverageLat: number
  coverageLng: number
  coverageRadiusMetres: number
  verificationStatus: AllyVerificationStatus
  appliedAt: string
  verifiedAt: string | null
}

export interface AllyAlertItem {
  notificationId: string
  title: string
  body: string
  relatedEntityId: string | null
  isRead: boolean
  createdAt: string
  actionConfirmedAt: string | null
  actionSummary: string | null
}

export interface SubmitAllyApplicationRequest {
  organizationName: string
  allyType: AllyType
  coverageLabel: string
  coverageLat: number
  coverageLng: number
  coverageRadiusMetres: number
}

export const alliesApi = {
  getMyProfile: () => apiClient.get<AllyProfile | null>('/allies/me').then((response) => response.data),

  submitApplication: (payload: SubmitAllyApplicationRequest) =>
    apiClient.post<AllyProfile>('/allies/me/application', payload).then((response) => response.data),

  getMyAlerts: () =>
    apiClient.get<AllyAlertItem[]>('/allies/me/alerts').then((response) => response.data),

  confirmAlertAction: (notificationId: string, actionSummary: string) =>
    apiClient.put(`/allies/me/alerts/${notificationId}/action`, { actionSummary }).then(() => undefined),
}
