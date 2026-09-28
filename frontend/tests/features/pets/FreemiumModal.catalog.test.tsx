import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FreemiumModal } from "@/features/pets/components/FreemiumModal";
import { renderWithProviders } from "../../utils/renderWithProviders";

const subscriptionMocks = vi.hoisted(() => ({
  useSubscriptionCatalog: vi.fn(),
  useMyTier: vi.fn(),
}));

vi.mock("@/features/pets/hooks/useSubscription", () => ({
  useSubscriptionCatalog: subscriptionMocks.useSubscriptionCatalog,
}));

vi.mock("@/features/pets/hooks/useMyTier", () => ({
  useMyTier: subscriptionMocks.useMyTier,
}));

vi.mock("@/features/pets/components/SinpePaymentModal", () => ({
  SinpePaymentModal: () => null,
}));

vi.mock("@/features/bundles/components/BundleOrderModal", () => ({
  BundleOrderModal: () => null,
}));

const activeCatalog = [
  {
    id: "plan-plus",
    tier: "UserPlus",
    displayName: "Plus",
    description: "",
    monthlyPriceCrc: 3990,
    annualPriceCrc: null,
    isActive: true,
    version: "v1",
  },
  {
    id: "plan-family",
    tier: "UserFamilia",
    displayName: "Familia",
    description: "",
    monthlyPriceCrc: 5990,
    annualPriceCrc: null,
    isActive: true,
    version: "v1",
  },
];

beforeEach(() => {
  vi.clearAllMocks();
  subscriptionMocks.useSubscriptionCatalog.mockReturnValue({ data: activeCatalog });
  subscriptionMocks.useMyTier.mockReturnValue({
    tier: "Explorador",
    isPlus: false,
    isFamilia: false,
    isLoading: false,
  });
});

describe("FreemiumModal catalog and claims", () => {
  it("uses the active catalog and avoids unlimited or unimplemented promises", () => {
    renderWithProviders(<FreemiumModal onClose={vi.fn()} />);

    expect(screen.getByLabelText("Plan actual")).toBeInTheDocument();
    expect(screen.getByText(/₡3.?990/)).toBeInTheDocument();
    expect(screen.getByText(/₡5.?990/)).toBeInTheDocument();
    expect(screen.getByText(/hasta 25 mascotas activas/i)).toBeInTheDocument();
    expect(screen.getByText(/hasta 5 miembros/i)).toBeInTheDocument();
    expect(screen.getByText(/10 búsquedas visuales por ciclo/i)).toBeInTheDocument();
    expect(screen.queryByText(/ilimitad[oa]/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/predicción de movimiento/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/soporte prioritario/i)).not.toBeInTheDocument();
    expect(screen.queryByText("Bundle Collar GPS + 12 meses Plus")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Pedir collar →" })).not.toBeInTheDocument();
  });

  it("does not fall back to stale hardcoded prices or allow purchase without catalog approval", () => {
    subscriptionMocks.useSubscriptionCatalog.mockReturnValue({ data: undefined });

    renderWithProviders(<FreemiumModal onClose={vi.fn()} />);

    expect(screen.getAllByText(/precio no disponible/i)).toHaveLength(2);
    expect(screen.getByRole("button", { name: "Activar Plus" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Activar Familia" })).toBeDisabled();
  });
});
