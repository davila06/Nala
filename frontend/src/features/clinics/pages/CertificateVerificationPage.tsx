import { useParams, Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { certificateApi, CERTIFICATE_TYPE_LABELS } from "@/features/clinics/api/certificateApi";

function VerificationBadge({ isValid, isRevoked }: { isValid: boolean; isRevoked: boolean }) {
  if (isRevoked) {
    return (
      <div className="flex items-center gap-2 rounded-2xl border-2 border-danger-300 bg-danger-50 px-5 py-3">
        <span className="text-2xl" aria-hidden="true">🚫</span>
        <div>
          <p className="font-bold text-danger-800">Certificado revocado</p>
          <p className="text-xs text-danger-600">Este certificado ha sido anulado por la clínica.</p>
        </div>
      </div>
    );
  }
  if (!isValid) {
    return (
      <div className="flex items-center gap-2 rounded-2xl border-2 border-warn-300 bg-warn-50 px-5 py-3">
        <span className="text-2xl" aria-hidden="true">⏰</span>
        <div>
          <p className="font-bold text-warn-800">Certificado vencido</p>
          <p className="text-xs text-warn-600">La vigencia de este certificado ha expirado.</p>
        </div>
      </div>
    );
  }
  return (
    <div className="flex items-center gap-2 rounded-2xl border-2 border-rescue-300 bg-rescue-50 px-5 py-3">
      <span className="text-2xl" aria-hidden="true">✅</span>
      <div>
        <p className="font-bold text-rescue-800">Certificado válido y auténtico</p>
        <p className="text-xs text-rescue-600">Emitido por una clínica verificada en PawTrack CR.</p>
      </div>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4 border-b border-sand-100 py-2.5 last:border-0">
      <p className="text-xs font-semibold uppercase tracking-[0.15em] text-sand-400 shrink-0">{label}</p>
      <p className="text-sm font-semibold text-sand-900 text-right">{value}</p>
    </div>
  );
}

export default function CertificateVerificationPage() {
  const { code } = useParams<{ code: string }>();

  const { data: cert, isLoading, isError } = useQuery({
    queryKey: ["certificate-verify", code],
    queryFn: () => certificateApi.verify(code ?? ""),
    enabled: !!code,
    retry: false,
    staleTime: 60_000,
  });

  return (
    <div className="min-h-screen bg-surface-warm">
      {/* Header */}
      <header className="border-b border-sand-200 bg-surface px-4 py-4">
        <div className="mx-auto flex max-w-lg items-center justify-between">
          <Link to="/" className="flex items-center gap-2">
            <span className="text-xl font-black text-brand-600">PawTrack</span>
            <span className="rounded-full bg-brand-100 px-2 py-0.5 text-[10px] font-bold uppercase tracking-widest text-brand-700">
              CR
            </span>
          </Link>
          <p className="text-xs text-sand-400">Verificación de certificados</p>
        </div>
      </header>

      <main className="mx-auto max-w-lg px-4 py-10 animate-fade-in-up">
        {/* Loading */}
        {isLoading && (
          <div className="space-y-4">
            <div className="h-16 animate-pulse rounded-2xl bg-sand-100" />
            <div className="h-48 animate-pulse rounded-2xl bg-sand-100" />
          </div>
        )}

        {/* Not found */}
        {!isLoading && (isError || !cert) && (
          <div className="flex flex-col items-center gap-4 rounded-3xl border border-danger-200 bg-danger-50 p-8 text-center">
            <span className="text-4xl" aria-hidden="true">❓</span>
            <h1 className="text-lg font-black text-sand-900">Certificado no encontrado</h1>
            <p className="text-sm text-sand-600">
              El código <strong className="font-mono">{code}</strong> no corresponde a ningún
              certificado emitido en PawTrack CR, o el código es incorrecto.
            </p>
            <Link
              to="/"
              className="rounded-xl border border-sand-200 px-4 py-2 text-sm font-semibold text-sand-700 hover:bg-sand-50 transition-colors"
            >
              Ir al inicio
            </Link>
          </div>
        )}

        {/* Found */}
        {!isLoading && cert && (
          <motion.div
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            className="space-y-5"
          >
            {/* Hero */}
            <div className="text-center">
              <p className="text-xs font-semibold uppercase tracking-[0.4em] text-sand-400">
                Código de verificación
              </p>
              <p className="mt-1 font-mono text-3xl font-black tracking-[0.25em] text-sand-900">
                {cert.verificationCode}
              </p>
            </div>

            {/* Status badge */}
            <VerificationBadge isValid={cert.isValid} isRevoked={cert.isRevoked} />

            {/* Certificate details */}
            <div className="rounded-3xl border border-sand-200 bg-surface px-5 py-4 shadow-sm">
              <h2 className="mb-1 text-base font-bold text-sand-900">
                {CERTIFICATE_TYPE_LABELS[cert.type] ?? cert.type}
              </h2>
              <p className="mb-4 text-xs text-sand-400">Detalles del certificado</p>

              <InfoRow
                label="Tipo"
                value={CERTIFICATE_TYPE_LABELS[cert.type] ?? cert.type}
              />
              <InfoRow
                label="Fecha de emisión"
                value={new Date(cert.issuedAt).toLocaleDateString("es-CR", {
                  day: "numeric",
                  month: "long",
                  year: "numeric",
                })}
              />
              <InfoRow
                label="Válido hasta"
                value={
                  cert.validUntil
                    ? new Date(cert.validUntil).toLocaleDateString("es-CR", {
                        day: "numeric",
                        month: "long",
                        year: "numeric",
                      })
                    : "Sin vencimiento"
                }
              />
              <InfoRow
                label="Estado"
                value={
                  <span
                    className={`rounded-full px-2 py-0.5 text-[10px] font-bold ${
                      cert.isRevoked
                        ? "bg-danger-100 text-danger-700"
                        : cert.isValid
                          ? "bg-rescue-100 text-rescue-800"
                          : "bg-warn-100 text-warn-700"
                    }`}
                  >
                    {cert.isRevoked ? "Revocado" : cert.isValid ? "Vigente" : "Vencido"}
                  </span>
                }
              />
              {cert.notes && (
                <div className="mt-3 rounded-xl bg-surface-warm px-4 py-3">
                  <p className="text-[10px] font-semibold uppercase tracking-[0.2em] text-sand-400">
                    Observaciones
                  </p>
                  <p className="mt-1 text-sm text-sand-700">{cert.notes}</p>
                </div>
              )}
            </div>

            {/* PDF download */}
            {cert.pdfUrl && (
              <a
                href={cert.pdfUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center justify-center gap-2 rounded-2xl border border-trust-200 bg-trust-50 py-3 text-sm font-semibold text-trust-700 hover:bg-trust-100 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400"
              >
                <span aria-hidden="true">📄</span>
                Descargar certificado PDF
              </a>
            )}

            {/* Trust footer */}
            <div className="rounded-2xl border border-sand-100 bg-surface px-4 py-3 text-center">
              <p className="text-[10px] text-sand-400">
                Este certificado fue emitido digitalmente por una clínica verificada en{" "}
                <span className="font-semibold text-brand-600">PawTrack CR</span> —
                plataforma de identidad veterinaria para Costa Rica.
              </p>
            </div>
          </motion.div>
        )}
      </main>
    </div>
  );
}
