import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ConsolidatedMedicalTimeline } from "@/features/medical/components/ConsolidatedMedicalTimeline";
import { renderWithProviders } from "../../utils/renderWithProviders";
import { medicalApi } from "@/features/medical/api/medicalApi";

vi.mock("@/features/medical/api/medicalApi", () => ({ medicalApi: { getTimeline: vi.fn() } }));

vi.mock("@/features/clinics/hooks/useCertificates", () => ({
  useDownloadCertificatePdf: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

describe("ConsolidatedMedicalTimeline", () => {
  beforeEach(() => vi.clearAllMocks());

  it("offers the consolidated PDF report to authorized timeline users", async () => {
    vi.mocked(medicalApi.getTimeline).mockResolvedValueOnce({ items: [], hasMore: false });
    renderWithProviders(<ConsolidatedMedicalTimeline petId="report-pet" />);

    expect(await screen.findByRole("button", { name: /descargar reporte integral/i })).toBeInTheDocument();
  });
  it("labels radiographs only when the attachment kind was explicitly declared", async () => {
    vi.mocked(medicalApi.getTimeline).mockResolvedValueOnce({ items: [
      { id: "declared", source: "MedicalRecord", date: "2026-09-20", label: "Documento uno",
        kind: "Other", documentUrl: "https://example.invalid/one.pdf", verificationCode: null,
        isRevoked: false, documentKind: "Radiograph" },
      { id: "legacy", source: "MedicalRecord", date: "2026-09-19", label: "Documento dos",
        kind: "Other", documentUrl: "https://example.invalid/two.pdf", verificationCode: null,
        isRevoked: false, documentKind: null },
    ], hasMore: false });

    renderWithProviders(<ConsolidatedMedicalTimeline petId="documents-pet" />);

    expect(await screen.findByText(/Radiografía \(declarada\)/)).toBeInTheDocument();
    expect(screen.getAllByText(/Registro médico/)).toHaveLength(1);
  });

  it("loads all timeline pages on demand instead of truncating at 100 entries", async () => {
    vi.mocked(medicalApi.getTimeline)
      .mockResolvedValueOnce({ items: [
        { id: "visit-1", source: "MedicalRecord", date: "2026-09-19", label: "Control clínico",
          kind: "Checkup", documentUrl: null, verificationCode: null, isRevoked: false },
      ], hasMore: true })
      .mockResolvedValueOnce({ items: [
        { id: "cert-1", source: "Certificate", date: "2026-09-18", label: "HealthClearance",
          kind: "Certificate", documentUrl: null, verificationCode: "SAFE-1", isRevoked: false },
      ], hasMore: false });

    renderWithProviders(<ConsolidatedMedicalTimeline petId="paged-pet" />);

    expect(await screen.findByText("Control clínico")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /cargar más/i }));
    expect(await screen.findByText("Certificado de Salud")).toBeInTheDocument();
    expect(medicalApi.getTimeline).toHaveBeenNthCalledWith(2, "paged-pet", 2);
    expect(screen.queryByRole("button", { name: /cargar más/i })).not.toBeInTheDocument();
  });

  it("orders consultations, attached exams and certificates without inferring a diagnosis", async () => {
    vi.mocked(medicalApi.getTimeline).mockResolvedValueOnce({ items: [
      { id: "visit-1", source: "MedicalRecord", date: "2026-09-19", label: "Control clínico",
        kind: "Checkup", documentUrl: null, verificationCode: null, isRevoked: false },
      { id: "cert-1", source: "Certificate", date: "2026-09-18", label: "HealthClearance",
        kind: "Certificate", documentUrl: null, verificationCode: "SAFE-1", isRevoked: false },
      { id: "exam-1", source: "MedicalRecord", date: "2026-09-17", label: "Examen adjunto",
        kind: "Other", documentUrl: "https://example.invalid/exam.pdf", verificationCode: null, isRevoked: false },
    ], hasMore: false });

    renderWithProviders(<ConsolidatedMedicalTimeline petId="pet-1" />);
    await screen.findByText("Control clínico");
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
