import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import ReportWelfareCasePage from "@/features/animal-welfare/pages/ReportWelfareCasePage";
import { server } from "../../mocks/server";
import { renderWithProviders } from "../../utils/renderWithProviders";

describe("ReportWelfareCasePage routing preference", () => {
  it("submits the routing opt-in while making clear that an admin confirms the recipient", async () => {
    let postedPayload: Record<string, unknown> | null = null;
    server.use(
      http.post("http://localhost:5000/api/public/welfare-cases", async ({ request }) => {
        postedPayload = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          publicCode: "WELFARE-123",
          status: "Received",
          severity: "High",
          canton: "Heredia",
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          closedAt: null,
        });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ReportWelfareCasePage />);

    await user.type(screen.getByPlaceholderText("Ej. Heredia"), "Heredia");
    await user.type(
      screen.getByPlaceholderText(/indica lugar, fecha aproximada/i),
      "Animal observado en riesgo, requiere valoración de bienestar.",
    );
    await user.click(screen.getByRole("checkbox", { name: /sugerencia automática de destinatario/i }));

    expect(screen.getByText(/Admin o Superadmin confirme el destinatario/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /enviar reporte/i }));

    await waitFor(() => expect(postedPayload).not.toBeNull());
    expect(postedPayload).toMatchObject({ autoRoutingRequested: true });
    expect(postedPayload).not.toHaveProperty("recipientUserId");
  });
});
