import { describe, expect, it } from "vitest";
import { isPositionStale } from "@/features/pets/lib/collarFreshness";

describe("isPositionStale", () => {
  it("returns false when there is no position yet", () => {
    expect(isPositionStale(null, 120)).toBe(false);
  });

  it("returns false when the position age is within the offline threshold", () => {
    expect(isPositionStale(60 * 60, 120)).toBe(false);
  });

  it("returns true when the position age exceeds the offline threshold", () => {
    expect(isPositionStale(3 * 60 * 60, 120)).toBe(true);
  });

  it("treats the exact threshold boundary as not yet stale", () => {
    expect(isPositionStale(120 * 60, 120)).toBe(false);
  });
});
