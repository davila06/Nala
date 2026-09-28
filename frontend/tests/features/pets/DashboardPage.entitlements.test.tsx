import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DashboardPage from "@/features/pets/pages/DashboardPage";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

const dashboardMocks = vi.hoisted(() => ({
  usePets: vi.fn(),
  useMyTier: vi.fn(),
  useMyEntitlements: vi.fn(),
}));

vi.mock("@/features/pets/hooks/usePets", () => ({ usePets: dashboardMocks.usePets }));
vi.mock("@/features/pets/hooks/useMyTier", () => ({ useMyTier: dashboardMocks.useMyTier }));
vi.mock("@/features/pets/hooks/useSubscription", () => ({ useMyEntitlements: dashboardMocks.useMyEntitlements }));
vi.mock("@/features/pets/components/HolographicPetCard", () => ({
  HolographicPetCard: ({ pet }: { pet: { name: string } }) => <li>{pet.name}</li>,
}));
vi.mock("@/features/pets/components/OnboardingWizard", () => ({ OnboardingWizard: () => null }));
vi.mock("@/features/pets/components/onboardingStorage", () => ({ shouldShowOnboarding: () => false }));
vi.mock("@/features/locations/components/AlertPreferencesToggle", () => ({ AlertPreferencesToggle: () => null }));
vi.mock("@/features/incentives/components/LeaderboardWidget", () => ({ LeaderboardWidget: () => null }));
vi.mock("@/features/advertising/components/BillboardBanner", () => ({ BillboardBanner: () => null }));
vi.mock("@/shared/hooks/usePullToRefresh", () => ({
  usePullToRefresh: () => ({ containerRef: { current: null }, pullProgress: 0, isRefreshing: false }),
}));

const pets = [
  { id: "pet-1", name: "Luna", status: "Active", species: "Dog" },
  { id: "pet-2", name: "Max", status: "Active", species: "Dog" },
  { id: "pet-3", name: "Mora", status: "Active", species: "Cat" },
];

beforeEach(() => {
  vi.clearAllMocks();
  useAuthStore.setState({
    user: { id: "owner-1", name: "Ana Mora", email: "ana@example.com", role: "Owner", isAdmin: false },
  });
  dashboardMocks.usePets.mockReturnValue({ data: pets, isLoading: false, isError: false, refetch: vi.fn() });
  dashboardMocks.useMyTier.mockReturnValue({ isPlus: true, isFamilia: true, isLoading: false });
  dashboardMocks.useMyEntitlements.mockReturnValue({
    data: { entitlements: { MaxPets: { numericValue: 25, consumed: 3 } } },
    isLoading: false,
    isError: false,
  });
});

describe("DashboardPage pet capacity", () => {
  it("shows and enforces the entitlement-backed family pet limit", () => {
    renderWithProviders(<DashboardPage />);

    expect(screen.getByText("3 / 25 mascotas")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /registrar mascota/i })).toBeInTheDocument();
  });
});
