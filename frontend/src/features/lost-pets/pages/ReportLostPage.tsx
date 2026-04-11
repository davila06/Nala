import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { usePetDetail } from '@/features/pets/hooks/usePets'
import { PhotoUpload } from '@/features/pets/components/PhotoUpload'
import { useReportLost } from '../hooks/useLostPets'
import { useGeolocation } from '../hooks/useGeolocation'
import { LastSeenMap } from '../components/LastSeenMap'
import {
  estimateSearchRadius,
  hoursElapsedSince,
  resolveSearchRadiusWithLocalStats,
} from '../utils/searchRadius'
import { useRecoveryRates } from '../hooks/useRecoveryStats'
import { addQueuedReport } from '@/shared/lib/offlineQueue'
import { Skeleton } from '@/shared/ui/Spinner'
import type { LastSeenCoords } from '../components/LastSeenMap'

/** datetime-local inputs need LOCAL time, not UTC */
const toLocalDatetime = (d: Date) =>
  new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16)

export default function ReportLostPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: pet, isLoading } = usePetDetail(id ?? '')
  const { mutateAsync: reportLost, isPending, error } = useReportLost()
  const geo = useGeolocation()

  const [description, setDescription] = useState('')
  const [publicMessage, setPublicMessage] = useState('')
  const [lastSeenAt, setLastSeenAt] = useState(() => toLocalDatetime(new Date()))
  const [coords, setCoords] = useState<LastSeenCoords | null>(null)
  const [recentPhoto, setRecentPhoto] = useState<File | null>(null)
  const [contactName, setContactName] = useState('')
  const [contactPhone, setContactPhone] = useState('')
  const [rewardAmount, setRewardAmount] = useState('')
  const [rewardNote, setRewardNote] = useState('')
  const [queuedOffline, setQueuedOffline] = useState(false)
  const [isQueuingOffline, setIsQueuingOffline] = useState(false)

  // Auto-request geolocation on mount and seed the pin with the first fix
  useEffect(() => {
    geo.request()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []) // Only on mount — geo.request is stable (useCallback)

  // Once geolocation resolves, auto-place the pin at the user's position
  // (only if the user hasn't already placed it manually)
  useEffect(() => {
    if (geo.status === 'granted' && geo.coords && !coords) {
      setCoords(geo.coords)
    }
  }, [geo.status, geo.coords, coords])

  if (isLoading) {
    return (
      <div className="mx-auto max-w-lg space-y-4 px-4 py-10">
        <Skeleton className="h-5 w-36 rounded" />
        <Skeleton className="h-6 w-48 rounded" />
        {/* Map placeholder */}
        <Skeleton className="h-64 rounded-2xl" />
        {/* datetime field */}
        <Skeleton className="h-10 rounded-xl" />
        {/* description textarea */}
        <Skeleton className="h-24 rounded-xl" />
        {/* contact fields */}
        <Skeleton className="h-10 rounded-xl" />
        <Skeleton className="h-10 rounded-xl" />
        {/* submit */}
        <Skeleton className="h-12 rounded-2xl" />
      </div>
    )
  }

  if (!pet) {
    return (
      <div className="mx-auto max-w-lg px-4 py-10 text-center">
        <p className="text-sand-500">Mascota no encontrada.</p>
        <Link to="/dashboard" className="mt-4 inline-block text-sm text-brand-600 hover:underline">
          ← Volver
        </Link>
      </div>
    )
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    // ── Offline path: persist to IndexedDB queue ──────────────────────────
    if (!navigator.onLine) {
      setIsQueuingOffline(true)
      try {
        await addQueuedReport({
          id: crypto.randomUUID(),
          petId: pet.id,
          petName: pet.name,
          capturedAt: new Date().toISOString(),
          lastSeenAt: new Date(lastSeenAt).toISOString(),
          lastSeenLat: coords?.lat ?? null,
          lastSeenLng: coords?.lng ?? null,
          description: description.trim() || null,
          publicMessage: publicMessage.trim() || null,
          contactName: contactName.trim() || null,
          contactPhone: contactPhone.trim() || null,
          photoBlob: recentPhoto,
        })
        setQueuedOffline(true)
      } finally {
        setIsQueuingOffline(false)
      }
      return
    }

    // ── Online path: submit immediately ────────────────────────────────────
    try {
      const result = await reportLost({
        petId: pet.id,
        description: description.trim() || null,
        publicMessage: publicMessage.trim() || null,
        lastSeenAt: new Date(lastSeenAt).toISOString(),
        lastSeenLat: coords?.lat ?? null,
        lastSeenLng: coords?.lng ?? null,
        recentPhoto,
        contactName: contactName.trim() || null,
        contactPhone: contactPhone.trim() || null,
        rewardAmount: rewardAmount !== '' ? parseFloat(rewardAmount) : null,
        rewardNote: rewardNote.trim() || null,
      })
      const recentPhotoUrl = recentPhoto ? URL.createObjectURL(recentPhoto) : null
      navigate(`/pets/${pet.id}/lost-confirmed`, {
        state: {
          lostEventId: result.id,
          lastSeenAt: new Date(lastSeenAt).toISOString(),
          description: description.trim() || null,
          recentPhotoUrl,
        },
      })
    } catch {
      // error state is handled by the mutation's `error` object shown in the UI
    }
  }

  // Recomputes on every render; estimateSearchRadius is O(n) and near-zero cost.
  const heuristicRadius = estimateSearchRadius(
    pet.species,
    pet.breed,
    hoursElapsedSince(new Date(lastSeenAt).toISOString()),
  )

  const { data: localRecoveryStats } = useRecoveryRates({
    species: pet.species,
    breed: pet.breed,
    canton: null,
  })

  const estimatedRadius = resolveSearchRadiusWithLocalStats(
    heuristicRadius,
    localRecoveryStats?.p90DistanceMeters,
  )

  return (
    <div className="mx-auto max-w-lg px-4 py-8">
      <Link
        to={`/pets/${pet.id}`}
        className="mb-5 flex items-center gap-1.5 rounded-lg text-sm text-sand-500 hover:text-sand-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
      >
        ← Volver a {pet.name}
      </Link>

      {/* ── Offline queued confirmation ───────────────────────────────── */}
      {queuedOffline ? (
        <div className="rounded-2xl border border-brand-200 bg-brand-50 p-6 text-center">
          <div className="mb-3 text-5xl" aria-hidden="true">📵</div>
          <h2 className="text-lg font-bold text-brand-800">Sin conexión — reporte guardado</h2>
          <p className="mt-2 text-sm text-brand-700">
            El reporte de <strong>{pet.name}</strong> quedó guardado en tu dispositivo. Se
            enviará automáticamente cuando recuperes conexión a internet.
          </p>
          <p className="mt-1 text-xs text-brand-600">
            Puedes cerrar esta pantalla. La sincronización ocurre en segundo plano.
          </p>
          <Link
            to="/dashboard"
              className="mt-5 inline-block rounded-xl bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
          >
            ← Volver al inicio
          </Link>
        </div>
      ) : (
        <>
          <div className="mb-6 rounded-2xl border border-danger-200 bg-danger-50 p-5">
            <h1 className="text-lg font-bold text-danger-700">
              🚨 Reportar a {pet.name} como perdido
            </h1>
            <p className="mt-1 text-sm text-danger-600">
              Esto cambiará el estado de {pet.name} a &ldquo;Perdido&rdquo; y te enviará una alerta.
            </p>
          </div>

          {error && (
        <div role="alert" className="mb-4 rounded-xl bg-danger-50 px-4 py-3 text-sm text-danger-600">
          Ocurrió un error. Intenta de nuevo.
        </div>
      )}

      {/* ── Form progress indicator ─────────────────────────────────── */}
      {(() => {
        const steps = [
          { label: 'Cuándo',      done: true },
          { label: 'Dónde',       done: !!coords },
          { label: 'Foto',        done: !!recentPhoto },
          { label: 'Descripción', done: description.trim().length > 0 },
          { label: 'Contacto',    done: contactName.trim().length > 0 },
        ]
        const completed = steps.filter((s) => s.done).length
        const pct = Math.round((completed / steps.length) * 100)
        return (
          <div className="mb-5 rounded-2xl border border-sand-200 bg-white px-4 py-3">
            <div className="mb-2 flex items-center justify-between">
              <p className="text-xs font-semibold text-sand-600">Completado</p>
              <p className="text-xs font-bold text-brand-600">{pct}%</p>
            </div>
            <div className="relative h-2 overflow-hidden rounded-full bg-sand-100">
              <div
                className="absolute inset-y-0 left-0 rounded-full bg-brand-500 transition-all duration-500"
                style={{ width: `${pct}%` }}
                role="progressbar"
                aria-label="Progreso del reporte"
                aria-valuenow={pct}
                aria-valuemin={0}
                aria-valuemax={100}
              />
            </div>
            <div className="mt-2.5 flex gap-2">
              {steps.map((step) => (
                <div key={step.label} className="flex flex-1 flex-col items-center gap-0.5">
                  <div
                    className={`h-1.5 w-full rounded-full transition-colors duration-300 ${
                      step.done ? 'bg-brand-500' : 'bg-sand-200'
                    }`}
                  />
                  <span className={`text-[9px] font-medium ${step.done ? 'text-brand-600' : 'text-sand-400'}`}>
                    {step.label}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )
      })()}

      <form onSubmit={handleSubmit} className="space-y-5">
        {/* ── Last seen datetime ── */}
        <div>
          <label
            htmlFor="lastSeenAt"
            className="mb-1 block text-sm font-semibold text-sand-700"
          >
            ¿Cuándo fue visto por última vez?
          </label>
          <input
            id="lastSeenAt"
            type="datetime-local"
            value={lastSeenAt}
            onChange={(e) => setLastSeenAt(e.target.value)}
            required
            max={toLocalDatetime(new Date())}
            className="w-full rounded-xl border border-sand-300 px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
          />
        </div>

        {/* ── Interactive last-seen map ── */}
        <div>
          <div className="mb-2 flex items-center justify-between">
            <span className="text-sm font-semibold text-sand-700">
              📍 Última ubicación conocida
            </span>
            {coords ? (
              <button
                type="button"
                onClick={() => setCoords(null)}
                className="text-xs text-sand-400 underline hover:text-sand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 rounded"
              >
                Quitar pin
              </button>
            ) : (
              <span className="text-xs text-sand-400">Opcional</span>
            )}
          </div>

          {/* Geolocation status banner */}
          {geo.status === 'requesting' && (
            <div className="mb-2 flex items-center gap-2 rounded-lg bg-trust-50 px-3 py-2 text-xs text-trust-600">
              <span className="inline-block h-3 w-3 animate-spin rounded-full border-2 border-trust-400 border-t-transparent" />
              Obteniendo tu ubicación…
            </div>
          )}
          {geo.status === 'denied' && geo.error && (
            <div className="mb-2 rounded-lg bg-brand-50 px-3 py-2 text-xs text-brand-700">
              ⚠️ {geo.error}
            </div>
          )}
          {geo.status === 'unavailable' && (
            <div className="mb-2 rounded-lg bg-sand-50 px-3 py-2 text-xs text-sand-500">
              Geolocalización no disponible. Puedes hacer clic en el mapa para marcar la ubicación.
            </div>
          )}

          <LastSeenMap
            value={coords}
            onChange={setCoords}
            userCoords={geo.coords}
            geoStatus={geo.status}
            petName={pet.name}
            estimatedRadius={estimatedRadius}
            className="h-64 w-full overflow-hidden rounded-2xl border border-sand-200 shadow-sm"
          />

          {coords ? (
            <p className="mt-1.5 text-xs text-sand-400">
              Pin en {coords.lat.toFixed(5)}, {coords.lng.toFixed(5)} · Arrastra para ajustar
            </p>
          ) : (
            <p className="mt-1.5 text-xs text-sand-400">
              Toca el mapa para marcar dónde fue visto {pet.name} por última vez.
            </p>
          )}
        </div>

        {/* ── Recent photo ── */}
        <div>
          <p className="mb-1.5 text-sm font-semibold text-sand-700">
            📷 Foto reciente (opcional)
          </p>
          <p className="mb-2 text-xs text-sand-500">
            Adjunta una foto actual de {pet.name} para ayudar a reconocerlo. Se usará en el flyer de búsqueda.
          </p>
          <PhotoUpload
            value={recentPhoto}
            onChange={setRecentPhoto}
            disabled={isPending}
          />
        </div>

        {/* ── Description ── */}
        <div>
          <label
            htmlFor="description"
            className="mb-1 block text-sm font-semibold text-sand-700"
          >
            Descripción (opcional)
          </label>
          <textarea
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            maxLength={1000}
            rows={4}
            placeholder="Collar, señas particulares, zona específica…"
            className="w-full resize-none rounded-xl border border-sand-300 px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
          />
          <p className="mt-1 text-right text-xs text-sand-400">
            {description.length}/1000
          </p>
        </div>

        {/* ── Public message for QR profile ── */}
        <div className="rounded-2xl border border-brand-200 bg-brand-50 p-4">
          <label
            htmlFor="publicMessage"
            className="mb-0.5 block text-sm font-semibold text-brand-800"
          >
            💬 Mensaje para quien encuentre a {pet.name}
          </label>
          <p className="mb-2 text-xs text-brand-700">
            Se mostrará en el perfil público de {pet.name} cuando alguien escanee su QR.
          </p>
          <textarea
            id="publicMessage"
            value={publicMessage}
            onChange={(e) => setPublicMessage(e.target.value)}
            maxLength={200}
            rows={3}
            placeholder={`Si encontraste a ${pet.name}, por favor contáctame. ¡Muchas gracias!`}
            className="w-full resize-none rounded-xl border border-brand-300 bg-white px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
          />
          <p className="mt-1 text-right text-xs text-brand-600">
            {publicMessage.length}/200
          </p>
        </div>

        {/* ── Contact info ── */}
        <div className="rounded-2xl border border-sand-200 bg-sand-50 p-4">
          <p className="mb-0.5 text-sm font-semibold text-sand-700">
            📞 Contacto de emergencia (opcional)
          </p>
          <p className="mb-3 text-xs text-sand-500">
            Quien encuentre a {pet.name} podrá ver tu nombre. El número de teléfono solo se mostrará
            a usuarios registrados.
          </p>

          <div className="space-y-3">
            <div>
              <label htmlFor="contactName" className="mb-1 block text-xs font-medium text-sand-600">
                Nombre de contacto
              </label>
              <input
                id="contactName"
                type="text"
                value={contactName}
                onChange={(e) => setContactName(e.target.value)}
                maxLength={100}
                placeholder="Ej. María Pérez"
                className="w-full rounded-xl border border-sand-300 bg-white px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
              />
            </div>

            <div>
              <label htmlFor="contactPhone" className="mb-1 block text-xs font-medium text-sand-600">
                Número de teléfono
              </label>
              <input
                id="contactPhone"
                type="tel"
                value={contactPhone}
                onChange={(e) => setContactPhone(e.target.value)}
                maxLength={30}
                pattern="[\d\s()+\-.]{7,30}"
                placeholder="Ej. +506 8888-0000"
                className="w-full rounded-xl border border-sand-300 bg-white px-4 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
              />
            </div>
          </div>
        </div>

        {/* ── Reward (optional) ── */}
        <div className="rounded-2xl border border-warn-200 bg-warn-50 p-4">
          <p className="mb-0.5 text-sm font-semibold text-warn-800">
            🏅 Recompensa (opcional)
          </p>
          <p className="mb-3 text-xs text-warn-700">
            Puedes ofrecer una recompensa en colones (₡) para quien ayude a encontrar a{' '}
            {pet.name}. No es obligatorio ni se gestiona dentro de la plataforma.
          </p>

          <div className="space-y-3">
            <div>
              <label htmlFor="rewardAmount" className="mb-1 block text-xs font-medium text-warn-800">
                Monto de la recompensa (₡)
              </label>
              <input
                id="rewardAmount"
                type="number"
                min={1}
                max={10_000_000}
                step={1000}
                value={rewardAmount}
                onChange={(e) => setRewardAmount(e.target.value)}
                placeholder="Ej. 50000"
                className="w-full rounded-xl border border-warn-300 bg-white px-4 py-2.5 text-sm focus:border-warn-500 focus:outline-none focus:ring-2 focus:ring-warn-200"
              />
            </div>

            <div>
              <label htmlFor="rewardNote" className="mb-1 block text-xs font-medium text-warn-800">
                Nota sobre la recompensa (opcional)
              </label>
              <input
                id="rewardNote"
                type="text"
                maxLength={150}
                value={rewardNote}
                onChange={(e) => setRewardNote(e.target.value)}
                placeholder="Ej. Se coordinará directamente con la familia"
                className="w-full rounded-xl border border-warn-300 bg-white px-4 py-2.5 text-sm focus:border-warn-500 focus:outline-none focus:ring-2 focus:ring-warn-200"
              />
              <p className="mt-1 text-right text-xs text-sand-400">{rewardNote.length}/150</p>
            </div>
          </div>
        </div>

        <button
          type="submit"
          disabled={isPending || isQueuingOffline}
          className="w-full rounded-2xl bg-danger-600 py-3.5 text-sm font-bold text-white hover:bg-danger-700 disabled:opacity-50"
        >
          {isPending ? 'Enviando reporte…' : isQueuingOffline ? 'Guardando…' : '\uD83D\uDEA8 Reportar como perdido'}
        </button>
      </form>
    </>
  )}
    </div>
  )
}


