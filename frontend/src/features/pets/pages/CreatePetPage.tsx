import { useNavigate, useParams } from 'react-router-dom'
import { PetForm } from '../components/PetForm'
import { useCreatePet } from '../hooks/useCreatePet'
import { useUpdatePet } from '../hooks/useUpdatePet'
import { usePetDetail } from '../hooks/usePets'
import type { CreatePetRequest } from '../api/petsApi'
import { Alert } from '@/shared/ui/Alert'

export default function CreatePetPage() {
  const { id } = useParams<{ id: string }>()
  const isEditMode = Boolean(id)
  const navigate = useNavigate()

  const { data: existing, isLoading: loadingExisting } = usePetDetail(id ?? '')
  const createMutation = useCreatePet()
  const updateMutation = useUpdatePet(id ?? '')

  const isLoading = createMutation.isPending || updateMutation.isPending

  const handleSubmit = async (data: CreatePetRequest) => {
    try {
      if (isEditMode && id) {
        await updateMutation.mutateAsync(data)
        navigate(`/pets/${id}`)
      } else {
        const response = await createMutation.mutateAsync(data)
        navigate(`/pets/${response.petId}`)
      }
    } catch {
      // Errors shown via mutation state
    }
  }

  if (isEditMode && loadingExisting) {
    return (
      <div className="mx-auto max-w-md px-4 py-12">
        <div className="h-8 w-48 animate-pulse rounded-lg bg-sand-100" />
      </div>
    )
  }

  const error = createMutation.error || updateMutation.error

  return (
    <div className="mx-auto max-w-md px-4 py-10 animate-fade-in-up">
      <div className="mb-8">
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="mb-4 flex items-center gap-1.5 rounded-lg text-sm text-sand-500 hover:text-sand-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          <span aria-hidden="true">←</span> Volver
        </button>
        <h1 className="text-xl font-bold text-sand-900">
          {isEditMode ? `Editar a ${existing?.name ?? 'mascota'}` : 'Registrar mascota'}
        </h1>
        <p className="mt-0.5 text-sm text-sand-500">
          {isEditMode ? 'Actualiza la información de tu mascota.' : 'Completa la información de tu mascota.'}
        </p>
      </div>

      {error && (
        <Alert variant="error" className="mb-5">
          Ocurrió un error inesperado. Por favor, intenta de nuevo.
        </Alert>
      )}

      <PetForm
        defaultValues={
          existing
            ? {
                name: existing.name,
                species: existing.species,
                breed: existing.breed ?? '',
                birthDate: existing.birthDate ?? '',
              }
            : undefined
        }
        existingPhotoUrl={existing?.photoUrl}
        onSubmit={handleSubmit}
        isLoading={isLoading}
        submitLabel={isEditMode ? 'Guardar cambios' : 'Registrar mascota'}
      />
    </div>
  )
}

