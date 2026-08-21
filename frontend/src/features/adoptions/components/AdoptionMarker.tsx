import { divIcon } from "leaflet";
import { Marker, Popup } from "react-leaflet";
import { Link } from "react-router-dom";
import type { AdoptablePetDto } from "../api/adoptionsApi";
import { SPECIES_LABELS } from "../api/adoptionsApi";

const adoptionIcon = divIcon({
  className: "",
  html: `<div style="
    width:28px;height:28px;border-radius:50%;
    background:#7c3aed;border:2px solid #fff;
    display:flex;align-items:center;justify-content:center;
    font-size:14px;box-shadow:0 2px 6px rgba(0,0,0,.3);
    line-height:1
  ">🐾</div>`,
  iconSize: [28, 28],
  iconAnchor: [14, 14],
  popupAnchor: [0, -16],
});

export function AdoptionMarker({
  animal,
  onClick,
}: {
  animal: AdoptablePetDto;
  onClick?: (id: string) => void;
}) {
  return (
    <Marker
      position={[animal.refLat, animal.refLng]}
      icon={adoptionIcon}
      eventHandlers={{ click: () => onClick?.(animal.id) }}
    >
      <Popup maxWidth={200}>
        <div style={{ minWidth: 160 }} className="space-y-1.5 text-sm">
          {animal.photoUrls[0] && (
            <img
              src={animal.photoUrls[0]}
              alt={animal.name}
              style={{
                width: "100%",
                height: 48,
                objectFit: "cover",
                borderRadius: 6,
              }}
            />
          )}
          <strong style={{ color: "#7c3aed" }}>{animal.name}</strong>
          <p style={{ fontSize: 11, color: "#6e5244", margin: 0 }}>
            {SPECIES_LABELS[animal.species]}
            {animal.breed && ` · ${animal.breed}`}
          </p>
          {animal.refLabel && (
            <p style={{ fontSize: 10, color: "#9a8578", margin: 0 }}>
              📍 {animal.refLabel}
            </p>
          )}
          <Link
            to={`/adopciones/${animal.id}`}
            style={{
              display: "block",
              background: "#7c3aed",
              color: "#fff",
              borderRadius: 8,
              padding: "4px 10px",
              fontSize: 11,
              fontWeight: 700,
              textAlign: "center",
              marginTop: 4,
            }}
          >
            Ver perfil →
          </Link>
        </div>
      </Popup>
    </Marker>
  );
}
