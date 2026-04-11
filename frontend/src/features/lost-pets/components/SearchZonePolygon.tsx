import { Polygon, Popup } from 'react-leaflet'
import type { SearchZone, SearchZoneStatus } from '../api/searchCoordinationApi'

interface SearchZonePolygonProps {
  zone: SearchZone
  currentUserId: string | undefined
  onClaim: (zoneId: string) => void
  onClear: (zoneId: string) => void
  onRelease: (zoneId: string) => void
}

/** Returns [lat, lng] coordinate pairs from a GeoJSON Polygon string. */
function parseGeoJsonPolygon(geoJson: string): [number, number][] {
  try {
    const parsed = JSON.parse(geoJson) as {
      type: string
      coordinates: [number, number][][]
    }
    // GeoJSON uses [lng, lat] — flip to [lat, lng] for Leaflet
    return parsed.coordinates[0].map(([lng, lat]) => [lat, lng])
  } catch {
    return []
  }
}

const STATUS_COLOR: Record<SearchZoneStatus, string> = {
  Free:  '#d4851a', // warn-600
  Taken: '#d42020', // danger-500
  Clear: '#17a26d', // rescue-500
}

const STATUS_FILL_OPACITY: Record<SearchZoneStatus, number> = {
  Free:  0.15,
  Taken: 0.25,
  Clear: 0.20,
}

const STATUS_LABEL: Record<SearchZoneStatus, string> = {
  Free:  '🟡 Libre',
  Taken: '🔴 Tomada',
  Clear: '✅ Limpia',
}

export function SearchZonePolygon({
  zone,
  currentUserId,
  onClaim,
  onClear,
  onRelease,
}: SearchZonePolygonProps) {
  const positions = parseGeoJsonPolygon(zone.geoJsonPolygon)
  if (positions.length === 0) return null

  const color       = STATUS_COLOR[zone.status]
  const fillOpacity = STATUS_FILL_OPACITY[zone.status]
  const isMyZone    = zone.assignedToUserId === currentUserId

  // Highlight the user's own zone with a blue override
  const displayColor = isMyZone && zone.status === 'Taken' ? '#3056c2' : color

  return (
    <Polygon
      positions={positions}
      pathOptions={{
        color:       displayColor,
        fillColor:   displayColor,
        fillOpacity,
        weight:      2,
      }}
    >
      <Popup>
        <div className="min-w-[160px] space-y-2 text-sm">
          <p className="font-bold text-sand-900">{zone.label}</p>
          <p className="text-sand-600">{STATUS_LABEL[zone.status]}</p>

          {/* Actions */}
          {zone.status === 'Free' && (
            <button
              type="button"
              onClick={() => onClaim(zone.id)}
              className="w-full rounded-lg bg-brand-500 px-3 py-1.5 text-xs font-bold text-white hover:bg-brand-600"
            >
              Tomar esta zona
            </button>
          )}

          {zone.status === 'Taken' && isMyZone && (
            <div className="flex flex-col gap-1.5">
              <button
                type="button"
                onClick={() => onClear(zone.id)}
                className="w-full rounded-lg bg-rescue-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-rescue-700"
              >
                Marcar como revisada
              </button>
              <button
                type="button"
                onClick={() => onRelease(zone.id)}
                className="w-full rounded-lg border border-sand-300 bg-white px-3 py-1.5 text-xs font-semibold text-sand-700 hover:bg-sand-50"
              >
                Liberar zona
              </button>
            </div>
          )}

          {zone.status === 'Taken' && !isMyZone && (
            <p className="text-xs text-sand-500">Zona en revisión por otro voluntario.</p>
          )}

          {zone.status === 'Clear' && (
            <p className="text-xs text-sand-500">Esta zona ya fue revisada.</p>
          )}
        </div>
      </Popup>
    </Polygon>
  )
}

