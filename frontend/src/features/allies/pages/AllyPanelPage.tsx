import { useState } from "react";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import type { AllyType } from "../api/alliesApi";
import { CoverageMapPicker } from "../components/CoverageMapPicker";
import {
  useConfirmAllyAlertAction,
  useDownloadAssignedWelfareEvidence,
  useMyAssignedWelfareCase,
  useMyAssignedWelfareCases,
  useMyAllyAlerts,
  useMyAllyProfile,
  useSubmitAllyApplication,
} from "../hooks/useAllies";

// ── Animated KPI counter ──────────────────────────────────────────────────────
function KPICard({
  icon,
  label,
  value,
  color,
}: {
  icon: string;
  label: string;
  value: number | string;
  color: string;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      className={`rounded-2xl border p-4 bg-surface ${color}`}
    >
      <p className="text-2xl" aria-hidden="true">
        {icon}
      </p>
      <p className="mt-2 text-2xl font-black tabular-nums text-sand-900">{value}</p>
      <p className="text-xs text-copy-secondary mt-0.5">{label}</p>
    </motion.div>
  );
}

const allyTypeOptions: Array<{ value: AllyType; label: string }> = [
  { value: "VeterinaryClinic", label: "Veterinaria" },
  { value: "Shelter", label: "Refugio" },
  { value: "PetFriendlyBusiness", label: "Comercio pet-friendly" },
  { value: "PrivateSecurity", label: "Seguridad privada" },
  { value: "Municipality", label: "Municipalidad" },
];

const initialForm = {
  organizationName: "",
  allyType: "VeterinaryClinic" as AllyType,
  coverageLabel: "",
  coverageLat: 9.9281,
  coverageLng: -84.0907,
  coverageRadiusMetres: 2000,
};

