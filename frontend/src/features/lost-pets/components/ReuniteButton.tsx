import { useState, useCallback } from 'react'
import confetti from 'canvas-confetti'
import { useUpdateLostPetStatus } from '../hooks/useLostPets'

interface ReuniteButtonProps {
  lostEventId: string
  petId: string
  petName: string
  onSuccess?: () => void
}

/** Fire a multi-burst celebration confetti in PawTrack brand colors */
function fireCelebration() {
  const brandColors = ['#e8521e', '#17a26d', '#3056c2', '#ffd44d', '#ffffff']

  // First burst — center
  confetti({
    particleCount: 120,
    spread: 80,
    origin: { x: 0.5, y: 0.55 },
    colors: brandColors,
    ticks: 300,
  })

  // Delayed side bursts
  setTimeout(() => {
    confetti({ particleCount: 60, angle: 60,  spread: 55, origin: { x: 0, y: 0.65 }, colors: brandColors })
    confetti({ particleCount: 60, angle: 120, spread: 55, origin: { x: 1, y: 0.65 }, colors: brandColors })
  }, 150)

  // Emoji shapes — paw prints falling
  setTimeout(() => {
    confetti({
      particleCount: 30,
      spread: 120,
      origin: { x: 0.5, y: 0.3 },
      shapes: ['circle'],
      colors: ['#e8521e', '#f98050'],
      scalar: 1.8,
      ticks: 400,
    })
  }, 300)
}

export function ReuniteButton({ lostEventId, petId, petName, onSuccess }: ReuniteButtonProps) {
  const [confirming, setConfirming] = useState(false)
  const mutation = useUpdateLostPetStatus(lostEventId, petId)

  const handleReunite = useCallback(async () => {
    await mutation.mutateAsync('Reunited')
    setConfirming(false)
    fireCelebration()
    setTimeout(() => onSuccess?.(), 800)
  }, [mutation, onSuccess])

  if (!confirming) {
    return (
      <button
        type="button"
        onClick={() => setConfirming(true)}
        className="group relative w-full overflow-hidden rounded-2xl bg-rescue-500 py-3.5 text-sm font-bold text-white shadow-md shadow-rescue-200 transition-all hover:-translate-y-0.5 hover:bg-rescue-600 hover:shadow-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
      >
        <span className="pointer-events-none absolute inset-0 translate-x-[-120%] skew-x-[-20deg] bg-white/20 group-hover:translate-x-[220%] transition-transform duration-700" aria-hidden="true" />
        <span aria-hidden="true">🎉</span> ¡{petName} fue encontrado!
      </button>
    )
  }

  return (
    <div className="rounded-2xl border-2 border-rescue-300 bg-gradient-to-br from-rescue-50 to-white p-5 shadow-inner">
      <div className="mb-3 flex justify-center text-4xl" aria-hidden="true">🐾</div>
      <p className="mb-1 text-center font-display text-lg font-semibold text-rescue-900">
        ¿Fue encontrado?
      </p>
      <p className="mb-4 text-center text-sm text-rescue-700">
        Confirma que <strong>{petName}</strong> fue reunificado con su familia.
      </p>
      <div className="flex gap-2">
        <button
          type="button"
          onClick={() => setConfirming(false)}
          disabled={mutation.isPending}
          className="flex-1 rounded-xl border border-sand-300 py-2.5 text-sm font-semibold text-sand-700 hover:bg-sand-50 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sand-400"
        >
          Cancelar
        </button>
        <button
          type="button"
          onClick={() => void handleReunite()}
          disabled={mutation.isPending}
          className="flex-1 rounded-xl bg-rescue-600 py-2.5 text-sm font-semibold text-white hover:bg-rescue-700 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
        >
          {mutation.isPending ? 'Guardando…' : 'Sí, fue reunido 🎉'}
        </button>
      </div>
    </div>
  )
}

