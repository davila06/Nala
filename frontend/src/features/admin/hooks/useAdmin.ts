import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { adminApi } from "../api/adminApi";

export function usePendingAllies() {
  return useQuery({
    queryKey: ["admin", "allies", "pending"],
    queryFn: adminApi.getPendingAllies,
  });
}

export function usePendingClinics() {
  return useQuery({
    queryKey: ["admin", "clinics", "pending"],
    queryFn: adminApi.getPendingClinics,
  });
}

export function useReviewAlly() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, approve }: { userId: string; approve: boolean }) =>
      adminApi.reviewAlly(userId, approve),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "allies", "pending"],
      });
    },
  });
}

export function useReviewClinic() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      clinicId,
      approve,
    }: {
      clinicId: string;
      approve: boolean;
    }) => adminApi.reviewClinic(clinicId, approve),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "clinics", "pending"],
      });
    },
  });
}

export function useAdminSubscriptions(pendingOnly = false) {
  return useQuery({
    queryKey: ["admin", "subscriptions", pendingOnly],
    queryFn: () => adminApi.getAdminSubscriptions(pendingOnly),
  });
}

export function useAdminActivateSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      billingMonths,
    }: {
      id: string;
      billingMonths?: number;
    }) => adminApi.adminActivateSubscription(id, billingMonths ?? 1),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "subscriptions"],
      });
    },
  });
}

export function useAdminCancelSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminApi.adminCancelSubscription(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "subscriptions"],
      });
    },
  });
}

// ── Adoptions admin hooks ──────────────────────────────────────────────────────

export function useAdoptionAdminStats() {
  return useQuery({
    queryKey: ["admin", "adoptions", "stats"],
    queryFn: adminApi.getAdoptionStats,
    staleTime: 60_000,
  });
}

export function useAdminAdoptionAnimals(status?: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["admin", "adoptions", "animals", status, page],
    queryFn: () => adminApi.getAdminAnimals(status, page, pageSize),
    staleTime: 30_000,
  });
}

export function useAdminModerateAnimal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, action }: { id: string; action: "remove" | "pause" | "restore" }) =>
      adminApi.moderateAnimal(id, action),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["admin", "adoptions"] });
      void queryClient.invalidateQueries({ queryKey: ["adoptions", "animals"] });
    },
  });
}
