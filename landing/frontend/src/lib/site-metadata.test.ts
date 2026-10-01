import { afterEach, describe, expect, it, vi } from "vitest";
import robots from "../app/robots";
import sitemap from "../app/sitemap";

describe("public metadata configuration", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("fails sitemap generation clearly when the canonical site URL is missing", () => {
    vi.stubEnv("NEXT_PUBLIC_SITE_URL", "");

    expect(() => sitemap()).toThrow(/NEXT_PUBLIC_SITE_URL/);
  });

  it("fails robots generation clearly when the canonical site URL is missing", () => {
    vi.stubEnv("NEXT_PUBLIC_SITE_URL", "");

    expect(() => robots()).toThrow(/NEXT_PUBLIC_SITE_URL/);
  });

  it("publishes absolute canonical URLs in sitemap and robots", () => {
    vi.stubEnv("NEXT_PUBLIC_SITE_URL", "https://www.nala.example");

    expect(
      sitemap().find((entry) => entry.url === "https://www.nala.example/"),
    ).toBeDefined();
    expect(
      sitemap().find(
        (entry) => entry.url === "https://www.nala.example/about/",
      ),
    ).toBeDefined();
    expect(robots().sitemap).toBe("https://www.nala.example/sitemap.xml");
  });
});
