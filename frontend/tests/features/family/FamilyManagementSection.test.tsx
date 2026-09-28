import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { FamilyManagementSection } from "@/features/family/components/FamilyManagementSection";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

const familyMocks = vi.hoisted(() => ({
  useMyFamily: vi.fn(),
  useInviteFamilyMember: vi.fn(),
  useRemoveFamilyMember: vi.fn(),
  useMyEntitlements: vi.fn(),
}));

vi.mock("@/features/family/hooks/useFamily", () => ({
  useMyFamily: familyMocks.useMyFamily,
  useInviteFamilyMember: familyMocks.useInviteFamilyMember,
  useRemoveFamilyMember: familyMocks.useRemoveFamilyMember,
}));

vi.mock("@/features/pets/hooks/useSubscription", () => ({
  useMyEntitlements: familyMocks.useMyEntitlements,
}));

vi.mock("@/features/pets/hooks/useMyTier", () => ({
  useMyTier: () => ({ isPlus: true, isFamilia: true, isLoading: false }),
}));

const family = {
  id: "family-1",
  name: "Familia Mora",
  pendingInvitations: 0,
  pendingInvitationLimit: 3,
  members: [
    {
      userId: "owner-1",
      name: "Ana Mora",
      email: "ana@example.com",
      role: "Owner" as const,
      joinedAt: "2026-09-28T00:00:00Z",
    },
    {
      userId: "member-1",
      name: "Luis Mora",
      email: "luis@example.com",
      role: "Member" as const,
      joinedAt: "2026-09-28T00:00:00Z",
    },
  ],
};

beforeEach(() => {
  vi.clearAllMocks();
  useAuthStore.setState({
    user: {
      id: "owner-1",
      name: "Ana Mora",
      email: "ana@example.com",
      role: "Owner",
      isAdmin: false,
    },
  });
  familyMocks.useMyFamily.mockReturnValue({ data: family, isLoading: false, isError: false });
  familyMocks.useInviteFamilyMember.mockReturnValue({ mutate: vi.fn(), isPending: false });
  familyMocks.useRemoveFamilyMember.mockReturnValue({ mutate: vi.fn(), isPending: false });
  familyMocks.useMyEntitlements.mockReturnValue({
    data: {
      entitlements: {
        MaxFamilyMembers: { numericValue: 3, consumed: 0 },
      },
    },
    isLoading: false,
    isError: false,
  });
});

describe("FamilyManagementSection", () => {
  it("shows the plan entitlement limit instead of a hardcoded family size", () => {
    renderWithProviders(<FamilyManagementSection />);

    expect(screen.getByText("2/3 miembros")).toBeInTheDocument();
    expect(screen.getByLabelText("Correo del nuevo miembro")).toBeInTheDocument();
  });

  it("hides member invitations when the entitlement limit is reached", () => {
    familyMocks.useMyFamily.mockReturnValue({
      data: { ...family, pendingInvitations: 1 },
      isLoading: false,
      isError: false,
    });

    renderWithProviders(<FamilyManagementSection />);

    expect(screen.getByText("3/3 miembros")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Invitar" })).not.toBeInTheDocument();
  });
});
