import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UpgradeBanner } from "@/features/pets/components/PlanGate";
import { renderWithProviders } from "../../utils/renderWithProviders";

const subscriptionMocks = vi.hoisted(() => ({
  useSubscriptionCatalog: vi.fn(),
  useMyTier: vi.fn(),
}));

vi.mock("@/features/pets/hooks/useSubscription", () => ({
  useSubscriptionCatalog: subscriptionMocks.useSubscriptionCatalog,
}));

vi.mock("@/features/pets/hooks/useMyTier", () => ({
  useMyTier: () => ({ isPlus: false, isFamilia: false, isLoading: false }),
}));

beforeEach(() => {
  vi.clearAllMocks();
});

describe("UpgradeBanner catalog price", () => {
  it("uses the active catalog price instead of a hardcoded amount", () => {
    subscriptionMocks.useSubscriptionCatalog.mockReturnValue({
      data: [{ tier: "UserPlus", monthlyPriceCrc: 3175, isActive: true }],
    });

    renderWithProviders(<UpgradeBanner requires="Plus" />);

    expect(screen.getByText(/₡3.?175\/mes/)).toBeInTheDocument();
    expect(screen.queryByText(/₡2,990\/mes/)).not.toBeInTheDocument();
  });

  it("does not invent a price when the plan is missing from the catalog", () => {
    subscriptionMocks.useSubscriptionCatalog.mockReturnValue({ data: [] });

    renderWithProviders(<UpgradeBanner requires="Plus" />);

    expect(screen.getByText(/precio no disponible/i)).toBeInTheDocument();
  });
});
