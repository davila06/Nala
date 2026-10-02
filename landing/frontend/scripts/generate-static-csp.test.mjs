import { createHash } from "node:crypto";
import { describe, expect, it } from "vitest";
import {
  createConnectSources,
  createScriptHashes,
} from "./generate-static-csp.mjs";

describe("static export script hashes", () => {
  it("hashes inline scripts and ignores external script sources", () => {
    const html =
      '<script>window.first = 1;</script><script src="/bundle.js"></script><script>window.second = 2;</script>';
    const expected = ["window.first = 1;", "window.second = 2;"].map((script) =>
      createHash("sha256").update(script).digest("base64"),
    );

    expect(createScriptHashes(html)).toEqual(expected.sort());
  });

  it("deduplicates identical inline scripts across pages", () => {
    const html =
      "<script>window.shared = true;</script><script>window.shared = true;</script>";

    expect(createScriptHashes(html)).toHaveLength(1);
  });
});

describe("static export connect sources", () => {
  it("allows only the configured API origin", () => {
    expect(createConnectSources("https://api.pawtrack.cr/anything")).toBe(
      "connect-src 'self' https://api.pawtrack.cr",
    );
  });

  it("rejects insecure non-local API origins", () => {
    expect(() => createConnectSources("http://api.pawtrack.cr")).toThrow(
      /HTTPS/,
    );
  });

  it("allows local HTTP for development", () => {
    expect(createConnectSources("http://localhost:5199")).toBe(
      "connect-src 'self' http://localhost:5199",
    );
  });

  it("requires an API URL for the static build", () => {
    expect(() => createConnectSources("")).toThrow(
      /NEXT_PUBLIC_API_URL must be configured/,
    );
  });
});
