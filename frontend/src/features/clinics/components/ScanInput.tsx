import { useEffect, useRef, useState } from 'react'
import { Alert } from '@/shared/ui/Alert'

interface ScanInputProps {
  onScan: (value: string, type: 'Qr' | 'RfidChip') => void
  isLoading?: boolean
}

// BarcodeDetector API type declaration for environments without lib.dom types
interface BarcodeDetector {
  detect(image: ImageBitmapSource): Promise<Array<{ rawValue: string }>>
}
declare const BarcodeDetector: {
  new (options?: { formats?: string[] }): BarcodeDetector
}

export function ScanInput({ onScan, isLoading = false }: ScanInputProps) {
  const [manualInput, setManualInput] = useState('')
  const [cameraActive, setCameraActive] = useState(false)
  const [cameraError, setCameraError] = useState<string | null>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const rafRef = useRef<number>(0)
  const detectorRef = useRef<BarcodeDetector | null>(null)

  const barcodeApiSupported = typeof window !== 'undefined' && 'BarcodeDetector' in window

  // ── Camera QR scanning ────────────────────────────────────────────────────

  async function startCamera() {
    setCameraError(null)
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' },
      })
      streamRef.current = stream
      if (videoRef.current) {
        videoRef.current.srcObject = stream
        await videoRef.current.play()
      }
      setCameraActive(true)

      detectorRef.current = new BarcodeDetector({ formats: ['qr_code'] })
      scanLoop()
    } catch {
      setCameraError('No se pudo acceder a la cámara. Usa el campo manual.')
    }
  }

  function stopCamera() {
    cancelAnimationFrame(rafRef.current)
    streamRef.current?.getTracks().forEach((t) => t.stop())
    streamRef.current = null
    setCameraActive(false)
  }

  function scanLoop() {
    rafRef.current = requestAnimationFrame(async () => {
      if (!videoRef.current || !detectorRef.current) return
      try {
        const results = await detectorRef.current.detect(videoRef.current)
        if (results.length > 0) {
          stopCamera()
          onScan(results[0].rawValue, 'Qr')
          return
        }
      } catch { /* detection failed on this frame, try next */ }
      scanLoop()
    })
  }

  // Cleanup on unmount
  useEffect(() => () => stopCamera(), [])

  // ── Manual submit ─────────────────────────────────────────────────────────

  function handleManualSubmit(e: React.FormEvent) {
    e.preventDefault()
    const value = manualInput.trim()
    if (!value) return

    // Heuristic: URLs are QR, otherwise treat as RFID chip ID
    const type: 'Qr' | 'RfidChip' = value.startsWith('http') ? 'Qr' : 'RfidChip'
    onScan(value, type)
    setManualInput('')
  }

  return (
    <div className="space-y-4">
      {/* ── Camera QR ── */}
      {barcodeApiSupported && (
        <div>
          {cameraActive ? (
            <div className="relative overflow-hidden rounded-2xl bg-black">
              <video
                ref={videoRef}
                className="w-full rounded-2xl"
                playsInline
                muted
              />
              {/* Targeting reticle */}
              <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
                <div className="h-48 w-48 rounded-2xl border-4 border-brand-400 opacity-80" />
              </div>
              <button
                type="button"
                onClick={stopCamera}
                className="absolute right-3 top-3 rounded-full bg-black/60 p-2 text-white"
              >
                ✕
              </button>
            </div>
          ) : (
            <button
              type="button"
              onClick={startCamera}
              disabled={isLoading}
              className="flex w-full items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-brand-400 bg-brand-50 py-5 text-sm font-semibold text-brand-700 hover:bg-brand-100 disabled:opacity-50"
            >
              <span className="text-xl">📷</span>
              Escanear código QR con cámara
            </button>
          )}
          {cameraError && (
            <Alert variant="error" className="mt-1">{cameraError}</Alert>
          )}
        </div>
      )}

      {/* ── Divider ── */}
      <div className="flex items-center gap-3 text-xs text-sand-400">
        <div className="h-px flex-1 bg-sand-200" />
        <span>o ingresa manualmente</span>
        <div className="h-px flex-1 bg-sand-200" />
      </div>

      {/* ── Manual input ── */}
      <form onSubmit={handleManualSubmit} className="flex gap-2">
        <input
          type="text"
          value={manualInput}
          onChange={(e) => setManualInput(e.target.value)}
          placeholder="URL del QR o código de microchip"
          disabled={isLoading}
          className="flex-1 rounded-xl border border-sand-300 px-4 py-3 text-sm outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 disabled:opacity-50"
        />
        <button
          type="submit"
          disabled={!manualInput.trim() || isLoading}
          className="rounded-xl bg-brand-500 px-4 py-3 text-sm font-bold text-white hover:bg-brand-600 disabled:opacity-50"
        >
          {isLoading ? '…' : 'Buscar'}
        </button>
      </form>
    </div>
  )
}

