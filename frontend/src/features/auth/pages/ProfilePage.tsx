import { useMemo, useState } from 'react'
import { useMyFosterProfile, useUpsertMyFosterProfile } from '@/features/sightings/hooks/useFosters'
import { useMyProfile, useUpdateProfile } from '../hooks/useProfile'
import { useAuthStore } from '../store/authStore'
import type { PetSpecies } from '@/features/sightings/api/fostersApi'
import { Button, Input, Badge, PageSpinner } from '@/shared/ui'
import { toast } from '@/shared/lib/toast'

const ALL_SPECIES: PetSpecies[] = ['Dog', 'Cat', 'Bird', 'Rabbit', 'Other']

export default function ProfilePage() {
  const { data: serverProfile, isLoading: profileLoading } = useMyProfile()
  const { mutateAsync: updateProfileName, isPending: updatingName } = useUpdateProfile()
  const user = useAuthStore((s) => s.user)

  const { data: fosterProfile, isLoading: fosterLoading } = useMyFosterProfile()
  const { mutateAsync: saveProfile, isPending: savingFoster } = useUpsertMyFosterProfile()

  // Identity section state
  const displayName = serverProfile?.name ?? user?.name ?? ''
  const [editingName, setEditingName] = useState(false)
  const [nameInput, setNameInput] = useState('')

  const handleEditName = () => {
    setNameInput(displayName)
    setEditingName(true)
  }

  const handleSaveName = async () => {
    if (!nameInput.trim()) return
    try {
      await updateProfileName({ name: nameInput.trim() })
      setEditingName(false)
      toast.success('Nombre actualizado correctamente.')
    } catch {
      toast.error('No se pudo actualizar el nombre. Intenta de nuevo.')
    }
  }

  // Foster section state
  const [isVolunteer, setIsVolunteer] = useState<boolean>(fosterProfile?.isAvailable ?? false)
  const [homeLat, setHomeLat] = useState<number>(fosterProfile?.homeLat ?? 0)
  const [homeLng, setHomeLng] = useState<number>(fosterProfile?.homeLng ?? 0)
  const [acceptedSpecies, setAcceptedSpecies] = useState<PetSpecies[]>(fosterProfile?.acceptedSpecies ?? ['Dog'])
  const [sizePreference, setSizePreference] = useState<string>(fosterProfile?.sizePreference ?? '')
  const [maxDays, setMaxDays] = useState<number>(fosterProfile?.maxDays ?? 3)

  const canSaveFoster = useMemo(
    () => isVolunteer && acceptedSpecies.length > 0 && maxDays > 0,
    [isVolunteer, acceptedSpecies, maxDays],
  )

  const requestLocation = () => {
    if (!navigator.geolocation) return
    navigator.geolocation.getCurrentPosition((pos) => {
      setHomeLat(pos.coords.latitude)
      setHomeLng(pos.coords.longitude)
    })
  }

  const toggleSpecies = (species: PetSpecies) => {
    setAcceptedSpecies((current) =>
      current.includes(species) ? current.filter((s) => s !== species) : [...current, species],
    )
  }

  const handleSaveFoster = async () => {
    try {
      await saveProfile({
        homeLat,
        homeLng,
        acceptedSpecies,
        sizePreference: sizePreference || null,
        maxDays,
        isAvailable: isVolunteer,
        availableUntil: null,
      })
      toast.success('Perfil de custodio actualizado correctamente.')
    } catch {
      toast.error('No se pudo guardar el perfil de custodio. Intenta de nuevo.')
    }
  }

  if (profileLoading || fosterLoading) {
    return <PageSpinner />
  }

  const initials = displayName
    .split(' ')
    .map((w) => w[0] ?? '')
    .join('')
    .toUpperCase()
    .slice(0, 2)

  return (
    <div className="mx-auto max-w-xl px-4 py-8 space-y-6 animate-fade-in-up">
      <h1 className="text-2xl font-bold text-sand-900">Mi perfil</h1>

      {/* ── Identity card ────────────────────────────────────────────── */}
      <div className="rounded-2xl border border-sand-200 bg-white p-5">
        <div className="flex items-center gap-4">
          {/* Avatar placeholder with initials */}
          <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-brand-100 text-xl font-bold text-brand-700 select-none">
            {initials || '?'}
          </div>

          <div className="min-w-0 flex-1">
            {editingName ? (
              <div className="flex items-center gap-2">
                <Input
                  type="text"
                  value={nameInput}
                  onChange={(e) => setNameInput(e.target.value)}
                  maxLength={100}
                  autoFocus
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') void handleSaveName()
                    if (e.key === 'Escape') setEditingName(false)
                  }}
                  className="min-w-0 flex-1"
                />
                <Button
                  size="sm"
                  loading={updatingName}
                  disabled={!nameInput.trim()}
                  onClick={() => void handleSaveName()}
                >
                  Guardar
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setEditingName(false)}
                >
                  Cancelar
                </Button>
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <span className="truncate text-base font-semibold text-sand-900">{displayName}</span>
                <Button variant="ghost" size="sm" onClick={handleEditName}>
                  Editar
                </Button>
              </div>
            )}

            <p className="mt-0.5 truncate text-sm text-sand-500">{serverProfile?.email ?? user?.email}</p>
            <Badge variant="neutral" className="mt-1">{user?.role}</Badge>
          </div>
        </div>
      </div>

      {/* ── Foster section ────────────────────────────────────────────── */}
      <div className="rounded-2xl border border-sand-200 bg-white p-5">
        <h2 className="text-base font-semibold text-sand-800">Voluntariado</h2>
        <p className="mt-1 text-sm text-sand-500">
          Activa esta opción para ofrecer custodia temporal a mascotas encontradas.
        </p>

        <button
          type="button"
          role="switch"
          aria-checked={isVolunteer}
          onClick={() => setIsVolunteer((v) => !v)}
          className={`mt-4 flex items-center gap-2 rounded-xl border px-4 py-2 text-sm font-semibold transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400 ${
            isVolunteer
              ? 'border-rescue-500 bg-rescue-50 text-rescue-700'
              : 'border-sand-300 bg-white text-sand-600 hover:bg-sand-50'
          }`}
        >
          <span
            className={`h-4 w-4 rounded-full border-2 transition-base ${
              isVolunteer ? 'border-rescue-500 bg-rescue-500' : 'border-sand-400 bg-white'
            }`}
            aria-hidden="true"
          />
          Soy custodio voluntario
        </button>

        {isVolunteer && (
          <div className="mt-4 space-y-4">
            <div>
              <p className="mb-2 text-sm font-medium text-sand-700">Ubicación de referencia</p>
              <Button variant="rescue" size="sm" onClick={requestLocation}>
                📍 Usar mi ubicación actual
              </Button>
              <p className="mt-2 text-xs text-sand-500">
                Lat: {homeLat.toFixed(5)} · Lng: {homeLng.toFixed(5)}
              </p>
            </div>

            <div>
              <p className="mb-2 text-sm font-medium text-sand-700">Especies aceptadas</p>
              <div className="flex flex-wrap gap-2">
                {ALL_SPECIES.map((species) => (
                  <button
                    key={species}
                    type="button"
                    onClick={() => toggleSpecies(species)}
                    className={`rounded-full px-3 py-1 text-xs font-semibold transition-base ${
                      acceptedSpecies.includes(species)
                        ? 'bg-rescue-100 text-rescue-800'
                        : 'bg-sand-100 text-sand-600 hover:bg-sand-200'
                    }`}
                  >
                    {species}
                  </button>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <label className="text-sm text-sand-700">
                Tamaño preferido
                <select
                  value={sizePreference}
                  onChange={(e) => setSizePreference(e.target.value)}
                  className="mt-1 w-full rounded-xl border border-sand-300 px-3 py-2 text-sm focus:border-brand-400 focus:outline-none focus:ring-2 focus:ring-brand-100"
                >
                  <option value="">Sin preferencia</option>
                  <option value="Small">Pequeño</option>
                  <option value="Medium">Mediano</option>
                  <option value="Large">Grande</option>
                </select>
              </label>

              <label className="text-sm text-sand-700">
                Máximo de días
                <input
                  type="number"
                  min={1}
                  max={30}
                  inputMode="numeric"
                  value={maxDays}
                  onChange={(e) => setMaxDays(Number(e.target.value))}
                  className="mt-1 w-full rounded-xl border border-sand-300 px-3 py-2 text-sm focus:border-brand-400 focus:outline-none focus:ring-2 focus:ring-brand-100"
                />
              </label>
            </div>
          </div>
        )}

        <Button
          fullWidth
          variant="primary"
          loading={savingFoster}
          disabled={!canSaveFoster}
          onClick={() => void handleSaveFoster()}
          className="mt-5"
        >
          Guardar perfil de custodio
        </Button>
      </div>
    </div>
  )
}

