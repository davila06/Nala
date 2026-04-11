import { Skeleton } from '@/shared/ui/Spinner'
import { useFosterSuggestions } from '../hooks/useFosters'

export function FosterSuggestions({ foundReportId }: { foundReportId: string }) {
  const { data, isLoading } = useFosterSuggestions(foundReportId)

  if (isLoading) {
    return (
      <div className="mt-6 space-y-2">
        <Skeleton className="h-14 rounded-2xl" />
        <Skeleton className="h-14 rounded-2xl" />
        <Skeleton className="h-14 rounded-2xl" />
      </div>
    )
  }

  if (!data || data.length === 0) {
    return (
      <div className="mt-6 rounded-2xl border border-sand-200 bg-sand-50 p-4 text-sm text-sand-500">
        No hay custodios disponibles en este momento.
      </div>
    )
  }

  return (
    <div className="mt-6 rounded-2xl border border-rescue-200 bg-rescue-50 p-4">
      <h3 className="text-sm font-bold text-rescue-900">Custodios temporales sugeridos</h3>
      <p className="mt-1 text-xs text-rescue-800">
        Personas voluntarias cercanas que pueden apoyar alojamiento temporal.
      </p>

      <ul className="mt-3 space-y-2">
        {data.map((item) => (
          <li key={item.userId} className="rounded-xl border border-rescue-100 bg-white p-3">
            <p className="text-sm font-semibold text-sand-800">{item.volunteerName}</p>
            <p className="text-xs text-sand-500">
              Distancia: {item.distanceLabel} · Cupo: {item.maxDays} días
            </p>
            <p className="text-xs text-sand-500">
              Tamaño: {item.sizePreference ?? 'Sin preferencia'} · Coincidencia especie:{' '}
              {item.speciesMatch ? 'Sí' : 'No'}
            </p>
          </li>
        ))}
      </ul>
    </div>
  )
}

