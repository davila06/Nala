import { useRef, useState } from "react";
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
import { useCertificatesForClinic } from "../hooks/useCertificates";
import {
  useClinicScanStats,
  useUploadClinicLogo,
  useClinicApiKeys,
  useCreateClinicApiKey,
  useRevokeClinicApiKey,
  useClinicNearbyAlerts,
} from "../hooks/useClinics";
import { CERTIFICATE_TYPE_LABELS } from "../api/certificateApi";
import { toast } from "@/shared/lib/toast";

export default function ClinicDashboardPage() {
  const [scanResult, setScanResult] = useState<ClinicScanResultDto | null>(
    null,
  );
  const [showTiers, setShowTiers] = useState(false);
  const [showCertificate, setShowCertificate] = useState(false);
  const [activeSection, setActiveSection] = useState<
    "scan" | "stats" | "api" | "alerts"
  >("scan");
  const logoInputRef = useRef<HTMLInputElement>(null);

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

  const { mutateAsync: uploadLogo, isPending: uploadingLogo } =
    useUploadClinicLogo();

  function handleScan(value: string, type: "Qr" | "RfidChip") {
    setScanResult(null);
    performScan({ input: value, inputType: type });
  }

  function handleReset() {
    setScanResult(null);
  }

  async function handleLogoChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      await uploadLogo(file);
      toast.success("Logo actualizado.");
    } catch {
      toast.error("No se pudo subir el logo. Intenta de nuevo.");
    }
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
            <div className="flex items-center gap-3">
              {/* Logo / avatar */}
              <button
                type="button"
                title="Subir logo de clínica"
                disabled={uploadingLogo}
                onClick={() => logoInputRef.current?.click()}
                className="relative h-12 w-12 shrink-0 overflow-hidden rounded-full border-2 border-sand-200 bg-sand-100 flex items-center justify-center text-xl hover:opacity-80 transition-opacity"
              >
                {clinic?.logoUrl ? (
                  <img
                    src={clinic.logoUrl}
                    alt="Logo"
                    className="h-full w-full object-cover"
                  />
                ) : (
                  <span aria-hidden="true">🏥</span>
                )}
                {uploadingLogo && (
                  <span className="absolute inset-0 flex items-center justify-center bg-white/60">
                    <span className="h-4 w-4 animate-spin rounded-full border-2 border-brand-400 border-t-transparent" />
                  </span>
                )}
              </button>
              <input
                ref={logoInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="sr-only"
                onChange={(e) => void handleLogoChange(e)}
              />
              <div>
                <h1 className="text-lg font-extrabold text-sand-900">
                  {clinic?.name ?? "Portal veterinaria"}
                </h1>
                {clinic && (
                  <p className="text-xs text-sand-400">
                    Licencia SENASA: {clinic.licenseNumber}
                  </p>
                )}
              </div>
            </div>
            <span className="rounded-full bg-rescue-100 px-2.5 py-0.5 text-xs font-semibold text-rescue-700">
              Activa
            </span>
          </div>
        </div>
      </header>

      {/* ── Main ── */}
      <main className="mx-auto max-w-lg animate-fade-in-up px-4 py-6 space-y-6">
        {/* ── Section tabs ─────────────────────────────────────────── */}
        <div className="flex gap-1 rounded-2xl bg-surface-warm p-1.5">
          {(["scan", "stats", "api", "alerts"] as const).map((s) => (
            <button
              key={s}
              type="button"
              onClick={() => setActiveSection(s)}
              className={[
                "flex-1 rounded-xl py-2 text-xs font-bold transition-colors",
                activeSection === s
                  ? "bg-surface text-sand-900 shadow-sm"
                  : "text-sand-500 hover:text-sand-700",
              ].join(" ")}
            >
              {s === "scan"
                ? "🔍 Escanear"
                : s === "stats"
                  ? "📊 Stats"
                  : s === "api"
                    ? "🔑 API"
                    : "🚨 Alertas"}
            </button>
          ))}
        </div>
        {activeSection === "scan" && (
          <>
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
                  PDF con firma digital y código QR de verificación. Tier
                  Partner.
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
                    Escanea el código QR del collar o ingresa el número de
                    microchip RFID.
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
          </>
        )}

        {activeSection === "stats" && <ClinicStatsSection />}

        {activeSection === "api" && clinic && (
          <ClinicApiKeysSection clinicId={clinic.id} />
        )}

        {activeSection === "alerts" && clinic && <ClinicNearbyAlertsSection />}
      </main>
    </div>
  );
}

