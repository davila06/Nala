// @vitest-environment jsdom

import { fireEvent, render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({ usePathname: () => "/" }));

import { SiteFooter, SiteHeader } from "../site-chrome";

beforeEach(() => {
  vi.stubEnv("NEXT_PUBLIC_APP_URL", "http://localhost:5173");
});

afterEach(() => {
  vi.unstubAllEnvs();
});

describe("brand mark", () => {
  it("uses two paw prints in the header and footer marks", () => {
    const { container } = render(
      <>
        <SiteHeader />
        <SiteFooter />
      </>,
    );

    expect(container.querySelectorAll(".brand-mark .brand-pawprint")).toHaveLength(4);
    expect(container.querySelectorAll(".brand-mark")).toHaveLength(2);
  });
});

describe("site navigation", () => {
  it("offers Contact in both desktop and mobile navigation", () => {
    const { container } = render(<SiteHeader />);

    const contactLinks = container.querySelectorAll('.desktop-nav a[href="/contact"], .mobile-menu a[href="/contact"]');

    expect(contactLinks).toHaveLength(2);
  });

  it("offers the found-pet route in both desktop and mobile navigation", () => {
    const { container } = render(<SiteHeader />);

    const foundPetLinks = container.querySelectorAll(
      '.desktop-nav a[href="/found-pets"], .mobile-menu a[href="/found-pets"]',
    );

    expect(foundPetLinks).toHaveLength(2);
  });

  it("keeps a prominent lost-pet report action and PawTrack entry in the header", () => {
    const { container } = render(<SiteHeader />);
    const lostPetAction = container.querySelector<HTMLAnchorElement>(".header-lost-cta");
    const openPawTrack = container.querySelector<HTMLAnchorElement>(".header-cta");

    expect(lostPetAction?.textContent).toMatch(/Perdí una mascota/i);
    expect(lostPetAction?.textContent).toMatch(/Perdí/);
    expect(lostPetAction?.href).toBe("http://localhost:5173/login?return=%2Flost-pets%2Freport");
    expect(openPawTrack?.textContent).toMatch(/Abrir PawTrack/i);
    expect(openPawTrack?.classList.contains("header-cta-primary")).toBe(true);
  });

  it("groups routes into desktop mega menus and mobile sections", () => {
    const { container } = render(<SiteHeader />);
    const desktopNav = container.querySelector(".desktop-nav");
    const mobileNav = container.querySelector("#mobile-navigation");

    expect(desktopNav?.querySelectorAll(".desktop-nav-group")).toHaveLength(3);
    expect(desktopNav?.querySelector('[data-nav-group="resources"]')).toBeTruthy();
    expect(mobileNav?.querySelectorAll(".mobile-nav-group")).toHaveLength(3);
    expect(desktopNav?.querySelector('a[href="/plans"]')).toBeTruthy();
    expect(desktopNav?.querySelector('a[href="/lost-pets"]')).toBeTruthy();
    expect(desktopNav?.querySelector('a[href="/found-pets"]')).toBeTruthy();
    expect(desktopNav?.querySelector('a[href="/clinics"]')).toBeTruthy();
    expect(mobileNav?.querySelector('a[href="/municipalities"]')).toBeTruthy();
    expect(mobileNav?.querySelector('a[href="/contact"]')).toBeTruthy();
  });

  it("groups accordions exclusively and dismisses the mobile drawer after navigation", () => {
    const { container } = render(<SiteHeader />);
    const desktopGroups = container.querySelectorAll(".desktop-nav-group");
    expect(Array.from(desktopGroups).every((group) => group.getAttribute("name") === "desktop-navigation-groups")).toBe(
      true,
    );

    const mobileMenu = container.querySelector<HTMLDetailsElement>(".mobile-menu")!;
    const mobileGroups = container.querySelectorAll<HTMLDetailsElement>(".mobile-nav-group");
    expect(Array.from(mobileGroups).every((group) => group.getAttribute("name") === "mobile-navigation-groups")).toBe(
      true,
    );
    mobileMenu.open = true;
    mobileGroups[1].open = true;

    const clinicLink = mobileGroups[1].querySelector('a[href="/clinics"]')!;
    clinicLink.addEventListener("click", (event) => event.preventDefault(), { once: true });
    fireEvent.click(clinicLink);
    expect(mobileGroups[1].open).toBe(false);
    expect(mobileMenu.open).toBe(false);
  });
});
