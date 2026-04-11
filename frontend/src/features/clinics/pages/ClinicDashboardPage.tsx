import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  clinicsApi,
  type ClinicScanResultDto,
  type ScanInputType,
} from '../api/clinicsApi'
import { ScanInput } from '../components/ScanInput'
import { MatchResultCard } from '../components/MatchResultCard'

export default function ClinicDashboardPage() {
  const [scanResult, setScanResult] = useState<ClinicScanResultDto | null>(null)

  const { data: clinic, isLoading: clinicLoading } = useQuery({
    queryKey: ['my-clinic'],
    queryFn: () => clinicsApi.getMyClinic(),
  })

  const { mutate: performScan, isPending: scanning, error: scanError } = useMutation({
    mutationFn: ({ input, inputType }: { input: string; inputType: ScanInputType }) =>
      clinicsApi.scan(input, inputType),
    onSuccess: (data) => setScanResult(data),
  })

  function handleScan(value: string, type: 'Qr' | 'RfidChip') {
    setScanResult(null)
    performScan({ input: value, inputType: type })
  }

  function handleReset() {
    setScanResult(null)
  }

  if (clinicLoading) {
    return (
      <div className="min-h-screen bg-sand-50">
        <div className="border-b border-sand-200 bg-white px-4 py-4">
          <div className="mx-auto max-w-lg">
            <div className="flex items-start justify-between">
              <div className="space-y-2">
                <div className="h-5 w-48 animate-pulse rounded-lg bg-sand-200" />
                <div className="h-3.5 w-32 animate-pulse rounded-lg bg-sand-100" />
              </div>
              <div className="h-6 w-16 animate-pulse rounded-full bg-sand-200" />
            </div>
          </div>
        </div>
        <div className="mx-auto max-w-lg space-y-4 px-4 py-6">
          <div className="h-5 w-36 animate-pulse rounded-lg bg-sand-200" />
          <div className="h-4 w-64 animate-pulse rounded-lg bg-sand-100" />
          <div className="h-24 animate-pulse rounded-2xl bg-sand-100" />
        </div>
      </div>
    )
  }

  // ── Suspended/Pending guard ───────────────────────────────────────────────

  if (clinic && clinic.status !== 'Active') {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center bg-sand-50 px-4">
        <p className="text-4xl">🔒</p>
        <h1 className="mt-3 text-lg font-extrabold text-sand-900">
          {clinic.status === 'Pending' ? 'Cuenta pendiente de activación' : 'Cuenta suspendida'}
        </h1>
        <p className="mt-2 max-w-xs text-center text-sm text-sand-500">
          {clinic.status === 'Pending'
            ? 'Tu clínica está en revisión. PawTrack activará tu cuenta en 1-2 días hábiles.'
            : 'Tu cuenta ha sido suspendida. Contacta al equipo de PawTrack para más información.'}
        </p>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-sand-50">
      {/* ── Header ── */}
      <header className="border-b border-sand-200 bg-white px-4 py-4">
        <div className="mx-auto max-w-lg">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-lg font-extrabold text-sand-900">
                🏥 {clinic?.name ?? 'Portal veterinaria'}
              </h1>
              {clinic && (
                <p className="text-xs text-sand-400">
                  Licencia SENASA: {clinic.licenseNumber}
                </p>
              )}
            </div>
            <span className="rounded-full bg-rescue-100 px-2.5 py-0.5 text-xs font-semibold text-rescue-700">
              Activa
            </span>
          </div>
        </div>
      </header>

      {/* ── Main ── */}
      <main className="mx-auto max-w-lg animate-fade-in-up px-4 py-6 space-y-6">
        {scanResult ? (
          <MatchResultCard result={scanResult} onReset={handleReset} />
        ) : (
          <>
            <div>
              <h2 className="text-base font-bold text-sand-800">Escanear mascota</h2>
              <p className="text-sm text-sand-500">
                Escanea el código QR del collar o ingresa el número de microchip RFID.
              </p>
            </div>

            <ScanInput onScan={handleScan} isLoading={scanning} />

            {scanError && (
              <p className="rounded-xl bg-danger-50 px-4 py-3 text-sm text-danger-600">
                {scanError instanceof Error
                  ? scanError.message
                  : 'Error al procesar el escaneo. Intenta de nuevo.'}
              </p>
            )}
          </>
        )}
      </main>
    </div>
  )
}

