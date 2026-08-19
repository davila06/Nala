import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { billboardsApi, type BillboardPlacement } from "../api/billboardsApi";

const key = (placement: BillboardPlacement) =>
  ["billboards", placement] as const;
const adminKey = () => ["billboards", "admin"] as const;

export function useBillboards(placement: BillboardPlacement, enabled = true) {
  return useQuery({
    queryKey: key(placement),
    queryFn: () => billboardsApi.getActive(placement),
    staleTime: 5 * 60_000,
    gcTime: 10 * 60_000,
    enabled,
  });
}

export function useAdminBillboards(page = 1) {
  return useQuery({
    queryKey: [...adminKey(), page],
    queryFn: () => billboardsApi.getAll(page),
    staleTime: 60_000,
  });
}

export function useCreateBillboard() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: billboardsApi.create,
    onSuccess: () => void qc.invalidateQueries({ queryKey: adminKey() }),
  });
}

export function useUpdateBillboard() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: Parameters<typeof billboardsApi.update>[1];
    }) => billboardsApi.update(id, data),
    onSuccess: () => void qc.invalidateQueries({ queryKey: adminKey() }),
  });
}

export function useSetBillboardStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      status,
    }: {
      id: string;
      status: "active" | "paused" | "expired";
    }) => billboardsApi.setStatus(id, status),
    onSuccess: () => void qc.invalidateQueries({ queryKey: adminKey() }),
  });
}

export function useUploadBillboardImage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) =>
      billboardsApi.uploadImage(id, file),
    onSuccess: () => void qc.invalidateQueries({ queryKey: adminKey() }),
  });
}
