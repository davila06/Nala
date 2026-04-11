import { useCallback, useRef, useState } from 'react'
import { petsApi } from '@/features/pets/api/petsApi'

// ── Types ──────────────────────────────────────────────────────────────────────

export type FlyerState = 'idle' | 'loading' | 'ready' | 'error'

export interface UseGenerateFlyerOptions {
  /** Scale factor for the output PNG. 2 = 2× resolution (retina). Default: 2 */
  scale?: number
}

/** Pre-fetched image assets as data URLs, ready for html2canvas rendering. */
export interface FlyerAssets {
  petPhotoDataUrl: string | null
  qrCodeDataUrl: string | null
  recentPhotoDataUrl: string | null
}

export interface UseGenerateFlyerReturn {
  /** Current lifecycle state of the flyer feature. */
  state: FlyerState
  /** Resolved image assets once prepareAssets() has completed successfully. */
  assets: FlyerAssets | null
  /** Pre-fetches the pet photo and QR code as data URLs so html2canvas can render them. */
  prepareAssets: () => Promise<void>
  /** Captures the flyer DOM element and triggers a browser PNG download. */
  downloadFlyer: (flyerRef: React.RefObject<HTMLDivElement | null>) => Promise<void>
  /** Captures the flyer DOM element and returns a Blob for use with navigator.share. */
  buildFlyerBlob: (flyerRef: React.RefObject<HTMLDivElement | null>) => Promise<Blob>
  /**
   * Captures the social-share image DOM element (1200×630) and triggers a
   * browser PNG download as `alerta-{petName}-social.png`.
   */
  downloadSocialImage: (ref: React.RefObject<HTMLDivElement | null>) => Promise<void>
  /**
   * Captures the social-share image DOM element (1200×630) and returns a
   * Blob for use with the Web Share API (`navigator.share`).
   */
  buildSocialImageBlob: (ref: React.RefObject<HTMLDivElement | null>) => Promise<Blob>
  errorMessage: string | null
}

// ── Helpers ────────────────────────────────────────────────────────────────────

/**
 * Fetches a URL (absolute or relative) and converts it to a base-64 data URL.
 * Returns null on network failure so the caller can fall back gracefully.
 */
async function fetchAsDataUrl(url: string): Promise<string | null> {
  try {
    const response = await fetch(url, { mode: 'cors', credentials: 'omit' })
    if (!response.ok) return null
    const blob = await response.blob()
    return blobToDataUrl(blob)
  } catch {
    return null
  }
}

function blobToDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result as string)
    reader.onerror = () => reject(new Error('FileReader error'))
    reader.readAsDataURL(blob)
  })
}

/**
 * Lazy-loads html2canvas and captures `element` as a high-resolution PNG Blob.
 * Uses `useCORS: false` because we pre-convert all images to data URLs before rendering.
 *
 * @param backgroundColor Canvas background fill colour. Defaults to the warm-cream
 *   tone used by the print flyer. Pass `'#ffffff'` for the social-share image.
 */
async function captureElementAsBlob(
  element: HTMLElement,
  scale: number,
  backgroundColor = '#fffbf5',
): Promise<Blob> {
  // Bundle-conditional import: html2canvas is only loaded when this function is called.
  const { default: html2canvas } = await import('html2canvas')

  const canvas = await html2canvas(element, {
    scale,
    useCORS: false,
    allowTaint: false,
    backgroundColor,
    logging: false,
    // Ensure the element dimensions are respected
    width: element.offsetWidth,
    height: element.offsetHeight,
    windowWidth: element.offsetWidth,
    windowHeight: element.offsetHeight,
  })

  return new Promise<Blob>((resolve, reject) => {
    canvas.toBlob(
      (blob) => {
        if (blob) {
          resolve(blob)
        } else {
          reject(new Error('canvas.toBlob returned null'))
        }
      },
      'image/png',
      1.0,
    )
  })
}

function triggerDownload(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  anchor.click()
  // Clean up the object URL after a short delay
  setTimeout(() => URL.revokeObjectURL(url), 30_000)
}

// ── Hook ───────────────────────────────────────────────────────────────────────

