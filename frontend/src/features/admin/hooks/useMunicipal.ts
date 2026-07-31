import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { municipalApi, type CapturedAnimalStatus } from "../api/municipalApi";

export function useCapturedAnimals(
  canton?: string,
  status?: CapturedAnimalStatus,
  page = 1,
) {
  return useQuery({
    queryKey: ["captures", canton, status, page],
    queryFn: () => municipalApi.search(canton, status, page),
    staleTime: 30_000,
  });
}

export function useRecordCapture() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: municipalApi.record,
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["captures"] }),
  });
}

export function useUpdateCaptureStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      status,
      matchedPetId,
    }: {
      id: string;
      status: CapturedAnimalStatus;
      matchedPetId?: string;
    }) => municipalApi.updateStatus(id, status, matchedPetId),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["captures"] }),
  });
}
