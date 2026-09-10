import { act, fireEvent, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import AuthenticatedLayout from "@/app/layout/AuthenticatedLayout";
import { useAuthStore } from "@/features/auth/store/authStore";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/notifications/components/NotificationBell", () => ({
  NotificationBell: () => null,
}));

vi.mock("@/features/lost-pets/components/OfflineQueueBanner", () => ({
  OfflineQueueBanner: () => null,
}));

vi.mock("@/shared/hooks/useScrollToTop", () => ({
  useScrollToTop: () => undefined,
}));

describe("AuthenticatedLayout", () => {
  it("shows role-specific navigation in the desktop more menu", () => {
    act(() => {
      useAuthStore.getState().setAuth(
        {
          id: "admin-1",
          name: "Admin",
          email: "admin@pawtrack.cr",
          role: "Admin",
          isAdmin: true,
        },
        "admin-token",
      );
    });

    renderWithProviders(<AuthenticatedLayout />, {
      initialEntries: ["/dashboard"],
    });

    expect(screen.getByRole("link", { name: "Adopciones" })).toHaveAttribute(
      "href",
      "/adopciones",
    );

    const moreButton = screen.getByRole("button", { name: "Más opciones" });
    expect(moreButton).toHaveAttribute("aria-expanded", "false");
    expect(
      screen.queryByRole("menuitem", { name: "Administración" }),
    ).not.toBeInTheDocument();

    fireEvent.click(moreButton);

    expect(moreButton).toHaveAttribute("aria-expanded", "true");
    expect(
      screen.getByRole("menuitem", { name: "Administración" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("menuitem", { name: "Estadísticas" }),
    ).toBeInTheDocument();
  });
});
