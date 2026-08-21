import { useState } from "react";
import type {
  AdoptionFilters,
  PetSpecies,
  PetSize,
  AgeCategory,
} from "../api/adoptionsApi";
import { SPECIES_LABELS, SIZE_LABELS, AGE_LABELS } from "../api/adoptionsApi";

interface AdoptionFiltersBarProps {
  filters: AdoptionFilters;
  onChange: (next: AdoptionFilters) => void;
}

export function AdoptionFiltersBar({
  filters,
  onChange,
}: AdoptionFiltersBarProps) {
  const [locating, setLocating] = useState(false);

  const set = (partial: Partial<AdoptionFilters>) =>
    onChange({ ...filters, ...partial, page: 1 });

  const clear = () => onChange({ page: 1 });

  const useMyLocation = () => {
    if (!navigator.geolocation) return;
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        set({ lat: pos.coords.latitude, lng: pos.coords.longitude });
        setLocating(false);
      },
      () => setLocating(false),
    );
  };

  const hasFilters = !!(
    filters.species ||
    filters.size ||
    filters.ageCategory ||
    filters.isVaccinated ||
    filters.isSterilized ||
    filters.okWithKids ||
    filters.okWithDogs ||
    filters.lat
  );

  return (
    <div className="space-y-3">
      {/* Row 1: selects */}
      <div className="flex flex-wrap gap-2">
        <select
          value={filters.species ?? ""}
          onChange={(e) =>
            set({ species: (e.target.value as PetSpecies) || undefined })
          }
          className="rounded-xl border border-sand-200 bg-surface px-3 py-1.5 text-sm text-ink-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          <option value="">Especie</option>
          {(Object.entries(SPECIES_LABELS) as [PetSpecies, string][]).map(
            ([v, l]) => (
              <option key={v} value={v}>
                {l}
              </option>
            ),
          )}
        </select>

        <select
          value={filters.size ?? ""}
          onChange={(e) =>
            set({ size: (e.target.value as PetSize) || undefined })
          }
          className="rounded-xl border border-sand-200 bg-surface px-3 py-1.5 text-sm text-ink-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          <option value="">Tamaño</option>
          {(Object.entries(SIZE_LABELS) as [PetSize, string][]).map(
            ([v, l]) => (
              <option key={v} value={v}>
                {l}
              </option>
            ),
          )}
        </select>

        <select
          value={filters.ageCategory ?? ""}
          onChange={(e) =>
            set({ ageCategory: (e.target.value as AgeCategory) || undefined })
          }
          className="rounded-xl border border-sand-200 bg-surface px-3 py-1.5 text-sm text-ink-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          <option value="">Edad</option>
          {(Object.entries(AGE_LABELS) as [AgeCategory, string][]).map(
            ([v, l]) => (
              <option key={v} value={v}>
                {l}
              </option>
            ),
          )}
        </select>
      </div>

      {/* Row 2: checkboxes + location */}
      <div className="flex flex-wrap items-center gap-3">
        {(
          [
            ["isVaccinated", "Vacunado"],
            ["isSterilized", "Castrado"],
            ["okWithKids", "OK niños"],
            ["okWithDogs", "OK perros"],
          ] as [keyof AdoptionFilters, string][]
        ).map(([key, label]) => (
          <label
            key={key}
            className="flex items-center gap-1.5 cursor-pointer text-sm text-ink-700"
          >
            <input
              type="checkbox"
              checked={!!filters[key]}
              onChange={(e) => set({ [key]: e.target.checked || undefined })}
              className="rounded border-sand-300 text-brand-500 focus:ring-brand-400"
            />
            {label}
          </label>
        ))}

        <button
          onClick={useMyLocation}
          disabled={locating}
          className="ml-auto flex items-center gap-1.5 rounded-xl border border-sand-200 bg-surface px-3 py-1.5 text-sm text-ink-700 hover:border-brand-400 transition-colors disabled:opacity-50"
        >
          {locating ? "Buscando…" : "📍 Mi zona"}
        </button>

        {filters.lat && (
          <select
            value={filters.radiusKm ?? 50}
            onChange={(e) => set({ radiusKm: Number(e.target.value) })}
            className="rounded-xl border border-sand-200 bg-surface px-3 py-1.5 text-sm text-ink-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
          >
            {[10, 25, 50, 100].map((r) => (
              <option key={r} value={r}>
                {r} km
              </option>
            ))}
          </select>
        )}

        {hasFilters && (
          <button
            onClick={clear}
            className="text-xs text-sand-400 hover:text-brand-500 underline transition-colors"
          >
            Limpiar
          </button>
        )}
      </div>
    </div>
  );
}
