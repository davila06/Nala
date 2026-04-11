import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { MapContainer, TileLayer } from 'react-leaflet'
import { useQuery } from '@tanstack/react-query'
import { useAuthStore } from '@/features/auth/store/authStore'
import { searchCoordinationApi, type SearchZone } from '../api/searchCoordinationApi'
import { useSearchCoordinationHub } from '../hooks/useSearchCoordinationHub'
import { SearchZonePolygon } from '../components/SearchZonePolygon'
import { Alert } from '@/shared/ui/Alert'
import markerIcon2xUrl from 'leaflet/dist/images/marker-icon-2x.png'
import markerIconUrl from 'leaflet/dist/images/marker-icon.png'
import markerShadowUrl from 'leaflet/dist/images/marker-shadow.png'

// Fix Leaflet default icon paths broken by bundlers
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2xUrl,
  iconUrl: markerIconUrl,
  shadowUrl: markerShadowUrl,
})

export default function SearchCoordinationPage() {
  const { lostEventId } = useParams<{ lostEventId: string }>()
  const currentUser = useAuthStore((s) => s.user)
  const [zones, setZones] = useState<SearchZone[]>([])
  const [activating, setActivating] = useState(false)
  const [activateError, setActivateError] = useState<string | null>(null)

  const eventId = lostEventId ?? ''

  // Initial zone load via REST
  const { data: zonesData, isLoading, error: loadError } = useQuery<SearchZone[]>({
    queryKey: ['search-zones', eventId],
    queryFn: () => searchCoordinationApi.getZones(eventId),
    enabled: !!eventId,
  })

  useEffect(() => {
    if (zonesData) setZones(zonesData)
  }, [zonesData])

  // Merge a single updated zone into local state (from SignalR broadcasts)
  const mergeZone = useCallback((updated: SearchZone) => {
    setZones((prev) =>
      prev.map((z) => (z.id === updated.id ? { ...z, ...updated } : z))
    )
  }, [])

  // SignalR hub connection
  const { isConnected, claimZone, clearZone, releaseZone } = useSearchCoordinationHub({
    lostEventId: eventId,
    onZoneClaimed:  mergeZone,
    onZoneCleared:  mergeZone,
    onZoneReleased: mergeZone,
  })

  // Stats derived from zone list
  const total = zones.length
  const free  = zones.filter((z) => z.status === 'Free').length
  const taken = zones.filter((z) => z.status === 'Taken').length
  const clear = zones.filter((z) => z.status === 'Clear').length

  async function handleActivate() {
    if (!eventId) return
    setActivating(true)
    setActivateError(null)
    try {
      const res = await searchCoordinationApi.activateCoordination(eventId)
      const fresh = await searchCoordinationApi.getZones(eventId)
      setZones(fresh)
      void res
    } catch {
      setActivateError('No se pudo activar el modo coordinación. Intenta de nuevo.')
    } finally {
      setActivating(false)
    }
  }

  // Use center of first zone as initial map center, fall back to Costa Rica
  const mapCenter: [number, number] = (() => {
    try {
      if (zones.length > 0) {
        const parsed = JSON.parse(zones[Math.floor(zones.length / 2)].geoJsonPolygon) as {
          coordinates: [number, number][][]
        }
        const [lng, lat] = parsed.coordinates[0][0]
        return [lat, lng]
      }
    } catch { /* ignore */ }
    return [9.9281, -84.0908] // San José, Costa Rica
  })()

  return (
    <div className="flex h-screen flex-col bg-sand-50">
      {/* ── Header ── */}
      <header className="flex items-center justify-between border-b border-sand-200 bg-white px-4 py-3">
        <div>
          <h1 className="text-lg font-extrabold text-sand-900">Búsqueda coordinada</h1>
          <p className="text-xs text-sand-500">
            {isConnected
              ? '🟢 Conectado en tiempo real'
              : '🔴 Sin conexión en tiempo real'}
          </p>
        </div>

        {total === 0 && (
          <button
            type="button"
            onClick={handleActivate}
            disabled={activating}
            className="rounded-xl bg-brand-500 px-4 py-2 text-sm font-bold text-white hover:bg-brand-600 disabled:opacity-50"
          >
            {activating ? 'Activando…' : 'Activar modo coordinación'}
          </button>
        )}
      </header>

      {activateError && (
        <Alert variant="error">{activateError}</Alert>
      )}

      {/* ── Stats bar ── */}
      {total > 0 && (
        <div className="flex gap-4 border-b border-sand-200 bg-white px-4 py-2 text-xs font-semibold">
          <span className="text-sand-500">Total: {total}</span>
          <span className="text-brand-600">🟡 Libre: {free}</span>
          <span className="text-danger-600">🔴 Tomada: {taken}</span>
          <span className="text-rescue-600">✅ Limpia: {clear}</span>
        </div>
      )}

      {/* ── Map ── */}
      <div className="relative flex-1">
        {isLoading && (
          <div className="absolute inset-0 z-10 flex items-center justify-center bg-white/80">
            <p className="text-sm text-sand-500">Cargando zonas…</p>
          </div>
        )}

        {loadError && (
          <div className="absolute inset-0 z-10 flex items-center justify-center bg-white/80">
            <Alert variant="error">Error cargando zonas.</Alert>
          </div>
        )}

        <MapContainer
          center={mapCenter}
          zoom={zones.length > 0 ? 15 : 13}
          scrollWheelZoom
          style={{ height: '100%', width: '100%' }}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />

          {zones.map((zone) => (
            <SearchZonePolygon
              key={zone.id}
              zone={zone}
              currentUserId={currentUser?.id}
              onClaim={claimZone}
              onClear={clearZone}
              onRelease={releaseZone}
            />
          ))}
        </MapContainer>
      </div>

      {/* ── No zones empty state ── */}
      {!isLoading && total === 0 && (
        <div className="absolute inset-x-0 bottom-24 mx-auto max-w-sm rounded-2xl bg-white p-5 shadow-lg text-center">
          <p className="text-3xl">🗺️</p>
          <p className="mt-2 text-sm font-semibold text-sand-700">
            Aún no hay zonas de búsqueda
          </p>
          <p className="mt-1 text-xs text-sand-500">
            El dueño de la mascota puede activar el modo coordinación para dividir el área en zonas.
          </p>
        </div>
      )}
    </div>
  )
}

