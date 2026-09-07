import { apiClient } from "@/shared/lib/apiClient";

export type ServiceProviderCategory =
  | "Trainer"
  | "Groomer"
  | "Hotel"
  | "Daycare"
  | "Walker"
  | "Photographer"
  | "Other";

export interface PublicServiceProviderDto {
  id: string;
  name: string;
  description: string;
  category: ServiceProviderCategory;
  address: string;
  lat: number;
  lng: number;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  isFeatured: boolean;
  status: "Pending" | "Active" | "Rejected" | "Suspended";
  isVerified: boolean;
}

export type ServiceModality =
  | "AtProviderLocation"
  | "AtCustomerLocation"
  | "Virtual"
  | "Group"
  | "OvernightStay";

export interface ProviderServiceDto {
  id: string;
  serviceProviderId: string;
  name: string;
  description: string;
  modality: ServiceModality;
  durationMinutes: number;
  priceCrc: number;
  capacity: number;
  status: "Published" | "Paused" | "Archived";
}

export interface ServiceProviderDirectoryFilter {
  category?: ServiceProviderCategory;
  modality?: ServiceModality;
  minPriceCrc?: number;
  maxPriceCrc?: number;
}

export type ProviderBookingStatus =
  | "Requested"
  | "Confirmed"
  | "InProgress"
  | "Completed"
  | "CancelledByCustomer"
  | "CancelledByProvider"
  | "NoShow"
  | "Expired";

export interface ProviderServiceAvailabilitySlotDto {
  startsAt: string;
  endsAt: string;
  availableCapacity: number;
}

export interface ProviderBookingDto {
  id: string;
  serviceProviderId: string;
  providerServiceId: string;
  petId: string;
  serviceName: string;
  startsAt: string;
  endsAt: string;
  priceCrc: number;
  quantity: number;
  status: ProviderBookingStatus;
}

export interface ProviderVerificationDto {
  id: string;
  status: "Pending" | "Verified" | "Rejected" | "Expired";
  submittedAt: string;
  expiresAt: string | null;
  rejectionReason: string | null;
}

export interface ServiceAvailabilityRuleDto {
  id: string;
  dayOfWeek: number;
  startsAtLocalTime: string;
  endsAtLocalTime: string;
  isActive: boolean;
}

export interface ServiceAvailabilityBlockDto {
  id: string;
  startsAt: string;
  endsAt: string;
  reason: string;
  isActive: boolean;
}

export const SERVICE_PROVIDER_CATEGORY_LABELS: Record<
  ServiceProviderCategory,
  string
> = {
  Trainer: "Adiestramiento",
  Groomer: "Grooming",
  Hotel: "Hotel para mascotas",
  Daycare: "Guarderia",
  Walker: "Paseos",
  Photographer: "Fotografia",
  Other: "Otros servicios",
};

export const SERVICE_MODALITY_LABELS: Record<ServiceModality, string> = {
  AtProviderLocation: "En mi establecimiento",
  AtCustomerLocation: "A domicilio",
  Virtual: "Virtual",
  Group: "Grupal",
  OvernightStay: "Estadia nocturna",
};

