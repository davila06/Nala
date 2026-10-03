import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { SinpePaymentModal } from "@/features/pets/components/SinpePaymentModal";
import { renderWithProviders } from "../../utils/renderWithProviders";

const mocks = vi.hoisted(() => ({
  createSubscription: vi.fn(),
  reportPayment: vi.fn(),
  chargeCard: vi.fn(),
  getMine: vi.fn(),
  useSubscriptionCatalog: vi.fn(),
  useBillingProfile: vi.fn(),
  onSuccess: vi.fn(),
}));

vi.mock("@/features/pets/hooks/useSubscription", () => ({
  useCreateSubscription: () => ({ mutateAsync: mocks.createSubscription, isPending: false }),
  useReportPayment: () => ({ mutateAsync: mocks.reportPayment, isPending: false }),
  useSubscriptionCatalog: mocks.useSubscriptionCatalog,
}));

vi.mock("@/features/payments/hooks/usePaymentProfiles", () => ({
  useChargeCard: () => ({ mutateAsync: mocks.chargeCard, isPending: false }),
}));

vi.mock("@/features/payments/hooks/useBilling", () => ({
  useBillingProfile: mocks.useBillingProfile,
}));

vi.mock("@/features/payments/components/SecureCardPaymentForm", () => ({
  SecureCardPaymentForm: ({ onPay }: { onPay: (data: unknown) => void }) => (
    <button
      type="button"
      onClick={() =>
        onPay({
          paymentProfileId: "profile-1",
          transientToken: "token-1",
          cardholderName: "Test User",
          saveProfile: false,
        })
      }
    >
      Pay test card
    </button>
  ),
}));

vi.mock("@/shared/hooks/useHaptic", () => ({
  useHaptic: () => ({ tap: vi.fn(), success: vi.fn(), warning: vi.fn() }),
}));

vi.mock("@/features/pets/api/subscriptionApi", () => ({
  subscriptionApi: {
    getMine: mocks.getMine,
    getSinpeQrBlob: vi.fn().mockResolvedValue(new Blob()),
  },
}));

describe("SinpePaymentModal settlement states", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createSubscription.mockResolvedValue({
      id: "subscription-1",
      amountCrc: 2990,
      paymentReference: "PT-TEST-1",
    });
    mocks.reportPayment.mockResolvedValue(undefined);
    mocks.chargeCard.mockResolvedValue({ success: true, authorizationCode: "AUTH-1" });
    mocks.getMine.mockResolvedValue({ id: "subscription-1", status: "PendingPayment" });
    mocks.useSubscriptionCatalog.mockReturnValue({
      data: [{ tier: "UserPlus", displayName: "Plus", monthlyPriceCrc: 2990, annualPriceCrc: null }],
    });
    mocks.useBillingProfile.mockReturnValue({ data: undefined });
  });

  it("keeps SINPE report pending without notifying subscription success", async () => {
    const user = userEvent.setup();
    renderWithProviders(<SinpePaymentModal tier="UserPlus" onClose={vi.fn()} onSuccess={mocks.onSuccess} />);

    await user.click(screen.getByRole("button", { name: /continuar con sinpe/i }));
    await user.click(await screen.findByRole("button", { name: /ya realicé el pago sinpe/i }));

    await waitFor(() => expect(screen.getByText("¡Aviso recibido con éxito!")).toBeInTheDocument());
    expect(mocks.onSuccess).not.toHaveBeenCalled();
  });

  it("does not report card authorization as success before settlement", async () => {
    const user = userEvent.setup();
    renderWithProviders(<SinpePaymentModal tier="UserPlus" onClose={vi.fn()} onSuccess={mocks.onSuccess} />);

    await user.click(screen.getByRole("button", { name: /tarjeta \(inmediato\)/i }));
    await user.click(screen.getByRole("button", { name: "Pay test card" }));

    expect(await screen.findByText("Autorización recibida")).toBeInTheDocument();
    expect(mocks.onSuccess).not.toHaveBeenCalled();
  });

  it("notifies success only after settlement and explicit confirmation", async () => {
    mocks.getMine.mockResolvedValue({ id: "subscription-1", status: "Active" });
    const user = userEvent.setup();
    renderWithProviders(<SinpePaymentModal tier="UserPlus" onClose={vi.fn()} onSuccess={mocks.onSuccess} />);

    await user.click(screen.getByRole("button", { name: /tarjeta \(inmediato\)/i }));
    await user.click(screen.getByRole("button", { name: "Pay test card" }));

    const activeButton = await screen.findByRole("button", { name: /comenzar a disfrutar/i });
    expect(mocks.onSuccess).not.toHaveBeenCalled();
    await user.click(activeButton);
    expect(mocks.onSuccess).toHaveBeenCalledOnce();
  });
});
