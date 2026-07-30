import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { PhotoUpload } from '../components/PhotoUpload'
import { BreedCombobox } from '../components/BreedCombobox'
import { useCreatePet } from '../hooks/useCreatePet'
import { useUpdatePet } from '../hooks/useUpdatePet'
import { usePetDetail } from '../hooks/usePets'
import type { CreatePetRequest, PetSpecies } from '../api/petsApi'
import { Alert } from '@/shared/ui/Alert'
import { useHaptic } from '@/shared/hooks/useHaptic'

const SPECIES_OPTIONS: { value: PetSpecies; label: string; emoji: string }[] = [
  { value: 'Dog',    label: 'Perro',  emoji: '🐶' },
  { value: 'Cat',    label: 'Gato',   emoji: '🐱' },
  { value: 'Bird',   label: 'Ave',    emoji: '🐦' },
  { value: 'Rabbit', label: 'Conejo', emoji: '🐰' },
  { value: 'Other',  label: 'Otra',   emoji: '🐾' },
]

const STEPS = [
  { id: 1, label: 'Básicos',  emoji: '🏷️' },
  { id: 2, label: 'Detalles', emoji: '📷' },
  { id: 3, label: 'Confirmar', emoji: '✅' },
]

export default function CreatePetPage() {
  const { id } = useParams<{ id: string }>()
  const isEditMode = Boolean(id)
  const navigate = useNavigate()
  const { tap, success } = useHaptic()

  const { data: existing, isLoading: loadingExisting } = usePetDetail(id ?? '')
  const createMutation = useCreatePet()
  const updateMutation = useUpdatePet(id ?? '')
  const isLoading = createMutation.isPending || updateMutation.isPending
  const [step, setStep] = useState<1 | 2 | 3>(1)
  const [direction, setDirection] = useState<1 | -1>(1)

  // Form state
  const [name, setName] = useState(existing?.name ?? '')
  const [species, setSpecies] = useState<PetSpecies>(existing?.species ?? 'Dog')
  const [breed, setBreed] = useState(existing?.breed ?? '')
  const [birthDate, setBirthDate] = useState(existing?.birthDate ?? '')
  const [microchipId, setMicrochipId] = useState(existing?.microchipId ?? '')
  const [photo, setPhoto] = useState<File | null>(null)

  const goNext = () => { setDirection(1); tap(); setStep((s) => Math.min(3, s + 1) as 1 | 2 | 3) }
  const goBack = () => { setDirection(-1); tap(); setStep((s) => Math.max(1, s - 1) as 1 | 2 | 3) }

  const handleSubmit = async () => {
    const data: CreatePetRequest = {
      name: name.trim(),
      species,
      breed: breed.trim() || undefined,
      birthDate: birthDate || undefined,
      photo: photo ?? undefined,
      microchipId: microchipId.trim().toUpperCase() || undefined,
    }
    try {
      if (isEditMode && id) {
        await updateMutation.mutateAsync(data)
        success()
        navigate(`/pets/${id}`)
      } else {
        const response = await createMutation.mutateAsync(data)
        success()
        navigate(`/pets/${response.petId}`)
      }
    } catch { /* errors shown via mutation state */ }
  }

  const hasError = !!(createMutation.error || updateMutation.error)

  const slideVariants = {
    enter: (d: number) => ({ x: d > 0 ? 40 : -40, opacity: 0 }),
    center: { x: 0, opacity: 1 },
    exit:  (d: number) => ({ x: d > 0 ? -40 : 40, opacity: 0 }),
  }

  if (isEditMode && loadingExisting) {
    return (
      <div className="mx-auto max-w-md px-4 py-12">
        <div className="h-8 w-48 animate-pulse rounded-lg bg-sand-100" />
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-md px-4 py-8 animate-fade-in-up">
      {/* Back button */}
      <button
        type="button"
        onClick={() => navigate(-1)}
        className="mb-5 flex items-center gap-1.5 rounded-lg text-sm text-sand-500 hover:text-sand-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
      >
        ← Volver
      </button>

      {/* Page header */}
      <div className="mb-6 rounded-2xl border border-brand-200 bg-gradient-to-br from-brand-50 to-white p-5">
        <h1 className="font-display text-xl font-bold text-sand-900">
          {isEditMode ? `Editar a ${existing?.name ?? 'mascota'}` : '🐾 Registrar mascota'}
        </h1>
        <p className="mt-0.5 text-sm text-sand-500">
          {isEditMode ? 'Actualiza la información.' : `Paso ${step} de 3 — ${STEPS[step - 1].label}`}
        </p>
      </div>

      {/* Step indicators */}
      <div className="mb-6 flex items-center">
        {STEPS.map((s, i) => {
          const isActive = s.id === step
          const isDone   = s.id < step
          return (
            <div key={s.id} className="flex flex-1 items-center">
              <div className="flex flex-col items-center flex-1">
                <motion.div
                  animate={{
                    scale: isActive ? 1.1 : 1,
                    backgroundColor: isDone ? '#17a26d' : isActive ? '#e8521e' : '#e2d3c4',
                  }}
                  className="flex h-9 w-9 items-center justify-center rounded-full text-sm font-bold text-white shadow-sm"
                  transition={{ type: 'spring', stiffness: 400, damping: 30 }}
                >
                  {isDone ? '✓' : s.emoji}
                </motion.div>
                <span className={['mt-1 text-[10px] font-semibold', isActive ? 'text-brand-600' : isDone ? 'text-rescue-600' : 'text-sand-400'].join(' ')}>
                  {s.label}
                </span>
              </div>
              {i < STEPS.length - 1 && (
                <div className="h-0.5 w-full flex-1 mx-1 rounded-full overflow-hidden bg-sand-200">
                  <motion.div
                    className="h-full bg-rescue-500 origin-left"
                    animate={{ scaleX: step > s.id ? 1 : 0 }}
                    transition={{ duration: 0.3 }}
                  />
                </div>
              )}
            </div>
          )
        })}
      </div>

      {hasError && (
        <Alert variant="error" className="mb-4">
          Ocurrió un error. Por favor intenta de nuevo.
        </Alert>
      )}

      {/* Animated steps */}
      <div className="overflow-hidden">
        <AnimatePresence mode="wait" custom={direction} initial={false}>
          <motion.div
            key={step}
            custom={direction}
            variants={slideVariants}
            initial="enter"
            animate="center"
            exit="exit"
            transition={{ duration: 0.22, ease: [0.4, 0, 0.2, 1] }}
          >
            {/* ══ STEP 1: Básicos ════════════════════════════════════════════ */}
            {step === 1 && (
              <div className="space-y-5">
                <div>
                  <label htmlFor="pet-name" className="mb-1 block text-sm font-semibold text-sand-700">
                    Nombre <span aria-hidden="true" className="text-danger-500">*</span>
                  </label>
                  <input
                    id="pet-name"
                    type="text"
                    required
                    maxLength={100}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="Ej. Firulais"
                    autoFocus
                    className="block w-full rounded-xl border border-sand-300 px-3.5 py-2.5 text-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200"
                  />
                </div>

                <div>
                  <p className="mb-2 text-sm font-semibold text-sand-700">Especie</p>
                  <div className="grid grid-cols-5 gap-2">
                    {SPECIES_OPTIONS.map((opt) => (
                      <button
                        key={opt.value}
                        type="button"
                        onClick={() => { setSpecies(opt.value); setBreed('') }}
                        className={[
                          'flex flex-col items-center gap-1 rounded-xl border py-3 text-xs font-semibold transition-all',
                          species === opt.value
                            ? 'border-brand-400 bg-brand-50 text-brand-700 shadow-sm scale-105'
                            : 'border-sand-200 bg-white text-sand-500 hover:border-sand-300 hover:bg-sand-50',
                        ].join(' ')}
                      >
                        <span className="text-xl">{opt.emoji}</span>
                        {opt.label}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* ══ STEP 2: Detalles ═══════════════════════════════════════════ */}
            {step === 2 && (
              <div className="space-y-5">
                <div>
                  <p className="mb-1.5 text-sm font-semibold text-sand-700">📷 Foto (opcional)</p>
                  <p className="mb-2 text-xs text-sand-500">Se usará en el perfil público y la placa QR.</p>
                  <PhotoUpload value={photo} onChange={setPhoto} disabled={isLoading} />
                </div>

                <div>
                  <label htmlFor="pet-breed" className="mb-1 block text-sm font-semibold text-sand-700">
                    Raza (opcional)
                  </label>
                  <BreedCombobox
                    species={species}
                    defaultValue={breed}
                    disabled={isLoading}
                  />
                </div>

                <div>
                  <label htmlFor="pet-birthdate" className="mb-1 block text-sm font-semibold text-sand-700">
                    Fecha de nacimiento (opcional)
                  </label>
                  <input
                    id="pet-birthdate"
                    type="date"
                    value={birthDate}
                    onChange={(e) => setBirthDate(e.target.value)}
                    max={new Date().toISOString().split('T')[0]}
                    className="block w-full rounded-xl border border-sand-300 px-3.5 py-2.5 text-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200"
                  />
                </div>

                <div>
                  <label htmlFor="pet-microchip" className="mb-1 block text-sm font-semibold text-sand-700">
                    Microchip RFID
                    <span className="ml-1 text-xs font-normal text-sand-400">(ISO 11784 — opcional)</span>
                  </label>
                  <input
                    id="pet-microchip"
                    type="text"
                    maxLength={15}
                    value={microchipId}
                    onChange={(e) => setMicrochipId(e.target.value.toUpperCase().replace(/[^A-F0-9]/g, ''))}
                    placeholder="Ej. 0006000123456"
                    className="block w-full rounded-xl border border-sand-300 px-3.5 py-2.5 font-mono text-sm uppercase outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200"
                  />
                  <p className="mt-1 text-xs text-sand-400">El código de 15 dígitos del chip de tu veterinario.</p>
                </div>
              </div>
            )}

            {/* ══ STEP 3: Confirmar ══════════════════════════════════════════ */}
            {step === 3 && (
              <div className="space-y-5">
                {/* Preview card */}
                <div className="rounded-2xl border border-sand-200 field-input overflow-hidden shadow-sm">
                  {/* Photo preview */}
                  <div className="h-48 bg-brand-50 flex items-center justify-center overflow-hidden">
                    {photo ? (
                      <img
                        src={URL.createObjectURL(photo)}
                        alt="Vista previa"
                        className="h-full w-full object-cover"
                      />
                    ) : existing?.photoUrl ? (
                      <img
                        src={existing.photoUrl}
                        alt="Foto actual"
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <span className="text-7xl">
                        {SPECIES_OPTIONS.find((s) => s.value === species)?.emoji ?? '🐾'}
                      </span>
                    )}
                  </div>

                  <div className="p-4">
                    <h2 className="font-display text-xl font-bold text-sand-900">{name || '—'}</h2>
                    <p className="mt-0.5 text-sm text-sand-500">
                      {SPECIES_OPTIONS.find((s) => s.value === species)?.label}
                      {breed ? ` · ${breed}` : ''}
                      {birthDate ? ` · Nació: ${new Date(birthDate).toLocaleDateString('es-CR')}` : ''}
                    </p>
                  </div>
                </div>

                <p className="text-xs text-sand-400 text-center">
                  Se generará un código QR único para {name || 'tu mascota'} al guardar.
                </p>

                <button
                  type="button"
                  onClick={() => void handleSubmit()}
                  disabled={isLoading || !name.trim()}
                  className="group relative w-full overflow-hidden rounded-2xl bg-brand-500 py-4 text-sm font-bold text-white shadow-md shadow-brand-200 hover:bg-brand-600 disabled:opacity-50 transition-all hover:-translate-y-0.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  <span className="pointer-events-none absolute inset-0 translate-x-[-120%] skew-x-[-20deg] bg-white/20 group-hover:translate-x-[220%] transition-transform duration-700" aria-hidden="true" />
                  {isLoading
                    ? 'Guardando…'
                    : isEditMode
                      ? '💾 Guardar cambios'
                      : '🐾 Registrar mascota'}
                </button>
              </div>
            )}
          </motion.div>
        </AnimatePresence>
      </div>

      {/* Nav buttons */}
      <div className="mt-6 flex gap-3">
        {step > 1 && (
          <button
            type="button"
            onClick={goBack}
            className="flex-1 rounded-xl border border-sand-300 py-3 text-sm font-semibold text-sand-700 hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            ← Anterior
          </button>
        )}
        {step < 3 && (
          <button
            type="button"
            onClick={goNext}
            disabled={step === 1 && !name.trim()}
            className="flex-1 rounded-xl bg-brand-500 py-3 text-sm font-semibold text-white hover:bg-brand-600 disabled:opacity-40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Siguiente →
          </button>
        )}
      </div>
    </div>
  )
}
