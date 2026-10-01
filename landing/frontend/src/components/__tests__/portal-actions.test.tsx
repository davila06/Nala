// @vitest-environment jsdom

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { ProfileDemoForm } from "../profile-demo-form";
import { ReportDemoForm } from "../report-demo-form";

describe("portal actions", () => {
  beforeEach(() => {
    vi.stubEnv("NEXT_PUBLIC_APP_URL", "https://app.example.test");
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("sends pet profile creation to the product login flow", () => {
    render(<ProfileDemoForm />);

    expect(screen.getByRole("link", { name: /crear cuenta o iniciar sesión en nala/i }).getAttribute("href")).toBe(
      "https://app.example.test/login",
    );
    expect(screen.queryByRole("textbox")).toBeNull();
  });

  it("sends found-pet reports to the existing public product flow", () => {
    render(<ReportDemoForm mode="found" />);

    expect(screen.getByRole("link", { name: /reportar mascota encontrada en nala/i }).getAttribute("href")).toBe(
      "https://app.example.test/encontre-mascota",
    );
    expect(screen.queryByRole("textbox")).toBeNull();
  });

  it("sends lost-pet reports to login without collecting incident details", () => {
    render(<ReportDemoForm mode="lost" />);

    expect(screen.getByRole("link", { name: "Crear cuenta o iniciar sesión" }).getAttribute("href")).toBe(
      "https://app.example.test/login",
    );
    expect(screen.queryByRole("textbox")).toBeNull();
  });
});
