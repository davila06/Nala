import { act, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { Alert } from "@/shared/ui/Alert";
import { OfflineIndicator } from "@/shared/ui/OfflineIndicator";
import { PageSpinner } from "@/shared/ui/Spinner";

afterEach(() => {
  vi.useRealTimers();
  vi.restoreAllMocks();
});

describe("shared status announcements", () => {
  it("announces errors and loading with appropriate roles", () => {
    render(
      <>
        <Alert variant="error">No se pudo guardar</Alert>
        <PageSpinner />
      </>,
    );
    expect(screen.getByRole("alert")).toHaveTextContent("No se pudo guardar");
    expect(screen.getByRole("status", { name: "Cargando…" })).toBeInTheDocument();
  });

  it("does not hide a new offline warning after reconnecting", () => {
    vi.useFakeTimers();
    const clearTimer = vi.spyOn(window, "clearTimeout");
    render(<OfflineIndicator />);
    act(() => {
      window.dispatchEvent(new Event("offline"));
    });
    expect(screen.getByRole("status")).toHaveTextContent("Sin conexión");

    act(() => {
      window.dispatchEvent(new Event("online"));
    });
    expect(screen.getByRole("status")).toHaveTextContent("Conexión restaurada");
    const reconnectTimer = vi.getTimerCount();
    act(() => {
      window.dispatchEvent(new Event("offline"));
    });
    expect(clearTimer).toHaveBeenCalled();
    expect(vi.getTimerCount()).toBeLessThan(reconnectTimer);
    act(() => {
      vi.advanceTimersByTime(3000);
    });
    expect(screen.getByRole("status")).toHaveTextContent("Sin conexión");
  });
});
