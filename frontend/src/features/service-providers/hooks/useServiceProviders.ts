import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  serviceProvidersApi,
  type ServiceProviderDirectoryFilter,
} from "../api/serviceProvidersApi";

export function usePublicServiceProviders(
  filter: ServiceProviderDirectoryFilter = {},
  enabled = true,
) {
  return useQuery({
    queryKey: ["service-providers", "public", filter],
    queryFn: () => serviceProvidersApi.getAll(filter),
    enabled,
    staleTime: 5 * 60_000,
  });
}

export function useServiceProviderDetail(id: string) {
  return useQuery({
    queryKey: ["service-providers", id],
    queryFn: () => serviceProvidersApi.getDetail(id),
    enabled: !!id,
    staleTime: 2 * 60_000,
  });
}

export function useRegisterServiceProvider() {
  return useMutation({ mutationFn: serviceProvidersApi.register });
}

export function useMyServiceProvider() {
  return useQuery({
    queryKey: ["service-providers", "mine"],
    queryFn: serviceProvidersApi.getMine,
    retry: false,
  });
}

export function useMyProviderServices() {
  return useQuery({
    queryKey: ["service-providers", "mine", "services"],
    queryFn: serviceProvidersApi.getServices,
    retry: false,
    staleTime: 60_000,
  });
}

export function useAddProviderService() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: serviceProvidersApi.addService,
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["service-providers", "mine", "services"],
      }),
  });
}

export function useUpdateServiceProviderProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: serviceProvidersApi.updateProfile,
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["service-providers", "mine"],
      }),
  });
}

export function usePublicProviderServices(providerId: string) {
  return useQuery({
    queryKey: ["service-providers", providerId, "services"],
    queryFn: () => serviceProvidersApi.getPublicServices(providerId),
    enabled: !!providerId,
    staleTime: 2 * 60_000,
  });
}

export function useProviderServiceAvailability(
  serviceId: string,
  date: string,
) {
  return useQuery({
    queryKey: ["service-providers", "availability", serviceId, date],
    queryFn: () => serviceProvidersApi.getAvailability(serviceId, date),
    enabled: !!serviceId && !!date,
  });
}

export function useCreateProviderBooking() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: serviceProvidersApi.createBooking,
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["provider-bookings"] }),
  });
}

export function useMyProviderBookings() {
  return useQuery({
    queryKey: ["provider-bookings", "mine"],
    queryFn: serviceProvidersApi.getMyBookings,
  });
}

export function useIncomingProviderBookings() {
  return useQuery({
    queryKey: ["provider-bookings", "incoming"],
    queryFn: serviceProvidersApi.getIncomingBookings,
  });
}

export function useUpdateProviderBookingStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      bookingId,
      status,
      reason,
    }: {
      bookingId: string;
      status: Parameters<typeof serviceProvidersApi.updateBookingStatus>[1];
      reason?: string;
    }) => serviceProvidersApi.updateBookingStatus(bookingId, status, reason),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["provider-bookings"] }),
  });
}

export function useRescheduleProviderBooking() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      bookingId,
      startsAt,
    }: {
      bookingId: string;
      startsAt: string;
    }) => serviceProvidersApi.rescheduleBooking(bookingId, startsAt),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["provider-bookings"] }),
  });
}

export function useProviderVerification() {
  return useQuery({
    queryKey: ["service-providers", "mine", "verification"],
    queryFn: serviceProvidersApi.getVerification,
    retry: false,
  });
}

export function useUploadProviderVerificationDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: serviceProvidersApi.uploadVerificationDocument,
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["service-providers", "mine", "verification"],
      }),
  });
}

export function useAddServiceAvailabilityRule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: serviceProvidersApi.addAvailabilityRule,
    onSuccess: (_, variables) =>
      void queryClient.invalidateQueries({
        queryKey: [
          "service-providers",
          "availability-rules",
          variables.serviceId,
        ],
      }),
  });
}

export function useServiceAvailabilityRules(serviceId: string) {
  return useQuery({
    queryKey: ["service-providers", "availability-rules", serviceId],
    queryFn: () => serviceProvidersApi.getAvailabilityRules(serviceId),
    enabled: !!serviceId,
  });
}

export function useDeactivateServiceAvailabilityRule(serviceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: serviceProvidersApi.deactivateAvailabilityRule,
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["service-providers", "availability-rules", serviceId],
      }),
  });
}

export function useSetProviderServiceStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      serviceId,
      status,
    }: {
      serviceId: string;
      status: "Published" | "Paused" | "Archived";
    }) => serviceProvidersApi.setServiceStatus(serviceId, status),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["service-providers", "mine", "services"],
      }),
  });
}

export function useUpdateProviderService() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      serviceId,
      ...data
    }: {
      serviceId: string;
      name: string;
      description: string;
      modality: import("../api/serviceProvidersApi").ServiceModality;
      durationMinutes: number;
      priceCrc: number;
      capacity: number;
    }) => serviceProvidersApi.updateService(serviceId, data),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["service-providers", "mine", "services"],
      }),
  });
}

export function useServiceAvailabilityBlocks(serviceId: string) {
  const from = new Date();
  const to = new Date(from);
  to.setDate(to.getDate() + 89);
  return useQuery({
    queryKey: ["service-providers", "availability-blocks", serviceId],
    queryFn: () =>
      serviceProvidersApi.getAvailabilityBlocks(
        serviceId,
        from.toISOString(),
        to.toISOString(),
      ),
    enabled: !!serviceId,
  });
}

export function useAddServiceAvailabilityBlock() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: serviceProvidersApi.addAvailabilityBlock,
    onSuccess: (_, variables) =>
      void queryClient.invalidateQueries({
        queryKey: [
          "service-providers",
          "availability-blocks",
          variables.serviceId,
        ],
      }),
  });
}

export function useDeactivateServiceAvailabilityBlock(serviceId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: serviceProvidersApi.deactivateAvailabilityBlock,
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["service-providers", "availability-blocks", serviceId],
      }),
  });
}
