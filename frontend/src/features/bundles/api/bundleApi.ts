import { apiClient } from "@/shared/lib/apiClient";

// ── Types ─────────────────────────────────────────────────────────────────────

export type CollarModel = "TractiveGPSDog4" | "TractiveGPSCat4";
export type BundleOrderStatus =
  | "PendingPayment"
  | "Paid"
  | "Sourcing"
  | "Shipped"
  | "Delivered"
  | "Cancelled";

export type BundleProductType =
  | "CollarGpsPlus"
  | "QrPlate"
  | "SiliconeTag"
  | "NfcQrCombo"
  | "EmergencyPack";

export interface BundleOrderDto {
  id: string;
  userId: string;
  collarModel: CollarModel;
  collarModelLabel: string;
  productType: BundleProductType;
  productTypeLabel: string;
  status: BundleOrderStatus;
  statusLabel: string;
  paymentReference: string;
  amountCrc: number;
  shippingFullName: string;
  shippingAddress: string;
  shippingCanton: string;
  shippingPhone: string;
  deliveryNotes: string | null;
  trackingNumber: string | null;
  adminNotes: string | null;
  paymentReportedByUser: boolean;
  activatedSubscriptionId: string | null;
  createdAt: string;
  paidAt: string | null;
  sourcedAt: string | null;
  shippedAt: string | null;
  deliveredAt: string | null;
  cancelledAt: string | null;
}

export interface BundleOrderPageDto {
  items: BundleOrderDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateBundleOrderRequest {
  collarModel: CollarModel;
  shippingFullName: string;
  shippingAddress: string;
  shippingCanton: string;
  shippingPhone: string;
  deliveryNotes?: string;
  productType?: BundleProductType;
}

// ── Labels & Prices ───────────────────────────────────────────────────────────

export const COLLAR_MODEL_LABELS: Record<CollarModel, string> = {
  TractiveGPSDog4: "Tractive GPS DOG 4 — Perros (IPX7)",
  TractiveGPSCat4: "Tractive GPS CAT 4 — Gatos (Ultra liviano)",
};

export const STATUS_COLORS: Record<BundleOrderStatus, string> = {
  PendingPayment: "bg-warn-100 text-warn-700",
  Paid:           "bg-trust-100 text-trust-700",
  Sourcing:       "bg-brand-100 text-brand-700",
  Shipped:        "bg-rescue-100 text-rescue-700",
  Delivered:      "bg-green-100 text-green-700",
  Cancelled:      "bg-sand-100 text-sand-500",
};

export const BUNDLE_AMOUNT_CRC = 49_900;

export const PRODUCT_TYPE_CONFIG: Record<
  BundleProductType,
  { label: string; description: string; priceCrc: number; emoji: string; requiresCollar: boolean }
> = {
  CollarGpsPlus:  { label: "Bundle Collar GPS + 12 meses Plus", description: "Tractive GPS + PawTrack Plus todo incluido · Envío a CR", priceCrc: 49_900, emoji: "📡", requiresCollar: true },
  QrPlate:        { label: "Placa QR de aluminio",              description: "3×5 cm · grabado láser · resistente al agua",             priceCrc:  4_500, emoji: "🔖", requiresCollar: false },
  SiliconeTag:    { label: "Tag de silicona con QR",            description: "Flexible · colores · impresión UV",                       priceCrc:  5_500, emoji: "🏷️", requiresCollar: false },
  NfcQrCombo:     { label: "Combo NFC + QR",                    description: "Toca con Android · escanea con iOS · NTAG213",            priceCrc: 12_000, emoji: "📲", requiresCollar: false },
  EmergencyPack:  { label: "Pack emergencia",                   description: "Placa QR + tarjeta bolsillo + guía de emergencia",        priceCrc:  7_000, emoji: "🆘", requiresCollar: false },
};

// ── API ───────────────────────────────────────────────────────────────────────

export const bundleApi = {
  create: (data: CreateBundleOrderRequest): Promise<BundleOrderDto> =>
    apiClient.post<BundleOrderDto>("/bundles", data).then((r) => r.data),

  getMine: (): Promise<BundleOrderDto[]> =>
    apiClient.get<BundleOrderDto[]>("/bundles/mine").then((r) => r.data),

  reportPayment: (id: string): Promise<void> =>
    apiClient.put(`/bundles/${id}/report-payment`).then(() => undefined),

  cancel: (id: string): Promise<BundleOrderDto> =>
    apiClient.put<BundleOrderDto>(`/bundles/${id}/cancel`).then((r) => r.data),

  // Admin
  getAll: (status?: BundleOrderStatus, page = 1): Promise<BundleOrderPageDto> =>
    apiClient
      .get<BundleOrderPageDto>("/bundles/admin", {
        params: { status, page, pageSize: 25 },
      })
      .then((r) => r.data),

  adminConfirmPayment: (id: string): Promise<BundleOrderDto> =>
    apiClient
      .put<BundleOrderDto>(`/bundles/admin/${id}/confirm-payment`)
      .then((r) => r.data),

  adminMarkSourced: (
    id: string,
    adminNotes?: string,
  ): Promise<BundleOrderDto> =>
    apiClient
      .put<BundleOrderDto>(`/bundles/admin/${id}/sourced`, { adminNotes })
      .then((r) => r.data),

  adminMarkShipped: (
    id: string,
    trackingNumber: string,
    adminNotes?: string,
  ): Promise<BundleOrderDto> =>
    apiClient
      .put<BundleOrderDto>(`/bundles/admin/${id}/shipped`, {
        trackingNumber,
        adminNotes,
      })
      .then((r) => r.data),

  adminMarkDelivered: (id: string): Promise<BundleOrderDto> =>
    apiClient
      .put<BundleOrderDto>(`/bundles/admin/${id}/delivered`)
      .then((r) => r.data),

  adminCancel: (id: string, adminNotes?: string): Promise<BundleOrderDto> =>
    apiClient
      .put<BundleOrderDto>(`/bundles/admin/${id}/cancel`, { adminNotes })
      .then((r) => r.data),
};
