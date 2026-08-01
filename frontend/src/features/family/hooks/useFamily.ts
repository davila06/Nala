import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { familyApi } from "../api/familyApi";

const FAMILY_KEY = ["family", "me"] as const;

export function useMyFamily() {
  return useQuery({
    queryKey: FAMILY_KEY,
    queryFn: familyApi.getMyFamily,
    staleTime: 30_000,
    retry: (count, err: { response?: { status?: number } }) =>
      err?.response?.status !== 403 && err?.response?.status !== 404 && count < 2,
  });
}

export function useCreateFamilyAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => familyApi.createAccount(name),
    onSuccess: () => void qc.invalidateQueries({ queryKey: FAMILY_KEY }),
  });
}

export function useInviteFamilyMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (email: string) => familyApi.invite(email),
    onSuccess: () => void qc.invalidateQueries({ queryKey: FAMILY_KEY }),
  });
}

export function useAcceptFamilyInvitation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (token: string) => familyApi.acceptInvitation(token),
    onSuccess: () => void qc.invalidateQueries({ queryKey: FAMILY_KEY }),
  });
}

export function useRemoveFamilyMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (memberId: string) => familyApi.removeMember(memberId),
    onSuccess: () => void qc.invalidateQueries({ queryKey: FAMILY_KEY }),
  });
}
