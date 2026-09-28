import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ConsolidatedMedicalTimeline } from "@/features/medical/components/ConsolidatedMedicalTimeline";
import { renderWithProviders } from "../../utils/renderWithProviders";
import { medicalApi } from "@/features/medical/api/medicalApi";

vi.mock("@/features/medical/api/medicalApi", () => ({
  medicalApi: {
    getTimeline: vi.fn(),
    downloadMedicalDocument: vi.fn(),
    downloadConsolidatedReport: vi.fn(),
    requestHealthReportExport: vi.fn(),
    getHealthReportExport: vi.fn(),
    downloadHealthReportExport: vi.fn(),
  },
}));

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

  it("requests, polls and exposes download for a long-running report", async () => {
    vi.mocked(medicalApi.getTimeline).mockResolvedValueOnce({ items: [], hasMore: false });
    vi.mocked(medicalApi.requestHealthReportExport).mockResolvedValueOnce({
      id: "export-1",
      petId: "report-pet",
      status: "Queued",
      requestedAt: "2026-09-28T12:00:00Z",
      completedAt: null,
      expiresAt: "2026-09-29T12:00:00Z",
      itemCount: null,
      errorCode: null,
    });
    vi.mocked(medicalApi.getHealthReportExport).mockResolvedValue({
      id: "export-1",
      petId: "report-pet",
      status: "Completed",
      requestedAt: "2026-09-28T12:00:00Z",
      completedAt: "2026-09-28T12:00:10Z",
      expiresAt: "2026-09-29T12:00:00Z",
      itemCount: 6001,
      errorCode: null,
    });
    vi.mocked(medicalApi.downloadHealthReportExport).mockResolvedValueOnce(new Blob(["pdf"]));
    Object.defineProperty(URL, "createObjectURL", { configurable: true, value: vi.fn(() => "blob:report") });
    Object.defineProperty(URL, "revokeObjectURL", { configurable: true, value: vi.fn() });
    vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => undefined);
    renderWithProviders(<ConsolidatedMedicalTimeline petId="report-pet" />);

    fireEvent.click(await screen.findByRole("button", { name: /solicitar reporte en segundo plano/i }));
    expect(await screen.findByText(/Reporte listo · 6001 eventos/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /descargar reporte listo/i }));
    await waitFor(() => expect(medicalApi.downloadHealthReportExport).toHaveBeenCalledWith("report-pet", "export-1"));
  });

  it("does not offer an unavailable certificate PDF", async () => {
    vi.mocked(medicalApi.getTimeline).mockResolvedValueOnce({
      items: [
        {
          id: "cert-no-pdf",
          source: "Certificate",
          date: "2026-09-19",
          label: "HealthClearance",
          kind: "Certificate",
          documentUrl: null,
          verificationCode: "SAFE-2",
          isRevoked: false,
          hasPdf: false,
        },
      ],
      hasMore: false,
    });
    renderWithProviders(<ConsolidatedMedicalTimeline petId="cert-pet" />);

    expect(await screen.findByText("Certificado de Salud")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^pdf$/i })).not.toBeInTheDocument();
  });
  it("labels radiographs only when the attachment kind was explicitly declared", async () => {
    vi.mocked(medicalApi.getTimeline).mockResolvedValueOnce({
      items: [
        {
          id: "declared",
          source: "MedicalRecord",
          date: "2026-09-20",
          label: "Documento uno",
          kind: "Other",
          documentUrl: null,
          hasDocument: true,
          verificationCode: null,
          isRevoked: false,
          documentKind: "Radiograph",
        },
        {
          id: "legacy",
          source: "MedicalRecord",
          date: "2026-09-19",
          label: "Documento dos",
          kind: "Other",
          documentUrl: null,
          hasDocument: true,
          verificationCode: null,
          isRevoked: false,
          documentKind: null,
        },
      ],
      hasMore: false,
    });

    renderWithProviders(<ConsolidatedMedicalTimeline petId="documents-pet" />);

    expect(await screen.findByText(/Radiografía \(declarada\)/)).toBeInTheDocument();
    expect(screen.getAllByText(/Registro médico/)).toHaveLength(1);
  });

  it("loads all timeline pages on demand instead of truncating at 100 entries", async () => {
    vi.mocked(medicalApi.getTimeline)
      .mockResolvedValueOnce({
        items: [
          {
            id: "visit-1",
            source: "MedicalRecord",
            date: "2026-09-19",
            label: "Control clínico",
            kind: "Checkup",
            documentUrl: null,
            verificationCode: null,
            isRevoked: false,
          },
        ],
        hasMore: true,
      })
      .mockResolvedValueOnce({
        items: [
          {
            id: "cert-1",
            source: "Certificate",
            date: "2026-09-18",
            label: "HealthClearance",
            kind: "Certificate",
            documentUrl: null,
            verificationCode: "SAFE-1",
            isRevoked: false,
          },
        ],
        hasMore: false,
      });

    renderWithProviders(<ConsolidatedMedicalTimeline petId="paged-pet" />);

    expect(await screen.findByText("Control clínico")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /cargar más/i }));
    expect(await screen.findByText("Certificado de Salud")).toBeInTheDocument();
    expect(medicalApi.getTimeline).toHaveBeenNthCalledWith(2, "paged-pet", 2);
    expect(screen.queryByRole("button", { name: /cargar más/i })).not.toBeInTheDocument();
  });

  it("orders consultations, attached exams and certificates without inferring a diagnosis", async () => {
    vi.mocked(medicalApi.getTimeline).mockResolvedValueOnce({
      items: [
        {
          id: "visit-1",
          source: "MedicalRecord",
          date: "2026-09-19",
          label: "Control clínico",
          kind: "Checkup",
          documentUrl: null,
          verificationCode: null,
          isRevoked: false,
        },
        {
          id: "cert-1",
          source: "Certificate",
          date: "2026-09-18",
          label: "HealthClearance",
          kind: "Certificate",
          documentUrl: null,
          verificationCode: "SAFE-1",
          isRevoked: false,
        },
        {
          id: "exam-1",
          source: "MedicalRecord",
          date: "2026-09-17",
          label: "Examen adjunto",
          kind: "Other",
          documentUrl: null,
          hasDocument: true,
          verificationCode: null,
          isRevoked: false,
        },
      ],
      hasMore: false,
    });

    renderWithProviders(<ConsolidatedMedicalTimeline petId="pet-1" />);
    await screen.findByText("Control clínico");
    const items = screen.getAllByRole("listitem");
    expect(items[0]).toHaveTextContent("Control clínico");
    expect(items[1]).toHaveTextContent("Certificado de Salud");
    expect(items[2]).toHaveTextContent("Examen adjunto");
    expect(screen.getByRole("button", { name: /descargar documento: examen adjunto/i })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /documento adjunto/i })).not.toBeInTheDocument();
    expect(screen.queryByText(/radiografía|obesidad|diagnóstico automático/i)).not.toBeInTheDocument();
  });
});
