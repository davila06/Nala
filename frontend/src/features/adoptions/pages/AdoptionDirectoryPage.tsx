import { useEffect } from "react";
import { Helmet } from "react-helmet-async";
import { Link, useSearchParams } from "react-router-dom";
import { Skeleton } from "@/shared/ui/Spinner";
import { AnimalCard } from "../components/AnimalCard";
import { AdoptionFiltersBar } from "../components/AdoptionFiltersBar";
import {
  useAdoptableAnimals,
  useAdoptableAnimalsForMap,
} from "../hooks/useAdoptions";
import type { AdoptionFilters } from "../api/adoptionsApi";
import { BillboardBanner } from "@/features/advertising/components/BillboardBanner";
import { MapContainer } from "@/features/map/components/MapContainer";
import { trackProductEvent } from "@/shared/lib/telemetry";
import { useAuthStore } from "@/features/auth/store/authStore";

const SPECIES = new Set(["Dog", "Cat", "Bird", "Rabbit", "Other"]);
const SIZES = new Set(["XSmall", "Small", "Medium", "Large", "XLarge"]);
const AGE_CATEGORIES = new Set(["Puppy", "Young", "Adult", "Senior"]);

function readPositiveNumber(
  value: string | null,
  fallback?: number,
): number | undefined {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function readCoordinate(value: string | null): number | undefined {
  if (value === null || value.trim() === "") return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function getFilters(searchParams: URLSearchParams): AdoptionFilters {
  const species = searchParams.get("species");
  const size = searchParams.get("size");
  const ageCategory = searchParams.get("ageCategory");
  const lat = readCoordinate(searchParams.get("lat"));
  const lng = readCoordinate(searchParams.get("lng"));

  return {
    page: readPositiveNumber(searchParams.get("page"), 1),
    pageSize: 20,
    species: SPECIES.has(species ?? "")
      ? (species as AdoptionFilters["species"])
      : undefined,
    size: SIZES.has(size ?? "") ? (size as AdoptionFilters["size"]) : undefined,
    ageCategory: AGE_CATEGORIES.has(ageCategory ?? "")
      ? (ageCategory as AdoptionFilters["ageCategory"])
      : undefined,
    isVaccinated: searchParams.get("isVaccinated") === "true" || undefined,
    isSterilized: searchParams.get("isSterilized") === "true" || undefined,
    okWithKids: searchParams.get("okWithKids") === "true" || undefined,
    okWithDogs: searchParams.get("okWithDogs") === "true" || undefined,
    lat,
    lng,
    radiusKm:
      lat !== undefined && lng !== undefined
        ? readPositiveNumber(searchParams.get("radiusKm"), 50)
        : undefined,
  };
}

function toSearchParams(filters: AdoptionFilters, view: "list" | "map") {
  const next = new URLSearchParams();
  if (view === "map") next.set("view", view);
  if (filters.species) next.set("species", filters.species);
  if (filters.size) next.set("size", filters.size);
  if (filters.ageCategory) next.set("ageCategory", filters.ageCategory);
  (
    ["isVaccinated", "isSterilized", "okWithKids", "okWithDogs"] as const
  ).forEach((key) => {
    if (filters[key]) next.set(key, "true");
  });
  if (filters.lat !== undefined && filters.lng !== undefined) {
    next.set("lat", String(filters.lat));
    next.set("lng", String(filters.lng));
    next.set("radiusKm", String(filters.radiusKm ?? 50));
  }
  if ((filters.page ?? 1) > 1) next.set("page", String(filters.page));
  return next;
}

export default function AdoptionDirectoryPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const filters = getFilters(searchParams);
  const view = searchParams.get("view") === "map" ? "map" : "list";
  const { data, isLoading, isError, refetch } = useAdoptableAnimals(filters);
  const { data: mapAnimals = [], isLoading: isMapLoading } =
    useAdoptableAnimalsForMap(filters, view === "map");

  const animals = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const page = filters.page ?? 1;
  useEffect(() => {
    trackProductEvent("AdoptionDirectoryViewed", {
      source: "adoption-directory",
    });
  }, []);

  const updateFilters = (nextFilters: AdoptionFilters) => {
    setSearchParams(toSearchParams(nextFilters, view), { replace: true });
    trackProductEvent("AdoptionFiltersApplied", {
      source: "adoption-directory",
    });
  };

  const updateView = (nextView: "list" | "map") => {
    setSearchParams(toSearchParams(filters, nextView), { replace: true });
    if (nextView === "map") {
      trackProductEvent("AdoptionMapOpened", { source: "adoption-directory" });
    }
  };

  return (
    <>
      <Helmet>
        <title>Adopciones · PawTrack CR</title>
        <meta
          name="description"
          content="Adopta una mascota en Costa Rica. Encuentra perros, gatos y más animales esperando un hogar."
        />
      </Helmet>

      <div className="mx-auto max-w-5xl px-4 py-8 space-y-6">
        <Link
          to={isAuthenticated ? "/dashboard" : "/map"}
          className="inline-flex text-sm font-semibold text-brand-600 hover:underline"
        >
          Volver al inicio
        </Link>
        {/* Header */}
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-ink-900">🐾 Adopciones</h1>
            <p className="text-sand-500 text-sm mt-1">
              Animales buscando hogar en Costa Rica
              {data && ` · ${data.totalCount} disponibles`}
            </p>
          </div>
          <Link
            to="/adopciones/ferias"
            className="rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 text-sm font-semibold text-brand-700 transition-colors hover:border-brand-300 hover:bg-brand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Ver ferias
          </Link>
        </div>
        <BillboardBanner placement="AdoptionDirectory" />

        <div className="flex items-center justify-between gap-3 border-b border-sand-200">
          <div
            role="tablist"
            aria-label="Vista del directorio de adopciones"
            className="flex gap-1"
          >
            <button
              type="button"
              role="tab"
              aria-selected={view === "list"}
              onClick={() => updateView("list")}
              className={`border-b-2 px-3 py-2 text-sm font-semibold transition-colors ${
                view === "list"
                  ? "border-brand-500 text-brand-700"
                  : "border-transparent text-sand-600 hover:text-sand-900"
              }`}
            >
              Lista
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={view === "map"}
              onClick={() => updateView("map")}
              className={`border-b-2 px-3 py-2 text-sm font-semibold transition-colors ${
                view === "map"
                  ? "border-brand-500 text-brand-700"
                  : "border-transparent text-sand-600 hover:text-sand-900"
              }`}
            >
              Mapa
            </button>
          </div>
        </div>

        {view === "map" ? (
          isMapLoading ? (
            <Skeleton className="h-[32rem] rounded-lg" />
          ) : (
            <MapContainer
              events={[]}
              adoptions={mapAnimals}
              onBBoxChange={() => undefined}
              className="h-[32rem] overflow-hidden rounded-lg border border-sand-200"
            />
          )
        ) : (
          <>
            <AdoptionFiltersBar filters={filters} onChange={updateFilters} />
            {isError ? (
              <div className="py-16 text-center text-sand-600">
                <p className="text-base font-semibold text-ink-900">
                  No pudimos cargar las adopciones
                </p>
                <button
                  type="button"
                  onClick={() => void refetch()}
                  className="mt-3 text-sm font-semibold text-brand-600 hover:text-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  Reintentar
                </button>
              </div>
            ) : isLoading ? (
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                {Array.from({ length: 8 }).map((_, i) => (
                  <Skeleton key={i} className="h-64 rounded-2xl" />
                ))}
              </div>
            ) : animals.length === 0 ? (
              <div className="py-20 text-center text-sand-400">
                <p className="text-4xl mb-3">🔍</p>
                <p className="text-base font-medium">
                  No encontramos animales con estos filtros
                </p>
                <p className="text-sm mt-1">
                  Intenta ajustar los filtros o ampliar el radio de búsqueda
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                {animals.map((animal) => (
                  <AnimalCard key={animal.id} animal={animal} />
                ))}
              </div>
            )}

            {totalPages > 1 && (
              <div className="flex items-center justify-between pt-4 border-t border-sand-100">
                <button
                  disabled={page <= 1}
                  onClick={() => updateFilters({ ...filters, page: page - 1 })}
                  className="px-4 py-2 rounded-xl border border-sand-200 text-sm text-ink-700 hover:border-brand-400 disabled:opacity-40 transition-colors"
                >
                  ← Anterior
                </button>
                <span className="text-sm text-sand-400">
                  Página {page} de {totalPages}
                </span>
                <button
                  disabled={!data?.hasNextPage}
                  onClick={() => updateFilters({ ...filters, page: page + 1 })}
                  className="px-4 py-2 rounded-xl border border-sand-200 text-sm text-ink-700 hover:border-brand-400 disabled:opacity-40 transition-colors"
                >
                  Siguiente →
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </>
  );
}
