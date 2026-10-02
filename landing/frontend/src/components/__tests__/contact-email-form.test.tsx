// @vitest-environment jsdom

import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ContactEmailForm } from "../contact-email-form";

beforeEach(() => {
  vi.stubEnv("NEXT_PUBLIC_APP_URL", "http://localhost:5173");
});

afterEach(() => {
  cleanup();
  window.history.replaceState({}, "", "/");
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe("ContactEmailForm", () => {
  it("exposes labeled fields and keeps welfare reports on their dedicated flow", () => {
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

  it("prefills commercial contact details from a selected plan", async () => {
    window.history.replaceState({}, "", "/contact?topic=Consulta+comercial&plan=Plan+Plus&tier=UserPlus");
    render(<ContactEmailForm />);

    await waitFor(() => expect((screen.getByLabelText("Tema") as HTMLSelectElement).value).toBe("Consulta comercial"));
    expect((screen.getByLabelText("Mensaje") as HTMLTextAreaElement).value).toMatch(/Plan Plus \(UserPlus\)/);
    expect((screen.getByLabelText("Mensaje") as HTMLTextAreaElement).value).toMatch(/precio, límites y condiciones/i);
  });

  it("submits the message to PawTrack and announces provider acceptance", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "http://localhost:5199");
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, status: 202 });
    vi.stubGlobal("fetch", fetchMock);
    render(<ContactEmailForm />);

    fireEvent.change(screen.getByLabelText("Tu correo para responderte"), {
      target: { value: "ana@example.cr" },
    });
    fireEvent.change(screen.getByLabelText("Mensaje"), {
      target: { value: "Necesito ayuda para acceder a mi cuenta de PawTrack." },
    });
    fireEvent.click(screen.getByRole("button", { name: /enviar mensaje/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledOnce());
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5199/api/public/contact",
      expect.objectContaining({ method: "POST", headers: { "Content-Type": "application/json" } }),
    );
    const request = fetchMock.mock.calls[0][1] as RequestInit;
    expect(JSON.parse(String(request.body))).toEqual({
      name: "",
      email: "ana@example.cr",
      topic: "Consulta general",
      message: "Necesito ayuda para acceder a mi cuenta de PawTrack.",
      website: "",
    });
    expect((await screen.findByRole("status")).textContent).toMatch(/aceptó el mensaje/i);
    expect(screen.getByLabelText("Mensaje").getAttribute("aria-describedby")).toBe("contact-message-privacy");
  });

  it("shows a retryable error when the API cannot send the message", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "http://localhost:5199");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: false, status: 503 }));
    render(<ContactEmailForm />);

    fireEvent.change(screen.getByLabelText("Tu correo para responderte"), {
      target: { value: "ana@example.cr" },
    });
    fireEvent.change(screen.getByLabelText("Mensaje"), {
      target: { value: "Necesito ayuda para acceder a mi cuenta de PawTrack." },
    });
    fireEvent.click(screen.getByRole("button", { name: /enviar mensaje/i }));

    expect((await screen.findByRole("alert")).textContent).toMatch(/no pudimos enviar/i);
    expect((screen.getByRole("button", { name: /enviar mensaje/i }) as HTMLButtonElement).disabled).toBe(false);
  });

  it("shows field guidance when the API rejects invalid input", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_URL", "http://localhost:5199");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: false, status: 422 }));
    render(<ContactEmailForm />);

    fireEvent.change(screen.getByLabelText("Tu correo para responderte"), {
      target: { value: "ana@example.cr" },
    });
    fireEvent.change(screen.getByLabelText("Mensaje"), {
      target: { value: "Necesito ayuda para acceder a mi cuenta de PawTrack." },
    });
    fireEvent.click(screen.getByRole("button", { name: /enviar mensaje/i }));

    expect((await screen.findByRole("alert")).textContent).toMatch(/revisa el correo y el mensaje/i);
  });
});
