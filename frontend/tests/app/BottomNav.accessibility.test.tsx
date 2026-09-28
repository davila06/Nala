import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { BottomNav } from "@/app/layout/BottomNav";

describe("BottomNav emergency recovery entry", () => {
  it("exposes the lost-pet action to owners on mobile navigation", () => {
    render(
      <MemoryRouter initialEntries={["/dashboard"]}>
        <BottomNav isOwner />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: /reportar mascota perdida/i })).toHaveAttribute(
      "href",
      "/dashboard?action=report-lost",
    );
  });
});
