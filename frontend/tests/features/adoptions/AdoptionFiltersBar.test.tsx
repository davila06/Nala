import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AdoptionFiltersBar } from "@/features/adoptions/components/AdoptionFiltersBar";
import { renderWithProviders } from "../../utils/renderWithProviders";

const originalGeolocation = navigator.geolocation;

afterEach(() => {
  Object.defineProperty(navigator, "geolocation", {
    configurable: true,
    value: originalGeolocation,
  });
});

describe("AdoptionFiltersBar", () => {
  it("recovers when the visitor denies location access", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    Object.defineProperty(navigator, "geolocation", {
      configurable: true,
      value: {
        getCurrentPosition: (
          _success: PositionCallback,
          error: PositionErrorCallback,
        ) =>
          error({
            code: 1,
            message: "Denied",
            PERMISSION_DENIED: 1,
            POSITION_UNAVAILABLE: 2,
            TIMEOUT: 3,
          }),
      },
    });

    renderWithProviders(
      <AdoptionFiltersBar filters={{ page: 1 }} onChange={onChange} />,
    );
    await user.click(screen.getByRole("button", { name: "📍 Mi zona" }));

    expect(screen.getByRole("button", { name: "📍 Mi zona" })).toBeEnabled();
    expect(onChange).not.toHaveBeenCalled();
  });
});
