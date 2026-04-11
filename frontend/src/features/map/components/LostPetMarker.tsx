import { divIcon } from 'leaflet'
import { Marker, Popup } from 'react-leaflet'
import { formatDate } from '@/shared/lib/formatDate'
import { Link } from 'react-router-dom'
import type { PublicMapEvent } from '../api/publicMapApi'

const redIcon = divIcon({
  className: '',
  html: `<div style="
    width:28px;height:28px;border-radius:50% 50% 50% 0;
    background:#d42020;border:2px solid #fff;
    transform:rotate(-45deg);box-shadow:0 1px 4px rgba(0,0,0,.3)
  "></div>`,
  iconSize: [28, 28],
  iconAnchor: [14, 28],
  popupAnchor: [0, -30],
})

interface LostPetMarkerProps {
  event: PublicMapEvent
}

export function LostPetMarker({ event }: LostPetMarkerProps) {
  return (
    <Marker position={[event.lat, event.lng]} icon={redIcon}>
      <Popup>
        <div className="min-w-[140px] text-sm">
          {event.photoUrl && (
            <img
              src={event.photoUrl}
              alt="Mascota perdida"
              className="mb-2 h-24 w-full rounded object-cover"
            />
          )}
          <p className="font-bold text-danger-600">🚨 Mascota perdida reportada</p>
          <p className="text-xs text-sand-500">
            {formatDate(event.occurredAt)}
          </p>
          <Link
            to={`/p/${event.petId}`}
            className="mt-1 block text-xs font-semibold text-brand-600 hover:underline"
          >
            Ver perfil de la mascota →
          </Link>
        </div>
      </Popup>
    </Marker>
  )
}

