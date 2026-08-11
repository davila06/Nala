import { Link } from "react-router-dom";
import type { PetSummary } from "../api/petsApi";
import { PetStatusBadge } from "./PetStatusBadge";

const SPECIES_EMOJI: Record<string, string> = {
  Dog: "🐶",
  Cat: "🐱",
  Bird: "🐦",
  Rabbit: "🐰",
  Other: "🐾",
};

const SPECIES_LABEL: Record<string, string> = {
  Dog: "Perro",
  Cat: "Gato",
  Bird: "Ave",
  Rabbit: "Conejo",
  Other: "Otra",
};

interface PetCardProps {
  pet: PetSummary;
}

export const PetCard = ({ pet }: PetCardProps) => (
  <Link
    to={`/pets/${pet.id}`}
    aria-label={`Ver detalles de ${pet.name}${pet.status === "Lost" ? " — perdido/a" : ""}`}
    className={[
      "group relative flex flex-col overflow-hidden rounded-2xl border shadow-sm transition-all duration-200",
      "hover:-translate-y-0.5 hover:shadow-md focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:outline-none",
      pet.status === "Lost"
        ? "border-danger-300 animate-[pulse-border_2s_ease-in-out_infinite]"
        : "border-sand-200 field-input",
    ].join(" ")}
  >
    {/* Photo area */}
    <div
      className={`relative h-44 overflow-hidden ${pet.status === "Lost" ? "bg-danger-50" : "bg-sand-100"}`}
    >
      {pet.photoUrl ? (
        <img
          src={pet.photoUrl}
          alt={pet.name}
          className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
          loading="lazy"
        />
      ) : (
        <div className="flex h-full items-center justify-center text-5xl">
          {SPECIES_EMOJI[pet.species] ?? "🐾"}
        </div>
      )}

      {/* Lost banner overlay */}
      {pet.status === "Lost" && (
        <div className="absolute inset-x-0 top-0 flex items-center justify-center gap-1.5 bg-danger-600/90 py-1.5 text-xs font-semibold uppercase tracking-widest text-white">
          <span aria-hidden="true">⚠</span> Perdido/a
        </div>
      )}
    </div>

    {/* Info */}
    <div className="flex flex-1 flex-col gap-1 p-4">
      <div className="flex items-start justify-between gap-2">
        <p className="flex-1 truncate font-semibold text-sand-900">
          {pet.name}
        </p>
        <PetStatusBadge status={pet.status} />
      </div>
      <p className="text-sm text-sand-500">
        {SPECIES_LABEL[pet.species] ?? pet.species}
        {pet.breed ? ` · ${pet.breed}` : ""}
      </p>

      {/* Quick action for lost pets */}
      {pet.status === "Lost" && pet.activeLostEventId && (
        <span className="mt-1.5 inline-flex items-center gap-1 text-xs font-semibold text-danger-600">
          Ver sala de búsqueda
          <svg
            viewBox="0 0 16 16"
            fill="currentColor"
            className="h-3 w-3"
            aria-hidden="true"
          >
            <path
              fillRule="evenodd"
              d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z"
              clipRule="evenodd"
            />
          </svg>
        </span>
      )}
    </div>
  </Link>
);
