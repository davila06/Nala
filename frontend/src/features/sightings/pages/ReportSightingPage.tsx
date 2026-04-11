import { useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Alert } from '@/shared/ui/Alert'
import { useReportSighting } from '../hooks/useSightings'
import { useVisualMatchBySighting } from '../hooks/useVisualMatch'
import type { VisualMatchResult } from '../api/matchingApi'

// ── Auto-match panel shown on success when photo was uploaded ─────────────────

function SightingAutoMatchPanel({
  sightingId,
  lat,
  lng,
}: {
  sightingId: string
  lat: number | null
  lng: number | null
}) {
  const [visibleCount, setVisibleCount] = useState(10)
  const { data: matches, isLoading, isError } = useVisualMatchBySighting(
    sightingId,
    lat ?? undefined,
    lng ?? undefined,
  )

  if (isLoading) {
    return (
      <div className="mt-6 flex items-center justify-center gap-2 text-sm text-sand-500">
        <span className="h-4 w-4 animate-spin rounded-full border-2 border-sand-400 border-t-transparent" />
        Buscando mascotas perdidas similares…
      </div>
    )
  }

  if (isError || !matches || matches.length === 0) return null

  return (
    <div className="mt-6 text-left">
      <h2 className="mb-3 text-base font-bold text-sand-900">
        🔍 ¿Podría ser alguna de estas?
      </h2>
      <p className="mb-4 text-xs text-sand-500">
        Encontramos mascotas reportadas como perdidas que se parecen a la foto que subiste.
      </p>
      <div className="flex flex-col gap-3">
        {matches.slice(0, visibleCount).map((m: VisualMatchResult) => {
          const pct = Math.round(m.similarityScore * 100)
          return (
            <div
              key={m.petId}
              className="flex gap-3 rounded-xl border border-sand-100 bg-sand-50 p-3 shadow-sm"
            >
              <div className="relative h-16 w-16 shrink-0 overflow-hidden rounded-lg bg-sand-200">
                {m.photoUrl ? (
                  <img src={m.photoUrl} alt={m.petName} className="h-full w-full object-cover" />
                ) : (
                  <div className="flex h-full w-full items-center justify-center text-2xl" aria-hidden="true">🐾</div>
                )}
                <span className="absolute bottom-0 right-0 rounded-tl-md bg-sand-900/75 px-1 py-0.5 text-[9px] font-bold text-white">
                  {pct}%
                </span>
              </div>
              <div className="flex flex-1 flex-col justify-between">
                <p className="truncate text-sm font-semibold text-sand-900">{m.petName}</p>
                <p className="text-xs text-sand-400">{m.species}</p>
                {m.distanceKm != null && (
                  <p className="text-xs text-sand-400">
                    📍 {m.distanceKm < 1
                      ? `${Math.round(m.distanceKm * 1000)} m`
                      : `${m.distanceKm.toFixed(1)} km`}
                  </p>
                )}
                <Link
                  to={m.publicProfileUrl.replace('https://pawtrack.cr', '')}
                  className="mt-1 self-start rounded-lg bg-brand-500 px-3 py-1 text-[11px] font-bold text-white hover:bg-brand-600"
                >
                  Ver perfil →
                </Link>
              </div>
            </div>
          )
        })}
      </div>
      {visibleCount < matches.length && (
        <button
          type="button"
          onClick={() => setVisibleCount((prev) => Math.min(prev + 10, matches.length))}
          className="mt-3 w-full rounded-xl border border-sand-200 bg-white py-2 text-sm font-medium text-sand-600 hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          Mostrar más ({matches.length - visibleCount} restantes)
        </button>
      )}
    </div>
  )
}

