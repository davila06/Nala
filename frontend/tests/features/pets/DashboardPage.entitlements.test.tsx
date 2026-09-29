import { act, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DashboardPage from "@/features/pets/pages/DashboardPage";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

const dashboardMocks = vi.hoisted(() => ({
  usePets: vi.fn(),
  useMyTier: vi.fn(),
  useMyEntitlements: vi.fn(),
  shouldShowOnboarding: vi.fn(),
}));

vi.mock("@/features/pets/hooks/usePets", () => ({ usePets: dashboardMocks.usePets }));
vi.mock("@/features/pets/hooks/useMyTier", () => ({ useMyTier: dashboardMocks.useMyTier }));
vi.mock("@/features/pets/hooks/useSubscription", () => ({ useMyEntitlements: dashboardMocks.useMyEntitlements }));
vi.mock("@/features/pets/components/HolographicPetCard", () => ({
  HolographicPetCard: ({ pet }: { pet: { name: string } }) => <li>{pet.name}</li>,
}));
vi.mock("@/features/pets/components/OnboardingWizard", () => ({
  OnboardingWizard: () => <div data-testid="onboarding" />,
}));
vi.mock("@/features/pets/components/onboardingStorage", () => ({
  shouldShowOnboarding: dashboardMocks.shouldShowOnboarding,
}));
vi.mock("@/features/locations/components/AlertPreferencesToggle", () => ({ AlertPreferencesToggle: () => null }));
vi.mock("@/features/incentives/components/LeaderboardWidget", () => ({ LeaderboardWidget: () => null }));
vi.mock("@/features/advertising/components/BillboardBanner", () => ({
  BillboardBanner: () => <div data-testid="billboard" />,
}));
vi.mock("@/features/pets/components/FreemiumModal", () => ({
  FreemiumModal: () => <div data-testid="upgrade-modal" />,
}));
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
  dashboardMocks.shouldShowOnboarding.mockReturnValue(false);
  dashboardMocks.useMyEntitlements.mockReturnValue({
    data: { entitlements: { MaxPets: { numericValue: 25, consumed: 3 } } },
    isLoading: false,
    isError: false,
  });
});

describe("DashboardPage pet capacity", () => {
  it("suppresses commercial surfaces while a pet is lost but preserves emergency context", () => {
    dashboardMocks.usePets.mockReturnValue({
      data: [{ ...pets[0], status: "Lost" }, ...pets.slice(1)],
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });
    dashboardMocks.useMyTier.mockReturnValue({ isPlus: false, isFamilia: false, isLoading: false });
    dashboardMocks.useMyEntitlements.mockReturnValue({
      data: { entitlements: { MaxPets: { numericValue: 3, consumed: 3 } } },
      isLoading: false,
      isError: false,
    });
    renderWithProviders(<DashboardPage />);

    expect(screen.getByText(/1 perdida/i)).toBeInTheDocument();
    expect(screen.queryByTestId("billboard")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /activa plus/i })).not.toBeInTheDocument();
    act(() => {
      window.dispatchEvent(new Event("pawtrack:open-upgrade-modal"));
    });
    expect(screen.queryByTestId("upgrade-modal")).not.toBeInTheDocument();
  });

  it("does not put first-run onboarding in front of urgent pet selection", () => {
    dashboardMocks.usePets.mockReturnValue({ data: [], isLoading: false, isError: false, refetch: vi.fn() });
    dashboardMocks.shouldShowOnboarding.mockReturnValue(true);
    renderWithProviders(<DashboardPage />, { initialEntries: ["/dashboard?action=report-lost"] });
    expect(screen.queryByTestId("onboarding")).not.toBeInTheDocument();
    expect(screen.queryByTestId("billboard")).not.toBeInTheDocument();
  });

  it("keeps the normal dashboard billboard when no loss is active", () => {
    renderWithProviders(<DashboardPage />);
    expect(screen.getByTestId("billboard")).toBeInTheDocument();
  });

  it("shows and enforces the entitlement-backed family pet limit", () => {
    renderWithProviders(<DashboardPage />);

    expect(screen.getByText("3 / 25 mascotas")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /registrar mascota/i })).toBeInTheDocument();
  });

  it("does not advertise an available pet slot when the entitlement snapshot failed", () => {
    dashboardMocks.useMyEntitlements.mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    });

    renderWithProviders(<DashboardPage />);

    expect(screen.getByText(/no se pudo verificar el límite/i)).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /registrar mascota/i })).not.toBeInTheDocument();
  });
});
