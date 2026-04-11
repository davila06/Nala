import { useEffect, useRef, useState } from 'react'
import { PhotoUpload } from './PhotoUpload'
import { BreedCombobox } from './BreedCombobox'
import type { CreatePetRequest, PetSpecies } from '../api/petsApi'

const SPECIES_OPTIONS: { value: PetSpecies; label: string }[] = [
  { value: 'Dog', label: '🐶 Perro' },
  { value: 'Cat', label: '🐱 Gato' },
  { value: 'Bird', label: '🐦 Ave' },
  { value: 'Rabbit', label: '🐰 Conejo' },
  { value: 'Other', label: '🐾 Otra' },
]

export interface PetFormValues {
  name: string
  species: PetSpecies
  breed: string
  birthDate: string
  photo: File | null
}

interface PetFormProps {
  defaultValues?: Partial<PetFormValues>
  existingPhotoUrl?: string | null
  onSubmit: (data: CreatePetRequest) => void
  isLoading?: boolean
  submitLabel?: string
}

export const PetForm = ({
  defaultValues,
  existingPhotoUrl,
  onSubmit,
  isLoading,
  submitLabel = 'Guardar',
}: PetFormProps) => {
  const nameRef = useRef<HTMLInputElement>(null)
  const [species, setSpecies] = useState<PetSpecies>(defaultValues?.species ?? 'Dog')

  // Accessible: focus first field on mount
  useEffect(() => {
    nameRef.current?.focus()
  }, [])

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)

    const photo = (e.currentTarget.elements.namedItem('photoFile') as HTMLInputElement)
      ?.files?.[0] ?? undefined

    const data: CreatePetRequest = {
      name: (fd.get('name') as string).trim(),
      species: fd.get('species') as PetSpecies,
      breed: (fd.get('breed') as string).trim() || undefined,
      birthDate: (fd.get('birthDate') as string) || undefined,
      photo,
    }

    onSubmit(data)
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-5">
      {/* Name */}
      <div className="space-y-1">
        <label htmlFor="pet-name" className="block text-sm font-medium text-sand-700">
          Nombre <span aria-hidden="true" className="text-danger-500">*</span>
        </label>
        <input
          ref={nameRef}
          id="pet-name"
          name="name"
          type="text"
          required
          maxLength={100}
          defaultValue={defaultValues?.name}
          placeholder="Ej. Firulais"
          className="block w-full rounded-xl border border-sand-300 px-3.5 py-2.5 text-sm shadow-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200"
        />
      </div>

      {/* Species */}
      <div className="space-y-1">
        <label htmlFor="pet-species" className="block text-sm font-medium text-sand-700">
          Especie <span aria-hidden="true" className="text-danger-500">*</span>
        </label>
        <select
          id="pet-species"
          name="species"
          required
          value={species}
          onChange={(e) => setSpecies(e.target.value as PetSpecies)}
          className="block w-full rounded-xl border border-sand-300 px-3.5 py-2.5 text-sm shadow-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200"
        >
          {SPECIES_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      {/* Breed */}
      <div className="space-y-1">
        <label htmlFor="pet-breed" className="block text-sm font-medium text-sand-700">
          Raza
        </label>
        <BreedCombobox
          id="pet-breed"
          species={species}
          defaultValue={defaultValues?.breed}
          disabled={isLoading}
        />
      </div>

      {/* Birth date */}
      <div className="space-y-1">
        <label htmlFor="pet-birthdate" className="block text-sm font-medium text-sand-700">
          Fecha de nacimiento
        </label>
        <input
          id="pet-birthdate"
          name="birthDate"
          type="date"
          max={new Date().toISOString().split('T')[0]}
          defaultValue={defaultValues?.birthDate}
          className="block w-full rounded-xl border border-sand-300 px-3.5 py-2.5 text-sm shadow-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200"
        />
      </div>

      {/* Photo */}
      <div className="space-y-1">
        <p className="text-sm font-medium text-sand-700">Foto</p>
        <PhotoUpload
          previewUrl={existingPhotoUrl}
          onChange={() => {/* handled via input ref in handleSubmit */}}
          disabled={isLoading}
        />
      </div>

      {/* Submit */}
      <button
        type="submit"
        disabled={isLoading}
        className="w-full rounded-xl bg-brand-500 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-brand-600 disabled:opacity-60 focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:outline-none"
      >
        {isLoading ? 'Guardando…' : submitLabel}
      </button>
    </form>
  )
}

