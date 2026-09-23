import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import HealthHubPage from "@/features/medical/pages/HealthHubPage";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/pets/hooks/usePets", () => ({
  usePets: vi.fn(),
}));

vi.mock("@/features/auth/store/authStore", () => ({
  useAuthStore: (selector: (state: { user?: { role?: string } }) => unknown) => selector({ user: { role: "Owner" } }),
}));

import { usePets } from "@/features/pets/hooks/usePets";

const mockedUsePets = vi.mocked(usePets);

describe("HealthHubPage", () => {
  it("shows the full pet name without truncating it in the health card", () => {
    const longName = "Luna de la familia y la casa Costa Rica";
    mockedUsePets.mockReturnValue({
      data: [
        {
          id: "pet-1",
          name: longName,
          photoUrl: null,
        },
      ],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof usePets>);

    renderWithProviders(<HealthHubPage />);

    const name = screen.getByText(longName);
    expect(name).toBeInTheDocument();
    expect(name).not.toHaveClass("truncate");
    expect(screen.getByText(/expediente, vacunas y recordatorios/i)).toBeInTheDocument();
  });
});
