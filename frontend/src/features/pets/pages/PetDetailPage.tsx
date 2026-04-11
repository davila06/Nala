import { useState } from 'react'
import { jsPDF } from 'jspdf'
import { formatDate, formatDateTime } from '@/shared/lib/formatDate'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { LostPetBanner } from '@/features/lost-pets/components/LostPetBanner'
import { ReuniteButton } from '@/features/lost-pets/components/ReuniteButton'
import { SearchChecklist } from '@/features/lost-pets/components/SearchChecklist'
import { SharePetButton } from '@/features/lost-pets/components/SharePetButton'
import { useActiveLostReport } from '@/features/lost-pets/hooks/useLostPets'
import { SightingList } from '@/features/sightings/components/SightingList'
import { PetStatusBadge } from '../components/PetStatusBadge'
import { QRCodeDisplay } from '../components/QRCodeDisplay'
import { usePetDetail, usePetScanHistory } from '../hooks/usePets'
import { petsApi } from '../api/petsApi'
import { Alert } from '@/shared/ui/Alert'
import { Skeleton } from '@/shared/ui/Spinner'

export default function PetDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: pet, isLoading, isError } = usePetDetail(id ?? '')
  const { data: scanHistory } = usePetScanHistory(id ?? '')
  const { data: activeReport } = useActiveLostReport(id ?? '')
  const [showQr, setShowQr] = useState(false)
  const [showScanHistory, setShowScanHistory] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [avatarLoading, setAvatarLoading] = useState(false)

  if (isLoading) {
    return (
      <div className="mx-auto max-w-lg px-4 py-12">
        <div className="space-y-3">
          <Skeleton className="h-64 rounded-2xl" />
          <Skeleton className="h-6 w-40 rounded" />
          <Skeleton className="h-4 w-24 rounded" />
        </div>
      </div>
    )
  }

  if (isError || !pet) {
    return (
      <div className="mx-auto max-w-lg px-4 py-12">
        <Alert variant="error">Mascota no encontrada o acceso no autorizado.</Alert>
        <Link to="/dashboard" className="mt-4 inline-block text-sm text-brand-600 hover:underline">
          ← Volver al inicio
        </Link>
      </div>
    )
  }

  const handleDelete = async () => {
    setDeleting(true)
    try {
      await petsApi.deletePet(pet.id)
      navigate('/dashboard')
    } catch {
      setDeleting(false)
      setConfirmDelete(false)
    }
  }

  const handleWhatsAppAvatar = async () => {
    if (!pet || avatarLoading) return

    setAvatarLoading(true)
    try {
      const blob = await petsApi.getWhatsAppAvatar(pet.id)
      const url = URL.createObjectURL(blob)
      window.open(url, '_blank', 'noopener,noreferrer')
      setTimeout(() => URL.revokeObjectURL(url), 60_000)
    } finally {
      setAvatarLoading(false)
    }
  }

  const handleExportScanHistoryPdf = () => {
    if (!pet || !scanHistory || scanHistory.events.length === 0) return

    const doc = new jsPDF({ unit: 'pt', format: 'a4' })
    let y = 56

    doc.setFont('helvetica', 'bold')
    doc.setFontSize(16)
    doc.text(`Cadena de custodia QR - ${pet.name}`, 40, y)
    y += 24

    doc.setFont('helvetica', 'normal')
    doc.setFontSize(11)
    doc.text(`Escaneos hoy: ${scanHistory.scansToday}`, 40, y)
    y += 20
    doc.text(`Generado: ${new Date().toLocaleString()}`, 40, y)
    y += 26

    for (const event of scanHistory.events) {
      if (y > 760) {
        doc.addPage()
        y = 56
      }

      const when = formatDateTime(event.scannedAt)
      const location = [event.cityName, event.countryCode].filter(Boolean).join(', ') || 'Ubicacion aproximada desconocida'

      doc.text(`- ${when} | ${location} | ${event.deviceSummary}`, 40, y)
      y += 18
    }

    doc.save(`cadena-custodia-qr-${pet.name.toLowerCase()}.pdf`)
  }

  const todayLabel = scanHistory
    ? `${scanHistory.scansToday} ${scanHistory.scansToday === 1 ? 'escaneo' : 'escaneos'} hoy`
    : '0 escaneos hoy'

  return (
    <main className="mx-auto max-w-lg px-4 py-8 animate-fade-in-up">
      {/* Back */}
      <Link to="/dashboard" className="mb-5 flex items-center gap-1.5 rounded-lg text-sm text-sand-500 hover:text-sand-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400">
        ← Mis mascotas
      </Link>

      {/* Photo */}
      <div className="mb-6 overflow-hidden rounded-2xl border border-sand-200 bg-sand-100">
        {pet.photoUrl ? (
          <img src={pet.photoUrl} alt={pet.name} loading="lazy" className="h-64 w-full object-cover" />
        ) : (
          <div aria-hidden="true" className="flex h-64 items-center justify-center text-7xl">
            {pet.species === 'Dog' ? '🐶' : pet.species === 'Cat' ? '🐱' : '🐾'}
          </div>
        )}
      </div>

      {/* Lost banner */}
      {pet.status === 'Lost' && (
        <LostPetBanner petName={pet.name} className="mb-4" />
      )}

      {/* Share profile — visible when pet is lost */}
      {pet.status === 'Lost' && (
        <SharePetButton
          petId={pet.id}
          petName={pet.name}
          variant="primary"
          className="mb-4"
        />
      )}

      {pet.status === 'Lost' && (
        <div className="mb-4 rounded-2xl border border-rescue-200 bg-rescue-50 p-4">
          <button
            type="button"
            onClick={() => void handleWhatsAppAvatar()}
            disabled={avatarLoading}
            className="w-full rounded-xl bg-rescue-600 py-3 text-sm font-semibold text-white hover:bg-rescue-700 disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rescue-400"
          >
            {avatarLoading ? 'Generando avatar…' : <><span aria-hidden="true">📱</span> Avatar para WhatsApp</>}
          </button>
          <p className="mt-2 text-xs text-rescue-800">
            Descarga esta imagen y usala como tu foto de perfil en WhatsApp para que tus contactos
            puedan escanear el QR de {pet.name} directamente.
          </p>
        </div>
      )}

      {/* Header */}
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-display font-semibold text-sand-900">{pet.name}</h1>
        <PetStatusBadge status={pet.status} />
      </div>

      <p className="mb-4 inline-flex rounded-full bg-trust-50 px-3 py-1 text-xs font-semibold text-trust-700">
        {todayLabel}
      </p>

      {/* Details grid */}
      <dl className="mb-6 grid grid-cols-2 gap-3 rounded-2xl border border-sand-100 bg-sand-50 p-4 text-sm">
        <div>
          <dt className="text-sand-400">Especie</dt>
          <dd className="font-medium text-sand-800">{{ Dog:'Perro', Cat:'Gato', Bird:'Ave', Rabbit:'Conejo', Other:'Otra' }[pet.species] ?? pet.species}</dd>
        </div>
        {pet.breed && (
          <div>
            <dt className="text-sand-400">Raza</dt>
            <dd className="font-medium text-sand-800">{pet.breed}</dd>
          </div>
        )}
        {pet.birthDate && (
          <div>
            <dt className="text-sand-400">Nacimiento</dt>
            <dd className="font-medium text-sand-800">{pet.birthDate}</dd>
          </div>
        )}
        <div>
          <dt className="text-sand-400">Registrada</dt>
          <dd className="font-medium text-sand-800">
            {formatDate(pet.createdAt)}
          </dd>
        </div>
      </dl>

      {/* Actions */}
      <div className="mb-6 flex flex-wrap gap-3">
        <Link
          to={`/pets/${pet.id}/edit`}
          className="flex-1 rounded-xl border border-sand-300 py-3 text-center text-sm font-semibold text-sand-700 hover:bg-sand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          <span aria-hidden="true">✏</span> Editar
        </Link>
        <button
          type="button"
          onClick={() => setShowQr((v) => !v)}
          aria-expanded={showQr}
          className="flex-1 rounded-xl bg-brand-500 py-3 text-sm font-semibold text-white hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          {showQr ? 'Ocultar QR' : <><span aria-hidden="true">📷</span> Código QR</>}
        </button>
        {confirmDelete ? (
          <div className="flex flex-1 items-center gap-2 rounded-xl border border-danger-200 bg-danger-50 px-3 py-2">
            <span className="flex-1 text-xs font-semibold text-danger-700">¿Eliminar a {pet.name}?</span>
            <button
              type="button"
              onClick={() => void handleDelete()}
              disabled={deleting}
              className="rounded-lg bg-danger-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-danger-700 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400 focus-visible:ring-offset-1"
            >
              {deleting ? 'Eliminando…' : 'Sí, eliminar'}
            </button>
            <button
              type="button"
              onClick={() => setConfirmDelete(false)}
              disabled={deleting}
              className="rounded-lg border border-sand-300 px-3 py-1.5 text-xs font-semibold text-sand-700 hover:bg-sand-100 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              Cancelar
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setConfirmDelete(true)}
            disabled={deleting}
            className="flex-1 rounded-xl border border-danger-200 py-3 text-sm font-semibold text-danger-600 hover:bg-danger-50 disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400"
          >
            <span aria-hidden="true">🗑</span> Eliminar
          </button>
        )}
      </div>

      {/* Sprint 3: Lost pet actions */}
      {activeReport && (
        <div className="mb-6">
          <ReuniteButton
            lostEventId={activeReport.id}
            petId={pet.id}
            petName={pet.name}
            onSuccess={() => navigate('/dashboard')}
          />
        </div>
      )}

      {/* Action checklist — visible while pet is lost */}
      {activeReport && pet.status === 'Lost' && (
        <SearchChecklist
          lostEventId={activeReport.id}
          petName={pet.name}
          className="mb-6"
        />
      )}

      {pet.status === 'Active' && (
        <Link
          to={`/pets/${pet.id}/report-lost`}
          className="mb-6 flex w-full items-center justify-center gap-2 rounded-xl border border-danger-200 py-3 text-sm font-semibold text-danger-600 hover:bg-danger-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger-400 focus-visible:ring-offset-1"
        >
          <span aria-hidden="true">🚨</span> Reportar como perdido
        </Link>
      )}

      {/* QR Panel */}
      {showQr && (
        <div className="rounded-2xl border border-sand-200 bg-white p-6">
          <h2 className="mb-4 text-center text-sm font-semibold text-sand-700">
            Código QR de {pet.name}
          </h2>
          <QRCodeDisplay petId={pet.id} petName={pet.name} />
        </div>
      )}

      <div className="mt-6 rounded-2xl border border-sand-200 bg-white p-4">
        <button
          type="button"
          onClick={() => setShowScanHistory((v) => !v)}
          className="flex w-full items-center justify-between text-left"
        >
          <span className="text-sm font-semibold text-sand-800">Historial de escaneos del QR</span>
          <span className="text-xs text-sand-500">{showScanHistory ? 'Ocultar' : 'Mostrar'}</span>
        </button>

        {showScanHistory && (
          <div className="mt-4 space-y-3">
            {scanHistory && scanHistory.events.length > 0 ? (
              <>
                <ul className="space-y-2">
                  {scanHistory.events.map((event) => {
                    const location = [event.cityName, event.countryCode]
                      .filter(Boolean)
                      .join(', ') || 'Ubicacion aproximada desconocida'

                    return (
                      <li
                        key={`${event.scannedAt}-${event.deviceSummary}`}
                        className="rounded-xl border border-sand-100 bg-sand-50 p-3"
                      >
                        <p className="text-sm font-medium text-sand-800">📍 {location}</p>
                        <p className="text-xs text-sand-500">
                          {new Date(event.scannedAt).toLocaleString()} - {event.deviceSummary}
                        </p>
                      </li>
                    )
                  })}
                </ul>

                <button
                  type="button"
                  onClick={handleExportScanHistoryPdf}
                  className="rounded-lg border border-sand-300 px-3 py-2 text-xs font-semibold text-sand-700 hover:bg-sand-50"
                >
                  Exportar como PDF
                </button>
              </>
            ) : (
              <p className="text-xs text-sand-500">Todavía no hay escaneos registrados para este QR.</p>
            )}
          </div>
        )}
      </div>

      {/* Sightings */}
      <div className="mt-6">
        <h2 className="mb-3 text-sm font-semibold text-sand-700">Avistamientos reportados</h2>
        <SightingList petId={pet.id} />
      </div>
    </main>
  )
}
