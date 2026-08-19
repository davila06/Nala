import { useState } from "react";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from "recharts";
import {
  useActivityLogs,
  useLogActivity,
  useDeleteActivity,
} from "../hooks/useActivity";
import type { ActivityType } from "../api/activityApi";
import { Button } from "@/shared/ui/Button";
import { toast } from "@/shared/lib/toast";
import { Skeleton } from "@/shared/ui/Spinner";
import { useMyTier } from "@/features/pets/hooks/useMyTier";

// ── Config ────────────────────────────────────────────────────────────────────

const TYPE_CONFIG: Record<
  ActivityType,
  { label: string; emoji: string; color: string }
> = {
  Walk: { label: "Paseo", emoji: "🦮", color: "var(--color-brand-500)" },
  Run: { label: "Carrera", emoji: "🏃", color: "var(--color-danger-500)" },
  Play: { label: "Juego", emoji: "🎾", color: "var(--color-rescue-500)" },
  Swim: { label: "Natación", emoji: "🏊", color: "var(--color-trust-500)" },
  Training: {
    label: "Entrenamiento",
    emoji: "🏋️",
    color: "var(--color-warn-600)",
  },
  Other: { label: "Otro", emoji: "🐾", color: "var(--color-sand-500)" },
};

// ── Quick log form ────────────────────────────────────────────────────────────

function QuickLogForm({
  petId,
  onClose,
}: {
  petId: string;
  onClose: () => void;
}) {
  const [type, setType] = useState<ActivityType>("Walk");
  const [duration, setDuration] = useState(30);
  const [distance, setDistance] = useState("");
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const logActivity = useLogActivity(petId);

  const handleSubmit = () => {
    logActivity.mutate(
      {
        date,
        type,
        durationMinutes: duration,
        distanceMeters: distance
          ? Math.round(parseFloat(distance) * 1000)
          : undefined,
      },
      {
        onSuccess: () => {
          toast.success("Actividad registrada");
          onClose();
        },
        onError: () => toast.error("No se pudo guardar"),
      },
    );
  };

  return (
    <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4 space-y-4">
      <h3 className="text-sm font-semibold text-brand-800">
        Registrar actividad
      </h3>

      {/* Type selector */}
      <div>
        <p className="mb-2 text-xs font-medium text-sand-600">Tipo</p>
        <div className="flex flex-wrap gap-2">
          {(Object.keys(TYPE_CONFIG) as ActivityType[]).map((t) => (
            <button
              key={t}
              type="button"
              onClick={() => setType(t)}
              aria-pressed={type === t}
              className={[
                "flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-semibold border transition-all",
                type === t
                  ? "border-brand-500 bg-brand-500 text-white"
                  : "border-sand-200 bg-white text-sand-700 hover:border-brand-300",
              ].join(" ")}
            >
              <span aria-hidden="true">{TYPE_CONFIG[t].emoji}</span>
              {TYPE_CONFIG[t].label}
            </button>
          ))}
        </div>
      </div>

      {/* Duration */}
      <div>
        <label className="mb-1 block text-xs font-medium text-sand-600">
          Duración:{" "}
          <span className="font-bold text-sand-900">{duration} min</span>
        </label>
        <input
          type="range"
          min={5}
          max={120}
          step={5}
          value={duration}
          onChange={(e) => setDuration(Number(e.target.value))}
          className="w-full accent-brand-500"
          aria-label={`Duración: ${duration} minutos`}
        />
        <div className="flex justify-between text-[10px] text-sand-400 mt-0.5">
          <span>5 min</span>
          <span>60 min</span>
          <span>120 min</span>
        </div>
      </div>

      {/* Distance (optional) */}
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label
            htmlFor="act-distance"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Distancia km (opcional)
          </label>
          <input
            id="act-distance"
            type="number"
            min="0"
            step="0.1"
            placeholder="ej. 2.5"
            value={distance}
            onChange={(e) => setDistance(e.target.value)}
            className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
        <div>
          <label
            htmlFor="act-date"
            className="mb-1 block text-xs font-medium text-sand-600"
          >
            Fecha
          </label>
          <input
            id="act-date"
            type="date"
            value={date}
            max={new Date().toISOString().slice(0, 10)}
            onChange={(e) => setDate(e.target.value)}
            className="w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>
      </div>

      <div className="flex gap-2">
        <Button
          fullWidth
          onClick={handleSubmit}
          loading={logActivity.isPending}
        >
          Registrar
        </Button>
        <Button variant="secondary" onClick={onClose}>
          Cancelar
        </Button>
      </div>
    </div>
  );
}

