import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { usePublicClinics } from "../hooks/useClinics";
import type { PublicClinicDto } from "../api/clinicsApi";

// ── Clinic card ───────────────────────────────────────────────────────────────

function ClinicCard({ clinic }: { clinic: PublicClinicDto }) {
  return (
    <div className="group rounded-2xl border border-sand-100 bg-surface hover:shadow-md hover:-translate-y-0.5 transition-all duration-200 overflow-hidden">
      {/* Logo / placeholder */}
      <div className="relative h-24 bg-sand-100 flex items-center justify-center overflow-hidden">
        {clinic.logoUrl ? (
          <img
            src={clinic.logoUrl}
            alt={clinic.name}
            className="h-full w-full object-cover group-hover:scale-105 transition-transform duration-300"
          />
        ) : (
          <span className="text-4xl select-none opacity-60">🏥</span>
        )}
        {clinic.isFeatured && (
          <span className="absolute top-2 right-2 bg-trust-500 text-white text-[10px] font-bold rounded-full px-2 py-0.5">
            ⭐ Verificada
          </span>
        )}
        {clinic.isEmergency24h && (
          <span className="absolute top-2 left-2 bg-danger-500 text-white text-[10px] font-bold rounded-full px-2 py-0.5">
            🚨 24h
          </span>
        )}
      </div>

      {/* Info */}
      <div className="p-3 space-y-2">
        <p className="font-semibold text-ink-900 text-sm leading-tight line-clamp-1 group-hover:text-brand-600 transition-colors">
          {clinic.name}
        </p>
        <p className="text-xs text-sand-400 line-clamp-1">
          📍 {clinic.address}
        </p>

        <div className="flex flex-wrap gap-1.5 pt-0.5">
          {clinic.phoneNumber && (
            <a
              href={`tel:${clinic.phoneNumber}`}
              onClick={(e) => e.stopPropagation()}
              className="inline-flex items-center gap-1 rounded-lg bg-sand-50 border border-sand-200 px-2 py-1 text-[11px] font-medium text-sand-700 hover:bg-sand-100 transition-colors"
            >
              📞{" "}
              {clinic.isEmergency24h
                ? (clinic.emergencyPhone ?? clinic.phoneNumber)
                : clinic.phoneNumber}
            </a>
          )}
          {clinic.website && (
            <a
              href={clinic.website}
              target="_blank"
              rel="noopener noreferrer"
              onClick={(e) => e.stopPropagation()}
              className="inline-flex items-center gap-1 rounded-lg bg-sand-50 border border-sand-200 px-2 py-1 text-[11px] font-medium text-sand-700 hover:bg-sand-100 transition-colors"
            >
              🌐 Sitio web
            </a>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function ClinicDirectoryPage() {
  const [query, setQuery] = useState("");
  const [showEmergencyOnly, setShowEmergencyOnly] = useState(false);
  const [locating, setLocating] = useState(false);
  const [coords, setCoords] = useState<{ lat: number; lng: number } | null>(
    null,
  );

  const { data: clinics = [], isLoading } = usePublicClinics(
    coords?.lat,
    coords?.lng,
    true,
  );

  const filtered = clinics.filter((c) => {
    const matchesQuery =
      !query ||
      c.name.toLowerCase().includes(query.toLowerCase()) ||
      c.address.toLowerCase().includes(query.toLowerCase());
    const matchesEmergency = !showEmergencyOnly || c.isEmergency24h;
    return matchesQuery && matchesEmergency;
  });

  // Featured clinics appear first
  const sorted = [...filtered].sort((a, b) => {
    if (a.isFeatured && !b.isFeatured) return -1;
    if (!a.isFeatured && b.isFeatured) return 1;
    if (a.isEmergency24h && !b.isEmergency24h) return -1;
    if (!a.isEmergency24h && b.isEmergency24h) return 1;
    return a.name.localeCompare(b.name);
  });

  const handleLocate = () => {
    if (!navigator.geolocation) return;
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setCoords({ lat: pos.coords.latitude, lng: pos.coords.longitude });
        setLocating(false);
      },
      () => setLocating(false),
    );
  };

  return (
    <>
      <Helmet>
        <title>Directorio de Clínicas Veterinarias · PawTrack CR</title>
        <meta
          name="description"
          content="Encuentra clínicas veterinarias afiliadas a PawTrack CR. Emergencias 24h, escaneo de microchip y QR, expediente digital."
        />
      </Helmet>

      <div className="mx-auto max-w-5xl px-4 py-8 space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-ink-900">
            🏥 Clínicas veterinarias
          </h1>
          <p className="text-sand-500 text-sm mt-1">
            Clínicas afiliadas a PawTrack CR · Escaneo de QR y microchip ·
            Expediente digital
            {clinics.length > 0 && ` · ${clinics.length} registradas`}
          </p>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap gap-3 items-center">
          <div className="relative flex-1 min-w-48">
            <input
              type="search"
              placeholder="Buscar por nombre o zona…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="w-full rounded-xl border border-sand-200 bg-surface px-4 py-2.5 text-sm text-ink-800 focus:outline-none focus:ring-2 focus:ring-brand-400 pl-9"
            />
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sand-400 text-sm">
              🔍
            </span>
          </div>

          <label className="flex items-center gap-2 text-sm text-ink-700 cursor-pointer select-none">
            <input
              type="checkbox"
              checked={showEmergencyOnly}
              onChange={(e) => setShowEmergencyOnly(e.target.checked)}
              className="rounded border-sand-300 text-danger-500 focus:ring-danger-400"
            />
            <span>🚨 Solo emergencias 24h</span>
          </label>

          <button
            onClick={handleLocate}
            disabled={locating}
            className="flex items-center gap-2 rounded-xl border border-sand-200 bg-surface px-4 py-2.5 text-sm text-ink-700 hover:border-brand-400 disabled:opacity-50 transition-colors"
          >
            {locating ? "Buscando…" : coords ? "📍 Zona activa" : "📍 Mi zona"}
          </button>

          {coords && (
            <button
              onClick={() => setCoords(null)}
              className="text-xs text-sand-400 hover:text-brand-500 underline transition-colors"
            >
              Limpiar zona
            </button>
          )}
        </div>

        {/* Results count */}
        {!isLoading && (
          <p className="text-xs text-sand-400">
            {sorted.length} clínica{sorted.length !== 1 ? "s" : ""}
            {showEmergencyOnly && " · solo emergencias"}
            {coords && " · cerca de ti"}
            {query && ` · "${query}"`}
          </p>
        )}

        {/* Grid */}
        {isLoading ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="h-48 rounded-2xl" />
            ))}
          </div>
        ) : sorted.length === 0 ? (
          <div className="py-20 text-center text-sand-400">
            <p className="text-4xl mb-3">🔍</p>
            <p className="text-base font-medium">
              No encontramos clínicas con esos filtros
            </p>
            <p className="text-sm mt-1">
              Intenta cambiar la búsqueda o ampliar la zona
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
            {sorted.map((clinic) => (
              <ClinicCard key={clinic.id} clinic={clinic} />
            ))}
          </div>
        )}

        {/* CTA for clinics */}
        <div className="rounded-2xl bg-trust-50 border border-trust-100 p-5 flex items-center justify-between gap-4">
          <div>
            <p className="font-semibold text-trust-800 text-sm">
              ¿Eres una clínica veterinaria?
            </p>
            <p className="text-xs text-trust-600 mt-0.5">
              Únete a la red PawTrack CR. Escanea QR y microchip, aparece en el
              mapa y recibe alertas de mascotas perdidas cercanas.
            </p>
          </div>
          <a
            href="/clinica/registro"
            className="shrink-0 rounded-xl bg-trust-600 hover:bg-trust-700 text-white font-semibold text-sm px-4 py-2 transition-colors"
          >
            Registrarse
          </a>
        </div>
      </div>
    </>
  );
}
