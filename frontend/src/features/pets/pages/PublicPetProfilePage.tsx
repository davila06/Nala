import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { LostPetBanner } from '@/features/lost-pets/components/LostPetBanner'
import { SharePetButton } from '@/features/lost-pets/components/SharePetButton'
import { useGetLostPetContact } from '@/features/lost-pets/hooks/useLostPets'
import { FraudReportButton } from '@/features/safety/components/FraudReportButton'
import { PetStatusBadge } from '../components/PetStatusBadge'
import { usePublicPetProfile } from '../hooks/usePets'
import { useAuthStore } from '@/features/auth/store/authStore'

export default function PublicPetProfilePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: pet, isLoading, isError } = usePublicPetProfile(id ?? '')
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  const [revealPhone, setRevealPhone] = useState(false)
  const {
    data: contact,
    isLoading: contactLoading,
  } = useGetLostPetContact(revealPhone ? (pet?.activeLostEventId ?? null) : null)

  const handleRevealPhone = () => {
    if (!isAuthenticated) {
      navigate(`/login?return=/p/${id ?? ''}`)
      return
    }
    setRevealPhone(true)
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-white">
        <div className="animate-pulse space-y-3 p-4">
          <div className="h-72 rounded-2xl bg-sand-100" />
          <div className="h-7 w-36 rounded bg-sand-100" />
          <div className="h-5 w-24 rounded bg-sand-100" />
        </div>
      </div>
    )
  }

  if (isError || !pet) {
    return (
      <div role="alert" className="flex min-h-screen flex-col items-center justify-center gap-4 bg-white px-6 text-center">
        <span className="text-6xl" aria-hidden="true">🔍</span>
        <h1 className="text-2xl font-bold text-sand-900">Perfil no encontrado</h1>
        <p className="text-sm text-sand-500">
          Este código QR puede ya no estar activo o la mascota fue eliminada.
        </p>
      </div>
    )
  }

  const isLost = pet.status === 'Lost'

  return (
    <div className="min-h-screen bg-white">
      {/* Lost banner */}
      {isLost && <LostPetBanner petName={pet.name} publicMessage={pet.publicMessage} />}

      {/* Photo */}
      <div className="relative overflow-hidden">
        {pet.photoUrl ? (
          <img
            src={pet.photoUrl}
            alt={pet.name}
            loading="lazy"
            className="h-72 w-full object-cover"
          />
        ) : (
          <div aria-hidden="true" className="flex h-72 w-full items-center justify-center bg-brand-50 text-8xl">
            {pet.species === 'Dog' ? '🐶' : pet.species === 'Cat' ? '🐱' : '🐾'}
          </div>
        )}
      </div>

      {/* Content */}
      <div className="px-5 pb-10 pt-5">
        {/* Name + badge */}
        <div className="mb-1 flex items-center gap-2.5">
          <h1 className="text-3xl font-display font-semibold text-sand-900">{pet.name}</h1>
          <PetStatusBadge status={pet.status} />
        </div>

        {/* Species / breed */}
        <p className="mb-5 text-base text-sand-500">
          {{ Dog: 'Perro', Cat: 'Gato', Bird: 'Ave', Rabbit: 'Conejo', Other: 'Otra' }[pet.species] ?? pet.species}
          {pet.breed ? ` · ${pet.breed}` : ''}
        </p>

        {/* Report sighting CTA */}
        <Link
          to={`/p/${pet.id}/report-sighting`}
          className="mb-3 block w-full rounded-2xl bg-brand-500 py-4 text-center text-base font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          <span aria-hidden="true">🐾</span> Reportar avistamiento
        </Link>

        {/* Share profile — so anyone who finds the pet can spread the word */}
        <SharePetButton
          petId={pet.id}
          petName={pet.name}
          variant="outline"
          className="mb-4"
        />

        {/* Safe masked chat CTA — only while the pet is lost */}
        {isLost && pet.activeLostEventId && pet.ownerId && (
          <Link
            to={`/chat/${pet.activeLostEventId}/${pet.ownerId}`}
            className="mb-3 flex w-full items-center justify-center gap-2 rounded-2xl border border-sand-200 bg-white py-3.5 text-sm font-semibold text-sand-700 shadow-sm hover:bg-sand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
          >
            <span aria-hidden="true">💬</span> Contactar al dueño (chat seguro)
          </Link>
        )}

        {/* Contact card — visible when pet is lost and a contact exists */}
        {isLost && pet.activeLostEventId && (pet.contactName ?? contact?.contactName) && (
          <div className="mb-4 rounded-2xl border border-brand-200 bg-brand-50 p-4">
            <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-brand-700">
              Contacto del dueño
            </p>
            <p className="mb-3 text-sm font-semibold text-sand-800">
              {pet.contactName ?? contact?.contactName}
            </p>

            {contact?.contactPhone ? (
              <a
                href={`tel:${contact.contactPhone}`}
                className="flex items-center gap-2 rounded-xl bg-brand-500 px-4 py-2.5 text-sm font-bold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-1"
              >
                <span aria-hidden="true">📞</span> {contact.contactPhone}
              </a>
            ) : (
              <button
                type="button"
                onClick={handleRevealPhone}
                disabled={contactLoading}
                className="flex w-full items-center justify-center gap-2 rounded-xl border border-brand-400 bg-white px-4 py-2.5 text-sm font-semibold text-brand-700 hover:bg-brand-50 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                {contactLoading ? (
                  <span className="inline-block h-4 w-4 animate-spin rounded-full border-2 border-brand-400 border-t-transparent" />
                ) : (
                  <><span aria-hidden="true">📞</span> Ver número de teléfono</>
                )}
              </button>
            )}
          </div>
        )}

        {/* PawTrack attribution */}
        <div className="mt-8 flex flex-col items-center gap-3">
          {/* Fraud report — always visible but compact */}
          <FraudReportButton
            context="PublicProfile"
            relatedEntityId={pet.activeLostEventId}
            targetUserId={pet.ownerId}
          />
          <Link
            to="/"
            className="rounded text-xs font-bold tracking-wider text-sand-400 hover:text-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Powered by PawTrack CR
          </Link>
        </div>
      </div>
    </div>
  )
}
