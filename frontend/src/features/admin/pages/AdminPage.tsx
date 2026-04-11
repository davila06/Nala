import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/store/authStore'
import { usePendingAllies, usePendingClinics, useReviewAlly, useReviewClinic } from '../hooks/useAdmin'
import type { PendingAllyDto, PendingClinicDto } from '../api/adminApi'

type Tab = 'allies' | 'clinics'

export default function AdminPage() {
  const user = useAuthStore((s) => s.user)
  const [activeTab, setActiveTab] = useState<Tab>('allies')

  if (!user || user.role !== 'Admin') {
    return <Navigate to="/dashboard" replace />
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 animate-fade-in-up">
      <h1 className="text-2xl font-bold text-sand-900">Panel de administración</h1>
      <p className="mt-1 text-sm text-sand-500">Revisa y aprueba solicitudes pendientes.</p>

      {/* Tabs */}
      <div className="mt-6 flex gap-2 border-b border-sand-200">
        <TabButton label="Aliados" tab="allies" active={activeTab} onClick={setActiveTab} />
        <TabButton label="Clínicas" tab="clinics" active={activeTab} onClick={setActiveTab} />
      </div>

      <div className="mt-6">
        {activeTab === 'allies' && <AlliesTab />}
        {activeTab === 'clinics' && <ClinicsTab />}
      </div>
    </div>
  )
}

function TabButton({
  label,
  tab,
  active,
  onClick,
}: {
  label: string
  tab: Tab
  active: Tab
  onClick: (t: Tab) => void
}) {
  const isActive = tab === active
  return (
    <button
      type="button"
      onClick={() => onClick(tab)}
      className={`px-4 py-2 text-sm font-semibold border-b-2 transition-colors ${
        isActive
          ? 'border-brand-500 text-brand-600'
          : 'border-transparent text-sand-500 hover:text-sand-700'
      }`}
    >
      {label}
    </button>
  )
}

function AlliesTab() {
  const { data, isLoading, isError } = usePendingAllies()
  const { mutateAsync: review, isPending } = useReviewAlly()
  const [processingId, setProcessingId] = useState<string | null>(null)

  if (isLoading) return <Loading />
  if (isError) return <Error msg="No se pudieron cargar las solicitudes de aliados." />
  if (!data || data.length === 0)
    return <Empty msg="No hay solicitudes de aliados pendientes." />

  const handle = async (ally: PendingAllyDto, approve: boolean) => {
    setProcessingId(ally.userId)
    try {
      await review({ userId: ally.userId, approve })
    } finally {
      setProcessingId(null)
    }
  }

  return (
    <ul className="space-y-4">
      {data.map((ally) => (
        <li key={ally.userId} className="rounded-2xl border border-sand-200 bg-white p-4">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0">
              <p className="truncate font-semibold text-sand-900">{ally.organizationName}</p>
              <p className="text-xs text-sand-500">
                {ally.allyType} · {ally.coverageLabel}
              </p>
              <p className="mt-1 text-xs text-sand-400">
                Aplicación: {new Date(ally.appliedAt).toLocaleDateString('es-CR')}
              </p>
            </div>
            <div className="flex shrink-0 gap-2">
              <ActionButton
                label="Aprobar"
                variant="approve"
                loading={isPending && processingId === ally.userId}
                onClick={() => void handle(ally, true)}
              />
              <ActionButton
                label="Rechazar"
                variant="reject"
                loading={isPending && processingId === ally.userId}
                onClick={() => void handle(ally, false)}
              />
            </div>
          </div>
        </li>
      ))}
    </ul>
  )
}

function ClinicsTab() {
  const { data, isLoading, isError } = usePendingClinics()
  const { mutateAsync: review, isPending } = useReviewClinic()
  const [processingId, setProcessingId] = useState<string | null>(null)

  if (isLoading) return <Loading />
  if (isError) return <Error msg="No se pudieron cargar las clínicas pendientes." />
  if (!data || data.length === 0)
    return <Empty msg="No hay clínicas pendientes de aprobación." />

  const handle = async (clinic: PendingClinicDto, approve: boolean) => {
    setProcessingId(clinic.id)
    try {
      await review({ clinicId: clinic.id, approve })
    } finally {
      setProcessingId(null)
    }
  }

  return (
    <ul className="space-y-4">
      {data.map((clinic) => (
        <li key={clinic.id} className="rounded-2xl border border-sand-200 bg-white p-4">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0">
              <p className="truncate font-semibold text-sand-900">{clinic.name}</p>
              <p className="text-xs text-sand-500">
                Lic: {clinic.licenseNumber} · {clinic.address}
              </p>
              <p className="mt-0.5 text-xs text-sand-400">{clinic.contactEmail}</p>
              <p className="mt-1 text-xs text-sand-400">
                Registro: {new Date(clinic.registeredAt).toLocaleDateString('es-CR')}
              </p>
            </div>
            <div className="flex shrink-0 gap-2">
              <ActionButton
                label="Activar"
                variant="approve"
                loading={isPending && processingId === clinic.id}
                onClick={() => void handle(clinic, true)}
              />
              <ActionButton
                label="Suspender"
                variant="reject"
                loading={isPending && processingId === clinic.id}
                onClick={() => void handle(clinic, false)}
              />
            </div>
          </div>
        </li>
      ))}
    </ul>
  )
}

function ActionButton({
  label,
  variant,
  loading,
  onClick,
}: {
  label: string
  variant: 'approve' | 'reject'
  loading: boolean
  onClick: () => void
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={loading}
      className={`rounded-xl px-3 py-1.5 text-xs font-semibold disabled:opacity-60 ${
        variant === 'approve'
          ? 'bg-rescue-100 text-rescue-800 hover:bg-rescue-200'
          : 'bg-danger-100 text-danger-700 hover:bg-danger-200'
      }`}
    >
      {loading ? '...' : label}
    </button>
  )
}

function Loading() {
  return <p className="text-sm text-sand-500">Cargando...</p>
}

function Error({ msg }: { msg: string }) {
  return <p className="text-sm text-danger-600">{msg}</p>
}

function Empty({ msg }: { msg: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-sand-300 px-6 py-10 text-center">
      <p className="text-sm text-sand-500">{msg}</p>
    </div>
  )
}

