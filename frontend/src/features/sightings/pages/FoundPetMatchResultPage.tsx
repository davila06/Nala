import { useLocation, Link } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { FoundPetMatchCard } from '../components/FoundPetMatchCard'
import { FosterSuggestions } from '../components/FosterSuggestions'
import type { ReportFoundPetResult } from '../api/foundPetsApi'

// ── Confidence gauge ring ─────────────────────────────────────────────────────

function ConfidenceRing({ percent }: { percent: number }) {
  const r = 28
  const c = 2 * Math.PI * r
  const fill = (percent / 100) * c
  const color = percent >= 70 ? '#17a26d' : percent >= 45 ? '#f0b800' : '#d42020'

  return (
    <div className="relative flex h-16 w-16 items-center justify-center flex-shrink-0">
      <svg viewBox="0 0 72 72" className="absolute inset-0 -rotate-90 w-full h-full">
        <circle cx="36" cy="36" r={r} fill="none" stroke="#e2d3c4" strokeWidth="6" />
        <motion.circle
          cx="36" cy="36" r={r}
          fill="none"
          stroke={color}
          strokeWidth="6"
          strokeLinecap="round"
          strokeDasharray={c}
          initial={{ strokeDashoffset: c }}
          animate={{ strokeDashoffset: c - fill }}
          transition={{ duration: 0.8, ease: 'easeOut', delay: 0.2 }}
        />
      </svg>
      <span className="text-xs font-bold" style={{ color }}>{percent}%</span>
    </div>
  )
}

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
        <p className="mt-2 text-sm text-sand-500">Vuelve al inicio para hacer un nuevo reporte.</p>
        <Link to="/encontre-mascota" className="mt-6 inline-block rounded-xl bg-rescue-500 px-6 py-3 text-sm font-semibold text-white transition hover:bg-rescue-600">
          Nuevo reporte
        </Link>
      </div>
    )
  }

  const { reportId, candidates } = state.result
  const topCandidate = candidates[0]

  return (
    <div className="mx-auto max-w-md px-4 py-8 animate-fade-in-up">
      {/* Header */}
      <div className="mb-6 text-center">
        <motion.p
          className="text-5xl"
          initial={{ scale: 0 }}
          animate={{ scale: 1 }}
          transition={{ type: 'spring', stiffness: 400, damping: 20 }}
        >
          {candidates.length > 0 ? '🔍' : '✅'}
        </motion.p>
        <h1 className="mt-3 text-xl font-bold text-sand-900">
          {candidates.length > 0 ? 'Posibles coincidencias' : 'Reporte enviado'}
        </h1>
        <p className="mt-1 text-sm text-sand-500">
          {candidates.length > 0
            ? 'Encontramos mascotas perdidas que podrían coincidir con la que encontraste.'
            : 'Tu reporte fue registrado. Notificaremos a los dueños si hay una coincidencia.'}
        </p>
      </div>

      {/* High-confidence auto-match banner with confidence ring */}
      {topCandidate && topCandidate.scorePercent >= 70 && (
        <motion.div
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-4 flex items-center gap-4 rounded-2xl border border-rescue-200 bg-rescue-50 p-4"
        >
          <ConfidenceRing percent={topCandidate.scorePercent} />
          <p className="text-sm font-medium text-rescue-700">
            🎉 Notificamos automáticamente al dueño de <strong>{topCandidate.petName}</strong>.
            Si hay match, se pondrán en contacto contigo.
          </p>
        </motion.div>
      )}

      {/* Candidate list with stagger */}
      <AnimatePresence>
        {candidates.length > 0 ? (
          <div className="space-y-3">
            {candidates.map((c, i) => (
              <motion.div
                key={c.lostPetEventId}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.07, duration: 0.2 }}
              >
                <FoundPetMatchCard candidate={c} />
              </motion.div>
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
      </AnimatePresence>

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

