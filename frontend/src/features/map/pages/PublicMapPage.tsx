import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { MapContainer } from "../components/MapContainer";
import { useDebouncedBBox, usePublicMapEvents } from "../hooks/usePublicMap";
import { useMovementPredictions } from "../hooks/useMovementPrediction";
import type { MapBBox } from "../api/publicMapApi";
import { useAuthStore } from "@/features/auth/store/authStore";
import { usePublicClinics } from "@/features/clinics/hooks/useClinics";
import { usePublicStores } from "@/features/stores/hooks/useStores";
import { StoreDetailSheet } from "@/features/stores/components/StoreDetailSheet";
import { CartDrawer } from "@/features/stores/components/CartDrawer";
import { CheckoutModal } from "@/features/stores/components/CheckoutModal";
import { BillboardBanner } from "@/features/advertising/components/BillboardBanner";

export default function PublicMapPage() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [searchParams] = useSearchParams();
  const [bbox, setBbox] = useState<MapBBox | null>(null);
  const [locateTrigger, setLocateTrigger] = useState(0);
  const [locating, setLocating] = useState(false);
  const [legendOpen, setLegendOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [flyTarget, setFlyTarget] = useState<{
    lat: number;
    lng: number;
    zoom?: number;
  } | null>(null);
  const [showClinics, setShowClinics] = useState(false);
  const [showEmergencyOnly, setShowEmergencyOnly] = useState(false);
  const [showStores, setShowStores] = useState(false);
  const [activeStoreId, setActiveStoreId] = useState<string | null>(() =>
    searchParams.get("storeId"),
  );
  const [cartOpen, setCartOpen] = useState(false);
  const [checkoutOpen, setCheckoutOpen] = useState(false);

  // Auto-activate store layer when arriving from directory deep-link
  useEffect(() => {
    if (searchParams.get("storeId")) setShowStores(true);
  }, [searchParams]);

  const { data: publicClinics = [] } = usePublicClinics(
    undefined,
    undefined,
    showClinics,
  );
  const { data: publicStores = [] } = usePublicStores(showStores);

  const displayedClinics = showEmergencyOnly
    ? publicClinics.filter((c) => c.isEmergency24h)
    : publicClinics;

  const { debounce } = useDebouncedBBox(150);
  const { data: events = [], isFetching, isError } = usePublicMapEvents(bbox);

  const filteredEvents = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return events;
    return events.filter((e) => e.petName?.toLowerCase().includes(q));
  }, [events, searchQuery]);

  // When exactly one result matches, fly to it
  const handleSearchChange = (q: string) => {
    setSearchQuery(q);
    const trimmed = q.trim().toLowerCase();
    if (!trimmed) {
      setFlyTarget(null);
      return;
    }
    const matches = events.filter((e) =>
      e.petName?.toLowerCase().includes(trimmed),
    );
    if (
      matches.length === 1 &&
      matches[0]!.lat != null &&
      matches[0]!.lng != null
    ) {
      setFlyTarget({ lat: matches[0]!.lat, lng: matches[0]!.lng, zoom: 14 });
    }
  };

  const lostPetEventIds = useMemo(
    () =>
      filteredEvents.filter((e) => e.eventType === "LostPet").map((e) => e.id),
    [filteredEvents],
  );
  const predictions = useMovementPredictions(lostPetEventIds);

  const handleBBoxChange = useCallback(
    (newBBox: MapBBox) => debounce(setBbox, newBBox),
    [debounce],
  );

  return (
    <div className="relative h-screen w-full">
      <Helmet>
        <title>Mapa en vivo — PawTrack CR</title>
        <meta
          name="description"
          content="Mapa en tiempo real de mascotas perdidas y avistamientos en Costa Rica. Ayuda a reunir mascotas con sus familias."
        />
        <meta property="og:title" content="Mapa en vivo — PawTrack CR" />
        <meta
          property="og:description"
          content="Mascotas perdidas y avistamientos en tiempo real en Costa Rica."
        />
        <meta property="og:type" content="website" />
      </Helmet>
      {/* Glassmorphism header strip */}
      <div className="absolute left-0 right-0 top-0 z-[1000] flex flex-col border-b border-white/10 bg-zinc-900/70 px-4 pt-2.5 backdrop-blur-md">
        <div className="flex items-center justify-between pb-2.5">
          <div className="flex items-center gap-2">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-rescue-400 opacity-75" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-rescue-400" />
            </span>
            <span className="text-sm font-bold text-white tracking-tight">
              PawTrack — Mapa en vivo
            </span>
          </div>
          <span className="text-xs text-zinc-300">
            <span className="font-semibold text-white">
              {searchQuery ? filteredEvents.length : events.length}
            </span>{" "}
            eventos
            {isFetching && (
              <span className="ml-1.5 text-brand-400">• actualizando…</span>
            )}
            {isError && (
              <span className="ml-1.5 text-danger-400">• error al cargar</span>
            )}
          </span>
        </div>
        {/* Search bar */}
        <div className="relative pb-2.5">
          <span
            className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-zinc-400 text-sm"
            aria-hidden="true"
          >
            🔍
          </span>
          <input
            type="search"
            placeholder="Buscar mascota por nombre…"
            value={searchQuery}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="w-full rounded-xl border border-white/10 bg-white/10 py-2 pl-9 pr-4 text-sm text-white placeholder:text-zinc-400 outline-none focus:border-brand-400 focus:bg-white/15 transition"
            aria-label="Filtrar mascotas en el mapa por nombre"
          />
          {searchQuery && (
            <button
              type="button"
              onClick={() => {
                setSearchQuery("");
                setFlyTarget(null);
              }}
              className="absolute inset-y-0 right-2 flex items-center px-2 text-zinc-400 hover:text-white"
              aria-label="Limpiar búsqueda"
            >
              ✕
            </button>
          )}
        </div>
      </div>

      {/* Legend — collapsible on mobile, always visible on sm+ */}
      <div className="absolute bottom-6 left-3 z-[1000] rounded-2xl border border-white/10 bg-zinc-900/70 shadow-xl backdrop-blur-md">
        {/* Toggle button visible only on mobile */}
        <button
          type="button"
          onClick={() => setLegendOpen((o) => !o)}
          className="flex w-full items-center justify-between gap-2 px-3.5 py-3 text-xs font-bold uppercase tracking-widest text-zinc-400 sm:cursor-default sm:pointer-events-none"
          aria-expanded={legendOpen}
          aria-controls="map-legend-items"
        >
          Leyenda
          <span className="sm:hidden">{legendOpen ? "▲" : "▼"}</span>
        </button>
        <div
          id="map-legend-items"
          className={`px-3.5 pb-3 text-xs ${legendOpen ? "block" : "hidden"} sm:block`}
        >
          {[
            { color: "bg-danger-500", label: "Mascota perdida", pulse: true },
            { color: "bg-brand-500", label: "Avistamiento", pulse: false },
            {
              color: "border-2 border-dashed border-trust-400 bg-transparent",
              label: "Trayectoria",
              pulse: false,
            },
            {
              color: "border-2 border-rescue-400 bg-rescue-200/40",
              label: "Zona proyectada",
              pulse: false,
            },
            { color: "bg-trust-500", label: "Clínica", pulse: false },
            {
              color: "bg-brand-300 border-2 border-brand-500",
              label: "Clínica Plus",
              pulse: false,
            },
          ].map(({ color, label, pulse }) => (
            <div
              key={label}
              className="mb-1.5 flex items-center gap-2 last:mb-0"
            >
              <span
                className={`relative inline-flex h-3 w-3 flex-shrink-0 rounded-full ${color}`}
              >
                {pulse && (
                  <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-danger-400 opacity-60" />
                )}
              </span>
              <span className="text-zinc-200">{label}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Controls panel */}
      <div className="absolute bottom-6 right-3 z-[1000] flex flex-col gap-2">
        {isAuthenticated && (
          <Link
            to="/dashboard"
            className="flex items-center gap-2 rounded-xl border border-white/10 bg-zinc-900/70 px-4 py-2.5 text-sm font-semibold text-white shadow-lg backdrop-blur-md transition-colors hover:bg-zinc-800/80"
          >
            ← Dashboard
          </Link>
        )}
        <Link
          to="/estadisticas"
          className="flex items-center gap-2 rounded-xl border border-sand-300 bg-white/95 px-4 py-2.5 text-sm font-semibold text-sand-700 shadow-lg transition-colors hover:bg-sand-50"
        >
          📊 Ver estadísticas
        </Link>
        <Link
          to="/map/match"
          className="flex items-center gap-2 rounded-xl bg-sand-900 px-4 py-2.5 text-sm font-bold text-white shadow-lg transition-colors hover:bg-sand-700"
        >
          🔍 ¿Encontraste un animal?
        </Link>
        <button
          type="button"
          onClick={() => {
            setLocating(true);
            setLocateTrigger((t) => t + 1);
            // Reset spinner after 8 s (matches GPS timeout in LocateUser)
            setTimeout(() => setLocating(false), 8_000);
          }}
          disabled={locating}
          className="flex items-center gap-2 rounded-xl border border-white/10 bg-zinc-900/70 px-4 py-2.5 text-sm font-semibold text-white shadow-lg backdrop-blur-md transition-colors hover:bg-zinc-800/80 disabled:opacity-60"
          aria-label="Centrar mapa en mi ubicación"
        >
          {locating ? (
            <>
              <span className="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />
              Buscando…
            </>
          ) : (
            <>📍 Mi ubicación</>
          )}
        </button>
        {/* Clinics toggle */}
        <button
          type="button"
          onClick={() => setShowClinics((v) => !v)}
          className={`flex items-center gap-2 rounded-xl border px-4 py-2.5 text-sm font-semibold shadow-lg backdrop-blur-md transition-colors ${showClinics ? "border-trust-400 bg-trust-700/90 text-white" : "border-white/10 bg-zinc-900/70 text-zinc-300 hover:bg-zinc-800/80"}`}
        >
          🏥 Clínicas {showClinics ? "✓" : ""}
        </button>
        {/* Emergency-only filter (visible when clinics layer is on) */}
        {showClinics && (
          <button
            type="button"
            onClick={() => setShowEmergencyOnly((v) => !v)}
            aria-pressed={showEmergencyOnly}
            className={`flex items-center gap-1.5 rounded-xl border px-3 py-2.5 text-sm font-semibold shadow-lg backdrop-blur-md transition-colors ${showEmergencyOnly ? "border-danger-400 bg-danger-700/90 text-white" : "border-white/10 bg-zinc-900/70 text-zinc-300 hover:bg-zinc-800/80"}`}
          >
            🚨 Solo emergencias
          </button>
        )}
        {/* Pet stores toggle */}
        <button
          type="button"
          onClick={() => setShowStores((v) => !v)}
          aria-pressed={showStores}
          className={`flex items-center gap-2 rounded-xl border px-4 py-2.5 text-sm font-semibold shadow-lg backdrop-blur-md transition-colors ${showStores ? "border-rescue-400 bg-rescue-700/90 text-white" : "border-white/10 bg-zinc-900/70 text-zinc-300 hover:bg-zinc-800/80"}`}
        >
          🛒 Tiendas {showStores ? "✓" : ""}
        </button>
      </div>

      <MapContainer
        events={filteredEvents}
        predictions={predictions}
        clinics={showClinics ? displayedClinics : undefined}
        stores={showStores ? publicStores : undefined}
        onStoreClick={(id) => { setActiveStoreId(id); setShowStores(true); }}
        locateTrigger={locateTrigger}
        flyTarget={flyTarget}
        onLocated={() => setLocating(false)}
        onBBoxChange={handleBBoxChange}
        className="h-full w-full"
      />

      {/* Store detail sheet */}
      {activeStoreId && (
        <StoreDetailSheet
          storeId={activeStoreId}
          isOpen={!!activeStoreId}
          onClose={() => setActiveStoreId(null)}
          onCheckout={() => { setActiveStoreId(null); setCartOpen(true); }}
        />
      )}
      <CartDrawer
        isOpen={cartOpen}
        onClose={() => setCartOpen(false)}
        onCheckout={() => { setCartOpen(false); setCheckoutOpen(true); }}
      />
      <CheckoutModal isOpen={checkoutOpen} onClose={() => setCheckoutOpen(false)} />

      {/* Billboard for the map placement — subtle non-intrusive slot */}
      {showStores && (
        <div className="absolute bottom-36 left-3 z-[999] w-72">
          <BillboardBanner placement="Map" />
        </div>
      )}

      {/* Clinic count badge when layer is active */}
      {showClinics && (
        <div className="absolute bottom-28 right-3 z-[1000] rounded-full bg-trust-700 px-3 py-1 text-xs font-bold text-white shadow-lg">
          {showEmergencyOnly ? "🚨" : "🏥"} {displayedClinics.length} clínica
          {displayedClinics.length !== 1 ? "s" : ""}
          {showEmergencyOnly && " · 24h"}
          {!showEmergencyOnly &&
            displayedClinics.filter((c) => c.isFeatured).length > 0 &&
            ` · ${displayedClinics.filter((c) => c.isFeatured).length} verificada${displayedClinics.filter((c) => c.isFeatured).length !== 1 ? "s" : ""}`}
        </div>
      )}
    </div>
  );
}
