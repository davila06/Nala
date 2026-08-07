import { useState } from "react";
import type { VetReminderDto } from "@/features/medical/api/medicalApi";

const TYPE_COLOR: Record<string, string> = {
  Vaccine: "bg-trust-500",
  Deworming: "bg-green-500",
  Checkup: "bg-brand-500",
  Surgery: "bg-danger-500",
  Medication: "bg-warn-500",
  Allergy: "bg-pink-500",
  Other: "bg-sand-400",
};

const MONTH_NAMES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];
const DAY_NAMES = ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];

function buildCalendarDays(year: number, month: number) {
  const firstDay = new Date(year, month, 1).getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const cells: (number | null)[] = Array(firstDay).fill(null);
  for (let d = 1; d <= daysInMonth; d++) cells.push(d);
  while (cells.length % 7 !== 0) cells.push(null);
  return cells;
}

interface Props {
  reminders: VetReminderDto[];
}

export function ReminderCalendar({ reminders }: Props) {
  const today = new Date();
  const [viewYear, setViewYear] = useState(today.getFullYear());
  const [viewMonth, setViewMonth] = useState(today.getMonth());
  const [selectedDay, setSelectedDay] = useState<number | null>(null);

  const days = buildCalendarDays(viewYear, viewMonth);

  // Map "YYYY-MM-DD" → reminders
  const remindersByDay = new Map<string, VetReminderDto[]>();
  reminders.forEach((r) => {
    const existing = remindersByDay.get(r.dueDate) ?? [];
    remindersByDay.set(r.dueDate, [...existing, r]);
  });

  const todayStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;

  function dayKey(day: number) {
    return `${viewYear}-${String(viewMonth + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
  }

  const selectedReminders = selectedDay ? (remindersByDay.get(dayKey(selectedDay)) ?? []) : [];

  const prevMonth = () => {
    if (viewMonth === 0) { setViewYear((y) => y - 1); setViewMonth(11); }
    else setViewMonth((m) => m - 1);
    setSelectedDay(null);
  };

  const nextMonth = () => {
    if (viewMonth === 11) { setViewYear((y) => y + 1); setViewMonth(0); }
    else setViewMonth((m) => m + 1);
    setSelectedDay(null);
  };

  return (
    <div className="rounded-2xl border border-sand-100 bg-white p-4 space-y-3">
      {/* Header */}
      <div className="flex items-center justify-between">
        <button type="button" onClick={prevMonth}
          className="rounded-lg p-1.5 text-sand-500 hover:bg-sand-100 hover:text-sand-800">◀</button>
        <span className="text-sm font-semibold text-sand-800">
          {MONTH_NAMES[viewMonth]} {viewYear}
        </span>
        <button type="button" onClick={nextMonth}
          className="rounded-lg p-1.5 text-sand-500 hover:bg-sand-100 hover:text-sand-800">▶</button>
      </div>

      {/* Day headers */}
      <div className="grid grid-cols-7 gap-px">
        {DAY_NAMES.map((d) => (
          <div key={d} className="py-1 text-center text-xs font-semibold text-sand-400">{d}</div>
        ))}
      </div>

      {/* Cells */}
      <div className="grid grid-cols-7 gap-px">
        {days.map((day, i) => {
          if (day === null) return <div key={`e-${i}`} />;
          const key = dayKey(day);
          const rems = remindersByDay.get(key) ?? [];
          const isToday = key === todayStr;
          const isSelected = day === selectedDay;
          const hasOverdue = rems.some((r) => !r.isCompleted && key < todayStr);

          return (
            <button
              key={key}
              type="button"
              onClick={() => setSelectedDay(isSelected ? null : day)}
              className={`relative flex flex-col items-center rounded-lg p-1 transition-colors ${
                isSelected ? "bg-brand-100 ring-2 ring-brand-400" :
                isToday ? "bg-trust-50 ring-1 ring-trust-300" : "hover:bg-sand-50"
              }`}
            >
              <span className={`text-xs font-medium ${
                isToday ? "text-trust-700" :
                hasOverdue ? "text-danger-600" : "text-sand-700"
              }`}>{day}</span>
              {/* Dots for reminders */}
              <div className="mt-0.5 flex gap-0.5 flex-wrap justify-center max-w-[28px]">
                {rems.slice(0, 3).map((r, ri) => (
                  <span key={ri} className={`h-1.5 w-1.5 rounded-full ${TYPE_COLOR[r.type] ?? "bg-sand-400"}`} />
                ))}
              </div>
            </button>
          );
        })}
      </div>

      {/* Selected day detail */}
      {selectedDay !== null && selectedReminders.length > 0 && (
        <div className="border-t border-sand-100 pt-3 space-y-2">
          <p className="text-xs font-semibold text-sand-500">
            {selectedDay} de {MONTH_NAMES[viewMonth]}
          </p>
          {selectedReminders.map((r) => (
            <div key={r.id} className="flex items-start gap-2 rounded-lg bg-sand-50 p-2">
              <span className={`mt-0.5 h-2 w-2 shrink-0 rounded-full ${TYPE_COLOR[r.type] ?? "bg-sand-400"}`} />
              <div className="min-w-0">
                <p className={`text-xs font-semibold ${r.isCompleted ? "line-through text-sand-400" : "text-sand-800"}`}>
                  {r.title}
                </p>
                {r.notes && <p className="text-xs text-sand-500 truncate">{r.notes}</p>}
              </div>
              {r.isCompleted && <span className="ml-auto text-xs text-green-600 shrink-0">✓</span>}
            </div>
          ))}
        </div>
      )}
      {selectedDay !== null && selectedReminders.length === 0 && (
        <div className="border-t border-sand-100 pt-3">
          <p className="text-center text-xs text-sand-400">Sin recordatorios este día</p>
        </div>
      )}
    </div>
  );
}
