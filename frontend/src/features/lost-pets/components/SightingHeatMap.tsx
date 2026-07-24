import 'leaflet/dist/leaflet.css'
import L, { divIcon } from 'leaflet'
import { useMemo } from 'react'
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet'
import type { SightingDetail } from '@/features/sightings/api/sightingsApi'
import markerIcon2xUrl from 'leaflet/dist/images/marker-icon-2x.png'
import markerIconUrl from 'leaflet/dist/images/marker-icon.png'
import markerShadowUrl from 'leaflet/dist/images/marker-shadow.png'

// Fix Leaflet's bundler icon resolution
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2xUrl,
  iconUrl: markerIconUrl,
  shadowUrl: markerShadowUrl,
})

// ── Premium SVG marker factory ────────────────────────────────────────────────

let pulseStyleInjected = false
function ensurePulseStyle() {
  if (pulseStyleInjected || typeof document === 'undefined') return
  pulseStyleInjected = true
  const style = document.createElement('style')
  style.textContent = `
    @keyframes _pt_ring{0%{transform:scale(.7);opacity:.9}80%,100%{transform:scale(2.2);opacity:0}}
    @keyframes _pt_dot_pulse{0%,100%{transform:scale(1)}50%{transform:scale(1.12)}}
  `
  document.head.appendChild(style)
}

function buildSightingIcon(badge: SightingDetail['priorityBadge']) {
  ensurePulseStyle()

  const cfg =
    badge === 'Urgent'
      ? { fill: '#d42020', ring: 'rgba(212,32,32,0.35)',  size: 28, pulse: true }
      : badge === 'Validate'
        ? { fill: '#f0b800', ring: 'rgba(240,184,0,0.30)', size: 22, pulse: false }
        : { fill: '#17a26d', ring: 'rgba(23,162,109,0.25)', size: 18, pulse: false }

  const { fill, ring, size, pulse } = cfg
  const r   = size / 2
  const dot = r * 0.46

  const ringAnim = pulse
    ? `<circle cx="${r}" cy="${r}" r="${r * 0.85}" fill="none" stroke="${ring}" stroke-width="${r * 0.6}" style="animation:_pt_ring 1.4s cubic-bezier(0.215,.61,.355,1) infinite"/>`
    : ''

  const svg = `
    <svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}" overflow="visible">
      ${ringAnim}
      <circle cx="${r}" cy="${r + 1}" r="${dot + 2}" fill="rgba(0,0,0,0.18)"/>
      <circle cx="${r}" cy="${r}" r="${dot + 2.5}" fill="#fff"/>
      <circle cx="${r}" cy="${r}" r="${dot}" fill="${fill}" style="animation:_pt_dot_pulse 2s ease-in-out infinite"/>
      <circle cx="${r - dot * 0.3}" cy="${r - dot * 0.3}" r="${dot * 0.28}" fill="rgba(255,255,255,0.55)"/>
    </svg>
  `.trim()

  return divIcon({
    className: '',
    html: svg,
    iconSize:   [size, size],
    iconAnchor: [size / 2, size / 2],
    popupAnchor: [0, -(size / 2 + 6)],
  })
}

// ── Types ─────────────────────────────────────────────────────────────────────

interface SightingHeatMapProps {
  sightings: SightingDetail[]
  defaultCenter?: [number, number]
  className?: string
}

// ── Component ─────────────────────────────────────────────────────────────────

export function SightingHeatMap({
  sightings,
  defaultCenter = [9.7489, -83.7534],
  className = 'h-72 w-full rounded-2xl overflow-hidden',
}: SightingHeatMapProps) {
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
        <Marker key={s.id} position={[s.lat, s.lng]} icon={buildSightingIcon(s.priorityBadge)}>
          <Popup>
            <div style={{ minWidth: '150px', fontSize: '0.82rem' }}>
              {s.photoUrl && (
                <img src={s.photoUrl} alt="Avistamiento" style={{ display:'block', width:'100%', height:'80px', objectFit:'cover', borderRadius:'6px', marginBottom:'6px' }} />
              )}
              <p style={{ margin:'0 0 3px', fontWeight:700, color:'#18181b' }}>
                <span aria-hidden="true">🐾</span> {s.priorityBadge === 'Urgent' ? '🔴 Urgente' : s.priorityBadge === 'Validate' ? '🟡 Validar' : '🟢 Confirmado'}
              </p>
              {s.note && <p style={{ margin:'0 0 3px', color:'#52525b' }}>{s.note}</p>}
              <p style={{ margin:0, color:'#a1a1aa' }}>{new Date(s.sightedAt).toLocaleString('es-CR')}</p>
              {s.recommendedAction && (
                <p style={{ margin:'4px 0 0', fontSize:'0.75rem', color:'#71717a', borderTop:'1px solid #f4f4f5', paddingTop:'4px' }}>
                  {s.recommendedAction}
                </p>
              )}
            </div>
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  )
}

