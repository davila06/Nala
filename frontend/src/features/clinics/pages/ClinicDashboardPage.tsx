import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  clinicsApi,
  type ClinicScanResultDto,
  type ScanInputType,
} from "../api/clinicsApi";
import { ScanInput } from "../components/ScanInput";
import { MatchResultCard } from "../components/MatchResultCard";
import { ClinicTiersModal } from "../components/ClinicTiersModal";
import { CertificateIssueModal } from "../components/CertificateIssueModal";
import {
  useCertificatesForClinic,
} from "../hooks/useCertificates";
import { CERTIFICATE_TYPE_LABELS } from "../api/certificateApi";

export default function ClinicDashboardPage() {
  const [scanResult, setScanResult] = useState<ClinicScanResultDto | null>(
    null,
  );
  const [showTiers, setShowTiers] = useState(false);
  const [showCertificate, setShowCertificate] = useState(false);

  const { data: clinic, isLoading: clinicLoading } = useQuery({
    queryKey: ["my-clinic"],
    queryFn: () => clinicsApi.getMyClinic(),
  });

  const {
    mutate: performScan,
    isPending: scanning,
    error: scanError,
  } = useMutation({
    mutationFn: ({
      input,
      inputType,
    }: {
      input: string;
      inputType: ScanInputType;
    }) => clinicsApi.scan(input, inputType),
    onSuccess: (data) => setScanResult(data),
  });

  function handleScan(value: string, type: "Qr" | "RfidChip") {
    setScanResult(null);
    performScan({ input: value, inputType: type });
  }

  function handleReset() {
    setScanResult(null);
  }

  if (clinicLoading) {
    return (
      <div className="min-h-screen bg-surface-warm">
        <div className="border-b border-sand-200 field-input px-4 py-4">
          <div className="mx-auto max-w-lg">
            <div className="flex items-start justify-between">
              <div className="space-y-2">
                <div className="h-5 w-48 animate-pulse rounded-lg bg-sand-200" />
                <div className="h-3.5 w-32 animate-pulse rounded-lg bg-sand-100" />
              </div>
              <div className="h-6 w-16 animate-pulse rounded-full bg-sand-200" />
            </div>
          </div>
        </div>
        <div className="mx-auto max-w-lg space-y-4 px-4 py-6">
          <div className="h-5 w-36 animate-pulse rounded-lg bg-sand-200" />
          <div className="h-4 w-64 animate-pulse rounded-lg bg-sand-100" />
          <div className="h-24 animate-pulse rounded-2xl bg-sand-100" />
        </div>
      </div>
    );
  }

  // ── Suspended/Pending guard ───────────────────────────────────────────────

  if (clinic && clinic.status !== "Active") {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center bg-surface-warm px-4">
        <p className="text-4xl">🔒</p>
        <h1 className="mt-3 text-lg font-extrabold text-sand-900">
          {clinic.status === "Pending"
            ? "Cuenta pendiente de activación"
            : "Cuenta suspendida"}
        </h1>
        <p className="mt-2 max-w-xs text-center text-sm text-sand-500">
          {clinic.status === "Pending"
            ? "Tu clínica está en revisión. PawTrack activará tu cuenta en 1-2 días hábiles."
            : "Tu cuenta ha sido suspendida. Contacta al equipo de PawTrack para más información."}
        </p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-surface-warm">
      {/* ── Header ── */}
      <header className="border-b border-sand-200 field-input px-4 py-4">
        <div className="mx-auto max-w-lg">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-lg font-extrabold text-sand-900">
                🏥 {clinic?.name ?? "Portal veterinaria"}
              </h1>
              {clinic && (
                <p className="text-xs text-sand-400">
                  Licencia SENASA: {clinic.licenseNumber}
                </p>
              )}
            </div>
            <span className="rounded-full bg-rescue-100 px-2.5 py-0.5 text-xs font-semibold text-rescue-700">
              Activa
            </span>
          </div>
        </div>
      </header>

      {/* ── Main ── */}
      <main className="mx-auto max-w-lg animate-fade-in-up px-4 py-6 space-y-6">
        {/* ── Tier upgrade banner ───────────────────────────────────── */}
        <button
          type="button"
          onClick={() => setShowTiers(true)}
          className="w-full rounded-2xl border border-trust-200 bg-linear-to-r from-trust-50 to-brand-50 px-4 py-3 flex items-center gap-3 text-left hover:from-trust-100 hover:to-brand-100 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400"
        >
          <span className="text-2xl shrink-0" aria-hidden="true">
            ⭐
          </span>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold text-trust-900">
              Plan Afiliada básica
            </p>
            <p className="text-xs text-trust-600 mt-0.5">
              Actualiza a <strong>Plus (₡15,000/mes)</strong> para posición
              destacada, badge verificado y estadísticas.
            </p>
          </div>
          <span className="shrink-0 rounded-xl bg-trust-600 px-3 py-1.5 text-xs font-bold text-white">
            Ver planes →
          </span>
        </button>

        {showTiers && (
          <ClinicTiersModal
            currentTier="basic"
            onClose={() => setShowTiers(false)}
          />
        )}

        {/* ── Partner: certificate issuing ──────────────────────────── */}
        {/* Shown only when clinic has Partner tier; gated by backend on issue */}
        <button
          type="button"
          onClick={() => setShowCertificate(true)}
          className="w-full rounded-2xl border border-brand-200 bg-linear-to-r from-brand-50 to-trust-50 px-4 py-3 flex items-center gap-3 text-left hover:from-brand-100 hover:to-trust-100 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          <span className="text-2xl shrink-0" aria-hidden="true">
            📄
          </span>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold text-brand-900">
              Emitir certificado veterinario
            </p>
            <p className="text-xs text-brand-600 mt-0.5">
              PDF con firma digital y código QR de verificación. Tier Partner.
            </p>
          </div>
          <span className="shrink-0 rounded-xl bg-brand-600 px-3 py-1.5 text-xs font-bold text-white">
            Nuevo →
          </span>
        </button>

        {showCertificate && clinic && (
          <CertificateIssueModal
            clinicId={clinic.id}
            clinicName={clinic.name}
            clinicLicense={clinic.licenseNumber}
            onClose={() => setShowCertificate(false)}
          />
        )}

        {/* ── Certificate history (Partner tier) ───────────────────── */}
        {clinic && <ClinicCertificateHistory clinicId={clinic.id} />}

        {scanResult ? (
          <MatchResultCard result={scanResult} onReset={handleReset} />
        ) : (
          <>
            <div>
              <h2 className="text-base font-bold text-sand-800">
                Escanear mascota
              </h2>
              <p className="text-sm text-sand-500">
                Escanea el código QR del collar o ingresa el número de microchip
                RFID.
              </p>
            </div>

            <ScanInput onScan={handleScan} isLoading={scanning} />

            {scanError && (
              <p className="rounded-xl bg-danger-50 px-4 py-3 text-sm text-danger-600">
                {scanError instanceof Error
                  ? scanError.message
                  : "Error al procesar el escaneo. Intenta de nuevo."}
              </p>
            )}
          </>
        )}
      </main>
    </div>
  );
}

