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

export const collarApi = {
  getStatus: (petId: string) =>
    apiClient
      .get<CollarDto | null>(`/collars/pet/${petId}`)
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
