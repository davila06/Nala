import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { Circle, MapContainer, Marker, TileLayer, useMapEvents } from 'react-leaflet'
import markerIcon2xUrl from 'leaflet/dist/images/marker-icon-2x.png'
import markerIconUrl from 'leaflet/dist/images/marker-icon.png'
import markerShadowUrl from 'leaflet/dist/images/marker-shadow.png'

// Fix Leaflet icon paths broken by bundlers
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2xUrl,
  iconUrl: markerIconUrl,
  shadowUrl: markerShadowUrl,
})

interface CoverageMapPickerProps {
  lat: number
  lng: number
  radiusMetres: number
  onChange: (lat: number, lng: number, radiusMetres: number) => void
}

/** Listens for map clicks and updates the center. Must be rendered inside <MapContainer>. */
function ClickHandler({ onCenterChange }: { onCenterChange: (lat: number, lng: number) => void }) {
  useMapEvents({
    click: (e) => {
      onCenterChange(e.latlng.lat, e.latlng.lng)
    },
  })
  return null
}

const MIN_RADIUS = 100
const MAX_RADIUS = 20_000

export function CoverageMapPicker({ lat, lng, radiusMetres, onChange }: CoverageMapPickerProps) {
  return (
    <div className="overflow-hidden rounded-2xl border border-sand-200 shadow-sm">
      {/* Map */}
      <div className="relative">
        <MapContainer
          center={[lat, lng]}
          zoom={13}
          scrollWheelZoom
          className="h-64 w-full"
          style={{ cursor: 'crosshair' }}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <ClickHandler
            onCenterChange={(newLat, newLng) => onChange(
              Math.round(newLat * 1e6) / 1e6,
              Math.round(newLng * 1e6) / 1e6,
              radiusMetres,
            )}
          />
          <Marker
            position={[lat, lng]}
            draggable
            eventHandlers={{
              dragend: (e) => {
                const pos = (e.target as L.Marker).getLatLng()
                onChange(
                  Math.round(pos.lat * 1e6) / 1e6,
                  Math.round(pos.lng * 1e6) / 1e6,
                  radiusMetres,
                )
              },
            }}
          />
          <Circle
            center={[lat, lng]}
            radius={radiusMetres}
            pathOptions={{ color: '#059669', fillColor: '#059669', fillOpacity: 0.15, weight: 2 }}
          />
        </MapContainer>

        {/* Click hint */}
        <span className="pointer-events-none absolute bottom-2 left-1/2 -translate-x-1/2 rounded-full bg-black/50 px-3 py-1 text-xs text-white backdrop-blur-sm">
          Clic en el mapa para mover el centro · Arrastrá el pin
        </span>
      </div>

      {/* Radius slider */}
      <div className="bg-white px-5 py-4">
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-sand-500 uppercase tracking-[0.2em]">Radio de cobertura</span>
          <span className="rounded-full bg-rescue-100 px-3 py-1 text-xs font-bold text-rescue-800">
            {radiusMetres >= 1000
              ? `${(radiusMetres / 1000).toFixed(1)} km`
              : `${radiusMetres} m`}
          </span>
        </div>
        <input
          type="range"
          min={MIN_RADIUS}
          max={MAX_RADIUS}
          step={100}
          value={radiusMetres}
          onChange={(e) => onChange(lat, lng, Number(e.target.value))}
          aria-label="Radio de cobertura en metros"
          className="mt-3 w-full accent-rescue-600"
        />
        <div className="mt-1 flex justify-between text-[10px] text-sand-400">
          <span>{MIN_RADIUS} m</span>
          <span>{(MAX_RADIUS / 1000).toFixed(0)} km</span>
        </div>

        {/* Coordinate readout */}
        <div className="mt-3 grid grid-cols-2 gap-2 rounded-xl bg-sand-50 px-4 py-3 text-xs text-sand-600 font-mono">
          <div>
            <span className="block text-[10px] font-sans font-semibold uppercase tracking-[0.15em] text-sand-400 mb-0.5">Lat</span>
            {lat.toFixed(6)}
          </div>
          <div>
            <span className="block text-[10px] font-sans font-semibold uppercase tracking-[0.15em] text-sand-400 mb-0.5">Lng</span>
            {lng.toFixed(6)}
          </div>
        </div>
      </div>
    </div>
  )
}

