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

export function useAdminClinicVerifications() {
  return useQuery({
    queryKey: ["admin", "clinic-verifications"],
    queryFn: () => adminApi.getClinicVerifications(),
    staleTime: 30_000,
  });
}

export function useReviewClinicVerification() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      verificationId,
      payload,
    }: {
      verificationId: string;
      payload: {
        approve: boolean;
        expiresAt?: string;
        reason?: string;
        notes?: string;
      };
    }) => adminApi.reviewClinicVerification(verificationId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "clinic-verifications"],
      });
    },
  });
}

export function useAdminClinicVeterinariansForReview() {
  return useQuery({
    queryKey: ["admin", "clinic-veterinarians-review"],
    queryFn: () => adminApi.getClinicVeterinariansForReview(),
    staleTime: 30_000,
  });
}

export function useReviewClinicVeterinarian() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      veterinarianId,
      payload,
    }: {
      veterinarianId: string;
      payload: {
        approve: boolean;
        expiresAt?: string;
        reason?: string;
        notes?: string;
      };
    }) => adminApi.reviewClinicVeterinarian(veterinarianId, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "clinic-veterinarians-review"],
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

export function useSubscriptionPlans() {
  return useQuery({
    queryKey: ["admin", "subscription-plans"],
    queryFn: () => adminApi.getSubscriptionPlans(true),
  });
}

export function useCreateSubscriptionPlan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.createSubscriptionPlan,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "subscription-plans"],
      });
    },
  });
}

export function useUpdateSubscriptionPlan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: Parameters<typeof adminApi.updateSubscriptionPlan>[0] extends never
      ? never
      : {
          id: string;
          payload: Parameters<typeof adminApi.updateSubscriptionPlan>[1];
        }) => adminApi.updateSubscriptionPlan(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "subscription-plans"],
      });
    },
  });
}

export function useDeleteSubscriptionPlan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, version }: { id: string; version: string }) =>
      adminApi.deleteSubscriptionPlan(id, version),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["admin", "subscription-plans"],
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

export function useAdminAdoptionAnimals(
  status?: string,
  page = 1,
  pageSize = 20,
) {
  return useQuery({
    queryKey: ["admin", "adoptions", "animals", status, page],
    queryFn: () => adminApi.getAdminAnimals(status, page, pageSize),
    staleTime: 30_000,
  });
}

export function useAdminModerateAnimal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      action,
    }: {
      id: string;
      action: "remove" | "pause" | "restore";
    }) => adminApi.moderateAnimal(id, action),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["admin", "adoptions"] });
      void queryClient.invalidateQueries({
        queryKey: ["adoptions", "animals"],
      });
    },
  });
}
