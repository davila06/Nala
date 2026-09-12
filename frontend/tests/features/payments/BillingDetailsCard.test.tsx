import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { BillingDetailsCard } from "@/features/payments/components/BillingDetailsCard";
import { server } from "../../mocks/server";
import { renderWithProviders } from "../../utils/renderWithProviders";

describe("BillingDetailsCard", () => {
  it("renders empty state when no billing profile is configured", async () => {
    server.use(
      http.get("http://localhost:5000/api/billing/profile", () => HttpResponse.json(null)),
      http.get("http://localhost:5000/api/billing/invoices", () => HttpResponse.json([])),
    );

    renderWithProviders(<BillingDetailsCard />);

    await waitFor(() => {
      expect(screen.getByText(/no has configurado tus datos de facturación/i)).toBeInTheDocument();
    });
    expect(screen.getByRole("button", { name: /\+ configurar datos fiscales/i })).toBeInTheDocument();
  });

  it("renders existing billing profile details and opens edit mode", async () => {
    server.use(
      http.get("http://localhost:5000/api/billing/profile", () =>
        HttpResponse.json({
          id: "bp-1",
          userId: "uid-1",
          identificationType: "Juridica",
          identificationNumber: "3101999888",
          legalName: "VETERINARIA EL BOSQUE S.A.",
          billingEmail: "facturas@elbosque.cr",
          requiresInvoice: true,
          createdAt: new Date().toISOString(),
        }),
      ),
      http.get("http://localhost:5000/api/billing/invoices", () => HttpResponse.json([])),
    );

    const user = userEvent.setup();
    renderWithProviders(<BillingDetailsCard />);

    await waitFor(() => {
      expect(screen.getByText("VETERINARIA EL BOSQUE S.A.")).toBeInTheDocument();
      expect(screen.getByText(/juridica: 3101999888/i)).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /editar datos fiscales/i }));

    expect(screen.getByLabelText(/razón social/i)).toHaveValue("VETERINARIA EL BOSQUE S.A.");
  });
});
