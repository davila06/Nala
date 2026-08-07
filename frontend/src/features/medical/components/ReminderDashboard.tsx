import { Link } from "react-router-dom";
import { useMyReminders } from "@/features/medical/hooks/useMedical";
import type { PetReminderDto } from "@/features/medical/api/medicalApi";

const TYPE_EMOJI: Record<string, string> = {
  Vaccine: "💉",
  Deworming: "🪱",
  Checkup: "🩺",
  Surgery: "🔪",
  Medication: "💊",
  Allergy: "🌿",
  Other: "📋",
};

function getDayLabel(dueDate: string): string {
  const today = new Date();
  const todayStr = today.toISOString().slice(0, 10);
  const diff = (new Date(dueDate).getTime() - new Date(todayStr).getTime()) / 86_400_000;

  if (diff < 0) return "Vencido";
  if (diff === 0) return "Hoy";
  if (diff === 1) return "Mañana";
  if (diff <= 7) return "Esta semana";
  if (diff <= 14) return "Próximas 2 semanas";
  return "Próximo mes";
}

const BUCKET_ORDER = ["Vencido", "Hoy", "Mañana", "Esta semana", "Próximas 2 semanas", "Próximo mes"];

function ReminderRow({ reminder }: { reminder: PetReminderDto }) {
  const isOverdue = reminder.isOverdue;
  return (
    <Link
      to={`/pets/${reminder.petId}`}
      className={`flex items-center gap-3 rounded-xl border p-3 transition-colors hover:bg-sand-50 ${
        isOverdue ? "border-danger-200 bg-danger-50" : "border-sand-100 bg-white"
      }`}
    >
      {reminder.petPhotoUrl ? (
        <img src={reminder.petPhotoUrl} alt={reminder.petName}
          className="h-9 w-9 rounded-full object-cover shrink-0" />
      ) : (
        <div className="h-9 w-9 rounded-full bg-sand-100 flex items-center justify-center text-lg shrink-0">🐾</div>
      )}
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5">
          <span className="text-sm font-semibold text-sand-800 truncate">{reminder.title}</span>
          <span className="text-xs text-sand-500 shrink-0">{TYPE_EMOJI[reminder.type]}</span>
        </div>
        <p className="text-xs text-sand-500">{reminder.petName} · {reminder.dueDate}</p>
      </div>
      {isOverdue && (
        <span className="shrink-0 rounded-full bg-danger-100 px-2 py-0.5 text-xs font-semibold text-danger-700">
          Vencido
        </span>
      )}
    </Link>
  );
}

interface Props {
  daysAhead?: number;
}

export function ReminderDashboard({ daysAhead = 30 }: Props) {
  const { data: reminders, isLoading } = useMyReminders(daysAhead);

  if (isLoading) {
    return (
      <div className="animate-pulse space-y-2">
        {[1, 2, 3].map((i) => <div key={i} className="h-14 rounded-xl bg-sand-100" />)}
      </div>
    );
  }

  if (!reminders || reminders.length === 0) {
    return (
      <div className="rounded-2xl border border-sand-100 bg-sand-50 p-8 text-center">
        <p className="text-2xl mb-2">🎉</p>
        <p className="text-sm font-medium text-sand-600">Sin recordatorios pendientes</p>
        <p className="text-xs text-sand-400 mt-1">en los próximos {daysAhead} días</p>
      </div>
    );
  }

  // Group by time bucket
  const grouped = new Map<string, PetReminderDto[]>();
  reminders.forEach((r) => {
    const bucket = getDayLabel(r.dueDate);
    grouped.set(bucket, [...(grouped.get(bucket) ?? []), r]);
  });

  const overdueCount = reminders.filter((r) => r.isOverdue).length;

  return (
    <div className="space-y-4">
      {overdueCount > 0 && (
        <div className="flex items-center gap-2 rounded-xl bg-danger-50 border border-danger-200 px-3 py-2">
          <span className="text-sm font-semibold text-danger-700">
            ⚠️ {overdueCount} recordatorio{overdueCount !== 1 ? "s" : ""} vencido{overdueCount !== 1 ? "s" : ""}
          </span>
        </div>
      )}

      {BUCKET_ORDER.filter((b) => grouped.has(b)).map((bucket) => (
        <div key={bucket}>
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-sand-500">{bucket}</p>
          <div className="space-y-2">
            {grouped.get(bucket)!.map((r) => <ReminderRow key={r.reminderId} reminder={r} />)}
          </div>
        </div>
      ))}
    </div>
  );
}
