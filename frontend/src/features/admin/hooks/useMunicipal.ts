import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { municipalApi, type CapturedAnimalStatus } from "../api/municipalApi";

const PROFILE_KEY = ["municipal-profile"] as const;
const CAPTURES_KEY = (canton?: string, status?: CapturedAnimalStatus, page = 1) =>
  ["captures", canton, status, page] as const;

export function useMunicipalProfile() {
  return useQuery({
    queryKey: PROFILE_KEY,
    queryFn: municipalApi.getProfile,
    staleTime: 60_000,
  });
}

export function useCapturedAnimals(
  canton?: string,
  status?: CapturedAnimalStatus,
  page = 1,
) {
  return useQuery({
    queryKey: CAPTURES_KEY(canton, status, page),
    queryFn: () => municipalApi.search(canton, status, page),
    staleTime: 30_000,
  });
}

export function useRecordCapture() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: municipalApi.record,
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["captures"] }),
  });
}

export function useUpdateCaptureStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status, matchedPetId }: { id: string; status: CapturedAnimalStatus; matchedPetId?: string }) =>
      municipalApi.updateStatus(id, status, matchedPetId),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["captures"] }),
  });
}

export function useBulkUpdateStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      animalIds,
      newStatus,
      matchedPetId,
    }: {
      animalIds: string[];
      newStatus: CapturedAnimalStatus;
      matchedPetId?: string;
    }) => municipalApi.bulkUpdateStatus(animalIds, newStatus, matchedPetId),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["captures"] }),
  });
}

export function useUploadCapturePhoto(captureId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => municipalApi.uploadPhoto(captureId, file),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["captures"] }),
  });
}

export function useCantonStats(canton?: string) {
  return useQuery({
    queryKey: ["municipal-stats", canton],
    queryFn: () => municipalApi.getStats(canton),
    staleTime: 120_000,
  });
}

export function useRegionalDashboard() {
  return useQuery({
    queryKey: ["municipal-regional"],
    queryFn: municipalApi.getRegionalDashboard,
    staleTime: 120_000,
  });
}

export function useTransferCapture() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      destinationCanton,
      notes,
    }: {
      id: string;
      destinationCanton: string;
      notes?: string;
    }) => municipalApi.transfer(id, destinationCanton, notes),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["captures"] }),
  });
}

