import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useRecoveryOverview, useRecoveryRates } from '../hooks/useRecoveryStats'
import { CantonChoroplethMap } from '../components/CantonChoroplethMap'
import type { PetSpecies } from '@/features/pets/api/petsApi'
import { BREEDS_BY_SPECIES } from '@/features/pets/data/breeds'

const speciesOptions: Array<{ label: string; value: PetSpecies | '' }> = [
  { label: 'Todas', value: '' },
  { label: 'Perros', value: 'Dog' },
  { label: 'Gatos', value: 'Cat' },
  { label: 'Aves', value: 'Bird' },
  { label: 'Conejos', value: 'Rabbit' },
  { label: 'Otras', value: 'Other' },
]

const cantonPresets = ['Montes de Oca', 'San José', 'Escazú', 'Cartago', 'Heredia']

function toPercent(value: number): string {
  return `${(value * 100).toFixed(0)}%`
}

function formatHours(value: number | null): string {
  if (value == null) return '—'
  return `${value.toFixed(1)} h`
}

function formatMeters(value: number | null): string {
  if (value == null) return '—'
  if (value >= 1000) return `${(value / 1000).toFixed(1)} km`
  return `${Math.round(value)} m`
}

export default function RecoveryStatsPage() {
  const [species, setSpecies] = useState<PetSpecies | ''>('')
  const [breed, setBreed] = useState('')
  const [canton, setCanton] = useState('')

  const filters = useMemo(() => ({
    species: species || null,
    breed: breed.trim() || null,
    canton: canton.trim() || null,
  }), [species, breed, canton])

  const { data, isLoading, isFetching } = useRecoveryRates(filters)
  const { data: overview } = useRecoveryOverview()

  const maxRecoveryRate = useMemo(() => {
    const values = overview?.cantonRecovery.map((x) => x.recoveryRate) ?? []
    if (values.length === 0) return 1
    return Math.max(...values)
  }, [overview])

  const maxMedianHours = useMemo(() => {
    const values = overview?.speciesRecovery
      .map((x) => x.medianRecoveryHours ?? 0)
      .filter((x) => x > 0) ?? []
    if (values.length === 0) return 1
    return Math.max(...values)
  }, [overview])

  return (
    <main className="mx-auto max-w-[980px] px-4 pb-12 pt-8 animate-fade-in-up">
      {/* Header */}
      <header className="mb-6">
        <Link
          to="/map"
          className="inline-flex items-center gap-1 text-sm text-trust-600 hover:text-trust-800 transition-base"
        >
          ← Volver al mapa público
        </Link>
        <h1 className="mt-2 font-display text-3xl font-semibold leading-tight text-sand-900">
          Estadísticas de Recuperación en Costa Rica
        </h1>
        <p className="mt-1 text-sm text-sand-500">
          Datos anonimizados para mejorar la búsqueda de mascotas perdidas por cantón, especie y raza.
        </p>
      </header>

      {/* Filters */}
      <section className="mb-5 grid grid-cols-1 gap-3 rounded-2xl border border-sand-200 bg-gradient-to-br from-sand-50 to-trust-50 p-4 sm:grid-cols-3">
        <label htmlFor="filter-species" className="flex flex-col gap-1.5 text-sm font-medium text-sand-700">
          Especie
          <select
            id="filter-species"
            value={species}
            onChange={(e) => { setSpecies(e.target.value as PetSpecies | ''); setBreed('') }}
            className="rounded-xl border border-sand-300 bg-white px-3 py-2 text-sm focus:border-brand-400 focus:outline-none focus:ring-2 focus:ring-brand-100"
          >
            {speciesOptions.map((option) => (
              <option key={option.label} value={option.value}>{option.label}</option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1.5 text-sm font-medium text-sand-700">
          Cantón
          <input
            value={canton}
            onChange={(e) => setCanton(e.target.value)}
            placeholder="Ej: Montes de Oca"
            list="cantones-sugeridos"
            className="rounded-xl border border-sand-300 bg-white px-3 py-2 text-sm focus:border-brand-400 focus:outline-none focus:ring-2 focus:ring-brand-100"
          />
          <datalist id="cantones-sugeridos">
            {cantonPresets.map((item) => (
              <option key={item} value={item} />
            ))}
          </datalist>
        </label>

        <label htmlFor="filter-breed" className="flex flex-col gap-1.5 text-sm font-medium text-sand-700">
          Raza
          <select
            id="filter-breed"
            value={breed}
            onChange={(e) => setBreed(e.target.value)}
            disabled={!species}
            className={`rounded-xl border border-sand-300 bg-white px-3 py-2 text-sm focus:border-brand-400 focus:outline-none focus:ring-2 focus:ring-brand-100 ${!species ? 'text-sand-400' : 'text-sand-900'} disabled:cursor-not-allowed disabled:bg-sand-50`}
          >
            <option value="">{species ? 'Todas las razas' : 'Selecciona una especie primero'}</option>
            {species && BREEDS_BY_SPECIES[species].map((b) => (
              <option key={b} value={b}>{b}</option>
            ))}
          </select>
        </label>
      </section>

      {isLoading && (
        <p className="text-sm text-sand-500">Cargando estadísticas…</p>
      )}

      {!isLoading && data && (
        <>
          {/* Metric cards */}
          <section className="mb-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
            {[
              { label: 'Tasa de recuperación', value: toPercent(data.recoveryRate) },
              { label: 'Tiempo mediano',        value: formatHours(data.medianRecoveryHours) },
              { label: 'Distancia mediana',     value: formatMeters(data.medianDistanceMeters) },
              { label: 'P90 distancia',         value: formatMeters(data.p90DistanceMeters) },
            ].map(({ label, value }) => (
              <article key={label} className="rounded-2xl border border-sand-200 bg-white p-4">
                <p className="text-[0.75rem] text-sand-500">{label}</p>
                <p className="mt-1 text-2xl font-bold text-sand-900">{value}</p>
              </article>
            ))}
          </section>

          {/* Summary */}
          <section className="mb-5 rounded-2xl border border-sand-200 bg-white p-5">
            <h2 className="mb-3 text-base font-semibold text-sand-900">Lectura rápida</h2>
            <p className="text-sm text-sand-700">
              {data.totalReports > 0
                ? `De ${data.totalReports} reportes analizados, ${data.recoveredCount} terminaron reunificados. El percentil 90 de distancia es ${formatMeters(data.p90DistanceMeters)}, útil para ajustar el radio de búsqueda automático.`
                : 'Todavía no hay suficientes reportes para calcular métricas locales con confianza.'}
            </p>
            {isFetching && (
              <p className="mt-2 text-xs text-sand-400">Actualizando datos…</p>
            )}
          </section>

          {overview && (
            <>
              {/* Canton heatmap */}
              <section className="mb-5 rounded-2xl border border-sand-200 bg-white p-5">
                <h2 className="mb-1 text-base font-semibold text-sand-900">Mapa de calor por cantón</h2>
                <p className="mb-4 text-sm text-sand-500">
                  Intensidad proporcional a la tasa de recuperación local. Pase el cursor sobre un cantón para ver detalles.
                </p>
                <CantonChoroplethMap
                  cantonRecovery={overview.cantonRecovery}
                  maxRecoveryRate={maxRecoveryRate}
                />
              </section>

              {/* Species bar chart */}
              <section className="rounded-2xl border border-sand-200 bg-white p-5">
                <h2 className="mb-1 text-base font-semibold text-sand-900">Tiempo de recuperación por especie</h2>
                <p className="mb-4 text-sm text-sand-500">
                  Gráfico de barras sobre la mediana de horas para cada especie.
                </p>
                <div className="flex flex-col gap-3">
                  {overview.speciesRecovery.map((item) => {
                    const median = item.medianRecoveryHours ?? 0
                    const pct = `${(median / maxMedianHours) * 100}%`

                    return (
                      <div key={item.species}>
                        <div className="mb-1 flex items-center justify-between">
                          <span className="text-sm font-semibold text-sand-800">{item.species}</span>
                          <span className="text-sm text-sand-500">{formatHours(item.medianRecoveryHours)}</span>
                        </div>
                        <div className="h-2.5 overflow-hidden rounded-full bg-sand-200">
                          <div
                            className="h-full rounded-full bg-gradient-to-r from-trust-600 to-trust-400 transition-all duration-500"
                            style={{ width: pct, minWidth: median > 0 ? 8 : 0 }}
                          />
                        </div>
                      </div>
                    )
                  })}
                </div>
              </section>
            </>
          )}
        </>
      )}
    </main>
  )
}
