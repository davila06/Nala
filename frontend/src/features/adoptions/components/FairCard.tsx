import type { AdoptionFairDto } from "../api/adoptionsApi";

interface FairCardProps {
  fair: AdoptionFairDto;
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString("es-CR", {
    weekday: "short",
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  });
}

const STATUS_MAP: Record<string, { label: string; color: string }> = {
  Upcoming: { label: "Próxima", color: "bg-blue-50 text-blue-700" },
  Active: { label: "En curso ✓", color: "bg-green-50 text-green-700" },
  Finished: { label: "Finalizada", color: "bg-sand-100 text-sand-500" },
  Cancelled: { label: "Cancelada", color: "bg-red-50 text-red-500" },
};

export function FairCard({ fair }: FairCardProps) {
  const st = STATUS_MAP[fair.status] ?? { label: fair.status, color: "" };

  return (
    <div className="rounded-2xl border border-sand-100 bg-surface p-4 space-y-3 hover:shadow-md transition-shadow">
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div>
          <h3 className="font-semibold text-ink-800 leading-tight">
            {fair.title}
          </h3>
          {fair.description && (
            <p className="text-xs text-sand-500 mt-0.5 line-clamp-2">
              {fair.description}
            </p>
          )}
        </div>
        <span
          className={`text-[10px] font-bold px-2.5 py-1 rounded-full shrink-0 ${st.color}`}
        >
          {st.label}
        </span>
      </div>

      {/* Details */}
      <div className="space-y-1 text-xs text-sand-500">
        <p>
          📅 {formatDateTime(fair.startsAt)} → {formatDateTime(fair.endsAt)}
        </p>
        <p>📍 {fair.venueLabel}</p>
        {fair.animalIds.length > 0 && (
          <p>🐾 {fair.animalIds.length} animales presentes</p>
        )}
      </div>
    </div>
  );
}
