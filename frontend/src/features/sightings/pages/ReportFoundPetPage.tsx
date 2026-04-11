import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useReportFoundPet } from '../hooks/useFoundPets'
import type { PetSpecies, ReportFoundPetPayload } from '../api/foundPetsApi'
import { BREEDS_BY_SPECIES } from '@/features/pets/data/breeds'

// ── Step types ────────────────────────────────────────────────────────────────

type Step = 1 | 2 | 3 | 4

const SPECIES_OPTIONS: { value: PetSpecies; label: string; emoji: string }[] = [
  { value: 'Dog', label: 'Perro', emoji: '🐕' },
  { value: 'Cat', label: 'Gato', emoji: '🐈' },
  { value: 'Bird', label: 'Ave', emoji: '🐦' },
  { value: 'Rabbit', label: 'Conejo', emoji: '🐇' },
  { value: 'Other', label: 'Otro', emoji: '🐾' },
]

// ── Step indicator ────────────────────────────────────────────────────────────

function StepIndicator({ current, total }: { current: Step; total: number }) {
  return (
    <div className="flex items-center justify-center gap-2">
      {Array.from({ length: total }, (_, i) => i + 1).map((step) => (
        <div
          key={step}
          className={`h-2 w-8 rounded-full transition-colors ${
            step === current
              ? 'bg-rescue-500'
              : step < current
                ? 'bg-rescue-200'
                : 'bg-sand-200'
          }`}
        />
      ))}
    </div>
  )
}

// ── Main component ────────────────────────────────────────────────────────────

