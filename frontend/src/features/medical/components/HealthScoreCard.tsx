import { useHealthScore } from "../hooks/useMedical";
import { Skeleton } from "@/shared/ui/Spinner";
import { useMyTier } from "@/features/pets/hooks/useMyTier";

interface HealthScoreCardProps {
  petId: string;
  petName: string;
}

function ScoreCircle({ score }: { score: number }) {
  const radius = 36;
  const circ = 2 * Math.PI * radius;
  const fill = (score / 100) * circ;

  const color =
    score >= 80
      ? "var(--color-rescue-500)"
      : score >= 50
        ? "var(--color-warn-500)"
        : "var(--color-danger-500)";

  const label =
    score >= 80 ? "Excelente" : score >= 50 ? "Regular" : "Atención";

  return (
    <div className="flex flex-col items-center" aria-hidden="true">
      <svg width="88" height="88" viewBox="0 0 88 88">
        {/* Track */}
        <circle
          cx="44"
          cy="44"
          r={radius}
          fill="none"
          stroke="var(--color-sand-200)"
          strokeWidth="7"
        />
        {/* Progress */}
        <circle
          cx="44"
          cy="44"
          r={radius}
          fill="none"
          stroke={color}
          strokeWidth="7"
          strokeLinecap="round"
          strokeDasharray={`${fill} ${circ}`}
          strokeDashoffset={circ / 4} /* start at 12 o'clock */
          style={{ transition: "stroke-dasharray 0.6s ease-out" }}
        />
        <text
          x="44"
          y="41"
          textAnchor="middle"
          fontSize="18"
          fontWeight="700"
          fill={color}
          fontFamily="var(--font-display)"
        >
          {score}
        </text>
        <text
          x="44"
          y="55"
          textAnchor="middle"
          fontSize="9"
          fill="var(--color-sand-500)"
          fontFamily="var(--font-body)"
        >
          {label}
        </text>
      </svg>
    </div>
  );
}

function HealthScoreInner({ petId, petName }: HealthScoreCardProps) {
  const { data, isLoading, error } = useHealthScore(petId);
  const is403 =
    (error as { response?: { status?: number } } | null)?.response?.status ===
    403;

  if (isLoading) return <Skeleton className="h-24 w-full rounded-xl" />;
  if (is403 || !data) return null;

  return (
    <div
      className="flex items-start gap-4"
      role="region"
      aria-label={`Score de salud de ${petName}: ${data.score} de 100`}
    >
      <ScoreCircle score={data.score} />

      <div className="flex-1 min-w-0">
        <p className="text-xs font-semibold uppercase tracking-wide text-sand-500 mb-2">
          Score de salud preventiva
        </p>
        <ul className="space-y-1.5">
          {data.breakdown.map((item) => (
            <li
              key={item.recordType}
              className="flex items-center gap-2 text-xs"
            >
              <span
                className={`h-2 w-2 shrink-0 rounded-full ${item.isCompliant ? "bg-rescue-500" : "bg-danger-400"}`}
                aria-hidden="true"
              />
              <span
                className={`flex-1 ${item.isCompliant ? "text-sand-700" : "text-sand-900 font-medium"}`}
              >
                {item.protocolName}
              </span>
              {item.lastDate ? (
                <span className="text-sand-400 shrink-0">
                  {item.isCompliant ? "✓" : "⚠"} {item.lastDate}
                </span>
              ) : (
                <span className="text-danger-500 font-semibold shrink-0">
                  Sin registro
                </span>
              )}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export function HealthScoreCard({ petId, petName }: HealthScoreCardProps) {
  const { isPlus, isLoading } = useMyTier();
  if (isLoading) return null;
  if (!isPlus) return null; // Silent — PlanGate shown at banner level

  return (
    <div className="mb-4 rounded-2xl border border-sand-100 bg-surface p-4">
      <HealthScoreInner petId={petId} petName={petName} />
    </div>
  );
}
