import { describe, expect, it } from "vitest";
import { parseApprovedClaims } from "@/shared/config/claimGovernance";

describe("claimGovernance", () => {
  it("normalizes the configured approved claim list", () => {
    expect([...parseApprovedClaims(" Recovery, enterprise, recovery ")]).toEqual(["recovery", "enterprise"]);
  });

  it("does not approve claims when configuration is absent", () => {
    expect(parseApprovedClaims(undefined).size).toBe(0);
  });
});