export default function AllyPanelPage() {
  const [form, setForm] = useState(initialForm);
  const [selectedWelfareCaseId, setSelectedWelfareCaseId] = useState<string | null>(null);
  const { data: profile, isLoading: isProfileLoading } = useMyAllyProfile();
  const { mutate: submitApplication, isPending: isSubmitting } = useSubmitAllyApplication();
  const { mutate: confirmAction, isPending: isConfirmingAction } = useConfirmAllyAlertAction();
  const isVerified = profile?.verificationStatus === "Verified";
  const { data: alerts, isLoading: isAlertsLoading } = useMyAllyAlerts(isVerified);
  const { data: assignedWelfareCases, isLoading: isWelfareCasesLoading } = useMyAssignedWelfareCases(isVerified);
  const { data: selectedWelfareCase, isLoading: isWelfareCaseDetailLoading } = useMyAssignedWelfareCase(
    selectedWelfareCaseId,
    isVerified,
  );
  const downloadEvidence = useDownloadAssignedWelfareEvidence();

  const handleEvidenceDownload = async (evidenceId: string, fileName: string) => {
    if (!selectedWelfareCaseId) return;
    try {
      const blob = await downloadEvidence.mutateAsync({ caseId: selectedWelfareCaseId, evidenceId });
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      // Private case evidence is not cached or logged when access is denied.
    }
  };

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    submitApplication(form);
  };

  return (
    <div className="mx-auto max-w-5xl px-4 py-8 animate-fade-in-up">
      <div className="mb-8 flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-rescue-700">Operación comunitaria</p>
          <h1 className="mt-2 text-3xl font-black tracking-tight text-sand-950">Red de aliados verificados</h1>
          <p className="mt-2 max-w-2xl text-sm text-copy-secondary">
            Postula tu organización y, una vez verificada, recibe alertas operativas por zona para apoyar casos activos.
          </p>
        </div>
        <Link
          to="/dashboard"
          className="rounded-full border border-sand-300 px-4 py-2 text-sm font-semibold text-sand-700 transition hover:border-sand-400 hover:text-sand-900"
        >
          Volver al dashboard
        </Link>
      </div>

      {/* KPI bar for verified allies */}
      {isVerified && alerts && (
        <div className="mb-8 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <KPICard icon="🔔" label="Alertas recibidas" value={alerts.length} color="border-sand-200" />
          <KPICard
            icon="✅"
            label="Respondidas"
            value={alerts.filter((a) => a.actionConfirmedAt !== null).length}
            color="border-rescue-200"
          />
          <KPICard
            icon="📊"
            label="Tasa de respuesta"
            value={
              alerts.length > 0
                ? `${Math.round((alerts.filter((a) => a.actionConfirmedAt !== null).length / alerts.length) * 100)}%`
                : "—"
            }
            color="border-trust-200"
          />
          <KPICard
            icon="📍"
            label="Radio cubierto"
            value={profile ? `${(profile.coverageRadiusMetres / 1000).toFixed(1)} km` : "—"}
            color="border-brand-200"
          />
        </div>
      )}

      {isProfileLoading && <div className="h-40 animate-pulse rounded-3xl bg-sand-100" />}

      {!isProfileLoading && isVerified && (
        <section className="mb-8 rounded-3xl border border-rescue-200 bg-surface p-6 shadow-sm">
          <div className="mb-5">
            <h2 className="text-xl font-bold text-sand-900">Casos de bienestar asignados</h2>
            <p className="mt-1 text-sm text-copy-secondary">
              Casos confirmados por PawTrack para tu organización; el acceso se revisa según tu verificación y cobertura
              vigentes.
            </p>
          </div>
          {isWelfareCasesLoading && <div className="h-20 animate-pulse rounded-2xl bg-sand-100" />}
          {!isWelfareCasesLoading && !assignedWelfareCases?.items.length && (
            <p className="rounded-xl border border-dashed border-sand-300 p-5 text-center text-sm text-copy-secondary">
              No tienes casos de bienestar asignados.
            </p>
          )}
          {!!assignedWelfareCases?.items.length && (
            <div className="grid gap-4 lg:grid-cols-[minmax(220px,0.8fr)_minmax(0,1.2fr)]">
              <ul className="space-y-2" aria-label="Casos asignados">
                {assignedWelfareCases.items.map((item) => (
                  <li key={item.id}>
                    <button
                      type="button"
                      aria-pressed={selectedWelfareCaseId === item.id}
                      onClick={() => setSelectedWelfareCaseId(item.id)}
                      className={`w-full rounded-xl border p-3 text-left transition ${selectedWelfareCaseId === item.id ? "border-trust-500 bg-trust-50" : "border-sand-200 bg-white hover:border-trust-300"}`}
                    >
                      <span className="flex items-center justify-between gap-2">
                        <span className="font-bold text-sand-900">#{item.publicCode}</span>
                        <span className="text-xs font-semibold text-trust-800">{item.severity}</span>
                      </span>
                      <span className="mt-1 block text-xs text-copy-secondary">
                        {item.type} · {item.canton}
                      </span>
                      <span className="mt-1 block text-xs text-copy-muted">{item.status}</span>
                    </button>
                  </li>
                ))}
              </ul>
              <div className="min-h-40 rounded-2xl border border-sand-200 bg-sand-50 p-4">
                {!selectedWelfareCaseId && (
                  <p className="text-sm text-copy-secondary">
                    Selecciona un caso para ver la información compartida contigo.
                  </p>
                )}
                {isWelfareCaseDetailLoading && (
                  <p role="status" className="text-sm text-copy-secondary">
                    Cargando caso…
                  </p>
                )}
                {selectedWelfareCase && (
                  <article>
                    <h3 className="font-bold text-sand-900">
                      #{selectedWelfareCase.publicCode} · {selectedWelfareCase.type}
                    </h3>
                    <p className="mt-1 text-xs text-copy-secondary">
                      {selectedWelfareCase.canton} · {selectedWelfareCase.severity} · {selectedWelfareCase.status}
                    </p>
                    <p className="mt-3 whitespace-pre-wrap text-sm text-sand-800">{selectedWelfareCase.description}</p>
                    {selectedWelfareCase.evidence.length > 0 && (
                      <div className="mt-4 border-t border-sand-200 pt-3">
                        <p className="mb-2 text-xs font-bold uppercase tracking-wide text-copy-secondary">
                          Evidencia compartida
                        </p>
                        <ul className="space-y-2">
                          {selectedWelfareCase.evidence.map((item) => (
                            <li key={item.id} className="flex items-center justify-between gap-3 text-xs">
                              <span>
                                {item.evidenceKind} · {Math.ceil(item.fileSizeBytes / 1024)} KB
                                {item.isSensitive ? " · sensible" : ""}
                              </span>
                              <button
                                type="button"
                                disabled={downloadEvidence.isPending}
                                onClick={() => void handleEvidenceDownload(item.id, `evidencia-${item.id}`)}
                                className="font-semibold text-trust-800 underline disabled:opacity-50"
                              >
                                Descargar
                              </button>
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </article>
                )}
              </div>
            </div>
          )}
        </section>
      )}

      {!isProfileLoading && profile && (
        <section className="mb-8 rounded-3xl border border-sand-200 field-input p-6 shadow-sm">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.25em] text-copy-secondary">Estado actual</p>
              <h2 className="mt-2 text-xl font-bold text-sand-900">{profile.organizationName}</h2>
              <p className="mt-1 text-sm text-copy-secondary">
                {profile.coverageLabel} · Radio declarado de {profile.coverageRadiusMetres} m
              </p>
            </div>
            <span
              className={`rounded-full px-3 py-1 text-xs font-bold uppercase tracking-[0.2em] ${
                profile.verificationStatus === "Verified"
                  ? "bg-rescue-100 text-rescue-800"
                  : profile.verificationStatus === "Pending"
                    ? "bg-brand-100 text-brand-800"
                    : "bg-danger-100 text-danger-800"
              }`}
            >
              {profile.verificationStatus === "Verified"
                ? "Verificado"
                : profile.verificationStatus === "Pending"
                  ? "Pendiente de revisión"
                  : "Requiere nueva postulación"}
            </span>
          </div>
        </section>
      )}

      {!isProfileLoading && !isVerified && (
        <section className="grid gap-6 lg:grid-cols-[1.1fr_0.9fr]">
          <form onSubmit={handleSubmit} className="rounded-3xl border border-sand-200 field-input p-6 shadow-sm">
            <div className="mb-6">
              <h2 className="text-xl font-bold text-sand-900">Solicitud de verificación</h2>
              <p className="mt-2 text-sm text-copy-secondary">
                Declara la organización, el tipo de aliado y la cobertura geográfica que tu equipo puede atender.
              </p>
            </div>

            <div className="space-y-4">
              <label className="block text-sm font-semibold text-sand-700">
                Organización
                <input
                  type="text"
                  value={form.organizationName}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      organizationName: event.target.value,
                    }))
                  }
                  className="mt-2 w-full rounded-2xl border border-sand-300 px-4 py-3 text-sm text-sand-900 outline-none transition focus:border-brand-500"
                />
              </label>

              <label className="block text-sm font-semibold text-sand-700">
                Tipo de aliado
                <select
                  value={form.allyType}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      allyType: event.target.value as AllyType,
                    }))
                  }
                  className="mt-2 w-full rounded-2xl border border-sand-300 px-4 py-3 text-sm text-sand-900 outline-none transition focus:border-brand-500"
                >
                  {allyTypeOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block text-sm font-semibold text-sand-700">
                Zona de cobertura
                <input
                  type="text"
                  value={form.coverageLabel}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      coverageLabel: event.target.value,
                    }))
                  }
                  className="mt-2 w-full rounded-2xl border border-sand-300 px-4 py-3 text-sm text-sand-900 outline-none transition focus:border-brand-500"
                />
              </label>

              <div>
                <p className="mb-2 text-sm font-semibold text-sand-700">Área de cobertura</p>
                <CoverageMapPicker
                  lat={form.coverageLat}
                  lng={form.coverageLng}
                  radiusMetres={form.coverageRadiusMetres}
                  onChange={(lat, lng, radiusMetres) =>
                    setForm((current) => ({
                      ...current,
                      coverageLat: lat,
                      coverageLng: lng,
                      coverageRadiusMetres: radiusMetres,
                    }))
                  }
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="mt-6 rounded-full bg-rescue-600 px-5 py-3 text-sm font-bold text-white transition hover:bg-rescue-700 disabled:opacity-60"
            >
              {isSubmitting ? "Enviando..." : "Enviar solicitud"}
            </button>
          </form>

          <aside className="rounded-3xl bg-trust-900 p-6 text-white shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-trust-200">MVP</p>
            <h2 className="mt-3 text-2xl font-black tracking-tight">Verificación manual por el equipo PawTrack</h2>
            <p className="mt-3 text-sm text-trust-50/90">
              Cuando la solicitud quede aprobada, tu organización recibirá una bandeja operativa con alertas de pérdida
              dentro de su cobertura declarada.
            </p>
          </aside>
        </section>
      )}

      {!isProfileLoading && isVerified && (
        <section className="rounded-3xl border border-sand-200 bg-surface p-6 shadow-sm">
          <div className="mb-6 flex items-center justify-between gap-4">
            <div>
              <h2 className="text-xl font-bold text-sand-900">Bandeja operativa</h2>
              <p className="mt-1 text-sm text-copy-secondary">
                Alertas activas dentro de la cobertura de tu organización.
              </p>
            </div>
            <span
              className={`rounded-full px-3 py-1 text-xs font-bold uppercase tracking-[0.2em] ${
                (alerts?.filter((a) => !a.actionConfirmedAt).length ?? 0) > 0
                  ? "bg-warn-100 text-warn-700"
                  : "bg-rescue-100 text-rescue-800"
              }`}
            >
              {alerts?.filter((a) => !a.actionConfirmedAt).length ?? 0} sin responder
            </span>
          </div>

          {isAlertsLoading && <div className="h-24 animate-pulse rounded-2xl bg-sand-100" />}

          {!isAlertsLoading && (!alerts || alerts.length === 0) && (
            <div className="rounded-2xl border border-dashed border-sand-300 px-6 py-10 text-center text-sm text-copy-secondary">
              No hay alertas operativas en este momento.
            </div>
          )}

          {!isAlertsLoading && alerts && alerts.length > 0 && (
            <div className="space-y-4">
              {alerts.map((alert) => (
                <article key={alert.notificationId} className="rounded-2xl border border-sand-200 bg-surface-warm p-5">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <h3 className="text-base font-bold text-sand-900">{alert.title}</h3>
                      <p className="mt-2 text-sm text-copy-secondary">{alert.body}</p>
                      <p className="mt-3 text-xs font-medium uppercase tracking-[0.2em] text-copy-muted">
                        {new Date(alert.createdAt).toLocaleString("es-CR")}
                      </p>
                    </div>
                    {alert.actionConfirmedAt ? (
                      <span className="rounded-full bg-rescue-100 px-3 py-1 text-xs font-bold uppercase tracking-[0.2em] text-rescue-800">
                        Acción confirmada
                      </span>
                    ) : (
                      <button
                        type="button"
                        disabled={isConfirmingAction}
                        onClick={() =>
                          confirmAction({
                            notificationId: alert.notificationId,
                            actionSummary: "Ya buscamos en nuestra área",
                          })
                        }
                        className="rounded-full bg-sand-950 px-4 py-2 text-sm font-bold text-white transition hover:bg-sand-800 disabled:opacity-60"
                      >
                        Ya buscamos en nuestra área
                      </button>
                    )}
                  </div>
                  {alert.actionSummary && (
                    <p className="mt-3 text-sm font-medium text-rescue-700">{alert.actionSummary}</p>
                  )}
                </article>
              ))}
            </div>
          )}
        </section>
      )}
    </div>
  );
}
