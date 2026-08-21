import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  adoptionsApi,
  type AdoptionFilters,
  type PublishAnimalPayload,
  type UpdateAnimalPayload,
} from "../api/adoptionsApi";

export function useAdoptableAnimals(filters: AdoptionFilters = {}) {
  return useQuery({
    queryKey: ["adoptions", "animals", filters],
    queryFn: () => adoptionsApi.getAnimals(filters),
    staleTime: 3 * 60_000,
  });
}

export function useAdoptableAnimalsForMap(enabled = true) {
  return useQuery({
    queryKey: ["adoptions", "animals", "map"],
    queryFn: adoptionsApi.getAnimalsForMap,
    staleTime: 5 * 60_000,
    enabled,
  });
}

export function useAdoptableAnimal(id: string, enabled = true) {
  return useQuery({
    queryKey: ["adoptions", "animals", id],
    queryFn: () => adoptionsApi.getAnimal(id),
    enabled: !!id && enabled,
    staleTime: 2 * 60_000,
  });
}

export function useMyAdoptionAnimals(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["adoptions", "mine", page, pageSize],
    queryFn: () => adoptionsApi.getMyAnimals(page, pageSize),
    staleTime: 60_000,
  });
}

export function usePublishAnimal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: PublishAnimalPayload) =>
      adoptionsApi.publishAnimal(data),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["adoptions", "mine"] }),
  });
}

export function useUpdateAnimal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: { id: string } & UpdateAnimalPayload) =>
      adoptionsApi.updateAnimal(id, data),
    onSuccess: (_data, vars) => {
      void qc.invalidateQueries({
        queryKey: ["adoptions", "animals", vars.id],
      });
      void qc.invalidateQueries({ queryKey: ["adoptions", "mine"] });
    },
  });
}

export function useUploadAdoptionPhoto() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ animalId, file }: { animalId: string; file: File }) =>
      adoptionsApi.uploadPhoto(animalId, file),
    onSuccess: (_data, vars) =>
      void qc.invalidateQueries({
        queryKey: ["adoptions", "animals", vars.animalId],
      }),
  });
}

export function useDeleteAdoptionPhoto() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      animalId,
      photoUrl,
    }: {
      animalId: string;
      photoUrl: string;
    }) => adoptionsApi.deletePhoto(animalId, photoUrl),
    onSuccess: (_data, vars) =>
      void qc.invalidateQueries({
        queryKey: ["adoptions", "animals", vars.animalId],
      }),
  });
}

export function useApplyToAdopt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ animalId, note }: { animalId: string; note: string }) =>
      adoptionsApi.applyToAdopt(animalId, note),
    onSuccess: () =>
      void qc.invalidateQueries({
        queryKey: ["adoptions", "applications", "mine"],
      }),
  });
}

export function useApplicationsForAnimal(animalId: string, enabled = true) {
  return useQuery({
    queryKey: ["adoptions", "applications", animalId],
    queryFn: () => adoptionsApi.getApplicationsForAnimal(animalId),
    enabled: !!animalId && enabled,
    staleTime: 30_000,
  });
}

export function useReviewApplication() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      applicationId,
      approve,
      reviewNote,
    }: {
      applicationId: string;
      approve: boolean;
      reviewNote?: string;
    }) => adoptionsApi.reviewApplication(applicationId, approve, reviewNote),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["adoptions", "applications"] }),
  });
}

export function useWithdrawApplication() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (applicationId: string) =>
      adoptionsApi.withdrawApplication(applicationId),
    onSuccess: () =>
      void qc.invalidateQueries({
        queryKey: ["adoptions", "applications", "mine"],
      }),
  });
}

export function useMarkAdopted() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (animalId: string) => adoptionsApi.markAdopted(animalId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["adoptions", "mine"] });
      void qc.invalidateQueries({ queryKey: ["adoptions", "animals"] });
    },
  });
}

export function useMyAdoptionApplications(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["adoptions", "applications", "mine", page],
    queryFn: () => adoptionsApi.getMyApplications(page, pageSize),
    staleTime: 60_000,
  });
}

export function useUpcomingFairs(
  lat?: number,
  lng?: number,
  radiusKm?: number,
) {
  return useQuery({
    queryKey: ["adoptions", "fairs", lat, lng, radiusKm],
    queryFn: () => adoptionsApi.getFairs(lat, lng, radiusKm),
    staleTime: 5 * 60_000,
  });
}

export function useCreateFair() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: adoptionsApi.createFair,
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["adoptions", "fairs"] }),
  });
}
