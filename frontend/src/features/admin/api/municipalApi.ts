import { apiClient } from '@/shared/lib/apiClient'

export type CapturedAnimalStatus =
  | 'Received' | 'OwnerFound' | 'Transferred' | 'Released' | 'Adopted'

export interface CapturedAnimalDto {
  id: string
  canton: string
  species: string
  breed: string | null
  color: string
  estimatedAge: string | null
  photoUrl: string | null
  notes: string | null
  collarChipNumber: string | null
  matchedPetId: string | null
  status: CapturedAnimalStatus
  capturedAt: string
}

export interface CapturedAnimalPageDto {
  items: CapturedAnimalDto[]
  total: number
  page: number
  pageSize: number
}

export const STATUS_LABELS: Record<CapturedAnimalStatus, string> = {
  Received:   'Recibido',
  OwnerFound: 'Dueño localizado',
  Transferred:'Transferido',
  Released:   'Liberado',
  Adopted:    'Adoptado',
}

export const municipalApi = {
  search: (canton?: string, status?: CapturedAnimalStatus, page = 1) =>
    apiClient
      .get<CapturedAnimalPageDto>('/municipalities/captures', { params: { canton, status, page, pageSize: 20 } })
      .then((r) => r.data),

  record: (data: {
    canton: string; species: string; color: string;
    breed?: string; estimatedAge?: string; notes?: string;
    collarChipNumber?: string; capturedAt?: string;
  }) =>
    apiClient.post<CapturedAnimalDto>('/municipalities/captures', data).then((r) => r.data),

  updateStatus: (id: string, status: CapturedAnimalStatus, matchedPetId?: string) =>
    apiClient
      .put<CapturedAnimalDto>(`/municipalities/captures/${id}/status`, { status, matchedPetId })
      .then((r) => r.data),
}
