import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import IncomingProviderBookingsPage from "@/features/service-providers/pages/IncomingProviderBookingsPage";

const updateBooking = vi.fn();
vi.mock("@/features/service-providers/hooks/useServiceProviders", () => ({
  useIncomingProviderBookings: () => ({
    data: [
      {
        id: "booking-1",
        serviceName: "Paseo de prueba",
        startsAt: "2026-09-28T15:00:00Z",
        quantity: 1,
        status: "Confirmed",
      },
    ],
    isLoading: false,
  }),
  useUpdateProviderBookingStatus: () => ({ mutate: updateBooking, isPending: false }),
}));

describe("IncomingProviderBookingsPage", () => {
  it("submits only the booking status after confirming a rejection", () => {
    updateBooking.mockClear();
    render(<IncomingProviderBookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: "Rechazar" }));
    expect(updateBooking).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Confirmar rechazo" }));
    expect(updateBooking).toHaveBeenCalledWith(
      { bookingId: "booking-1", status: "CancelledByProvider", reason: "No disponible" },
      expect.any(Object),
    );
  });

  it("requires confirmation before marking a booking as no-show", () => {
    updateBooking.mockClear();
    render(<IncomingProviderBookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: "Marcar inasistencia" }));
    const dialog = screen.getByRole("dialog", { name: "Confirmar inasistencia" });
    expect(dialog).toHaveTextContent("Paseo de prueba");
    expect(updateBooking).not.toHaveBeenCalled();
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(updateBooking).not.toHaveBeenCalled();
  });
});
