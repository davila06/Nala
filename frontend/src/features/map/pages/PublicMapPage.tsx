import { useCallback, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { MapContainer } from "../components/MapContainer";
import { useDebouncedBBox, usePublicMapEvents } from "../hooks/usePublicMap";
import { useMovementPredictions } from "../hooks/useMovementPrediction";
import type { MapBBox } from "../api/publicMapApi";
import { useAuthStore } from "@/features/auth/store/authStore";

export default function PublicMapPage() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [bbox, setBbox] = useState<MapBBox | null>(null);
  const [locateTrigger, setLocateTrigger] = useState(0);
  const { debounce } = useDebouncedBBox(500);
  const { data: events = [], isFetching } = usePublicMapEvents(bbox);

  const lostPetEventIds = useMemo(
    () => events.filter((e) => e.eventType === "LostPet").map((e) => e.id),
    [events],
  );
  const predictions = useMovementPredictions(lostPetEventIds);

  const handleBBoxChange = useCallback(
    (newBBox: MapBBox) => debounce(setBbox, newBBox),
    [debounce],
  );

  return (
    <div className="relative h-screen w-full">
      <Helmet>
        <title>Mapa en vivo — PawTrack CR</title>
        <meta
          name="description"
          content="Mapa en tiempo real de mascotas perdidas y avistamientos en Costa Rica. Ayuda a reunir mascotas con sus familias."
        />
        <meta property="og:title" content="Mapa en vivo — PawTrack CR" />
        <meta
          property="og:description"
          content="Mascotas perdidas y avistamientos en tiempo real en Costa Rica."
        />
        <meta property="og:type" content="website" />
      </Helmet>
      {/* Glassmorphism header strip */}
      <div className="absolute left-0 right-0 top-0 z-[1000] flex items-center justify-between border-b border-white/10 bg-zinc-900/70 px-4 py-2.5 backdrop-blur-md">
        <div className="flex items-center gap-2">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-rescue-400 opacity-75" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-rescue-400" />
          </span>
          <span className="text-sm font-bold text-white tracking-tight">
            PawTrack — Mapa en vivo
          </span>
        </div>
        <span className="text-xs text-zinc-300">
          <span className="font-semibold text-white">{events.length}</span>{" "}
          eventos
          {isFetching && (
            <span className="ml-1.5 text-brand-400">• actualizando…</span>
          )}
        </span>
      </div>

      {/* Glassmorphism legend */}
      <div className="absolute bottom-6 left-3 z-[1000] rounded-2xl border border-white/10 bg-zinc-900/70 px-3.5 py-3 text-xs shadow-xl backdrop-blur-md">
        <p className="mb-2 text-[10px] font-bold uppercase tracking-widest text-zinc-400">
          Leyenda
        </p>
        {[
          { color: "bg-danger-500", label: "Mascota perdida", pulse: true },
          { color: "bg-brand-500", label: "Avistamiento", pulse: false },
          {
            color: "border-2 border-dashed border-trust-400 bg-transparent",
            label: "Trayectoria",
            pulse: false,
          },
          {
            color: "border-2 border-rescue-400 bg-rescue-200/40",
            label: "Zona proyectada",
            pulse: false,
          },
        ].map(({ color, label, pulse }) => (
          <div key={label} className="mb-1.5 flex items-center gap-2 last:mb-0">
            <span
              className={`relative inline-flex h-3 w-3 rounded-full flex-shrink-0 ${color}`}
            >
              {pulse && (
                <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-danger-400 opacity-60" />
              )}
            </span>
            <span className="text-zinc-200">{label}</span>
          </div>
        ))}
      </div>

      {/* Controls panel */}
      <div className="absolute bottom-6 right-3 z-[1000] flex flex-col gap-2">
        {isAuthenticated && (
          <Link
            to="/dashboard"
            className="flex items-center gap-2 rounded-xl border border-white/10 bg-zinc-900/70 px-4 py-2.5 text-sm font-semibold text-white shadow-lg backdrop-blur-md transition-colors hover:bg-zinc-800/80"
          >
            ← Dashboard
          </Link>
        )}
        <Link
          to="/estadisticas"
          className="flex items-center gap-2 rounded-xl border border-sand-300 bg-white/95 px-4 py-2.5 text-sm font-semibold text-sand-700 shadow-lg transition-colors hover:bg-sand-50"
        >
          📊 Ver estadísticas
        </Link>
        <Link
          to="/map/match"
          className="flex items-center gap-2 rounded-xl bg-sand-900 px-4 py-2.5 text-sm font-bold text-white shadow-lg transition-colors hover:bg-sand-700"
        >
          🔍 ¿Encontraste un animal?
        </Link>
        <button
          type="button"
          onClick={() => setLocateTrigger((t) => t + 1)}
          className="flex items-center gap-2 rounded-xl border border-white/10 bg-zinc-900/70 px-4 py-2.5 text-sm font-semibold text-white shadow-lg backdrop-blur-md transition-colors hover:bg-zinc-800/80"
          aria-label="Centrar mapa en mi ubicación"
        >
          📍 Mi ubicación
        </button>
      </div>

      <MapContainer
        events={events}
        predictions={predictions}
        locateTrigger={locateTrigger}
        onBBoxChange={handleBBoxChange}
        className="h-full w-full"
      />
    </div>
  );
}
