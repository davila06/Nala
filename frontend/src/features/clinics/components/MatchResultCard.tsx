import type { ClinicScanResultDto } from '../api/clinicsApi'

interface MatchResultCardProps {
  result: ClinicScanResultDto
  onReset: () => void
}

export function MatchResultCard({ result, onReset }: MatchResultCardProps) {
  if (!result.matched) {
    return (
      <div className="rounded-2xl bg-sand-50 p-6 text-center">
        <p className="text-4xl">🔍</p>
        <p className="mt-3 text-base font-semibold text-sand-800">
          Mascota no encontrada
        </p>
        <p className="mt-1 text-sm text-sand-500">
          No hay ninguna mascota registrada con ese QR o microchip en PawTrack.
        </p>
        <button
          type="button"
          onClick={onReset}
          className="mt-5 rounded-xl bg-sand-200 px-5 py-2 text-sm font-semibold text-sand-700 hover:bg-sand-300"
        >
          Intentar de nuevo
        </button>
      </div>
    )
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-rescue-200 bg-rescue-50">
      {/* Header */}
      <div className="border-b border-rescue-200 bg-rescue-600 px-4 py-3">
        <p className="text-sm font-bold text-white">✅ ¡Mascota identificada!</p>
      </div>

      <div className="p-5">
        {/* Pet info */}
        <div className="flex items-center gap-4">
          {result.petPhotoUrl ? (
            <img
              src={result.petPhotoUrl}
              alt={result.petName ?? 'Mascota'}
              className="h-20 w-20 rounded-xl object-cover"
            />
          ) : (
            <div className="flex h-20 w-20 items-center justify-center rounded-xl bg-sand-200 text-3xl">
              🐾
            </div>
          )}
          <div>
            <p className="text-xl font-extrabold text-sand-900">{result.petName}</p>
            {result.petSpecies && (
              <p className="text-sm text-sand-500 capitalize">
                {{ Dog: 'Perro', Cat: 'Gato', Bird: 'Ave', Rabbit: 'Conejo', Other: 'Otra' }[result.petSpecies] ?? result.petSpecies}
              </p>
            )}
          </div>
        </div>

        {/* Owner info */}
        <div className="mt-4 rounded-xl bg-white p-4 shadow-sm">
          <p className="text-xs font-semibold uppercase tracking-wide text-sand-400">
            Datos del dueño
          </p>
          <p className="mt-1 text-base font-bold text-sand-900">{result.ownerName}</p>
          <a
            href={`mailto:${result.ownerEmail}`}
            className="mt-0.5 block text-sm text-rescue-600 underline"
          >
            {result.ownerEmail}
          </a>
        </div>

        <p className="mt-3 text-xs text-sand-500">
          Se ha notificado al dueño que su mascota fue vista aquí.
        </p>
      </div>

      <div className="border-t border-rescue-200 px-5 pb-5 pt-3">
        <button
          type="button"
          onClick={onReset}
          className="w-full rounded-xl bg-rescue-600 py-2.5 text-sm font-bold text-white hover:bg-rescue-700"
        >
          Escanear otra mascota
        </button>
      </div>
    </div>
  )
}

