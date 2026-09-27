import { useState } from "react";
import { FileText } from "lucide-react";
import type { MedicalRecordDto } from "../api/medicalApi";
import { useCertificatesForPet, useDownloadCertificatePdf } from "@/features/clinics/hooks/useCertificates";
import { CERTIFICATE_TYPE_LABELS } from "@/features/clinics/api/certificateApi";

interface Props {
  petId: string;
  records: MedicalRecordDto[];
}

export function ConsolidatedMedicalTimeline({ petId, records }: Props) {
  const { data: certificates = [], isLoading, isError } = useCertificatesForPet(petId);
  const download = useDownloadCertificatePdf();
  const [downloadError, setDownloadError] = useState(false);

  const events = [
    ...records.map((record) => ({
      id: `record-${record.id}`,
      date: record.date,
      label: record.description,
      kind: record.type === "Checkup" ? "Consulta" : record.type === "Other" ? "Registro médico" : record.type,
      documentUrl: record.documentUrl,
      certificateId: null as string | null,
      verificationCode: null as string | null,
      isRevoked: false,
    })),
    ...certificates.map((certificate) => ({
      id: `certificate-${certificate.id}`,
      date: certificate.issuedAt.slice(0, 10),
      label: CERTIFICATE_TYPE_LABELS[certificate.type],
      kind: "Certificado",
      documentUrl: null as string | null,
      certificateId: certificate.pdfUrl ? certificate.id : null,
      verificationCode: certificate.verificationCode,
      isRevoked: certificate.isRevoked,
    })),
  ].sort((left, right) => right.date.localeCompare(left.date) || left.id.localeCompare(right.id));

  const handleDownload = async (id: string, verificationCode: string) => {
    setDownloadError(false);
    try {
      const blob = await download.mutateAsync(id);
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `pawtrack-certificate-${verificationCode}.pdf`;
      link.click();
      URL.revokeObjectURL(url);
    } catch {
      setDownloadError(true);
    }
  };

  return (
    <details className="border-y border-sand-200 py-3">
      <summary className="cursor-pointer text-sm font-semibold text-sand-800">Historial consolidado</summary>
      {isLoading && <p className="mt-3 text-xs text-sand-500">Cargando certificados…</p>}
      {isError && (
        <p role="alert" className="mt-3 text-xs text-danger-600">
          No se pudieron cargar los certificados. El historial puede estar incompleto.
        </p>
      )}
      {downloadError && (
        <p role="alert" className="mt-3 text-xs text-danger-600">
          No se pudo descargar el certificado.
        </p>
      )}
      {!isLoading && events.length === 0 && (
        <p className="mt-3 text-xs text-sand-500">Aún no hay registros ni certificados.</p>
      )}
      <ol className="mt-3 space-y-0 divide-y divide-sand-100">
        {events.slice(0, 100).map((event) => (
          <li key={event.id} className="flex items-start justify-between gap-3 py-2 text-sm">
            <div className="min-w-0">
              <p className="wrap-break-word font-medium text-sand-800">{event.label}</p>
              <p className="text-xs text-sand-500">
                {event.date} · {event.kind}
                {event.isRevoked ? " · Revocado" : ""}
              </p>
            </div>
            {event.documentUrl && (
              <a
                href={event.documentUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="shrink-0 text-xs text-brand-600 hover:underline"
                aria-label={`Documento adjunto: ${event.label}`}
              >
                <FileText className="inline h-4 w-4" aria-hidden="true" /> Documento adjunto
              </a>
            )}
            {event.certificateId && event.verificationCode && (
              <button
                type="button"
                disabled={download.isPending}
                onClick={() => void handleDownload(event.certificateId!, event.verificationCode!)}
                className="shrink-0 text-xs text-brand-600 hover:underline disabled:opacity-50"
              >
                <FileText className="inline h-4 w-4" aria-hidden="true" /> PDF
              </button>
            )}
          </li>
        ))}
      </ol>
      {events.length > 100 && (
        <p className="mt-2 text-xs text-sand-600">
          Se muestran los 100 eventos más recientes. Consulta el expediente para ver los demás.
        </p>
      )}
    </details>
  );
}
