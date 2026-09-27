import { useState } from "react";
import { useInfiniteQuery } from "@tanstack/react-query";
import { FileText } from "lucide-react";
import { medicalApi } from "../api/medicalApi";
import { useDownloadCertificatePdf } from "@/features/clinics/hooks/useCertificates";
import { CERTIFICATE_TYPE_LABELS, type CertificateType } from "@/features/clinics/api/certificateApi";

interface Props {
  petId: string;
}

export function ConsolidatedMedicalTimeline({ petId }: Props) {
  const { data, isLoading, isError, hasNextPage, fetchNextPage, isFetchingNextPage } = useInfiniteQuery({
    queryKey: ["medical-timeline", petId],
    queryFn: ({ pageParam }) => medicalApi.getTimeline(petId, pageParam),
    initialPageParam: 1,
    getNextPageParam: (lastPage, pages) => lastPage.hasMore ? pages.length + 1 : undefined,
    retry: false,
  });
  const download = useDownloadCertificatePdf();
  const [downloadError, setDownloadError] = useState(false);
  const events = data?.pages.flatMap((page) => page.items) ?? [];

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
      {isLoading && <p className="mt-3 text-xs text-sand-500">Cargando historial…</p>}
      {isError && (
        <p role="alert" className="mt-3 text-xs text-danger-600">
          No se pudo cargar el historial. Inténtalo de nuevo.
        </p>
      )}
      {downloadError && (
        <p role="alert" className="mt-3 text-xs text-danger-600">
          No se pudo descargar el certificado.
        </p>
      )}
      {!isLoading && !isError && events.length === 0 && (
        <p className="mt-3 text-xs text-sand-500">Aún no hay registros ni certificados.</p>
      )}
      <ol className="mt-3 space-y-0 divide-y divide-sand-100">
        {events.map((event) => (
          <li key={`${event.source}-${event.id}`} className="flex items-start justify-between gap-3 py-2 text-sm">
            <div className="min-w-0">
              <p className="wrap-break-word font-medium text-sand-800">
                {event.source === "Certificate"
                  ? CERTIFICATE_TYPE_LABELS[event.label as CertificateType] ?? event.label
                  : event.label}
              </p>
              <p className="text-xs text-sand-500">
                {event.date} · {event.source === "Certificate" ? "Certificado" : event.kind === "Checkup" ? "Consulta" : "Registro médico"}
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
            {event.source === "Certificate" && event.verificationCode && (
              <button
                type="button"
                disabled={download.isPending}
                onClick={() => void handleDownload(event.id, event.verificationCode!)}
                className="shrink-0 text-xs text-brand-600 hover:underline disabled:opacity-50"
              >
                <FileText className="inline h-4 w-4" aria-hidden="true" /> PDF
              </button>
            )}
          </li>
        ))}
      </ol>
      {hasNextPage && (
        <button type="button" disabled={isFetchingNextPage} onClick={() => void fetchNextPage()}
          className="mt-3 text-xs font-medium text-brand-600 hover:underline disabled:opacity-50">
          {isFetchingNextPage ? "Cargando…" : "Cargar más"}
        </button>
      )}
    </details>
  );
}
