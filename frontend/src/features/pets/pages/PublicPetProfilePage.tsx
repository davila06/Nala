import {
  useState,
  useRef,
  useEffect,
  useCallback,
  lazy,
  Suspense,
} from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { SharePetButton } from "@/features/lost-pets/components/SharePetButton";
import { useGetLostPetContact } from "@/features/lost-pets/hooks/useLostPets";
import { FraudReportButton } from "@/features/safety/components/FraudReportButton";
import { PetStatusBadge } from "../components/PetStatusBadge";
import { usePublicPetProfile } from "../hooks/usePets";
import { useAuthStore } from "@/features/auth/store/authStore";

// Lazy-load the 3D tag (Three.js is heavy — only load when needed)
const PetTag3D = lazy(() =>
  import("../components/PetTag3D").then((m) => ({ default: m.PetTag3D })),
);

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

/** Parallax hero photo — layers move at different rates on scroll */
function ParallaxHero({
  photoUrl,
  petName,
  species,
  isLost,
}: {
  photoUrl?: string | null;
  petName: string;
  species: string;
  isLost: boolean;
}) {
  const heroRef = useRef<HTMLDivElement>(null);
  const imgRef = useRef<HTMLImageElement | HTMLDivElement>(null);

  const handleScroll = useCallback(() => {
    const hero = heroRef.current;
    if (!hero) return;
    const rect = hero.getBoundingClientRect();
    const scrolled = -rect.top;
    if (scrolled < 0) return;
    const rate = scrolled * 0.35;
    if (imgRef.current) {
      (imgRef.current as HTMLElement).style.transform =
        `translateY(${rate}px) scale(1.15)`;
    }
  }, []);

  useEffect(() => {
    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, [handleScroll]);

  return (
    <div
      ref={heroRef}
      className="relative h-80 overflow-hidden"
      style={{ isolation: "isolate" }}
    >
      {/* Background layer (blurred duplicate) */}
      {photoUrl && (
        <div
          className="absolute inset-0 scale-110 blur-2xl opacity-40"
          style={{
            backgroundImage: `url(${photoUrl})`,
            backgroundSize: "cover",
            backgroundPosition: "center",
            transform: "scale(1.2)",
          }}
          aria-hidden="true"
        />
      )}

      {/* Main photo */}
      {photoUrl ? (
        <img
          ref={imgRef as React.Ref<HTMLImageElement>}
          src={photoUrl}
          alt={petName}
          loading="eager"
          className="absolute inset-0 h-full w-full object-cover will-change-transform"
          style={{ transform: "translateY(0) scale(1.15)", transition: "none" }}
        />
      ) : (
        <div
          className="flex h-full w-full items-center justify-center text-9xl"
          style={{
            background: isLost
              ? "linear-gradient(135deg, #fff4f4 0%, #ffe4e4 100%)"
              : "linear-gradient(135deg, #fff8f4 0%, #ffe8d9 100%)",
          }}
        >
          {SPECIES_EMOJI[species] ?? "🐾"}
        </div>
      )}

      {/* Gradient scrim bottom */}
      <div
        className="absolute inset-x-0 bottom-0 h-48 pointer-events-none"
        style={{
          background:
            "linear-gradient(to top, rgba(255,255,255,1) 0%, rgba(255,255,255,0.6) 40%, transparent 100%)",
        }}
        aria-hidden="true"
      />

      {/* Lost alert overlay */}
      {isLost && (
        <div className="absolute inset-x-0 top-0 flex items-center justify-center gap-2 bg-danger-600/90 py-2.5 backdrop-blur-sm">
          {/* Pulsing dot */}
          <span className="relative flex h-2.5 w-2.5">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full field-input opacity-75" />
            <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-white" />
          </span>
          <span className="text-sm font-bold uppercase tracking-widest text-white">
            Mascota perdida — Necesita ayuda
          </span>
        </div>
      )}
    </div>
  );
}

export default function PublicPetProfilePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: pet, isLoading, isError } = usePublicPetProfile(id ?? "");
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const currentUserId = useAuthStore((s) => s.user?.id);

  const [revealPhone, setRevealPhone] = useState(false);
  const { data: contact, isLoading: contactLoading } = useGetLostPetContact(
    revealPhone ? (pet?.activeLostEventId ?? null) : null,
  );

  const handleRevealPhone = () => {
    if (!isAuthenticated) {
      navigate(`/login?return=/p/${id ?? ""}`);
      return;
    }
    setRevealPhone(true);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-white">
        <div className="animate-pulse">
          <div className="h-80 bg-sand-100" />
          <div className="p-5 space-y-3">
            <div className="h-8 w-40 rounded-xl bg-sand-100" />
            <div className="h-5 w-28 rounded bg-sand-100" />
            <div className="h-14 rounded-2xl bg-sand-100 mt-6" />
          </div>
        </div>
      </div>
    );
  }

  if (isError || !pet) {
    return (
      <div
        role="alert"
        className="flex min-h-screen flex-col items-center justify-center gap-4 field-input px-6 text-center"
      >
        <span className="text-6xl" aria-hidden="true">
          🔍
        </span>
        <h1 className="text-2xl font-bold text-sand-900">
          Perfil no encontrado
        </h1>
        <p className="text-sm text-sand-500">
          Este código QR puede ya no estar activo o la mascota fue eliminada.
        </p>
        <Link
          to="/"
          className="mt-2 rounded-xl bg-brand-500 px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-600"
        >
          Ir a PawTrack CR
        </Link>
      </div>
    );
  }

  const isLost = pet.status === "Lost";
  const isOwner = isAuthenticated && currentUserId === pet.ownerId;
  const speciesLabel = SPECIES_LABEL[pet.species] ?? pet.species;
  const pageTitle = isLost
    ? `¡${pet.name} está perdido/a! — PawTrack CR`
    : `${pet.name} — Perfil público | PawTrack CR`;
  const pageDescription = isLost
    ? `${pet.name} es un/a ${speciesLabel.toLowerCase()} que está perdido/a. Ayuda a encontrarlo escaneando este perfil en PawTrack CR.`
    : `Perfil público de ${pet.name}, ${speciesLabel.toLowerCase()}. Escanea para ver información de contacto y reportar avistamientos.`;

  return (
    <div className="min-h-screen bg-white">
      <Helmet>
        <title>{pageTitle}</title>
        <meta name="description" content={pageDescription} />
        <meta property="og:title" content={pageTitle} />
        <meta property="og:description" content={pageDescription} />
        {pet.photoUrl && <meta property="og:image" content={pet.photoUrl} />}
        <meta property="og:type" content="profile" />
      </Helmet>
      {/* Cinematic parallax hero */}
      <ParallaxHero
        photoUrl={pet.photoUrl}
        petName={pet.name}
        species={pet.species}
        isLost={isLost}
      />

      {/* Content floats over the hero gradient */}
      <div className="relative -mt-12 px-5 pb-12">
        {/* Name + badge floating card */}
        <div
          className={[
            "mb-5 rounded-2xl border p-4 shadow-lg backdrop-blur-sm",
            isLost
              ? "border-danger-200 bg-white/95"
              : "border-sand-200 bg-white/95",
          ].join(" ")}
        >
          <div className="mb-1 flex items-center gap-2.5">
            <h1 className="font-display text-3xl font-semibold text-sand-900">
              {pet.name}
            </h1>
            <PetStatusBadge status={pet.status} />
          </div>
          <p className="text-sm text-sand-500">
            {SPECIES_LABEL[pet.species] ?? pet.species}
            {pet.breed ? ` · ${pet.breed}` : ""}
          </p>

          {/* Lost public message */}
          {isLost && pet.publicMessage && (
            <p className="mt-3 rounded-xl bg-danger-50 px-3 py-2.5 text-sm text-danger-800 leading-relaxed border border-danger-100">
              {pet.publicMessage}
            </p>
          )}
        </div>

        {/* 3D floating pet tag — the hero differentiator */}
        <div className="mb-5">
          <Suspense
            fallback={
              <div className="flex h-60 items-center justify-center text-sand-300 text-xs">
                Cargando placa 3D…
              </div>
            }
          >
            <PetTag3D
              petName={pet.name}
              isLost={isLost}
              species={pet.species}
              height={220}
            />
          </Suspense>
        </div>

        {/* Primary CTA: Report sighting (hidden for the pet's owner) */}
        {!isOwner && (
          <Link
            to={`/p/${pet.id}/report-sighting`}
            className={[
              "mb-3 flex w-full items-center justify-center gap-2.5 rounded-2xl py-4 text-base font-bold text-white shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2",
              isLost
                ? "bg-danger-500 hover:bg-danger-600 focus-visible:ring-danger-400 shadow-danger-200"
                : "bg-brand-500 hover:bg-brand-600 focus-visible:ring-brand-400",
            ].join(" ")}
          >
            <span aria-hidden="true" className="text-xl">
              🐾
            </span>
            {isLost
              ? "Vi a esta mascota — Reportar avistamiento"
              : "Reportar avistamiento"}
          </Link>
        )}

        {/* Owner shortcut */}
        {isOwner && (
          <Link
            to={`/pets/${pet.id}`}
            className="mb-3 flex w-full items-center justify-center gap-2.5 rounded-2xl border-2 border-brand-400 bg-brand-50 py-3.5 text-base font-bold text-brand-700 shadow-sm transition-all hover:bg-brand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            <span aria-hidden="true">⚙️</span> Administrar esta mascota
          </Link>
        )}

        {/* Share */}
        <SharePetButton
          petId={pet.id}
          petName={pet.name}
          variant="outline"
          className="mb-4"
        />

        {/* Safe chat CTA (hidden for the pet's owner — no self-chat) */}
        {!isOwner && isLost && pet.activeLostEventId && pet.ownerId && (
          <Link
            to={`/chat/${pet.activeLostEventId}/${pet.ownerId}`}
            className="mb-3 flex w-full items-center justify-center gap-2 rounded-2xl border border-sand-200 field-input py-3.5 text-sm font-semibold text-sand-700 shadow-sm hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
          >
            <span aria-hidden="true">💬</span> Contactar al dueño (chat seguro)
          </Link>
        )}

        {/* Contact card (hidden for owner) */}
        {!isOwner &&
          isLost &&
          pet.activeLostEventId &&
          (pet.contactName ?? contact?.contactName) && (
            <div className="mb-4 rounded-2xl border border-brand-200 bg-gradient-to-br from-brand-50 to-white p-4 shadow-sm">
              <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-brand-700">
                Contacto del dueño
              </p>
              <p className="mb-3 text-sm font-semibold text-sand-800">
                {pet.contactName ?? contact?.contactName}
              </p>
              {contact?.contactPhone ? (
                <a
                  href={`tel:${contact.contactPhone}`}
                  className="flex items-center gap-2 rounded-xl bg-brand-500 px-4 py-2.5 text-sm font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
                >
                  <span aria-hidden="true">📞</span> {contact.contactPhone}
                </a>
              ) : (
                <button
                  type="button"
                  onClick={handleRevealPhone}
                  disabled={contactLoading}
                  className="flex w-full items-center justify-center gap-2 rounded-xl border border-brand-400 field-input px-4 py-2.5 text-sm font-semibold text-brand-700 hover:bg-brand-50 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  {contactLoading ? (
                    <span className="inline-block h-4 w-4 animate-spin rounded-full border-2 border-brand-400 border-t-transparent" />
                  ) : (
                    <>
                      <span aria-hidden="true">📞</span> Ver número de teléfono
                    </>
                  )}
                </button>
              )}
            </div>
          )}

        {/* PawTrack attribution */}
        <div className="mt-8 flex flex-col items-center gap-3">
          <FraudReportButton
            context="PublicProfile"
            relatedEntityId={pet.activeLostEventId}
            targetUserId={pet.ownerId}
          />
          <Link
            to="/"
            className="rounded text-xs font-bold tracking-wider text-sand-400 hover:text-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Powered by PawTrack CR
          </Link>
        </div>
      </div>
    </div>
  );
}
