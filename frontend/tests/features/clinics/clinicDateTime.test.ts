import { describe, expect, it } from "vitest";
import {
  formatCostaRicaDate,
  getClinicDayRange,
  getClinicWeekRange,
  toCostaRicaDateTimeInput,
  toCostaRicaUtc,
} from "@/features/clinics/clinicDateTime";

describe("clinicDateTime", () => {
  it("builds day and week API ranges from Costa Rica midnight", () => {
    expect(getClinicDayRange("2026-09-25")).toEqual({
      from: "2026-09-25T06:00:00.000Z",
      to: "2026-09-26T06:00:00.000Z",
    });
    expect(getClinicWeekRange("2026-09-25")).toEqual({
      from: "2026-09-21T06:00:00.000Z",
      to: "2026-09-28T06:00:00.000Z",
    });
  });

  it("converts clinic datetime-local input using Costa Rica time, not browser time", () => {
    expect(toCostaRicaUtc("2026-09-25T09:30").toISOString()).toBe("2026-09-25T15:30:00.000Z");
    expect(toCostaRicaDateTimeInput("2026-09-25T15:30:00.000Z")).toBe("2026-09-25T09:30");
  });

  it("formats a date-only value without shifting its calendar day", () => {
    expect(formatCostaRicaDate("2026-09-25")).toBe("25/09/2026");
  });
});
