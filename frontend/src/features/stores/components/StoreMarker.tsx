import { divIcon } from "leaflet";
import { Marker, Popup } from "react-leaflet";
import type { PublicStoreDto } from "../api/storesApi";
import { Link } from "react-router-dom";

const featuredIcon = divIcon({
  className: "",
  html: `<div style="
    width:32px;height:32px;border-radius:50%;
    background:#17a26d;border:3px solid #f0b800;
    display:flex;align-items:center;justify-content:center;
    font-size:16px;box-shadow:0 2px 6px rgba(0,0,0,.35);
    color:#fff;line-height:1
  ">🛒</div>`,
  iconSize: [32, 32],
  iconAnchor: [16, 16],
  popupAnchor: [0, -18],
});

const standardIcon = divIcon({
  className: "",
  html: `<div style="
    width:26px;height:26px;border-radius:50%;
    background:#17a26d;border:2px solid #fff;
    display:flex;align-items:center;justify-content:center;
    font-size:13px;box-shadow:0 1px 4px rgba(0,0,0,.25);
    color:#fff;line-height:1
  ">🛒</div>`,
  iconSize: [26, 26],
  iconAnchor: [13, 13],
  popupAnchor: [0, -15],
});

export function StoreMarker({ store }: { store: PublicStoreDto }) {
  return (
    <Marker
      position={[store.lat, store.lng]}
      icon={store.isFeatured ? featuredIcon : standardIcon}
    >
      <Popup maxWidth={220}>
        <div className="text-sm space-y-1.5" style={{ minWidth: 180 }}>
          {store.logoUrl && (
            <img
              src={store.logoUrl}
              alt={store.name}
              style={{
                width: "100%",
                height: 56,
                objectFit: "cover",
                borderRadius: 6,
              }}
            />
          )}
          <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
            <strong style={{ color: "#17a26d" }}>{store.name}</strong>
            {store.isFeatured && (
              <span
                style={{
                  background: "#f0b800",
                  color: "#000",
                  borderRadius: 100,
                  padding: "1px 6px",
                  fontSize: 9,
                  fontWeight: 700,
                }}
              >
                DESTACADA
              </span>
            )}
          </div>
          <p style={{ color: "#6e5244", fontSize: 11, margin: 0 }}>
            📍 {store.address}
          </p>
          {store.phoneNumber && (
            <a
              href={`tel:${store.phoneNumber}`}
              style={{ color: "#17a26d", fontWeight: 600, fontSize: 12 }}
            >
              📞 {store.phoneNumber}
            </a>
          )}
          <Link
            to={`/tiendas/${store.id}`}
            style={{
              display: "block",
              background: "#17a26d",
              color: "#fff",
              borderRadius: 8,
              padding: "4px 10px",
              fontSize: 11,
              fontWeight: 700,
              textAlign: "center",
              marginTop: 4,
            }}
          >
            Ver productos →
          </Link>
        </div>
      </Popup>
    </Marker>
  );
}
