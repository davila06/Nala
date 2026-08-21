import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type PetSpecies = "Dog" | "Cat" | "Bird" | "Rabbit" | "Other";
export type PetSize = "XSmall" | "Small" | "Medium" | "Large" | "XLarge";
export type AgeCategory = "Puppy" | "Young" | "Adult" | "Senior";
export type AdoptionStatus =
  | "Available"
  | "InProcess"
  | "Adopted"
  | "Paused"
  | "Removed";
export type ApplicationStatus =
  | "Pending"
  | "UnderReview"
  | "Approved"
  | "Rejected"
  | "Withdrawn";
export type FairStatus = "Upcoming" | "Active" | "Finished" | "Cancelled";

export interface AdoptablePetDto {
  id: string;
  organizationUserId: string;
  organizationName: string;
  name: string;
  species: PetSpecies;
  breed: string | null;
  size: PetSize;
  ageCategory: AgeCategory;
  ageMonthsApprox: number | null;
  story: string;
  requirements: string | null;
  medicalNotes: string | null;
  isVaccinated: boolean;
  isSterilized: boolean;
  isMicrochipped: boolean;
  okWithKids: boolean;
  okWithDogs: boolean;
  okWithCats: boolean;
  needsYard: boolean;
  refLat: number;
  refLng: number;
  refLabel: string | null;
  status: AdoptionStatus;
  photoUrls: string[];
  publishedAt: string;
}

export interface AdoptionApplicationDto {
  id: string;
  adoptablePetId: string;
  applicantUserId: string;
  applicantNote: string;
  status: ApplicationStatus;
  reviewNote: string | null;
  appliedAt: string;
  reviewedAt: string | null;
}

export interface AdoptionFairDto {
  id: string;
  organizationUserId: string;
  title: string;
  description: string | null;
  venueLabel: string;
  lat: number;
  lng: number;
  startsAt: string;
  endsAt: string;
  status: FairStatus;
  animalIds: string[];
}

export interface AdoptionFilters {
  species?: PetSpecies;
  size?: PetSize;
  ageCategory?: AgeCategory;
  isVaccinated?: boolean;
  isSterilized?: boolean;
  okWithKids?: boolean;
  okWithDogs?: boolean;
  lat?: number;
  lng?: number;
  radiusKm?: number;
  page?: number;
  pageSize?: number;
}

export interface PagedAdoptions {
  items: AdoptablePetDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PagedApplications {
  items: AdoptionApplicationDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export type PublishAnimalPayload = Pick<
  AdoptablePetDto,
  | "name"
  | "species"
  | "size"
  | "ageCategory"
  | "ageMonthsApprox"
  | "story"
  | "requirements"
  | "medicalNotes"
  | "breed"
  | "isVaccinated"
  | "isSterilized"
  | "isMicrochipped"
  | "okWithKids"
  | "okWithDogs"
  | "okWithCats"
  | "needsYard"
  | "refLat"
  | "refLng"
  | "refLabel"
>;

export type UpdateAnimalPayload = Pick<
  AdoptablePetDto,
  | "name"
  | "story"
  | "requirements"
  | "medicalNotes"
  | "isVaccinated"
  | "isSterilized"
  | "isMicrochipped"
  | "okWithKids"
  | "okWithDogs"
  | "okWithCats"
  | "needsYard"
>;

export const SPECIES_LABELS: Record<PetSpecies, string> = {
  Dog: "Perro",
  Cat: "Gato",
  Bird: "Pájaro",
  Rabbit: "Conejo",
  Other: "Otro",
};

export const SIZE_LABELS: Record<PetSize, string> = {
  XSmall: "Muy pequeño",
  Small: "Pequeño",
  Medium: "Mediano",
  Large: "Grande",
  XLarge: "Muy grande",
};

export const AGE_LABELS: Record<AgeCategory, string> = {
  Puppy: "Cachorro (<1 año)",
  Young: "Joven (1–3 años)",
  Adult: "Adulto (3–8 años)",
  Senior: "Senior (8+ años)",
};

// ── API client ────────────────────────────────────────────────────────────────

export const adoptionsApi = {
  getAnimals: (filters: AdoptionFilters = {}) =>
    apiClient
      .get<PagedAdoptions>("/adoptions/animals", { params: filters })
      .then((r) => r.data),

  getAnimalsForMap: () =>
    apiClient
      .get<AdoptablePetDto[]>("/adoptions/animals/map")
      .then((r) => r.data),

  getAnimal: (id: string) =>
    apiClient
      .get<AdoptablePetDto>(`/adoptions/animals/${id}`)
      .then((r) => r.data),

  publishAnimal: (data: PublishAnimalPayload) =>
    apiClient
      .post<AdoptablePetDto>("/adoptions/animals", data)
      .then((r) => r.data),

  updateAnimal: (id: string, data: UpdateAnimalPayload) =>
    apiClient
      .patch<AdoptablePetDto>(`/adoptions/animals/${id}`, data)
      .then((r) => r.data),

  uploadPhoto: (animalId: string, file: File) => {
    const form = new FormData();
    form.append("photo", file);
    return apiClient
      .post<{ photoUrl: string }>(
        `/adoptions/animals/${animalId}/photos`,
        form,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      )
      .then((r) => r.data);
  },

  deletePhoto: (animalId: string, photoUrl: string) =>
    apiClient.delete(`/adoptions/animals/${animalId}/photos`, {
      data: { photoUrl },
    }),

  getMyAnimals: (page = 1, pageSize = 20) =>
    apiClient
      .get<PagedAdoptions>("/adoptions/animals/mine", {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  applyToAdopt: (animalId: string, note: string) =>
    apiClient
      .post<AdoptionApplicationDto>(`/adoptions/animals/${animalId}/apply`, {
        note,
      })
      .then((r) => r.data),

  getApplicationsForAnimal: (animalId: string) =>
    apiClient
      .get<
        AdoptionApplicationDto[]
      >(`/adoptions/animals/${animalId}/applications`)
      .then((r) => r.data),

  reviewApplication: (
    applicationId: string,
    approve: boolean,
    reviewNote?: string,
  ) =>
    apiClient
      .patch<AdoptionApplicationDto>(
        `/adoptions/applications/${applicationId}/review`,
        {
          approve,
          reviewNote,
        },
      )
      .then((r) => r.data),

  withdrawApplication: (applicationId: string) =>
    apiClient.delete(`/adoptions/applications/${applicationId}`),

  markAdopted: (animalId: string) =>
    apiClient
      .patch<AdoptablePetDto>(`/adoptions/animals/${animalId}/mark-adopted`)
      .then((r) => r.data),

  getMyApplications: (page = 1, pageSize = 20) =>
    apiClient
      .get<PagedApplications>("/adoptions/applications/mine", {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getFairs: (lat?: number, lng?: number, radiusKm?: number) =>
    apiClient
      .get<
        AdoptionFairDto[]
      >("/adoptions/fairs", { params: { lat, lng, radiusKm } })
      .then((r) => r.data),

  createFair: (
    data: Omit<AdoptionFairDto, "id" | "organizationUserId" | "status">,
  ) =>
    apiClient
      .post<AdoptionFairDto>("/adoptions/fairs", data)
      .then((r) => r.data),
};
