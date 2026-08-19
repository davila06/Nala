import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type StoreStatus = "Pending" | "Active" | "Suspended";
export type ProductCategory =
  | "Food"
  | "Accessories"
  | "Grooming"
  | "Health"
  | "Toys"
  | "Clothing"
  | "Other";
export type OrderFulfillmentType = "Pickup" | "Delivery";
export type StoreOrderStatus =
  | "PendingPayment"
  | "PaymentReported"
  | "Confirmed"
  | "Preparing"
  | "ReadyForPickup"
  | "OutForDelivery"
  | "Delivered"
  | "Cancelled";

export interface PublicStoreDto {
  id: string;
  name: string;
  description: string;
  address: string;
  lat: number;
  lng: number;
  phoneNumber: string | null;
  website: string | null;
  logoUrl: string | null;
  isFeatured: boolean;
  status: StoreStatus;
}

export interface StoreProductDto {
  id: string;
  storeId: string;
  name: string;
  description: string | null;
  category: ProductCategory;
  priceCrc: number;
  imageUrl: string | null;
  isAvailable: boolean;
}

export interface StoreDetailDto {
  store: PublicStoreDto;
  products: StoreProductDto[];
}

export interface StoreOrderItemDto {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPriceCrc: number;
  subtotalCrc: number;
}

export interface StoreOrderDto {
  id: string;
  storeId: string;
  storeName: string;
  customerId: string;
  status: StoreOrderStatus;
  fulfillmentType: OrderFulfillmentType;
  paymentReference: string;
  totalCrc: number;
  deliveryAddress: string | null;
  customerNote: string | null;
  storeNote: string | null;
  paymentReportedByCustomer: boolean;
  placedAt: string;
  confirmedAt: string | null;
  completedAt: string | null;
  items: StoreOrderItemDto[];
}

// ── Labels ────────────────────────────────────────────────────────────────────

export const CATEGORY_LABELS: Record<ProductCategory, string> = {
  Food: "🍖 Alimentos",
  Accessories: "🎒 Accesorios",
  Grooming: "✂️ Grooming",
  Health: "💊 Salud",
  Toys: "🎾 Juguetes",
  Clothing: "👕 Ropa",
  Other: "📦 Otro",
};

export const ORDER_STATUS_LABELS: Record<StoreOrderStatus, string> = {
  PendingPayment: "Pendiente de pago",
  PaymentReported: "Pago reportado",
  Confirmed: "Confirmado",
  Preparing: "Preparando",
  ReadyForPickup: "Listo para recoger",
  OutForDelivery: "En camino",
  Delivered: "Entregado",
  Cancelled: "Cancelado",
};

export const ORDER_STATUS_COLORS: Record<StoreOrderStatus, string> = {
  PendingPayment: "bg-warn-100 text-warn-700",
  PaymentReported: "bg-trust-100 text-trust-700",
  Confirmed: "bg-brand-100 text-brand-700",
  Preparing: "bg-sand-100 text-sand-700",
  ReadyForPickup: "bg-rescue-100 text-rescue-700",
  OutForDelivery: "bg-rescue-200 text-rescue-800",
  Delivered: "bg-rescue-50 text-rescue-600",
  Cancelled: "bg-danger-100 text-danger-700",
};

// ── API ───────────────────────────────────────────────────────────────────────

export const storesApi = {
  // Public
  getAll: (): Promise<PublicStoreDto[]> =>
    apiClient.get<PublicStoreDto[]>("/public/stores").then((r) => r.data),

  getDetail: (id: string): Promise<StoreDetailDto> =>
    apiClient.get<StoreDetailDto>(`/public/stores/${id}`).then((r) => r.data),

  // Store owner
  getMine: (): Promise<PublicStoreDto> =>
    apiClient.get<PublicStoreDto>("/stores/mine").then((r) => r.data),

  updateProfile: (data: {
    name: string;
    description: string;
    address: string;
    lat: number;
    lng: number;
    phoneNumber?: string;
    website?: string;
  }): Promise<PublicStoreDto> =>
    apiClient.put<PublicStoreDto>("/stores/profile", data).then((r) => r.data),

  getProducts: (): Promise<StoreProductDto[]> =>
    apiClient.get<StoreProductDto[]>("/stores/products").then((r) => r.data),

  addProduct: (data: {
    name: string;
    description?: string;
    category: string;
    priceCrc: number;
  }): Promise<StoreProductDto> =>
    apiClient
      .post<StoreProductDto>("/stores/products", data)
      .then((r) => r.data),

  updateProduct: (
    id: string,
    data: {
      name: string;
      description?: string;
      category: string;
      priceCrc: number;
      isAvailable: boolean;
    },
  ): Promise<StoreProductDto> =>
    apiClient
      .put<StoreProductDto>(`/stores/products/${id}`, data)
      .then((r) => r.data),

  deleteProduct: (id: string): Promise<void> =>
    apiClient.delete(`/stores/products/${id}`).then(() => undefined),

  // Register
  register: (data: {
    name: string;
    description: string;
    address: string;
    lat: number;
    lng: number;
    contactEmail: string;
    password: string;
  }): Promise<PublicStoreDto> =>
    apiClient
      .post<PublicStoreDto>("/stores/register", data)
      .then((r) => r.data),
};
