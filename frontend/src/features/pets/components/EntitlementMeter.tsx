import { useMyEntitlements } from "../hooks/useSubscription";

interface EntitlementMeterProps {
  entitlement: string;
  label: string;
  className?: string;
}

export function EntitlementMeter({ entitlement, label, className = "" }: EntitlementMeterProps) {
  const { data, isLoading, isError } = useMyEntitlements();
  const definition = data?.entitlements[entitlement];

  if (isLoading) {
    return <div className={`${className} animate-pulse text-xs text-slate-400`}>Cargando capacidad...</div>;
  }

  if (isError) {
    return <div className={`${className} text-xs text-rose-600`}>No se pudo consultar la capacidad.</div>;
  }

  if (!definition) {
    return <div className={`${className} text-xs text-slate-500`}>{label}: no incluido</div>;
  }

  if (definition.valueType === "Boolean") {
    return (
      <div className={`${className} text-xs ${definition.booleanValue ? "text-emerald-700" : "text-slate-500"}`}>
        {label}: {definition.booleanValue ? "Disponible" : "No incluido"}
      </div>
    );
  }

  if (definition.numericValue === null) {
    return <div className={`${className} text-xs text-slate-500`}>{label}: disponible</div>;
  }

  const limit = definition.numericValue;
  const consumed = definition.consumed;
  const remaining = Math.max(0, limit - consumed);
  const percentage = limit === 0 ? 100 : Math.min(100, Math.round((consumed / limit) * 100));
  const resetsAt = data.cycleEnd ? new Date(data.cycleEnd).toLocaleDateString() : null;

  return (
    <div className={className} aria-label={`${label}: ${consumed} de ${limit}`}>
      <div className="flex items-center justify-between text-xs text-slate-600">
        <span>{label}</span>
        <span className={`font-semibold ${remaining === 0 ? "text-rose-700" : "text-slate-900"}`}>
          {remaining === 0 ? "Agotado" : `${consumed} / ${limit}`}
        </span>
      </div>
      <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-slate-200">
        <div
          className={`h-full rounded-full ${percentage >= 100 ? "bg-rose-500" : "bg-brand-500"}`}
          style={{ width: `${percentage}%` }}
        />
      </div>
      {resetsAt && <p className="mt-1 text-[11px] text-slate-500">Reinicia el {resetsAt}</p>}
    </div>
  );
}
