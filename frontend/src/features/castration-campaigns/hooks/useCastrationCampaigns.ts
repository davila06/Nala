import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { castrationCampaignsApi } from "../api/castrationCampaignsApi";
import type { ReserveAppointmentRequest } from "../api/castrationCampaignsApi";

export function useCastrationCampaigns(canton: string, page: number) {
  return useQuery({
    queryKey: ["castration-campaigns", canton, page],
    queryFn: () => castrationCampaignsApi.getPublished(canton, page),
    staleTime: 60_000,
  });
}

export function useReserveCastrationAppointment(campaignId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: ReserveAppointmentRequest) => castrationCampaignsApi.reserve(campaignId, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["castration-campaigns"] });
      void queryClient.invalidateQueries({ queryKey: ["castration-appointments", "mine"] });
    },
  });
}

export function useCastrationAgenda(campaignId: string) {
  return useQuery({
    queryKey: ["castration-agenda", campaignId],
    queryFn: () => castrationCampaignsApi.getAgenda(campaignId),
    enabled: Boolean(campaignId),
  });
}

export function useOperateCastrationAppointment(campaignId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      appointmentId,
      operation,
      details,
    }: {
      appointmentId: string;
      operation: string;
      details?: { veterinarianId?: string; clinicalOutcome?: string; postOperativeInstructions?: string };
    }) => castrationCampaignsApi.operate(appointmentId, operation, details),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["castration-agenda", campaignId] }),
  });
}
