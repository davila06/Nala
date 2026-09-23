import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/lib/apiClient";

interface ProductCohortMetric {
  cohort: string;
  canton: string;
  channel: string;
  species: string;
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
  channel: string | null;
  species: string | null;
  activeProtectedPets30Days: number;
  activeProtectedPets90Days: number;
  activeProtectedPets180Days: number;
  cohorts: ProductCohortMetric[];
}

function useProductPerformance() {
  return useQuery({
    queryKey: ["admin-product-performance"],
    queryFn: () =>
      apiClient
        .get<ProductPerformance>("/v1/product-events/performance", {
          params: {
            from: new Date(Date.now() - 90 * 86400000).toISOString(),
          },
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
        <p className="text-sm text-sand-500">
          p50 y p90 calculados por incidente de pérdida, no mezclados por mascota.
        </p>
      </div>
      {data && (
        <div className="mb-5 grid gap-3 sm:grid-cols-3" aria-label="Mascotas activas protegidas">
          {[
            ["30 días", data.activeProtectedPets30Days],
            ["90 días", data.activeProtectedPets90Days],
            ["180 días", data.activeProtectedPets180Days],
          ].map(([window, count]) => (
            <div key={window} className="rounded-xl border border-sand-200 bg-sand-50 px-4 py-3">
              <p className="text-xs uppercase tracking-wide text-sand-500">Activas protegidas · {window}</p>
              <p className="mt-1 text-2xl font-bold tabular-nums text-sand-900">
                {Number(count).toLocaleString("es-CR")}
              </p>
            </div>
          ))}
        </div>
      )}
      {rows.length === 0 ? (
        <p className="text-sm text-sand-500">Aún no hay incidentes en el periodo.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-245 text-left text-sm">
            <thead className="border-b border-sand-200 text-xs uppercase tracking-wide text-sand-500">
              <tr>
                <th className="px-3 py-2">Cohorte</th>
                <th className="px-3 py-2">Cantón</th>
                <th className="px-3 py-2">Canal</th>
                <th className="px-3 py-2">Especie</th>
                <th className="px-3 py-2">Pérdidas</th>
                <th className="px-3 py-2">Reunidas</th>
                <th className="px-3 py-2">Recuperación</th>
                <th className="px-3 py-2">Mediana 1a respuesta</th>
                <th className="px-3 py-2">Mediana reunificación</th>
                <th className="px-3 py-2">p90 1a respuesta</th>
                <th className="px-3 py-2">p90 reunificación</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={`${row.cohort}-${row.canton}`} className="border-b border-sand-100 last:border-0">
                  <td className="px-3 py-3 font-semibold text-sand-900">{row.cohort}</td>
                  <td className="px-3 py-3 text-sand-700">{row.canton}</td>
                  <td className="px-3 py-3 text-sand-700">{row.channel}</td>
                  <td className="px-3 py-3 text-sand-700">{row.species}</td>
                  <td className="px-3 py-3 tabular-nums">{row.lostReports.toLocaleString("es-CR")}</td>
                  <td className="px-3 py-3 tabular-nums">{row.reunitedReports.toLocaleString("es-CR")}</td>
                  <td className="px-3 py-3 font-semibold tabular-nums">{row.recoveryRatePercent.toFixed(1)}%</td>
                  <td className="px-3 py-3 tabular-nums">{minutes(row.medianFirstResponseMinutes)}</td>
                  <td className="px-3 py-3 tabular-nums">{minutes(row.medianReunionMinutes)}</td>
                  <td className="px-3 py-3 tabular-nums">{minutes(row.p90FirstResponseMinutes)}</td>
                  <td className="px-3 py-3 tabular-nums">{minutes(row.p90ReunionMinutes)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
