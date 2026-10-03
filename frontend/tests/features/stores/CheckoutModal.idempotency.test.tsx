import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { CheckoutModal } from "@/features/stores/components/CheckoutModal";
import { renderWithProviders } from "../../utils/renderWithProviders";

const mocks = vi.hoisted(() => ({
  cartState: {
    items: [{ product: { id: "product-1", name: "Alimento", priceCrc: 2990 }, quantity: 1 }],
    storeId: "store-1",
    totalCrc: () => 2990,
    clear: vi.fn(),
  },
  usePlaceOrder: vi.fn(),
  mutate: vi.fn(),
  toastError: vi.fn(),
}));

vi.mock("@/features/stores/store/cartStore", () => ({
  useCartStore: () => mocks.cartState,
}));

vi.mock("@/features/stores/hooks/useStoreOrders", () => ({
  usePlaceOrder: mocks.usePlaceOrder,
}));

vi.mock("@/shared/lib/toast", () => ({
  toast: { error: mocks.toastError },
}));

describe("CheckoutModal idempotency", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.usePlaceOrder.mockReturnValue({ mutate: mocks.mutate, isPending: false });
    mocks.mutate.mockImplementation((_request: unknown, callbacks: { onError?: () => void }) => {
      callbacks.onError?.();
    });
  });

  it("reuses the same idempotency key when retrying the same checkout", async () => {
    const user = userEvent.setup();
    renderWithProviders(<CheckoutModal isOpen onClose={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Hacer pedido" }));
    await user.click(screen.getByRole("button", { name: "Hacer pedido" }));

    const firstRequest = mocks.mutate.mock.calls[0]?.[0] as { idempotencyKey?: string };
    const retryRequest = mocks.mutate.mock.calls[1]?.[0] as { idempotencyKey?: string };
    expect(firstRequest.idempotencyKey).toBeTruthy();
    expect(retryRequest.idempotencyKey).toBe(firstRequest.idempotencyKey);
  });

  it("directs the customer to their orders after an idempotency conflict", async () => {
    mocks.mutate.mockImplementation((_request: unknown, callbacks: { onError?: (error: unknown) => void }) => {
      callbacks.onError?.({ isAxiosError: true, response: { status: 409 } });
    });
    const user = userEvent.setup();
    renderWithProviders(<CheckoutModal isOpen onClose={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Hacer pedido" }));

    expect(mocks.toastError).toHaveBeenCalledWith(expect.stringMatching(/mis pedidos/i));
  });
});
