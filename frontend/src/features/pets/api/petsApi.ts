import { apiClient } from '@/shared/lib/apiClient'

// ── Types ─────────────────────────────────────────────────────────────────────
export type PetSpecies = 'Dog' | 'Cat' | 'Bird' | 'Rabbit' | 'Other'
export type PetStatus = 'Active' | 'Lost' | 'Reunited'

export interface PetSummary {
  id: string
  name: string
  species: PetSpecies
  breed: string | null
  photoUrl: string | null
  status: PetStatus
}

export interface PetDetail {
  id: string
  ownerId: string
  name: string
  species: PetSpecies
  breed: string | null
  birthDate: string | null
  photoUrl: string | null
  status: PetStatus
  createdAt: string
  updatedAt: string
}

export interface PublicPetProfile {
  id: string
  name: string
  species: PetSpecies
  breed: string | null
  birthDate: string | null
  photoUrl: string | null
  status: PetStatus
  ownerId: string
  activeLostEventId: string | null
  contactName: string | null
  publicMessage: string | null
}

export interface PetScanEvent {
  scannedAt: string
  cityName: string | null
  countryCode: string | null
  deviceSummary: string
}

export interface PetScanHistory {
  scansToday: number
  events: PetScanEvent[]
}

export interface CreatePetRequest {
  name: string
  species: PetSpecies
  breed?: string
  birthDate?: string
  photo?: File
}

export interface UpdatePetRequest extends CreatePetRequest {}

export interface CreatePetResponse {
  petId: string
}

// ── API client ────────────────────────────────────────────────────────────────
export const petsApi = {
  getMyPets: () =>
    apiClient.get<PetSummary[]>('/pets').then((r) => r.data),

  getPetDetail: (id: string) =>
    apiClient.get<PetDetail>(`/pets/${id}`).then((r) => r.data),

  getPublicProfile: (id: string) =>
    apiClient.get<PublicPetProfile>(`/public/pets/${id}`).then((r) => r.data),

  createPet: (data: CreatePetRequest) => {
    const form = buildFormData(data)
    return apiClient
      .post<CreatePetResponse>('/pets', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: data.photo
          ? (e) => {
              // Progress is consumed by caller via mutation context
              void e
            }
          : undefined,
      })
      .then((r) => r.data)
  },

  updatePet: (id: string, data: UpdatePetRequest) => {
    const form = buildFormData(data)
    return apiClient
      .put<{ petId: string }>(`/pets/${id}`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data)
  },

  deletePet: (id: string) => apiClient.delete(`/pets/${id}`),

  getQrCode: (id: string) =>
    apiClient
      .get(`/pets/${id}/qr`, { responseType: 'blob' })
      .then((r) => r.data as Blob),

  getWhatsAppAvatar: (id: string) =>
    apiClient
      .get(`/pets/${id}/whatsapp-avatar`, { responseType: 'blob' })
      .then((r) => r.data as Blob),

  getScanHistory: (id: string) =>
    apiClient
      .get<PetScanHistory>(`/pets/${id}/scan-history`)
      .then((r) => r.data),
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function buildFormData(data: CreatePetRequest): FormData {
  const form = new FormData()
  form.append('Name', data.name)
  form.append('Species', data.species)
  if (data.breed) form.append('Breed', data.breed)
  if (data.birthDate) form.append('BirthDate', data.birthDate)
  if (data.photo) form.append('Photo', data.photo)
  return form
}
