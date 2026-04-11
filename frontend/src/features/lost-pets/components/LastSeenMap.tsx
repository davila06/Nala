import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { useEffect, useRef } from 'react'
import {
  Circle,
  MapContainer as LeafletMapContainer,
  Marker,
  Popup,
  TileLayer,
  useMapEvents,
} from 'react-leaflet'
import { formatRadius } from '../utils/searchRadius'
import type { GeolocationStatus } from '../hooks/useGeolocation'
import markerIcon2xUrl from 'leaflet/dist/images/marker-icon-2x.png'
import markerIconUrl from 'leaflet/dist/images/marker-icon.png'
import markerShadowUrl from 'leaflet/dist/images/marker-shadow.png'

// ── Leaflet icon fix (bundler breaks default icon paths) ──────────────────────
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2xUrl,
  iconUrl: markerIconUrl,
  shadowUrl: markerShadowUrl,
})

/** Red pulsing icon for the "last seen" pin */
const lastSeenIcon = L.divIcon({
  className: '',
  html: `
    <div style="position:relative;width:32px;height:32px">
      <div style="
        position:absolute;inset:0;border-radius:50%;
        background:rgba(220,38,38,0.3);
        animation:pulse-ring 1.6s cubic-bezier(0.215,0.61,0.355,1) infinite;
      "></div>
      <div style="
        position:absolute;inset:6px;border-radius:50%;
        background:#dc2626;border:2px solid #fff;
        box-shadow:0 2px 6px rgba(0,0,0,0.4);
      "></div>
    </div>
    <style>
      @keyframes pulse-ring{0%{transform:scale(.8);opacity:1}80%,100%{transform:scale(2.2);opacity:0}}
    </style>
  `,
  iconSize: [32, 32],
  iconAnchor: [16, 16],
  popupAnchor: [0, -20],
})

/** Blue dot for the user's current device position */
const userIcon = L.divIcon({
  className: '',
  html: `
    <div style="
      width:16px;height:16px;border-radius:50%;
      background:#3b82f6;border:3px solid #fff;
      box-shadow:0 2px 6px rgba(59,130,246,0.6);
    "></div>
  `,
  iconSize: [16, 16],
  iconAnchor: [8, 8],
  popupAnchor: [0, -10],
})

// ── Costa Rica center (fallback when geolocation is unavailable) ───────────────
const CR_CENTER: [number, number] = [9.7489, -83.7534]
const DEFAULT_ZOOM = 13

// ── Sub-components ────────────────────────────────────────────────────────────

interface ClickHandlerProps {
  onMapClick: (lat: number, lng: number) => void
}

/** Captures map click events and emits lat/lng to parent */
function ClickHandler({ onMapClick }: ClickHandlerProps) {
  useMapEvents({
    click(e) {
      onMapClick(e.latlng.lat, e.latlng.lng)
    },
  })
  return null
}

interface FlyToProps {
  lat: number
  lng: number
}

/** Flies the map to a given position — used when geolocation resolves */
function FlyTo({ lat, lng }: FlyToProps) {
  const map = useMapEvents({})

  const didFly = useRef(false)
  useEffect(() => {
    if (!didFly.current) {
      map.flyTo([lat, lng], DEFAULT_ZOOM, { animate: true, duration: 1 })
      didFly.current = true
    }
  }, [lat, lng, map])

  return null
}

// ── Public API ─────────────────────────────────────────────────────────────────

export interface LastSeenCoords {
  lat: number
  lng: number
}

interface LastSeenMapProps {
  /** Selected pin position (null = no pin yet) */
  value: LastSeenCoords | null
  /** Called whenever the user clicks on the map to position the pin */
  onChange: (coords: LastSeenCoords) => void
  /** Current device position (from useGeolocation) */
  userCoords: LastSeenCoords | null
  /** Geolocation permission status */
  geoStatus: GeolocationStatus
  /** Pet name — shown in the pin popup */
  petName: string
  /**
   * Estimated search radius in metres (from `estimateSearchRadius`).
   * When provided, replaces the fixed 200 m circle and shows a legend overlay.
   */
  estimatedRadius?: number
  className?: string
}

/**
 * Interactive Leaflet map for selecting the last-seen location of a lost pet.
 *
 * - Clicking the map places / moves the red pulsing pin.
 * - The pin is draggable for fine-grained positioning.
 * - The user's device position is shown as a blue dot.
 * - A subtle circle shows the estimated search radius based on species and time.
 */
export function LastSeenMap({
  value,
  onChange,
  userCoords,
  geoStatus,
  petName,
  estimatedRadius,
  className = 'h-64 w-full rounded-2xl overflow-hidden',
}: LastSeenMapProps) {
  const circleRadius = estimatedRadius ?? 200

  return (
    <div
      className={`${className} relative`}
      role="application"
      aria-label="Mapa para marcar última ubicación"
    >
      <LeafletMapContainer
        center={userCoords ? [userCoords.lat, userCoords.lng] : CR_CENTER}
        zoom={DEFAULT_ZOOM}
        scrollWheelZoom
        style={{ height: '100%', width: '100%' }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {/* Fly to user position once geolocation resolves */}
        {geoStatus === 'granted' && userCoords && (
          <FlyTo lat={userCoords.lat} lng={userCoords.lng} />
        )}

        {/* Click handler — converts map click → pin placement */}
        <ClickHandler onMapClick={(lat, lng) => onChange({ lat, lng })} />

        {/* User's device position */}
        {userCoords && (
          <Marker position={[userCoords.lat, userCoords.lng]} icon={userIcon}>
            <Popup>Tu ubicación actual</Popup>
          </Marker>
        )}

        {/* Last-seen pin (draggable) */}
        {value && (
          <>
            <Marker
              position={[value.lat, value.lng]}
              icon={lastSeenIcon}
              draggable
              eventHandlers={{
                dragend(e) {
                  const { lat, lng } = (e.target as L.Marker).getLatLng()
                  onChange({ lat, lng })
                },
              }}
            >
              <Popup>
                <span className="text-sm font-semibold">Última ubicación de {petName}</span>
                <br />
                <span className="text-xs text-sand-500">
                  {value.lat.toFixed(5)}, {value.lng.toFixed(5)}
                </span>
                <br />
                <span className="text-xs text-sand-400">Arrastra para ajustar</span>
              </Popup>
            </Marker>

            {/* Dynamic search radius circle */}
            <Circle
              center={[value.lat, value.lng]}
              radius={circleRadius}
              pathOptions={{
                color: '#d42020',
                fillColor: '#d42020',
                fillOpacity: 0.06,
                weight: 1.5,
                dashArray: '6 4',
              }}
            />
          </>
        )}
      </LeafletMapContainer>

      {/* Radius legend overlay — only shown when a pin is placed */}
      {value && (
        <div
          className="pointer-events-none absolute bottom-2 left-2 z-[1000] rounded-lg border border-danger-100 bg-white/90 px-2.5 py-1.5 text-xs shadow-sm backdrop-blur-sm"
          aria-live="polite"
          aria-label={`Radio de b\u00fasqueda estimado: ${formatRadius(circleRadius)}`}
        >
          <span className="font-semibold text-danger-700">
            {'\uD83D\uDD0D'} Radio estimado: {formatRadius(circleRadius)}
          </span>
        </div>
      )}
    </div>
  )
}

