import { createHash } from "node:crypto";
import { describe, expect, it } from "vitest";
import { createScriptHashes } from "./generate-static-csp.mjs";

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
