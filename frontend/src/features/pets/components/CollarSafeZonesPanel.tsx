import { useState } from "react";
import {
  MapContainer,
  TileLayer,
  Polygon,
  Marker,
  useMapEvents,
} from "react-leaflet";
import "leaflet/dist/leaflet.css";
import type { CollarSafeZoneDto, CollarSafeZonePoint } from "../api/collarApi";
import {
  useCollarSafeZones,
  useCreateCollarSafeZone,
  useDeleteCollarSafeZone,
  useUpdateCollarSafeZone,
} from "../hooks/useCollar";

interface CollarSafeZonesPanelProps {
  collarId: string;
  centerLat: number;
  centerLng: number;
}

/** Click-to-draw polygon capture — avoids adding a leaflet-draw dependency. */
function DrawClickCapture({
  onPointAdded,
}: {
  onPointAdded: (point: CollarSafeZonePoint) => void;
}) {
  useMapEvents({
    click: (e) => onPointAdded({ lat: e.latlng.lat, lng: e.latlng.lng }),
  });
  return null;
}

function parsePolygon(polygonJson: string): CollarSafeZonePoint[] {
  try {
    return JSON.parse(polygonJson) as CollarSafeZonePoint[];
  } catch {
    return [];
  }
}

/** Combines the zone list, drawing map, and create/edit form (merged for UX cohesion). */
export function CollarSafeZonesPanel({
  collarId,
  centerLat,
  centerLng,
}: CollarSafeZonesPanelProps) {
  const { data: zones } = useCollarSafeZones(collarId);
  const create = useCreateCollarSafeZone(collarId);
  const update = useUpdateCollarSafeZone(collarId);
  const remove = useDeleteCollarSafeZone(collarId);

  const [isDrawing, setIsDrawing] = useState(false);
  const [drawPoints, setDrawPoints] = useState<CollarSafeZonePoint[]>([]);
  const [zoneName, setZoneName] = useState("");

  const handleSave = () => {
    if (drawPoints.length < 3 || !zoneName.trim()) return;
    create.mutate(
      { name: zoneName.trim(), points: drawPoints },
      {
        onSuccess: () => {
          setIsDrawing(false);
          setDrawPoints([]);
          setZoneName("");
        },
      },
    );
  };

  const toggleEnabled = (zone: CollarSafeZoneDto) => {
    update.mutate({
      zoneId: zone.id,
      name: zone.name,
      points: parsePolygon(zone.polygonJson),
      enabled: !zone.enabled,
    });
  };

  return (
    <div className="space-y-3 rounded-2xl border border-sand-200 bg-surface p-4">
      <p className="text-sm font-semibold text-sand-800">Zonas seguras</p>

      {zones && zones.length > 0 && (
        <ul className="space-y-2">
          {zones.map((zone) => (
            <li
              key={zone.id}
              className="flex items-center justify-between gap-2 rounded-xl border border-sand-200 px-3 py-2"
            >
              <span className="text-xs font-semibold text-sand-800">
                {zone.name}
              </span>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => toggleEnabled(zone)}
                  className={`rounded-lg px-2 py-1 text-[10px] font-bold ${
                    zone.enabled
                      ? "bg-green-100 text-green-700"
                      : "bg-sand-100 text-sand-500"
                  }`}
                >
                  {zone.enabled ? "Activa" : "Inactiva"}
                </button>
                <button
                  type="button"
                  onClick={() => remove.mutate(zone.id)}
                  className="text-xs text-red-500 underline hover:text-red-700"
                >
                  Eliminar
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {!isDrawing ? (
        <button
          type="button"
          onClick={() => setIsDrawing(true)}
          className="text-xs font-semibold text-brand-600 underline hover:text-brand-800"
        >
          + Agregar zona segura
        </button>
      ) : (
        <div className="space-y-2">
          <p className="text-xs text-sand-500">
            Toca el mapa para agregar puntos (mínimo 3) y dibujar el perímetro.
          </p>
          <div
            className="overflow-hidden rounded-xl border border-sand-200"
            style={{ height: 220 }}
          >
            <MapContainer
              center={[centerLat, centerLng]}
              zoom={15}
              className="h-full w-full"
            >
              <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution="© OpenStreetMap"
              />
              <DrawClickCapture
                onPointAdded={(p) => setDrawPoints((prev) => [...prev, p])}
              />
              {drawPoints.map((p, i) => (
                <Marker key={i} position={[p.lat, p.lng]} />
              ))}
              {drawPoints.length >= 3 && (
                <Polygon
                  positions={drawPoints.map((p): [number, number] => [
                    p.lat,
                    p.lng,
                  ])}
                  pathOptions={{ color: "#059669" }}
                />
              )}
            </MapContainer>
          </div>
          <input
            type="text"
            value={zoneName}
            onChange={(e) => setZoneName(e.target.value)}
            placeholder="Nombre de la zona (ej: Casa)"
            className="w-full rounded-xl border border-sand-200 px-3 py-2 text-sm outline-none focus:border-brand-400"
          />
          <div className="flex gap-2">
            <button
              type="button"
              disabled={
                drawPoints.length < 3 || !zoneName.trim() || create.isPending
              }
              onClick={handleSave}
              className="rounded-xl bg-brand-600 px-3 py-1.5 text-xs font-bold text-white disabled:opacity-40 hover:bg-brand-700"
            >
              {create.isPending ? "Guardando…" : "Guardar zona"}
            </button>
            <button
              type="button"
              onClick={() => {
                setIsDrawing(false);
                setDrawPoints([]);
                setZoneName("");
              }}
              className="text-xs text-sand-500 underline"
            >
              Cancelar
            </button>
            {drawPoints.length > 0 && (
              <button
                type="button"
                onClick={() => setDrawPoints((prev) => prev.slice(0, -1))}
                className="text-xs text-sand-500 underline"
              >
                Deshacer punto
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
