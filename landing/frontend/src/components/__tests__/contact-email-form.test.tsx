// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ContactEmailForm } from "../contact-email-form";

describe("ContactEmailForm", () => {
  it("exposes labeled email fields and keeps welfare reports on their dedicated flow", () => {
    vi.stubEnv("NEXT_PUBLIC_APP_URL", "http://localhost:5173");
    render(<ContactEmailForm />);

    expect(screen.getByLabelText("Tu nombre (opcional)")).toBeTruthy();
    expect(screen.getByLabelText("Tu correo para responderte")).toBeTruthy();
    expect(screen.getByLabelText("Tema")).toBeTruthy();
    expect(screen.getByLabelText("Mensaje")).toBeTruthy();
    expect(screen.getByRole("link", { name: /reportar maltrato o un animal en riesgo/i }).getAttribute("href")).toBe(
      "http://localhost:5173/bienestar/reportar",
    );
  });
});
