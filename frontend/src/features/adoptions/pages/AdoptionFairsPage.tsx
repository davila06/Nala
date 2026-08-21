import { useState } from "react";
import { Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { FairCard } from "../components/FairCard";
import { useUpcomingFairs } from "../hooks/useAdoptions";

export default function AdoptionFairsPage() {
  const [locating, setLocating] = useState(false);
  const [coords, setCoords] = useState<{ lat: number; lng: number } | null>(
    null,
  );
  const { data: fairs = [], isLoading } = useUpcomingFairs(
    coords?.lat,
    coords?.lng,
  );

  const locate = () => {
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
        <title>Ferias de Adopción · PawTrack CR</title>
        <meta
          name="description"
          content="Eventos y ferias de adopción de mascotas en Costa Rica."
        />
      </Helmet>

      <div className="mx-auto max-w-2xl px-4 py-8 space-y-6">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-xl font-bold text-ink-900">
              🎉 Ferias de adopción
            </h1>
            <p className="text-sm text-sand-500 mt-1">
              Eventos presenciales donde puedes conocer a los animales
            </p>
          </div>
          <Link
            to="/adopciones"
            className="text-sm text-brand-600 hover:underline shrink-0"
          >
            Ver animales →
          </Link>
        </div>

        <button
          onClick={locate}
          disabled={locating}
          className="flex items-center gap-2 rounded-xl border border-sand-200 bg-surface px-4 py-2 text-sm text-ink-700 hover:border-brand-400 disabled:opacity-50 transition-colors"
        >
          {locating
            ? "Buscando…"
            : coords
              ? "📍 Mostrando ferias cerca tuyo"
              : "📍 Buscar ferias en mi zona"}
        </button>

        {isLoading ? (
          <div className="space-y-3">
            {[1, 2].map((i) => (
              <Skeleton key={i} className="h-28 rounded-2xl" />
            ))}
          </div>
        ) : fairs.length === 0 ? (
          <div className="py-16 text-center text-sand-400">
            <p className="text-4xl mb-3">🎪</p>
            <p className="text-sm font-medium">No hay ferias próximas</p>
            <p className="text-sm mt-1">
              {coords
                ? "No encontramos ferias en tu zona."
                : "Activa la ubicación para ver ferias cercanas."}
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {fairs.map((fair) => (
              <FairCard key={fair.id} fair={fair} />
            ))}
          </div>
        )}
      </div>
    </>
  );
}
