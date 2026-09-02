import { useMemo, useState } from "react";
import { MapContainer, TileLayer, CircleMarker } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import {
  useCollarLocationHeatmap,
  useExportCollarLocationHistory,
} from "../hooks/useCollar";

interface CollarLocationHistoryPanelProps {
  collarId: string;
  centerLat: number;
  centerLng: number;
}

const DAYS_OPTIONS = [
  { value: 7, label: "7 días" },
  { value: 14, label: "14 días" },
  { value: 30, label: "30 días" },
];

/** Cheap density approximation via CircleMarker overlap — avoids adding a leaflet.heat dependency. */
function useDensityDots(points: { lat: number; lng: number }[] | undefined) {
  return useMemo(() => {
    if (!points) return [];
    const buckets = new Map<
      string,
      { lat: number; lng: number; count: number }
    >();
    for (const p of points) {
      // Round to ~100m grid so nearby points accumulate into the same dot
      const key = `${p.lat.toFixed(3)}:${p.lng.toFixed(3)}`;
      const existing = buckets.get(key);
      if (existing) existing.count += 1;
      else buckets.set(key, { lat: p.lat, lng: p.lng, count: 1 });
    }
    return Array.from(buckets.values());
  }, [points]);
}

/** Combines heatmap view, date-range selector, and CSV export (merged for UX cohesion). */
export function CollarLocationHistoryPanel({
  collarId,
  centerLat,
  centerLng,
}: CollarLocationHistoryPanelProps) {
  const [days, setDays] = useState(7);
  const { data: points, isLoading } = useCollarLocationHeatmap(collarId, days);
  const exportCsv = useExportCollarLocationHistory();
  const dots = useDensityDots(points);
  const maxCount = Math.max(1, ...dots.map((d) => d.count));

  const handleExport = () => {
    exportCsv.mutate(
      { collarId },
      {
        onSuccess: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement("a");
          a.href = url;
          a.download = `collar-${collarId}-history.csv`;
          a.click();
          URL.revokeObjectURL(url);
        },
      },
    );
  };

  return (
    <div className="space-y-3 rounded-2xl border border-sand-200 bg-surface p-4">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm font-semibold text-sand-800">
          Historial de ubicaciones
        </p>
        <button
          type="button"
          disabled={exportCsv.isPending}
          onClick={handleExport}
          className="rounded-lg bg-sand-100 px-3 py-1.5 text-[10px] font-bold text-sand-700 hover:bg-sand-200 disabled:opacity-40"
        >
          {exportCsv.isPending ? "Exportando…" : "⬇ Exportar CSV"}
        </button>
      </div>

      <div className="flex gap-1">
        {DAYS_OPTIONS.map((opt) => (
          <button
            key={opt.value}
            type="button"
            onClick={() => setDays(opt.value)}
            className={[
              "rounded-xl px-2.5 py-1 text-[10px] font-semibold transition-colors",
              days === opt.value
                ? "bg-brand-600 text-white"
                : "bg-sand-100 text-sand-600 hover:bg-sand-200",
            ].join(" ")}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="h-56 animate-pulse rounded-xl bg-sand-100" />
      ) : (
        <div
          className="overflow-hidden rounded-xl border border-sand-200"
          style={{ height: 260 }}
        >
          <MapContainer
            center={[centerLat, centerLng]}
            zoom={13}
            className="h-full w-full"
          >
            <TileLayer
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              attribution="© OpenStreetMap"
            />
            {dots.map((d, i) => (
              <CircleMarker
                key={i}
                center={[d.lat, d.lng]}
                radius={4 + (d.count / maxCount) * 10}
                pathOptions={{
                  color: "#dc2626",
                  fillColor: "#dc2626",
                  fillOpacity: 0.15 + (d.count / maxCount) * 0.45,
                  weight: 0,
                }}
              />
            ))}
          </MapContainer>
        </div>
      )}
      <p className="text-[10px] text-sand-400">
        {points?.length ?? 0} puntos registrados en los últimos {days} días.
      </p>
    </div>
  );
}
