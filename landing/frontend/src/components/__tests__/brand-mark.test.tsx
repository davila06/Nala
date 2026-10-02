// @vitest-environment jsdom

import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({ usePathname: () => "/" }));

import { SiteFooter, SiteHeader } from "../site-chrome";

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
