import { Link } from "react-router-dom";
import type { AdoptablePetDto, PetSize } from "../api/adoptionsApi";
import { SPECIES_LABELS, AGE_LABELS } from "../api/adoptionsApi";

const SIZE_ORDER: PetSize[] = ["XSmall", "Small", "Medium", "Large", "XLarge"];

const SPECIES_EMOJI: Record<string, string> = {
  Dog: "🐕",
  Cat: "🐈",
  Bird: "🐦",
  Rabbit: "🐇",
  Other: "🐾",
};

interface AnimalCardProps {
  animal: AdoptablePetDto;
}

export function AnimalCard({ animal }: AnimalCardProps) {
  const photo = animal.photoUrls[0];

  return (
    <Link
      to={`/adopciones/${animal.id}`}
      className="group rounded-2xl border border-sand-100 bg-surface hover:shadow-md hover:-translate-y-0.5 transition-all duration-200 overflow-hidden"
    >
      {/* Photo */}
      <div className="relative h-40 bg-sand-100 flex items-center justify-center overflow-hidden">
        {photo ? (
          <img
            src={photo}
            alt={animal.name}
            className="h-full w-full object-cover group-hover:scale-105 transition-transform duration-300"
          />
        ) : (
          <span className="text-5xl select-none opacity-60">
            {SPECIES_EMOJI[animal.species] ?? "🐾"}
          </span>
        )}
        {animal.status === "InProcess" && (
          <span className="absolute top-2 left-2 bg-warn-400 text-white text-[10px] font-bold rounded-full px-2 py-0.5">
            En proceso
          </span>
        )}
        {animal.status === "Adopted" && (
          <span className="absolute top-2 left-2 bg-sand-400 text-white text-[10px] font-bold rounded-full px-2 py-0.5">
            Adoptado ✓
          </span>
        )}
      </div>

      {/* Info */}
      <div className="p-3 space-y-2">
        <div className="flex items-start justify-between gap-1">
          <p className="font-semibold text-ink-900 text-sm leading-tight line-clamp-1 group-hover:text-brand-600 transition-colors">
            {animal.name}
          </p>
          <span className="text-xs text-sand-400 shrink-0">
            {SIZE_ORDER.indexOf(animal.size) <= 1
              ? "pequeño"
              : animal.size === "Medium"
                ? "mediano"
                : "grande"}
          </span>
        </div>

        <p className="text-xs text-sand-500 line-clamp-1">
          {SPECIES_LABELS[animal.species]}
          {animal.breed && ` · ${animal.breed}`}
          {" · "}
          {AGE_LABELS[animal.ageCategory]}
        </p>

        {/* Badges */}
        <div className="flex flex-wrap gap-1">
          {animal.isVaccinated && (
            <span className="inline-flex items-center gap-0.5 bg-green-50 text-green-700 text-[10px] font-medium px-1.5 py-0.5 rounded-full">
              ✓ Vacunado
            </span>
          )}
          {animal.isSterilized && (
            <span className="inline-flex items-center gap-0.5 bg-blue-50 text-blue-700 text-[10px] font-medium px-1.5 py-0.5 rounded-full">
              ✓ Castrado
            </span>
          )}
          {animal.okWithKids && (
            <span className="inline-flex items-center gap-0.5 bg-purple-50 text-purple-700 text-[10px] font-medium px-1.5 py-0.5 rounded-full">
              👶 Niños OK
            </span>
          )}
        </div>

        <p className="text-[11px] text-sand-400 line-clamp-1">
          📍 {animal.refLabel ?? "Costa Rica"}
        </p>

        <p className="text-xs text-sand-500 line-clamp-2 leading-relaxed">
          {animal.story}
        </p>
      </div>
    </Link>
  );
}
