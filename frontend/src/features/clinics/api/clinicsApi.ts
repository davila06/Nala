import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────

export type ClinicStatus = 'Pending' | 'Active' | 'Suspended'
export type ScanInputType = 'Qr' | 'RfidChip'

export interface ClinicDto {
  id: string
  name: string
  licenseNumber: string
  address: string
  lat: number
  lng: number
  contactEmail: string
  status: ClinicStatus
  registeredAt: string
}

export interface ClinicScanResultDto {
  scanId: string
  matched: boolean
  petName: string | null
  petPhotoUrl: string | null
  ownerName: string | null
  ownerEmail: string | null
  petSpecies: string | null
}

export interface RegisterClinicRequest {
  name: string
  licenseNumber: string
  address: string
  lat: number
  lng: number
  contactEmail: string
  password: string
}

// ── API client methods ─────────────────────────────────────────────────────────

export const clinicsApi = {
  register: (payload: RegisterClinicRequest): Promise<ClinicDto> =>
    apiClient.post<ClinicDto>('/clinics/register', payload).then((r) => r.data),

  getMyClinic: (): Promise<ClinicDto> =>
    apiClient.get<ClinicDto>('/clinics/me').then((r) => r.data),

  scan: (input: string, inputType: ScanInputType): Promise<ClinicScanResultDto> =>
    apiClient
      .post<ClinicScanResultDto>('/clinics/scan', { input, inputType })
      .then((r) => r.data),
}
