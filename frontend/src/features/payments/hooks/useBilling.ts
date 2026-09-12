import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { billingApi, type UpsertBillingProfileRequest } from "../api/billingApi";

export function useBillingProfile() {
  return useQuery({
    queryKey: ["billing-profile"],
    queryFn: billingApi.getProfile,
  });
}

export function useUpsertBillingProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpsertBillingProfileRequest) => billingApi.upsertProfile(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["billing-profile"] });
    },
  });
}

export function useUserInvoices() {
  return useQuery({
    queryKey: ["user-invoices"],
    queryFn: billingApi.getInvoices,
  });
}
