import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { bundleApi, type BundleOrderStatus, type CreateBundleOrderRequest } from "../api/bundleApi";

const MY_ORDERS_KEY = ["bundle-orders", "mine"] as const;
const ADMIN_KEY = (status?: BundleOrderStatus, page = 1) =>
  ["bundle-orders", "admin", status, page] as const;

export function useMyBundleOrders() {
  return useQuery({
    queryKey: MY_ORDERS_KEY,
    queryFn: bundleApi.getMine,
    staleTime: 30_000,
  });
}

export function useCreateBundleOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateBundleOrderRequest) => bundleApi.create(data),
    onSuccess: () => void qc.invalidateQueries({ queryKey: MY_ORDERS_KEY }),
  });
}

export function useReportBundlePayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => bundleApi.reportPayment(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: MY_ORDERS_KEY }),
  });
}

export function useCancelBundleOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => bundleApi.cancel(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: MY_ORDERS_KEY }),
  });
}

// ── Admin hooks ───────────────────────────────────────────────────────────────

export function useAdminBundleOrders(status?: BundleOrderStatus, page = 1) {
  return useQuery({
    queryKey: ADMIN_KEY(status, page),
    queryFn: () => bundleApi.getAll(status, page),
    staleTime: 20_000,
  });
}

export function useAdminConfirmBundlePayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => bundleApi.adminConfirmPayment(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["bundle-orders"] }),
  });
}

export function useAdminMarkBundleSourced() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, adminNotes }: { id: string; adminNotes?: string }) =>
      bundleApi.adminMarkSourced(id, adminNotes),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["bundle-orders"] }),
  });
}

export function useAdminMarkBundleShipped() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, trackingNumber, adminNotes }: { id: string; trackingNumber: string; adminNotes?: string }) =>
      bundleApi.adminMarkShipped(id, trackingNumber, adminNotes),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["bundle-orders"] }),
  });
}

export function useAdminMarkBundleDelivered() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => bundleApi.adminMarkDelivered(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["bundle-orders"] }),
  });
}

export function useAdminCancelBundleOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, adminNotes }: { id: string; adminNotes?: string }) =>
      bundleApi.adminCancel(id, adminNotes),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["bundle-orders"] }),
  });
}