// ── Nearby active alerts section (Partner) ───────────────────────────────────

function ClinicNearbyAlertsSection() {
  const { data: alerts, isLoading, isError } = useClinicNearbyAlerts(15);

  if (isLoading)
    return <div className="h-40 animate-pulse rounded-2xl bg-sand-100" />;

  if (isError)
    return (
      <p className="rounded-xl bg-warn-50 border border-warn-200 px-4 py-3 text-sm text-warn-700">
        Las alertas activas cercanas requieren el plan{" "}
        <strong>Clínica Partner</strong>.
      </p>
    );

  if (!alerts || alerts.length === 0)
    return (
      <div className="flex flex-col items-center rounded-2xl border border-dashed border-sand-200 py-12 text-center">
        <span className="mb-2 text-3xl" aria-hidden="true">
          ✅
        </span>
        <p className="text-sm font-semibold text-sand-700">
          Sin alertas activas
        </p>
        <p className="mt-1 text-xs text-sand-400">
          No hay mascotas perdidas reportadas en un radio de 15 km.
        </p>
      </div>
    );

  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between">
        <h2 className="text-base font-bold text-sand-800">
          Alertas cercanas activas
        </h2>
        <span className="rounded-full bg-danger-100 px-2.5 py-0.5 text-xs font-bold text-danger-700">
          {alerts.length} activa{alerts.length !== 1 ? "s" : ""}
        </span>
      </div>
      <p className="text-xs text-sand-500">
        Mascotas perdidas reportadas en un radio de 15 km de tu clínica.
      </p>
      <ul className="space-y-2">
        {alerts.map((alert) => (
          <li
            key={alert.lostPetEventId}
            className="flex items-center gap-3 rounded-2xl border border-danger-100 bg-surface px-4 py-3 shadow-sm"
          >
            {alert.recentPhotoUrl ? (
              <img
                src={alert.recentPhotoUrl}
                alt={alert.petName}
                className="h-10 w-10 shrink-0 rounded-full object-cover border border-danger-200"
              />
            ) : (
              <span
                className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-danger-100 text-xl"
                aria-hidden="true"
              >
                🐾
              </span>
            )}
            <div className="min-w-0 flex-1">
              <p className="text-sm font-bold text-sand-900">{alert.petName}</p>
              <p className="text-[11px] text-sand-500">
                {alert.petSpecies}
                {" · "}
                {new Date(alert.reportedAt).toLocaleDateString("es-CR", {
                  day: "numeric",
                  month: "short",
                  hour: "2-digit",
                  minute: "2-digit",
                })}
              </p>
            </div>
            <a
              href={`/map`}
              className="shrink-0 rounded-xl bg-danger-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-danger-700"
            >
              Ver →
            </a>
          </li>
        ))}
      </ul>
    </section>
  );
}

// ── Stats section ─────────────────────────────────────────────────────────────

