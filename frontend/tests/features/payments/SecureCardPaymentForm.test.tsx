import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { SecureCardPaymentForm, type CardPaymentData } from "@/features/payments/components/SecureCardPaymentForm";
import { server } from "../../mocks/server";
import { renderWithProviders } from "../../utils/renderWithProviders";

describe("SecureCardPaymentForm", () => {
  it("renders hosted card fields when no saved cards are present", () => {
    server.use(http.get("http://localhost:5000/api/payments/profiles", () => HttpResponse.json([])));
    server.use(
      http.get("http://localhost:5000/api/payments/capture-context", () =>
        HttpResponse.json({
          clientLibraryUrl: "https://flex.cybersource.com/flex.js",
          captureContextJwt: "sandbox-capture-context",
          keyId: "sandbox-key",
          isConfigured: true,
        }),
      ),
    );

    renderWithProviders(
      <SecureCardPaymentForm amountCrc={2990} isProcessing={false} onPay={vi.fn<(data: CardPaymentData) => void>()} />,
    );

    expect(screen.getByLabelText(/nombre en la tarjeta/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/vencimiento/i)).toBeInTheDocument();
    expect(screen.getByText(/campo seguro de tarjeta/i)).toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: /número de tarjeta/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: /código cvv/i })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /pagar ₡2.990/i })).toBeInTheDocument();
  });

  it("validates empty fields and shows error message", async () => {
    server.use(http.get("http://localhost:5000/api/payments/profiles", () => HttpResponse.json([])));
    server.use(
      http.get("http://localhost:5000/api/payments/capture-context", () =>
        HttpResponse.json({
          clientLibraryUrl: "",
          captureContextJwt: "",
          keyId: "",
          isConfigured: false,
        }),
      ),
    );

    const onPay = vi.fn<(data: CardPaymentData) => void>();
    const user = userEvent.setup();

    renderWithProviders(<SecureCardPaymentForm amountCrc={2990} isProcessing={false} onPay={onPay} />);

    await user.click(screen.getByRole("button", { name: /pagar ₡2.990/i }));

    expect(screen.getByText(/fecha de vencimiento inválida/i)).toBeInTheDocument();
    expect(onPay).not.toHaveBeenCalled();
  });

  it("submits tokenized payment data on valid input", async () => {
    server.use(http.get("http://localhost:5000/api/payments/profiles", () => HttpResponse.json([])));
    server.use(
      http.get("http://localhost:5000/api/payments/capture-context", () =>
        HttpResponse.json({
          clientLibraryUrl: "https://flex.cybersource.com/flex.js",
          captureContextJwt: "sandbox-capture-context",
          keyId: "sandbox-key",
          isConfigured: true,
        }),
      ),
    );
    vi.stubGlobal(
      "Flex",
      class {
        constructor() {}

        microform() {
          return {
            createField: () => ({ load: vi.fn() }),
            createToken: (_options: unknown, callback: (error: Error | null, token?: string) => void) =>
              callback(null, "sandbox-transient-token"),
          };
        }
      },
    );

    const onPay = vi.fn<(data: CardPaymentData) => void>();
    const user = userEvent.setup();

    renderWithProviders(<SecureCardPaymentForm amountCrc={2990} isProcessing={false} onPay={onPay} />);

    await user.type(screen.getByLabelText(/nombre en la tarjeta/i), "MARIA MORA");
    await user.type(screen.getByLabelText(/vencimiento/i), "1228");

    await user.click(screen.getByRole("button", { name: /pagar ₡2.990/i }));

    await waitFor(() => {
      expect(onPay).toHaveBeenCalledTimes(1);
    });

    const calls = onPay.mock.calls;
    const firstCall = calls[0];
    expect(firstCall).toBeDefined();
    if (firstCall) {
      expect(firstCall[0].cardholderName).toBe("MARIA MORA");
      expect(firstCall[0].transientToken).toBe("sandbox-transient-token");
      expect(firstCall[0].transientToken).not.toContain("tok_flex");
      expect(firstCall[0].saveProfile).toBe(true);
    }
  });
});
