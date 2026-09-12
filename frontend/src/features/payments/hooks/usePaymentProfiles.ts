import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { paymentApi, type ChargeCardRequest } from "../api/paymentApi";

export function usePaymentProfiles() {
  return useQuery({
    queryKey: ["payment-profiles"],
    queryFn: paymentApi.getProfiles,
  });
}

export function useCaptureContext() {
  return useQuery({
    queryKey: ["payment-capture-context"],
    queryFn: paymentApi.getCaptureContext,
    staleTime: 1000 * 60 * 15, // 15 mins
  });
}

export function useSavePaymentProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: paymentApi.saveProfile,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["payment-profiles"] });
    },
  });
}

export function useDeletePaymentProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (profileId: string) => paymentApi.deleteProfile(profileId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["payment-profiles"] });
    },
  });
}

export function useChargeCard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: ChargeCardRequest) => paymentApi.chargeCard(request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["subscription"] });
      void queryClient.invalidateQueries({ queryKey: ["bundle-orders"] });
      void queryClient.invalidateQueries({ queryKey: ["payment-profiles"] });
    },
  });
}
