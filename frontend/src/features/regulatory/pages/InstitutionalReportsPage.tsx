import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { nalaApi, type ExportFormat, type ReportType } from "../api/nalaApi";

const today = new Date().toISOString().slice(0, 10);
const monthStart = `${today.slice(0, 7)}-01`;

export default function InstitutionalReportsPage() {
  const queryClient = useQueryClient();
  const catalog = useQuery({
    queryKey: ["regulatory", "catalog"],
    queryFn: nalaApi.getCatalog,
  });
  const exports = useQuery({
    queryKey: ["regulatory", "exports"],
    queryFn: nalaApi.getExports,
  });
  const [reportType, setReportType] = useState<ReportType>("NalaOverview");
  const [format, setFormat] = useState<ExportFormat>("Pdf");
  const [periodStart, setPeriodStart] = useState(monthStart);
  const [periodEnd, setPeriodEnd] = useState(today);
  const [canton, setCanton] = useState("");
  const request = useMutation({
    mutationFn: () =>
      nalaApi.requestExport({
        reportType,
        format,
        scope: "Institutional",
        periodStart,
        periodEnd,
        canton: canton || undefined,
        idempotencyKey: crypto.randomUUID(),
      }),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["regulatory", "exports"],
      }),
  });
  const generate = useMutation({
    mutationFn: nalaApi.generateExport,
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["regulatory", "exports"],
      }),
  });

  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-8">
      <header>
        <p className="text-xs font-bold uppercase tracking-[0.16em] text-brand-600">
          SENASA-ready
        </p>
        <h1 className="mt-1 text-3xl font-black text-sand-900">
          Reportes institucionales
        </h1>
        <p className="mt-1 text-sm text-sand-500">
          Exports agregados, auditables y sin envío oficial externo.
        </p>
      </header>
      <form
        className="grid gap-3 rounded-2xl border border-sand-200 bg-surface p-5 md:grid-cols-5"
        onSubmit={(event) => {
          event.preventDefault();
          request.mutate();
        }}
      >
        <label className="text-xs font-semibold text-sand-600">
          Reporte
          <select
            value={reportType}
            onChange={(event) =>
              setReportType(event.target.value as ReportType)
            }
            className="mt-1 block w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm"
          >
            {(catalog.data ?? []).map((item) => (
              <option key={item.reportType} value={item.reportType}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
        <label className="text-xs font-semibold text-sand-600">
          Formato
          <select
            value={format}
            onChange={(event) => setFormat(event.target.value as ExportFormat)}
            className="mt-1 block w-full rounded-xl border border-sand-200 bg-white px-3 py-2 text-sm"
          >
            <option value="Pdf">PDF</option>
            <option value="Csv">CSV</option>
            <option value="Json">JSON</option>
          </select>
        </label>
        <label className="text-xs font-semibold text-sand-600">
          Desde
          <input
            type="date"
            value={periodStart}
            onChange={(event) => setPeriodStart(event.target.value)}
            className="mt-1 block w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </label>
        <label className="text-xs font-semibold text-sand-600">
          Hasta
          <input
            type="date"
            value={periodEnd}
            onChange={(event) => setPeriodEnd(event.target.value)}
            className="mt-1 block w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </label>
        <label className="text-xs font-semibold text-sand-600">
          Cantón
          <input
            value={canton}
            onChange={(event) => setCanton(event.target.value)}
            placeholder="Scope autorizado"
            className="mt-1 block w-full rounded-xl border border-sand-200 px-3 py-2 text-sm"
          />
        </label>
        <button
          type="submit"
          disabled={request.isPending}
          className="rounded-xl bg-sand-900 px-4 py-2 text-sm font-bold text-white disabled:opacity-50 md:col-span-5"
        >
          {request.isPending ? "Solicitando..." : "Solicitar export"}
        </button>
      </form>
      {request.isError && (
        <p className="rounded-xl bg-danger-50 p-3 text-sm text-danger-700">
          No fue posible solicitar el export.
        </p>
      )}
      <section className="space-y-3">
        <h2 className="text-lg font-black text-sand-900">Mis exports</h2>
        {(exports.data?.items ?? []).map((item) => (
          <article
            key={item.id}
            className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-sand-200 bg-surface p-4"
          >
            <div>
              <p className="font-bold text-sand-900">
                {item.exportCode} · {item.reportType}
              </p>
              <p className="text-xs text-sand-500">
                {item.format} · {item.status} · {item.periodStart} a{" "}
                {item.periodEnd}
              </p>
            </div>
            <div className="flex gap-2">
              {item.status === "Requested" && (
                <button
                  type="button"
                  onClick={() => generate.mutate(item.id)}
                  className="rounded-xl bg-trust-100 px-3 py-1.5 text-xs font-bold text-trust-700"
                >
                  Generar
                </button>
              )}
              {item.status === "Completed" && (
                <button
                  type="button"
                  onClick={() =>
                    void nalaApi.downloadExport(
                      item.id,
                      `${item.exportCode}.${item.format.toLowerCase()}`,
                    )
                  }
                  className="rounded-xl bg-sand-900 px-3 py-1.5 text-xs font-bold text-white"
                >
                  Descargar
                </button>
              )}
            </div>
          </article>
        ))}
        {!exports.isLoading && !exports.data?.items.length && (
          <p className="text-sm text-sand-500">No hay exports solicitados.</p>
        )}
      </section>
    </main>
  );
}
