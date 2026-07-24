import { useRef, useCallback, useEffect } from 'react'
import { Link } from 'react-router-dom'
import type { PetSummary } from '../api/petsApi'
import { PetStatusBadge } from './PetStatusBadge'

const SPECIES_EMOJI: Record<string, string> = {
  Dog: '🐶',
  Cat: '🐱',
  Bird: '🐦',
  Rabbit: '🐰',
  Other: '🐾',
}

const SPECIES_LABEL: Record<string, string> = {
  Dog: 'Perro',
  Cat: 'Gato',
  Bird: 'Ave',
  Rabbit: 'Conejo',
  Other: 'Otra',
}

interface HolographicPetCardProps {
  pet: PetSummary
  index?: number
}

/** Apply 3D tilt + shadow to the card element */
function applyTilt(card: HTMLElement, rx: number, ry: number) {
  card.style.transform = `perspective(900px) rotateX(${rx}deg) rotateY(${ry}deg) scale(1.02)`
  card.style.boxShadow = `
    ${ry * -1.5}px ${rx * 1.5}px 30px rgb(232 82 30 / 0.15),
    0 20px 40px rgb(26 21 18 / 0.12)
  `
}

function resetTilt(card: HTMLElement) {
  card.style.transform = 'perspective(900px) rotateX(0deg) rotateY(0deg) scale(1)'
  card.style.boxShadow = ''
}

export function HolographicPetCard({ pet, index = 0 }: HolographicPetCardProps) {
  const cardRef = useRef<HTMLAnchorElement>(null)
  const gyroRef = useRef(false)

  // ── Gyroscope (mobile) ───────────────────────────────────────────────────
  useEffect(() => {
    const isMobile = 'ontouchstart' in window

    if (!isMobile || !window.DeviceOrientationEvent) return

    let permission: Promise<string> | null = null

    // iOS 13+ requires explicit permission
    if (typeof (DeviceOrientationEvent as unknown as { requestPermission?: () => Promise<string> }).requestPermission === 'function') {
      permission = (DeviceOrientationEvent as unknown as { requestPermission: () => Promise<string> }).requestPermission()
    }

    const attach = () => {
      const handler = (e: DeviceOrientationEvent) => {
        const card = cardRef.current
        if (!card) return
        gyroRef.current = true

        const beta  = e.beta  ?? 0  // front-back tilt  -180..180
        const gamma = e.gamma ?? 0  // left-right tilt  -90..90

        // Normalize to ±10deg card tilt
        const rx = Math.max(-10, Math.min(10, -(beta  - 45) * 0.25))
        const ry = Math.max(-10, Math.min(10,   gamma       * 0.25))
        applyTilt(card, rx, ry)
      }

      window.addEventListener('deviceorientation', handler, { passive: true })
      return () => window.removeEventListener('deviceorientation', handler)
    }

    let cleanup: (() => void) | undefined
    if (permission) {
      permission.then((state) => { if (state === 'granted') cleanup = attach() }).catch(() => {})
    } else {
      cleanup = attach()
    }

    return () => cleanup?.()
  }, [])

  // ── Mouse (desktop) ──────────────────────────────────────────────────────
  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLAnchorElement>) => {
    if (gyroRef.current) return   // gyro active → ignore mouse
    const card = cardRef.current
    if (!card) return
    const rect = card.getBoundingClientRect()
    const x = e.clientX - rect.left
    const y = e.clientY - rect.top
    const cx = rect.width / 2
    const cy = rect.height / 2
    applyTilt(card, ((y - cy) / cy) * -10, ((x - cx) / cx) * 10)
    card.style.setProperty('--holo-x', `${(x / rect.width) * 100}%`)
    card.style.setProperty('--holo-y', `${(y / rect.height) * 100}%`)
  }, [])

  const handleMouseLeave = useCallback(() => {
    if (gyroRef.current) return
    const card = cardRef.current
    if (card) resetTilt(card)
  }, [])

  const isLost = pet.status === 'Lost'

  return (
    <Link
      ref={cardRef}
      to={`/pets/${pet.id}`}
      aria-label={`Ver detalles de ${pet.name}`}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      style={{
        animationDelay: `${index * 60}ms`,
        transition: 'transform 0.12s ease, box-shadow 0.12s ease',
        transformStyle: 'preserve-3d',
        willChange: 'transform',
      }}
      className={[
        'holo-card group relative flex flex-col overflow-hidden rounded-2xl border bg-white',
        'focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:outline-none',
        'stagger-in',
        isLost
          ? 'border-danger-300 shadow-danger-100 shadow-lg'
          : 'border-sand-200 shadow-sm',
      ].join(' ')}
    >
      {/* Photo */}
      <div className="relative h-44 overflow-hidden bg-sand-100">
        {pet.photoUrl ? (
          <img
            src={pet.photoUrl}
            alt={pet.name}
            className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
            loading="lazy"
            style={{ transform: 'translateZ(20px)', transition: 'transform 0.12s ease' }}
          />
        ) : (
          <div className="flex h-full items-center justify-center text-5xl" style={{ transform: 'translateZ(20px)' }}>
            {SPECIES_EMOJI[pet.species] ?? '🐾'}
          </div>
        )}

        {isLost && (
          <>
            <div className="absolute inset-0 bg-danger-600/10" />
            <div className="absolute inset-x-0 top-0 flex items-center justify-center gap-1.5 bg-danger-600/90 py-1.5 text-xs font-bold uppercase tracking-widest text-white">
              <span className="inline-block h-1.5 w-1.5 rounded-full bg-white" style={{ animation: 'pulse-soft 0.8s ease infinite' }} />
              Perdida
            </div>
          </>
        )}

        <div className="pointer-events-none absolute inset-0" style={{ background: 'linear-gradient(135deg, rgba(255,255,255,0.15) 0%, transparent 50%)', transform: 'translateZ(30px)' }} />
      </div>

      {/* Info */}
      <div className="flex flex-1 flex-col gap-1 p-4" style={{ transform: 'translateZ(10px)' }}>
        <div className="flex items-start justify-between gap-2">
          <p className="flex-1 truncate font-semibold text-sand-900">{pet.name}</p>
          <PetStatusBadge status={pet.status} />
        </div>
        <p className="text-sm text-sand-500">
          {SPECIES_LABEL[pet.species] ?? pet.species}
          {pet.breed ? ` · ${pet.breed}` : ''}
        </p>
      </div>

      {isLost && (
        <div className="pointer-events-none absolute inset-x-0 bottom-0 h-1 bg-gradient-to-r from-danger-400 via-danger-500 to-danger-400" style={{ animation: 'pulse-soft 2s ease infinite' }} />
      )}
    </Link>
  )
}
