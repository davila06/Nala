import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { subscriptionApi, type SubscriptionTier } from "../api/subscriptionApi";

export function useMySubscription(clinicId?: string) {
  return useQuery({
    queryKey: ["subscription", "me", clinicId ?? "user"],
    queryFn: () => subscriptionApi.getMine(clinicId),
    staleTime: 60_000,
  });
}

export function useSubscriptionCatalog() {
  return useQuery({
    queryKey: ["subscription", "catalog"],
    queryFn: subscriptionApi.getCatalog,
    staleTime: 5 * 60_000,
  });
}

export function useCreateSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      tier,
      clinicId,
    }: {
      tier: SubscriptionTier;
      clinicId?: string;
    }) => subscriptionApi.create(tier, clinicId),
    onSuccess: (_data, { clinicId }) => {
      void queryClient.invalidateQueries({
        queryKey: ["subscription", "me", clinicId ?? "user"],
      });
    },
  });
}

export function useActivateSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (paymentReference: string) =>
      subscriptionApi.activate(paymentReference),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });
}

export function useCancelSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (subscriptionId: string) =>
      subscriptionApi.cancel(subscriptionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });
}

export function useReportPayment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (subscriptionId: string) =>
      subscriptionApi.reportPayment(subscriptionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });
}
