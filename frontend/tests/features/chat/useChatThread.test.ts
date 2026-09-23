import { describe, expect, it } from "vitest";
import { getChatMessagesRefetchInterval } from "@/features/chat/hooks/useChatThread";

describe("getChatMessagesRefetchInterval", () => {
  it("disables fallback polling while SignalR is connected", () => {
    expect(getChatMessagesRefetchInterval("connected")).toBe(false);
  });

  it("keeps a bounded fallback while realtime transport is unavailable", () => {
    expect(getChatMessagesRefetchInterval("disconnected")).toBe(10_000);
    expect(getChatMessagesRefetchInterval("reconnecting")).toBe(10_000);
  });
});
