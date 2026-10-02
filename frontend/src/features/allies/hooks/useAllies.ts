import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { alliesApi, type SubmitAllyApplicationRequest } from "../api/alliesApi";

const ALLY_PROFILE_KEY = ["ally-profile"] as const;
const ALLY_ALERTS_KEY = ["ally-alerts"] as const;
const ALLY_WELFARE_CASES_KEY = ["ally-assigned-welfare-cases"] as const;

export function useMyAllyProfile() {
  return useQuery({
    queryKey: ALLY_PROFILE_KEY,
    queryFn: () => alliesApi.getMyProfile(),
  });
}

export function useSubmitAllyApplication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SubmitAllyApplicationRequest) => alliesApi.submitApplication(payload),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ALLY_PROFILE_KEY }),
  });
}

export function useMyAllyAlerts(enabled: boolean) {
  return useQuery({
    queryKey: ALLY_ALERTS_KEY,
    queryFn: () => alliesApi.getMyAlerts(),
    enabled,
  });
}

export function useMyAssignedWelfareCases(enabled: boolean) {
  return useQuery({
    queryKey: ALLY_WELFARE_CASES_KEY,
    queryFn: () => alliesApi.getAssignedWelfareCases(),
    enabled,
    staleTime: 20_000,
    refetchInterval: enabled ? 30_000 : false,
  });
}

export function useMyAssignedWelfareCase(caseId: string | null, enabled: boolean) {
  return useQuery({
    queryKey: [...ALLY_WELFARE_CASES_KEY, caseId],
    queryFn: () => alliesApi.getAssignedWelfareCase(caseId!),
    enabled: enabled && !!caseId,
    staleTime: 15_000,
  });
}

export function useDownloadAssignedWelfareEvidence() {
  return useMutation({
    mutationFn: ({ caseId, evidenceId }: { caseId: string; evidenceId: string }) =>
      alliesApi.downloadAssignedWelfareEvidence(caseId, evidenceId),
  });
}

export function useConfirmAllyAlertAction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ notificationId, actionSummary }: { notificationId: string; actionSummary: string }) =>
      alliesApi.confirmAlertAction(notificationId, actionSummary),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ALLY_ALERTS_KEY }),
  });
}
