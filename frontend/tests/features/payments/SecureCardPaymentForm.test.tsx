import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { SecureCardPaymentForm, type CardPaymentData } from "@/features/payments/components/SecureCardPaymentForm";
import { server } from "../../mocks/server";
import { renderWithProviders } from "../../utils/renderWithProviders";

describe("SecureCardPaymentForm", () => {
  it("renders card inputs when no saved cards are present", () => {
    server.use(http.get("http://localhost:5000/api/payments/profiles", () => HttpResponse.json([])));

    renderWithProviders(
      <SecureCardPaymentForm amountCrc={2990} isProcessing={false} onPay={vi.fn<(data: CardPaymentData) => void>()} />,
    );

    expect(screen.getByLabelText(/nombre en la tarjeta/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/número de tarjeta/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/vencimiento/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/código cvv/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /pagar ₡2.990/i })).toBeInTheDocument();
  });

  it("validates empty fields and shows error message", async () => {
    server.use(http.get("http://localhost:5000/api/payments/profiles", () => HttpResponse.json([])));

    const onPay = vi.fn<(data: CardPaymentData) => void>();
    const user = userEvent.setup();

    renderWithProviders(<SecureCardPaymentForm amountCrc={2990} isProcessing={false} onPay={onPay} />);

    await user.click(screen.getByRole("button", { name: /pagar ₡2.990/i }));

    expect(screen.getByText(/número de tarjeta incompleto/i)).toBeInTheDocument();
    expect(onPay).not.toHaveBeenCalled();
  });

  it("submits tokenized payment data on valid input", async () => {
    server.use(http.get("http://localhost:5000/api/payments/profiles", () => HttpResponse.json([])));

    const onPay = vi.fn<(data: CardPaymentData) => void>();
    const user = userEvent.setup();

    renderWithProviders(<SecureCardPaymentForm amountCrc={2990} isProcessing={false} onPay={onPay} />);

    await user.type(screen.getByLabelText(/nombre en la tarjeta/i), "MARIA MORA");
    await user.type(screen.getByLabelText(/número de tarjeta/i), "4242424242424242");
    await user.type(screen.getByLabelText(/vencimiento/i), "1228");
    await user.type(screen.getByLabelText(/código cvv/i), "123");

    await user.click(screen.getByRole("button", { name: /pagar ₡2.990/i }));

    await waitFor(() => {
      expect(onPay).toHaveBeenCalledTimes(1);
    });

    const calls = onPay.mock.calls;
    const firstCall = calls[0];
    expect(firstCall).toBeDefined();
    if (firstCall) {
      expect(firstCall[0].cardholderName).toBe("MARIA MORA");
      expect(firstCall[0].transientToken).toContain("tok_flex_visa_4242");
      expect(firstCall[0].saveProfile).toBe(true);
    }
  });
});
