import { useCallback, useState } from 'react'

// ── Types ──────────────────────────────────────────────────────────────────────

export type ShareStatus = 'idle' | 'sharing' | 'shared' | 'copied' | 'error'

export interface SharePetOptions {
  petId: string
  petName: string
  /** Optional extra context appended to the share text */
  context?: string
  /** Override the base URL (defaults to window.location.origin) */
  baseUrl?: string
}

export interface SharePetResult {
  /** Current status of the share operation */
  status: ShareStatus
  /** Whether the native Web Share API is available in this browser */
  canShare: boolean
  /** Trigger native share sheet */
  share: () => Promise<void>
  /** Copy the public URL to the clipboard */
  copyLink: () => Promise<void>
  /** Open WhatsApp with pre-filled message */
  shareWhatsApp: () => void
  /** Open Facebook share dialog */
  shareFacebook: () => void
  /** Open X / Twitter share dialog */
  shareX: () => void
  /** Reset status back to idle */
  reset: () => void
}

// ── Hook ───────────────────────────────────────────────────────────────────────

/**
 * Encapsulates all sharing logic for a lost-pet public profile.
 *
 * Provides:
 * - Native Web Share API (Chrome Android, Safari iOS, Edge)
 * - Clipboard copy fallback
 * - Deep links to WhatsApp, Facebook, and X
 *
 * The status machine:
 *   'idle' → 'sharing' → 'shared' | 'error'
 *   'idle' → 'copied'
 *   After 3 s: 'shared' | 'copied' | 'error' → 'idle'
 */
export function useSharePet({
  petId,
  petName,
  context,
  baseUrl,
}: SharePetOptions): SharePetResult {
  const [status, setStatus] = useState<ShareStatus>('idle')

  const resolvedBase = baseUrl ?? (typeof window !== 'undefined' ? window.location.origin : '')
  const publicUrl = `${resolvedBase}/p/${petId}`

  const shareTitle = `¡Ayuda a encontrar a ${petName}!`
  const shareText = context
    ? `${petName} está perdido. ${context} Si lo ves, escanea su QR o contacta al dueño.`
    : `${petName} está perdido. Si lo ves, escanea su QR o contacta al dueño.`

  // ── Auto-reset after 3 seconds ─────────────────────────────────────────────

  const resolveWithTimeout = (next: ShareStatus) => {
    setStatus(next)
    setTimeout(() => setStatus('idle'), 3_000)
  }

  // ── Native share ───────────────────────────────────────────────────────────

  const canShare =
    typeof navigator !== 'undefined' &&
    typeof navigator.share === 'function'

  const share = useCallback(async () => {
    if (!canShare) return
    setStatus('sharing')
    try {
      await navigator.share({ title: shareTitle, text: shareText, url: publicUrl })
      resolveWithTimeout('shared')
    } catch (err) {
      // User dismissed the sheet — not an error
      if (err instanceof Error && err.name === 'AbortError') {
        setStatus('idle')
        return
      }
      resolveWithTimeout('error')
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canShare, shareTitle, shareText, publicUrl])

  // ── Clipboard copy ─────────────────────────────────────────────────────────

  const copyLink = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(publicUrl)
      resolveWithTimeout('copied')
    } catch {
      // Clipboard API unavailable (e.g. non-secure context) — silent fallback
      resolveWithTimeout('error')
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [publicUrl])

  // ── Social links ───────────────────────────────────────────────────────────

  const shareWhatsApp = useCallback(() => {
    const encoded = encodeURIComponent(`${shareText}\n${publicUrl}`)
    window.open(`https://wa.me/?text=${encoded}`, '_blank', 'noopener,noreferrer')
  }, [shareText, publicUrl])

  const shareFacebook = useCallback(() => {
    const encoded = encodeURIComponent(publicUrl)
    window.open(
      `https://www.facebook.com/sharer/sharer.php?u=${encoded}`,
      '_blank',
      'noopener,noreferrer,width=600,height=450',
    )
  }, [publicUrl])

  const shareX = useCallback(() => {
    const text = encodeURIComponent(`${shareTitle}\n${publicUrl}`)
    window.open(
      `https://twitter.com/intent/tweet?text=${text}`,
      '_blank',
      'noopener,noreferrer,width=600,height=450',
    )
  }, [shareTitle, publicUrl])

  // ── Reset ──────────────────────────────────────────────────────────────────

  const reset = useCallback(() => setStatus('idle'), [])

  return {
    status,
    canShare,
    share,
    copyLink,
    shareWhatsApp,
    shareFacebook,
    shareX,
    reset,
  }
}
