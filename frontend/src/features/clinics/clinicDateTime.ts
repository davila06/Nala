const CLINIC_TIME_ZONE = "America/Costa_Rica";
const DAY_IN_MILLISECONDS = 24 * 60 * 60 * 1000;

function getDateParts(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) throw new RangeError("Expected a YYYY-MM-DD clinic date.");
  return { year: Number(match[1]), month: Number(match[2]), day: Number(match[3]) };
}

function getOffsetMinutes(instant: Date) {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: CLINIC_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hourCycle: "h23",
  }).formatToParts(instant);
  const values = Object.fromEntries(parts.map(({ type, value }) => [type, value]));
  const clinicWallTime = Date.UTC(
    Number(values.year),
    Number(values.month) - 1,
    Number(values.day),
    Number(values.hour),
    Number(values.minute),
    Number(values.second),
  );
  return (clinicWallTime - instant.getTime()) / 60000;
}

export function toCostaRicaUtc(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?$/.exec(value);
  if (!match) throw new RangeError("Expected a YYYY-MM-DDTHH:mm clinic date-time.");
  const [, year, month, day, hour, minute, second = "0"] = match;
  const wallTimeAsUtc = Date.UTC(
    Number(year),
    Number(month) - 1,
    Number(day),
    Number(hour),
    Number(minute),
    Number(second),
  );
  let instant = wallTimeAsUtc;

  for (let attempt = 0; attempt < 2; attempt += 1) {
    instant = wallTimeAsUtc - getOffsetMinutes(new Date(instant)) * 60000;
  }

  return new Date(instant);
}

export function getClinicDateInputValue(now = new Date()) {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: CLINIC_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(now);
  const values = Object.fromEntries(parts.map(({ type, value }) => [type, value]));
  return `${values.year}-${values.month}-${values.day}`;
}

export function getClinicDayRange(value: string) {
  const { year, month, day } = getDateParts(value);
  const start = toCostaRicaUtc(`${value}T00:00`);
  const nextDate = new Date(Date.UTC(year, month - 1, day + 1));
  const nextDateValue = `${nextDate.getUTCFullYear()}-${String(nextDate.getUTCMonth() + 1).padStart(2, "0")}-${String(nextDate.getUTCDate()).padStart(2, "0")}`;
  return { from: start.toISOString(), to: toCostaRicaUtc(`${nextDateValue}T00:00`).toISOString() };
}

export function getClinicWeekRange(value: string) {
  const { year, month, day } = getDateParts(value);
  const weekday = new Date(Date.UTC(year, month - 1, day)).getUTCDay();
  const mondayOffset = weekday === 0 ? -6 : 1 - weekday;
  const monday = new Date(Date.UTC(year, month - 1, day + mondayOffset));
  const nextMonday = new Date(monday.getTime() + 7 * DAY_IN_MILLISECONDS);
  const toClinicDate = (date: Date) =>
    `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, "0")}-${String(date.getUTCDate()).padStart(2, "0")}`;
  return {
    from: toCostaRicaUtc(`${toClinicDate(monday)}T00:00`).toISOString(),
    to: toCostaRicaUtc(`${toClinicDate(nextMonday)}T00:00`).toISOString(),
  };
}

export function toCostaRicaDateTimeInput(value: string) {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: CLINIC_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).formatToParts(new Date(value));
  const values = Object.fromEntries(parts.map(({ type, value: partValue }) => [type, partValue]));
  return `${values.year}-${values.month}-${values.day}T${values.hour}:${values.minute}`;
}

export function formatCostaRicaDate(value: string | Date) {
  const date =
    typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value)
      ? new Date(`${value}T12:00:00.000Z`)
      : new Date(value);
  return new Intl.DateTimeFormat("es-CR", {
    timeZone: CLINIC_TIME_ZONE,
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(date);
}

export function formatCostaRicaTime(value: string) {
  return new Intl.DateTimeFormat("es-CR", {
    timeZone: CLINIC_TIME_ZONE,
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(new Date(value));
}

export function daysFromClinicToday(dueDate: string, now = new Date()) {
  const due = getDateParts(dueDate);
  const today = getDateParts(getClinicDateInputValue(now));
  return (
    (Date.UTC(due.year, due.month - 1, due.day) - Date.UTC(today.year, today.month - 1, today.day)) /
    DAY_IN_MILLISECONDS
  );
}
