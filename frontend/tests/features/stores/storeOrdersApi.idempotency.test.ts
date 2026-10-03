import { beforeEach, describe, expect, it, vi } from "vitest";
import { storeOrdersApi } from "@/features/stores/api/storeOrdersApi";

const mocks = vi.hoisted(() => ({ post: vi.fn() }));

vi.mock("@/shared/lib/apiClient", () => ({
  apiClient: { post: mocks.post },
}));

describe("storeOrdersApi idempotency", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.post.mockResolvedValue({ data: { id: "order-1" } });
  });

  it("sends the idempotency key as a header, not in the order payload", async () => {
    const payload = {
      storeId: "store-1",
      fulfillmentType: "Pickup" as const,
      lines: [{ productId: "product-1", quantity: 2 }],
    };

    await storeOrdersApi.place(payload, "checkout-attempt-1");

    expect(mocks.post).toHaveBeenCalledWith("/store-orders", payload, {
      headers: { "Idempotency-Key": "checkout-attempt-1" },
    });
  });
});
