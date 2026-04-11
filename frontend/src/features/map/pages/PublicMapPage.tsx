import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { MapContainer } from '../components/MapContainer'
import { useDebouncedBBox, usePublicMapEvents } from '../hooks/usePublicMap'
import { useMovementPredictions } from '../hooks/useMovementPrediction'
import type { MapBBox } from '../api/publicMapApi'
import { useAuthStore } from '@/features/auth/store/authStore'

export default function PublicMapPage() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const [bbox, setBbox] = useState<MapBBox | null>(null)
  const { debounce } = useDebouncedBBox(500)
  const { data: events = [], isFetching } = usePublicMapEvents(bbox)

  // Extract IDs of all visible lost-pet events so we can fetch movement predictions in parallel.
  const lostPetEventIds = useMemo(
    () => events.filter((e) => e.eventType === 'LostPet').map((e) => e.id),
    [events],
  )
  const predictions = useMovementPredictions(lostPetEventIds)

  return (
    <div className="relative h-screen w-full">
      {/* header strip */}
      <div className="absolute left-0 right-0 top-0 z-[1000] flex items-center justify-between bg-white/90 px-4 py-2 shadow backdrop-blur">
        <span className="text-sm font-bold text-sand-800">🗺️ PawTrack — Mapa público</span>
        <span className="text-xs text-sand-500">
          {events.length} eventos
          {isFetching && <span className="ml-1 text-brand-500">• actualizando…</span>}
        </span>
      </div>

      {/* legend */}
      <div className="absolute bottom-6 left-3 z-[1000] rounded-xl bg-white/90 px-3 py-2 text-xs shadow backdrop-blur">
        <div className="mb-1 flex items-center gap-2">
          <span className="inline-block h-3 w-3 rounded-full bg-danger-500" />
          <span className="text-sand-700">Mascota perdida</span>
        </div>
        <div className="mb-1 flex items-center gap-2">
          <span className="inline-block h-3 w-3 rounded-full bg-brand-500" />
          <span className="text-sand-700">Avistamiento</span>
        </div>
        <div className="mb-1 flex items-center gap-2">
          <span className="inline-block h-3 w-3 rounded-full border-2 border-dashed border-trust-500 bg-transparent" />
          <span className="text-sand-700">Trayectoria</span>
        </div>
        <div className="flex items-center gap-2">
          <span className="inline-block h-3 w-3 rounded-full border-2 border-rescue-500 bg-rescue-200 opacity-80" />
          <span className="text-sand-700">Zona proyectada</span>
        </div>
      </div>

      {/* Visual match shortcut */}
      <div className="absolute bottom-6 right-3 z-[1000] flex flex-col gap-2">
        {isAuthenticated && (
          <Link
            to="/dashboard"
            className="flex items-center gap-2 rounded-xl border border-sand-300 bg-white/95 px-4 py-2.5 text-sm font-semibold text-sand-700 shadow-lg transition-colors hover:bg-sand-50"
          >
            ← Volver al dashboard
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
      </div>

      <MapContainer
        events={events}
        predictions={predictions}
        onBBoxChange={(newBBox) => debounce(setBbox, newBBox)}
        className="h-full w-full"
      />
    </div>
  )
}

