// @vitest-environment jsdom

import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.hoisted(() => {
  process.env.NEXT_PUBLIC_APP_URL = "https://app.pawtrack.test";
});

import Home from "./page";
import PublicPage from "./[slug]/page";

vi.mock("next/link", async () => {
  const React = await import("react");

  return {
    default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) =>
      React.createElement("a", { ...props, href }, children),
  };
});
vi.mock("@/components/site-chrome", () => ({ SiteHeader: () => null, SiteFooter: () => null }));
vi.mock("@/components/landing-picture", async () => {
  const React = await import("react");

  return {
    LandingPicture: ({ alt, className }: { alt: string; className?: string }) =>
      React.createElement("img", { alt, className }),
  };
});

beforeEach(() => {
  vi.stubEnv("NEXT_PUBLIC_APP_URL", "https://app.pawtrack.test");
});

afterEach(() => {
  cleanup();
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe("home journeys", () => {
  it("sends contact visitors directly to the form", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "contact" }) });
    render(page);

    expect(screen.getByRole("link", { name: /ir al formulario/i }).getAttribute("href")).toBe("#contact-form");
  });

  it("hands the found-pet guide off to PawTrack's real report form", async () => {
    const page = await PublicPage({ params: Promise.resolve({ slug: "found-pets" }) });
    render(page);

    expect(screen.getByRole("link", { name: /continuar en nala/i }).getAttribute("href")).toBe(
      "https://app.pawtrack.test/encontre-mascota",
    );
  });

  it("offers four clear starting paths and keeps welfare reporting separate", () => {
    const { container } = render(<Home />);
    const selector = container.querySelector(".intent-grid");

    expect(selector?.querySelectorAll("[data-journey]")).toHaveLength(4);
    expect(selector?.querySelector('a[href="/lost-pets"]')).toBeTruthy();
    expect(selector?.querySelector('a[href="/found-pets"]')).toBeTruthy();
    expect(selector?.querySelector('a[href="/pet-id"]')).toBeTruthy();
    expect(selector?.querySelector('a[href="/services#provider-onboarding"]')).toBeTruthy();
    expect(
      container.querySelector('.welfare-report-section a[href="https://app.pawtrack.test/bienestar/reportar"]'),
    ).toBeTruthy();
  });

  it("provides a safe animal-welfare reporting path with an emergency caveat", () => {
    const { container } = render(<Home />);
    const welfareSection = container.querySelector(".welfare-report-section");

    expect(screen.getByRole("link", { name: /reportar un caso de bienestar/i }).getAttribute("href")).toBe(
      "https://app.pawtrack.test/bienestar/reportar",
    );
    expect(welfareSection).toBeTruthy();
    expect(welfareSection?.closest(".intent-section")).toBeNull();
    expect(
      within(welfareSection as HTMLElement).getByText("El formulario de bienestar no es un servicio de emergencia."),
    ).toBeTruthy();
    expect(within(welfareSection as HTMLElement).getByText(/peligro inmediato/i)).toBeTruthy();
    expect(
      within(welfareSection as HTMLElement).getByText(/ni sustituye una denuncia ante las autoridades/i),
    ).toBeTruthy();
  });

  it("explains the loss-report prerequisites and offers a registration alternative", () => {
    const { container } = render(<Home />);

    const lostPath = container.querySelector('[data-journey="lost"]');
    expect(within(lostPath as HTMLElement).getByText(/requiere una cuenta y una mascota registrada/i)).toBeTruthy();
    expect(
      within(lostPath as HTMLElement)
        .getByRole("link", { name: /crear una cuenta/i })
        .getAttribute("href"),
    ).toBe("https://app.pawtrack.test/register?return=%2Flost-pets%2Freport");
    expect(screen.getByRole("link", { name: /registra primero a tu mascota/i }).getAttribute("href")).toContain(
      "/login?return=%2Fpets%2Fnew",
    );
  });

  it("opens a clearly fictitious QR profile example without collecting data", () => {
    render(<Home />);
    const disclosure = screen.getByText("Ver perfil de ejemplo");

    fireEvent.click(disclosure);

    expect(screen.getByText("PERFIL FICTICIO")).toBeTruthy();
    expect(screen.getByText(/no se recopilan datos/i)).toBeTruthy();
    expect(screen.getByRole("link", { name: /crear mi perfil en pawtrack/i }).getAttribute("href")).toBe(
      "https://app.pawtrack.test/register",
    );
  });

  it("shows consistent capability definitions and real provider-registration handoff", () => {
    render(<Home />);

    expect(screen.getByText("En el producto:")).toBeTruthy();
    expect(screen.getByText("Parcial", { selector: ".capability-status" })).toBeTruthy();
    expect(screen.getByText("Operación externa:")).toBeTruthy();

    const providerLink = screen.getByRole("link", { name: /registrar servicio en pawtrack/i });
    expect(providerLink.getAttribute("href")).toBe("https://app.pawtrack.test/servicio/registro");
  });

  it("keeps lost and found actions in a visible navigation immediately after the hero", () => {
    const { container } = render(<Home />);

    const hero = container.querySelector("#home-hero");
    const quickActions = screen.getByRole("navigation", { name: "Acciones rápidas de recuperación" });
    const boundary = quickActions.closest(".recovery-sticky-boundary");
    expect(quickActions.hasAttribute("hidden")).toBe(false);
    expect(Boolean((hero?.compareDocumentPosition(quickActions) ?? 0) & Node.DOCUMENT_POSITION_FOLLOWING)).toBe(true);
    expect(boundary?.contains(container.querySelector(".closing-cta"))).toBe(false);
    expect(within(quickActions).getByRole("link", { name: "Perdí" }).getAttribute("href")).toContain(
      "/login?return=%2Flost-pets%2Freport",
    );
    expect(within(quickActions).getByRole("link", { name: "Encontré" }).getAttribute("href")).toBe(
      "https://app.pawtrack.test/encontre-mascota",
    );
  });
});
