import { useEffect, useState, useRef, useCallback } from 'react'
import { petsApi } from '../api/petsApi'
import { useHaptic } from '@/shared/hooks/useHaptic'

interface QRFlipCardProps {
  petId: string
  petName: string
  petPhotoUrl?: string | null
  petSpecies?: string
}

const SPECIES_EMOJI: Record<string, string> = {
  Dog: '🐶', Cat: '🐱', Bird: '🐦', Rabbit: '🐰', Other: '🐾',
}

/**
 * 3D flip card:
 *   Front → pet photo / avatar placeholder
 *   Back  → QR code + download action
 * Tap, click, OR horizontal swipe to flip. Haptic on flip.
 */
export function QRFlipCard({ petId, petName, petPhotoUrl, petSpecies = 'Other' }: QRFlipCardProps) {
  const [isFlipped, setIsFlipped] = useState(false)
  const [blobUrl, setBlobUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(false)
  const blobRef = useRef<string | null>(null)
  const touchStartX = useRef<number | null>(null)
  const { tap } = useHaptic()

  const doFlip = useCallback(() => {
    tap()
    setIsFlipped((f) => !f)
  }, [tap])

  // Touch swipe detection
  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].clientX
  }, [])

  const handleTouchEnd = useCallback((e: React.TouchEvent) => {
    if (touchStartX.current === null) return
    const dx = e.changedTouches[0].clientX - touchStartX.current
    touchStartX.current = null
    // Swipe threshold: 40px horizontal
    if (Math.abs(dx) > 40) doFlip()
  }, [doFlip])

  // Lazy-load QR only when first flipped
  useEffect(() => {
    if (!isFlipped || blobUrl || loading) return

    setLoading(true)
    setError(false)
    petsApi
      .getQrCode(petId)
      .then((blob) => {
        const url = URL.createObjectURL(blob)
        blobRef.current = url
        setBlobUrl(url)
      })
      .catch(() => setError(true))
      .finally(() => setLoading(false))
  }, [isFlipped, petId, blobUrl, loading])

  useEffect(() => {
    return () => {
      if (blobRef.current) URL.revokeObjectURL(blobRef.current)
    }
  }, [])

  const handleDownload = () => {
    if (!blobUrl) return
    const a = document.createElement('a')
    a.href = blobUrl
    a.download = `qr-${petName.replace(/\s+/g, '-').toLowerCase()}.png`
    a.click()
  }

  return (
    <div className="w-full">
      {/* Flip hint */}
      <p className="mb-2 text-center text-xs text-sand-400">
        {isFlipped ? '← Toca para ver la foto' : 'Toca para ver el QR →'}
      </p>

      {/* Scene container — click, swipe, or keyboard to flip */}
      <div
        className="flip-scene mx-auto"
        style={{ width: 200, height: 200 }}
        onClick={doFlip}
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
        role="button"
        aria-label={isFlipped ? 'Mostrar foto de la mascota' : 'Mostrar código QR'}
        tabIndex={0}
        onKeyDown={(e) => e.key === 'Enter' && doFlip()}
      >
        <div
          className={`flip-card rounded-2xl ${isFlipped ? 'is-flipped' : ''}`}
          style={{ width: 200, height: 200 }}
        >
          {/* ── FRONT: Pet photo ─────────────────────────────────────────── */}
          <div className="flip-card__face flip-card__face--front rounded-2xl border border-sand-200 bg-sand-50 shadow-md cursor-pointer">
            {petPhotoUrl ? (
              <img
                src={petPhotoUrl}
                alt={petName}
                className="h-full w-full object-cover rounded-2xl"
                loading="lazy"
              />
            ) : (
              <div className="flex h-full w-full items-center justify-center text-7xl rounded-2xl bg-brand-50">
                {SPECIES_EMOJI[petSpecies] ?? '🐾'}
              </div>
            )}

            {/* Flip indicator badge */}
            <div className="absolute bottom-2 right-2 flex items-center gap-1 rounded-full bg-white/90 px-2 py-1 text-[10px] font-semibold text-sand-600 shadow backdrop-blur-sm">
              <svg viewBox="0 0 16 16" fill="none" className="h-3 w-3" aria-hidden="true">
                <path d="M2 8h12M10 5l3 3-3 3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
              QR
            </div>
          </div>

          {/* ── BACK: QR code ─────────────────────────────────────────────── */}
          <div className="flip-card__face flip-card__face--back rounded-2xl border border-sand-200 bg-white shadow-md cursor-pointer flex flex-col items-center justify-center gap-3 p-4">
            {loading && (
              <div className="flex flex-col items-center gap-2">
                <div className="h-5 w-5 rounded-full border-2 border-brand-300 border-t-brand-500 animate-spin" />
                <p className="text-xs text-sand-400">Generando QR…</p>
              </div>
            )}

            {error && (
              <p className="text-center text-xs text-danger-500">No se pudo cargar el QR.</p>
            )}

            {blobUrl && (
              <>
                <div className="relative">
                  {/* Metallic frame */}
                  <div className="absolute -inset-1.5 rounded-xl bg-gradient-to-br from-sand-300 via-sand-200 to-sand-400 shadow-inner" />
                  <img
                    src={blobUrl}
                    alt={`QR de ${petName}`}
                    className="relative h-28 w-28 rounded-lg"
                  />
                </div>
                <button
                  type="button"
                  onClick={(e) => { e.stopPropagation(); handleDownload() }}
                  className="flex items-center gap-1 rounded-lg bg-brand-500 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  ⬇ Descargar
                </button>
                <p className="text-center text-[10px] text-sand-400 leading-tight">
                  Imprime y adjunta al collar
                </p>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
