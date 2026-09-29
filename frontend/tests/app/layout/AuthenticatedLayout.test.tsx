import { act, fireEvent, screen, within } from "@testing-library/react";
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
  it.each([
    ["Owner", "Elegir mascota perdida", "/dashboard?action=report-lost"],
    ["Clinic", "Panel Clínica", "/clinica/portal"],
    ["Ally", "Panel Aliado", "/allies/panel"],
    ["Store", "Portal Tienda", "/tienda/portal"],
    ["ServiceProvider", "Portal de servicios", "/servicio/portal"],
    ["Municipality", "Portal Municipal", "/municipalidad/portal"],
  ] as const)("exposes the primary mobile task for %s", (role, label, path) => {
    act(() => {
      useAuthStore
        .getState()
        .setAuth(
          { id: `test-${role}`, name: "Cuenta de prueba", email: "example@example.test", role, isAdmin: false },
          "test-token",
        );
    });
    renderWithProviders(<AuthenticatedLayout />, { initialEntries: ["/dashboard"] });
    const mobileMenu = screen
      .getAllByRole("button", { name: "Menú de usuario" })
      .find((button) => button.classList.contains("md:hidden"));
    expect(mobileMenu).toBeDefined();
    fireEvent.click(mobileMenu!);
    expect(
      within(screen.getByRole("navigation", { name: "Navegación móvil" })).getByRole("link", { name: label }),
    ).toHaveAttribute("href", path);
  });

  it("links a service provider to their portal from the mobile menu", () => {
    act(() => {
      useAuthStore.getState().setAuth(
        {
          id: "provider-1",
          name: "Proveedor",
          email: "provider@example.test",
          role: "ServiceProvider",
          isAdmin: false,
        },
        "provider-token",
      );
    });

    renderWithProviders(<AuthenticatedLayout />, { initialEntries: ["/dashboard"] });
    const mobileMenu = screen
      .getAllByRole("button", { name: "Menú de usuario" })
      .find((button) => button.classList.contains("md:hidden"));
    expect(mobileMenu).toBeDefined();
    fireEvent.click(mobileMenu!);
    expect(
      within(screen.getByRole("navigation", { name: "Navegación móvil" })).getByRole("link", {
        name: "Portal de servicios",
      }),
    ).toHaveAttribute("href", "/servicio/portal");
  });

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

    expect(screen.getByRole("link", { name: "Saltar al contenido" })).toHaveAttribute("href", "#main-content");
    expect(screen.getByRole("main")).toHaveAttribute("id", "main-content");

    fireEvent.keyDown(moreButton, { key: "Escape" });
    expect(moreButton).toHaveAttribute("aria-expanded", "false");
  });
});
