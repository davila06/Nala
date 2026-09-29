import { render } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { BillboardBanner } from "@/features/advertising/components/BillboardBanner";
import { billboardsApi } from "@/features/advertising/api/billboardsApi";
import { trackProductEvent } from "@/shared/lib/telemetry";

vi.mock("@/features/advertising/hooks/useBillboards", () => ({
  useBillboards: () => ({
    data: [
      { id: "billboard-test", title: "Anuncio de prueba", body: null, imageUrl: null, ctaUrl: null, ctaLabel: null },
    ],
  }),
}));
vi.mock("@/features/advertising/api/billboardsApi", () => ({
  billboardsApi: { trackDelivery: vi.fn().mockResolvedValue({}) },
}));
vi.mock("@/shared/lib/telemetry", () => ({ trackProductEvent: vi.fn() }));

describe("BillboardBanner consent", () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.clearAllMocks();
  });

  it("does not persist a visitor identifier or send commercial events before consent", () => {
    render(<BillboardBanner placement="Dashboard" />);
    expect(window.localStorage.getItem("pawtrack:billboard:visitor")).toBeNull();
    expect(billboardsApi.trackDelivery).not.toHaveBeenCalled();
    expect(trackProductEvent).not.toHaveBeenCalled();
  });

  it("can report an impression after analytics consent", () => {
    window.localStorage.setItem("pawtrack_cookie_consent", "accepted");
    render(<BillboardBanner placement="Dashboard" />);
    expect(billboardsApi.trackDelivery).toHaveBeenCalledOnce();
    expect(trackProductEvent).toHaveBeenCalledWith(
      "BillboardImpression",
      expect.objectContaining({ placement: "Dashboard" }),
    );
  });
});
