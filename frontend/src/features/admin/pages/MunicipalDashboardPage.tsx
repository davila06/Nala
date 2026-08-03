import { useState } from "react";
import {
  useMunicipalProfile,
  useCapturedAnimals,
  useRecordCapture,
  useUpdateCaptureStatus,
  useBulkUpdateStatus,
  useCantonStats,
  useRegionalDashboard,
} from "../hooks/useMunicipal";
import {
  STATUS_LABELS,
  TIER_LABELS,
  type CapturedAnimalStatus,
  type MunicipalTier,
} from "../api/municipalApi";
import { Button, Input, Card } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";

// ── Tier badge ────────────────────────────────────────────────────────────────

function TierBadge({ tier }: { tier: MunicipalTier }) {
  const cls: Record<MunicipalTier, string> = {
    Basica: "bg-sand-100 text-sand-600",
    Full: "bg-trust-100 text-trust-700",
    RedRegional: "bg-rescue-100 text-rescue-700",
  };
  return (
    <span
      className={`rounded-full px-2.5 py-0.5 text-xs font-semibold ${cls[tier]}`}
    >
      {TIER_LABELS[tier]}
    </span>
  );
}

// ── Captures tab (all tiers) ──────────────────────────────────────────────────

function CapturesTab({
  tier,
  canton,
}: {
  tier: MunicipalTier;
  canton: string;
}) {
  const [filterStatus, setFilterStatus] = useState<CapturedAnimalStatus | "">(
    "",
  );
  const [filterCanton, setFilterCanton] = useState("");
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [showNewForm, setShowNewForm] = useState(false);

  const canMultiCanton = tier !== "Basica";

  const { data, isLoading } = useCapturedAnimals(
    filterCanton || undefined,
    filterStatus || undefined,
    page,
  );
  const recordCapture = useRecordCapture();
  const updateStatus = useUpdateCaptureStatus();
  const bulkUpdate = useBulkUpdateStatus();

  // New capture form state
  const [form, setForm] = useState({
    canton,
    species: "",
    color: "",
    breed: "",
    estimatedAge: "",
    notes: "",
    collarChipNumber: "",
  });

  const handleRecord = () => {
    if (!form.species.trim() || !form.color.trim()) {
      toast.error("Especie y color son requeridos");
      return;
    }
    recordCapture.mutate(
      { ...form, canton: form.canton || canton },
      {
        onSuccess: () => {
          toast.success("Registro creado");
          setShowNewForm(false);
          setForm({
            canton,
            species: "",
            color: "",
            breed: "",
            estimatedAge: "",
            notes: "",
            collarChipNumber: "",
          });
        },
        onError: () => toast.error("No se pudo crear el registro"),
      },
    );
  };

  const toggleSelect = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

  const handleBulkUpdate = (status: CapturedAnimalStatus) => {
    if (selectedIds.size === 0) return;
    bulkUpdate.mutate(
      { animalIds: [...selectedIds], newStatus: status },
      {
        onSuccess: (r) => {
          toast.success(`${r.updated} registros actualizados`);
          setSelectedIds(new Set());
        },
        onError: (err: unknown) =>
          toast.error(
            (err as { response?: { data?: { detail?: string } } })?.response
              ?.data?.detail ?? "Error",
          ),
      },
    );
  };

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-wrap gap-2">
        {canMultiCanton && (
          <Input
            placeholder="Filtrar por cantón"
            value={filterCanton}
            onChange={(e) => {
              setFilterCanton(e.target.value);
              setPage(1);
            }}
            className="w-40"
          />
        )}
        <select
          value={filterStatus}
          onChange={(e) => {
            setFilterStatus(e.target.value as CapturedAnimalStatus | "");
            setPage(1);
          }}
          className="rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          <option value="">Todos los estados</option>
          {(Object.keys(STATUS_LABELS) as CapturedAnimalStatus[]).map((s) => (
            <option key={s} value={s}>
              {STATUS_LABELS[s]}
            </option>
          ))}
        </select>
        <Button size="sm" onClick={() => setShowNewForm((v) => !v)}>
          {showNewForm ? "Cerrar" : "+ Registrar captura"}
        </Button>
        {tier !== "Basica" && selectedIds.size > 0 && (
          <div className="flex gap-1">
            {(
              ["OwnerFound", "Adopted", "Released"] as CapturedAnimalStatus[]
            ).map((s) => (
              <button
                key={s}
                type="button"
                disabled={bulkUpdate.isPending}
                onClick={() => handleBulkUpdate(s)}
                className="rounded-lg border border-sand-200 px-2 py-1 text-xs font-semibold text-sand-700 hover:bg-sand-100 disabled:opacity-50"
              >
                {selectedIds.size} → {STATUS_LABELS[s]}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* New capture form */}
      {showNewForm && (
        <div className="rounded-2xl border border-trust-200 bg-trust-50 p-4 space-y-3">
          <p className="text-sm font-semibold text-trust-800">
            Nuevo registro de captura
          </p>
          <div className="grid grid-cols-2 gap-2">
            {canMultiCanton && (
              <div>
                <label className="mb-1 block text-xs font-medium text-sand-600">
                  Cantón
                </label>
                <Input
                  value={form.canton}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, canton: e.target.value }))
                  }
                  placeholder={canton}
                />
              </div>
            )}
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Especie *
              </label>
              <Input
                value={form.species}
                onChange={(e) =>
                  setForm((f) => ({ ...f, species: e.target.value }))
                }
                placeholder="Perro, Gato…"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Color *
              </label>
              <Input
                value={form.color}
                onChange={(e) =>
                  setForm((f) => ({ ...f, color: e.target.value }))
                }
                placeholder="Café, negro…"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Raza
              </label>
              <Input
                value={form.breed}
                onChange={(e) =>
                  setForm((f) => ({ ...f, breed: e.target.value }))
                }
                placeholder="Opcional"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                Edad estimada
              </label>
              <Input
                value={form.estimatedAge}
                onChange={(e) =>
                  setForm((f) => ({ ...f, estimatedAge: e.target.value }))
                }
                placeholder="1-2 años"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-sand-600">
                N° chip/collar
              </label>
              <Input
                value={form.collarChipNumber}
                onChange={(e) =>
                  setForm((f) => ({ ...f, collarChipNumber: e.target.value }))
                }
              />
            </div>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-sand-600">
              Notas
            </label>
            <textarea
              value={form.notes}
              onChange={(e) =>
                setForm((f) => ({ ...f, notes: e.target.value }))
              }
              rows={2}
              className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-trust-400"
            />
          </div>
          <div className="flex gap-2">
            <Button
              onClick={handleRecord}
              loading={recordCapture.isPending}
              className="flex-1"
            >
              Guardar
            </Button>
            <Button
              variant="secondary"
              onClick={() => setShowNewForm(false)}
              className="flex-1"
            >
              Cancelar
            </Button>
          </div>
        </div>
      )}

      {/* List */}
      {isLoading ? (
        <div className="animate-pulse space-y-2">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-16 rounded-xl bg-sand-100" />
          ))}
        </div>
      ) : data && data.items.length > 0 ? (
        <>
          <ul className="space-y-2">
            {data.items.map((a) => (
              <li
                key={a.id}
                className="rounded-xl border border-sand-100 bg-surface-warm px-4 py-3"
              >
                <div className="flex items-start gap-3">
                  {tier !== "Basica" && (
                    <input
                      type="checkbox"
                      checked={selectedIds.has(a.id)}
                      onChange={() => toggleSelect(a.id)}
                      className="mt-1 h-4 w-4 rounded border-sand-300 text-brand-600"
                    />
                  )}
                  {a.photoUrl && (
                    <img
                      src={a.photoUrl}
                      alt={a.species}
                      className="h-12 w-12 shrink-0 rounded-lg object-cover border border-sand-200"
                    />
                  )}
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-semibold text-sand-900">
                        {a.species}
                      </span>
                      {a.breed && (
                        <span className="text-xs text-sand-500">{a.breed}</span>
                      )}
                      <span className="rounded-full bg-sand-100 px-2 py-0.5 text-xs font-medium text-sand-600">
                        {a.color}
                      </span>
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                          a.status === "Received"
                            ? "bg-warn-100 text-warn-700"
                            : a.status === "OwnerFound"
                              ? "bg-rescue-100 text-rescue-700"
                              : "bg-sand-100 text-sand-600"
                        }`}
                      >
                        {STATUS_LABELS[a.status]}
                      </span>
                    </div>
                    <p className="text-xs text-sand-500">
                      {a.canton} ·{" "}
                      {new Date(a.capturedAt).toLocaleDateString("es-CR")}
                      {a.collarChipNumber && ` · Chip: ${a.collarChipNumber}`}
                    </p>
                    {a.notes && (
                      <p className="mt-1 text-xs text-sand-600 truncate">
                        {a.notes}
                      </p>
                    )}
                  </div>
                  <select
                    value={a.status}
                    onChange={(e) =>
                      updateStatus.mutate({
                        id: a.id,
                        status: e.target.value as CapturedAnimalStatus,
                      })
                    }
                    className="shrink-0 rounded-lg border border-sand-200 bg-white px-2 py-1 text-xs focus:outline-none"
                  >
                    {(Object.keys(STATUS_LABELS) as CapturedAnimalStatus[]).map(
                      (s) => (
                        <option key={s} value={s}>
                          {STATUS_LABELS[s]}
                        </option>
                      ),
                    )}
                  </select>
                </div>
              </li>
            ))}
          </ul>
          <div className="flex items-center justify-between text-xs text-sand-500">
            <span>{data.total} registros</span>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={page === 1}
                onClick={() => setPage((p) => p - 1)}
                className="rounded px-2 py-1 border border-sand-200 disabled:opacity-40 hover:bg-sand-100"
              >
                ← Anterior
              </button>
              <button
                type="button"
                disabled={page * 20 >= data.total}
                onClick={() => setPage((p) => p + 1)}
                className="rounded px-2 py-1 border border-sand-200 disabled:opacity-40 hover:bg-sand-100"
              >
                Siguiente →
              </button>
            </div>
          </div>
        </>
      ) : (
        <Card padding="sm">
          <p className="text-center text-sm text-sand-400">
            No hay registros con los filtros actuales.
          </p>
        </Card>
      )}
    </div>
  );
}

// ── Stats tab (Full+) ─────────────────────────────────────────────────────────

function StatsTab({ tier }: { tier: MunicipalTier }) {
  const { data: stats, isLoading } = useCantonStats();

  if (tier === "Basica") {
    return (
      <div className="rounded-2xl border border-warn-200 bg-warn-50 p-5 text-center space-y-2">
        <p className="text-sm font-semibold text-warn-800">
          📊 Estadísticas requieren el plan Full
        </p>
        <p className="text-xs text-warn-700">
          Contacta a PawTrack CR para actualizar tu plan municipal.
        </p>
      </div>
    );
  }

  if (isLoading)
    return (
      <div className="animate-pulse space-y-2">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-16 rounded-xl bg-sand-100" />
        ))}
      </div>
    );

  if (!stats) return null;

  const cards = [
    {
      label: "Total capturado",
      value: stats.totalCaptured,
      color: "text-sand-900",
    },
    { label: "En custodia", value: stats.received, color: "text-warn-700" },
    {
      label: "Dueño localizado",
      value: stats.ownerFound,
      color: "text-rescue-700",
    },
    { label: "Adoptado", value: stats.adopted, color: "text-trust-700" },
    {
      label: "Tasa recuperación",
      value: `${stats.recoveryRate}%`,
      color: "text-brand-700",
    },
  ];

  const maxDay = Math.max(...stats.last30Days.map((d) => d.count), 1);

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        {cards.map((c) => (
          <div
            key={c.label}
            className="rounded-xl border border-sand-100 bg-surface-warm p-3 text-center"
          >
            <p className={`text-2xl font-black tabular-nums ${c.color}`}>
              {c.value}
            </p>
            <p className="text-xs text-sand-500">{c.label}</p>
          </div>
        ))}
      </div>
      <div>
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-sand-500">
          Últimos 30 días
        </p>
        {stats.last30Days.length > 0 ? (
          <div className="flex items-end gap-1 h-24">
            {stats.last30Days.map((d) => (
              <div
                key={d.date}
                className="flex-1 flex flex-col items-center gap-1"
                title={`${d.date}: ${d.count}`}
              >
                <div
                  className="w-full rounded-t bg-brand-400"
                  style={{
                    height: `${(d.count / maxDay) * 100}%`,
                    minHeight: d.count > 0 ? "4px" : "0",
                  }}
                />
              </div>
            ))}
          </div>
        ) : (
          <p className="text-xs text-sand-400">
            Sin actividad en los últimos 30 días.
          </p>
        )}
      </div>
    </div>
  );
}

// ── Regional tab (RedRegional only) ──────────────────────────────────────────

function RegionalTab({ tier }: { tier: MunicipalTier }) {
  const { data: dashboard, isLoading } = useRegionalDashboard();

  if (tier !== "RedRegional") {
    return (
      <div className="rounded-2xl border border-warn-200 bg-warn-50 p-5 text-center space-y-2">
        <p className="text-sm font-semibold text-warn-800">
          🗺️ Dashboard regional requiere el plan Red Regional
        </p>
        <p className="text-xs text-warn-700">
          Contacta a PawTrack CR para actualizar tu plan.
        </p>
      </div>
    );
  }

  if (isLoading)
    return (
      <div className="animate-pulse space-y-2">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-12 rounded-xl bg-sand-100" />
        ))}
      </div>
    );

  if (!dashboard) return null;

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3">
        <div className="rounded-xl border border-sand-100 bg-surface-warm p-3 text-center">
          <p className="text-2xl font-black text-sand-900">
            {dashboard.regionalTotal}
          </p>
          <p className="text-xs text-sand-500">Total regional</p>
        </div>
        <div className="rounded-xl border border-sand-100 bg-surface-warm p-3 text-center">
          <p className="text-2xl font-black text-brand-700">
            {dashboard.regionalRecoveryRate}%
          </p>
          <p className="text-xs text-sand-500">Tasa recuperación</p>
        </div>
      </div>
      <div>
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-sand-500">
          Por cantón
        </p>
        <ul className="space-y-2">
          {dashboard.summary.map((s) => (
            <li
              key={s.canton}
              className="rounded-xl border border-sand-100 bg-surface-warm px-4 py-3"
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-sand-900">{s.canton}</p>
                  <p className="text-xs text-sand-500">
                    {s.total} total · {s.active} en custodia · {s.ownerFound}{" "}
                    localizados
                  </p>
                </div>
                <span className="text-lg font-black text-brand-700">
                  {s.recoveryRate}%
                </span>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

// ── Main dashboard ────────────────────────────────────────────────────────────

export default function MunicipalDashboardPage() {
  const { data: profile, isLoading: loadingProfile } = useMunicipalProfile();
  const [activeTab, setActiveTab] = useState<
    "capturas" | "estadisticas" | "regional"
  >("capturas");

  if (loadingProfile) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10 animate-pulse space-y-3">
        <div className="h-8 w-48 rounded bg-sand-100" />
        <div className="h-40 rounded-2xl bg-sand-100" />
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="mx-auto max-w-lg px-4 py-12 text-center space-y-3">
        <p className="text-3xl">🏛️</p>
        <p className="text-lg font-semibold text-sand-800">
          Perfil municipal no configurado
        </p>
        <p className="text-sm text-sand-500">
          Tu cuenta tiene rol Municipalidad pero aún no tiene un perfil
          asignado. Contacta al equipo de PawTrack CR para activar tu acceso.
        </p>
      </div>
    );
  }

  const tier = profile.tier as MunicipalTier;

  const TABS = [
    { id: "capturas" as const, label: "📋 Capturas" },
    {
      id: "estadisticas" as const,
      label: "📊 Estadísticas",
      disabled: tier === "Basica",
    },
    {
      id: "regional" as const,
      label: "🗺️ Regional",
      disabled: tier !== "RedRegional",
    },
  ];

  return (
    <main className="mx-auto max-w-2xl px-4 py-8 animate-fade-in-up space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="font-display text-xl font-bold text-sand-900">
            {profile.orgName}
          </h1>
          <p className="text-sm text-sand-500">
            {profile.canton}
            {profile.allCantons.length > 1 &&
              ` + ${profile.allCantons.length - 1} cantones más`}
            {profile.expiresAt && (
              <>
                {" "}
                · vence{" "}
                {new Date(profile.expiresAt).toLocaleDateString("es-CR")}
              </>
            )}
          </p>
        </div>
        <TierBadge tier={tier} />
      </div>

      {/* Tab nav */}
      <div className="flex gap-1 rounded-2xl bg-surface-warm p-1.5">
        {TABS.map((t) => (
          <button
            key={t.id}
            type="button"
            disabled={t.disabled}
            onClick={() => setActiveTab(t.id)}
            className={[
              "flex-1 rounded-xl py-2 text-xs font-bold transition-colors",
              activeTab === t.id
                ? "bg-surface text-sand-900 shadow-sm"
                : t.disabled
                  ? "text-sand-300 cursor-not-allowed"
                  : "text-sand-500 hover:text-sand-700",
            ].join(" ")}
          >
            {t.label}
          </button>
        ))}
      </div>

      {activeTab === "capturas" && (
        <CapturesTab tier={tier} canton={profile.canton} />
      )}
      {activeTab === "estadisticas" && <StatsTab tier={tier} />}
      {activeTab === "regional" && <RegionalTab tier={tier} />}
    </main>
  );
}
