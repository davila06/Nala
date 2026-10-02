import { describe, expect, it } from "vitest";
import { buildContactMailto } from "./contact-mailto";

describe("buildContactMailto", () => {
  it("creates an encoded email draft with the visitor's reply address and message", () => {
    const mailto = buildContactMailto({
      name: "Ana Pérez",
      email: "ana@example.cr",
      topic: "Consulta general",
      message: "Quisiera conocer el alcance de PawTrack.",
    });
    const url = new URL(mailto);

    expect(url.protocol).toBe("mailto:");
    expect(url.pathname).toBe("soporte@pawtrack.cr");
    expect(url.searchParams.get("subject")).toBe("Contacto PawTrack CR · Consulta general");
    expect(url.searchParams.get("body")).toContain("ana@example.cr");
    expect(url.searchParams.get("body")).toContain("Quisiera conocer el alcance de PawTrack.");
  });
});
