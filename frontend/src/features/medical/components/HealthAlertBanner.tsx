import { AnimatePresence, motion } from "framer-motion";
import { useHealthAlerts } from "../hooks/useMedical";
import type { HealthAlertDto } from "../api/medicalApi";

interface HealthAlertBannerProps {
  petId: string;
  petName: string;
  /** Called when user taps "Programar cita" — parent decides what to open */
  onSchedule?: (alert: HealthAlertDto) => void;
}

function severityClasses(s: string) {
  switch (s) {
    case "critical": return { border: "border-danger-300",  bg: "bg-danger-50",  text: "text-danger-800",  icon: "🔴", btnCls: "bg-danger-600 hover:bg-danger-700" };
    case "warning":  return { border: "border-warn-300",    bg: "bg-warn-50",    text: "text-warn-800",    icon: "🟡", btnCls: "bg-warn-600 hover:bg-warn-700" };
    default:         return { border: "border-trust-200",   bg: "bg-trust-50",   text: "text-trust-800",   icon: "ℹ️",  btnCls: "bg-trust-600 hover:bg-trust-700" };
  }
}

function relativeDate(daysUntilDue: number, isOverdue: boolean) {
  if (isOverdue) return `Atrasado ${Math.abs(daysUntilDue)} día${Math.abs(daysUntilDue) !== 1 ? "s" : ""}`;
  if (daysUntilDue === 0) return "Vence hoy";
  if (daysUntilDue === 1) return "Vence mañana";
  return `Vence en ${daysUntilDue} días`;
}

export function HealthAlertBanner({ petId, petName, onSchedule }: HealthAlertBannerProps) {
  const { data: alerts = [], isLoading } = useHealthAlerts(petId);

  // Only show critical and warning — info is too noisy in a banner
  const visible = alerts.filter((a) => a.severity === "critical" || a.severity === "warning");

  if (isLoading || visible.length === 0) return null;

  return (
    <div className="mb-4 space-y-2" role="region" aria-label={`Alertas de salud de ${petName}`}>
      <AnimatePresence initial={false}>
        {visible.map((alert) => {
          const cls = severityClasses(alert.severity);
          return (
            <motion.div
              key={alert.recordType}
              initial={{ opacity: 0, y: -8, height: 0 }}
              animate={{ opacity: 1, y: 0, height: "auto" }}
              exit={{ opacity: 0, y: -8, height: 0 }}
              transition={{ duration: 0.22, ease: [0.4, 0, 0.2, 1] }}
            >
              <div
                role="alert"
                className={`flex items-center gap-3 rounded-xl border px-4 py-3 ${cls.border} ${cls.bg}`}
              >
                <span className="text-lg shrink-0" aria-hidden="true">{cls.icon}</span>

                <div className="flex-1 min-w-0">
                  <p className={`text-xs font-semibold ${cls.text}`}>
                    {alert.protocolName}
                    {" · "}
                    <span className="font-normal">{petName}</span>
                  </p>
                  <p className={`text-xs ${cls.text} opacity-80`}>
                    {relativeDate(alert.daysUntilDue, alert.isOverdue)}
                    {alert.lastDate && ` · Último: ${alert.lastDate}`}
                  </p>
                </div>

                {onSchedule && (
                  <button
                    type="button"
                    onClick={() => onSchedule(alert)}
                    className={`shrink-0 rounded-lg px-3 py-1.5 text-xs font-bold text-white transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 focus-visible:ring-brand-400 ${cls.btnCls}`}
                    aria-label={`Programar cita para ${alert.protocolName}`}
                  >
                    Programar
                  </button>
                )}
              </div>
            </motion.div>
          );
        })}
      </AnimatePresence>
    </div>
  );
}
