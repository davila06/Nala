import { useEffect, useState } from 'react'
import { petsApi } from '../api/petsApi'

interface QRCodeDisplayProps {
  petId: string
  petName: string
}

export const QRCodeDisplay = ({ petId, petName }: QRCodeDisplayProps) => {
  const [blobUrl, setBlobUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(false)

  useEffect(() => {
    setLoading(true)
    setError(false)
    petsApi
      .getQrCode(petId)
      .then((blob) => {
        const url = URL.createObjectURL(blob)
        setBlobUrl(url)
      })
      .catch(() => setError(true))
      .finally(() => setLoading(false))

    return () => {
      if (blobUrl) URL.revokeObjectURL(blobUrl)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [petId])

  const handleDownload = () => {
    if (!blobUrl) return
    const a = document.createElement('a')
    a.href = blobUrl
    a.download = `qr-${petName.replace(/\s+/g, '-').toLowerCase()}.png`
    a.click()
  }

  if (loading)
    return (
      <div className="flex h-40 items-center justify-center text-sm text-sand-400">
        Generando QR…
      </div>
    )

  if (error)
    return (
      <div className="flex h-40 items-center justify-center text-sm text-danger-500">
        No se pudo cargar el código QR.
      </div>
    )

  return (
    <div className="flex flex-col items-center gap-3">
      {blobUrl && (
        <img
          src={blobUrl}
          alt={`Código QR de ${petName}`}
          loading="lazy"
          className="size-40 rounded-xl border border-sand-200"
        />
      )}
      <button
        type="button"
        onClick={handleDownload}
        className="flex items-center gap-1.5 rounded-lg bg-brand-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-brand-600"
      >
        ⬇ Descargar QR
      </button>
      <p className="text-center text-xs text-sand-400">
        Imprime este QR y adjúntalo al collar de {petName}.
      </p>
    </div>
  )
}

