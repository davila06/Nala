import { useState } from "react";
import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { Button } from "@/shared/ui/Button";
import { useMyAdoptionAnimals, useMarkAdopted } from "../hooks/useAdoptions";
import { SPECIES_LABELS, AGE_LABELS } from "../api/adoptionsApi";
import type { AdoptablePetDto } from "../api/adoptionsApi";
import { toast } from "@/shared/lib/toast";

const STATUS_COLORS: Record<string, string> = {
  Available: "bg-green-50 text-green-700",
  InProcess: "bg-warn-100 text-warn-700",
  Adopted: "bg-sand-100 text-sand-500",
  Paused: "bg-orange-50 text-orange-600",
  Removed: "bg-red-50 text-red-500",
};

const STATUS_LABELS: Record<string, string> = {
  Available: "Disponible",
  InProcess: "En proceso",
  Adopted: "Adoptado",
  Paused: "Pausado",
  Removed: "Removido",
};

function AnimalRow({ animal }: { animal: AdoptablePetDto }) {
  const markAdopted = useMarkAdopted();
  const photo = animal.photoUrls[0];

  return (
    <div className="flex items-center gap-3 p-4 rounded-xl border border-sand-100 bg-surface hover:shadow-sm transition-shadow">
      {/* Thumbnail */}
      <div className="h-14 w-14 rounded-xl overflow-hidden bg-sand-100 shrink-0 flex items-center justify-center">
        {photo ? (
          <img
            src={photo}
            alt={animal.name}
            className="h-full w-full object-cover"
          />
        ) : (
          <span className="text-2xl">🐾</span>
        )}
      </div>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <p className="font-semibold text-sm text-ink-800 truncate">
            {animal.name}
          </p>
          <span
            className={`text-[10px] font-bold px-2 py-0.5 rounded-full shrink-0 ${STATUS_COLORS[animal.status] ?? ""}`}
          >
            {STATUS_LABELS[animal.status] ?? animal.status}
          </span>
        </div>
        <p className="text-xs text-sand-400 mt-0.5">
          {SPECIES_LABELS[animal.species]} · {AGE_LABELS[animal.ageCategory]}
          {animal.refLabel && ` · ${animal.refLabel}`}
        </p>
      </div>

      {/* Actions */}
      <div className="flex gap-2 shrink-0">
        <Link
          to={`/adopciones/${animal.id}`}
          className="text-xs text-brand-600 hover:underline"
        >
          Ver
        </Link>
        <Link
          to={`/shelter/animales/${animal.id}/aplicaciones`}
          className="text-xs text-ink-600 hover:underline"
        >
          Solicitudes
        </Link>
        {animal.status === "InProcess" && (
          <button
            onClick={() =>
              markAdopted.mutate(animal.id, {
                onSuccess: () =>
                  toast.success(`${animal.name} marcado como adoptado ✓`),
              })
            }
            disabled={markAdopted.isPending}
            className="text-xs text-green-600 hover:underline disabled:opacity-50"
          >
            Marcar adoptado
          </button>
        )}
      </div>
    </div>
  );
}

export default function ShelterDashboardPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useMyAdoptionAnimals(page);
  const animals = data?.items ?? [];

  return (
    <>
      <Helmet>
        <title>Panel Shelter · PawTrack CR</title>
      </Helmet>

      <div className="mx-auto max-w-3xl px-4 py-8 space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-ink-900">
              Panel del Shelter
            </h1>
            <p className="text-sm text-sand-500">
              {data?.totalCount ?? 0} animales publicados en total
            </p>
          </div>
          <Link to="/shelter/publicar">
            <Button>+ Publicar animal</Button>
          </Link>
        </div>

        {/* Animal list */}
        {isLoading ? (
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-20 rounded-xl" />
            ))}
          </div>
        ) : animals.length === 0 ? (
          <div className="py-16 text-center text-sand-400">
            <p className="text-4xl mb-3">🐾</p>
            <p className="text-sm font-medium">
              No has publicado animales todavía
            </p>
            <Link
              to="/shelter/publicar"
              className="mt-3 inline-block text-brand-600 underline text-sm"
            >
              Publicar el primer animal
            </Link>
          </div>
        ) : (
          <div className="space-y-2">
            {animals.map((animal) => (
              <AnimalRow key={animal.id} animal={animal} />
            ))}
          </div>
        )}

        {(data?.totalPages ?? 1) > 1 && (
          <div className="flex items-center justify-between pt-4 border-t border-sand-100">
            <button
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="px-4 py-2 rounded-xl border border-sand-200 text-sm disabled:opacity-40"
            >
              ← Anterior
            </button>
            <span className="text-sm text-sand-400">
              Página {page} de {data?.totalPages}
            </span>
            <button
              disabled={!data?.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
              className="px-4 py-2 rounded-xl border border-sand-200 text-sm disabled:opacity-40"
            >
              Siguiente →
            </button>
          </div>
        )}
      </div>
    </>
  );
}