/**
 * Manages the full lifecycle of the search-flyer feature:
 * 1. `prepareAssets()` — pre-fetches the pet photo and QR code as data URLs.
 * 2. `downloadFlyer(ref)` — captures the flyer DOM element and downloads as PNG.
 * 3. `buildFlyerBlob(ref)` — captures and returns a Blob for use with navigator.share.
 *
 * html2canvas is loaded lazily (bundle-conditional) only when the user interacts
 * with the flyer feature, keeping the initial page bundle lean.
 */
export function useGenerateFlyer(
  petId: string,
  petName: string,
  petPhotoUrl: string | null,
  recentPhotoUrl: string | null,
  options?: UseGenerateFlyerOptions,
): UseGenerateFlyerReturn {
  const scale = options?.scale ?? 2

  const [state, setState] = useState<FlyerState>('idle')
  const [assets, setAssets] = useState<FlyerAssets | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  // Guard against concurrent calls
  const prepareInProgress = useRef(false)

  const prepareAssets = useCallback(async () => {
    if (prepareInProgress.current || state === 'ready') return
    prepareInProgress.current = true
    setState('loading')
    setErrorMessage(null)

    try {
      const [petPhotoDataUrl, qrBlob, recentPhotoDataUrl] = await Promise.all([
        petPhotoUrl ? fetchAsDataUrl(petPhotoUrl) : Promise.resolve(null),
        petsApi.getQrCode(petId).catch(() => null as Blob | null),
        recentPhotoUrl ? fetchAsDataUrl(recentPhotoUrl) : Promise.resolve(null),
      ])

      const qrCodeDataUrl = qrBlob ? await blobToDataUrl(qrBlob) : null

      setAssets({ petPhotoDataUrl, qrCodeDataUrl, recentPhotoDataUrl })
      setState('ready')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error preparando el flyer'
      setErrorMessage(message)
      setState('error')
    } finally {
      prepareInProgress.current = false
    }
  }, [petId, petPhotoUrl, recentPhotoUrl, state])

  const downloadFlyer = useCallback(
    async (flyerRef: React.RefObject<HTMLDivElement | null>) => {
      if (!flyerRef.current) {
        setErrorMessage('Componente de flyer no encontrado')
        return
      }
      setState('loading')
      try {
        const blob = await captureElementAsBlob(flyerRef.current, scale)
        const safeName = petName.toLowerCase().replace(/\s+/g, '-')
        triggerDownload(blob, `flyer-${safeName}.png`)
        setState('ready')
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Error generando el PNG'
        setErrorMessage(message)
        setState('error')
      }
    },
    [petName, scale],
  )

  const buildFlyerBlob = useCallback(
    async (flyerRef: React.RefObject<HTMLDivElement | null>): Promise<Blob> => {
      if (!flyerRef.current) {
        throw new Error('Componente de flyer no encontrado')
      }
      return captureElementAsBlob(flyerRef.current, scale)
    },
    [scale],
  )

  // ── Social-share image methods (1200×630, scale=1) ─────────────────────────

  const downloadSocialImage = useCallback(
    async (ref: React.RefObject<HTMLDivElement | null>) => {
      if (!ref.current) {
        setErrorMessage('Componente de imagen social no encontrado')
        return
      }
      setState('loading')
      try {
        const blob = await captureElementAsBlob(ref.current, 1, '#ffffff')
        const safeName = petName.toLowerCase().replace(/\s+/g, '-')
        triggerDownload(blob, `alerta-${safeName}-social.png`)
        setState('ready')
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Error generando la imagen social'
        setErrorMessage(message)
        setState('error')
      }
    },
    [petName],
  )

  const buildSocialImageBlob = useCallback(
    async (ref: React.RefObject<HTMLDivElement | null>): Promise<Blob> => {
      if (!ref.current) {
        throw new Error('Componente de imagen social no encontrado')
      }
      return captureElementAsBlob(ref.current, 1, '#ffffff')
    },
    [],
  )

  return {
    state,
    assets,
    prepareAssets,
    downloadFlyer,
    buildFlyerBlob,
    downloadSocialImage,
    buildSocialImageBlob,
    errorMessage,
  }
}
