import { useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useVisualMatch } from '../hooks/useVisualMatch'
import type { VisualMatchResult } from '../api/matchingApi'
import { Alert } from '@/shared/ui/Alert'

// ── Helpers ───────────────────────────────────────────────────────────────────

function similarityLabel(score: number): { text: string; color: string } {
  if (score >= 0.85) return { text: 'Alta similitud', color: 'text-rescue-600' }
  if (score >= 0.75) return { text: 'Posible coincidencia', color: 'text-brand-600' }
  return { text: 'Coincidencia leve', color: 'text-sand-500' }
}

function formatDistance(km: number): string {
  if (km < 1) return `${Math.round(km * 1000)} m`
  return `${km.toFixed(1)} km`
}

// ── Result card ───────────────────────────────────────────────────────────────

function MatchCard({ match }: { match: VisualMatchResult }) {
  const label = similarityLabel(match.similarityScore)
  const pct = Math.round(match.similarityScore * 100)

  return (
    <div className="flex gap-3 rounded-xl border border-sand-100 bg-white p-3 shadow-sm transition-shadow hover:shadow-md">
      {/* Pet photo */}
      <div className="relative h-20 w-20 shrink-0 overflow-hidden rounded-lg bg-sand-100">
        {match.photoUrl ? (
          <img
            src={match.photoUrl}
            alt={match.petName}
            className="h-full w-full object-cover"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-3xl">🐾</div>
        )}
        {/* Score badge */}
        <span className="absolute bottom-0 right-0 rounded-tl-lg bg-sand-900/80 px-1.5 py-0.5 text-[10px] font-bold text-white">
          {pct}%
        </span>
      </div>

      {/* Info */}
      <div className="flex min-w-0 flex-1 flex-col justify-between">
        <div>
          <p className="truncate font-semibold text-sand-900">{match.petName}</p>
          <p className="text-xs text-sand-500">{match.species}</p>
          <p className={`mt-0.5 text-xs font-medium ${label.color}`}>{label.text}</p>
        </div>
        <div className="flex items-center justify-between">
          {match.distanceKm != null && (
            <span className="text-xs text-sand-400">
              📍 {formatDistance(match.distanceKm)} del último avistamiento
            </span>
          )}
          <Link
            to={match.publicProfileUrl.replace('https://pawtrack.cr', '')}
            className="ml-auto rounded-lg bg-sand-900 px-3 py-1 text-xs font-semibold text-white transition-colors hover:bg-sand-700"
          >
            Ver perfil →
          </Link>
        </div>
      </div>
    </div>
  )
}

// ── Panel ─────────────────────────────────────────────────────────────────────