function ClinicStatsSection() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const { data: stats, isLoading, isError } = useClinicScanStats(year, month);

  const MONTHS = [
    "Enero",
    "Febrero",
    "Marzo",
    "Abril",
    "Mayo",
    "Junio",
    "Julio",
    "Agosto",
    "Septiembre",
    "Octubre",
    "Noviembre",
    "Diciembre",
  ];

  if (isLoading)
    return <div className="h-40 animate-pulse rounded-2xl bg-sand-100" />;

  if (isError)
    return (
      <p className="rounded-xl bg-warn-50 border border-warn-200 px-4 py-3 text-sm text-warn-700">
        Las estadísticas de escaneos requieren el plan{" "}
        <strong>Clínica Plus</strong>.{" "}
        <span className="underline cursor-pointer">Actualizar →</span>
      </p>
    );

  const maxTotal = Math.max(...(stats?.byDay.map((d) => d.total) ?? [1]), 1);

  return (
    <section className="space-y-4">
      {/* Month picker */}
      <div className="flex items-center gap-2">
        <select
          className="rounded-xl border border-sand-200 bg-surface px-3 py-1.5 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
          value={month}
          onChange={(e) => setMonth(Number(e.target.value))}
        >
          {MONTHS.map((m, i) => (
            <option key={i + 1} value={i + 1}>
              {m}
            </option>
          ))}
        </select>
        <input
          type="number"
          min={2024}
          max={now.getFullYear()}
          value={year}
          onChange={(e) => setYear(Number(e.target.value))}
          className="w-20 rounded-xl border border-sand-200 bg-surface px-3 py-1.5 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
        />
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {[
          {
            label: "Total escaneos",
            value: stats?.totalScans ?? 0,
            color: "text-trust-700",
          },
          {
            label: "Con match",
            value: stats?.matchedScans ?? 0,
            color: "text-rescue-700",
          },
          { label: "QR", value: stats?.qrScans ?? 0, color: "text-brand-700" },
          {
            label: "RFID",
            value: stats?.rfidScans ?? 0,
            color: "text-sand-700",
          },
        ].map(({ label, value, color }) => (
          <div
            key={label}
            className="rounded-2xl border border-sand-200 bg-surface p-3 text-center"
          >
            <p className={`text-2xl font-black tabular-nums ${color}`}>
              {value}
            </p>
            <p className="mt-0.5 text-xs text-sand-500">{label}</p>
          </div>
        ))}
      </div>

      {/* Daily bar chart */}
      {stats && stats.byDay.length > 0 && (
        <div className="rounded-2xl border border-sand-200 bg-surface p-4">
          <p className="mb-3 text-xs font-bold text-sand-500">
            Escaneos por día
          </p>
          <div className="flex items-end gap-0.5 h-20">
            {stats.byDay.map((d) => (
              <div
                key={d.day}
                title={`${d.day}: ${d.total} escaneos`}
                className="flex-1 rounded-sm bg-brand-400 hover:bg-brand-500 transition-colors min-h-[2px]"
                style={{
                  height: `${Math.max(4, (d.total / maxTotal) * 100)}%`,
                }}
              />
            ))}
          </div>
          <div className="mt-1 flex justify-between text-[10px] text-sand-400">
            <span>{stats.byDay[0]?.day.slice(8)}</span>
            <span>{stats.byDay[stats.byDay.length - 1]?.day.slice(8)}</span>
          </div>
        </div>
      )}

      {stats && stats.byDay.length === 0 && (
        <p className="rounded-xl bg-sand-50 border border-sand-200 px-4 py-6 text-sm text-sand-500 text-center">
          Sin escaneos registrados en {MONTHS[month - 1]} {year}.
        </p>
      )}
    </section>
  );
}

// ── API Keys section ──────────────────────────────────────────────────────────

