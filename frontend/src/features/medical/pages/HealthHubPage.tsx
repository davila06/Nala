import { HeartPulse, Plus, ShieldCheck } from "lucide-react";
import { Link } from "react-router-dom";
import { usePets } from "@/features/pets/hooks/usePets";
import { Alert } from "@/shared/ui/Alert";
import { Skeleton } from "@/shared/ui/Spinner";

export default function HealthHubPage() {
  const { data: pets, isLoading, isError } = usePets();

  return (
    <main className="mx-auto max-w-5xl px-4 py-8">
      <header className="mb-6 flex items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase text-brand-600">Salud</p>
          <h1 className="font-display text-2xl font-semibold text-sand-900">Expedientes de tus mascotas</h1>
        </div>
        <Link className="rounded-lg bg-brand-500 p-2 text-white" to="/pets/new" aria-label="Registrar mascota">
          <Plus className="h-5 w-5" />
        </Link>
      </header>

      {isError && <Alert variant="error">No se pudieron cargar los expedientes.</Alert>}
      {isLoading && (
        <div className="grid gap-3 sm:grid-cols-2">
          {[0, 1].map((item) => (
            <Skeleton key={item} className="h-28 rounded-lg" />
          ))}
        </div>
      )}

      {!isLoading && pets?.length === 0 && (
        <div className="border-y border-sand-200 py-12 text-center">
          <HeartPulse className="mx-auto mb-3 h-8 w-8 text-brand-500" />
          <p className="font-semibold text-sand-900">Registra una mascota para crear su expediente.</p>
        </div>
      )}

      <div className="grid gap-3 sm:grid-cols-2">
        {pets?.map((pet) => (
          <Link
            key={pet.id}
            to={`/pets/${pet.id}?tab=salud`}
            className="flex items-center gap-4 rounded-lg border border-sand-200 bg-white p-4 transition hover:border-brand-300"
          >
            {pet.photoUrl ? (
              <img src={pet.photoUrl} alt="" className="h-16 w-16 rounded-lg object-cover" />
            ) : (
              <div className="flex h-16 w-16 items-center justify-center rounded-lg bg-sand-100">
                <HeartPulse className="h-6 w-6 text-sand-500" />
              </div>
            )}
            <div className="min-w-0 flex-1">
              <p className="truncate font-semibold text-sand-900">{pet.name}</p>
              <p className="text-sm text-sand-500">Expediente, vacunas y recordatorios</p>
            </div>
            <ShieldCheck className="h-5 w-5 text-trust-600" aria-hidden="true" />
          </Link>
        ))}
      </div>
    </main>
  );
}
