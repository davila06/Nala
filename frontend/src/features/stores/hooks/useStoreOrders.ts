import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { storeOrdersApi } from "../api/storeOrdersApi";
import type { PlaceOrderPayload } from "../api/storeOrdersApi";
import type { StoreOrderStatus } from "../api/storesApi";

export function useMyOrders(page = 1) {
  return useQuery({
    queryKey: ["my-store-orders", page],
    queryFn: () => storeOrdersApi.getMine(page, 20),
    staleTime: 30_000,
    refetchInterval: 30_000,
  });
}

export function useIncomingOrders() {
  return useQuery({
    queryKey: ["store-incoming-orders"],
    queryFn: () => storeOrdersApi.getIncoming(),
    staleTime: 15_000,
    refetchInterval: 15_000,
  });
}

export function usePlaceOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: PlaceOrderPayload) => storeOrdersApi.place(payload),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-orders"] }),
  });
}

export function useReportPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (orderId: string) => storeOrdersApi.reportPayment(orderId),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["my-store-orders"] }),
  });
}

export function useConfirmOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ orderId, note }: { orderId: string; note?: string }) =>
      storeOrdersApi.confirm(orderId, note),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["store-incoming-orders"] }),
  });
}

export function useUpdateOrderStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      orderId,
      status,
      note,
    }: {
      orderId: string;
      status: StoreOrderStatus;
      note?: string;
    }) => storeOrdersApi.updateStatus(orderId, status, note),
    onSuccess: () =>
      void qc.invalidateQueries({ queryKey: ["store-incoming-orders"] }),
  });
}