export function VisualMatchPanel() {
  const { mutateAsync: runMatch, isPending, data: results, reset } = useVisualMatch()

  const [photo, setPhoto] = useState<File | null>(null)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  const [lat, setLat] = useState<number | null>(null)
  const [lng, setLng] = useState<number | null>(null)
  const [geoStatus, setGeoStatus] = useState<'idle' | 'loading' | 'granted' | 'denied'>('idle')
  const [error, setError] = useState<string | null>(null)
  const [hasSearched, setHasSearched] = useState(false)
  const [visibleCount, setVisibleCount] = useState(10)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const dropZoneRef = useRef<HTMLDivElement>(null)

  // ── File handling ──────────────────────────────────────────────────────────

  const applyFile = (file: File) => {
    if (!file.type.startsWith('image/')) {
      setError('Selecciona una imagen (JPEG, PNG o WebP).')
      return
    }
    if (file.size > 5 * 1024 * 1024) {
      setError('La imagen debe pesar menos de 5 MB.')
      return
    }
    setPhoto(file)
    setPhotoPreview(URL.createObjectURL(file))
    setError(null)
    reset()
    setHasSearched(false)
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) applyFile(file)
  }

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    const file = e.dataTransfer.files?.[0]
    if (file) applyFile(file)
  }

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault()
  }

  // ── Geolocation ────────────────────────────────────────────────────────────

  const handleGetLocation = () => {
    if (!navigator.geolocation) {
      setGeoStatus('denied')
      return
    }
    setGeoStatus('loading')
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLat(pos.coords.latitude)
        setLng(pos.coords.longitude)
        setGeoStatus('granted')
      },
      () => setGeoStatus('denied'),
      { timeout: 10_000 },
    )
  }

  // ── Submit ─────────────────────────────────────────────────────────────────

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!photo) {
      setError('Por favor selecciona una foto primero.')
      return
    }
    setError(null)
    try {
      await runMatch({
        photo,
        lat: lat ?? undefined,
        lng: lng ?? undefined,
      })
      setHasSearched(true)
      setVisibleCount(10)
    } catch {
      setError('No se pudo completar la búsqueda. Intenta de nuevo.')
    }
  }

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="mx-auto max-w-lg px-4 py-8">
      {/* Title */}
      <div className="mb-6 text-center">
        <h1 className="text-2xl font-bold text-sand-900">¿Encontraste una mascota?</h1>
        <p className="mt-1 text-sm text-sand-500">
          Sube una foto y busca si está reportada como perdida.
        </p>
      </div>

      <form onSubmit={handleSubmit} noValidate>
        {/* Photo drop-zone */}
        <div
          ref={dropZoneRef}
          onDrop={handleDrop}
          onDragOver={handleDragOver}
          onClick={() => fileInputRef.current?.click()}
          className="relative mb-4 flex h-48 cursor-pointer flex-col items-center justify-center overflow-hidden rounded-2xl border-2 border-dashed border-sand-300 bg-sand-50 transition-colors hover:border-sand-400 hover:bg-sand-100"
        >
          {photoPreview ? (
            <img
              src={photoPreview}
              alt="Foto seleccionada"
              className="h-full w-full object-contain"
            />
          ) : (
            <>
              <span className="text-4xl" aria-hidden="true">📷</span>
              <p className="mt-2 text-sm font-medium text-sand-600">
                Arrastra o haz clic para seleccionar
              </p>
              <p className="text-xs text-sand-400">JPEG, PNG, WebP — máx. 5 MB</p>
            </>
          )}
          {photoPreview && (
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation()
                setPhoto(null)
                setPhotoPreview(null)
                reset()
                setHasSearched(false)
              }}
              className="absolute right-2 top-2 flex h-6 w-6 items-center justify-center rounded-full bg-sand-900/70 text-xs text-white hover:bg-sand-900"
              aria-label="Quitar foto"
            >
              <span aria-hidden="true">✕</span>
            </button>
          )}
        </div>
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          className="hidden"
          onChange={handleFileChange}
        />

        {/* Location */}
        <button
          type="button"
          onClick={handleGetLocation}
          disabled={geoStatus === 'loading'}
          className="mb-4 flex w-full items-center justify-center gap-2 rounded-xl border border-sand-200 bg-white py-2.5 text-sm font-medium text-sand-700 shadow-sm transition-colors hover:bg-sand-50 disabled:opacity-60"
        >
          {geoStatus === 'loading' && (
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-sand-400 border-t-transparent" />
          )}
          {geoStatus === 'granted' ? '📍 Ubicación compartida' : '📍 Usar mi ubicación (opcional)'}
        </button>
        {geoStatus === 'denied' && (
          <p className="mb-3 text-center text-xs text-brand-600">
            Sin ubicación, igual podemos buscar pero el ranking será menos preciso.
          </p>
        )}

        {/* Error */}
        {error && (
          <Alert variant="error">{error}</Alert>
        )}

        {/* Submit */}
        <button
          type="submit"
          disabled={isPending || !photo}
          className="flex w-full items-center justify-center gap-2 rounded-xl bg-sand-900 py-3 text-sm font-bold text-white shadow-md transition-colors hover:bg-sand-700 disabled:opacity-50"
        >
          {isPending ? (
            <>
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
              Analizando…
            </>
          ) : (
            '🔍 Buscar mascota perdida'
          )}
        </button>
      </form>

      {/* Results */}
      {hasSearched && results && (
        <div className="mt-8">
          {results.length === 0 ? (
            <div className="rounded-2xl border border-sand-100 bg-sand-50 p-6 text-center">
              <p className="text-3xl">🐾</p>
              <p className="mt-2 font-semibold text-sand-700">Sin coincidencias por ahora</p>
              <p className="mt-1 text-sm text-sand-500">
                No encontramos mascotas perdidas que se parezcan a la foto. Si reconoces al animal,
                puedes reportar el avistamiento desde el perfil de la mascota.
              </p>
            </div>
          ) : (
            <>
              <p className="mb-3 text-sm font-semibold text-sand-700">
                {Math.min(visibleCount, results.length)} de {results.length} posible
                {results.length > 1 ? 's' : ''} coincidencia
                {results.length > 1 ? 's' : ''}
              </p>
              <div className="flex flex-col gap-3">
                {results.slice(0, visibleCount).map((match) => (
                  <MatchCard key={match.petId} match={match} />
                ))}
              </div>
              {visibleCount < results.length && (
                <button
                  type="button"
                  onClick={() => setVisibleCount((prev) => Math.min(prev + 10, results.length))}
                  className="mt-4 w-full rounded-xl border border-sand-200 bg-white py-2.5 text-sm font-medium text-sand-700 shadow-sm transition-colors hover:bg-sand-50"
                >
                  Mostrar más ({results.length - visibleCount} restantes)
                </button>
              )}
            </>
          )}
        </div>
      )}
    </div>
  )
}

