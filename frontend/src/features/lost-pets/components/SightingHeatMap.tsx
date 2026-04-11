import 'leaflet/dist/leaflet.css'
import L, { divIcon } from 'leaflet'
import { useMemo } from 'react'
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet'
import type { SightingDetail } from '@/features/sightings/api/sightingsApi'
import markerIcon2xUrl from 'leaflet/dist/images/marker-icon-2x.png'
import markerIconUrl from 'leaflet/dist/images/marker-icon.png'
import markerShadowUrl from 'leaflet/dist/images/marker-shadow.png'

// Fix Leaflet's bundler icon resolution (same pattern used across the app)
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2xUrl,
  iconUrl: markerIconUrl,
  shadowUrl: markerShadowUrl,
})

// ── Icon factory ──────────────────────────────────────────────────────────────

function buildIcon(badge: SightingDetail['priorityBadge']) {
  const color =
    badge === 'Urgent' ? '#dc2626' : badge === 'Validate' ? '#f59e0b' : '#22c55e'
  const size = badge === 'Urgent' ? 26 : badge === 'Validate' ? 22 : 18

  return divIcon({
    className: '',
    html: `<div style="
      width:${size}px;height:${size}px;border-radius:50%;
      background:${color};border:2px solid #fff;
      box-shadow:0 2px 6px rgba(0,0,0,.35);
    "></div>`,
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2],
    popupAnchor: [0, -(size / 2 + 4)],
  })
}

// ── Types ─────────────────────────────────────────────────────────────────────

interface SightingHeatMapProps {
  sightings: SightingDetail[]
  /** Optional centre to focus the map when no sightings exist yet */
  defaultCenter?: [number, number]
  className?: string
}

// ── Component ─────────────────────────────────────────────────────────────────

export function SightingHeatMap({
  sightings,
  defaultCenter = [9.7489, -83.7534], // Costa Rica geographic center
  className = 'h-72 w-full rounded-2xl overflow-hidden',
}: SightingHeatMapProps) {
  // Derive map center from the most recent sighting, falling back to defaultCenter
  const center = useMemo<[number, number]>(() => {
    if (sightings.length === 0) return defaultCenter
    const sorted = [...sightings].sort(
      (a, b) => new Date(b.sightedAt).getTime() - new Date(a.sightedAt).getTime(),
    )
    return [sorted[0].lat, sorted[0].lng]
  }, [sightings, defaultCenter])

  const zoom = sightings.length === 0 ? 8 : 14

  return (
    <MapContainer center={center} zoom={zoom} scrollWheelZoom className={className}>
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />

      {sightings.map((s) => (
        <Marker key={s.id} position={[s.lat, s.lng]} icon={buildIcon(s.priorityBadge)}>
          <Popup>
            <div style={{ minWidth: '140px', fontSize: '0.82rem' }}>
              {s.photoUrl && (
                <img
                  src={s.photoUrl}
                  alt="Avistamiento"
                  style={{
                    display: 'block',
                    width: '100%',
                    height: '80px',
                    objectFit: 'cover',
                    borderRadius: '6px',
                    marginBottom: '6px',
                  }}
                />
              )}
              <p style={{ margin: '0 0 3px', fontWeight: 700, color: '#18181b' }}>
                <span aria-hidden="true">🐾</span> {s.priorityBadge}
              </p>
              {s.note && (
                <p style={{ margin: '0 0 3px', color: '#52525b' }}>{s.note}</p>
              )}
              <p style={{ margin: 0, color: '#a1a1aa' }}>
                {new Date(s.sightedAt).toLocaleString('es-CR')}
              </p>
              <p style={{ margin: '4px 0 0', fontSize: '0.75rem', color: '#71717a' }}>
                {s.recommendedAction}
              </p>
            </div>
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  )
}
