import { apiClient } from "@/shared/lib/apiClient";
import type { StoreOrderDto, StoreOrderStatus } from "./storesApi";

export interface PlaceOrderPayload {
  storeId: string;
  fulfillmentType: "Pickup" | "Delivery";
  deliveryAddress?: string;
  customerNote?: string;
  lines: { productId: string; quantity: number }[];
}

export const storeOrdersApi = {
  place: (payload: PlaceOrderPayload): Promise<StoreOrderDto> =>
    apiClient.post<StoreOrderDto>("/store-orders", payload).then((r) => r.data),

  getMine: (page = 1, pageSize = 20): Promise<StoreOrderDto[]> =>
    apiClient
      .get<
        StoreOrderDto[]
      >("/store-orders/mine", { params: { page, pageSize } })
      .then((r) => r.data),

  reportPayment: (orderId: string): Promise<void> =>
    apiClient
      .put(`/store-orders/${orderId}/report-payment`)
      .then(() => undefined),

  // Store owner
  getIncoming: (page = 1, pageSize = 20): Promise<StoreOrderDto[]> =>
    apiClient
      .get<
        StoreOrderDto[]
      >("/store-orders/incoming", { params: { page, pageSize } })
      .then((r) => r.data),

  confirm: (orderId: string, note?: string): Promise<StoreOrderDto> =>
    apiClient
      .put<StoreOrderDto>(`/store-orders/${orderId}/confirm`, { note })
      .then((r) => r.data),

  updateStatus: (
    orderId: string,
    status: StoreOrderStatus,
    note?: string,
  ): Promise<StoreOrderDto> =>
    apiClient
      .put<StoreOrderDto>(`/store-orders/${orderId}/status`, { status, note })
      .then((r) => r.data),
};
