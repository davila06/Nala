import { useState } from 'react'
import { useUpdateLostPetStatus } from '../hooks/useLostPets'

interface ReuniteButtonProps {
  lostEventId: string
  petId: string
  petName: string
  onSuccess?: () => void
}

export function ReuniteButton({ lostEventId, petId, petName, onSuccess }: ReuniteButtonProps) {
  const [confirming, setConfirming] = useState(false)
  const mutation = useUpdateLostPetStatus(lostEventId, petId)

  const handleReunite = async () => {
    await mutation.mutateAsync('Reunited')
    setConfirming(false)
    onSuccess?.()
  }

  if (!confirming) {
    return (
      <button
        type="button"
        onClick={() => setConfirming(true)}
        className="w-full rounded-2xl bg-rescue-500 py-3 text-sm font-bold text-white hover:bg-rescue-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
      >
        <span aria-hidden="true">🎉</span> Marcar como reunido
      </button>
    )
  }

  return (
    <div className="rounded-2xl border border-rescue-200 bg-rescue-50 p-4">
      <p className="mb-3 text-center text-sm font-semibold text-rescue-800">
        ¿Confirmas que {petName} fue encontrado?
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
          onClick={handleReunite}
          disabled={mutation.isPending}
          className="flex-1 rounded-xl bg-rescue-600 py-2.5 text-sm font-semibold text-white hover:bg-rescue-700 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
        >
          {mutation.isPending ? 'Guardando…' : <>Sí, fue reunido <span aria-hidden="true">🎉</span></>}
        </button>
      </div>
    </div>
  )
}

