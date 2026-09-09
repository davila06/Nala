import { useDeferredValue, useState } from "react";
import { Helmet } from "react-helmet-async";
import { Link } from "react-router-dom";
import { Skeleton } from "@/shared/ui/Spinner";
import {
  SERVICE_PROVIDER_CATEGORY_LABELS,
  SERVICE_MODALITY_LABELS,
  type PublicServiceProviderDto,
  type ServiceProviderCategory,
  type ServiceModality,
} from "../api/serviceProvidersApi";
import { usePublicServiceProviders } from "../hooks/useServiceProviders";
import { BillboardBanner } from "@/features/advertising/components/BillboardBanner";

const categories: Array<ServiceProviderCategory | "All"> = [
  "All",
  "Trainer",
  "Groomer",
  "Hotel",
  "Daycare",
  "Walker",
  "Photographer",
  "Other",
];

function ProviderCard({ provider }: { provider: PublicServiceProviderDto }) {
  return (
    <Link
      to={`/servicios/${provider.id}`}
      className="group overflow-hidden rounded-xl border border-sand-100 bg-surface transition hover:-translate-y-0.5 hover:shadow-md"
    >
      <div className="relative flex h-28 items-center justify-center bg-trust-50">
        {provider.logoUrl ? (
          <img
            src={provider.logoUrl}
            alt={provider.name}
            className="h-full w-full object-cover"
          />
        ) : (
          <span aria-hidden="true" className="text-4xl">
            🐾
          </span>
        )}
        {provider.isFeatured ? (
          <span className="absolute right-2 top-2 rounded-full bg-warn-400 px-2 py-0.5 text-[10px] font-bold text-white">
            Destacado
          </span>
        ) : null}
      </div>
      <div className="space-y-1 p-3">
        <p className="line-clamp-1 text-sm font-semibold text-ink-900 transition-colors group-hover:text-brand-600">
          {provider.name}
        </p>
        <p className="text-xs font-medium text-brand-600">
          {SERVICE_PROVIDER_CATEGORY_LABELS[provider.category]}
        </p>
        {provider.isVerified ? (
          <p className="text-xs font-semibold text-rescue-700">
            Verificado por PawTrack CR
          </p>
        ) : null}
        <p className="line-clamp-2 text-xs text-sand-500">
          {provider.description}
        </p>
        <p className="line-clamp-1 text-xs text-sand-400">{provider.address}</p>
      </div>
    </Link>
  );
}

