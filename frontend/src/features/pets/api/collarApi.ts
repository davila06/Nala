import { apiClient } from "@/shared/lib/apiClient";

export type CollarProvider = "Own" | "Tractive" | "Kippy" | "Generic";

export interface CollarDto {
  id: string;
  petId: string;
  provider: CollarProvider;
  externalDeviceId: string | null;
  batteryPercent: number | null;
  lastLat: number | null;
  lastLng: number | null;
  lastSeenAt: string | null;
  isActive: boolean;
}

export interface LocationPointDto {
  lat: number;
  lng: number;
  recordedAt: string;
}

export const collarApi = {
  getStatus: (petId: string) =>
    apiClient
      .get<CollarDto | null>(`/collars/pet/${petId}`)
      .then((r) => r.data),

  getHistory: (petId: string, hours = 24, maxPoints = 500) =>
    apiClient
      .get<LocationPointDto[]>(`/collars/pet/${petId}/history`, {
        params: { hours, maxPoints },
      })
      .then((r) => r.data),

  register: (
    petId: string,
    provider: CollarProvider,
    externalDeviceId?: string,
  ) =>
    apiClient
      .post<CollarDto>("/collars", {
        petId,
        provider,
        externalDeviceId: externalDeviceId ?? null,
      })
      .then((r) => r.data),
};
