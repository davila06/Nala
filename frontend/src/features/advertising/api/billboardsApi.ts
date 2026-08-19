import { apiClient } from "@/shared/lib/apiClient";

export type BillboardPlacement = "Map" | "Dashboard" | "Directory" | "Feed";
export type BillboardStatus = "Draft" | "Active" | "Paused" | "Expired";

export interface BillboardDto {
  id: string;
  title: string;
  body: string | null;
  imageUrl: string | null;
  ctaLabel: string | null;
  ctaUrl: string | null;
  placement: BillboardPlacement;
  status: BillboardStatus;
  startsAt: string;
  endsAt: string;
  priority: number;
  createdAt: string;
}

export const billboardsApi = {
  getActive: (placement: BillboardPlacement): Promise<BillboardDto[]> =>
    apiClient
      .get<BillboardDto[]>("/billboards", { params: { placement } })
      .then((r) => r.data),

  getAll: (page = 1, pageSize = 20) =>
    apiClient
      .get<{
        items: BillboardDto[];
        totalCount: number;
        pageNumber: number;
        pageSize: number;
        totalPages: number;
        hasNextPage: boolean;
      }>("/billboards/admin", {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  create: (data: {
    title: string;
    body?: string;
    placement: BillboardPlacement;
    startsAt: string;
    endsAt: string;
    ctaLabel?: string;
    ctaUrl?: string;
    priority?: number;
  }) => apiClient.post<BillboardDto>("/billboards", data).then((r) => r.data),

  update: (
    id: string,
    data: {
      title: string;
      body?: string;
      ctaLabel?: string;
      ctaUrl?: string;
      startsAt: string;
      endsAt: string;
      priority: number;
    },
  ) =>
    apiClient.put<BillboardDto>(`/billboards/${id}`, data).then((r) => r.data),

  setStatus: (id: string, status: "active" | "paused" | "expired") =>
    apiClient
      .patch<BillboardDto>(`/billboards/${id}/status`, { status })
      .then((r) => r.data),

  uploadImage: (id: string, file: File) => {
    const form = new FormData();
    form.append("image", file);
    return apiClient
      .post<BillboardDto>(`/billboards/${id}/image`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },
};