export const serviceProvidersApi = {
  getAll: (
    filter: ServiceProviderDirectoryFilter = {},
  ): Promise<PublicServiceProviderDto[]> =>
    apiClient
      .get<PublicServiceProviderDto[]>("/public/service-providers", {
        params: { page: 1, pageSize: 50, ...filter },
      })
      .then((response) => response.data),

  getDetail: (id: string): Promise<PublicServiceProviderDto> =>
    apiClient
      .get<PublicServiceProviderDto>(`/public/service-providers/${id}`)
      .then((response) => response.data),

  getMine: (): Promise<PublicServiceProviderDto> =>
    apiClient
      .get<PublicServiceProviderDto>("/service-providers/mine")
      .then((response) => response.data),

  updateProfile: (data: {
    name: string;
    description: string;
    category: ServiceProviderCategory;
    address: string;
    lat: number;
    lng: number;
    phoneNumber?: string;
    website?: string;
  }): Promise<PublicServiceProviderDto> =>
    apiClient
      .put<PublicServiceProviderDto>("/service-providers/profile", data)
      .then((response) => response.data),

  getServices: (): Promise<ProviderServiceDto[]> =>
    apiClient
      .get<ProviderServiceDto[]>("/service-providers/services")
      .then((response) => response.data),

  getPublicServices: (providerId: string): Promise<ProviderServiceDto[]> =>
    apiClient
      .get<
        ProviderServiceDto[]
      >(`/public/service-providers/${providerId}/services`)
      .then((response) => response.data),

  getAvailability: (
    providerServiceId: string,
    date: string,
  ): Promise<ProviderServiceAvailabilitySlotDto[]> =>
    apiClient
      .get<
        ProviderServiceAvailabilitySlotDto[]
      >(`/provider-bookings/availability/${providerServiceId}`, { params: { date } })
      .then((response) => response.data),

  createBooking: (data: {
    providerServiceId: string;
    petId: string;
    startsAt: string;
    quantity: number;
    customerNote?: string;
  }): Promise<ProviderBookingDto> =>
    apiClient
      .post<ProviderBookingDto>("/provider-bookings", data)
      .then((response) => response.data),

  getMyBookings: (): Promise<ProviderBookingDto[]> =>
    apiClient
      .get<ProviderBookingDto[]>("/provider-bookings/mine")
      .then((response) => response.data),

  getIncomingBookings: (): Promise<ProviderBookingDto[]> =>
    apiClient
      .get<ProviderBookingDto[]>("/provider-bookings/incoming")
      .then((response) => response.data),

  updateBookingStatus: (
    bookingId: string,
    status: ProviderBookingStatus,
    reason?: string,
  ): Promise<ProviderBookingDto> =>
    apiClient
      .put<ProviderBookingDto>(`/provider-bookings/${bookingId}/status`, {
        status,
        reason,
      })
      .then((response) => response.data),

  rescheduleBooking: (
    bookingId: string,
    startsAt: string,
  ): Promise<ProviderBookingDto> =>
    apiClient
      .put<ProviderBookingDto>(`/provider-bookings/${bookingId}/reschedule`, {
        startsAt,
      })
      .then((response) => response.data),

  getVerification: (): Promise<ProviderVerificationDto | null> =>
    apiClient
      .get<ProviderVerificationDto | null>("/service-providers/verification")
      .then((response) => response.data),

  uploadVerificationDocument: (
    file: File,
  ): Promise<ProviderVerificationDto> => {
    const form = new FormData();
    form.append("file", file);
    return apiClient
      .post<ProviderVerificationDto>(
        "/service-providers/verification/document",
        form,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      )
      .then((response) => response.data);
  },

  downloadVerificationDocument: (): Promise<Blob> =>
    apiClient
      .get("/service-providers/verification/document", { responseType: "blob" })
      .then((response) => response.data),

  addAvailabilityRule: (data: {
    serviceId: string;
    dayOfWeek: number;
    startsAtLocalTime: string;
    endsAtLocalTime: string;
  }): Promise<{ id: string }> =>
    apiClient
      .post<{ id: string }>(
        `/service-providers/services/${data.serviceId}/availability`,
        {
          dayOfWeek: data.dayOfWeek,
          startsAtLocalTime: data.startsAtLocalTime,
          endsAtLocalTime: data.endsAtLocalTime,
        },
      )
      .then((response) => response.data),

  getAvailabilityRules: (
    serviceId: string,
  ): Promise<ServiceAvailabilityRuleDto[]> =>
    apiClient
      .get<
        ServiceAvailabilityRuleDto[]
      >(`/service-providers/services/${serviceId}/availability`)
      .then((response) => response.data),

  deactivateAvailabilityRule: (ruleId: string): Promise<void> =>
    apiClient
      .delete(`/service-providers/availability/${ruleId}`)
      .then(() => undefined),

  getAvailabilityBlocks: (
    serviceId: string,
    from: string,
    to: string,
  ): Promise<ServiceAvailabilityBlockDto[]> =>
    apiClient
      .get<
        ServiceAvailabilityBlockDto[]
      >(`/service-providers/services/${serviceId}/availability-blocks`, { params: { from, to } })
      .then((response) => response.data),

  addAvailabilityBlock: (data: {
    serviceId: string;
    startsAt: string;
    endsAt: string;
    reason: string;
  }): Promise<{ id: string }> =>
    apiClient
      .post<{
        id: string;
      }>(`/service-providers/services/${data.serviceId}/availability-blocks`, {
        startsAt: data.startsAt,
        endsAt: data.endsAt,
        reason: data.reason,
      })
      .then((response) => response.data),

  deactivateAvailabilityBlock: (blockId: string): Promise<void> =>
    apiClient
      .delete(`/service-providers/availability-blocks/${blockId}`)
      .then(() => undefined),

  addService: (data: {
    name: string;
    description: string;
    modality: ServiceModality;
    durationMinutes: number;
    priceCrc: number;
    capacity: number;
  }): Promise<ProviderServiceDto> =>
    apiClient
      .post<ProviderServiceDto>("/service-providers/services", data)
      .then((response) => response.data),

  setServiceStatus: (
    serviceId: string,
    status: ProviderServiceDto["status"],
  ): Promise<ProviderServiceDto> =>
    apiClient
      .put<ProviderServiceDto>(
        `/service-providers/services/${serviceId}/status`,
        {
          status,
        },
      )
      .then((response) => response.data),

  updateService: (
    serviceId: string,
    data: {
      name: string;
      description: string;
      modality: ServiceModality;
      durationMinutes: number;
      priceCrc: number;
      capacity: number;
    },
  ): Promise<ProviderServiceDto> =>
    apiClient
      .put<ProviderServiceDto>(`/service-providers/services/${serviceId}`, data)
      .then((response) => response.data),

  register: (data: {
    name: string;
    description: string;
    category: ServiceProviderCategory;
    address: string;
    lat: number;
    lng: number;
    contactEmail: string;
    password: string;
  }): Promise<PublicServiceProviderDto> =>
    apiClient
      .post<PublicServiceProviderDto>("/service-providers/register", data)
      .then((response) => response.data),
};
