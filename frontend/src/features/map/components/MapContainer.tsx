import "leaflet/dist/leaflet.css";
import L from "leaflet";
import { useEffect, useState } from "react";
import {
  MapContainer as LeafletMapContainer,
  TileLayer,
  useMap,
  useMapEvents,
} from "react-leaflet";
import markerIcon2xUrl from "leaflet/dist/images/marker-icon-2x.png";
import markerIconUrl from "leaflet/dist/images/marker-icon.png";
import markerShadowUrl from "leaflet/dist/images/marker-shadow.png";
import type { MovementPrediction, PublicMapEvent } from "../api/publicMapApi";
import type { MapBBox } from "../api/publicMapApi";
import { LostPetMarker } from "./LostPetMarker";
import { PredictionTrail } from "./PredictionTrail";
import { SightingMarker } from "./SightingMarker";

// Fix Leaflet's default icon paths broken by bundlers
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)
  ._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2xUrl,
  iconUrl: markerIconUrl,
  shadowUrl: markerShadowUrl,
});

interface MapContainerProps {
  events: PublicMapEvent[];
  onBBoxChange: (bbox: MapBBox) => void;
  /** Keyed by lost-pet event ID. Trails and uncertainty circles are rendered for each entry. */
  predictions?: Record<string, MovementPrediction>;
  /** Increment to re-trigger fly-to-user */
  locateTrigger?: number;
  className?: string;
}

/** Fires current bbox on mount AND on every moveend */
function BBoxListener({
  onBBoxChange,
}: {
  onBBoxChange: (bbox: MapBBox) => void;
}) {
  const map = useMapEvents({
    moveend: () => {
      const b = map.getBounds();
      onBBoxChange({
        north: b.getNorth(),
        south: b.getSouth(),
        east: b.getEast(),
        west: b.getWest(),
      });
    },
  });

  // Fire the initial bounding box as soon as the map renders so the
  // data query starts immediately without waiting for a user pan/zoom.
  useEffect(() => {
    const b = map.getBounds();
    onBBoxChange({
      north: b.getNorth(),
      south: b.getSouth(),
      east: b.getEast(),
      west: b.getWest(),
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // intentionally only on mount

  return null;
}

/**
 * Requests the browser geolocation and flies the map to the user's position.
 * Re-fires whenever `trigger` increments (e.g. when the user clicks "Mi ubicación").
 * On mount (trigger=0) it auto-locates so nearby pets load immediately.
 */
function LocateUser({ trigger }: { trigger: number }) {
  const map = useMap();

  useEffect(() => {
    if (!navigator.geolocation) return;
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        map.flyTo([pos.coords.latitude, pos.coords.longitude], 13, {
          duration: 1.2,
        });
      },
      undefined,
      { timeout: 8_000, maximumAge: 60_000 },
    );
  }, [map, trigger]); // trigger=0 on mount = auto; increment = re-locate

  return null;
}

export function MapContainer({
  events,
  onBBoxChange,
  predictions = {},
  locateTrigger = 0,
  className = "h-[60vh] w-full",
}: MapContainerProps) {
  // Costa Rica center — used as fallback when GPS is unavailable
  const [center] = useState<[number, number]>([9.7489, -83.7534]);

  return (
    <div
      role="application"
      aria-label="Mapa interactivo de mascotas perdidas y avistamientos"
      className={className}
    >
      <LeafletMapContainer
        center={center}
        zoom={8}
        scrollWheelZoom
        style={{ height: "100%", width: "100%" }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <BBoxListener onBBoxChange={onBBoxChange} />
        <LocateUser trigger={locateTrigger} />
        {/* Prediction trails are rendered beneath the markers so markers stay clickable */}
        {Object.entries(predictions).map(([id, prediction]) => (
          <PredictionTrail key={`trail-${id}`} prediction={prediction} />
        ))}
        {events.map((event) =>
          event.eventType === "LostPet" ? (
            <LostPetMarker key={event.id} event={event} />
          ) : (
            <SightingMarker key={event.id} event={event} />
          ),
        )}
      </LeafletMapContainer>
    </div>
  );
}
