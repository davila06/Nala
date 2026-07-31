import { useState } from "react";
import { motion } from "framer-motion";
import { MapContainer, TileLayer, Marker, Popup, Polyline, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import { useCollarHistory, useCollarStatus, useRegisterCollar } from "../hooks/useCollar";

interface CollarGpsTabProps {
  petId: string;
  isOwner: boolean;
}

const PROVIDER_LABELS = {
  Own: "PawTrack GPS",
  Tractive: "Tractive",
  Kippy: "Kippy",
  Generic: "Genérico",
};

const HOURS_OPTIONS = [
  { value: 1,   label: "Última hora" },
  { value: 6,   label: "6 horas" },
  { value: 12,  label: "12 horas" },
  { value: 24,  label: "24 horas" },
  { value: 72,  label: "3 días" },
  { value: 168, label: "7 días" },
];

// Fit map to polyline bounds when track changes
function FitBounds({ positions }: { positions: [number, number][] }) {
  const map = useMap();
  if (positions.length >= 2) {
    map.fitBounds(positions, { padding: [24, 24], maxZoom: 16 });
  }
  return null;
}

export function CollarGpsTab({ petId, isOwner }: CollarGpsTabProps) {
  const { data: collar, isLoading } = useCollarStatus(petId);
  const { mutateAsync: register, isPending } = useRegisterCollar();
  const [showSetup, setShowSetup] = useState(false);
  const [deviceId, setDeviceId] = useState("");
  const [hours, setHours] = useState(24);

  const { data: history, isFetching: historyFetching } = useCollarHistory(
    petId,
    hours,
  );

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-2xl bg-sand-100" />;
  }

  if (!collar) {
    return (
      <div className="rounded-2xl border border-dashed border-sand-200 bg-surface-warm p-6 text-center">
        <span className="text-4xl" aria-hidden="true">
          📡
        </span>
        <h3 className="mt-2 text-sm font-semibold text-sand-700">
          Sin dispositivo GPS registrado
        </h3>
        <p className="mt-1 text-xs text-sand-400">
          Conecta un collar GPS para ver la posición en tiempo real y el
          historial de trayectoria.
        </p>
        {isOwner && (
          <button
            type="button"
            onClick={() => setShowSetup(true)}
            className="mt-4 rounded-xl bg-brand-600 px-4 py-2 text-xs font-bold text-white hover:bg-brand-700 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Conectar dispositivo GPS →
          </button>
        )}

        {/* Setup form */}
        {showSetup && (
          <div className="mt-4 rounded-2xl border border-brand-200 bg-surface p-4 text-left space-y-3">
            <p className="text-sm font-semibold text-sand-800">
              Configurar collar GPS
            </p>
            <p className="text-xs text-sand-500">
              Compatible con <strong>Tractive</strong>, <strong>Kippy</strong> o
              cualquier GPS genérico. Ingresa el ID del dispositivo impreso en
              el collar.
            </p>
            {/* Tractive OAuth2 connect — redirects to Tractive consent screen */}
            <a
              href={`${import.meta.env.VITE_API_URL}/api/collars/tractive/connect?petId=${petId}`}
              className="flex items-center gap-2 rounded-xl border border-sand-200 bg-surface px-4 py-2.5 text-xs font-semibold text-sand-700 hover:bg-sand-50 transition-colors"
            >
              <span aria-hidden="true">📡</span> Conectar con Tractive →
            </a>
            <div className="flex items-center gap-2">
              <hr className="flex-1 border-sand-200" />
              <span className="text-[10px] text-sand-400">
                o ingresa manualmente
              </span>
              <hr className="flex-1 border-sand-200" />
            </div>
            <label className="block text-xs font-semibold text-sand-700">
              ID del dispositivo (Kippy, genérico, etc.)
              <input
                type="text"
                value={deviceId}
                onChange={(e) => setDeviceId(e.target.value)}
                placeholder="Ej: KIPPY-ABC123"
                className="mt-1 w-full rounded-xl border border-sand-200 px-3 py-2 text-sm text-sand-900 outline-none focus:border-brand-400 focus:ring-1 focus:ring-brand-400"
              />
            </label>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={async () => {
                  await register({
                    petId,
                    provider: deviceId.toLowerCase().startsWith("tractive")
                      ? "Tractive"
                      : "Generic",
                    externalDeviceId: deviceId || undefined,
                  });
                  setShowSetup(false);
                }}
                disabled={isPending}
                className="rounded-xl bg-brand-600 px-4 py-2 text-xs font-bold text-white hover:bg-brand-700 disabled:opacity-60"
              >
                {isPending ? "Registrando…" : "Registrar"}
              </button>
              <button
                type="button"
                onClick={() => setShowSetup(false)}
                className="text-xs text-sand-400 hover:text-sand-600"
              >
                Cancelar
              </button>
            </div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Status bar */}
      <motion.div
        initial={{ opacity: 0, y: 4 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex items-center gap-3 rounded-2xl border border-sand-200 bg-surface p-3"
      >
        <span className="text-2xl shrink-0" aria-hidden="true">
          📡
        </span>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-sand-900">
            {PROVIDER_LABELS[collar.provider] ?? collar.provider}
            {collar.externalDeviceId && (
              <span className="ml-2 text-xs font-normal text-sand-400">
                {collar.externalDeviceId}
              </span>
            )}
          </p>
          <p className="text-xs text-sand-500">
            {collar.lastSeenAt
              ? `Última señal: ${new Date(collar.lastSeenAt).toLocaleString("es-CR")}`
              : "Sin señal reciente"}
          </p>
        </div>
        {collar.batteryPercent !== null && (
          <div className="flex flex-col items-center">
            <span
              className={`text-sm font-bold tabular-nums ${collar.batteryPercent < 20 ? "text-danger-600" : "text-rescue-600"}`}
            >
              {collar.batteryPercent}%
            </span>
            <span className="text-[10px] text-sand-400">Batería</span>
          </div>
        )}
        <span
          className={`h-2 w-2 rounded-full shrink-0 ${collar.isActive ? "bg-rescue-500" : "bg-sand-300"}`}
        />
      </motion.div>

      {/* Time range selector */}
      <div className="flex items-center justify-between gap-2">
        <p className="text-xs font-semibold text-sand-600">Trayectoria</p>
        <div className="flex gap-1 flex-wrap justify-end">
          {HOURS_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              type="button"
              onClick={() => setHours(opt.value)}
              className={[
                "rounded-xl px-2.5 py-1 text-[10px] font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400",
                hours === opt.value
                  ? "bg-brand-600 text-white"
                  : "bg-sand-100 text-sand-600 hover:bg-sand-200",
              ].join(" ")}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      {/* Track stats */}
      {history && history.length > 0 && (
        <div className="flex gap-3 text-xs text-sand-500">
          <span className="font-semibold text-sand-700">{history.length}</span> puntos registrados
          {historyFetching && <span className="text-brand-500 animate-pulse">· actualizando…</span>}
        </div>
      )}

      {/* Map */}
      {collar.lastLat !== null && collar.lastLng !== null ? (
        <div
          className="overflow-hidden rounded-2xl border border-sand-200"
          style={{ height: 320 }}
        >
          <MapContainer
            center={[collar.lastLat, collar.lastLng]}
            zoom={15}
            className="h-full w-full"
            zoomControl={false}
          >
            <TileLayer
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              attribution="© OpenStreetMap"
            />

            {/* Historical track polyline */}
            {history && history.length >= 2 && (() => {
              const positions = history.map(
                (p): [number, number] => [p.lat, p.lng],
              );
              return (
                <>
                  <FitBounds positions={positions} />
                  <Polyline
                    positions={positions}
                    pathOptions={{
                      color: "#f97316",
                      weight: 3,
                      opacity: 0.85,
                      dashArray: undefined,
                    }}
                  />
                  {/* Start dot */}
                  <Marker
                    position={positions[0]}
                    title={`Inicio: ${new Date(history[0].recordedAt).toLocaleTimeString("es-CR")}`}
                  />
                </>
              );
            })()}

            {/* Current position marker */}
            <Marker position={[collar.lastLat, collar.lastLng]}>
              <Popup>
                <strong>Posición actual</strong>
                <br />
                {collar.lastSeenAt &&
                  new Date(collar.lastSeenAt).toLocaleString("es-CR")}
              </Popup>
            </Marker>
          </MapContainer>
        </div>
      ) : (
        <div className="flex h-40 items-center justify-center rounded-2xl border border-dashed border-sand-200 bg-surface-warm">
          <p className="text-sm text-sand-400">Esperando primera señal GPS…</p>
        </div>
      )}

      <p className="text-center text-[10px] text-sand-400">
        Posición en tiempo real · trayectoria de hasta 7 días ·
        actualización automática cada 30 s.
      </p>
    </div>
  );
}
