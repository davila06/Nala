// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LandingPicture } from "../landing-picture";

describe("LandingPicture", () => {
  it("provides mobile AVIF and WebP sources with an accessible desktop fallback", () => {
    const { container } = render(
      <LandingPicture alt="Familia reunida con su perro en un parque" asset="hero" className="hero-photo" priority />,
    );

    expect(screen.getByRole("img", { name: "Familia reunida con su perro en un parque" })).toBeTruthy();
    expect(container.querySelector("img")?.getAttribute("loading")).toBe("eager");
    expect(container.querySelector("img")?.getAttribute("fetchpriority")).toBe("high");
    expect(
      container.querySelector('source[media="(max-width: 700px)"][type="image/avif"]')?.getAttribute("srcSet"),
    ).toContain("nala-hero-family-reunion-20261001-mobile.avif");
    expect(
      container.querySelector('source[media="(max-width: 700px)"][type="image/webp"]')?.getAttribute("srcSet"),
    ).toContain("nala-hero-family-reunion-20261001-mobile.webp");
    expect(container.querySelector("img")?.getAttribute("src")).toContain(
      "nala-hero-family-reunion-20261001-desktop.webp",
    );
  });

  it("selects the QR dogtag artwork for the digital journey", () => {
    const { container } = render(<LandingPicture alt="Dogtag con código QR demostrativo" asset="dogtag" />);

    expect(screen.getByRole("img", { name: "Dogtag con código QR demostrativo" })).toBeTruthy();
    expect(
      container.querySelector('source[media="(max-width: 700px)"][type="image/avif"]')?.getAttribute("srcSet"),
    ).toContain("nala-qr-scan-identity-tagged-20261001-mobile.avif");
  });
});
