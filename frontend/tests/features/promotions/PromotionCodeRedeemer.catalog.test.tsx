import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { PromotionCodeRedeemer } from "@/features/promotions/components/PromotionCodeRedeemer";
import { server } from "../../mocks/server";
import { renderWithProviders } from "../../utils/renderWithProviders";

const API = "http://localhost:5000/api";

describe("PromotionCodeRedeemer plan prices", () => {
  it("uses active catalog prices when selecting a discounted tier", async () => {
    server.use(
      http.get(`${API}/catalog/subscription-plans`, () =>
        HttpResponse.json([
          {
            id: "plan-plus",
            tier: "UserPlus",
            displayName: "Plus",
            description: "",
            monthlyPriceCrc: 3175,
            annualPriceCrc: null,
            isActive: true,
            version: "v2",
          },
          {
            id: "plan-family",
            tier: "UserFamilia",
            displayName: "Familia",
            description: "",
            monthlyPriceCrc: 5990,
            annualPriceCrc: null,
            isActive: true,
            version: "v2",
          },
        ]),
      ),
      http.get(`${API}/promotions/validate/SAVE20`, () =>
        HttpResponse.json({
          code: "SAVE20",
          type: "PercentageDiscount",
          benefitDescription: "20% de descuento",
          discountPercent: 20,
          freeMonths: null,
          targetTier: null,
          isFullyFree: false,
          requiresPayment: true,
        }),
      ),
    );

    const user = userEvent.setup();
    renderWithProviders(<PromotionCodeRedeemer />);

    await user.type(screen.getByPlaceholderText("Ej: FREEPL4B"), "SAVE20");
    await user.click(screen.getByRole("button", { name: "Validar" }));

    const planSelect = await screen.findByLabelText("Seleccioná el plan al que aplicar el descuento");
    expect(planSelect).toHaveTextContent(/Plan Plus \(₡3\s*175\/mes\)/);
    expect(planSelect).toHaveTextContent(/Plan Familia \(₡5\s*990\/mes\)/);
    expect(planSelect).not.toHaveTextContent("₡2,990");
  });
});
