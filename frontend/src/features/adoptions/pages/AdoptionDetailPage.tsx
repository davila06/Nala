import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { Skeleton } from "@/shared/ui/Spinner";
import { useAdoptableAnimal } from "../hooks/useAdoptions";
import { SPECIES_LABELS, SIZE_LABELS, AGE_LABELS } from "../api/adoptionsApi";
import { ApplyDrawer } from "../components/ApplyDrawer";
import { useAuthStore } from "@/features/auth/store/authStore";
import { toast } from "@/shared/lib/toast";

export default function AdoptionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: animal, isLoading } = useAdoptableAnimal(id ?? "");
  const [photoIndex, setPhotoIndex] = useState(0);
  const [applyOpen, setApplyOpen] = useState(false);
  const { user } = useAuthStore();

  if (isLoading) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10 space-y-4">
        <Skeleton className="h-72 rounded-2xl" />
        <Skeleton className="h-8 w-48 rounded" />
        <Skeleton className="h-32 rounded-xl" />
      </div>
    );
  }

  if (!animal) {
    return (
      <div className="mx-auto max-w-lg px-4 py-20 text-center">
        <p className="text-4xl mb-3">🐾</p>
        <p className="text-sand-500">Este animal no está disponible.</p>
        <Link
          to="/adopciones"
          className="mt-4 inline-block text-brand-600 underline text-sm"
        >
          Ver todos los animales
        </Link>
      </div>
    );
  }

  const isAvailable = animal.status === "Available";
  const isAuthenticated = !!user;

  return (
    <>
      <Helmet>
        <title>{animal.name} · Adopciones · PawTrack CR</title>
        <meta
          name="description"
          content={`Adopta a ${animal.name} en Costa Rica. ${animal.story.slice(0, 120)}`}
        />
      </Helmet>

      <div className="mx-auto max-w-2xl px-4 py-8 space-y-6">
        {/* Breadcrumb */}
        <nav className="text-xs text-sand-400">
          <Link to="/adopciones" className="hover:text-brand-500">
            Adopciones
          </Link>
          {" / "}
          <span className="text-ink-700">{animal.name}</span>
        </nav>

        {/* Photo gallery */}
        {animal.photoUrls.length > 0 ? (
          <div className="space-y-2">
            <div className="relative h-72 rounded-2xl overflow-hidden bg-sand-100">
              <img
                src={animal.photoUrls[photoIndex]}
                alt={animal.name}
                className="h-full w-full object-cover"
              />
            </div>
            {animal.photoUrls.length > 1 && (
              <div className="flex gap-2">
                {animal.photoUrls.map((url, i) => (
                  <button
                    key={url}
                    onClick={() => setPhotoIndex(i)}
                    className={`h-14 w-14 rounded-lg overflow-hidden border-2 transition-colors ${i === photoIndex ? "border-brand-500" : "border-transparent"}`}
                  >
                    <img
                      src={url}
                      alt=""
                      className="h-full w-full object-cover"
                    />
                  </button>
                ))}
              </div>
            )}
          </div>
        ) : (
          <div className="h-48 rounded-2xl bg-sand-100 flex items-center justify-center text-5xl">
            🐾
          </div>
        )}

        {/* Name + status */}
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-ink-900">{animal.name}</h1>
            <p className="text-sm text-sand-500 mt-0.5">
              {SPECIES_LABELS[animal.species]}
              {animal.breed && ` · ${animal.breed}`}
              {" · "}
              {SIZE_LABELS[animal.size]}
              {" · "}
              {AGE_LABELS[animal.ageCategory]}
            </p>
          </div>
          <div className="text-right shrink-0">
            {isAvailable ? (
              <span className="inline-block bg-green-100 text-green-700 text-xs font-bold px-3 py-1 rounded-full">
                Disponible
              </span>
            ) : animal.status === "InProcess" ? (
              <span className="inline-block bg-warn-100 text-warn-700 text-xs font-bold px-3 py-1 rounded-full">
                En proceso
              </span>
            ) : (
              <span className="inline-block bg-sand-100 text-sand-500 text-xs font-bold px-3 py-1 rounded-full">
                Adoptado ✓
              </span>
            )}
            {animal.refLabel && (
              <p className="text-xs text-sand-400 mt-1">📍 {animal.refLabel}</p>
            )}
          </div>
        </div>

        {/* Health badges */}
        <div className="flex flex-wrap gap-2">
          {animal.isVaccinated && <Chip color="green">✓ Vacunado</Chip>}
          {animal.isSterilized && <Chip color="blue">✓ Castrado</Chip>}
          {animal.isMicrochipped && <Chip color="purple">✓ Microchip</Chip>}
          {animal.okWithKids && <Chip color="yellow">👶 OK con niños</Chip>}
          {animal.okWithDogs && <Chip color="orange">🐕 OK con perros</Chip>}
          {animal.okWithCats && <Chip color="pink">🐈 OK con gatos</Chip>}
          {!animal.needsYard && <Chip color="teal">🏠 Apto apartamento</Chip>}
        </div>

        {/* Story */}
        <section>
          <h2 className="text-sm font-semibold text-ink-800 mb-2">
            Historia y personalidad
          </h2>
          <p className="text-sm text-ink-700 leading-relaxed whitespace-pre-line">
            {animal.story}
          </p>
        </section>

        {/* Requirements */}
        {animal.requirements && (
          <section>
            <h2 className="text-sm font-semibold text-ink-800 mb-2">
              Requisitos para el adoptante
            </h2>
            <p className="text-sm text-ink-700 leading-relaxed">
              {animal.requirements}
            </p>
          </section>
        )}

        {/* Medical notes */}
        {animal.medicalNotes && (
          <section>
            <h2 className="text-sm font-semibold text-ink-800 mb-2">
              Notas médicas
            </h2>
            <p className="text-sm text-ink-700 leading-relaxed">
              {animal.medicalNotes}
            </p>
          </section>
        )}

        {/* Organization */}
        <section className="rounded-xl bg-sand-50 border border-sand-100 p-4">
          <p className="text-xs text-sand-400 mb-1">Publicado por</p>
          <p className="text-sm font-semibold text-ink-800">
            {animal.organizationName}
          </p>
        </section>

        {/* CTA */}
        <div className="sticky bottom-4">
          {isAvailable &&
            (isAuthenticated ? (
              <button
                onClick={() => setApplyOpen(true)}
                className="w-full bg-brand-500 hover:bg-brand-600 text-white font-bold py-3 rounded-2xl shadow-lg transition-colors"
              >
                🐾 Quiero adoptarlo
              </button>
            ) : (
              <Link
                to="/login"
                className="block w-full text-center bg-brand-500 hover:bg-brand-600 text-white font-bold py-3 rounded-2xl shadow-lg transition-colors"
              >
                Inicia sesión para aplicar
              </Link>
            ))}
        </div>
      </div>

      {id && (
        <ApplyDrawer
          animalId={id}
          animalName={animal.name}
          isOpen={applyOpen}
          onClose={() => setApplyOpen(false)}
          onSuccess={() => {
            setApplyOpen(false);
            toast.success(
              "¡Solicitud enviada! La organización te contactará pronto.",
            );
          }}
        />
      )}
    </>
  );
}

function Chip({
  children,
  color,
}: {
  children: React.ReactNode;
  color: string;
}) {
  const colorMap: Record<string, string> = {
    green: "bg-green-50 text-green-700",
    blue: "bg-blue-50 text-blue-700",
    purple: "bg-purple-50 text-purple-700",
    yellow: "bg-yellow-50 text-yellow-700",
    orange: "bg-orange-50 text-orange-700",
    pink: "bg-pink-50 text-pink-700",
    teal: "bg-teal-50 text-teal-700",
  };
  return (
    <span
      className={`inline-block text-xs font-medium px-2.5 py-1 rounded-full ${colorMap[color] ?? "bg-sand-100 text-sand-600"}`}
    >
      {children}
    </span>
  );
}
