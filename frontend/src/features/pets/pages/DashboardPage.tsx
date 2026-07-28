import { useCallback, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { HolographicPetCard } from "../components/HolographicPetCard";
import { OnboardingWizard, shouldShowOnboarding } from "../components/OnboardingWizard";
import { usePets } from "../hooks/usePets";
import { AlertPreferencesToggle } from "@/features/locations/components/AlertPreferencesToggle";
import { LeaderboardWidget } from "@/features/incentives/components/LeaderboardWidget";
import { Alert } from "@/shared/ui/Alert";
import { Skeleton } from "@/shared/ui/Spinner";
import { EmptyState } from "@/shared/ui/Card";
import { usePullToRefresh } from "@/shared/hooks/usePullToRefresh";

export default function DashboardPage() {
  const { data: pets, isLoading, isError, refetch } = usePets();
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState<'all' | 'Lost' | 'Active'>('all');
  const [filterSpecies, setFilterSpecies] = useState<string>('all');
  const [onboardingDismissed, setOnboardingDismissed] = useState(false);

  const filteredPets = useMemo(() => {
    if (!pets) return [];
    return pets.filter((p) => {
      const matchesSearch = search === '' || p.name.toLowerCase().includes(search.toLowerCase());
      const matchesStatus = filterStatus === 'all' || p.status === filterStatus;
      const matchesSpecies = filterSpecies === 'all' || p.species === filterSpecies;
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
      {!isLoading && !isError && pets?.length === 0 && !onboardingDismissed && shouldShowOnboarding() && (
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
          <h1 className="font-display text-2xl font-semibold text-sand-900">
            Mis mascotas
          </h1>
          <p className="mt-0.5 text-sm text-sand-500">
            {pets?.length ?? 0} mascota{pets?.length !== 1 ? "s" : ""}{" "}
            registrada{pets?.length !== 1 ? "s" : ""}
          </p>
        </div>
        <Link
          to="/pets/new"
          className="inline-flex items-center gap-2 rounded-xl bg-brand-500 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-base hover:bg-brand-600 focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:outline-none"
        >
          <span aria-hidden="true">＋</span> Registrar mascota
        </Link>
      </div>

      {/* Quick actions */}
      <div className="mb-8 grid gap-3 sm:grid-cols-3">
        <Link
          to="/encontre-mascota"
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
      </div>

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
              <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-sand-400" aria-hidden="true">
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
            <div className="flex flex-wrap gap-2">
              {(['all', 'Active', 'Lost'] as const).map((s) => (
                <button
                  key={s}
                  type="button"
                  onClick={() => setFilterStatus(s)}
                  className={[
                    'rounded-full px-3.5 py-1.5 text-xs font-semibold transition-all',
                    filterStatus === s
                      ? s === 'Lost'
                        ? 'bg-danger-500 text-white shadow-sm'
                        : 'bg-brand-500 text-white shadow-sm'
                      : 'bg-sand-100 text-sand-600 hover:bg-sand-200',
                  ].join(' ')}
                >
                  {s === 'all' ? 'Todos' : s === 'Lost' ? '🚨 Perdidos' : '✅ Activos'}
                </button>
              ))}
              {species.map((sp) => (
                <button
                  key={sp}
                  type="button"
                  onClick={() => setFilterSpecies(filterSpecies === sp ? 'all' : sp)}
                  className={[
                    'rounded-full px-3.5 py-1.5 text-xs font-semibold transition-all',
                    filterSpecies === sp
                      ? 'bg-trust-500 text-white shadow-sm'
                      : 'bg-sand-100 text-sand-600 hover:bg-sand-200',
                  ].join(' ')}
                >
                  {sp === 'Dog' ? '🐶 Perros' : sp === 'Cat' ? '🐱 Gatos' : sp === 'Bird' ? '🐦 Aves' : sp === 'Rabbit' ? '🐰 Conejos' : `🐾 ${sp}`}
                </button>
              ))}
            </div>
          </div>

          {filteredPets.length === 0 ? (
            <p className="py-8 text-center text-sm text-sand-400">No hay mascotas que coincidan con la búsqueda.</p>
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
