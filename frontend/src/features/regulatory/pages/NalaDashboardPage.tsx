import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { nalaApi } from "../api/nalaApi";

const today = new Date().toISOString().slice(0, 10);
const monthStart = `${today.slice(0, 7)}-01`;

const metrics = [
  ["activeLostPets", "Pérdidas activas"],
  ["reunitedPets", "Reunificaciones"],
  ["capturedAnimals", "Capturas"],
  ["availableAdoptions", "Adopciones disponibles"],
  ["adoptedAnimals", "Adopciones completadas"],
  ["openWelfareCases", "Casos de bienestar abiertos"],
  ["verifiedMicrochips", "Microchips verificados"],
  ["validCertificates", "Certificados vigentes"],
] as const;

export default function NalaDashboardPage() {
  const [periodStart, setPeriodStart] = useState(monthStart);
  const [periodEnd, setPeriodEnd] = useState(today);
  const [submittedPeriod, setSubmittedPeriod] = useState({
    periodStart: monthStart,
    periodEnd: today,
  });
  const overview = useQuery({
    queryKey: ["nala", "overview", submittedPeriod],
    queryFn: () =>
      nalaApi.getOverview(
        submittedPeriod.periodStart,
        submittedPeriod.periodEnd,
      ),
  });
  const mapLayers = useQuery({
    queryKey: ["nala", "map-layers"],
    queryFn: () =>
      nalaApi.getMapLayers({ south: 8, north: 11.5, west: -86, east: -82.5 }),
  });
  const trends = useQuery({
    queryKey: ["nala", "trends", submittedPeriod],
    queryFn: () =>
      nalaApi.getTrends(submittedPeriod.periodStart, submittedPeriod.periodEnd),
  });

  return (
    <main className="mx-auto max-w-6xl space-y-6 px-4 py-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-brand-600">
            NALA Core
          </p>
          <h1 className="mt-1 text-3xl font-black text-sand-900">
            Resumen operativo
          </h1>
          <p className="mt-1 text-sm text-sand-500">
            Indicadores agregados para coordinación institucional.
          </p>
        </div>
        <form
          className="flex flex-wrap items-end gap-2"
          onSubmit={(event) => {
            event.preventDefault();
            setSubmittedPeriod({ periodStart, periodEnd });
          }}
        >
          <label className="text-xs font-semibold text-sand-600">
            Desde
            <input
              type="date"
              value={periodStart}
              onChange={(event) => setPeriodStart(event.target.value)}
              className="mt-1 block rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm"
            />
          </label>
          <label className="text-xs font-semibold text-sand-600">
            Hasta
            <input
              type="date"
              value={periodEnd}
              onChange={(event) => setPeriodEnd(event.target.value)}
              className="mt-1 block rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm"
            />
          </label>
          <button
            type="submit"
            className="rounded-xl bg-sand-900 px-4 py-2 text-sm font-bold text-white hover:bg-sand-700"
          >
            Actualizar
          </button>
        </form>
      </header>

      {overview.isLoading && (
        <div className="h-48 animate-pulse rounded-2xl bg-sand-100" />
      )}
      {overview.isError && (
        <div className="rounded-2xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700">
          No se pudo cargar el resumen NALA.
        </div>
      )}
      {overview.data && (
        <>
          <div className="rounded-2xl border border-sand-200 bg-surface p-4 text-sm text-sand-600">
            <span className="font-semibold text-sand-900">
              Última actualización:
            </span>{" "}
            {new Date(overview.data.generatedAt).toLocaleString("es-CR")}
            {overview.data.isSuppressed && (
              <span className="ml-2 rounded-full bg-warn-100 px-2 py-1 text-xs font-bold text-warn-800">
                Datos suprimidos por privacidad
              </span>
            )}
          </div>
          <section
            className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4"
            aria-label="Indicadores NALA"
          >
            {metrics.map(([key, label]) => (
              <article
                key={key}
                className="rounded-2xl border border-sand-200 bg-surface p-5 shadow-sm"
              >
                <p className="text-3xl font-black tabular-nums text-sand-900">
                  {overview.data[key]}
                </p>
                <p className="mt-1 text-sm text-sand-500">{label}</p>
              </article>
            ))}
          </section>
          <section
            className="rounded-2xl border border-sand-200 bg-surface p-5 shadow-sm"
            aria-label="Capas operativas agregadas"
          >
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <div>
                <h2 className="text-lg font-black text-sand-900">
                  Capas operativas agregadas
                </h2>
                <p className="mt-1 text-xs text-sand-500">
                  Celdas generalizadas de pérdidas, bienestar y clínicas. No se
                  muestran domicilios ni identificadores.
                </p>
              </div>
              <span className="text-xs font-semibold text-sand-500">
                {mapLayers.data?.length ?? 0} celdas
              </span>
            </div>
            {mapLayers.isLoading && (
              <div className="mt-4 h-20 animate-pulse rounded-xl bg-sand-100" />
            )}
            {mapLayers.isError && (
              <p className="mt-4 text-sm text-danger-700">
                No se pudieron cargar las capas operativas.
              </p>
            )}
            {mapLayers.data && (
              <div className="mt-4 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
                {mapLayers.data.map((cell) => (
                  <div
                    key={`${cell.layer}-${cell.canton}-${cell.latitude}-${cell.longitude}`}
                    className="rounded-xl border border-sand-100 bg-sand-50 p-3"
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-xs font-bold text-sand-800">
                        {cell.layer}
                      </span>
                      <span className="text-xs font-black tabular-nums text-sand-900">
                        {cell.isSuppressed ? "Suprimido" : cell.count}
                      </span>
                    </div>
                    <p className="mt-1 text-[11px] text-sand-500">
                      {cell.canton} · celda generalizada
                    </p>
                  </div>
                ))}
              </div>
            )}
          </section>
          <section
            className="rounded-2xl border border-sand-200 bg-surface p-5 shadow-sm"
            aria-label="Tendencias NALA"
          >
            <h2 className="text-lg font-black text-sand-900">
              Tendencias del periodo
            </h2>
            <div className="mt-4 overflow-x-auto">
              <table className="w-full min-w-[640px] text-left text-xs">
                <caption className="sr-only">
                  Tendencias diarias agregadas
                </caption>
                <thead className="border-b border-sand-200 text-sand-500">
                  <tr>
                    <th className="px-2 py-2">Fecha</th>
                    <th className="px-2 py-2">Pérdidas</th>
                    <th className="px-2 py-2">Reunificaciones</th>
                    <th className="px-2 py-2">Capturas</th>
                    <th className="px-2 py-2">Adopciones</th>
                    <th className="px-2 py-2">Bienestar</th>
                  </tr>
                </thead>
                <tbody>
                  {(trends.data ?? []).map((point) => (
                    <tr key={point.date} className="border-b border-sand-100">
                      <td className="px-2 py-2 font-semibold text-sand-700">
                        {point.date}
                      </td>
                      <td className="px-2 py-2 tabular-nums">
                        {point.lostReports}
                      </td>
                      <td className="px-2 py-2 tabular-nums">
                        {point.reunitedPets}
                      </td>
                      <td className="px-2 py-2 tabular-nums">
                        {point.captures}
                      </td>
                      <td className="px-2 py-2 tabular-nums">
                        {point.adoptedAnimals}
                      </td>
                      <td className="px-2 py-2 tabular-nums">
                        {point.welfareCases}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {trends.isError && (
              <p className="mt-3 text-sm text-danger-700">
                No se pudieron cargar las tendencias.
              </p>
            )}
          </section>
        </>
      )}
    </main>
  );
}
