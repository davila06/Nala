import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import CertificateVerificationPage from "@/features/clinics/pages/CertificateVerificationPage";
import { certificateApi } from "@/features/clinics/api/certificateApi";
import { renderWithProviders } from "../../utils/renderWithProviders";

vi.mock("@/features/clinics/api/certificateApi", async () => {
  const actual = await vi.importActual<
    typeof import("@/features/clinics/api/certificateApi")
  >("@/features/clinics/api/certificateApi");
  return {
    ...actual,
    certificateApi: {
      ...actual.certificateApi,
      verify: vi.fn(),
    },
  };
});

const verifyMock = vi.mocked(certificateApi.verify);

function render(code = "ABCD1234") {
  return renderWithProviders(<CertificateVerificationPage />, {
    initialEntries: [`/verificar/${code}`],
    routePath: "/verificar/:code",
  });
}

describe("CertificateVerificationPage", () => {
  it("renders a valid SENASA-ready passport without private PDF or notes", async () => {
    verifyMock.mockResolvedValue({
      id: "cert-1",
      type: "VaccinePassport",
      petName: "Nala",
      petSpecies: "Dog",
      clinicName: "VetSalud",
      verificationCode: "ABCD1234",
      issuedAt: "2026-09-06T00:00:00Z",
      validUntil: "2027-09-06T00:00:00Z",
      isRevoked: false,
      isValid: true,
    });

    render();

    expect(
      await screen.findByText(/certificado válido y auténtico/i),
    ).toBeInTheDocument();
    expect(
      screen.getAllByText(/pasaporte veterinario/i).length,
    ).toBeGreaterThan(0);
    expect(
      screen.queryByText(/descargar certificado pdf/i),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/observaciones/i)).not.toBeInTheDocument();
  });

  it("shows revoked state prominently", async () => {
    verifyMock.mockResolvedValue({
      id: "cert-1",
      type: "VaccinePassport",
      petName: "Nala",
      petSpecies: "Dog",
      clinicName: "VetSalud",
      verificationCode: "ABCD1234",
      issuedAt: "2026-09-06T00:00:00Z",
      validUntil: "2027-09-06T00:00:00Z",
      isRevoked: true,
      isValid: false,
    });

    render();

    expect(
      await screen.findByText(/certificado revocado/i),
    ).toBeInTheDocument();
  });

  it("shows not found when code does not exist", async () => {
    verifyMock.mockResolvedValue(null);

    render("NOPE9999");

    expect(
      await screen.findByText(/certificado no encontrado/i),
    ).toBeInTheDocument();
  });
});