export default function ServiceProviderDirectoryPage() {
  const [category, setCategory] = useState<ServiceProviderCategory | "All">(
    "All",
  );
  const [query, setQuery] = useState("");
  const [modality, setModality] = useState<ServiceModality | "All">("All");
  const [minPriceCrc, setMinPriceCrc] = useState("");
  const [maxPriceCrc, setMaxPriceCrc] = useState("");
  const deferredQuery = useDeferredValue(query.trim().toLowerCase());
  const [centerLat, setCenterLat] = useState("");
  const [centerLng, setCenterLng] = useState("");
  const [radiusKm, setRadiusKm] = useState("10");
  const { data: providers = [], isLoading } = usePublicServiceProviders({
    category: category === "All" ? undefined : category,
    modality: modality === "All" ? undefined : modality,
    minPriceCrc: minPriceCrc ? Number(minPriceCrc) : undefined,
    maxPriceCrc: maxPriceCrc ? Number(maxPriceCrc) : undefined,
    centerLat: centerLat ? Number(centerLat) : undefined,
    centerLng: centerLng ? Number(centerLng) : undefined,
    radiusKm: centerLat && centerLng ? Number(radiusKm) : undefined,
  });
  const filtered = deferredQuery
    ? providers.filter((provider) =>
        [provider.name, provider.description, provider.address].some((value) =>
          value.toLowerCase().includes(deferredQuery),
        ),
      )
    : providers;

  return (
    <>
      <Helmet>
        <title>Servicios para mascotas · PawTrack CR</title>
        <meta
          name="description"
          content="Encuentra adiestradores, groomers, hoteles y otros servicios para mascotas en Costa Rica."
        />
      </Helmet>
      <main className="mx-auto max-w-5xl space-y-6 px-4 py-8">
        <header className="space-y-4">
          <Link
            to="/"
            className="inline-flex items-center gap-2 text-sm font-semibold text-brand-600 transition-colors hover:text-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            <span aria-hidden="true">←</span>
            Ir a inicio
          </Link>
          <div className="max-w-2xl space-y-2">
            <p className="text-xs font-semibold uppercase tracking-wide text-brand-600">
              Directorio local
            </p>
            <h1 className="font-display text-3xl font-semibold text-ink-900">
              Servicios para tu mascota
            </h1>
            <p className="text-sm text-sand-600">
              Encuentra apoyo confiable para el cuidado, entrenamiento y
              bienestar de tu companero.
            </p>
          </div>
        </header>
        <BillboardBanner placement="ServiceProviderDirectory" />
        <div className="space-y-3 border-y border-sand-100 py-4">
          <input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Buscar por nombre, servicio o zona"
            className="w-full rounded-lg border border-sand-200 bg-surface px-3 py-2.5 text-sm text-ink-900 placeholder:text-sand-400 focus:outline-none focus:ring-2 focus:ring-brand-400"
          />
          <div className="flex gap-2 overflow-x-auto pb-1">
            {categories.map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setCategory(item)}
                className={`shrink-0 rounded-full border px-3 py-1.5 text-xs font-medium transition ${category === item ? "border-brand-600 bg-brand-600 text-white" : "border-sand-200 bg-surface text-sand-600 hover:border-brand-300"}`}
              >
                {item === "All"
                  ? "Todos"
                  : SERVICE_PROVIDER_CATEGORY_LABELS[item]}
              </button>
            ))}
          </div>
          <div className="grid gap-2 sm:grid-cols-3">
            <select
              value={modality}
              onChange={(event) =>
                setModality(event.target.value as ServiceModality | "All")
              }
              className="rounded-lg border border-sand-200 bg-surface px-3 py-2 text-sm text-sand-700"
            >
              <option value="All">Todas las modalidades</option>
              {Object.entries(SERVICE_MODALITY_LABELS).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
            <input
              type="number"
              min="0"
              value={minPriceCrc}
              onChange={(event) => setMinPriceCrc(event.target.value)}
              placeholder="Precio minimo (CRC)"
              className="rounded-lg border border-sand-200 bg-surface px-3 py-2 text-sm"
            />
            <input
              type="number"
              min="0"
              value={maxPriceCrc}
              onChange={(event) => setMaxPriceCrc(event.target.value)}
              placeholder="Precio maximo (CRC)"
              className="rounded-lg border border-sand-200 bg-surface px-3 py-2 text-sm"
            />
          </div>
          <div className="grid gap-2 sm:grid-cols-3">
            <input
              type="number"
              step="any"
              value={centerLat}
              onChange={(event) => setCenterLat(event.target.value)}
              placeholder="Latitud de referencia"
              className="rounded-lg border border-sand-200 bg-surface px-3 py-2 text-sm"
            />
            <input
              type="number"
              step="any"
              value={centerLng}
              onChange={(event) => setCenterLng(event.target.value)}
              placeholder="Longitud de referencia"
              className="rounded-lg border border-sand-200 bg-surface px-3 py-2 text-sm"
            />
            <input
              type="number"
              min="1"
              max="100"
              value={radiusKm}
              onChange={(event) => setRadiusKm(event.target.value)}
              placeholder="Radio (km)"
              className="rounded-lg border border-sand-200 bg-surface px-3 py-2 text-sm"
            />
          </div>
        </div>
        {isLoading ? (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {Array.from({ length: 8 }, (_, index) => (
              <Skeleton key={index} className="h-52 rounded-xl" />
            ))}
          </div>
        ) : filtered.length > 0 ? (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {filtered.map((provider) => (
              <ProviderCard key={provider.id} provider={provider} />
            ))}
          </div>
        ) : (
          <div className="py-16 text-center text-sm text-sand-500">
            No encontramos proveedores con esos criterios.
          </div>
        )}
        <section className="flex flex-col items-start justify-between gap-3 rounded-xl border border-brand-100 bg-brand-50 p-5 sm:flex-row sm:items-center">
          <div>
            <p className="font-semibold text-ink-900">
              Ofreces servicios para mascotas?
            </p>
            <p className="text-sm text-sand-600">
              Crea tu perfil para aparecer en el directorio.
            </p>
          </div>
          <Link
            to="/servicio/registro"
            className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
          >
            Registrar mi servicio
          </Link>
        </section>
        <p className="text-center text-xs text-sand-400">
          ¿Tienes otro tipo de negocio?{" "}
          <Link
            to="/registro-negocio"
            className="font-semibold text-brand-600 hover:underline"
          >
            Ver todos los perfiles →
          </Link>
        </p>
      </main>
    </>
  );
}
