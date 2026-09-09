import { divIcon } from "leaflet";
import { Marker, Popup } from "react-leaflet";
import {
  SERVICE_PROVIDER_CATEGORY_LABELS,
  type PublicServiceProviderDto,
} from "@/features/service-providers/api/serviceProvidersApi";

const icons = {
  Groomer: "✂️",
  Trainer: "🎓",
  Hotel: "🏨",
  Daycare: "🐶",
  Walker: "🦮",
  Photographer: "📷",
  Other: "🐾",
} as const;

function providerIcon(category: PublicServiceProviderDto["category"]) {
  return divIcon({
    className: "",
    html: `<div style="width:28px;height:28px;border-radius:50%;background:#e8521e;border:2px solid #fff;display:flex;align-items:center;justify-content:center;font-size:14px;box-shadow:0 1px 5px rgba(0,0,0,.3);line-height:1">${icons[category]}</div>`,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
    popupAnchor: [0, -16],
  });
}

export function ServiceProviderMarker({
  provider,
  isAuthenticated,
}: {
  provider: PublicServiceProviderDto;
  isAuthenticated: boolean;
}) {
  return (
    <Marker
      position={[provider.lat, provider.lng]}
      icon={providerIcon(provider.category)}
    >
      <Popup maxWidth={240}>
        <div className="space-y-1.5 text-sm" style={{ minWidth: 190 }}>
          <strong style={{ color: "#e8521e" }}>{provider.name}</strong>
          <p style={{ color: "#6e5244", fontSize: 11, margin: 0 }}>
            {SERVICE_PROVIDER_CATEGORY_LABELS[provider.category]}
          </p>
          <p style={{ color: "#6e5244", fontSize: 11, margin: 0 }}>
            📍 {provider.address}
          </p>
          {provider.phoneNumber && (
            <a
              href={`tel:${provider.phoneNumber}`}
              style={{ color: "#e8521e", fontWeight: 600, fontSize: 12 }}
            >
              📞 {provider.phoneNumber}
            </a>
          )}
          {provider.whatsAppNumber && (
            <a
              href={`https://wa.me/${provider.whatsAppNumber}?text=${encodeURIComponent(`Hola, encontré ${provider.name} en PawTrack y quiero consultar sus servicios.`)}`}
              target="_blank"
              rel="noopener noreferrer"
              style={{
                display: "block",
                color: "#128c7e",
                fontWeight: 700,
                fontSize: 12,
              }}
            >
              WhatsApp →
            </a>
          )}
          {provider.website && (
            <a
              href={provider.website}
              target="_blank"
              rel="noopener noreferrer"
              style={{ display: "block", color: "#1a3484", fontSize: 11 }}
            >
              🌐 Sitio web
            </a>
          )}
          {isAuthenticated && (
            <a
              href={`/servicios/${provider.id}#reservas`}
              style={{
                display: "block",
                marginTop: 6,
                borderRadius: 6,
                background: "#e8521e",
                color: "#fff",
                fontWeight: 700,
                fontSize: 12,
                padding: "6px 8px",
                textAlign: "center",
              }}
            >
              Reservar servicio
            </a>
          )}
        </div>
      </Popup>
    </Marker>
  );
}
