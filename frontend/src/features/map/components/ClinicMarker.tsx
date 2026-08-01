import { divIcon } from "leaflet";
import { Marker, Popup } from "react-leaflet";
import type { PublicClinicDto } from "@/features/clinics/api/clinicsApi";

// Featured (Plus/Partner) clinics get a larger, bordered icon
const featuredIcon = divIcon({
  className: "",
  html: `<div style="
    width:32px;height:32px;border-radius:50%;
    background:#1a3484;border:3px solid #e8521e;
    display:flex;align-items:center;justify-content:center;
    font-size:16px;box-shadow:0 2px 6px rgba(0,0,0,.35);
    color:#fff;line-height:1
  ">🏥</div>`,
  iconSize: [32, 32],
  iconAnchor: [16, 16],
  popupAnchor: [0, -18],
});

const standardIcon = divIcon({
  className: "",
  html: `<div style="
    width:26px;height:26px;border-radius:50%;
    background:#334155;border:2px solid #fff;
    display:flex;align-items:center;justify-content:center;
    font-size:13px;box-shadow:0 1px 4px rgba(0,0,0,.25);
    color:#fff;line-height:1
  ">🏥</div>`,
  iconSize: [26, 26],
  iconAnchor: [13, 13],
  popupAnchor: [0, -15],
});

export function ClinicMarker({ clinic }: { clinic: PublicClinicDto }) {
  const icon = clinic.isFeatured ? featuredIcon : standardIcon;

  return (
    <Marker position={[clinic.lat, clinic.lng]} icon={icon}>
      <Popup maxWidth={220}>
        <div className="text-sm space-y-1.5" style={{ minWidth: 180 }}>
          {clinic.logoUrl && (
            <img
              src={clinic.logoUrl}
              alt={clinic.name}
              style={{
                width: "100%",
                height: 56,
                objectFit: "cover",
                borderRadius: 6,
              }}
            />
          )}
          <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
            <strong style={{ color: "#1a3484" }}>{clinic.name}</strong>
            {clinic.isFeatured && (
              <span
                style={{
                  background: "#e8521e",
                  color: "#fff",
                  borderRadius: 100,
                  padding: "1px 6px",
                  fontSize: 9,
                  fontWeight: 700,
                  letterSpacing: ".06em",
                }}
              >
                VERIFICADA
              </span>
            )}
          </div>
          <p style={{ color: "#6e5244", fontSize: 11, margin: 0 }}>
            📍 {clinic.address}
          </p>
          {clinic.phoneNumber && (
            <a
              href={`tel:${clinic.phoneNumber}`}
              style={{ color: "#e8521e", fontWeight: 600, fontSize: 12 }}
            >
              📞 {clinic.phoneNumber}
            </a>
          )}
          {clinic.website && (
            <a
              href={clinic.website}
              target="_blank"
              rel="noopener noreferrer"
              style={{ display: "block", color: "#1a3484", fontSize: 11 }}
            >
              🌐 Sitio web →
            </a>
          )}
        </div>
      </Popup>
    </Marker>
  );
}