// ── Weekly bar chart ──────────────────────────────────────────────────────────

function WeeklyChart({
  data,
}: {
  data: { day: string; minutes: number; type: ActivityType }[];
}) {
  return (
    <ResponsiveContainer width="100%" height={140}>
      <BarChart data={data} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
        <CartesianGrid
          strokeDasharray="3 3"
          stroke="var(--color-sand-200)"
          vertical={false}
        />
        <XAxis
          dataKey="day"
          tick={{ fontSize: 10, fill: "var(--color-sand-500)" }}
          axisLine={false}
          tickLine={false}
        />
        <YAxis
          tick={{ fontSize: 10, fill: "var(--color-sand-500)" }}
          axisLine={false}
          tickLine={false}
          unit=" m"
        />
        <Tooltip
          formatter={(v: number) => [`${v} min`, "Duración"]}
          contentStyle={{
            borderRadius: 12,
            border: "1px solid var(--color-sand-200)",
            fontSize: 11,
          }}
        />
        <Bar dataKey="minutes" radius={[4, 4, 0, 0]}>
          {data.map((entry, i) => (
            <Cell
              key={i}
              fill={
                entry.minutes > 0
                  ? "var(--color-brand-500)"
                  : "var(--color-sand-200)"
              }
            />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

// ── Main component ────────────────────────────────────────────────────────────

interface ActivityTabProps {
  petId: string;
  petName: string;
}

export function ActivityTab({ petId, petName }: ActivityTabProps) {
  const { isPlus, isLoading: tierLoading } = useMyTier();
  const [showForm, setShowForm] = useState(false);
  const deleteActivity = useDeleteActivity(petId);

  const to = new Date().toISOString().slice(0, 10);
  const from = new Date(Date.now() - 28 * 24 * 60 * 60 * 1000)
    .toISOString()
    .slice(0, 10);

  const { data, isLoading, error } = useActivityLogs(petId, from, to);
  const is403 =
    (error as { response?: { status?: number } } | null)?.response?.status ===
    403;

  if (tierLoading || isLoading)
    return <Skeleton className="h-48 w-full rounded-2xl" />;

  if (!isPlus || is403) {
    return (
      <div className="rounded-2xl border border-sand-200 bg-surface p-6 text-center space-y-3">
        <p className="text-2xl" aria-hidden="true">
          🏃
        </p>
        <p className="font-semibold text-sand-900">Registro de actividad</p>
        <p className="text-sm text-sand-500">
          Disponible en el plan Plus. Lleva el historial de ejercicio de{" "}
          {petName} y conecta con tu collar Tractive.
        </p>
      </div>
    );
  }

  // Build last-7-days chart data
  const today = new Date();
  const chartData = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(today);
    d.setDate(today.getDate() - 6 + i);
    const dateStr = d.toISOString().slice(0, 10);
    const logsForDay = data?.logs.filter((l) => l.date === dateStr) ?? [];
    const totalMin = logsForDay.reduce((s, l) => s + l.durationMinutes, 0);
    const dominantType = logsForDay[0]?.type ?? "Walk";
    return {
      day: d.toLocaleDateString("es-CR", { weekday: "short" }),
      minutes: totalMin,
      type: dominantType as ActivityType,
    };
  });

  const thisWeekMin =
    data?.weeklyTotals[data.weeklyTotals.length - 1]?.totalMinutes ?? 0;
  const benchmark = data?.benchmark;
  const streakPct = benchmark
    ? Math.min(
        100,
        Math.round((thisWeekMin / 7 / benchmark.dailyMinutesMax) * 100),
      )
    : 0;

  return (
    <div className="space-y-4">
      {/* Streak banner */}
      <div className="flex items-center justify-between rounded-2xl border border-sand-100 bg-surface p-4">
        <div className="flex items-center gap-2">
          <span className="text-2xl" aria-hidden="true">
            {(data?.streakDays ?? 0) >= 7 ? "🔥" : "⚡"}
          </span>
          <div>
            <p className="text-sm font-bold text-sand-900">
              {data?.streakDays ?? 0} día{data?.streakDays !== 1 ? "s" : ""}{" "}
              consecutivos
            </p>
            <p className="text-xs text-sand-500">Racha actual de {petName}</p>
          </div>
        </div>
        <Button
          size="sm"
          onClick={() => setShowForm(true)}
          aria-label="Registrar nueva actividad"
        >
          + Registrar
        </Button>
      </div>

      {showForm && (
        <QuickLogForm petId={petId} onClose={() => setShowForm(false)} />
      )}

      {/* Benchmark progress */}
      {benchmark && (
        <div className="rounded-xl border border-sand-100 bg-sand-50 p-4 space-y-2">
          <div className="flex items-center justify-between text-xs">
            <span className="font-semibold text-sand-700">
              Objetivo semanal: {benchmark.dailyMinutesMin * 7}–
              {benchmark.dailyMinutesMax * 7} min
            </span>
            <span
              className={`font-bold ${
                streakPct >= 80
                  ? "text-rescue-600"
                  : streakPct >= 50
                    ? "text-warn-600"
                    : "text-danger-600"
              }`}
            >
              {thisWeekMin} min esta semana
            </span>
          </div>
          <div
            className="h-2 rounded-full bg-sand-200 overflow-hidden"
            role="progressbar"
            aria-valuenow={streakPct}
            aria-valuemin={0}
            aria-valuemax={100}
          >
            <div
              className={`h-full rounded-full transition-all ${
                streakPct >= 80
                  ? "bg-rescue-500"
                  : streakPct >= 50
                    ? "bg-warn-500"
                    : "bg-danger-500"
              }`}
              style={{ width: `${streakPct}%` }}
            />
          </div>
        </div>
      )}

      {/* 7-day chart */}
      <div
        className="rounded-xl border border-sand-100 bg-surface p-4"
        role="img"
        aria-label={`Actividad de ${petName} en los últimos 7 días`}
      >
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-sand-500">
          Últimos 7 días (min)
        </p>
        <WeeklyChart data={chartData} />
      </div>

      {/* Activity log list */}
      {(data?.logs.length ?? 0) === 0 ? (
        <p className="py-6 text-center text-sm text-sand-400">
          Sin actividad registrada en los últimos 28 días. ¡Empieza hoy!
        </p>
      ) : (
        <ul
          className="space-y-2"
          aria-label={`Historial de actividad de ${petName}`}
        >
          {data!.logs.map((log) => (
            <li
              key={log.id}
              className="flex items-center gap-3 rounded-xl border border-sand-100 bg-surface p-3"
            >
              <span className="text-xl shrink-0" aria-hidden="true">
                {TYPE_CONFIG[log.type]?.emoji ?? "🐾"}
              </span>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-sand-900 flex items-center gap-1.5">
                  {TYPE_CONFIG[log.type]?.label ?? log.type}
                  {log.source === "Tractive" && (
                    <span className="rounded-full bg-trust-100 px-1.5 py-0.5 text-[9px] font-bold text-trust-700">
                      📡 GPS
                    </span>
                  )}
                </p>
                <p className="text-xs text-sand-500">
                  {log.date} · {log.durationMinutes} min
                  {log.distanceMeters != null
                    ? ` · ${(log.distanceMeters / 1000).toFixed(1)} km`
                    : ""}
                </p>
              </div>
              {log.source !== "Tractive" && (
                <button
                  type="button"
                  onClick={() =>
                    deleteActivity.mutate(log.id, {
                      onSuccess: () => toast.success("Eliminado"),
                      onError: () => toast.error("Error al eliminar"),
                    })
                  }
                  disabled={deleteActivity.isPending}
                  aria-label={`Eliminar actividad del ${log.date}`}
                  className="flex h-7 w-7 items-center justify-center rounded-lg text-sand-300 hover:bg-danger-50 hover:text-danger-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400 disabled:opacity-50"
                >
                  <svg
                    viewBox="0 0 16 16"
                    fill="currentColor"
                    className="h-3.5 w-3.5"
                    aria-hidden="true"
                  >
                    <path d="M11 1.75V3h2.25a.75.75 0 0 1 0 1.5H2.75a.75.75 0 0 1 0-1.5H5V1.75C5 .784 5.784 0 6.75 0h2.5C10.216 0 11 .784 11 1.75ZM4.496 6.675l.66 6.6a.25.25 0 0 0 .249.225h5.19a.25.25 0 0 0 .249-.225l.66-6.6a.75.75 0 0 1 1.492.149l-.66 6.6A1.748 1.748 0 0 1 10.595 15h-5.19a1.75 1.75 0 0 1-1.741-1.575l-.66-6.6a.75.75 0 1 1 1.492-.15ZM6.5 1.75V3h3V1.75a.25.25 0 0 0-.25-.25h-2.5a.25.25 0 0 0-.25.25Z" />
                  </svg>
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