// ── Certificate history list ───────────────────────────────────────────────────

function ClinicCertificateHistory({ clinicId }: { clinicId: string }) {
  const { data: certs, isLoading } = useCertificatesForClinic(clinicId);

  if (isLoading) return null;
  if (!certs || certs.length === 0) return null;

  return (
    <section className="space-y-3">
      <h2 className="text-sm font-bold text-sand-700">Certificados emitidos</h2>
      <ul className="space-y-2">
        {certs.map((cert) => (
          <li
            key={cert.id}
            className="flex items-center justify-between gap-3 rounded-2xl border border-sand-100 bg-surface px-4 py-3"
          >
            <div className="min-w-0">
              <p className="text-sm font-semibold text-sand-900">
                {CERTIFICATE_TYPE_LABELS[cert.type] ?? cert.type}
              </p>
              <p className="text-[11px] text-sand-400">
                {new Date(cert.issuedAt).toLocaleDateString("es-CR")} ·{" "}
                <span className="font-mono">{cert.verificationCode}</span>
              </p>
            </div>
            <div className="flex shrink-0 items-center gap-2">
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
              {cert.pdfUrl && (
                <a
                  href={cert.pdfUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-[10px] font-semibold text-trust-600 hover:underline"
                >
                  PDF
                </a>
              )}
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
