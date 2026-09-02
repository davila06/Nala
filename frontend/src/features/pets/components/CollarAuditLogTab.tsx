import { useCollarAuditLog } from "../hooks/useCollar";

const EVENT_LABELS: Record<string, string> = {
  SerialRegistered: "Serial registrado en inventario",
  SerialMarkedSold: "Marcado como vendido",
  Activated: "Collar activado",
  Deactivated: "Collar desvinculado",
  DeviceKeyRevoked: "Llave de dispositivo revocada",
  DeviceKeyRegenerated: "Llave de dispositivo regenerada",
  LocationIngestFailed: "Intento de ingesta de ubicación rechazado",
};

interface CollarAuditLogTabProps {
  collarId: string;
}

/** Read-only audit trail for a collar: activation, deactivation, key rotation, etc. */
export function CollarAuditLogTab({ collarId }: CollarAuditLogTabProps) {
  const { data: entries, isLoading } = useCollarAuditLog(collarId);

  if (isLoading) {
    return <div className="h-24 animate-pulse rounded-2xl bg-sand-100" />;
  }

  if (!entries || entries.length === 0) {
    return (
      <p className="text-xs text-sand-400">Sin eventos registrados todavía.</p>
    );
  }

  return (
    <ul className="space-y-2">
      {entries.map((entry) => (
        <li
          key={entry.id}
          className="rounded-xl border border-sand-200 bg-surface px-3 py-2"
        >
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold text-sand-800">
              {EVENT_LABELS[entry.event] ?? entry.event}
            </span>
            <span className="text-[10px] text-sand-400">
              {new Date(entry.createdAt).toLocaleString("es-CR")}
            </span>
          </div>
          {entry.details && (
            <p className="mt-0.5 text-[11px] text-sand-500">{entry.details}</p>
          )}
        </li>
      ))}
    </ul>
  );
}
