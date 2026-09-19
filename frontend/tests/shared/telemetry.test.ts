import { describe, expect, it, vi } from "vitest";
import { buildProductEvent, trackProductEvent, type ProductEventName } from "@/shared/lib/telemetry";

const { postMock } = vi.hoisted(() => ({
  postMock: vi.fn().mockResolvedValue({ data: { accepted: true } }),
}));

vi.mock("@/shared/lib/apiClient", () => ({
  apiClient: { post: postMock },
}));

describe("buildProductEvent", () => {
  it.each<ProductEventName>([
    "PetRegistered",
    "PetProfileCompleted",
    "QrGenerated",
    "QrScanned",
    "LostPetReported",
    "SightingCreated",
    "FirstResponseRecorded",
    "HandoverStarted",
    "HandoverCompleted",
    "PetReunited",
  ])("builds a versioned envelope for %s", (eventName) => {
    const event = buildProductEvent(
      eventName,
      {
        source: "test",
        petId: "pet-1",
      },
      "anonymous-1",
    );

    expect(event).toMatchObject({
      eventName,
      schemaVersion: "1",
      anonymousId: "anonymous-1",
      source: "test",
      petId: "pet-1",
    });
    expect(event.eventId).toMatch(/^[0-9a-f-]{36}$/i);
    expect(Number.isNaN(Date.parse(event.occurredAt))).toBe(false);
  });

  it("posts recovery completion to the product analytics endpoint", () => {
    trackProductEvent("PetReunited", { source: "handover", petId: "pet-1" });

    expect(postMock).toHaveBeenCalledWith(
      "/product-events",
      expect.objectContaining({
        eventName: "PetReunited",
        source: "handover",
        petId: "pet-1",
      }),
    );
  });
});
