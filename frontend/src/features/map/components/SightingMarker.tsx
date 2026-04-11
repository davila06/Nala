import { divIcon } from 'leaflet'
import { Marker, Popup } from 'react-leaflet'
import { formatDate } from '@/shared/lib/formatDate'
import type { PublicMapEvent } from '../api/publicMapApi'

const orangeIcon = divIcon({
  className: '',
  html: `<div style="
    width:22px;height:22px;border-radius:50%;
    background:#e8521e;border:2px solid #fff;
    box-shadow:0 1px 4px rgba(0,0,0,.3)
  "></div>`,
  iconSize: [22, 22],
  iconAnchor: [11, 11],
  popupAnchor: [0, -14],
})

interface SightingMarkerProps {
  event: PublicMapEvent
}

export function SightingMarker({ event }: SightingMarkerProps) {
  return (
    <Marker position={[event.lat, event.lng]} icon={orangeIcon}>
      <Popup>
        <div className="min-w-[130px] text-sm">
          {event.photoUrl && (
            <img
              src={event.photoUrl}
              alt="Avistamiento"
              className="mb-2 h-20 w-full rounded object-cover"
            />
          )}
          <p className="font-bold text-brand-600">🐾 Avistamiento reportado</p>
          <p className="text-xs text-sand-500">
            {formatDate(event.occurredAt)}
          </p>
        </div>
      </Popup>
    </Marker>
  )
}

