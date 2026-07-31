import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { collarApi, type CollarProvider } from "../api/collarApi";

export function useCollarStatus(petId: string) {
  return useQuery({
    queryKey: ["collar", petId],
    queryFn: () => collarApi.getStatus(petId),
    enabled: !!petId,
    refetchInterval: 30_000, // poll every 30 s when tab is open
  });
}

export function useRegisterCollar() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      petId,
      provider,
      externalDeviceId,
    }: {
      petId: string;
      provider: CollarProvider;
      externalDeviceId?: string;
    }) => collarApi.register(petId, provider, externalDeviceId),
    onSuccess: (_data, { petId }) => {
      void queryClient.invalidateQueries({ queryKey: ["collar", petId] });
    },
  });
}
