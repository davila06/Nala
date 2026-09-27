import { fireEvent, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ReminderCalendar } from "@/features/medical/components/ReminderCalendar";
import { serviceProvidersApi } from "@/features/service-providers/api/serviceProvidersApi";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/service-providers/api/serviceProvidersApi", () => ({
  serviceProvidersApi: { getCalendarBookings: vi.fn() },
}));

describe("ReminderCalendar", () => {
  it("keeps an in-progress training booking distinct from a requested one", async () => {
    const now = new Date();
    const startsAt = new Date(now.getFullYear(), now.getMonth(), 15, 10).toISOString();
    vi.mocked(serviceProvidersApi.getCalendarBookings).mockResolvedValueOnce([
      {
        id: "booking-training",
        petId: "pet-1",
        serviceName: "Obediencia",
        category: "Trainer",
        startsAt,
        endsAt: new Date(now.getFullYear(), now.getMonth(), 15, 11).toISOString(),
        status: "InProgress",
      },
    ]);

    renderWithProviders(<ReminderCalendar petId="pet-1" reminders={[]} />);
    fireEvent.click(screen.getByRole("button", { name: /^15$/ }));

    expect(await screen.findByText(/Entrenamiento.*Obediencia.*En curso/)).toBeInTheDocument();
  });

  it("shows only the pet's authorized grooming and training bookings on their date", async () => {
    const day = new Date();
    day.setDate(15);
    const startsAt = new Date(day.getFullYear(), day.getMonth(), 15, 10).toISOString();
    vi.mocked(serviceProvidersApi.getCalendarBookings).mockResolvedValueOnce([
      {
        id: "booking-1",
        petId: "pet-1",
        serviceName: "Baño",
        category: "Groomer",
        startsAt,
        endsAt: new Date(day.getFullYear(), day.getMonth(), 15, 11).toISOString(),
        status: "Confirmed",
      },
    ]);

    renderWithProviders(<ReminderCalendar petId="pet-1" reminders={[]} />);
    await waitFor(() =>
      expect(serviceProvidersApi.getCalendarBookings).toHaveBeenCalledWith(
        "pet-1",
        expect.any(String),
        expect.any(String),
        1,
      ),
    );
    fireEvent.click(screen.getByRole("button", { name: /^15$/ }));

    expect(await screen.findByText(/Grooming.*Baño/)).toBeInTheDocument();
    expect(screen.queryByText("Sin recordatorios este día")).not.toBeInTheDocument();
  });
});
