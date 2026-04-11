import { useEffect } from 'react'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { MapContainer, Marker, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import markerIcon2xUrl from 'leaflet/dist/images/marker-icon-2x.png'
import markerIconUrl from 'leaflet/dist/images/marker-icon.png'
import markerShadowUrl from 'leaflet/dist/images/marker-shadow.png'

delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2xUrl,
  iconUrl: markerIconUrl,
  shadowUrl: markerShadowUrl,
})

interface ClinicLocationPickerProps {
  lat: number
  lng: number
  onChange: (lat: number, lng: number) => void
}

function roundCoord(value: number) {
  return Math.round(value * 1e6) / 1e6
}

function ClickHandler({ onChange }: { onChange: (lat: number, lng: number) => void }) {
  useMapEvents({
    click: (event) => {
      onChange(roundCoord(event.latlng.lat), roundCoord(event.latlng.lng))
    },
  })
  return null
}

function KeepMapCentered({ lat, lng }: { lat: number; lng: number }) {
  const map = useMap()

  useEffect(() => {
    map.setView([lat, lng], map.getZoom(), { animate: false })
  }, [lat, lng, map])

  return null
}

export function ClinicLocationPicker({ lat, lng, onChange }: ClinicLocationPickerProps) {
  return (
    <div className="space-y-2">
      <p className="text-xs font-semibold uppercase tracking-widest text-sand-400">
        Ubicacion de la clinica
      </p>
      <p className="text-sm text-sand-600">Selecciona la ubicacion en el mapa (clic o arrastra el pin).</p>

      <div className="overflow-hidden rounded-2xl border border-sand-200 shadow-sm" data-testid="clinic-location-map">
        <MapContainer
          center={[lat, lng]}
          zoom={13}
          scrollWheelZoom
          className="h-56 w-full"
          style={{ cursor: 'crosshair' }}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <KeepMapCentered lat={lat} lng={lng} />
          <ClickHandler onChange={onChange} />
          <Marker
            position={[lat, lng]}
            draggable
            eventHandlers={{
              dragend: (event) => {
                const pos = (event.target as L.Marker).getLatLng()
                onChange(roundCoord(pos.lat), roundCoord(pos.lng))
              },
            }}
          />
        </MapContainer>
      </div>
    </div>
  )
}
