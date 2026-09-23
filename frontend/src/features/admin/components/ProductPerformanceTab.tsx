import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/lib/apiClient";

interface ProductCohortMetric {
  cohort: string;
  canton: string;
  registeredPets: number;
  activatedPets: number;
  lostReports: number;
  reunitedReports: number;
  medianFirstResponseMinutes: number | null;
  medianReunionMinutes: number | null;
  p90FirstResponseMinutes: number | null;
  p90ReunionMinutes: number | null;
  recoveryRatePercent: number;
  firstResponseSloPercent: number;
}

interface ProductPerformance {
  from: string;
  to: string;
  canton: string | null;
  cohorts: ProductCohortMetric[];
}

function useProductPerformance() {
  return useQuery({
    queryKey: ["admin-product-performance"],
    queryFn: () =>
      apiClient
        .get<ProductPerformance>("/v1/product-events/performance", {
          params: { from: new Date(Date.now() - 90 * 86400000).toISOString() },
        })
        .then((response) => response.data),
    staleTime: 60_000,
  });
}

function minutes(value: number | null): string {
  if (value === null) return "Sin datos";
  if (value < 60) return `${Math.round(value)} min`;
  return `${(value / 60).toFixed(1)} h`;
}

export function ProductPerformanceTab() {
  const { data, isLoading, isError } = useProductPerformance();

  if (isLoading) return <p className="p-6 text-sm text-sand-500">Cargando desempeño...</p>;
  if (isError) {
    return (
      <p role="alert" className="p-6 text-sm text-danger-600">
        No se pudo cargar el desempeño de recuperación.
      </p>
    );
  }

  const rows = data?.cohorts ?? [];
  return (
    <section className="rounded-2xl border border-sand-200 bg-surface p-5 shadow-sm">
      <div className="mb-5">
        <h2 className="text-lg font-bold text-sand-900">Desempeño de recuperación</h2>
        <p className="text-sm text-sand-500">Medianas calculadas por incidente de pérdida, no mezcladas por mascota.</p>
      </div>
      {rows.length === 0 ? (
        <p className="text-sm text-sand-500">Aún no hay incidentes en el periodo.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="border-b border-sand-200 text-xs uppercase tracking-wide text-sand-500">
              <tr>
                <th className="px-3 py-2">Cohorte</th>
                <th className="px-3 py-2">Cantón</th>
                <th className="px-3 py-2">Pérdidas</th>
                <th className="px-3 py-2">Reunidas</th>
                <th className="px-3 py-2">Recuperación</th>
                <th className="px-3 py-2">Mediana 1a respuesta</th>
                <th className="px-3 py-2">Mediana reunificación</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={`${row.cohort}-${row.canton}`} className="border-b border-sand-100 last:border-0">
                  <td className="px-3 py-3 font-semibold text-sand-900">{row.cohort}</td>
                  <td className="px-3 py-3 text-sand-700">{row.canton}</td>
                  <td className="px-3 py-3 tabular-nums">{row.lostReports.toLocaleString("es-CR")}</td>
                  <td className="px-3 py-3 tabular-nums">{row.reunitedReports.toLocaleString("es-CR")}</td>
                  <td className="px-3 py-3 font-semibold tabular-nums">{row.recoveryRatePercent.toFixed(1)}%</td>
                  <td className="px-3 py-3 tabular-nums">{minutes(row.medianFirstResponseMinutes)}</td>
                  <td className="px-3 py-3 tabular-nums">{minutes(row.medianReunionMinutes)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
