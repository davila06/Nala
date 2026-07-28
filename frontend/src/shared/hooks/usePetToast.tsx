import { toast } from 'sonner'

interface PetToastOptions {
  petName: string
  petPhotoUrl?: string | null
  species?: string
}

const SPECIES_EMOJI: Record<string, string> = {
  Dog: '🐶', Cat: '🐱', Bird: '🐦', Rabbit: '🐰', Other: '🐾',
}

/**
 * usePetToast — custom toast notifications that include the pet's photo or emoji.
 *
 * Usage:
 *   const { sightingAlert, lostAlert, foundAlert } = usePetToast()
 *   sightingAlert({ petName: 'Luna', petPhotoUrl: '...', zone: 'Escazú' })
 */
export function usePetToast() {
  const sightingAlert = ({
    petName,
    petPhotoUrl,
    species = 'Other',
    zone,
  }: PetToastOptions & { zone?: string }) => {
    toast.custom(() => (
      <div className="flex items-center gap-3 rounded-2xl border border-trust-200 field-input px-4 py-3 shadow-xl w-full">
        {/* Photo or emoji */}
        <div className="flex-shrink-0">
          {petPhotoUrl ? (
            <img
              src={petPhotoUrl}
              alt={petName}
              className="h-10 w-10 rounded-full object-cover border-2 border-trust-300"
            />
          ) : (
            <span className="text-2xl">{SPECIES_EMOJI[species] ?? '🐾'}</span>
          )}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-bold text-sand-900 truncate">
            🔵 Avistamiento — {petName}
          </p>
          {zone && (
            <p className="text-xs text-sand-500 truncate">📍 {zone}</p>
          )}
        </div>
      </div>
    ), { duration: 5000 })
  }

  const lostAlert = ({ petName, petPhotoUrl, species = 'Other' }: PetToastOptions) => {
    toast.custom(() => (
      <div className="flex items-center gap-3 rounded-2xl border border-danger-200 bg-danger-50 px-4 py-3 shadow-xl w-full">
        <div className="flex-shrink-0">
          {petPhotoUrl ? (
            <img
              src={petPhotoUrl}
              alt={petName}
              className="h-10 w-10 rounded-full object-cover border-2 border-danger-300"
            />
          ) : (
            <span className="text-2xl">{SPECIES_EMOJI[species] ?? '🐾'}</span>
          )}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-bold text-danger-900">
            🚨 Mascota perdida — {petName}
          </p>
          <p className="text-xs text-danger-600">
            Alerta enviada a usuarios cercanos
          </p>
        </div>
      </div>
    ), { duration: 6000 })
  }

  const foundAlert = ({ petName, petPhotoUrl, species = 'Other' }: PetToastOptions) => {
    toast.custom(() => (
      <div className="flex items-center gap-3 rounded-2xl border border-rescue-200 bg-rescue-50 px-4 py-3 shadow-xl w-full">
        <div className="flex-shrink-0">
          {petPhotoUrl ? (
            <img
              src={petPhotoUrl}
              alt={petName}
              className="h-10 w-10 rounded-full object-cover border-2 border-rescue-300"
            />
          ) : (
            <span className="text-2xl">{SPECIES_EMOJI[species] ?? '🐾'}</span>
          )}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-bold text-rescue-900">
            🎉 ¡{petName} fue encontrado!
          </p>
          <p className="text-xs text-rescue-600">Caso cerrado exitosamente</p>
        </div>
      </div>
    ), { duration: 6000 })
  }

  return { sightingAlert, lostAlert, foundAlert }
}
