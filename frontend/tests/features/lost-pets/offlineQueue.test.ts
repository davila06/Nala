import { describe, expect, it } from "vitest";
import { classifyOfflineFailure, MAX_OFFLINE_RETRIES } from "@/shared/lib/offlineQueue";

describe("classifyOfflineFailure", () => {
  it.each([400, 401, 403, 404, 422])("stops automatic retries for HTTP %i", (status) => {
    expect(classifyOfflineFailure(status, 1)).toBe("failed");
  });

  it("marks HTTP 409 as a conflict", () => {
    expect(classifyOfflineFailure(409, 1)).toBe("conflict");
  });

  it.each([undefined, 408, 425, 429, 500, 503])("retries transient status %s", (status) => {
    expect(classifyOfflineFailure(status, 1)).toBe("retry");
  });

  it("requires user intervention after retry exhaustion", () => {
    expect(classifyOfflineFailure(503, MAX_OFFLINE_RETRIES)).toBe("failed");
  });
});
