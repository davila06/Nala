import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceArea,
  ResponsiveContainer,
  type TooltipProps,
} from "recharts";
import { useWeightHistory } from "../hooks/useMedical";
import { Skeleton } from "@/shared/ui/Spinner";
import { PlanGate } from "@/features/pets/components/PlanGate";

interface WeightTrendChartProps {
  petId: string;
  petName: string;
}

function ChartTooltip(props: TooltipProps<number, string>) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { active, payload } = props as any;
  if (!active || !payload?.length) return null;
  const d = payload[0]?.payload as { date: string; weightKg: number; source: string; clinicName: string | null } | undefined;
  if (!d) return null;
  return (
    <div className="rounded-xl border border-sand-200 bg-surface px-3 py-2 shadow-lg text-xs">
      <p className="font-semibold text-sand-900">{d.weightKg.toFixed(1)} kg</p>
      <p className="text-sand-500">{d.date}</p>
      <p className="text-sand-400">
        {d.source === "Clinic" ? `🏥 ${d.clinicName ?? "Clínica"}` : "📝 Dueño"}
      </p>
    </div>
  );
}

function WeightChartInner({ petId, petName }: WeightTrendChartProps) {
  const { data, isLoading, error } = useWeightHistory(petId);

  // Silently skip if plan gate returned 403 — PlanGate wrapper shows the upsell
  const is403 =
    (error as { response?: { status?: number } } | null)?.response?.status ===
    403;

  if (isLoading) return <Skeleton className="h-52 w-full rounded-2xl" />;
  if (is403 || !data) return null;
  if (data.entries.length < 2)
    return (
      <p className="rounded-xl border border-sand-100 bg-sand-50 px-4 py-3 text-xs text-sand-400">
        Registra el peso de {petName} en cada visita para ver la tendencia aquí.
      </p>
    );

  const chartData = data.entries.map((e) => ({
    date: new Date(e.date).toLocaleDateString("es-CR", {
      day: "numeric",
      month: "short",
      year: "2-digit",
    }),
    weightKg: e.weightKg,
    source: e.source,
    clinicName: e.clinicName,
  }));

  const allWeights = data.entries.map((e) => e.weightKg);
  const minW = Math.min(...allWeights);
  const maxW = Math.max(...allWeights);
  const padding = Math.max((maxW - minW) * 0.25, 0.5);
  const yMin = Math.max(0, minW - padding);
  const yMax = maxW + padding;

  return (
    <div className="space-y-2">
      {data.weightChangeAlert && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-xl border border-warn-200 bg-warn-50 px-3 py-2 text-xs text-warn-800"
        >
          <span aria-hidden="true" className="shrink-0">
            ⚠️
          </span>
          <span>{data.weightChangeAlert}</span>
        </div>
      )}

      <div
        role="img"
        aria-label={`Gráfico de peso de ${petName}: ${allWeights.length} registros, de ${minW.toFixed(1)} kg a ${maxW.toFixed(1)} kg`}
      >
        <ResponsiveContainer width="100%" height={200}>
          <LineChart
            data={chartData}
            margin={{ top: 4, right: 8, left: -16, bottom: 0 }}
          >
            <CartesianGrid
              strokeDasharray="3 3"
              stroke="var(--color-sand-200)"
            />

            <XAxis
              dataKey="date"
              tick={{ fontSize: 10, fill: "var(--color-sand-500)" }}
              axisLine={false}
              tickLine={false}
              interval="preserveStartEnd"
            />
            <YAxis
              domain={[yMin, yMax]}
              tickFormatter={(v: number) => `${v.toFixed(1)}`}
              tick={{ fontSize: 10, fill: "var(--color-sand-500)" }}
              axisLine={false}
              tickLine={false}
              unit=" kg"
            />

            <Tooltip content={<ChartTooltip />} />

            {/* Healthy weight reference band */}
            {data.reference && (
              <ReferenceArea
                y1={data.reference.minKg}
                y2={data.reference.maxKg}
                fill="var(--color-rescue-100)"
                fillOpacity={0.4}
                label={{
                  value: data.reference.label,
                  position: "insideTopRight",
                  fontSize: 9,
                  fill: "var(--color-rescue-600)",
                }}
              />
            )}

            <Line
              type="monotone"
              dataKey="weightKg"
              stroke="var(--color-brand-500)"
              strokeWidth={2}
              dot={(props) => {
                const isClinic = props.payload?.source === "Clinic";
                const cx = props.cx ?? 0;
                const cy = props.cy ?? 0;
                return (
                  <rect
                    key={props.key}
                    x={cx - 4}
                    y={cy - 4}
                    width={8}
                    height={8}
                    fill={
                      isClinic
                        ? "var(--color-trust-500)"
                        : "var(--color-brand-500)"
                    }
                    rx={isClinic ? 0 : 4}
                    stroke="white"
                    strokeWidth={1.5}
                  />
                );
              }}
              activeDot={{ r: 5, strokeWidth: 0 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>

      <div className="flex items-center gap-4 text-[10px] text-sand-500">
        <span className="flex items-center gap-1">
          <span className="inline-block h-2 w-2 rounded-full bg-brand-500" />
          Registrado por dueño
        </span>
        <span className="flex items-center gap-1">
          <span className="inline-block h-2 w-2 rounded bg-trust-500" />
          Registrado por clínica
        </span>
        {data.reference && (
          <span className="flex items-center gap-1">
            <span className="inline-block h-2 w-3 rounded-sm bg-rescue-200" />
            Rango saludable
          </span>
        )}
      </div>
    </div>
  );
}

export function WeightTrendChart({ petId, petName }: WeightTrendChartProps) {
  return (
    <PlanGate requires="Familia">
      <div className="rounded-2xl border border-sand-100 bg-surface p-4">
        <h3 className="mb-3 text-xs font-semibold uppercase tracking-wide text-sand-500">
          Tendencia de peso
        </h3>
        <WeightChartInner petId={petId} petName={petName} />
      </div>
    </PlanGate>
  );
}