export default function ReportSightingPage() {
  const { id: petId } = useParams<{ id: string }>()
  const { mutateAsync: reportSighting, isPending } = useReportSighting(petId ?? '')

  const [lat, setLat] = useState<number | null>(null)
  const [lng, setLng] = useState<number | null>(null)
  const [locationStatus, setLocationStatus] = useState<
    'idle' | 'loading' | 'granted' | 'denied'
  >('idle')
  const [note, setNote] = useState('')
  const [photo, setPhoto] = useState<File | null>(null)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [submittedSightingId, setSubmittedSightingId] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleGetLocation = () => {
    if (!navigator.geolocation) {
      setLocationStatus('denied')
      return
    }
    setLocationStatus('loading')
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLat(pos.coords.latitude)
        setLng(pos.coords.longitude)
        setLocationStatus('granted')
      },
      () => setLocationStatus('denied'),
      { timeout: 10_000 },
    )
  }

  const handlePhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null
    if (!file) return
    if (file.size > 5 * 1024 * 1024) {
      setError('La foto debe pesar menos de 5 MB.')
      return
    }
    setPhoto(file)
    setPhotoPreview(URL.createObjectURL(file))
    setError(null)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!petId) return
    if (lat === null || lng === null) {
      setError('Comparte tu ubicación antes de enviar el reporte.')
      return
    }
    setError(null)

    try {
      const result = await reportSighting({
        petId,
        lat,
        lng,
        note: note.trim() || null,
        sightedAt: new Date().toISOString(),
        photo,
      })
      setSubmittedSightingId(result.id)
    } catch {
      setError('Ocurrió un error inesperado. Intenta de nuevo.')
    }
  }

  if (submittedSightingId) {
    return (
      <div className="flex min-h-screen flex-col items-center bg-white px-6 py-12">
        <span className="text-6xl" aria-hidden="true">🐾</span>
        <h1 className="mt-4 text-2xl font-extrabold text-sand-900">¡Gracias por ayudar!</h1>
        <p className="mt-2 max-w-xs text-center text-sm text-sand-500">
          Tu avistamiento fue registrado. El dueño recibirá una notificación.
        </p>

        {/* Auto-match section — only shown when photo was included */}
        {photo !== null && (
          <div className="mt-2 w-full max-w-sm">
            <SightingAutoMatchPanel
              sightingId={submittedSightingId}
              lat={lat}
              lng={lng}
            />
          </div>
        )}

        <Link
          to={`/p/${petId}`}
          className="mt-8 rounded-xl bg-brand-500 px-6 py-3 text-sm font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
        >
          ← Ver perfil de mascota
        </Link>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-white">
      {/* Header */}
      <div className="border-b border-sand-100 px-5 py-4">
        <Link
          to={petId ? `/p/${petId}` : '/'}
          className="text-sm text-sand-500 hover:text-sand-800 rounded focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          ← Volver al perfil
        </Link>
        <h1 className="mt-1 text-xl font-extrabold text-sand-900">Reportar avistamiento</h1>
        <p className="mt-0.5 text-xs text-sand-400">
          Tus datos de contacto nunca se almacenan.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-5 px-5 py-6">
        {/* Location */}
        <div className="rounded-2xl border border-sand-200 p-4">
          <p className="mb-2 text-sm font-semibold text-sand-700">
            📍 Ubicación <span className="text-danger-500">*</span>
          </p>
          {locationStatus === 'granted' && lat !== null && lng !== null ? (
            <div className="rounded-xl bg-rescue-50 px-3 py-2 text-sm text-rescue-800">
              ✓ Ubicación capturada: {lat.toFixed(5)}, {lng.toFixed(5)}
            </div>
          ) : locationStatus === 'denied' ? (
            <p className="text-sm text-danger-600">
              El acceso a la ubicación fue denegado. Actívalo desde la configuración de tu navegador.
            </p>
          ) : (
            <button
              type="button"
              onClick={handleGetLocation}
              disabled={locationStatus === 'loading'}
              className="w-full rounded-xl bg-brand-500 py-3 text-sm font-bold text-white hover:bg-brand-600 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
            >
              {locationStatus === 'loading' ? 'Obteniendo ubicación…' : '📍 Usar mi ubicación'}
            </button>
          )}
        </div>

        {/* Photo */}
        <div className="rounded-2xl border border-sand-200 p-4">
          <p className="mb-2 text-sm font-semibold text-sand-700">📷 Foto (opcional)</p>
          {photoPreview ? (
            <div className="relative">
              <img
                src={photoPreview}
                alt="Vista previa de la foto"
                className="h-48 w-full rounded-xl object-cover"
              />
              <button
                type="button"
                aria-label="Quitar foto"
                onClick={() => { setPhoto(null); setPhotoPreview(null) }}
                className="absolute right-2 top-2 rounded-full bg-white/80 px-2 py-0.5 text-xs font-bold text-sand-600 hover:bg-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                <span aria-hidden="true">✕</span>
              </button>
            </div>
          ) : (
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              className="flex w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-sand-200 py-8 text-sm font-medium text-sand-400 hover:border-brand-400 hover:text-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              📸 Tomar / elegir foto
            </button>
          )}
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            capture="environment"
            className="hidden"
            onChange={handlePhotoChange}
          />
        </div>

        {/* Note */}
        <div>
          <label
            htmlFor="note"
            className="mb-1.5 block text-sm font-semibold text-sand-700"
          >
            Nota (opcional)
          </label>
          <textarea
            id="note"
            rows={3}
            maxLength={2000}
            value={note}
            onChange={(e) => setNote(e.target.value)}
            placeholder="Describe dónde viste a la mascota, qué estaba haciendo…"
            className="w-full rounded-xl border border-sand-200 bg-sand-50 px-3 py-2.5 text-sm text-sand-800 placeholder:text-sand-400 focus:border-brand-400 focus:outline-none focus:ring-2 focus:ring-brand-100"
          />
          <p className="mt-0.5 text-right text-xs text-sand-400">{note.length}/2000</p>
          <p className="text-xs text-sand-400">
            No incluyas tu teléfono, correo ni datos personales en la nota.
          </p>
        </div>

        {error && (
          <Alert variant="error">{error}</Alert>
        )}

        <button
          type="submit"
          disabled={isPending || lat === null}
          className="w-full rounded-xl bg-brand-500 py-4 text-base font-bold text-white hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isPending ? 'Enviando…' : <><span aria-hidden="true">🐾</span> Enviar avistamiento</>}
        </button>
      </form>
    </div>
  )
}