export default function ReportFoundPetPage() {
  const navigate = useNavigate()
  const { mutateAsync, isPending } = useReportFoundPet()

  const [step, setStep] = useState<Step>(1)
  const [photoFile, setPhotoFile] = useState<File | null>(null)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  const [species, setSpecies] = useState<PetSpecies | null>(null)
  const [breedEstimate, setBreedEstimate] = useState('')
  const [colorDescription, setColorDescription] = useState('')
  const [sizeEstimate, setSizeEstimate] = useState('')
  const [note, setNote] = useState('')
  const [foundLat, setFoundLat] = useState<number | null>(null)
  const [foundLng, setFoundLng] = useState<number | null>(null)
  const [gpsError, setGpsError] = useState<string | null>(null)
  const [contactName, setContactName] = useState('')
  const [contactPhone, setContactPhone] = useState('')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  // Auto-request GPS on step 3
  useEffect(() => {
    if (step === 3 && foundLat === null) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          setFoundLat(pos.coords.latitude)
          setFoundLng(pos.coords.longitude)
          setGpsError(null)
        },
        () => setGpsError('No pudimos obtener tu ubicación. Ingresa las coordenadas manualmente.'),
        { timeout: 10_000 },
      )
    }
  }, [step, foundLat])

  function handlePhotoChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setPhotoFile(file)
    setPhotoPreview(URL.createObjectURL(file))
  }

  async function handleSubmit() {
    if (!species || foundLat === null || foundLng === null) return

    const payload: ReportFoundPetPayload = {
      foundSpecies: species,
      breedEstimate: breedEstimate.trim() || null,
      colorDescription: colorDescription.trim() || null,
      sizeEstimate: sizeEstimate.trim() || null,
      foundLat,
      foundLng,
      contactName: contactName.trim(),
      contactPhone: contactPhone.trim(),
      note: note.trim() || null,
      photo: photoFile,
    }

    try {
      setSubmitError(null)
      const result = await mutateAsync(payload)
      navigate('/encontre-mascota/resultados', { state: { result } })
    } catch {
      setSubmitError('Ocurrió un error. Por favor intenta de nuevo.')
    }
  }

  // ── Step renderers ──────────────────────────────────────────────────────────

  const stepTitles: Record<Step, string> = {
    1: 'Foto de la mascota',
    2: 'Descripción',
    3: 'Ubicación del hallazgo',
    4: 'Tus datos de contacto',
  }

  return (
    <div className="mx-auto max-w-md px-4 py-8">
      {/* Header */}
      <div className="mb-6 text-center">
        <p className="text-3xl" aria-hidden="true">🐾</p>
        <h1 className="mt-2 text-xl font-bold text-sand-900">Encontré una mascota</h1>
        <p className="mt-1 text-sm text-sand-500">Ayúdanos a reunirla con su familia</p>
      </div>

      <StepIndicator current={step} total={4} />

      <h2 className="mb-4 mt-5 text-center text-sm font-semibold uppercase tracking-wide text-sand-400">
        Paso {step} — {stepTitles[step]}
      </h2>

      {/* ── Step 1: Photo ── */}
      {step === 1 && (
        <div className="space-y-4">
          <div
            role="button"
            tabIndex={0}
            aria-label="Subir foto de la mascota"
            onClick={() => fileInputRef.current?.click()}
            onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && fileInputRef.current?.click()}
            className="flex h-48 cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed border-sand-300 bg-sand-50 transition hover:border-rescue-400 hover:bg-rescue-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
          >
            {photoPreview ? (
              <img
                src={photoPreview}
                alt="Vista previa"
                className="h-full w-full rounded-xl object-cover"
              />
            ) : (
              <>  
                <span className="text-4xl" aria-hidden="true">📷</span>
                <p className="mt-2 text-sm text-sand-500">Toca para agregar una foto</p>
              </>
            )}
          </div>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="hidden"
            onChange={handlePhotoChange}
          />
          <p className="text-center text-xs text-sand-400">La foto es opcional pero ayuda mucho</p>
          <button
            type="button"
            onClick={() => setStep(2)}
            className="w-full rounded-xl bg-rescue-500 py-3 text-sm font-semibold text-white transition hover:bg-rescue-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
          >
            {photoPreview ? 'Continuar con esta foto' : 'Continuar sin foto'}
          </button>
        </div>
      )}

      {/* ── Step 2: Description ── */}
      {step === 2 && (
        <div className="space-y-4">
          <div>
            <fieldset className="m-0 border-0 p-0">
              <legend className="mb-1 block text-sm font-medium text-sand-700">
                Especie <span className="text-danger-500">*</span>
              </legend>
              <div className="grid grid-cols-5 gap-2">
                {SPECIES_OPTIONS.map((opt) => (
                  <button
                    key={opt.value}
                    type="button"
                    aria-pressed={species === opt.value}
                    onClick={() => { setSpecies(opt.value); setBreedEstimate('') }}
                    className={`flex flex-col items-center rounded-xl border p-3 transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400 ${
                      species === opt.value
                        ? 'border-rescue-500 bg-rescue-50'
                        : 'border-sand-200 bg-white hover:border-sand-300'
                    }`}
                  >
                    <span className="text-xl">{opt.emoji}</span>
                    <span className="mt-1 text-[10px] text-sand-600">{opt.label}</span>
                  </button>
                ))}
              </div>
            </fieldset>
          </div>

          {species && (
            <div>
              <label className="mb-1 block text-sm font-medium text-sand-700">Raza</label>
              <select
                value={breedEstimate}
                onChange={(e) => setBreedEstimate(e.target.value)}
                className="w-full rounded-lg border border-sand-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
              >
                <option value="">Selecciona una raza (opcional)</option>
                {BREEDS_BY_SPECIES[species].map((breed) => (
                  <option key={breed} value={breed}>{breed}</option>
                ))}
              </select>
            </div>
          )}

          <div>
            <label className="mb-1 block text-sm font-medium text-sand-700">Color / descripción</label>
            <input
              value={colorDescription}
              onChange={(e) => setColorDescription(e.target.value)}
              maxLength={200}
              placeholder="Ej: naranja con manchas blancas"
              className="w-full rounded-lg border border-sand-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-sand-700">Tamaño aproximado</label>
            <select
              value={sizeEstimate}
              onChange={(e) => setSizeEstimate(e.target.value)}
              className="w-full rounded-lg border border-sand-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            >
              <option value="">Selecciona una opción</option>
              <option value="Pequeño">Pequeño (menos de 10 kg)</option>
              <option value="Mediano">Mediano (10–25 kg)</option>
              <option value="Grande">Grande (más de 25 kg)</option>
            </select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-sand-700">Nota adicional</label>
            <textarea
              value={note}
              onChange={(e) => setNote(e.target.value)}
              maxLength={500}
              rows={3}
              placeholder="¿Algo más que quieras agregar?"
              className="w-full resize-none rounded-lg border border-sand-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            />
          </div>

          <div className="flex gap-3">
            <button
              onClick={() => setStep(1)}
              className="flex-1 rounded-xl border border-sand-300 py-3 text-sm font-medium text-sand-600 transition hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              Atrás
            </button>
            <button
              onClick={() => species && setStep(3)}
              disabled={!species}
              className="flex-1 rounded-xl bg-rescue-500 py-3 text-sm font-semibold text-white transition hover:bg-rescue-600 disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
            >
              Continuar
            </button>
          </div>
        </div>
      )}

      {/* ── Step 3: GPS location ── */}
      {step === 3 && (
        <div className="space-y-4">
          {foundLat === null ? (
            <div className="flex flex-col items-center gap-3 rounded-xl bg-sand-50 p-6 text-center">
              <span className="animate-spin text-3xl" aria-hidden="true">📍</span>
              <p className="text-sm text-sand-600">Obteniendo tu ubicación…</p>
            </div>
          ) : (
            <div className="rounded-xl bg-rescue-50 p-4 text-center">
              <p className="text-2xl" aria-hidden="true">📍</p>
              <p className="mt-1 text-sm font-medium text-rescue-700">Ubicación capturada</p>
              <p className="mt-1 text-xs text-sand-500">
                {foundLat.toFixed(5)}, {foundLng?.toFixed(5)}
              </p>
            </div>
          )}

          {gpsError && (
            <div className="space-y-2">
              <p className="text-center text-sm text-danger-600">{gpsError}</p>
              <div className="flex gap-2">
                <input
                  type="number"
                  placeholder="Latitud"
                  step="any"
                  inputMode="decimal"
                  onChange={(e) => setFoundLat(parseFloat(e.target.value))}
                  className="flex-1 rounded-lg border border-sand-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                />
                <input
                  type="number"
                  placeholder="Longitud"
                  step="any"
                  inputMode="decimal"
                  onChange={(e) => setFoundLng(parseFloat(e.target.value))}
                  className="flex-1 rounded-lg border border-sand-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                />
              </div>
            </div>
          )}

          <div className="flex gap-3">
            <button
              onClick={() => setStep(2)}
              className="flex-1 rounded-xl border border-sand-300 py-3 text-sm font-medium text-sand-600 transition hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              Atrás
            </button>
            <button
              onClick={() => foundLat !== null && setStep(4)}
              disabled={foundLat === null}
              className="flex-1 rounded-xl bg-rescue-500 py-3 text-sm font-semibold text-white transition hover:bg-rescue-600 disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
            >
              Continuar
            </button>
          </div>
        </div>
      )}

      {/* ── Step 4: Contact info ── */}
      {step === 4 && (
        <div className="space-y-4">
          <p className="text-sm text-sand-500">
            Tus datos solo se compartirán con el dueño si encontramos una coincidencia.
          </p>

          <div>
            <label className="mb-1 block text-sm font-medium text-sand-700">
              Tu nombre <span className="text-danger-500">*</span>
            </label>
            <input
              value={contactName}
              onChange={(e) => setContactName(e.target.value)}
              maxLength={100}
              autoComplete="name"
              placeholder="Ej: María González"
              className="w-full rounded-lg border border-sand-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-sand-700">
              Teléfono <span className="text-danger-500">*</span>
            </label>
            <input
              value={contactPhone}
              onChange={(e) => setContactPhone(e.target.value)}
              maxLength={30}
              type="tel"
              inputMode="tel"
              autoComplete="tel"
              placeholder="Ej: 8888-8888"
              className="w-full rounded-lg border border-sand-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            />
          </div>

          {submitError && (
            <p className="text-center text-sm text-danger-600">{submitError}</p>
          )}

          <div className="flex gap-3">
            <button
              onClick={() => setStep(3)}
              className="flex-1 rounded-xl border border-sand-300 py-3 text-sm font-medium text-sand-600 transition hover:bg-sand-50"
            >
              Atrás
            </button>
            <button
              onClick={handleSubmit}
              disabled={!contactName.trim() || !contactPhone.trim() || isPending}
              className="flex-1 rounded-xl bg-rescue-500 py-3 text-sm font-semibold text-white transition hover:bg-rescue-600 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isPending ? (
                <span className="flex items-center justify-center gap-2">
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  Enviando…
                </span>
              ) : (
                'Enviar reporte'
              )}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

