import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { WeightTrendChart } from "@/features/medical/components/WeightTrendChart";
import { useWeightHistory } from "@/features/medical/hooks/useMedical";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/medical/hooks/useMedical", () => ({ useWeightHistory: vi.fn() }));
vi.mock("@/features/pets/components/PlanGate", () => ({
  PlanGate: ({ children }: { children: React.ReactNode }) => children,
}));

describe("WeightTrendChart", () => {
  it("labels breed weight data as a reference, not a health diagnosis", () => {
    vi.mocked(useWeightHistory).mockReturnValue({
      data: {
        entries: [
          { date: "2026-08-01", weightKg: 10, source: "Owner", clinicName: null },
          { date: "2026-09-01", weightKg: 13, source: "Owner", clinicName: null },
        ],
        reference: { minKg: 8, maxKg: 14, label: "Rango saludable" },
        weightChangeAlert: "El peso registrado subió 3 kg.",
      },
      isLoading: false,
      error: null,
    } as ReturnType<typeof useWeightHistory>);

    renderWithProviders(<WeightTrendChart petId="pet-1" petName="Max" />);

    expect(screen.getByText("El peso registrado subió 3 kg.")).toBeInTheDocument();
    expect(screen.getByText("Referencia de peso para la raza")).toBeInTheDocument();
    expect(screen.queryByText("Rango saludable")).not.toBeInTheDocument();
  });
});
