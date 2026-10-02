// @vitest-environment jsdom

import { render } from "@testing-library/react";
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
});
