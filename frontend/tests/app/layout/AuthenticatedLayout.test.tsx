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
  it("shows four primary modes and role-specific actions in the more menu", () => {
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

    expect(
      screen.getAllByRole("link", { name: "Mascota" }).every((link) => link.getAttribute("href") === "/dashboard"),
    ).toBe(true);
    expect(
      screen.getAllByRole("link", { name: "Encontrar" }).every((link) => link.getAttribute("href") === "/map"),
    ).toBe(true);
    expect(screen.getAllByRole("link", { name: "Salud" }).every((link) => link.getAttribute("href") === "/salud")).toBe(
      true,
    );
    expect(screen.getAllByRole("link", { name: "Red" }).every((link) => link.getAttribute("href") === "/red")).toBe(
      true,
    );
    expect(screen.queryByRole("link", { name: "Adopciones" })).not.toBeInTheDocument();

    const moreButton = screen.getByRole("button", { name: "Más opciones" });
    expect(moreButton).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByRole("menuitem", { name: "Administración" })).not.toBeInTheDocument();

    fireEvent.click(moreButton);

    expect(moreButton).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("menuitem", { name: "Administración" })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Estadísticas" })).toBeInTheDocument();
  });
});
