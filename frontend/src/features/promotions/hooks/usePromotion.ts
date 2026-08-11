import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { promotionApi, type PromotionSpecRequest } from "../api/promotionApi";

export function useValidatePromotion() {
  return useMutation({
    mutationFn: (code: string) => promotionApi.validate(code),
  });
}

export function useRedeemPromotion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ code, selectedTier }: { code: string; selectedTier?: string }) =>
      promotionApi.redeem(code, selectedTier),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["subscription"] });
      void qc.invalidateQueries({ queryKey: ["my-tier"] });
    },
  });
}

// Admin hooks

export function useAdminPromotions() {
  return useQuery({
    queryKey: ["admin-promotions"],
    queryFn: () => promotionApi.getAll(),
    staleTime: 30_000,
  });
}

export function useCreatePromotionBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (specs: PromotionSpecRequest[]) => promotionApi.createBatch(specs),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["admin-promotions"] }),
  });
}

export function useTogglePromotion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, activate }: { id: string; activate: boolean }) =>
      promotionApi.toggle(id, activate),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["admin-promotions"] }),
  });
}
