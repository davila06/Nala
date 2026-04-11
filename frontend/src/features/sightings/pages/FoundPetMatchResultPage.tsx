import { useLocation, Link } from 'react-router-dom'
import { FoundPetMatchCard } from '../components/FoundPetMatchCard'
import { FosterSuggestions } from '../components/FosterSuggestions'
import type { ReportFoundPetResult } from '../api/foundPetsApi'

interface LocationState {
  result: ReportFoundPetResult
}

export default function FoundPetMatchResultPage() {
  const location = useLocation()
  const state = location.state as LocationState | null

  if (!state?.result) {
    return (
      <div className="mx-auto max-w-md px-4 py-16 text-center animate-fade-in-up">
        <p className="text-4xl" aria-hidden="true">🐾</p>
        <h1 className="mt-4 text-lg font-bold text-sand-900">Sin resultados disponibles</h1>
        <p className="mt-2 text-sm text-sand-500">
          Vuelve al inicio para hacer un nuevo reporte.
        </p>
        <Link
          to="/encontre-mascota"
          className="mt-6 inline-block rounded-xl bg-rescue-500 px-6 py-3 text-sm font-semibold text-white transition hover:bg-rescue-600"
        >
          Nuevo reporte
        </Link>
      </div>
    )
  }

  const { reportId, candidates } = state.result

  return (
    <div className="mx-auto max-w-md px-4 py-8 animate-fade-in-up">
      {/* Header */}
      <div className="mb-6 text-center">
        <p className="text-4xl">{candidates.length > 0 ? '🔍' : '✅'}</p>
        <h1 className="mt-3 text-xl font-bold text-sand-900">
          {candidates.length > 0 ? 'Posibles coincidencias' : 'Reporte enviado'}
        </h1>
        <p className="mt-1 text-sm text-sand-500">
          {candidates.length > 0
            ? 'Encontramos mascotas perdidas que podrían coincidir con la que encontraste.'
            : 'Tu reporte fue registrado. Notificaremos a los dueños si hay una coincidencia.'}
        </p>
      </div>

      {/* High-confidence auto-match banner */}
      {candidates.length > 0 && candidates[0].scorePercent >= 70 && (
        <div className="mb-4 rounded-xl border border-rescue-200 bg-rescue-50 p-4 text-center">
          <p className="text-sm font-medium text-rescue-700">
            🎉 Notificamos automáticamente al dueño de <strong>{candidates[0].petName}</strong>.
            Si hay match, se pondrán en contacto contigo.
          </p>
        </div>
      )}

      {/* Candidate list */}
      {candidates.length > 0 ? (
        <div className="space-y-3">
          {candidates.map((c) => (
            <FoundPetMatchCard key={c.lostPetEventId} candidate={c} />
          ))}
        </div>
      ) : (
        <div className="rounded-xl border border-sand-200 bg-sand-50 p-8 text-center">
          <p className="text-2xl" aria-hidden="true">🐾</p>
          <p className="mt-2 text-sm text-sand-600">
            No encontramos reportes activos cerca. Tu reporte está guardado y lo cruzaremos
            contra nuevos reportes automáticamente.
          </p>
        </div>
      )}

      {/* Report ID footer */}
      <p className="mt-6 text-center text-[10px] text-sand-400">
        ID de reporte: {reportId}
      </p>

      <FosterSuggestions foundReportId={reportId} />

      {/* CTA */}
      <div className="mt-6 flex flex-col gap-3">
        <Link
          to="/encontre-mascota"
          className="rounded-xl border border-sand-300 py-3 text-center text-sm font-medium text-sand-600 transition hover:bg-sand-50"
        >
          Hacer otro reporte
        </Link>
        <Link
          to="/"
          className="rounded-xl bg-rescue-500 py-3 text-center text-sm font-semibold text-white transition hover:bg-rescue-600"
        >
          Ir al inicio
        </Link>
      </div>
    </div>
  )
}

