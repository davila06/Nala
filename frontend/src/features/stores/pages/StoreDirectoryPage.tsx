import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { usePublicStores } from "../hooks/useStores";
import { BillboardBanner } from "@/features/advertising/components/BillboardBanner";
import type { PublicStoreDto } from "../api/storesApi";

function StoreCard({ store }: { store: PublicStoreDto }) {
  return (
    <Link
      to={`/mapa?storeId=${store.id}`}
      className="group rounded-2xl border border-sand-100 bg-surface hover:shadow-md hover:-translate-y-0.5 transition-all duration-200 overflow-hidden"
    >
      {/* Logo / placeholder */}
      <div className="relative h-28 bg-sand-100 flex items-center justify-center overflow-hidden">
        {store.logoUrl ? (
          <img
            src={store.logoUrl}
            alt={store.name}
            className="h-full w-full object-cover"
          />
        ) : (
          <span className="text-4xl select-none">🛒</span>
        )}
        {store.isFeatured && (
          <span className="absolute top-2 right-2 bg-warn-400 text-white text-[10px] font-bold rounded-full px-2 py-0.5">
            ⭐ Destacada
          </span>
        )}
      </div>

      {/* Info */}
      <div className="p-3 space-y-1">
        <p className="font-semibold text-ink-900 text-sm leading-tight line-clamp-1 group-hover:text-brand-600 transition-colors">
          {store.name}
        </p>
        {store.description && (
          <p className="text-xs text-sand-500 line-clamp-2">
            {store.description}
          </p>
        )}
        <p className="text-xs text-sand-400 line-clamp-1">📍 {store.address}</p>
      </div>
    </Link>
  );
}

export default function StoreDirectoryPage() {
  const [query, setQuery] = useState("");
  const { data: stores = [], isLoading } = usePublicStores();

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return stores;
    return stores.filter(
      (s) =>
        s.name.toLowerCase().includes(q) ||
        s.description?.toLowerCase().includes(q) ||
        s.address.toLowerCase().includes(q),
    );
  }, [stores, query]);

  const featured = filtered.filter((s) => s.isFeatured);
  const rest = filtered.filter((s) => !s.isFeatured);

  return (
    <>
      <Helmet>
        <title>Tiendas · PawTrack CR</title>
        <meta
          name="description"
          content="Directorio de tiendas de mascotas en Costa Rica. Encuentra alimentos, accesorios, grooming y más."
        />
      </Helmet>

      <div className="mx-auto max-w-3xl px-4 py-8 space-y-8">
        {/* Header */}
        <div className="space-y-1">
          <h1 className="text-2xl font-bold text-ink-900">
            🛒 Tiendas de mascotas
          </h1>
          <p className="text-sm text-sand-500">
            Descubre tiendas locales y haz tus pedidos directamente desde
            PawTrack CR.
          </p>
        </div>
        {/* Billboard — Directory placement */}
        <BillboardBanner placement="Directory" />
        {/* Search */}
        <div className="relative">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sand-400 pointer-events-none">
            🔍
          </span>
          <input
            type="search"
            placeholder="Buscar por nombre, descripción o dirección…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="w-full rounded-xl border border-sand-200 bg-surface py-2.5 pl-9 pr-4 text-sm text-ink-900 placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
        </div>

        {/* Loading */}
        {isLoading && (
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
            {[...Array(6)].map((_, i) => (
              <Skeleton key={i} className="h-52 rounded-2xl" />
            ))}
          </div>
        )}

        {!isLoading && filtered.length === 0 && (
          <div className="text-center py-16 text-sand-400 space-y-2">
            <p className="text-4xl">🐾</p>
            <p className="font-semibold text-sand-600">
              No encontramos tiendas
            </p>
            {query && (
              <button
                className="text-sm text-brand-500 underline"
                onClick={() => setQuery("")}
              >
                Limpiar búsqueda
              </button>
            )}
          </div>
        )}

        {/* Featured */}
        {featured.length > 0 && (
          <section className="space-y-3">
            <h2 className="text-sm font-semibold text-sand-600 uppercase tracking-wide">
              Tiendas destacadas
            </h2>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
              {featured.map((s) => (
                <StoreCard key={s.id} store={s} />
              ))}
            </div>
          </section>
        )}

        {/* All */}
        {rest.length > 0 && (
          <section className="space-y-3">
            {featured.length > 0 && (
              <h2 className="text-sm font-semibold text-sand-600 uppercase tracking-wide">
                Todas las tiendas
              </h2>
            )}
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
              {rest.map((s) => (
                <StoreCard key={s.id} store={s} />
              ))}
            </div>
          </section>
        )}

        {/* CTA */}
        <div className="rounded-2xl bg-brand-50 border border-brand-100 p-5 text-center space-y-2">
          <p className="font-semibold text-ink-900 text-sm">
            ¿Tienes una tienda de mascotas?
          </p>
          <p className="text-xs text-sand-600">
            Regístrala gratis y empieza a recibir pedidos en minutos.
          </p>
          <Link
            to="/tienda/registro"
            className="inline-block mt-1 rounded-xl bg-brand-500 px-5 py-2 text-sm font-semibold text-white hover:bg-brand-600 transition-colors"
          >
            Registrar mi tienda →
          </Link>
        </div>
      </div>
    </>
  );
}
