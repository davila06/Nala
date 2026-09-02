import { useRef, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/lib/apiClient";

interface CollarTagRow {
  id: string;
  serial: string;
  collarId: string | null;
  status: string;
  firmwareVersion: string;
  manufacturedAt: string;
  soldAt: string | null;
  activatedAt: string | null;
  lastPingAt: string | null;
}

interface PageResult {
  total: number;
  items: CollarTagRow[];
}

interface CollarTagMetrics {
  totalSerials: number;
  unactivatedCount: number;
  activatedCount: number;
  deactivatedCount: number;
  soldLast30Days: number;
  deadInventoryCount: number;
}

interface BulkActionResult {
  succeeded: number;
  failed: number;
  errors: string[];
}

const STATUS_COLORS: Record<string, string> = {
  Unactivated: "bg-sand-100 text-sand-700",
  Activated: "bg-rescue-100 text-rescue-700",
  Deactivated: "bg-amber-100 text-amber-700",
  Replaced: "bg-red-100 text-red-700",
};

export function CollarTagInventorySection() {
  const qc = useQueryClient();
  const [skip, setSkip] = useState(0);
  const TAKE = 50;
  const [newSerial, setNewSerial] = useState("");
  const [newFw, setNewFw] = useState("1.0.0");
  const [csvError, setCsvError] = useState<string | null>(null);
  const [csvResult, setCsvResult] = useState<{
    imported: number;
    skipped: number;
    errors: string[];
  } | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  // Advanced filters
  const [searchSerial, setSearchSerial] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [bulkResult, setBulkResult] = useState<BulkActionResult | null>(null);

  const { data: metrics } = useQuery({
    queryKey: ["admin-collar-tags-metrics"],
    queryFn: () =>
      apiClient
        .get<CollarTagMetrics>("/admin/collar-tags/metrics")
        .then((r) => r.data),
  });

  const { data, isLoading } = useQuery({
    queryKey: ["admin-collar-tags", skip, searchSerial, statusFilter],
    queryFn: () =>
      apiClient
        .get<PageResult>("/admin/collar-tags", {
          params: {
            skip,
            take: TAKE,
            serial: searchSerial || undefined,
            status: statusFilter || undefined,
          },
        })
        .then((r) => r.data),
  });

  const register = useMutation({
    mutationFn: () =>
      apiClient.post("/admin/collar-tags", {
        serial: newSerial,
        firmwareVersion: newFw,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin-collar-tags"] });
      setNewSerial("");
    },
  });

  const markSold = useMutation({
    mutationFn: (serial: string) =>
      apiClient.post(`/admin/collar-tags/${serial}/mark-sold`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-collar-tags"] }),
  });

  const revoke = useMutation({
    mutationFn: (serial: string) =>
      apiClient.post(`/admin/collar-tags/${serial}/revoke`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-collar-tags"] }),
  });

  const bulkMarkSold = useMutation({
    mutationFn: (serials: string[]) =>
      apiClient
        .post<BulkActionResult>("/admin/collar-tags/bulk-mark-sold", {
          serials,
        })
        .then((r) => r.data),
    onSuccess: (result) => {
      setBulkResult(result);
      setSelected(new Set());
      qc.invalidateQueries({ queryKey: ["admin-collar-tags"] });
      qc.invalidateQueries({ queryKey: ["admin-collar-tags-metrics"] });
    },
  });

  const bulkRevoke = useMutation({
    mutationFn: (serials: string[]) =>
      apiClient
        .post<BulkActionResult>("/admin/collar-tags/bulk-revoke", { serials })
        .then((r) => r.data),
    onSuccess: (result) => {
      setBulkResult(result);
      setSelected(new Set());
      qc.invalidateQueries({ queryKey: ["admin-collar-tags"] });
      qc.invalidateQueries({ queryKey: ["admin-collar-tags-metrics"] });
    },
  });

  const toggleSelected = (serial: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(serial)) next.delete(serial);
      else next.add(serial);
      return next;
    });
  };

  const bulkImport = useMutation({
    mutationFn: (file: File) => {
      const fd = new FormData();
      fd.append("file", file);
      return apiClient
        .post<{
          imported: number;
          skipped: number;
          errors: string[];
        }>("/admin/collar-tags/bulk-import", fd, {
          headers: { "Content-Type": "multipart/form-data" },
        })
        .then((r) => r.data);
    },
    onSuccess: (result) => {
      setCsvResult(result);
      qc.invalidateQueries({ queryKey: ["admin-collar-tags"] });
    },
    onError: (err) => setCsvError(String(err)),
  });

  return (
    <div className="space-y-6">
      {/* Metrics cards */}
      {metrics && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
          <MetricCard label="Total" value={metrics.totalSerials} />
          <MetricCard label="Sin activar" value={metrics.unactivatedCount} />
          <MetricCard label="Activados" value={metrics.activatedCount} />
          <MetricCard label="Desactivados" value={metrics.deactivatedCount} />
          <MetricCard label="Vendidos (30d)" value={metrics.soldLast30Days} />
          <MetricCard
            label="Inventario muerto"
            value={metrics.deadInventoryCount}
            warn={metrics.deadInventoryCount > 0}
          />
        </div>
      )}

      {metrics && metrics.deadInventoryCount > 0 && (
        <div className="rounded-xl border border-amber-300 bg-amber-50 px-4 py-2 text-xs text-amber-800">
          ⚠️ {metrics.deadInventoryCount} serial(es) vendidos hace más de 90
          días y aún sin activar — seguimiento recomendado.
        </div>
      )}

      {/* Add single serial */}
      <div className="rounded-2xl border border-sand-200 bg-surface p-4 space-y-3">
        <p className="text-sm font-semibold text-sand-800">Registrar serial</p>
        <div className="flex gap-2 flex-wrap">
          <input
            type="text"
            value={newSerial}
            onChange={(e) => setNewSerial(e.target.value.toUpperCase())}
            placeholder="PT-XXXX-0000000"
            className="rounded-xl border border-sand-200 px-3 py-2 font-mono text-xs uppercase flex-1 min-w-0"
            maxLength={15}
          />
          <input
            type="text"
            value={newFw}
            onChange={(e) => setNewFw(e.target.value)}
            placeholder="1.0.0"
            className="rounded-xl border border-sand-200 px-3 py-2 text-xs w-24"
          />
          <button
            type="button"
            disabled={newSerial.length < 13 || register.isPending}
            onClick={() => register.mutate()}
            className="rounded-xl bg-brand-600 px-4 py-2 text-xs font-bold text-white disabled:opacity-40 hover:bg-brand-700"
          >
            Registrar
          </button>
        </div>
        {register.isError && (
          <p className="text-xs text-red-600">{String(register.error)}</p>
        )}
      </div>

      {/* Bulk CSV import */}
      <div className="rounded-2xl border border-sand-200 bg-surface p-4 space-y-3">
        <p className="text-sm font-semibold text-sand-800">
          Import masivo (CSV)
        </p>
        <p className="text-xs text-sand-500">
          Formato: <code>serial,firmwareVersion</code> — una fila por línea, sin
          encabezado.
        </p>
        <div className="flex gap-2 items-center">
          <input
            ref={fileRef}
            type="file"
            accept=".csv,text/csv"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) {
                setCsvError(null);
                setCsvResult(null);
                bulkImport.mutate(file);
              }
            }}
          />
          <button
            type="button"
            disabled={bulkImport.isPending}
            onClick={() => fileRef.current?.click()}
            className="rounded-xl border border-sand-300 bg-sand-50 px-4 py-2 text-xs font-semibold text-sand-700 hover:bg-sand-100 disabled:opacity-40"
          >
            {bulkImport.isPending ? "Importando…" : "Subir CSV"}
          </button>
        </div>
        {csvResult && (
          <p className="text-xs text-green-700">
            ✅ {csvResult.imported} importados, {csvResult.skipped} ya existían
            {csvResult.errors.length > 0 &&
              ` — ${csvResult.errors.length} errores`}
          </p>
        )}
        {csvError && <p className="text-xs text-red-600">{csvError}</p>}
      </div>

      {/* Search + filters */}
      <div className="flex flex-wrap items-center gap-2 rounded-2xl border border-sand-200 bg-surface p-4">
        <input
          type="text"
          value={searchSerial}
          onChange={(e) => {
            setSearchSerial(e.target.value.toUpperCase());
            setSkip(0);
          }}
          placeholder="Buscar por serial…"
          className="flex-1 min-w-[160px] rounded-xl border border-sand-200 px-3 py-2 font-mono text-xs uppercase"
        />
        <select
          value={statusFilter}
          onChange={(e) => {
            setStatusFilter(e.target.value);
            setSkip(0);
          }}
          className="rounded-xl border border-sand-200 px-3 py-2 text-xs"
        >
          <option value="">Todos los estados</option>
          <option value="Unactivated">Sin activar</option>
          <option value="Activated">Activado</option>
          <option value="Deactivated">Desactivado</option>
          <option value="Replaced">Reemplazado</option>
        </select>
      </div>

      {/* Bulk actions toolbar */}
      {selected.size > 0 && (
        <div className="flex items-center justify-between gap-2 rounded-2xl border border-brand-200 bg-brand-50 px-4 py-2">
          <span className="text-xs font-semibold text-brand-800">
            {selected.size} seleccionado(s)
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={bulkMarkSold.isPending}
              onClick={() => bulkMarkSold.mutate(Array.from(selected))}
              className="rounded-lg bg-sand-100 px-3 py-1.5 text-[10px] font-bold text-sand-700 hover:bg-sand-200 disabled:opacity-40"
            >
              Marcar vendidos
            </button>
            <button
              type="button"
              disabled={bulkRevoke.isPending}
              onClick={() => bulkRevoke.mutate(Array.from(selected))}
              className="rounded-lg bg-red-50 px-3 py-1.5 text-[10px] font-bold text-red-700 hover:bg-red-100 disabled:opacity-40"
            >
              Revocar
            </button>
            <button
              type="button"
              onClick={() => setSelected(new Set())}
              className="text-[10px] text-sand-500 underline"
            >
              Limpiar
            </button>
          </div>
        </div>
      )}
      {bulkResult && (
        <p className="text-xs text-sand-600">
          ✅ {bulkResult.succeeded} exitosos, {bulkResult.failed} fallidos
          {bulkResult.errors.length > 0 && ` — ${bulkResult.errors.join("; ")}`}
        </p>
      )}

      {/* Inventory table */}
      <div className="rounded-2xl border border-sand-200 bg-surface overflow-hidden">
        <div className="flex items-center justify-between px-4 py-3 border-b border-sand-100">
          <p className="text-sm font-semibold text-sand-800">
            Inventario{data ? ` (${data.total})` : ""}
          </p>
        </div>
        {isLoading ? (
          <div className="h-32 animate-pulse bg-sand-50" />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-sand-100 bg-sand-50 text-left">
                  <th className="px-4 py-2">
                    <input
                      type="checkbox"
                      checked={
                        (data?.items.length ?? 0) > 0 &&
                        data!.items.every((t) => selected.has(t.serial))
                      }
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelected(
                            new Set(data?.items.map((t) => t.serial) ?? []),
                          );
                        } else {
                          setSelected(new Set());
                        }
                      }}
                    />
                  </th>
                  <th className="px-4 py-2 font-semibold text-sand-600">
                    Serial
                  </th>
                  <th className="px-4 py-2 font-semibold text-sand-600">
                    Estado
                  </th>
                  <th className="px-4 py-2 font-semibold text-sand-600">
                    Firmware
                  </th>
                  <th className="px-4 py-2 font-semibold text-sand-600">
                    Último ping
                  </th>
                  <th className="px-4 py-2 font-semibold text-sand-600">
                    Acciones
                  </th>
                </tr>
              </thead>
              <tbody>
                {data?.items.map((tag) => (
                  <tr
                    key={tag.id}
                    className="border-b border-sand-50 hover:bg-sand-25"
                  >
                    <td className="px-4 py-2">
                      <input
                        type="checkbox"
                        checked={selected.has(tag.serial)}
                        onChange={() => toggleSelected(tag.serial)}
                      />
                    </td>
                    <td className="px-4 py-2 font-mono font-semibold text-sand-900">
                      {tag.serial}
                    </td>
                    <td className="px-4 py-2">
                      <span
                        className={`rounded-full px-2 py-0.5 text-[10px] font-bold ${STATUS_COLORS[tag.status] ?? "bg-sand-100 text-sand-600"}`}
                      >
                        {tag.status}
                      </span>
                    </td>
                    <td className="px-4 py-2 text-sand-500">
                      {tag.firmwareVersion}
                    </td>
                    <td className="px-4 py-2 text-sand-500">
                      {tag.lastPingAt
                        ? new Date(tag.lastPingAt).toLocaleString("es-CR")
                        : "—"}
                    </td>
                    <td className="px-4 py-2">
                      <div className="flex gap-2">
                        {tag.status === "Unactivated" && (
                          <button
                            type="button"
                            disabled={markSold.isPending}
                            onClick={() => markSold.mutate(tag.serial)}
                            className="rounded-lg bg-sand-100 px-2 py-1 text-[10px] font-bold text-sand-700 hover:bg-sand-200 disabled:opacity-40"
                          >
                            Marcar vendido
                          </button>
                        )}
                        {tag.status === "Activated" && (
                          <button
                            type="button"
                            disabled={revoke.isPending}
                            onClick={() => revoke.mutate(tag.serial)}
                            className="rounded-lg bg-red-50 px-2 py-1 text-[10px] font-bold text-red-700 hover:bg-red-100 disabled:opacity-40"
                          >
                            Revocar
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {/* Pagination */}
        {data && data.total > TAKE && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-sand-100">
            <button
              type="button"
              disabled={skip === 0}
              onClick={() => setSkip(Math.max(0, skip - TAKE))}
              className="text-xs text-brand-600 disabled:opacity-40"
            >
              ← Anterior
            </button>
            <span className="text-xs text-sand-500">
              {skip + 1}–{Math.min(skip + TAKE, data.total)} de {data.total}
            </span>
            <button
              type="button"
              disabled={skip + TAKE >= data.total}
              onClick={() => setSkip(skip + TAKE)}
              className="text-xs text-brand-600 disabled:opacity-40"
            >
              Siguiente →
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

function MetricCard({
  label,
  value,
  warn,
}: {
  label: string;
  value: number;
  warn?: boolean;
}) {
  return (
    <div
      className={`rounded-xl border p-3 ${warn ? "border-amber-300 bg-amber-50" : "border-sand-200 bg-surface"}`}
    >
      <p
        className={`text-lg font-bold ${warn ? "text-amber-800" : "text-sand-900"}`}
      >
        {value}
      </p>
      <p className="text-[10px] text-sand-500">{label}</p>
    </div>
  );
}