function ClinicApiKeysSection(_: { clinicId: string }) {
  const [newLabel, setNewLabel] = useState("");
  const [justCreated, setJustCreated] = useState<string | null>(null);
  const { data: keys, isLoading, isError } = useClinicApiKeys();
  const { mutateAsync: createKey, isPending: creating } =
    useCreateClinicApiKey();
  const { mutateAsync: revokeKey } = useRevokeClinicApiKey();

  if (isLoading)
    return <div className="h-32 animate-pulse rounded-2xl bg-sand-100" />;

  if (isError)
    return (
      <p className="rounded-xl bg-warn-50 border border-warn-200 px-4 py-3 text-sm text-warn-700">
        Las API Keys requieren el plan <strong>Clínica Partner</strong>.
      </p>
    );

  const handleCreate = async () => {
    if (!newLabel.trim()) return;
    try {
      const key = await createKey(newLabel.trim());
      setNewLabel("");
      if (key.rawKey) setJustCreated(key.rawKey);
    } catch {
      toast.error("No se pudo crear la clave.");
    }
  };

  return (
    <section className="space-y-4">
      <div>
        <h2 className="text-base font-bold text-sand-800">API Keys</h2>
        <p className="text-xs text-sand-500 mt-0.5">
          Usa el header{" "}
          <code className="rounded bg-sand-100 px-1 text-[11px]">
            X-PawTrack-Key
          </code>{" "}
          para integrar tu sistema.
        </p>
      </div>

      {/* Widget snippet */}
      <div className="rounded-2xl border border-trust-200 bg-trust-50 p-4 space-y-2">
        <p className="text-xs font-bold text-trust-800">
          Embed en tu sitio web
        </p>
        <pre className="overflow-x-auto rounded-lg bg-trust-900 p-3 text-[10px] text-green-300 whitespace-pre-wrap">
          {`<div id="pawtrack-widget"></div>
<script src="https://pawtrack.cr/widget.js"></script>`}
        </pre>
        <p className="text-[11px] text-trust-600">
          Agrega un buscador de mascotas PawTrack en tu sitio. Plan Partner
          requerido.
        </p>
      </div>

      {justCreated && (
        <div className="rounded-2xl border border-rescue-300 bg-rescue-50 p-4 space-y-2">
          <p className="text-xs font-bold text-rescue-800">
            ⚠️ Copia tu clave — solo se muestra una vez
          </p>
          <div className="flex items-center gap-2">
            <code className="flex-1 overflow-x-auto rounded-lg bg-rescue-900 px-3 py-2 text-[11px] text-green-300 break-all">
              {justCreated}
            </code>
            <button
              type="button"
              onClick={() => {
                void navigator.clipboard.writeText(justCreated);
                toast.success("Copiada.");
              }}
              className="shrink-0 rounded-lg bg-rescue-100 px-3 py-1.5 text-xs font-bold text-rescue-800 hover:bg-rescue-200"
            >
              Copiar
            </button>
          </div>
          <button
            type="button"
            onClick={() => setJustCreated(null)}
            className="text-xs text-rescue-600 underline"
          >
            Ya copié la clave
          </button>
        </div>
      )}

      {/* Create form */}
      <div className="flex gap-2">
        <input
          type="text"
          value={newLabel}
          onChange={(e) => setNewLabel(e.target.value)}
          placeholder="Nombre de la clave (ej. SistemaVet)"
          maxLength={100}
          className="flex-1 rounded-xl border border-sand-200 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
        />
        <button
          type="button"
          onClick={() => void handleCreate()}
          disabled={creating || !newLabel.trim()}
          className="shrink-0 rounded-xl bg-brand-600 px-4 py-2 text-sm font-bold text-white hover:bg-brand-700 disabled:opacity-50"
        >
          {creating ? "…" : "Crear"}
        </button>
      </div>

      {/* Keys list */}
      {keys && keys.length > 0 && (
        <ul className="space-y-2">
          {keys.map((key) => (
            <li
              key={key.id}
              className={`flex items-center justify-between gap-3 rounded-2xl border px-4 py-3 ${key.isRevoked ? "border-sand-100 bg-sand-50 opacity-60" : "border-sand-200 bg-surface"}`}
            >
              <div className="min-w-0">
                <p className="text-sm font-semibold text-sand-900">
                  {key.label}
                </p>
                <p className="text-[11px] text-sand-400">
                  Creada {new Date(key.createdAt).toLocaleDateString("es-CR")}
                  {key.lastUsedAt &&
                    ` · Último uso ${new Date(key.lastUsedAt).toLocaleDateString("es-CR")}`}
                </p>
              </div>
              {key.isRevoked ? (
                <span className="rounded-full bg-sand-100 px-2 py-0.5 text-[10px] text-sand-500">
                  Revocada
                </span>
              ) : (
                <button
                  type="button"
                  onClick={() =>
                    void revokeKey(key.id).then(() =>
                      toast.success("Clave revocada."),
                    )
                  }
                  className="shrink-0 rounded-lg bg-danger-100 px-3 py-1 text-xs font-bold text-danger-700 hover:bg-danger-200"
                >
                  Revocar
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
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
                {cert.isRevoked
                  ? "Revocado"
                  : cert.isValid
                    ? "Vigente"
                    : "Vencido"}
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
