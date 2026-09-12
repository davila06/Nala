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
      billingMonths,
      clinicId,
      requiresInvoice,
    }: {
      tier: SubscriptionTier;
      billingMonths: number;
      clinicId?: string;
      requiresInvoice?: boolean;
    }) => subscriptionApi.create(tier, billingMonths, clinicId, requiresInvoice),
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
    mutationFn: (paymentReference: string) => subscriptionApi.activate(paymentReference),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });
}

export function useCancelSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (subscriptionId: string) => subscriptionApi.cancel(subscriptionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });
}

export function useScheduleDowngrade() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ subscriptionId, targetTier }: { subscriptionId: string; targetTier: SubscriptionTier }) =>
      subscriptionApi.downgrade(subscriptionId, targetTier),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });
}

export function useReportPayment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: string | { subscriptionId: string; bankReceiptNumber?: string }) => {
      const subscriptionId = typeof args === "string" ? args : args.subscriptionId;
      const receipt = typeof args === "string" ? undefined : args.bankReceiptNumber;
      return subscriptionApi.reportPayment(subscriptionId, receipt);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });
}
