import { useState } from "react";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { AnimalCard } from "../components/AnimalCard";
import { AdoptionFiltersBar } from "../components/AdoptionFiltersBar";
import { useAdoptableAnimals } from "../hooks/useAdoptions";
import type { AdoptionFilters } from "../api/adoptionsApi";

export default function AdoptionDirectoryPage() {
  const [filters, setFilters] = useState<AdoptionFilters>({ page: 1, pageSize: 20 });
  const { data, isLoading } = useAdoptableAnimals(filters);

  const animals = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const page = filters.page ?? 1;

  return (
    <>
      <Helmet>
        <title>Adopciones · PawTrack CR</title>
        <meta name="description" content="Adopta una mascota en Costa Rica. Encuentra perros, gatos y más animales esperando un hogar." />
      </Helmet>

      <div className="mx-auto max-w-5xl px-4 py-8 space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-ink-900">🐾 Adopciones</h1>
          <p className="text-sand-500 text-sm mt-1">
            Animales buscando hogar en Costa Rica
            {data && ` · ${data.totalCount} disponibles`}
          </p>
        </div>

        {/* Filters */}
        <AdoptionFiltersBar filters={filters} onChange={setFilters} />

        {/* Grid */}
        {isLoading ? (
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

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between pt-4 border-t border-sand-100">
            <button
              disabled={page <= 1}
              onClick={() => setFilters((f) => ({ ...f, page: (f.page ?? 1) - 1 }))}
              className="px-4 py-2 rounded-xl border border-sand-200 text-sm text-ink-700 hover:border-brand-400 disabled:opacity-40 transition-colors"
            >
              ← Anterior
            </button>
            <span className="text-sm text-sand-400">
              Página {page} de {totalPages}
            </span>
            <button
              disabled={!data?.hasNextPage}
              onClick={() => setFilters((f) => ({ ...f, page: (f.page ?? 1) + 1 }))}
              className="px-4 py-2 rounded-xl border border-sand-200 text-sm text-ink-700 hover:border-brand-400 disabled:opacity-40 transition-colors"
            >
              Siguiente →
            </button>
          </div>
        )}
      </div>
    </>
  );
}
