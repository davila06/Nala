import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ConsolidatedMedicalTimeline } from "@/features/medical/components/ConsolidatedMedicalTimeline";
import { useCertificatesForPet } from "@/features/clinics/hooks/useCertificates";
import { renderWithProviders } from "../../utils/renderWithProviders";
import type { MedicalRecordDto } from "@/features/medical/api/medicalApi";

vi.mock("@/features/clinics/hooks/useCertificates", () => ({
  useCertificatesForPet: vi.fn(),
  useDownloadCertificatePdf: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

describe("ConsolidatedMedicalTimeline", () => {
  it("orders consultations, attached exams and certificates without inferring a diagnosis", () => {
    vi.mocked(useCertificatesForPet).mockReturnValue({
      data: [
        {
          id: "cert-1",
          petId: "pet-1",
          type: "HealthClearance",
          issuedAt: "2026-09-18T12:00:00Z",
          verificationCode: "SAFE-1",
          pdfUrl: "https://example.invalid/cert.pdf",
          isRevoked: false,
          isValid: true,
          clinicId: "clinic-1",
          notes: null,
          validUntil: null,
        },
      ],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useCertificatesForPet>);
    const records = [
      {
        id: "visit-1",
        petId: "pet-1",
        type: "Checkup",
        date: "2026-09-19",
        description: "Control clínico",
        documentUrl: null,
        clinicId: "clinic-1",
        source: "Clinic",
      },
      {
        id: "exam-1",
        petId: "pet-1",
        type: "Other",
        date: "2026-09-17",
        description: "Examen adjunto",
        documentUrl: "https://example.invalid/exam.pdf",
        clinicId: "clinic-1",
        source: "Clinic",
      },
    ] as MedicalRecordDto[];

    renderWithProviders(<ConsolidatedMedicalTimeline petId="pet-1" records={records} />);

    const items = screen.getAllByRole("listitem");
    expect(items[0]).toHaveTextContent("Control clínico");
    expect(items[1]).toHaveTextContent("Certificado de Salud");
    expect(items[2]).toHaveTextContent("Examen adjunto");
    expect(screen.getByRole("link", { name: /documento adjunto/i })).toHaveAttribute(
      "href",
      "https://example.invalid/exam.pdf",
    );
    expect(screen.queryByText(/radiografía|obesidad|diagnóstico automático/i)).not.toBeInTheDocument();
  });
});
