import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { HolographicPetCard } from "../components/HolographicPetCard";
import {
  OnboardingWizard,
  shouldShowOnboarding,
} from "../components/OnboardingWizard";
import { FreemiumModal } from "../components/FreemiumModal";
import { usePets } from "../hooks/usePets";
import { useMyTier } from "../hooks/useMyTier";
import { useAuthStore } from "@/features/auth/store/authStore";
import { AlertPreferencesToggle } from "@/features/locations/components/AlertPreferencesToggle";
import { LeaderboardWidget } from "@/features/incentives/components/LeaderboardWidget";
import { BillboardBanner } from "@/features/advertising/components/BillboardBanner";
import { Alert } from "@/shared/ui/Alert";
import { Skeleton } from "@/shared/ui/Spinner";
import { EmptyState } from "@/shared/ui/Card";
import { usePullToRefresh } from "@/shared/hooks/usePullToRefresh";

export default function DashboardPage() {
  const { data: pets, isLoading, isError, refetch } = usePets();
  const user = useAuthStore((s) => s.user);
  const { isPlus, isFamilia } = useMyTier();
  const lostCount = useMemo(
    () => (pets ?? []).filter((p) => p.status === "Lost").length,
    [pets],
  );
  const petLimit = isFamilia ? -1 : isPlus ? 3 : 1;
  const petCount = pets?.length ?? 0;
  const atPetLimit = petLimit !== -1 && petCount >= petLimit;
  const [search, setSearch] = useState("");
  const [filterStatus, setFilterStatus] = useState<"all" | "Lost" | "Active">(
    "all",
  );
  const [filterSpecies, setFilterSpecies] = useState<string>("all");
  const [onboardingDismissed, setOnboardingDismissed] = useState(false);
  const [showFreemium, setShowFreemium] = useState(false);

  // useState(fn) ignores the cleanup return — must use useEffect for side effects
  useEffect(() => {
    const handler = () => setShowFreemium(true);
    window.addEventListener("pawtrack:open-upgrade-modal", handler);
    return () =>
      window.removeEventListener("pawtrack:open-upgrade-modal", handler);
  }, []);

  const filteredPets = useMemo(() => {
    if (!pets) return [];
    return pets.filter((p) => {
      const matchesSearch =
        search === "" || p.name.toLowerCase().includes(search.toLowerCase());
      const matchesStatus = filterStatus === "all" || p.status === filterStatus;
      const matchesSpecies =
        filterSpecies === "all" || p.species === filterSpecies;
      return matchesSearch && matchesStatus && matchesSpecies;
    });
  }, [pets, search, filterStatus, filterSpecies]);

  const species = useMemo(
    () => [...new Set((pets ?? []).map((p) => p.species))],
    [pets],
  );

  const handleRefresh = useCallback(async () => {
    await refetch();
  }, [refetch]);

  const { containerRef, pullProgress, isRefreshing } = usePullToRefresh({
    onRefresh: handleRefresh,
    enabled: !isLoading,
  });

  return (
    <>
      {/* Onboarding wizard — only for users with no pets who haven't dismissed it */}
      {!isLoading &&
        !isError &&
        pets?.length === 0 &&
        !onboardingDismissed &&
        shouldShowOnboarding() && (
          <OnboardingWizard onDismiss={() => setOnboardingDismissed(true)} />
        )}

      <div
        ref={containerRef}
        className="mx-auto max-w-5xl px-4 py-8 animate-fade-in-up overflow-auto"
      >
        {/* Pull-to-refresh indicator */}
        {(pullProgress > 0 || isRefreshing) && (
          <div
            className="flex items-center justify-center gap-2 overflow-hidden transition-all"
            style={{
              height: `${Math.max(pullProgress, isRefreshing ? 1 : 0) * 44}px`,
              opacity: Math.max(pullProgress, isRefreshing ? 1 : 0),
            }}
          >
            <div
              className={`h-5 w-5 rounded-full border-2 border-brand-300 border-t-brand-500 ${isRefreshing ? "animate-spin" : ""}`}
              style={{ transform: `rotate(${pullProgress * 360}deg)` }}
            />
            <span className="text-xs text-sand-400">
              {isRefreshing ? "Actualizando…" : "Suelta para actualizar"}
            </span>
          </div>
        )}
        {/* Header */}
        <div className="mb-8 flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-medium text-sand-400 uppercase tracking-wide">
              {new Date().toLocaleDateString("es-CR", {
                weekday: "long",
                day: "numeric",
                month: "long",
              })}
            </p>
            <h1 className="font-display text-2xl font-semibold text-sand-900">
              {user?.name ? `Hola, ${user.name.split(" ")[0]}` : "Mis mascotas"}
            </h1>
            <div className="mt-1 flex items-center gap-2">
              <p className="text-sm text-sand-500">
                {pets?.length ?? 0} mascota{pets?.length !== 1 ? "s" : ""}{" "}
                registrada{pets?.length !== 1 ? "s" : ""}
              </p>
              {lostCount > 0 && (
                <span className="inline-flex items-center gap-1 rounded-full bg-danger-100 px-2 py-0.5 text-xs font-bold text-danger-700">
                  ⚠️ {lostCount} perdida{lostCount !== 1 ? "s" : ""}
                </span>
              )}
            </div>
          </div>
          <div className="flex flex-col items-end gap-1.5">
            {!isFamilia && petCount > 0 && (
              <p className="text-xs text-sand-500">
                {petLimit === -1 ? "" : `${petCount} / ${petLimit} mascotas`}
                {atPetLimit && !isPlus && (
                  <button
                    type="button"
                    onClick={() => setShowFreemium(true)}
                    className="ml-1.5 font-semibold text-brand-600 underline"
                  >
                    Agrega hasta 3 con Plus →
                  </button>
                )}
              </p>
            )}
            {atPetLimit ? (
              <button
                type="button"
                onClick={() => setShowFreemium(true)}
                className="inline-flex items-center gap-2 rounded-xl border border-brand-300 bg-brand-50 px-4 py-2.5 text-sm font-semibold text-brand-700 transition-base hover:bg-brand-100"
              >
                🔒 Actualizar plan
              </button>
            ) : (
              <Link
                to="/pets/new"
                className="inline-flex items-center gap-2 rounded-xl bg-brand-500 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-base hover:bg-brand-600 focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:outline-none"
              >
                <span aria-hidden="true">＋</span> Registrar mascota
              </Link>
            )}
          </div>
        </div>

        {/* Quick actions */}
        <div className="mb-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Link
            to="/encontre"
            className="flex items-center gap-3 rounded-xl border border-rescue-200 bg-rescue-50 px-4 py-3 text-sm font-semibold text-rescue-700 transition-base hover:bg-rescue-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400 focus-visible:ring-offset-1"
          >
            <span aria-hidden="true" className="text-lg">
              🐾
            </span>
            Encontré una mascota
          </Link>
          <Link
            to="/map/match"
            className="flex items-center gap-3 rounded-xl border border-trust-200 bg-trust-50 px-4 py-3 text-sm font-semibold text-trust-700 transition-base hover:bg-trust-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400 focus-visible:ring-offset-1"
          >
            <span aria-hidden="true" className="text-lg">
              🔍
            </span>
            Buscar por foto (IA)
          </Link>
          <Link
            to="/notifications"
            className="flex items-center gap-3 rounded-xl border border-brand-200 bg-brand-50 px-4 py-3 text-sm font-semibold text-brand-700 transition-base hover:bg-brand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
          >
            <span aria-hidden="true" className="text-lg">
              💬
            </span>
            Mensajes y alertas
          </Link>
          {user?.role === "Admin" && (
            <Link
              to="/estadisticas"
              className="flex items-center gap-3 rounded-xl border border-sand-200 bg-surface-warm px-4 py-3 text-sm font-semibold text-sand-700 transition-base hover:bg-sand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sand-400 focus-visible:ring-offset-1"
            >
              <span aria-hidden="true" className="text-lg">
                📊
              </span>
              Estadísticas CR
            </Link>
          )}
        </div>

        {/* Freemium upsell — only for non-admin users with at least 1 pet */}
        {!isLoading && user?.role !== "Admin" && (pets?.length ?? 0) >= 1 && (
          <button
            type="button"
            onClick={() => setShowFreemium(true)}
            className="mb-8 w-full rounded-2xl border border-brand-200 bg-linear-to-r from-brand-50 to-rescue-50 px-4 py-3 flex items-center gap-3 text-left transition-colors hover:from-brand-100 hover:to-rescue-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            <span className="text-2xl shrink-0" aria-hidden="true">
              ⚡
            </span>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-brand-900">
                Activa Plus y protege más a tus mascotas
              </p>
              <p className="text-xs text-brand-600 mt-0.5">
                Alertas instantáneas, IA sin límite y hasta 3 mascotas desde{" "}
                <strong>₡2,990/mes</strong>.
              </p>
            </div>
            <span className="shrink-0 rounded-xl bg-brand-600 px-3 py-1.5 text-xs font-bold text-white">
              Ver planes →
            </span>
          </button>
        )}

        {showFreemium && (
          <FreemiumModal onClose={() => setShowFreemium(false)} />
        )}

        {/* Loading skeleton */}
        {isLoading && (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-56 rounded-2xl" />
            ))}
          </div>
        )}

        {isError && (
          <Alert variant="error">
            No se pudieron cargar tus mascotas. Por favor, intenta de nuevo.
          </Alert>
        )}

        {!isLoading && !isError && pets?.length === 0 && (
          <EmptyState
            icon={
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                className="h-12 w-12"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M21 8.25c0-2.485-2.099-4.5-4.688-4.5-1.935 0-3.597 1.126-4.312 2.733-.715-1.607-2.377-2.733-4.313-2.733C5.1 3.75 3 5.765 3 8.25c0 7.22 9 12 9 12s9-4.78 9-12z"
                />
              </svg>
            }
            title="Aún no tienes mascotas"
            description="Registra tu primera mascota y genera su placa QR de identidad."
            action={
              <Link
                to="/pets/new"
                className="inline-flex items-center gap-2 rounded-xl bg-brand-500 px-5 py-3 text-sm font-semibold text-white shadow-sm transition-base hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                Registrar mi primera mascota
              </Link>
            }
          />
        )}

        {!isLoading && !isError && pets && pets.length > 0 && (
          <>
            {/* Search + filter chips */}
            <div className="mb-5 flex flex-col gap-3">
              <div className="relative">
                <span
                  className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-sand-400"
                  aria-hidden="true"
                >
                  🔍
                </span>
                <input
                  type="search"
                  placeholder="Buscar mascota…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="w-full rounded-xl border border-sand-200 py-2.5 pl-9 pr-4 text-sm field-input placeholder:text-sand-400 outline-none focus:ring-2 focus:ring-brand-400 focus:border-brand-400"
                />
              </div>
              <div
                className="flex gap-2 overflow-x-auto pb-1 [scrollbar-width:none] [-webkit-overflow-scrolling:touch]"
                role="group"
                aria-label="Filtros de mascotas"
              >
                {(["all", "Active", "Lost"] as const).map((s) => (
                  <button
                    key={s}
                    type="button"
                    onClick={() => setFilterStatus(s)}
                    aria-pressed={filterStatus === s}
                    className={[
                      "shrink-0 rounded-full px-3.5 py-1.5 text-xs font-semibold transition-all",
                      filterStatus === s
                        ? s === "Lost"
                          ? "bg-danger-500 text-white shadow-sm"
                          : "bg-brand-500 text-white shadow-sm"
                        : "bg-sand-100 text-sand-600 hover:bg-sand-200",
                    ].join(" ")}
                  >
                    {s === "all" ? (
                      "Todos"
                    ) : s === "Lost" ? (
                      <>
                        <span aria-hidden="true">🚨 </span>Perdidos
                      </>
                    ) : (
                      <>
                        <span aria-hidden="true">✅ </span>Activos
                      </>
                    )}
                  </button>
                ))}
                {species.map((sp) => (
                  <button
                    key={sp}
                    type="button"
                    onClick={() =>
                      setFilterSpecies(filterSpecies === sp ? "all" : sp)
                    }
                    aria-pressed={filterSpecies === sp}
                    className={[
                      "shrink-0 rounded-full px-3.5 py-1.5 text-xs font-semibold transition-all",
                      filterSpecies === sp
                        ? "bg-trust-500 text-white shadow-sm"
                        : "bg-sand-100 text-sand-600 hover:bg-sand-200",
                    ].join(" ")}
                  >
                    {sp === "Dog" ? (
                      <>
                        <span aria-hidden="true">🐶 </span>Perros
                      </>
                    ) : sp === "Cat" ? (
                      <>
                        <span aria-hidden="true">🐱 </span>Gatos
                      </>
                    ) : sp === "Bird" ? (
                      <>
                        <span aria-hidden="true">🐦 </span>Aves
                      </>
                    ) : sp === "Rabbit" ? (
                      <>
                        <span aria-hidden="true">🐰 </span>Conejos
                      </>
                    ) : (
                      <>
                        <span aria-hidden="true">🐾 </span>
                        {sp}
                      </>
                    )}
                  </button>
                ))}
              </div>
            </div>

            {filteredPets.length === 0 ? (
              <p className="py-8 text-center text-sm text-sand-400">
                No hay mascotas que coincidan con la búsqueda.
              </p>
            ) : (
              <div className="stagger-grid grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
                {filteredPets.map((pet, i) => (
                  <HolographicPetCard key={pet.id} pet={pet} index={i} />
                ))}
              </div>
            )}
          </>
        )}

        {/* Alert preferences */}
        {!isLoading && (
          <div className="mt-10">
            <h2 className="mb-3 text-xs font-semibold uppercase tracking-wider text-sand-400">
              Configuración de alertas
            </h2>
            <AlertPreferencesToggle />
          </div>
        )}

        {/* Billboard — Dashboard placement */}
        {!isLoading && (
          <BillboardBanner placement="Dashboard" className="mt-4" />
        )}

        {/* Leaderboard */}
        {!isLoading && (
          <div className="mt-10">
            <LeaderboardWidget />
          </div>
        )}
      </div>
    </>
  );
}
